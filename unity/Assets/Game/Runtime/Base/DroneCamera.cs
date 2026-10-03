using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The AI's recon drone (ADR-0007): high three-quarter view, narrow FOV, a slow hover drift. One finger pans
    /// within bounds, two fingers pinch-zoom, a tap selects a slot or the core door. Input comes from the UI's
    /// world layer so HUD elements always win taps.
    /// </summary>
    public sealed class DroneCamera : MonoBehaviour
    {
        private const float TapSlop = 18f;
        private const float PanLimitX = 8f;
        private const float PanLimitZ = 7f;
        private const float DistrictPanZ = 15f;

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

        public static DroneCamera Instance { get; private set; }

        /// <summary>How far south the drone may pan: the district outside the gate opens at Tier 2 (SPEC-013).</summary>
        private static float SouthLimit => GameHost.Instance != null && GameHost.Instance.Sim.State.Tier >= 2 ? DistrictPanZ : PanLimitZ;

        public Camera Camera => _cam;

        /// <summary>Raised when a facility plot is tapped (slot id).</summary>
        public event System.Action<int> SlotTapped;

        /// <summary>Raised when the bunker door (the way to the core) is tapped.</summary>
        public event System.Action CoreTapped;

        /// <summary>Raised when empty ground is tapped (deselect).</summary>
        public event System.Action NothingTapped;

        /// <summary>Eases the view toward a world point (for example the selected slot).</summary>
        public void Focus(Vector3 world)
        {
            _panTarget = new Vector3(Mathf.Clamp(world.x * 0.5f, -PanLimitX, PanLimitX), 0, Mathf.Clamp((world.z - _look.Target.z) * 0.5f, -SouthLimit, PanLimitZ));
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
            _cam.backgroundColor = BaseLook.Srgb(_look.fogColor);
            _cam.allowHDR = true;
        }

        private void Start()
        {
            VisualElement world = UiRoot.Instance.World;
            world.RegisterCallback<PointerDownEvent>(OnDown);
            world.RegisterCallback<PointerMoveEvent>(OnMove);
            world.RegisterCallback<PointerUpEvent>(OnUp);
            world.RegisterCallback<PointerCancelEvent>(e => _pointers.Remove(e.pointerId));
            world.RegisterCallback<WheelEvent>(e => _zoomTarget = Mathf.Clamp(_zoomTarget + (e.delta.y * 0.02f), 0.7f, 1.3f));
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            bool reduced = GameHost.Instance != null && GameHost.Instance.Settings.ReducedMotion;
            float k = 1f - Mathf.Exp(-dt * 8f);
            _pan = Vector3.Lerp(_pan, _panTarget, k);
            _zoom = Mathf.Lerp(_zoom, _zoomTarget, k);

            float drift = reduced ? 0f : 1f;
            float az = (_look.camAzimuth + (Mathf.Sin(_time * 0.11f) * 1.4f * drift)) * Mathf.Deg2Rad;
            float el = (_look.camElevation + (Mathf.Sin(_time * 0.07f + 1.3f) * 0.8f * drift)) * Mathf.Deg2Rad;
            float dist = _look.camDistance * _zoom;
            Vector3 target = _look.Target + _pan;
            var offset = new Vector3(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), -Mathf.Cos(el) * Mathf.Cos(az)) * dist;
            Vector3 hover = new Vector3(0, Mathf.Sin(_time * 0.9f) * 0.08f * drift, 0);
            _cam.transform.position = target + offset + hover;
            _cam.transform.LookAt(target + hover);
        }

        private void OnDown(PointerDownEvent e)
        {
            _pointers[e.pointerId] = e.position;
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
                    _zoomTarget = Mathf.Clamp(_zoomAtPinch * (_pinchStart / Mathf.Max(1f, d)), 0.7f, 1.3f);
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
                _panTarget += new Vector3(-delta.x * scale, 0, delta.y * scale);
                _panTarget.x = Mathf.Clamp(_panTarget.x, -PanLimitX, PanLimitX);
                _panTarget.z = Mathf.Clamp(_panTarget.z, -SouthLimit, PanLimitZ);
            }
        }

        private void OnUp(PointerUpEvent e)
        {
            bool wasSingle = _pointers.Count == 1;
            _pointers.Remove(e.pointerId);
            if (!wasSingle || _moved)
            {
                return;
            }

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
