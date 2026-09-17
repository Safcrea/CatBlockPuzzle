/*
 psd2ui_export.jsx — Photoshop ExtendScript
 Exports every layer as PNG + layout.json for Unity UI import.
*/

#target photoshop
app.bringToFront();

var _savedRulerUnits = app.preferences.rulerUnits;
var _savedTypeUnits = app.preferences.typeUnits;
app.preferences.rulerUnits = Units.PIXELS;
app.preferences.typeUnits = TypeUnits.PIXELS;

var SNAPSHOT_NAME = "PSDTOUI_EXPORT_SNAP";

if (app.documents.length === 0) {
    alert("Open a PSD document first.");
} else {
    var originalDoc = app.activeDocument;
    var outputFolder = Folder.selectDialog("Select export folder for PNG + layout.json");
    if (outputFolder) {
        var exportTextAsImage = showTextExportDialog();

        var layout = {
            version: 2,
            document: {
                width: originalDoc.width.value,
                height: originalDoc.height.value,
                name: originalDoc.name
            },
            nodes: [],
            layers: []
        };

        var counterObj = { value: 0 };
        var totalArtLayers = countArtLayers(originalDoc.layers);

        var progress = createProgressWindow(totalArtLayers);
        progress.show();

        // Phase 1: Create working copy & pre-process
        progress.setPhase("Phase 1/3: Pre-processing", 0, totalArtLayers);
        var workDoc = originalDoc.duplicate("TEMP_EXPORT_WORK", false);
        makeBackgroundEditable(workDoc);
        unlockAllLayers(workDoc.layers);

        var savedDM = app.displayDialogs;
        app.displayDialogs = DialogModes.NO;
        rasterizeAllLayers(workDoc, workDoc.layers, progress);
        app.displayDialogs = savedDM;

        hideAllLayers(workDoc.layers);
        var snapshotId = takeSnapshot(workDoc);

        // Phase 2: Walk layer tree & export each layer
        progress.setPhase("Phase 2/3: Exporting PNGs", 0, totalArtLayers);
        exportLayerCollection(
            originalDoc, workDoc, originalDoc.layers,
            "", "", layout, outputFolder, counterObj, snapshotId, progress, exportTextAsImage
        );

        // Phase 3: Cleanup
        progress.setPhase("Phase 3/3: Saving layout.json", 0, 1);
        try { workDoc.close(SaveOptions.DONOTSAVECHANGES); } catch (e) {}
        writeLayoutJson(outputFolder, layout);

        progress.close();

        alert(
            "Export finished.\n" +
            "Nodes: " + layout.nodes.length + "\n" +
            "Exported PNGs: " + counterObj.value + "\n" +
            "Folder: " + outputFolder.fsName
        );
    }
}

app.preferences.rulerUnits = _savedRulerUnits;
app.preferences.typeUnits = _savedTypeUnits;


// --- Text export mode dialog ---

function showTextExportDialog() {
    var dlg = new Window("dialog", "Text Layer Export Mode");
    dlg.orientation = "column";
    dlg.alignChildren = ["fill", "top"];
    dlg.preferredSize = [380, -1];

    dlg.add("statictext", undefined, "How should text layers be handled?");
    dlg.add("statictext", undefined, " ");

    var desc1 = dlg.add("statictext", undefined,
        "Export as Images: Rasterize text to PNG. Preserves exact visual appearance including effects, but text is not editable in Unity.",
        { multiline: true });
    desc1.preferredSize = [360, 40];

    dlg.add("statictext", undefined, " ");

    var desc2 = dlg.add("statictext", undefined,
        "Export as Text: Skip PNG for text layers. Text content is stored as metadata and imported as editable TextMeshPro in Unity.",
        { multiline: true });
    desc2.preferredSize = [360, 40];

    dlg.add("statictext", undefined, " ");

    var btnGroup = dlg.add("group");
    btnGroup.alignment = ["center", "top"];
    btnGroup.spacing = 16;

    var btnImage = btnGroup.add("button", undefined, "Export as Images");
    btnImage.preferredSize = [160, 32];

    var btnText = btnGroup.add("button", undefined, "Export as Text");
    btnText.preferredSize = [160, 32];

    btnImage.onClick = function () { dlg.close(1); };
    btnText.onClick = function () { dlg.close(2); };

    dlg.defaultElement = btnImage;

    var result = dlg.show();

    return (result === 1);
}


// --- Layer tree walker ---

