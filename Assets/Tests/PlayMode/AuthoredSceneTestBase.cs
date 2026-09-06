using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CatBlockPuzzle.Tests
{
    public abstract class AuthoredSceneTestBase
    {
        [UnitySetUp]
        public IEnumerator LoadAuthoredScene()
        {
            yield return SceneManager.LoadSceneAsync("CatBlockPuzzle", LoadSceneMode.Single);
            yield return null;
        }
    }
}
