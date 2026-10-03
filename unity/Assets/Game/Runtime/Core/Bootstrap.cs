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
        }
    }
}
