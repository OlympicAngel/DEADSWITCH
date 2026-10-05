using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.Cosmetics
{
    /// <summary>The chosen HUD theme (F-036, cosmetic): a class on the UI root that swaps the phosphor tokens.</summary>
    public static class Theme
    {
        private const string Key = "ds.theme";
        private static readonly string[] Classes = { string.Empty, "theme-cold", "theme-bone", "theme-verdigris", "theme-ash" };

        public static int Count => Classes.Length;

        /// <summary>Display names, in order.</summary>
        public static readonly string[] Names = { "PHOSPHOR", "COLD SIGNAL", "BONE", "VERDIGRIS", "ASH" };

        /// <summary>0 phosphor, 1 cold signal and 2 bone are open to all; 3 verdigris and 4 ash come from the reward track.</summary>
        public static int Current => Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Classes.Length - 1);

        public static bool Owned(int theme)
        {
            return theme <= 2 || (theme < Classes.Length && Unlocks.Owns(Classes[theme]));
        }

        public static void Select(int theme, VisualElement root)
        {
            if (!Owned(theme))
            {
                return;
            }

            PlayerPrefs.SetInt(Key, theme);
            PlayerPrefs.Save();
            Apply(root);
        }

        public static void Apply(VisualElement root)
        {
            int current = Owned(Current) ? Current : 0;
            for (int i = 1; i < Classes.Length; i++)
            {
                root.EnableInClassList(Classes[i], i == current);
            }
        }
    }
}
