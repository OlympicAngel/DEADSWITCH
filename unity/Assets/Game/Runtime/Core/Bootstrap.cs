using UnityEngine;

namespace Deadswitch.Game.Core
{
    /// <summary>
    /// Code-only startup: the game needs no scene wiring. After the first scene loads, the persistent
    /// game root is created with the host and every presentation system, in an explicit order.
    /// </summary>
    public static class Bootstrap
    {
        public static GameObject Root { get; private set; }

        /// <summary>
        /// Raised once after the root and core systems exist. Assemblies the game assembly cannot reference
        /// (for example URP rendering) attach their components here from a BeforeSceneLoad initializer.
        /// </summary>
        public static event System.Action<GameObject> Booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Root != null)
            {
                return;
            }

            Screen.orientation = ScreenOrientation.Portrait;
            Root = new GameObject("DEADSWITCH");
            Object.DontDestroyOnLoad(Root);
            Root.AddComponent<GameHost>();
            Root.AddComponent<UI.UiRoot>();
            Root.AddComponent<UI.Hud.HudController>();
            Root.AddComponent<Base.BaseView>();
            Root.AddComponent<Base.DroneCamera>();
            Root.AddComponent<Audio.AudioDirector>();
            Booted?.Invoke(Root);
        }
    }
}
