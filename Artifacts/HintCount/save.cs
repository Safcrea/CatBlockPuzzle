var go=UnityEditor.EditorUtility.InstanceIDToObject(472588) as UnityEngine.GameObject;
var saved=UnityEditor.SceneManagement.EditorSceneManager.SaveScene(go.scene);
var cleaned=UnityEditor.AssetDatabase.DeleteAsset("Assets/Artifacts/HintCount");
UnityEditor.AssetDatabase.DeleteAsset("Assets/Artifacts");
return new{saved,dirty=go.scene.isDirty,cleaned,path=go.scene.path};
