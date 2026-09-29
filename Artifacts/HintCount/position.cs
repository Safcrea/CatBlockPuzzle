var go=UnityEditor.EditorUtility.InstanceIDToObject(472588) as UnityEngine.GameObject; if(go==null || UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Invalid target/mode");
var rect=go.GetComponent<UnityEngine.RectTransform>(); UnityEditor.Undo.RecordObject(rect,"Restore Hint Count position"); rect.localPosition=new UnityEngine.Vector3(-2.6f,-98f,0f);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(go.scene);
return new {position=rect.localPosition.ToString(),anchoredPosition=rect.anchoredPosition.ToString(),canvas=go.GetComponent<UnityEngine.UI.Text>().canvas.name};
