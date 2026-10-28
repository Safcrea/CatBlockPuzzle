using System;
using UnityEngine;

namespace CatBlocksUI {
    [Serializable] public class PixelSize {public int width,height;}
    [Serializable] public class ReferenceImage {public string filename;public int width,height;}
    [Serializable] public class AnchorData {public Vector2 min,max;}
    [Serializable] public class ScreenGrouping {public string screen,type,canvas,popup;}
    [Serializable] public class AssetPlacement {
        public string instanceId,assetId,filename,state,placementGroup,screenGroup,popupGroup;
        public bool active;
        public int layerOrder;
        public LayoutRect screenRect,visibleBounds,hitbox;
        public Vector2 pivot;
        public AnchorData anchor;
    }
    [Serializable] public class LayoutRect {
        public float x,y,width,height;
        public Vector2 size {get{return new Vector2(width,height);}}
        public Vector2 position {get{return new Vector2(x,y);}}
        public static implicit operator Rect(LayoutRect r){return new Rect(r.x,r.y,r.width,r.height);}
        public static implicit operator LayoutRect(Rect r){return new LayoutRect{x=r.x,y=r.y,width=r.width,height=r.height};}
    }
    [Serializable] public class LayoutDocument {
        public int schemaVersion = 1;
        public int specificationVersion = 2;
        public string screenId;
        public string source,boundsConvention;
        public PixelSize outputResolution,sourceOriginalResolution;
        public ReferenceImage referenceImage;
        public ScreenGrouping grouping;
        public string coordinateSystem = "top-left-pixels";
        public Vector2 referenceResolution = new Vector2(1080,1920);
        public Vector2 sourceResolution = new Vector2(941,1672);
        public float canvasMatch = 0.5f;
        public string previewBackdropScreen;
        public bool modal;
        public AssetRecord[] assets = Array.Empty<AssetRecord>();
        public ElementRecord[] elements = Array.Empty<ElementRecord>();
    }
    [Serializable] public class AssetRecord {
        public string id, path, provenance;
        public string guid, spriteName;
        public string filename,state,stateFamily,sha256,placementStatus,note;
        public PixelSize pixelSize;
        public LayoutRect nativeVisibleBounds;
        public AssetPlacement[] placements=Array.Empty<AssetPlacement>();
        public int width,height;
        public bool requested,opaque,previewOnly,preserveAspect;
        public Vector4 border;
        public bool repaired;
    }
    [Serializable] public class ElementRecord {
        public string id, parentId, assetId, kind = "image";
        public string onAssetId, offAssetId;
        public string knobElementId,onKnobAssetId,offKnobAssetId;
        public LayoutRect knobEnabledRect,knobDisabledRect;
        public bool preserveAspect;
        public bool isOn = true;
        public bool active = true,interactive;
        public string filename,state,placementGroup,screenGroup,popupGroup,placementBasis;
        public int layerOrder;
        public LayoutRect screenRect,visibleBounds,hitbox;
        public AnchorData anchor;
        public LayoutRect rect = new LayoutRect();
        public Vector2 anchorMin = new Vector2(0.5f,0.5f), anchorMax = new Vector2(0.5f,0.5f), pivot = new Vector2(0.5f,0.5f);
        public float rotation;
        public int order;
        public bool previewOnly;
        public string binding, text, textRole;
        public float fontSize = 42;
        public string color = "#612703", outlineColor = "#FFF2D9";
        public float outlineWidth;
        public float fillAmount = 1;
        public string imageType = "simple";
    }
}
