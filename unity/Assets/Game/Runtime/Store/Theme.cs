using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.Store
{
    /// <summary>The chosen HUD theme (F-036, cosmetic): a class on the UI root that swaps the phosphor tokens.</summary>
    public static class Theme
    {
        private const string Key = "ds.theme";
        private static readonly string[] Classes = { string.Empty, "theme-cold", "theme-bone", "theme-verdigris", "theme-ash" };

        public static int Count => Classes.Length;

        /// <summary>0 phosphor (always owned), 1 cold signal, 2 bone (ads), 3 verdigris, 4 ash (season track).</summary>
        public static int Current => Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Classes.Length - 1);

        public static bool Owned(int theme)
        {
            return theme == 0 || (theme < Classes.Length && Entitlements.Instance.OwnsCosmetic(theme <= 2 ? RewardedAds.Id((ConvenienceGrant)theme) : Classes[theme]));
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
