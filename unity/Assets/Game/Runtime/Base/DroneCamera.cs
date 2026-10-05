using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The AI's recon drone (ADR-0007): high three-quarter view, narrow FOV, a slow hover drift. One finger pans
    /// within bounds (flicks glide on), two fingers pinch-zoom, a double tap on open ground toggles a closer view,
    /// a tap selects a slot or the core door. Selecting a facility flies the drone in and swings it to a
    /// three-quarter angle (SPEC-039 C); idle for a while, it sways slowly. Cinematics borrow the camera through
    /// <see cref="Direct"/> and hand it back with <see cref="Release"/>. Input comes from the UI's world layer so
    /// HUD elements always win taps.
    /// </summary>
    public sealed class DroneCamera : MonoBehaviour
    {
        private const float TapSlop = 18f;
        private const float BasePanX = 8f;
        private const float PanLimitZ = 7f;
        private const float DistrictPanZ = 15f;
        private const float StrongholdPanZ = 25f;
        private const float SectorPanX = 13f;

        private readonly Dictionary<int, Vector2> _pointers = new Dictionary<int, Vector2>();
        private BaseLook _look;
        private Camera _cam;
        private Vector3 _pan;
        private Vector3 _panTarget;
        private float _zoom = 1f;
        private float _zoomTarget = 1f;
        private float _time;
        private Vector2 _downPos;
        private bool _moved;
        private float _pinchStart;
        private float _zoomAtPinch;
        private float _orbit;
        private float _orbitTarget;
        private bool _focused;
        private float _zoomBeforeFocus = 1f;
        private Vector3 _velocity;
        private float _idle;
        private float _lastTapAt = -10f;
        private Vector2 _lastTapPos;
        private float _trauma;
        private bool _override;
        private float _overrideBlend;
        private Vector3 _overridePos;
        private Vector3 _overrideLook;
        private float _overrideFov;

        public static DroneCamera Instance { get; private set; }

        /// <summary>How far south the drone may pan: the district outside the gate opens at Tier 2 (SPEC-013).</summary>
        private static float SouthLimit => Tier >= 3 ? StrongholdPanZ : Tier >= 2 ? DistrictPanZ : PanLimitZ;

        /// <summary>How far the drone may pan sideways: the Sector terraces open at Tier 4.</summary>
        private static float PanLimitX => Tier >= 4 ? SectorPanX : BasePanX;

        private static int Tier => GameHost.Instance != null && GameHost.Instance.Sim != null ? GameHost.Instance.Sim.State.Tier : 1;

        public Camera Camera => _cam;

        /// <summary>Raised when a facility plot is tapped (slot id).</summary>
        public event System.Action<int> SlotTapped;

        /// <summary>Raised when the bunker door (the way to the core) is tapped.</summary>
        public event System.Action CoreTapped;

        /// <summary>Raised when empty ground is tapped (deselect).</summary>
        public event System.Action NothingTapped;

        /// <summary>True while a facility is framed (the drone flew in).</summary>
        public bool IsFocused => _focused;

        /// <summary>Eases the view toward a world point without zooming (for example a battle at the gate).</summary>
        public void Focus(Vector3 world)
        {
            _velocity = Vector3.zero;
            _panTarget = ClampPan(world - _look.Target);
        }

        /// <summary>
        /// Flies in and frames a facility (SPEC-039 idea 21): pans it above the info card, zooms in and swings to a
        /// three-quarter angle. With the fly-in setting off it only pans.
        /// </summary>
        public void FocusOn(Vector3 world)
        {
            InterfaceConfig.FocusRules f = InterfaceConfig.Current.focus;
            bool fly = GameHost.Instance == null || GameHost.Instance.Settings.FocusFlyIn;
            if (!_focused)
            {
                _zoomBeforeFocus = _zoomTarget;
            }

            _focused = true;
            _velocity = Vector3.zero;
            _idle = 0f;
            if (fly)
            {
                _zoomTarget = Mathf.Min(_zoomTarget, f.zoom);
                _orbitTarget = (world.x >= 0f ? -1f : 1f) * f.orbitDegrees;
            }

            // the look point moves toward the drone so the facility sits higher on screen than the card below it
            float az = (_look.camAzimuth + _orbitTarget) * Mathf.Deg2Rad;
            float el = _look.camElevation * Mathf.Deg2Rad;
            var toward = new Vector3(Mathf.Sin(az), 0, -Mathf.Cos(az));
            float groundSpan = 2f * _look.camDistance * _zoomTarget * Mathf.Tan(_look.fov * 0.5f * Mathf.Deg2Rad) / Mathf.Max(0.2f, Mathf.Sin(el));
            _panTarget = ClampPan(world - _look.Target + (toward * f.screenLift * groundSpan), 4f);
        }

        /// <summary>Flies back out after a focus (deselect).</summary>
        public void ClearFocus()
        {
            if (!_focused)
            {
                return;
            }

            _focused = false;
            _zoomTarget = _zoomBeforeFocus;
            _orbitTarget = 0f;
        }

        /// <summary>Adds camera shake (0..1 trauma); scaled by effect intensity, none under reduced motion.</summary>
        public void Shake(float amount)
        {
            _trauma = Mathf.Min(1f, _trauma + amount);
        }

        /// <summary>A cinematic takes the camera this frame (blends in from the drone view on the first call).</summary>
        public void Direct(Vector3 position, Vector3 lookAt, float fov)
        {
            _override = true;
            _overridePos = position;
            _overrideLook = lookAt;
            _overrideFov = fov;
        }

        /// <summary>Hands the camera back to the drone (blends out).</summary>
        public void Release()
        {
            _override = false;
        }

        /// <summary>Snaps a running blend to the cinematic (for hard cuts between shots).</summary>
        public void Cut()
        {
            _overrideBlend = _override ? 1f : 0f;
        }

        /// <summary>Pan offset clamped to the drone's bounds (plus a margin while framing).</summary>
        private static Vector3 ClampPan(Vector3 rel, float margin = 0f)
        {
            return new Vector3(Mathf.Clamp(rel.x, -PanLimitX - margin, PanLimitX + margin), 0, Mathf.Clamp(rel.z, -SouthLimit - margin, PanLimitZ + margin));
        }

        private void Awake()
        {
            Instance = this;
            _look = BaseLook.Load();
            var go = new GameObject("Drone Camera");
            go.transform.SetParent(transform, false);
            _cam = go.AddComponent<Camera>();
            _cam.tag = "MainCamera";
            _cam.fieldOfView = _look.fov;
            _cam.nearClipPlane = 1f;
            _cam.farClipPlane = 400f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = BaseLook.Srgb(_look.At(12f).fogColor);
            _cam.allowHDR = true;

            // the sector map world lives on its own layer far below (SPEC-033)
            _cam.cullingMask &= ~(1 << MapView.Layer);
        }

        private void Start()
        {
            VisualElement world = UiRoot.Instance.World;
            world.RegisterCallback<PointerDownEvent>(OnDown);
            world.RegisterCallback<PointerMoveEvent>(OnMove);
            world.RegisterCallback<PointerUpEvent>(OnUp);
            world.RegisterCallback<PointerCancelEvent>(e => _pointers.Remove(e.pointerId));
            world.RegisterCallback<WheelEvent>(e =>
            {
                InterfaceConfig.CameraRules r = InterfaceConfig.Current.camera;
                _zoomTarget = Mathf.Clamp(_zoomTarget + (e.delta.y * 0.02f), r.minZoom, r.maxZoom);
                _idle = 0f;
            });
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            if (BaseView.Instance != null)
            {
                _cam.backgroundColor = BaseLook.Srgb(BaseView.Instance.Lighting.fogColor);
            }

            bool reduced = GameHost.Instance != null && GameHost.Instance.Settings.ReducedMotion;
            float effects = GameHost.Instance != null ? GameHost.Instance.Settings.Effects : 1f;
            InterfaceConfig.CameraRules rules = InterfaceConfig.Current.camera;

            // flick inertia: the pan glides on after release and settles
            if (_pointers.Count == 0 && _velocity.sqrMagnitude > 0.0001f)
            {
                _panTarget = ClampPan(_panTarget + (_velocity * dt), _focused ? 4f : 0f);
                _velocity *= Mathf.Exp(-rules.inertiaDamping * dt);
            }

            // idle sway: after a while untouched the drone drifts around the compound
            _idle += dt;
            if (!_focused && !reduced && _idle > rules.idleOrbitAfter)
            {
                const float amplitude = 12f;
                _orbitTarget = amplitude * Mathf.Sin((_idle - rules.idleOrbitAfter) * rules.idleOrbitDegreesPerSecond / amplitude);
            }

            float k = 1f - Mathf.Exp(-dt * (_focused ? 5f : 8f));
            _pan = Vector3.Lerp(_pan, _panTarget, k);
            _zoom = Mathf.Lerp(_zoom, _zoomTarget, k);
            _orbit = Mathf.Lerp(_orbit, _orbitTarget, 1f - Mathf.Exp(-dt * (_focused ? 4f : 1.5f)));

            float drift = reduced ? 0f : 1f;
            float az = (_look.camAzimuth + _orbit + (Mathf.Sin(_time * 0.11f) * 1.4f * drift)) * Mathf.Deg2Rad;
            float el = (_look.camElevation + (Mathf.Sin(_time * 0.07f + 1.3f) * 0.8f * drift)) * Mathf.Deg2Rad;
            float dist = _look.camDistance * _zoom;
            Vector3 target = _look.Target + _pan;
            var offset = new Vector3(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), -Mathf.Cos(el) * Mathf.Cos(az)) * dist;
            Vector3 hover = new Vector3(0, Mathf.Sin(_time * 0.9f) * 0.08f * drift, 0);
            Vector3 pos = target + offset + hover;
            Vector3 look = target + hover;
            float fov = _look.fov;

            // cinematics: blend the drone pose toward the directed shot and back
            _overrideBlend = Mathf.MoveTowards(_overrideBlend, _override ? 1f : 0f, dt * 2.2f);
            if (_overrideBlend > 0f)
            {
                float b = Ease.InOutSine(_overrideBlend);
                pos = Vector3.Lerp(pos, _overridePos, b);
                look = Vector3.Lerp(look, _overrideLook, b);
                fov = Mathf.Lerp(fov, _overrideFov, b);
            }

            // impact shake: trauma squared, smooth noise, decays (doc 11: never repeated or violent)
            _trauma = Mathf.Max(0f, _trauma - (dt * 1.6f));
            if (!reduced && _trauma > 0f)
            {
                float amp = _trauma * _trauma * effects;
                var jitter = new Vector3(Mathf.PerlinNoise(_time * 22f, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, _time * 22f) - 0.5f, Mathf.PerlinNoise(_time * 19f, 5.1f) - 0.5f);
                pos += jitter * amp * 0.9f;
                look += jitter * amp * 0.4f;
            }

            _cam.fieldOfView = fov;
            _cam.transform.position = pos;
            _cam.transform.LookAt(look);
        }

        private void OnDown(PointerDownEvent e)
        {
            _pointers[e.pointerId] = e.position;
            _idle = 0f;
            _velocity = Vector3.zero;
            if (!_focused)
            {
                _orbitTarget = 0f;
            }

            if (_pointers.Count == 1)
            {
                _downPos = e.position;
                _moved = false;
            }
            else if (_pointers.Count == 2)
            {
                _pinchStart = PinchDistance();
                _zoomAtPinch = _zoomTarget;
                _moved = true;
            }
        }

        private void OnMove(PointerMoveEvent e)
        {
            if (!_pointers.TryGetValue(e.pointerId, out Vector2 last))
            {
                return;
            }

            _pointers[e.pointerId] = e.position;
            if (_pointers.Count >= 2)
            {
                float d = PinchDistance();
                if (_pinchStart > 1f)
                {
                    _zoomTarget = Mathf.Clamp(_zoomAtPinch * (_pinchStart / Mathf.Max(1f, d)), InterfaceConfig.Current.camera.minZoom, InterfaceConfig.Current.camera.maxZoom);
                }

                return;
            }

            Vector2 pos = e.position;
            if ((pos - _downPos).magnitude > TapSlop)
            {
                _moved = true;
            }

            if (_moved)
            {
                Vector2 delta = pos - last;
                float scale = 0.035f * _zoom;
                var step = new Vector3(-delta.x * scale, 0, delta.y * scale);
                _panTarget = ClampPan(_panTarget + step, _focused ? 4f : 0f);
                float dt = Mathf.Max(1f / 120f, Time.unscaledDeltaTime);
                _velocity = Vector3.Lerp(_velocity, step / dt, 0.5f);
            }
        }

        private void OnUp(PointerUpEvent e)
        {
            bool wasSingle = _pointers.Count == 1;
            _pointers.Remove(e.pointerId);
            if (!wasSingle || _moved)
            {
                if (!wasSingle)
                {
                    _velocity = Vector3.zero;
                }

                return;
            }

            _velocity = Vector3.zero;

            if (!GroundPoint(e.position, out Vector3 ground))
            {
                return;
            }

            BaseView view = BaseView.Instance;
            int slot = view != null ? view.SlotAt(ground) : -1;
            if (slot >= 0)
            {
                SlotTapped?.Invoke(slot);
            }
            else if (view != null && view.IsCoreDoor(ground))
            {
                CoreTapped?.Invoke();
            }
            else
            {
                // a second tap on open ground toggles between the overview and a closer look (idea 26)
                InterfaceConfig.CameraRules r = InterfaceConfig.Current.camera;
                if (Time.unscaledTime - _lastTapAt < r.doubleTapSeconds && (e.position - (Vector3)_lastTapPos).magnitude < 80f)
                {
                    _zoomTarget = _zoomTarget > (r.doubleTapZoom + 1f) * 0.5f ? r.doubleTapZoom : 1f;
                    _panTarget = ClampPan(_panTarget + ((ground - (_look.Target + _panTarget)) * 0.5f));
                    _lastTapAt = -10f;
                    return;
                }

                _lastTapAt = Time.unscaledTime;
                _lastTapPos = e.position;
                NothingTapped?.Invoke();
            }
        }

        private float PinchDistance()
        {
            var e = _pointers.Values.GetEnumerator();
            e.MoveNext();
            Vector2 a = e.Current;
            e.MoveNext();
            Vector2 b = e.Current;
            return Vector2.Distance(a, b);
        }

        /// <summary>Panel position (y down) -> ground plane hit.</summary>
        private bool GroundPoint(Vector2 panelPos, out Vector3 ground)
        {
            ground = Vector3.zero;
            VisualElement root = UiRoot.Instance.Root;
            float w = root.layout.width;
            float h = root.layout.height;
            if (w <= 0f || h <= 0f)
            {
                return false;
            }

            var screen = new Vector3(panelPos.x / w * Screen.width, Screen.height - (panelPos.y / h * Screen.height), 0);
            Ray ray = _cam.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.y) < 0.0001f)
            {
                return false;
            }

            float t = -ray.origin.y / ray.direction.y;
            if (t <= 0f)
            {
                return false;
            }

            ground = ray.origin + (ray.direction * t);
            return true;
        }
    }
}