function exportLayerCollection(originalDoc, workDoc, layers, parentId, parentPath, layout, outputFolder, counterObj, snapshotId, progress, exportTextAsImage) {
    if (!layers || layers.length === 0) return;

    for (var i = layers.length - 1; i >= 0; i--) {
        var layer = layers[i];
        var nodeId = parentPath ? (parentPath + "/" + i) : String(i);

        var bounds = getLayerBoundsSafe(layer);
        var isGroup = layer.typename === "LayerSet";
        var isArtLayer = layer.typename === "ArtLayer";
        var isText = false;
        var textValue = "";

        if (isArtLayer) {
            try {
                isText = layer.kind === LayerKind.TEXT;
                if (isText && layer.textItem) {
                    textValue = layer.textItem.contents || "";
                }
            } catch (e) {}
        }

        var fileName = "";
        var finalX = bounds.x;
        var finalY = bounds.y;
        var finalWidth = bounds.width;
        var finalHeight = bounds.height;

        var shouldExportPng = isArtLayer && bounds.width > 0 && bounds.height > 0;
        if (isText && !exportTextAsImage) {
            shouldExportPng = false;
        }

        if (shouldExportPng) {
            progress.update(safeString(layer.name));
            var exportResult = exportSingleLayer(
                workDoc, nodeId, outputFolder, counterObj,
                layer.name, snapshotId
            );
            if (exportResult) {
                fileName = exportResult.file || "";
                if (fileName) {
                    finalX = exportResult.x;
                    finalY = exportResult.y;
                    finalWidth = exportResult.width;
                    finalHeight = exportResult.height;
                }
            }
        }

        var node = {
            id: nodeId,
            parentId: parentId,
            name: safeString(layer.name),
            file: fileName,
            x: finalX,
            y: finalY,
            width: finalWidth,
            height: finalHeight,
            opacity: safeOpacity(layer),
            visible: safeVisible(layer),
            isText: isText,
            text: textValue,
            isGroup: isGroup,
            order: i
        };

        layout.nodes.push(node);

        if (!isGroup) {
            layout.layers.push({
                name: node.name, file: node.file,
                x: node.x, y: node.y,
                width: node.width, height: node.height,
                opacity: node.opacity, visible: node.visible,
                isText: node.isText, text: node.text
            });
        }

        if (isGroup) {
            exportLayerCollection(
                originalDoc, workDoc, layer.layers, nodeId, nodeId,
                layout, outputFolder, counterObj, snapshotId, progress, exportTextAsImage
            );
        }
    }
}


// --- Single-layer export ---

function exportSingleLayer(workDoc, layerPath, outputFolder, counterObj, sourceLayerName, snapshotId) {
    var fileName = buildFileName(layerPath, sourceLayerName, counterObj.value);
    counterObj.value++;
    var outputFile = new File(outputFolder.fsName + "/" + fileName);

    try {
        revertToSnapshot(workDoc, snapshotId);

        var targetLayer = findLayerByPath(workDoc, layerPath);
        if (!targetLayer) return null;

        showParentChain(targetLayer);
        targetLayer.visible = true;

        var isClipped = false;
        try { isClipped = !!targetLayer.grouped; } catch (e) {}

        if (isClipped) {
            showRequiredClippingChain(targetLayer);
        }

        var tempHelper = workDoc.artLayers.add();
        tempHelper.name = "MERGE_HELPER";
        tempHelper.visible = true;

        var savedDM = app.displayDialogs;
        app.displayDialogs = DialogModes.NO;
        try {
            workDoc.mergeVisibleLayers();
        } catch (mergeErr) {
            app.displayDialogs = savedDM;
            revertToSnapshot(workDoc, snapshotId);
            return null;
        }
        app.displayDialogs = savedDM;

        var renderedLayer = workDoc.activeLayer;

        var renderBounds = getLayerBoundsSafe(renderedLayer);
        if (renderBounds.width <= 0 || renderBounds.height <= 0) {
            revertToSnapshot(workDoc, snapshotId);
            return null;
        }

        try {
            workDoc.crop([
                UnitValue(renderBounds.x, "px"),
                UnitValue(renderBounds.y, "px"),
                UnitValue(renderBounds.x + renderBounds.width, "px"),
                UnitValue(renderBounds.y + renderBounds.height, "px")
            ]);
        } catch (cropErr) {}

        savePngFast(workDoc, outputFile);
        revertToSnapshot(workDoc, snapshotId);

        if (!outputFile.exists) return null;

        return {
            file: fileName,
            x: renderBounds.x,
            y: renderBounds.y,
            width: renderBounds.width,
            height: renderBounds.height
        };
    } catch (err) {
        try { revertToSnapshot(workDoc, snapshotId); } catch (e) {}
        return null;
    }
}


// --- Pre-rasterization ---

