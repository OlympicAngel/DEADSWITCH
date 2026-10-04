using Deadswitch.Game.Core;
using Deadswitch.Game.Notifications;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// SETTINGS (F-024): effect intensity, text size, reduced motion, haptics, alerts, guide reset and (debug
    /// builds) time scale. Every change applies at once through GameSettings. Layout: Resources/UI/Settings.uxml.
    /// </summary>
    public sealed class SettingsScreen : IGameScreen
    {
        private static readonly int[] Effects = { 0, 25, 50, 75, 100 };
        private static readonly float[] TimeScales = { 1f, 10f, 60f, 600f };

        private readonly ScreenRouter _router;
        private readonly GameSettings _settings;
        private readonly VisualElement _ui;

        public SettingsScreen(ScreenRouter router, System.Action openPremium)
        {
            _router = router;
            _settings = GameHost.Instance.Settings;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Settings");
            Root.Add(tree);
            _ui = tree;
            _ui.Q("set-close").RegisterCallback<ClickEvent>(_ => _router.Show("core"));
            Bind("seg-effects", i => _settings.SetEffectIntensity(Effects[i]));
            Bind("seg-text", i => _settings.SetTextScale(GameSettings.TextScales[i]));
            Bind("seg-sound", i => _settings.SetSound(GameSettings.SoundSteps[i]));
            Toggle("tog-music", () => _settings.SetMusic(!_settings.Music));
            Bind("seg-time", i => _settings.SetDevTimeScale(TimeScales[i]));
            Toggle("tog-motion", () => _settings.SetReducedMotion(!_settings.ReducedMotion));
            Toggle("tog-haptics", () => _settings.SetHaptics(!_settings.Haptics));
            Toggle("tog-alerts", () => LocalAlerts.SetEnabled(!LocalAlerts.Enabled));
            _ui.Q("guide-reset").RegisterCallback<ClickEvent>(_ =>
            {
                PlayerPrefs.DeleteKey("ds.guide.off");
                PlayerPrefs.Save();
                GameHost.Instance.NotifyTicked();
            });
            _ui.Q("dev").EnableInClassList("is-hidden", !Debug.isDebugBuild);
            _ui.Q("open-premium").RegisterCallback<ClickEvent>(_ => openPremium());
        }

        public string Id => "settings";

        public VisualElement Root { get; }

        public void OnShow()
        {
            Refresh();
        }

        public void OnHide()
        {
        }

        private void Bind(string name, System.Action<int> pick)
        {
            VisualElement seg = _ui.Q(name);
            for (int i = 0; i < seg.childCount; i++)
            {
                int index = i;
                seg[i].RegisterCallback<ClickEvent>(_ =>
                {
                    pick(index);
                    Refresh();
                });
            }
        }

        private void Toggle(string name, System.Action flip)
        {
            _ui.Q(name).RegisterCallback<ClickEvent>(_ =>
            {
                flip();
                Refresh();
            });
        }

        private void Refresh()
        {
            Select("seg-effects", System.Array.IndexOf(Effects, _settings.EffectIntensityPct));
            Select("seg-text", System.Array.IndexOf(GameSettings.TextScales, _settings.TextScalePct));
            Select("seg-sound", System.Array.IndexOf(GameSettings.SoundSteps, _settings.SoundPct));
            SetToggle("tog-music", _settings.Music);
            Select("seg-time", System.Array.IndexOf(TimeScales, _settings.DevTimeScale));
            SetToggle("tog-motion", _settings.ReducedMotion);
            SetToggle("tog-haptics", _settings.Haptics);
            SetToggle("tog-alerts", LocalAlerts.Enabled);
        }

        private void Select(string name, int index)
        {
            VisualElement seg = _ui.Q(name);
            for (int i = 0; i < seg.childCount; i++)
            {
                seg[i].EnableInClassList("is-selected", i == index);
            }
        }

        private void SetToggle(string name, bool on)
        {
            VisualElement t = _ui.Q(name);
            t.EnableInClassList("is-on", on);
            t.Q<Label>().text = on ? "ON" : "OFF";
        }
    }
}
