using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// STORY (SPEC-024): the tier's chapter (villain, premise, twist, payoff progress) and the archive of memory
    /// fragments, which survives every reboot. Opened from CORE; opens itself when a chapter opens or closes.
    /// Layout: Resources/UI/Story.uxml.
    /// </summary>
    public sealed class StoryScreen : IGameScreen
    {
        private static readonly string[] TwistHints =
        {
            "Verify a battle report and catch the core in a lie to learn it sooner.",
            "Raise Vanguard heat to Watched, or put an agent inside their lines, to learn it sooner.",
            "Draw the Church's attention (heat Watched) to learn it sooner.",
            "It surfaces when the core's hidden project nears completion.",
        };

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly Label[] _fragIds = new Label[ChapterSystem.FragmentCount];
        private readonly Label[] _fragTexts = new Label[ChapterSystem.FragmentCount];
        private bool _visible;

        public StoryScreen(ScreenRouter router)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Story");
            Root.Add(tree);
            _ui = tree;
            Icons.Attach(tree);
            _ui.Q("sty-close").RegisterCallback<ClickEvent>(_ => router.Show("core"));
            Kit.BuildMeter(_ui.Q("sty-payoff-meter"));

            VisualElement list = _ui.Q("sty-fragments");
            list.Clear();
            for (int i = 0; i < ChapterSystem.FragmentCount; i++)
            {
                var row = new VisualElement { name = "frag-" + i };
                row.AddToClassList("sty-frag");
                _fragIds[i] = Kit.Label(string.Empty, "sty-frag__id");
                _fragTexts[i] = Kit.Label(string.Empty, "sty-frag__text");
                var well = new VisualElement();
                well.AddToClassList("sty-frag__well");
                well.Add(Icons.Create("memory", "sty-frag__ic", "sty-frag__ic--known"));
                well.Add(Icons.Create("lock", "sty-frag__ic", "sty-frag__ic--lost"));
                row.Add(well);
                var body = new VisualElement();
                body.AddToClassList("sty-frag__body");
                body.Add(_fragIds[i]);
                body.Add(_fragTexts[i]);
                row.Add(body);
                list.Add(row);
            }

            _host.Ticked += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
        }

        public string Id => "story";

        public VisualElement Root { get; }

        public void OnShow()
        {
            _visible = true;
            Refresh();
        }

        public void OnHide()
        {
            _visible = false;
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            ChapterConfig c = _host.Sim.Config.Chapters;
            int tier = System.Math.Max(1, s.ChapterTier == 0 ? s.Tier : s.ChapterTier);
            int index = System.Math.Min(tier, Story.Chapters.Length) - 1;
            Story.Chapter chapter = Story.For(tier);

            _ui.Q<Label>("sty-num").text = "CHAPTER " + tier + " OF " + Story.Chapters.Length + " // CYCLE " + (s.Cycle + 1);
            _ui.Q<Label>("sty-title").text = chapter.Title;
            _ui.Q<Label>("sty-villain").text = chapter.Villain;
            _ui.Q<Label>("sty-premise").text = chapter.Premise;

            // twist: encrypted until it lands, then the reveal
            bool twisted = s.ChapterBeat >= 1;
            VisualElement twist = _ui.Q("sty-twist");
            twist.EnableInClassList("is-locked", !twisted);
            twist.EnableInClassList("is-live", twisted && s.ChapterBeat == 1);
            twist.EnableInClassList("is-done", s.ChapterBeat == 2);
            long fallback = s.ChapterOpenedTick + ((long)c.TwistFallbackHours * SimConfig.TicksPerHour);
            _ui.Q<Label>("sty-twist-label").text = twisted ? "TWIST // DECRYPTED" : "TWIST // ENCRYPTED // OPENS IN " + Fmt.Countdown(_host.SecondsUntilTick(fallback));
            _ui.Q<Label>("sty-twist-text").text = twisted ? chapter.Twist : TwistHints[index];

            // payoff: hold the wall after the twist
            VisualElement payoff = _ui.Q("sty-payoff");
            bool closed = s.ChapterBeat == 2;
            payoff.EnableInClassList("is-locked", !twisted);
            payoff.EnableInClassList("is-live", s.ChapterBeat == 1);
            payoff.EnableInClassList("is-done", closed);
            int points = closed ? c.PayoffPoints : twisted ? System.Math.Min(s.ChapterPoints, c.PayoffPoints) : 0;
            _ui.Q<Label>("sty-payoff-label").text = closed ? "PAYOFF // CHAPTER CLOSED" : "PAYOFF // HOLD THE WALL " + points + " / " + c.PayoffPoints;
            Kit.SetMeter(_ui.Q("sty-payoff-meter"), (float)points / c.PayoffPoints);
            string villain = Names.Faction(ChapterSystem.Villain(tier));
            _ui.Q<Label>("sty-payoff-text").text = closed ? chapter.Payoff
                : "Repel or slip their attacks after the twist. " + villain + " attacks count " + (c.VillainPoints == 2 ? "double" : "x" + c.VillainPoints)
                  + ". Reward: " + Fmt.Num(c.PayoffEnergy[index]) + " E, " + Fmt.Num(c.PayoffCompute[index]) + " compute and a memory fragment.";

            // archive: recovered fragments read clear, the rest stay corrupted
            _ui.Q<Label>("sty-count").text = ChapterSystem.FragmentsKnown(s) + " / " + ChapterSystem.FragmentCount;
            for (int i = 0; i < ChapterSystem.FragmentCount; i++)
            {
                bool known = (s.Fragments & (1 << i)) != 0;
                _ui.Q("frag-" + i).EnableInClassList("is-known", known);
                _fragIds[i].text = (i + 1).ToString("00") + " // " + Story.Chapters[i / ChapterSystem.FragmentsPerChapter].Title;
                _fragTexts[i].text = known ? Story.Fragments[i] : "[SECTOR CORRUPTED // " + Redacted(Story.Fragments[i]) + "]";
            }
        }

        /// <summary>The fragment's shape without its words: letters become hashes, spaces stay.</summary>
        private static string Redacted(string text)
        {
            int n = System.Math.Min(text.Length, 34);
            var chars = new char[n];
            for (int i = 0; i < n; i++)
            {
                chars[i] = text[i] == ' ' ? ' ' : '#';
            }

            return new string(chars);
        }
    }
}
