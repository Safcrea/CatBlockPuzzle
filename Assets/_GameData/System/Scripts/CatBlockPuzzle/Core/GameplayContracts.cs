using System.Collections;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The scene-bound pieces every gameplay service works against. Implemented by the
    /// CatBlockPuzzleGame scene component so editor authoring can swap the underlying view
    /// components without the services holding stale references.
    /// </summary>
    internal interface IGameplayContext
    {
        MonoBehaviour Host { get; }
        GameplayHudView Hud { get; }
        GameplayPresentationAssets Assets { get; }
        MetaProgressionView MetaView { get; }
        LevelCompleteScreen CompleteScreen { get; }
        LevelFailScreen FailScreen { get; }
        CatPuzzleContentCatalog Catalog { get; }
        LevelManager Levels { get; }
    }

    /// <summary>Inspector-authored tuning that services read live, so Play Mode edits still apply.</summary>
    internal interface IPuzzleConfig
    {
        bool LevelOneTutorialEnabled { get; set; }
        bool ShowTutorialCaptions { get; }
        Sprite TutorialHandSprite { get; }
        float FreezeDurationSeconds { get; }
    }

    /// <summary>Level navigation exposed to the room progression and power-up services.</summary>
    internal interface ILevelFlow
    {
        void LoadLevel(int levelIndex, bool persistProgress);
        bool CanGoNextLevel { get; }
        void LoadNextLevel();
        void LoadNextLevelDirect();
        bool IsRevealing { get; }
        void CancelReveal();
    }

    /// <summary>Base for plain services that run coroutines on the scene component.</summary>
    internal abstract class HostedService
    {
        protected readonly IGameplayContext Context;

        protected HostedService(IGameplayContext context)
        {
            Context = context;
        }

        protected MonoBehaviour Host => Context.Host;
        protected GameplayHudView Hud => Context.Hud;
        protected GameplayPresentationAssets Assets => Context.Assets;

        protected Coroutine StartCoroutine(IEnumerator routine) => Host.StartCoroutine(routine);

        protected void StopCoroutine(Coroutine routine)
        {
            if (routine != null) Host.StopCoroutine(routine);
        }

        protected static void Destroy(Object target) => Object.Destroy(target);
    }
}
