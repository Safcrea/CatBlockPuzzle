var go=UnityEditor.EditorUtility.InstanceIDToObject(472588) as UnityEngine.GameObject;
return go.GetComponentsInParent<UnityEngine.Canvas>(true).Select(c=>new{c.name,c.sortingLayerName,c.sortingLayerID,layerValue=UnityEngine.SortingLayer.GetLayerValueFromID(c.sortingLayerID),c.sortingOrder,c.overrideSorting}).ToArray();