function rasterizeAllLayers(doc, layers, progress) {
    if (!layers) return;

    for (var i = 0; i < layers.length; i++) {
        var layer = layers[i];

        if (layer.typename === "LayerSet") {
            rasterizeAllLayers(doc, layer.layers, progress);
            continue;
        }

        if (layer.typename !== "ArtLayer") continue;

        progress.update(safeString(layer.name));

        try {
            if (layer.kind === LayerKind.TEXT) continue;
        } catch (e) {}

        var wasVisible = layer.visible;
        layer.visible = true;

        try {
            doc.activeLayer = layer;

            amRasterizeLayerStyle();

            if (amHasVectorMask()) {
                amRasterizeLayer();
                amSelectVectorMask();
                amRasterizeVectorMask();
                amApplyLayerMask();
            }

            if (amHasLayerMask()) {
                amRasterizeLayer();
                amSelectLayerMask();
                amApplyLayerMask();
            }

            layer.rasterize(RasterizeType.ENTIRELAYER);
        } catch (e) {}

        layer.visible = wasVisible;
    }
}


// --- ActionManager rasterization ---

function amRasterizeLayerStyle() {
    try {
        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        desc.putReference(charIDToTypeID("null"), ref);
        desc.putEnumerated(charIDToTypeID("What"), stringIDToTypeID("rasterizeItem"), stringIDToTypeID("layerStyle"));
        executeAction(stringIDToTypeID("rasterizeLayer"), desc, DialogModes.NO);
    } catch (e) {}
}

function amRasterizeLayer() {
    try {
        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        desc.putReference(charIDToTypeID("null"), ref);
        executeAction(stringIDToTypeID("rasterizeLayer"), desc, DialogModes.NO);
    } catch (e) {}
}

function amHasVectorMask() {
    try {
        var ref = new ActionReference();
        var keyVM = stringIDToTypeID("vectorMask");
        ref.putEnumerated(charIDToTypeID("Path"), charIDToTypeID("Ordn"), keyVM);
        var desc = executeActionGet(ref);
        if (desc.hasKey(charIDToTypeID("Knd "))) {
            return desc.getEnumerationValue(charIDToTypeID("Knd ")) === keyVM;
        }
    } catch (e) {}
    return false;
}

function amHasLayerMask() {
    try {
        var ref = new ActionReference();
        var keyUM = charIDToTypeID("UsrM");
        ref.putProperty(charIDToTypeID("Prpr"), keyUM);
        ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        var desc = executeActionGet(ref);
        return desc.hasKey(keyUM);
    } catch (e) {}
    return false;
}

function amSelectVectorMask() {
    try {
        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putEnumerated(charIDToTypeID("Path"), charIDToTypeID("Path"), stringIDToTypeID("vectorMask"));
        ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        desc.putReference(charIDToTypeID("null"), ref);
        executeAction(charIDToTypeID("slct"), desc, DialogModes.NO);
    } catch (e) {}
}

function amSelectLayerMask() {
    try {
        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putEnumerated(charIDToTypeID("Chnl"), charIDToTypeID("Chnl"), charIDToTypeID("Msk "));
        desc.putReference(charIDToTypeID("null"), ref);
        desc.putBoolean(charIDToTypeID("MkVs"), false);
        executeAction(charIDToTypeID("slct"), desc, DialogModes.NO);
    } catch (e) {}
}

function amRasterizeVectorMask() {
    try {
        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        desc.putReference(charIDToTypeID("null"), ref);
        desc.putEnumerated(charIDToTypeID("What"), stringIDToTypeID("rasterizeItem"), stringIDToTypeID("vectorMask"));
        executeAction(stringIDToTypeID("rasterizeLayer"), desc, DialogModes.NO);
    } catch (e) {}
}

