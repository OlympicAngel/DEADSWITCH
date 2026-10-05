using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Long-press explanations (SPEC-039 idea 59): holding a readout for <see cref="HoldSeconds"/> shows a short
    /// card saying what it means; releasing hides it. A tap still does what the element normally does.
    /// </summary>
    public static class Hints
    {
        private const float HoldSeconds = 0.45f;

        private static VisualElement _card;
        private static Label _title;
        private static Label _body;

        public static void Mount(VisualElement layer)
        {
            _card = new VisualElement { pickingMode = PickingMode.Ignore };
            _card.AddToClassList("hint");
            _card.AddToClassList("is-hidden");
            _title = Kit.Label(string.Empty, "hint__title");
            _body = Kit.Label(string.Empty, "hint__body");
            _card.Add(_title);
            _card.Add(_body);
            layer.Add(_card);
        }

        /// <summary>Explains <paramref name="el"/> on a long press.</summary>
        public static void Attach(VisualElement el, string title, string body)
        {
            IVisualElementScheduledItem pending = null;
            el.RegisterCallback<PointerDownEvent>(e =>
            {
                Vector2 at = e.position;
                pending = el.schedule.Execute(() => Show(title, body, at)).StartingIn((long)(HoldSeconds * 1000));
            }, TrickleDown.TrickleDown);
            el.RegisterCallback<PointerUpEvent>(_ => Release(ref pending), TrickleDown.TrickleDown);
            el.RegisterCallback<PointerLeaveEvent>(_ => Release(ref pending));
        }

        private static void Release(ref IVisualElementScheduledItem pending)
        {
            pending?.Pause();
            pending = null;
            _card?.AddToClassList("is-hidden");
        }

        private static void Show(string title, string body, Vector2 at)
        {
            if (_card == null)
            {
                return;
            }

            _title.text = title;
            _body.text = body;
            _card.RemoveFromClassList("is-hidden");
            float width = _card.parent.layout.width;
            _card.style.top = at.y + 40f;
            _card.style.left = Mathf.Clamp(at.x - 300f, 24f, Mathf.Max(24f, width - 624f));
        }
    }
}
