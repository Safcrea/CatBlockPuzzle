var go = UnityEditor.EditorUtility.InstanceIDToObject(472588) as UnityEngine.GameObject;
if (go == null || go.name != "Hint Count" || UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Target or mode changed");
var canvas=go.GetComponent<UnityEngine.Canvas>(); if(canvas==null) throw new System.Exception("Canvas missing");
var rect=go.GetComponent<UnityEngine.RectTransform>(); var before=rect.localPosition;
UnityEditor.Undo.DestroyObjectImmediate(canvas);
UnityEngine.Canvas.ForceUpdateCanvases();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(go.scene);
return new {path=UnityEditor.AnimationUtility.CalculateTransformPath(go.transform,null),before=before.ToString(),after=rect.localPosition.ToString(),hasLocalCanvas=go.GetComponent<UnityEngine.Canvas>()!=null,sibling=rect.GetSiblingIndex(),saved=go.scene.path};