function amApplyLayerMask() {
    try {
        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putEnumerated(charIDToTypeID("Chnl"), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        desc.putReference(charIDToTypeID("null"), ref);
        desc.putBoolean(charIDToTypeID("Aply"), true);
        executeAction(charIDToTypeID("Dlt "), desc, DialogModes.NO);
    } catch (e) {}
}


// --- Snapshot management ---

function takeSnapshot(doc) {
    var desc = new ActionDescriptor();
    var refSnap = new ActionReference();
    refSnap.putClass(charIDToTypeID("SnpS"));
    desc.putReference(charIDToTypeID("null"), refSnap);
    var refFrom = new ActionReference();
    refFrom.putProperty(charIDToTypeID("HstS"), charIDToTypeID("CrnH"));
    desc.putReference(charIDToTypeID("From"), refFrom);
    desc.putString(charIDToTypeID("Nm  "), SNAPSHOT_NAME);
    executeAction(charIDToTypeID("Mk  "), desc, DialogModes.NO);
    return SNAPSHOT_NAME;
}

function revertToSnapshot(doc, snapshotName) {
    var states = doc.historyStates;
    for (var i = states.length - 1; i >= 0; i--) {
        if (states[i].snapshot && states[i].name === snapshotName) {
            doc.activeHistoryState = states[i];
            return;
        }
    }
    for (var j = states.length - 1; j >= 0; j--) {
        if (states[j].snapshot) {
            doc.activeHistoryState = states[j];
            return;
        }
    }
}


// --- Layer utilities ---

function hideAllLayers(layers) {
    if (!layers) return;
    for (var i = 0; i < layers.length; i++) {
        try { layers[i].visible = false; } catch (e) {}
        if (layers[i].typename === "LayerSet") {
            hideAllLayers(layers[i].layers);
        }
    }
}

function unlockAllLayers(layers) {
    if (!layers) return;
    for (var i = 0; i < layers.length; i++) {
        try {
            if (layers[i].allLocked) layers[i].allLocked = false;
        } catch (e) {}
        if (layers[i].typename === "LayerSet") {
            unlockAllLayers(layers[i].layers);
        }
    }
}

function showParentChain(layer) {
    var current = layer;
    while (current) {
        try { current.visible = true; } catch (e) {}
        if (!current.parent || current.parent.typename === "Document") break;
        current = current.parent;
    }
}

function showRequiredClippingChain(layer) {
    var current = layer;
    while (current) {
        var isClipped = false;
        try { isClipped = !!current.grouped; } catch (e) {}
        if (!isClipped) break;

        var baseLayer = findLayerBelow(current);
        if (!baseLayer) break;

        showParentChain(baseLayer);
        try { baseLayer.visible = true; } catch (e) {}
        current = baseLayer;
    }
}

function findLayerBelow(layer) {
    if (!layer || !layer.parent || !layer.parent.layers) return null;
    var siblings = layer.parent.layers;
    for (var i = 0; i < siblings.length; i++) {
        if (siblings[i] === layer) {
            var belowIndex = i + 1;
            if (belowIndex < siblings.length) return siblings[belowIndex];
            return null;
        }
    }
    return null;
}

function findLayerByPath(doc, path) {
    if (!doc || !path) return null;
    var parts = path.split("/");
    var siblings = doc.layers;
    var current = null;

    for (var i = 0; i < parts.length; i++) {
        var index = parseInt(parts[i], 10);
        if (isNaN(index) || !siblings || index < 0 || index >= siblings.length) return null;
        current = siblings[index];
        siblings = current.layers;
    }
    return current;
}

function getLayerBoundsSafe(layer) {
    try {
        var b = layer.bounds;
        var left = b[0].value;
        var top = b[1].value;
        var right = b[2].value;
        var bottom = b[3].value;
        return {
            x: left,
            y: top,
            width: Math.max(0, right - left),
            height: Math.max(0, bottom - top)
        };
    } catch (e) {
        return { x: 0, y: 0, width: 0, height: 0 };
    }
}

function makeBackgroundEditable(doc) {
    try {
        if (doc.backgroundLayer) {
            doc.activeLayer = doc.backgroundLayer;
            doc.activeLayer.isBackgroundLayer = false;
        }
    } catch (e) {}
}

function safeOpacity(layer) {
    try { return layer.opacity / 100.0; } catch (e) { return 1.0; }
}

function safeVisible(layer) {
    try { return !!layer.visible; } catch (e) { return true; }
}

function safeString(value) {
    if (value === undefined || value === null) return "";
    return String(value);
}


// --- Progress bar ---

function createProgressWindow(totalLayers) {
    var win = new Window("palette", "PSD Export Progress", undefined, { closeButton: false });
    win.orientation = "column";
    win.alignChildren = ["fill", "top"];
    win.preferredSize = [420, 140];

    var phaseText = win.add("statictext", undefined, "Initializing...");
    phaseText.alignment = ["fill", "top"];

    var layerText = win.add("statictext", undefined, " ");
    layerText.alignment = ["fill", "top"];

    var bar = win.add("progressbar", undefined, 0, totalLayers);
    bar.preferredSize = [400, 20];

    var counterText = win.add("statictext", undefined, "0 / " + totalLayers);
    counterText.alignment = ["center", "top"];

    var current = 0;
    var phaseTotal = totalLayers;

    return {
        show: function () { win.show(); },
        close: function () { win.close(); },
        setPhase: function (phaseName, startVal, total) {
            phaseText.text = phaseName;
            current = startVal;
            phaseTotal = total;
            bar.minvalue = 0;
            bar.maxvalue = total;
            bar.value = startVal;
            counterText.text = startVal + " / " + total;
            win.update();
        },
        update: function (layerName) {
            current++;
            bar.value = current;
            layerText.text = layerName || "";
            counterText.text = current + " / " + phaseTotal;
            win.update();
        }
    };
}

function countArtLayers(layers) {
    if (!layers) return 0;
    var count = 0;
    for (var i = 0; i < layers.length; i++) {
        if (layers[i].typename === "LayerSet") {
            count += countArtLayers(layers[i].layers);
        } else if (layers[i].typename === "ArtLayer") {
            count++;
        }
    }
    return count;
}


// --- PNG save ---

function savePngFast(doc, outputFile) {
    var pngOpts = new PNGSaveOptions();
    pngOpts.compression = 6;
    pngOpts.interlaced = false;
    doc.saveAs(outputFile, pngOpts, true, Extension.LOWERCASE);
}


// --- File name helpers ---

function buildFileName(layerPath, layerName, counter) {
    var pathPart = layerPath.split("/").join("-").split("\\").join("-");
    var namePart = toAsciiSlug(layerName);
    return "L_" + pathPart + "_" + namePart + "_" + pad(counter, 4) + ".png";
}

function toAsciiSlug(value) {
    if (!value) return "layer";
    var text = String(value);
    var out = "";
    for (var i = 0; i < text.length; i++) {
        var c = text.charCodeAt(i);
        if (c >= 0x20 && c <= 0x7E) {
            out += text.charAt(i);
        } else {
            out += "_";
        }
    }
    out = out.split("\\").join("_");
    out = out.split("/").join("_");
    out = out.split(":").join("_");
    out = out.split("*").join("_");
    out = out.split("?").join("_");
    out = out.split("\"").join("_");
    out = out.split("<").join("_");
    out = out.split(">").join("_");
    out = out.split("|").join("_");
    out = out.split(" ").join("_");
    while (out.indexOf("__") >= 0) {
        out = out.split("__").join("_");
    }
    while (out.length > 0 && out.charAt(0) === "_") out = out.substring(1);
    while (out.length > 0 && out.charAt(out.length - 1) === "_") out = out.substring(0, out.length - 1);

    if (out.length === 0) out = "layer";
    if (out.length > 48) out = out.substring(0, 48);
    return out;
}

function pad(num, size) {
    var s = String(num);
    while (s.length < size) s = "0" + s;
    return s;
}


// --- JSON writer ---

function writeLayoutJson(folder, layoutObj) {
    var file = new File(folder.fsName + "/layout.json");
    file.encoding = "UTF8";
    file.open("w");
    file.write(stringify(layoutObj, 2));
    file.close();
}

function stringify(value, indentSize) {
    var indentUnit = "  ";
    if (indentSize === 4) indentUnit = "    ";
    return stringifyValue(value, "", indentUnit);
}

function stringifyValue(value, currentIndent, indentUnit) {
    if (value === null) return "null";
    var t = typeof value;
    if (t === "string") return quoteString(value);
    if (t === "number") return isFinite(value) ? String(value) : "0";
    if (t === "boolean") return value ? "true" : "false";

    if (value.constructor === Array) {
        if (value.length === 0) return "[]";
        var arrNext = currentIndent + indentUnit;
        var arrOut = "[\n";
        for (var i = 0; i < value.length; i++) {
            arrOut += arrNext + stringifyValue(value[i], arrNext, indentUnit);
            if (i < value.length - 1) arrOut += ",";
            arrOut += "\n";
        }
        arrOut += currentIndent + "]";
        return arrOut;
    }

    var keys = [];
    for (var key in value) {
        if (value.hasOwnProperty(key)) keys.push(key);
    }
    if (keys.length === 0) return "{}";

    var objNext = currentIndent + indentUnit;
    var out = "{\n";
    for (var k = 0; k < keys.length; k++) {
        var name = keys[k];
        out += objNext + quoteString(name) + ": " + stringifyValue(value[name], objNext, indentUnit);
        if (k < keys.length - 1) out += ",";
        out += "\n";
    }
    out += currentIndent + "}";
    return out;
}

function quoteString(s) {
    var escaped = String(s);
    escaped = escaped.split("\\").join("\\\\");
    escaped = escaped.split("\"").join("\\\"");
    escaped = escaped.split("\r").join("\\r");
    escaped = escaped.split("\n").join("\\n");
    escaped = escaped.split("\t").join("\\t");
    return "\"" + escaped + "\"";
}
