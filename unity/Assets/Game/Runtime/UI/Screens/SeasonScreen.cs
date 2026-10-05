using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Game.Store;
using Deadswitch.Host.Seasons;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// SEASON (F-048): the cosmetic season track. Rank and XP from play, the free and premium rewards, EQUIP for
    /// themes and voice packs, and codex logs readable in place. Opened from the premium screen.
    /// Layout: Resources/UI/Season.uxml.
    /// </summary>
    public sealed class SeasonScreen : IGameScreen
    {
        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly Label _reason;
        private string _back = "premium";
        private bool _visible;

        public SeasonScreen(ScreenRouter router)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Season");
            Root.Add(tree);
            _ui = tree;
            _reason = _ui.Q<Label>("ssn-reason");
            _ui.Q("ssn-close").RegisterCallback<ClickEvent>(_ => router.Show(_back));
            _ui.Q("ssn-buy").RegisterCallback<ClickEvent>(_ =>
            {
                Entitlements.Instance.BuySeason(SeasonTrack.Id, line =>
                {
                    _reason.text = line;
                    SeasonPass.GrantReached();
                    Refresh();
                });
            });
            Kit.BuildMeter(_ui.Q("ssn-meter"));

            VisualElement list = _ui.Q("ssn-rewards");
            list.Clear();
            for (int i = 0; i < SeasonTrack.Rewards.Length; i++)
            {
                SeasonReward r = SeasonTrack.Rewards[i];
                var row = new VisualElement { name = "r-" + i };
                row.AddToClassList("row");
                row.AddToClassList("ssn-r");
                row.EnableInClassList("is-premium", r.Premium);
                row.Add(Kit.Label(r.Rank.ToString(), "ssn-r__rank"));
                var body = new VisualElement();
                body.AddToClassList("grow");
                body.Add(Kit.Label(r.Name, "ssn-r__name"));
                Label state = Kit.Label(string.Empty, "ssn-r__state");
                state.name = "r-" + i + "-state";
                body.Add(state);
                if (r.Kind == RewardKind.Codex)
                {
                    Label codex = Kit.Label(SeasonTrack.Codex[r.Id], "ssn-r__codex");
                    codex.name = "r-" + i + "-codex";
                    body.Add(codex);
                }

                row.Add(body);
                if (r.Kind != RewardKind.Codex)
                {
                    VisualElement equip = Kit.Button("EQUIP", () => Equip(r), "ds-btn--ghost", "ssn-r__btn");
                    equip.name = "r-" + i + "-equip";
                    row.Add(equip);
                }

                list.Add(row);
            }

            SeasonPass.Hook(_host);
            SeasonPass.Changed += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
        }

        public string Id => "season";

        public VisualElement Root { get; }

        /// <summary>Where the close button returns to.</summary>
        public void ReturnTo(string screen)
        {
            _back = screen;
        }

        public void OnShow()
        {
            _visible = true;
            _reason.text = string.Empty;
            SeasonPass.GrantReached();
            Refresh();
        }

        public void OnHide()
        {
            _visible = false;
        }

        private void Equip(SeasonReward r)
        {
            if (!SeasonPass.Unlocked(r))
            {
                _reason.text = r.Premium && !Entitlements.Instance.OwnsSeason(SeasonTrack.Id) ? "On the premium track. Reach rank " + r.Rank + " with the pass." : "Reach rank " + r.Rank + " to unlock it.";
                return;
            }

            if (r.Kind == RewardKind.Theme)
            {
                Theme.Select(r.Id == "theme-verdigris" ? 3 : 4, UiRoot.Instance.Root);
            }
            else
            {
                int pack = System.Array.IndexOf(GameSettings.VoicePacks, r.Id);
                _host.Settings.SetVoicePack(_host.Settings.VoicePack == pack ? 0 : pack);
            }

            Refresh();
        }

        private bool Equipped(SeasonReward r)
        {
            return r.Kind == RewardKind.Theme ? Theme.Current == (r.Id == "theme-verdigris" ? 3 : 4)
                : r.Kind == RewardKind.Voice && GameSettings.VoicePacks[_host.Settings.VoicePack] == r.Id;
        }

        private void Refresh()
        {
            int xp = SeasonPass.Xp;
            int rank = SeasonPass.Rank;
            bool pass = Entitlements.Instance.OwnsSeason(SeasonTrack.Id);
            _ui.Q<Label>("ssn-title").text = SeasonTrack.Title;
            _ui.Q<Label>("ssn-rank").text = rank.ToString();
            _ui.Q<Label>("ssn-rank-of").text = "/ " + SeasonTrack.Ranks + "  RANK";
            bool max = rank >= SeasonTrack.Ranks;
            int into = xp - (rank * SeasonTrack.XpPerRank);
            Kit.SetMeter(_ui.Q("ssn-meter"), max ? 1f : (float)into / SeasonTrack.XpPerRank);
            _ui.Q<Label>("ssn-xp").text = Fmt.Num(xp) + " XP" + (max ? " // TRACK COMPLETE" : " // " + Fmt.Num(SeasonTrack.XpPerRank - into) + " TO RANK " + (rank + 1));
            Kit.SetButtonText(_ui.Q("ssn-buy"), pass ? "PASS OWNED" : "UNLOCK PASS");
            _ui.Q("ssn-buy").EnableInClassList("is-disabled", pass);

            for (int i = 0; i < SeasonTrack.Rewards.Length; i++)
            {
                SeasonReward r = SeasonTrack.Rewards[i];
                bool unlocked = SeasonPass.Unlocked(r);
                bool equipped = unlocked && Equipped(r);
                _ui.Q("r-" + i).EnableInClassList("is-unlocked", unlocked);
                _ui.Q("r-" + i).EnableInClassList("is-equipped", equipped);
                string track = r.Premium ? "PASS" : "FREE";
                _ui.Q<Label>("r-" + i + "-state").text = track + " // " + (equipped ? "EQUIPPED" : unlocked ? "UNLOCKED" : r.Premium && !pass && rank >= r.Rank ? "REACHED // NEEDS THE PASS" : "RANK " + r.Rank);
                VisualElement codex = _ui.Q("r-" + i + "-codex");
                if (codex != null)
                {
                    codex.style.display = unlocked ? DisplayStyle.Flex : DisplayStyle.None;
                }

                VisualElement equip = _ui.Q("r-" + i + "-equip");
                if (equip != null)
                {
                    equip.EnableInClassList("is-disabled", !unlocked);
                    Kit.SetButtonText(equip, equipped ? (r.Kind == RewardKind.Voice ? "UNEQUIP" : "ON") : "EQUIP");
                }
            }
        }
    }
}
