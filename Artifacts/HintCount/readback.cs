var go=UnityEditor.EditorUtility.InstanceIDToObject(472588) as UnityEngine.GameObject; var r=go.GetComponent<UnityEngine.RectTransform>();
return new {playing=UnityEditor.EditorApplication.isPlaying,position=r.localPosition.ToString(),anchored=r.anchoredPosition.ToString(),canvas=go.GetComponent<UnityEngine.Canvas>()!=null,dirty=go.scene.isDirty};
