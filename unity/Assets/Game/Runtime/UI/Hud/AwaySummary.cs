using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// "While you were away" (SPEC-039 idea 20): after a catch-up of at least <see cref="MinMinutes"/> game
    /// minutes, a holo card lists how long the Hub ran alone, what each stock gained or lost, and how many attacks
    /// came. Facts only, no pressure; a tap or a few seconds closes it. The advisor still gives its own summary.
    /// </summary>
    public sealed class AwaySummary
    {
        private const long MinMinutes = 30;
        private const float HoldSeconds = 12f;

        private readonly VisualElement _root;
        private float _left;

        public AwaySummary(VisualElement layer)
        {
            _root = new VisualElement();
            _root.AddToClassList("away");
            _root.AddToClassList("is-hidden");
            _root.RegisterCallback<ClickEvent>(_ => Hide());
            layer.Add(_root);
        }

        public void Show(GameHost host, CatchUpReport report)
        {
            if (report.Plan.Minutes < MinMinutes || report.Before == null || report.After == null)
            {
                return;
            }

            _root.Clear();
            VisualElement card = Kit.Card("clock", "WHILE YOU WERE AWAY", Fmt.Span(report.Plan.Minutes * 60.0) + " OF HUB TIME RAN WITHOUT YOU", "holo");
            for (int i = 0; i < 4; i++)
            {
                int delta = report.After[i] - report.Before[i];
                if (delta == 0 && i == (int)ResKind.Fuel)
                {
                    continue;
                }

                var row = new VisualElement();
                row.AddToClassList("away__row");
                row.Add(Icons.Create(ResourceInfo.GlyphOf((ResKind)i), "away__icon"));
                row.Add(Kit.Label(ResourceInfo.NameOf((ResKind)i), "away__name"));
                row.Add(Kit.Delta(delta, string.Empty));
                card.Add(row);
            }

            int raids = 0;
            int held = 0;
            var events = host.Sim.Log.Events;
            for (int i = report.FirstEventIndex; i < report.LastEventIndex && i < events.Count; i++)
            {
                if (events[i].Kind == EventKind.RaidResolved)
                {
                    raids++;
                    held += events[i].B == (int)Deadswitch.Sim.State.RaidOutcome.Repelled || events[i].B == (int)Deadswitch.Sim.State.RaidOutcome.Missed ? 1 : 0;
                }
            }

            if (raids > 0)
            {
                card.Add(Kit.Label(raids + (raids == 1 ? " ATTACK" : " ATTACKS") + " // " + held + " HELD", "away__raids"));
            }

            card.Add(Kit.Label("TAP TO CLOSE", "away__hint"));
            _root.Add(card);
            _root.RemoveFromClassList("is-hidden");
            _left = HoldSeconds;
            Motion.To(_root, 0.4f, Ease.OutBack, t =>
            {
                _root.style.opacity = t;
                _root.style.translate = new Translate(0, (1f - t) * -40f, 0);
            });
        }

        public void Tick(float dt)
        {
            if (_left <= 0f)
            {
                return;
            }

            _left -= dt;
            if (_left <= 0f)
            {
                Hide();
            }
        }

        private void Hide()
        {
            _left = 0f;
            Motion.To(_root, 0.25f, Ease.OutCubic, t => _root.style.opacity = 1f - t, () => _root.AddToClassList("is-hidden"));
        }
    }
}
