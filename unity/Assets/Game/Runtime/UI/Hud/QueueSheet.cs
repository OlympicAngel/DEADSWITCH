using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The build queue (SPEC-042 finding 9): every job with its live progress and time; a row flies to its plot.
    /// With nothing building it says so and offers the next free plot.
    /// </summary>
    public sealed class QueueSheet
    {
        private readonly VisualElement _root;
        private readonly VisualElement _list;
        private readonly System.Action<int> _onSlot;
        private readonly List<(BuildJob Job, VisualElement Bar, Label Time)> _rows = new List<(BuildJob, VisualElement, Label)>();

        public QueueSheet(VisualElement layer, System.Action<int> onSlot)
        {
            _onSlot = onSlot;
            _root = new VisualElement();
            _root.AddToClassList("ds-sheet");
            _root.AddToClassList("sheet");
            _root.AddToClassList("qsheet");
            _root.AddToClassList("is-hidden");
            Sheen.Attach(_root);
            var grip = new VisualElement();
            grip.AddToClassList("ds-sheet__grip");
            _root.Add(grip);
            _list = new VisualElement();
            _root.Add(_list);
            _root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            layer.Add(_root);
            Popovers.Register("queue", () => IsOpen, Close, _root);
            Back.Register(() =>
            {
                if (!IsOpen)
                {
                    return false;
                }

                Close();
                return true;
            });
        }

        public bool IsOpen { get; private set; }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            Popovers.Opening("queue");
            IsOpen = true;
            _root.RemoveFromClassList("is-hidden");
            Refresh();
            Choreo.Enter(_root);
        }

        public void Close()
        {
            IsOpen = false;
            _root.AddToClassList("is-hidden");
        }

        public void Refresh()
        {
            if (!IsOpen)
            {
                return;
            }

            GameHost host = GameHost.Instance;
            GameState s = host.Sim.State;
            _list.Clear();
            _rows.Clear();
            var head = new VisualElement();
            head.AddToClassList("sheet__head");
            var well = new VisualElement();
            well.AddToClassList("sheet__well");
            well.Add(Icons.Create("hammer", "sheet__icon"));
            head.Add(well);
            var titles = new VisualElement();
            titles.AddToClassList("sheet__titles");
            titles.Add(Kit.Label("BUILD QUEUE", "sheet__title"));
            titles.Add(Kit.Label(s.Jobs.Count == 0 ? "NOTHING UNDER CONSTRUCTION" : s.Jobs.Count + (s.Jobs.Count == 1 ? " JOB RUNNING" : " JOBS RUNNING"), "sheet__level"));
            head.Add(titles);
            var close = new VisualElement();
            close.AddToClassList("ds-iconbtn");
            close.Add(Icons.Create("close", "ds-iconbtn__icon"));
            close.RegisterCallback<ClickEvent>(_ => Close());
            head.Add(close);
            _list.Add(head);

            var jobs = new List<BuildJob>(s.Jobs);
            jobs.Sort((a, b) => a.CompleteTick.CompareTo(b.CompleteTick));
            foreach (BuildJob job in jobs)
            {
                var row = new VisualElement();
                row.AddToClassList("qsheet__row");
                row.Add(Icons.Create(Icons.ForFacility(job.Kind), "qsheet__icon"));
                var body = new VisualElement();
                body.AddToClassList("qsheet__body");
                var line = new VisualElement();
                line.AddToClassList("row");
                line.Add(Kit.Label(Fmt.FacilityName(job.Kind), "qsheet__name"));
                line.Add(Kit.Label(job.TargetLevel > 1 ? "UPGRADE TO L" + job.TargetLevel : "NEW BUILD", "qsheet__kind"));
                body.Add(line);
                VisualElement bar = Kit.Progress("amber");
                bar.AddToClassList("qsheet__bar");
                body.Add(bar);
                row.Add(body);
                Label time = Kit.Label(string.Empty, "qsheet__time");
                row.Add(time);
                int slot = job.Slot;
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    Close();
                    _onSlot(slot);
                });
                _list.Add(row);
                _rows.Add((job, bar, time));
            }

            int plot = ResourceInfo.FreePlot(s);
            if (plot >= 0)
            {
                var add = new VisualElement();
                add.AddToClassList("qsheet__row");
                add.AddToClassList("qsheet__row--add");
                add.Add(Icons.Create("plus", "qsheet__icon"));
                add.Add(Kit.Label(s.Jobs.Count == 0 ? "BUILDERS ARE IDLE // BUILD ON PLOT " + (plot + 1) : "QUEUE ANOTHER // PLOT " + (plot + 1), "qsheet__name"));
                add.RegisterCallback<ClickEvent>(_ =>
                {
                    Close();
                    _onSlot(plot);
                });
                _list.Add(add);
            }

            Tick();
        }

        /// <summary>Live bars and times while open.</summary>
        public void Tick()
        {
            if (!IsOpen)
            {
                return;
            }

            GameHost host = GameHost.Instance;
            long now = host.Sim.State.Tick;
            foreach ((BuildJob job, VisualElement bar, Label time) in _rows)
            {
                float total = Mathf.Max(1f, job.CompleteTick - job.StartTick);
                Kit.SetProgress(bar, ((now - job.StartTick) + host.TickProgress) / total);
                long ticks = job.CompleteTick - now;
                double seconds = ticks <= 0 ? 0 : host.SecondsToNextTick + ((ticks - 1) * 60.0 / host.Settings.DevTimeScale);
                time.text = Fmt.Countdown(seconds);
            }
        }
    }
}
