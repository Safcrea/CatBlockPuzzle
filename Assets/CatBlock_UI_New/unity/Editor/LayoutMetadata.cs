using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CatBlocksUI.Editor {
public static class LayoutMetadata {
    static LayoutRect Absolute(ElementRecord e,Dictionary<string,ElementRecord> map,LayoutRect alternate=null){var r=alternate??e.rect;float x=r.x,y=r.y;var parent=e.parentId;while(!string.IsNullOrEmpty(parent)){var p=map[parent];x+=p.rect.x;y+=p.rect.y;parent=p.parentId;}return new LayoutRect{x=x,y=y,width=r.width,height=r.height};}
    static LayoutRect Content(LayoutRect r,AssetRecord a,bool preserve){if(!preserve||a==null||a.pixelSize==null)return r;float k=Math.Min(r.width/a.pixelSize.width,r.height/a.pixelSize.height),w=a.pixelSize.width*k,h=a.pixelSize.height*k;return new LayoutRect{x=r.x+(r.width-w)/2,y=r.y+(r.height-h)/2,width=w,height=h};}
    static LayoutRect Visible(LayoutRect r,AssetRecord a,bool preserve){if(a==null||a.pixelSize==null||a.nativeVisibleBounds==null)return r;r=Content(r,a,preserve);var b=a.nativeVisibleBounds;return new LayoutRect{x=r.x+b.x*r.width/a.pixelSize.width,y=r.y+b.y*r.height/a.pixelSize.height,width=b.width*r.width/a.pixelSize.width,height=b.height*r.height/a.pixelSize.height};}
    static LayoutRect Hitbox(LayoutRect r,AssetRecord a,bool preserve){r=Content(r,a,preserve);float ix=a!=null&&!a.opaque&&a.pixelSize!=null?8*r.width/a.pixelSize.width:0,iy=a!=null&&!a.opaque&&a.pixelSize!=null?8*r.height/a.pixelSize.height:0;return new LayoutRect{x=r.x+ix,y=r.y+iy,width=r.width-2*ix,height=r.height-2*iy};}
    static string Family(string id,Dictionary<string,AssetRecord> assets){id=id??"";if(assets.TryGetValue(id,out var a)&&!string.IsNullOrEmpty(a.stateFamily))return a.stateFamily;if(id=="StarFilledYellow"||id=="StarEmptyBrown")return "RatingStar";foreach(var suffix in new[]{"Unselected","Selected","Purchased","Price"})if(id.EndsWith(suffix,StringComparison.Ordinal))return id.Substring(0,id.Length-suffix.Length);return id;}
    public static void Refresh(LayoutDocument d){var elements=d.elements.ToDictionary(e=>e.id);var assets=d.assets.ToDictionary(a=>a.id);
        foreach(var t in d.elements)if(t.kind=="toggle"){t.assetId=t.isOn?t.onAssetId:t.offAssetId;if(!string.IsNullOrEmpty(t.knobElementId)){var k=elements[t.knobElementId];k.assetId=t.isOn?t.onKnobAssetId:t.offKnobAssetId;k.rect=t.isOn?t.knobEnabledRect:t.knobDisabledRect;}}
        foreach(var e in d.elements){e.screenRect=Absolute(e,elements);assets.TryGetValue(e.assetId??"",out var a);e.filename=a==null?null:a.path;e.state=a==null?"default":a.state;e.layerOrder=e.order;e.anchor=new AnchorData{min=e.anchorMin,max=e.anchorMax};e.visibleBounds=Visible(e.screenRect,a,e.preserveAspect);e.interactive=e.kind=="button"||e.kind=="toggle"||e.kind=="overlay";e.hitbox=Hitbox(e.screenRect,a,e.preserveAspect);}
        foreach(var a in d.assets){var matches=d.elements.Where(e=>e.assetId==a.id).ToArray();bool alternative=matches.Length==0;if(alternative)matches=d.elements.Where(e=>Family(e.assetId,assets)==Family(a.id,assets)||(e.kind=="toggle"&&(e.onAssetId==a.id||e.offAssetId==a.id))).ToArray();a.placements=matches.Select(e=>{var r=e.screenRect;var owner=d.elements.FirstOrDefault(t=>t.knobElementId==e.id);if(owner!=null&&(a.id==owner.onKnobAssetId||a.id==owner.offKnobAssetId))r=Absolute(e,elements,a.id==owner.onKnobAssetId?owner.knobEnabledRect:owner.knobDisabledRect);return new AssetPlacement{instanceId=e.id,assetId=a.id,filename=a.path,state=a.state,active=!alternative&&e.active,layerOrder=e.order,screenRect=r,visibleBounds=Visible(r,a,e.preserveAspect),hitbox=owner==null?e.hitbox:Hitbox(r,a,e.preserveAspect),pivot=e.pivot,anchor=e.anchor,placementGroup=e.placementGroup,screenGroup=d.screenId,popupGroup=e.popupGroup};}).ToArray();}
    }
}
}
