using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Game.UI.Screens;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// Celebration moments (SPEC-043 s5): a short full-width banner when something earned happens (tier reached,
    /// memory restored, attack repelled, building fully upgraded, mastery earned) with an icon, what it means and
    /// one line from the AI. Never blocks input (it ignores the pointer), never sells anything, queues at most a
    /// few so a long absence does not replay a parade. Chapters have their own story screen and are not repeated.
    /// </summary>
    public sealed class Celebrations
    {
        private const int MaxQueued = 3;
        private const float ShowSeconds = 3.6f;

        private readonly VisualElement _root;
        private readonly VisualElement _band;
        private readonly VisualElement _well;
        private readonly VisualElement _ray;
        private readonly Label _kicker;
        private readonly Label _title;
        private readonly Label _line;
        private readonly Queue<Moment> _queue = new Queue<Moment>();
        private VisualElement _icon;
        private float _left;

        public Celebrations(VisualElement layer)
        {
            _root = new VisualElement { pickingMode = PickingMode.Ignore };
            // the banner sits in the sheet layer, outside the HUD tree: it brings the HUD's styles with it
            _root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Hud"));
            _root.AddToClassList("celeb");
            _root.AddToClassList("is-hidden");
            _band = new VisualElement { pickingMode = PickingMode.Ignore };
            _band.AddToClassList("celeb__band");
            _ray = new VisualElement { pickingMode = PickingMode.Ignore };
            _ray.AddToClassList("celeb__ray");
            _well = new VisualElement { pickingMode = PickingMode.Ignore };
            _well.AddToClassList("celeb__well");
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("celeb__body");
            _kicker = Kit.Label(string.Empty, "celeb__kicker");
            _title = Kit.Label(string.Empty, "celeb__title");
            _line = Kit.Label(string.Empty, "celeb__line");
            body.Add(_kicker);
            body.Add(_title);
            body.Add(_line);
            _band.Add(_ray);
            _band.Add(_well);
            _band.Add(body);
            _root.Add(_band);
            layer.Add(_root);
        }

        /// <summary>Turns a sim event into a moment, if it is one worth marking.</summary>
        public void OnEvent(SimEvent e, GameState s, SimConfig c)
        {
            switch (e.Kind)
            {
                case EventKind.TierAdvanced:
                    int plots = e.A - 2 >= 0 && e.A - 2 < c.Tier.SlotsAdded.Length ? c.Tier.SlotsAdded[e.A - 2] : 0;
                    Push(new Moment("tier", "up", "TIER REACHED", "TIER " + e.A + " // " + ModuleTexts.TierName(e.A),
                        "+" + plots + " PLOTS. " + e.B + " people left to settle the new ground. We are bigger now. Bigger is louder."));
                    break;
                case EventKind.ResearchCompleted:
                    var node = (ModuleNode)e.A;
                    Push(new Moment("module", "memory", "MEMORY RESTORED", ModuleTexts.Name(node), ModuleTexts.Effect(node, c)));
                    break;
                case EventKind.RaidResolved when (RaidOutcome)e.B == RaidOutcome.Repelled:
                    Push(new Moment("win", "shield", "ATTACK REPELLED", "THE WALL HELD",
                        "About " + Fmt.Num(e.C) + " came at us. Our defense was " + Fmt.Num(e.D) + ". They will remember that."));
                    break;
                case EventKind.BuildCompleted when e.C >= c.Facility((FacilityKind)e.B).MaxLevel:
                    Push(new Moment("max", "star", "FULLY UPGRADED", Names.Facility((FacilityKind)e.B) + " // LEVEL " + e.C,
                        "Nothing left to improve on it here. A new tier opens more plots."));
                    break;
                case EventKind.MasteryEarned:
                    string name = e.A >= 0 && e.A < LegacyScreen.MasteryNames.Length ? LegacyScreen.MasteryNames[e.A] : "MASTERY";
                    Push(new Moment("module", "check", "MASTERY EARNED", name, "+" + c.Legacy.ScorePerMastery + " legacy score. I did not think you would."));
                    break;
            }
        }

        public void Tick(float dt)
        {
            if (_left > 0f)
            {
                _left -= dt;
                if (_left <= 0f)
                {
                    Hide();
                }

                return;
            }

            if (_queue.Count > 0)
            {
                Show(_queue.Dequeue());
            }
        }

        private void Push(Moment m)
        {
            if (_queue.Count < MaxQueued)
            {
                _queue.Enqueue(m);
            }
        }

        private void Show(Moment m)
        {
            foreach (string tone in new[] { "tier", "module", "win", "max" })
            {
                _root.EnableInClassList("celeb--" + tone, tone == m.Tone);
            }

            _icon?.RemoveFromHierarchy();
            _icon = Icons.Create(m.Glyph, "celeb__icon");
            _icon.pickingMode = PickingMode.Ignore;
            _well.Add(_icon);
            _kicker.text = m.Kicker;
            _title.text = m.Title;
            _line.text = m.Line;
            _root.RemoveFromClassList("is-hidden");
            _left = ShowSeconds;

            // the band opens from the middle, the icon lands, the text rises in, a light sweeps across
            Motion.To(_band, 0.42f, Ease.OutBack, t =>
            {
                _band.style.opacity = Mathf.Clamp01(t * 2f);
                _band.style.scale = new Scale(new Vector3(0.6f + (0.4f * t), 1f, 1f));
            });
            Motion.After(_well, 0.18f, 0.5f, Ease.OutBack, t => _well.style.scale = new Scale(new Vector3(0.3f + (0.7f * t), 0.3f + (0.7f * t), 1f)));
            Label[] text = { _kicker, _title, _line };
            for (int i = 0; i < text.Length; i++)
            {
                Label l = text[i];
                Motion.After(l, 0.22f + (0.08f * i), 0.4f, Ease.OutCubic, t =>
                {
                    l.style.opacity = t;
                    l.style.translate = new Translate(24f * (1f - t), 0);
                });
            }

            Motion.After(_ray, 0.3f, 1.1f, Ease.InOutSine, t =>
            {
                _ray.style.left = Length.Percent(-30f + (140f * t));
                _ray.style.opacity = Mathf.Sin(t * Mathf.PI);
            });
        }

        private void Hide()
        {
            Motion.To(_band, 0.3f, Ease.OutCubic, t =>
            {
                _band.style.opacity = 1f - t;
                _band.style.translate = new Translate(0, -30f * t);
            }, () =>
            {
                _root.AddToClassList("is-hidden");
                _band.style.translate = new Translate(0, 0);
            });
        }

        private readonly struct Moment
        {
            public Moment(string tone, string glyph, string kicker, string title, string line)
            {
                Tone = tone;
                Glyph = glyph;
                Kicker = kicker;
                Title = title;
                Line = line;
            }

            public string Tone { get; }

            public string Glyph { get; }

            public string Kicker { get; }

            public string Title { get; }

            public string Line { get; }
        }
    }
}
