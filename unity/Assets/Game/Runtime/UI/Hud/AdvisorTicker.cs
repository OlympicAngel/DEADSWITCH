using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The advisor's one-line terminal voice under the status strip: types lines out, then holds them.
    /// Corruption glitches the text (never numbers); reduced motion shows lines instantly.
    /// </summary>
    public sealed class AdvisorTicker
    {
        private const float CharsPerSecond = 42f;

        private readonly Label _label;
        private string _line = string.Empty;
        private float _shown;
        private float _glitch;
        private float _glitchClock;
        private int _frame;

        public AdvisorTicker(Label label)
        {
            _label = label;
        }

        public string Current => _line;

        public void Say(string line)
        {
            _line = line ?? string.Empty;
            Audio.AudioDirector.Instance?.Speak(_line);
            _shown = Motion.Reduced ? _line.Length : 0f;
            Render();
        }

        /// <param name="glitch">0..1 corruption weight x effect intensity.</param>
        public void SetGlitch(float glitch)
        {
            _glitch = glitch;
        }

        public void Tick(float dt)
        {
            bool typing = _shown < _line.Length;
            if (typing)
            {
                _shown += dt * CharsPerSecond;
            }

            _glitchClock += dt;
            if (typing || (_glitch > 0f && _glitchClock > 0.12f))
            {
                _glitchClock = 0f;
                _frame++;
                Render();
            }
        }

        private void Render()
        {
            int n = (int)System.Math.Min(_line.Length, _shown);
            string text = _line.Substring(0, n);
            bool typing = n < _line.Length;
            text = GlitchText.Apply(text, _glitch, _frame / 4);
            _label.text = typing ? text + "_" : text;
        }
    }
}
