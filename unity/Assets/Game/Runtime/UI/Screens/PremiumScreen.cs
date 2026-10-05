using Deadswitch.Game.Core;
using Deadswitch.Game.Store;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// FULL GAME (F-036, ADR-0006): the free Tier 1 demo, the one-time premium unlock (with restore), and cosmetic
    /// HUD themes from optional ads. Opened from the tier gate and from SETTINGS. Layout: Resources/UI/Premium.uxml.
    /// </summary>
    public sealed class PremiumScreen : IGameScreen
    {
        private readonly VisualElement _ui;
        private readonly Label _reason;
        private string _back = "core";

        public PremiumScreen(ScreenRouter router, System.Action openSeason)
        {
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Premium");
            Root.Add(tree);
            _ui = tree;
            _reason = _ui.Q<Label>("prm-reason");
            _ui.Q("prm-close").RegisterCallback<ClickEvent>(_ => router.Show(_back));
            _ui.Q("prm-buy").RegisterCallback<ClickEvent>(_ => Say(Entitlements.Instance.BuyPremium()));
            _ui.Q("prm-restore").RegisterCallback<ClickEvent>(_ => Say(Entitlements.Instance.Restore()));
            for (int i = 0; i < Theme.Count; i++)
            {
                int theme = i;
                _ui.Q("theme-" + i).RegisterCallback<ClickEvent>(_ =>
                {
                    if (!Theme.Owned(theme))
                    {
                        Say(theme >= 3 ? "Earn it on the season track." : "Watch an ad below to unlock this theme.");
                        return;
                    }

                    Theme.Select(theme, UiRoot.Instance.Root);
                    Refresh();
                });
            }

            _ui.Q("ad-1").RegisterCallback<ClickEvent>(_ => Say(RewardedAds.Watch(ConvenienceGrant.ThemeCold)));
            _ui.Q("ad-2").RegisterCallback<ClickEvent>(_ => Say(RewardedAds.Watch(ConvenienceGrant.ThemeBone)));
            _ui.Q("ad-3").RegisterCallback<ClickEvent>(_ => Say(RewardedAds.Watch(ConvenienceGrant.SalvageRoll)));
            _ui.Q("ad-4").RegisterCallback<ClickEvent>(_ => Say(RewardedAds.Watch(ConvenienceGrant.IdleCap)));
            _ui.Q("prm-season").RegisterCallback<ClickEvent>(_ => openSeason());
            SeasonPass.Changed += Refresh;
            Entitlements.Instance.Changed += Refresh;
        }

        public string Id => "premium";

        public VisualElement Root { get; }

        /// <summary>Where the close button returns to.</summary>
        public void ReturnTo(string screen)
        {
            _back = screen;
        }

        public void OnShow()
        {
            _reason.text = string.Empty;
            Refresh();
        }

        public void OnHide()
        {
        }

        private void Say(string line)
        {
            _reason.text = line;
            Refresh();
        }

        private void Refresh()
        {
            bool premium = Entitlements.Instance.HasPremium;
            _ui.Q<Label>("prm-state").text = premium ? "FULL GAME // UNLOCKED" : "FREE DEMO // TIER 1";
            _ui.Q<Label>("prm-state").EnableInClassList("t-amber", !premium);
            _ui.Q<Label>("prm-state").EnableInClassList("t-phosphor", premium);
            Kit.SetButtonText(_ui.Q("prm-buy"), premium ? "UNLOCKED" : "UNLOCK FULL GAME");
            Kit.SetButtonText(_ui.Q("prm-season"), "SEASON TRACK // RANK " + SeasonPass.Rank + " / " + Deadswitch.Host.Seasons.SeasonTrack.Ranks);
            _ui.Q("prm-buy").EnableInClassList("is-disabled", premium);
            string blocked = RewardedAds.Blocked();
            for (int i = 0; i < Theme.Count; i++)
            {
                _ui.Q("theme-" + i).EnableInClassList("is-selected", Theme.Current == i && Theme.Owned(i));
                _ui.Q("theme-" + i).EnableInClassList("is-locked", !Theme.Owned(i));
            }

            for (int i = 1; i < 3; i++)
            {
                bool owned = Theme.Owned(i);
                _ui.Q("ad-" + i).EnableInClassList("is-disabled", owned || blocked != null);
                Kit.SetButtonText(_ui.Q("ad-" + i), owned ? RewardedAds.Name((ConvenienceGrant)i) + " OWNED" : "WATCH AD // " + (i == 1 ? "COLD SIGNAL" : "BONE"));
            }

            // convenience grants (doc 10 s1.1): the sim decides when they are allowed
            Deadswitch.Sim.State.GameState gs = GameHost.Instance.Sim.State;
            Deadswitch.Sim.SimConfig gc = GameHost.Instance.Sim.Config;
            bool salvage = blocked == null && AdSystem.Available(gs, AdGrant.SalvageRoll) == Deadswitch.Sim.Commands.RejectReason.None;
            bool storage = blocked == null && AdSystem.Available(gs, AdGrant.IdleCap) == Deadswitch.Sim.Commands.RejectReason.None;
            _ui.Q("ad-3").EnableInClassList("is-disabled", !salvage);
            _ui.Q("ad-4").EnableInClassList("is-disabled", !storage);
            Kit.SetButtonText(_ui.Q("ad-3"), "AD // SALVAGE +" + gc.Ads.SalvageEnergy + " E +" + gc.Ads.SalvageFuel + " F (DAILY)");
            Kit.SetButtonText(_ui.Q("ad-4"), "AD // STORAGE +" + gc.Ads.CapPct + "% FOR " + gc.Ads.CapHours + " H");
        }
    }
}
