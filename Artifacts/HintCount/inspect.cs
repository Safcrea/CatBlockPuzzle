var rows = new System.Collections.Generic.List<object>();
foreach (var t in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.RectTransform>()) {
 if (!t.gameObject.scene.IsValid() || !(t.name.ToLower().Contains("hint") || t.name.ToLower().Contains("freeze"))) continue;
 var text = t.GetComponent<UnityEngine.UI.Text>(); var image = t.GetComponent<UnityEngine.UI.Image>();
 rows.Add(new { id=t.gameObject.GetInstanceID(), path=UnityEditor.AnimationUtility.CalculateTransformPath(t,null), sibling=t.GetSiblingIndex(), active=t.gameObject.activeInHierarchy, pos=t.localPosition.ToString(), size=t.rect.ToString(), scale=t.localScale.ToString(), text=text == null ? null : new { value=text.text, enabled=text.enabled, color=text.color.ToString(), fontSize=text.fontSize, font=text.font==null?null:text.font.name, depth=text.depth, cull=text.canvasRenderer.cull }, image=image==null?null:new { enabled=image.enabled,depth=image.depth,color=image.color.ToString()}, components=t.GetComponents<UnityEngine.Component>().Select(c=>c.GetType().Name).ToArray() });
}
return new {playing=UnityEditor.EditorApplication.isPlaying,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,rows};
