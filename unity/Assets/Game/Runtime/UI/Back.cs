using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Predictable back (SPEC-042 rule 4): Android back / Escape (UI Toolkit's navigation cancel) asks each
    /// registered layer, most recent first, to close itself. The first one that was open consumes the press.
    /// </summary>
    public static class Back
    {
        private static readonly List<System.Func<bool>> Handlers = new List<System.Func<bool>>();

        public static void Install(VisualElement root)
        {
            root.RegisterCallback<NavigationCancelEvent>(e =>
            {
                if (Press())
                {
                    e.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
        }

        /// <summary>Registers a layer; return true when it was open and closed itself.</summary>
        public static void Register(System.Func<bool> closeIfOpen)
        {
            Handlers.Add(closeIfOpen);
        }

        public static bool Press()
        {
            for (int i = Handlers.Count - 1; i >= 0; i--)
            {
                if (Handlers[i]())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
