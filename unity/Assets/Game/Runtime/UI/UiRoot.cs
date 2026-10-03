using Deadswitch.Game.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Creates the UI Toolkit panel at runtime (no scene or asset wiring) and the screen layers:
    /// world (transparent, receives taps for the 3D base), hud, sheets, overlay (CRT, top-most, no input).
    /// Applies the safe area and drives motion, gauges and the CRT from one Update.
    /// </summary>
    public sealed class UiRoot : MonoBehaviour
    {
        public const int ReferenceWidth = 1080;
        public const int ReferenceHeight = 1920;

        private UIDocument _document;
        private CrtOverlay _crt;
        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreen;

        public static UiRoot Instance { get; private set; }

        /// <summary>Full-screen root with the token class; everything else lives under it.</summary>
        public VisualElement Root { get; private set; }

        /// <summary>Transparent layer behind the HUD; taps here go to the 3D world.</summary>
        public VisualElement World { get; private set; }

        /// <summary>Safe-area-padded HUD layer.</summary>
        public VisualElement Hud { get; private set; }

        /// <summary>Bottom sheets and dialogs, above the HUD.</summary>
        public VisualElement Sheets { get; private set; }

        /// <summary>Per-frame callbacks for presentation code (counters, gauges).</summary>
        public event System.Action<float> Frame;

        public CrtOverlay Crt => _crt;

        private void Awake()
        {
            Instance = this;

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "DEADSWITCH Panel";
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(ReferenceWidth, ReferenceHeight);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0f;
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/DeadswitchTheme");

            var host = new GameObject("UI");
            host.transform.SetParent(transform, false);
            host.SetActive(false);
            _document = host.AddComponent<UIDocument>();
            _document.panelSettings = settings;
            host.SetActive(true);

            Root = _document.rootVisualElement;
            Root.AddToClassList("ds-root");
            Root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Tokens"));
            Root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Components"));
            Root.style.flexGrow = 1;

            World = Layer("ds-layer-world");
            Hud = Layer("ds-layer-hud");
            Hud.pickingMode = PickingMode.Ignore;
            Sheets = Layer("ds-layer-sheets");
            Sheets.pickingMode = PickingMode.Ignore;
            var overlay = Layer("ds-layer-overlay");
            overlay.pickingMode = PickingMode.Ignore;
            Root.Add(World);
            Root.Add(Hud);
            Root.Add(Sheets);
            Root.Add(overlay);
            _crt = new CrtOverlay(overlay);
        }

        private void Start()
        {
            GameHost host = GameHost.Instance;
            if (host != null)
            {
                ApplySettings(host.Settings);
                host.Settings.Changed += () => ApplySettings(host.Settings);
            }
        }

        /// <summary>Loads a UXML layout from Resources/UI and decorates it with kit behaviors.</summary>
        public static TemplateContainer Load(string name)
        {
            var tree = Resources.Load<VisualTreeAsset>("UI/" + name);
            TemplateContainer el = tree.Instantiate();
            el.style.flexGrow = 1;
            el.pickingMode = PickingMode.Ignore;
            Kit.Decorate(el);
            return el;
        }

        /// <summary>Corruption glitch weight (0..1) for the CRT layer.</summary>
        public void SetGlitch(float weight)
        {
            GameHost host = GameHost.Instance;
            _crt.Configure(host != null ? host.Settings.Effects : 1f, weight);
        }

        private void ApplySettings(GameSettings s)
        {
            Motion.Reduced = s.ReducedMotion;
            Feedback.Enabled = s.Haptics;
            _crt.Configure(s.Effects, 0f);
        }

        private static VisualElement Layer(string cls)
        {
            var e = new VisualElement();
            e.AddToClassList(cls);
            e.style.position = Position.Absolute;
            e.style.left = 0;
            e.style.right = 0;
            e.style.top = 0;
            e.style.bottom = 0;
            return e;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            ApplySafeArea();
            Motion.Tick(dt);
            _crt.Tick(dt, Motion.Reduced);
            Frame?.Invoke(dt);
        }

        private void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == _appliedSafeArea && screen == _appliedScreen)
            {
                return;
            }

            float panelWidth = Root.layout.width;
            if (float.IsNaN(panelWidth) || panelWidth <= 0f || screen.x <= 0)
            {
                return;
            }

            _appliedSafeArea = safe;
            _appliedScreen = screen;
            float k = panelWidth / screen.x;
            foreach (VisualElement layer in new[] { Hud, Sheets })
            {
                layer.style.paddingLeft = safe.xMin * k;
                layer.style.paddingRight = (screen.x - safe.xMax) * k;
                layer.style.paddingTop = (screen.y - safe.yMax) * k;
                layer.style.paddingBottom = safe.yMin * k;
            }
        }
    }
}
