using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CatBlocksUI.Editor {
    // A fresh catalog per scan/build avoids stale paths after files are moved in the Project window.
    public sealed class LayoutAssetResolver {
        public readonly string Root;
        readonly string layoutDirectory;
        readonly string[] files;

        [Serializable] public sealed class AssetReference {
            public string id, requestedPath, resolvedPath, method, error;
            public string[] elements;
        }
        [Serializable] public sealed class ScanReport {
            public string layout, uiFolder;
            public List<AssetReference> assets = new List<AssetReference>();
            public List<string> errors = new List<string>();
            public bool IsValid { get { return errors.Count == 0; } }
        }
        [Serializable] sealed class Header {
            public int schemaVersion;
            public string coordinateSystem;
            public ElementRecord[] elements;
        }

        public LayoutAssetResolver(string root, string layoutPath) {
            Root = RequireFolder(root);
            layoutDirectory = Path.GetDirectoryName(layoutPath).Replace('\\', '/');
            if (!Inside(layoutPath, Root)) throw new InvalidDataException("The layout must be inside the selected UI folder.");
            files = ImportedFiles(Root)
                .Select(p => p.Replace('\\', '/')).Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        // Match Unity's ignored-folder conventions; do not scan backup/test projects or hidden folders.
        static IEnumerable<string> ImportedFiles(string folder) {
            foreach (var file in Directory.EnumerateFiles(folder)) {
                string name = Path.GetFileName(file);
                if (!name.StartsWith(".", StringComparison.Ordinal) && !name.EndsWith("~", StringComparison.Ordinal)) yield return file;
            }
            foreach (var child in Directory.EnumerateDirectories(folder)) {
                string name = Path.GetFileName(child);
                if (name.StartsWith(".", StringComparison.Ordinal) || name.EndsWith("~", StringComparison.Ordinal) || name.Equals("CVS", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var file in ImportedFiles(child)) yield return file;
            }
        }

        public static string RequireFolder(string path) {
            if (string.IsNullOrEmpty(path)) throw new InvalidDataException("Choose a UI folder under Assets.");
            string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string assets = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(full, assets, StringComparison.OrdinalIgnoreCase) && !full.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The UI folder must be inside this project's Assets folder.");
            if (!Directory.Exists(full)) throw new DirectoryNotFoundException(full);
            return "Assets" + full.Substring(assets.Length).Replace('\\', '/');
        }

        public static bool Inside(string path, string root) {
            return Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        public static string[] Discover(string folder, out string[] warnings) {
            folder = RequireFolder(folder);
            var found = new List<string>();
            var issues = new List<string>();
            foreach (var file in ImportedFiles(folder).Where(p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal)) {
                try {
                    var h = JsonUtility.FromJson<Header>(File.ReadAllText(file));
                    if (h != null && h.schemaVersion == 1 && h.coordinateSystem == "top-left-pixels" && h.elements != null)
                        found.Add(file.Replace('\\', '/'));
                } catch (Exception ex) {
                    // Other JSON formats (including arrays) are not layout documents.
                    if (Path.GetFileName(file).Equals("layout.json", StringComparison.OrdinalIgnoreCase)) issues.Add(file + ": " + ex.Message);
                }
            }
            warnings = issues.ToArray();
            return found.ToArray();
        }

        public string Resolve(AssetRecord asset, out string method) {
            method = "";
            if (!string.IsNullOrEmpty(asset.guid)) {
                string p = AssetDatabase.GUIDToAssetPath(asset.guid);
                if (!string.IsNullOrEmpty(p) && Inside(p, Root) && File.Exists(p)) { method = "GUID"; return p; }
            }
            string relative = (string.IsNullOrEmpty(asset.path) ? asset.filename : asset.path) ?? "";
            relative = relative.Replace('\\', '/');
            if (string.IsNullOrEmpty(relative)) throw new FileNotFoundException("Asset '" + asset.id + "' needs path, filename or a valid GUID.");
            if (Path.IsPathRooted(relative) || relative.Split('/').Contains("..")) throw new InvalidDataException("Asset paths must stay inside the selected UI folder: " + relative);
            string direct = relative.StartsWith("Assets/", StringComparison.Ordinal) ? relative : Root + "/" + relative;
            if (Inside(direct, Root) && File.Exists(direct)) { method = "Exact path"; return direct; }
            string local = layoutDirectory + "/" + relative;
            if (Inside(local, Root) && File.Exists(local)) { method = "Relative to JSON"; return local; }

            string filename = Path.GetFileName(relative);
            var candidates = files.Where(p => Path.GetFileName(p).Equals(filename, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (candidates.Length == 0) throw new FileNotFoundException("Missing asset '" + asset.id + "': " + relative);
            // Preserve as much of a moved path as possible before falling back to a unique filename.
            var segments = relative.Split('/');
            for (int count = segments.Length; count >= 2; count--) {
                string suffix = "/" + string.Join("/", segments.Skip(segments.Length - count));
                var matches = candidates.Where(p => p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length == 1) { method = "Matching path suffix"; return matches[0]; }
                if (matches.Length > 1) break;
            }
            if (candidates.Length == 1) { method = "Unique filename"; return candidates[0]; }
            throw new InvalidDataException("Ambiguous asset '" + asset.id + "' (" + relative + "). Set an exact path or GUID. Candidates: " + string.Join(", ", candidates));
        }

        public ScanReport Inspect(LayoutDocument doc, string layoutPath, bool convertTextures) {
            var report = new ScanReport { layout = layoutPath, uiFolder = Root };
            foreach (var asset in doc.assets) {
                var item = new AssetReference { id = asset.id, requestedPath = asset.path ?? asset.filename,
                    elements = doc.elements.Where(e => e.assetId == asset.id || e.onAssetId == asset.id || e.offAssetId == asset.id || e.onKnobAssetId == asset.id || e.offKnobAssetId == asset.id).Select(e => e.id).ToArray() };
                report.assets.Add(item);
                try {
                    string method;
                    item.resolvedPath = Resolve(asset, out method); item.method = method;
                    var importer = AssetImporter.GetAtPath(item.resolvedPath) as TextureImporter;
                    if (!importer) throw new InvalidDataException("Expected a texture at " + item.resolvedPath);
                    if (importer.textureType != TextureImporterType.Sprite) {
                        if (!convertTextures || !string.IsNullOrEmpty(asset.spriteName)) throw new InvalidDataException("Import as a Sprite first: " + item.resolvedPath);
                        item.method += " (will import as Sprite)";
                    } else LoadSprite(asset, item.resolvedPath);
                } catch (Exception ex) { item.error = ex.Message; report.errors.Add(ex.Message); }
            }
            return report;
        }

        public static Sprite LoadSprite(AssetRecord asset, string path) {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (!string.IsNullOrEmpty(asset.spriteName)) {
                var matches = sprites.Where(s => s.name == asset.spriteName).ToArray();
                if (matches.Length == 1) return matches[0];
                throw new InvalidDataException("Sprite '" + asset.spriteName + "' was not found uniquely in " + path);
            }
            if (sprites.Length == 1) return sprites[0];
            throw new InvalidDataException("Expected one Sprite in " + path + ". For a sliced sheet, specify assets[].spriteName.");
        }
    }
}
