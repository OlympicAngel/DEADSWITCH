using Deadswitch.Game.Core;
using Deadswitch.Game.Cosmetics;
using Deadswitch.Game.Presentation;
using Deadswitch.Host.Seasons;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// REWARD TRACK (F-048): free cosmetics earned by play. Rank and XP, every reward with the rank that unlocks it,
    /// EQUIP for themes and voices, codex logs readable in place. Opened from the Command menu.
    /// Layout: Resources/UI/Season.uxml.
    /// </summary>
    public sealed class SeasonScreen : IGameScreen
    {
        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly Label _reason;
        private string _back = "base";
        private bool _visible;

        public SeasonScreen(ScreenRouter router)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Season");
            Root.Add(tree);
            _ui = tree;
            Icons.Attach(tree);
            _reason = _ui.Q<Label>("ssn-reason");
            _ui.Q("ssn-close").RegisterCallback<ClickEvent>(_ => router.Show(_back));
            Kit.BuildMeter(_ui.Q("ssn-meter"));

            VisualElement list = _ui.Q("ssn-rewards");
            list.Clear();
            for (int i = 0; i < SeasonTrack.Rewards.Length; i++)
            {
                SeasonReward r = SeasonTrack.Rewards[i];
                var row = new VisualElement { name = "r-" + i };
                row.AddToClassList("row");
                row.AddToClassList("ssn-r");
                row.Add(Kit.Label(r.Rank.ToString(), "ssn-r__rank"));
                var well = new VisualElement();
                well.AddToClassList("ssn-r__well");
                well.Add(Icons.Create(r.Kind == RewardKind.Codex ? "book" : r.Kind == RewardKind.Voice ? "signal" : "eye", "ssn-r__icon"));
                row.Add(well);
                var body = new VisualElement();
                body.AddToClassList("grow");
                body.AddToClassList("ssn-r__body");
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
                _reason.text = "Reach rank " + r.Rank + " to unlock it.";
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
            _ui.Q<Label>("ssn-title").text = SeasonTrack.Title;
            _ui.Q<Label>("ssn-rank").text = rank.ToString();
            _ui.Q<Label>("ssn-rank-of").text = "/ " + SeasonTrack.Ranks + "  RANK";
            bool max = rank >= SeasonTrack.Ranks;
            int into = xp - (rank * SeasonTrack.XpPerRank);
            Kit.SetMeter(_ui.Q("ssn-meter"), max ? 1f : (float)into / SeasonTrack.XpPerRank);
            _ui.Q<Label>("ssn-xp").text = Fmt.Num(xp) + " XP" + (max ? " // TRACK COMPLETE" : " // " + Fmt.Num(SeasonTrack.XpPerRank - into) + " TO RANK " + (rank + 1));

            for (int i = 0; i < SeasonTrack.Rewards.Length; i++)
            {
                SeasonReward r = SeasonTrack.Rewards[i];
                bool unlocked = SeasonPass.Unlocked(r);
                bool equipped = unlocked && Equipped(r);
                _ui.Q("r-" + i).EnableInClassList("is-unlocked", unlocked);
                _ui.Q("r-" + i).EnableInClassList("is-equipped", equipped);
                _ui.Q<Label>("r-" + i + "-state").text = equipped ? "EQUIPPED" : unlocked ? "UNLOCKED" : "UNLOCKS AT RANK " + r.Rank;
                VisualElement codex = _ui.Q("r-" + i + "-codex");
                if (codex != null)
                {
                    codex.style.display = unlocked ? DisplayStyle.Flex : DisplayStyle.None;
                }

                VisualElement equip = _ui.Q("r-" + i + "-equip");
                if (equip != null)
                {
                    // a locked reward shows when it unlocks, not a dead EQUIP button
                    equip.style.display = unlocked ? DisplayStyle.Flex : DisplayStyle.None;
                    Kit.SetButtonText(equip, equipped ? (r.Kind == RewardKind.Voice ? "UNEQUIP" : "ON") : "EQUIP");
                }
            }
        }
    }
}
