using System.Collections.Generic;
using Deadswitch.Art.World;
using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// SECTOR MAP (F-019, SPEC-033): faction heat, the 2.5D map (a 3D render of the sector from <see cref="MapView"/>,
    /// the illustrated layer drawn with Painter2D, pins on the projected landmarks with the AI's estimates glitching
    /// by corruption), operations in the field and the selected site's sheet. Every action is a sim command.
    /// Layout: Resources/UI/Map.uxml + Map.uss.
    /// </summary>
    public sealed class MapScreen : IGameScreen
    {
        /// <summary>Panel px a press may wander before it counts as a drag (8 dp at the 1080 reference).</summary>
        private const float DragSlop = 24f;

        /// <summary>Plot height (0..1) kept clear above the open sheet: a tapped site closer to it than this pans into view.</summary>
        private const float RevealMargin = 0.12f;

        /// <summary>Wait for the opened sheet to lay out before measuring it.</summary>
        private const long RevealDelayMs = 60;

        private static readonly string[] FactionClass = { "map-site--rust", "map-site--vanguard", "map-site--church", "map-site--holdout" };

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly VisualElement _sites;
        private readonly VisualElement _ops;
        private readonly Label _reason;
        private readonly List<VisualElement> _markers = new List<VisualElement>();
        private readonly List<Label> _estimates = new List<Label>();
        private readonly VisualElement _plot;
        private readonly VisualElement _render;
        private readonly VisualElement _overlay;
        private VisualElement _hubMarker;
        private SectorOverlay _layer;
        private float _hazeTime;
        private float _tickClock;
        private int _frame;
        private float _pressAt = -1f;
        private int _pressSite = -1;
        private bool _longPressed;
        private int _selected;
        private OpKind _kind = OpKind.Raid;
        private int _squad = 4;
        private int _compute = 40;
        private bool _visible;
        private long _allyArmedAt = -1;
        private readonly List<Label> _opTimes = new List<Label>();
        private string _opsSignature;
        private readonly Dictionary<int, Vector2> _touches = new Dictionary<int, Vector2>();
        private VisualElement _pager;
        private Vector2 _dragFrom;
        private bool _dragged;
        private float _pinchFrom;
        private float _pinchZoom;
        private bool _viewMoved;
        private bool _sheetOpen;
        private bool _openingSheet;
        private int _sheetPage = -1;

        public MapScreen()
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Map");
            Root.Add(tree);
            _ui = tree;
            _sites = _ui.Q("map-sites");
            _plot = _ui.Q("map-plot");
            _render = _ui.Q("map-render");
            _overlay = _ui.Q("map-overlay");
            _overlay.generateVisualContent += DrawOverlay;
            _plot.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (_visible)
                {
                    RenderMap(true);
                }
            });
            _pager = _ui.Q("map-pager");
            BindPanZoom();
            BindSheet();
            _ops = _ui.Q("map-ops");
            _reason = _ui.Q<Label>("map-reason");
            for (int f = 0; f < WorldSystem.FactionCount; f++)
            {
                Kit.BuildMeter(_ui.Q("heat-" + f + "-meter"));
            }

            BuildMarkers();
            _ui.Q("op-scout").RegisterCallback<ClickEvent>(_ => Pick(OpKind.Scout));
            _ui.Q("op-raid").RegisterCallback<ClickEvent>(_ => Pick(OpKind.Raid));
            _ui.Q("op-hack").RegisterCallback<ClickEvent>(_ => Pick(OpKind.Hack));
            _ui.Q("op-sabotage").RegisterCallback<ClickEvent>(_ => Pick(OpKind.Sabotage));
            _ui.Q("op-minus").RegisterCallback<ClickEvent>(_ => Step(-1));
            _ui.Q("op-plus").RegisterCallback<ClickEvent>(_ => Step(1));
            _ui.Q("op-launch").RegisterCallback<ClickEvent>(_ => Run(Command.LaunchOp(_selected, _kind, _kind == OpKind.Hack ? _compute : _squad)));
            _ui.Q("site-claim").RegisterCallback<ClickEvent>(_ => Run(Command.ClaimOutpost(_selected)));
            for (int f = 0; f < WorldSystem.FactionCount; f++)
            {
                var faction = (Faction)f;
                _ui.Q("spy-" + f + "-a").RegisterCallback<ClickEvent>(_ => Run(IntelSystem.Has(_host.Sim.State, faction) ? Command.FrameFaction(faction) : Command.PlantSpy(faction)));
                _ui.Q("spy-" + f + "-b").RegisterCallback<ClickEvent>(_ => Run(Command.RecallSpy(faction)));
                _ui.Q("pact-" + f).RegisterCallback<ClickEvent>(_ => Run(Command.ProposeCeasefire(faction)));
                _ui.Q("ally-" + f).RegisterCallback<ClickEvent>(_ => Ally(faction));
            }

            for (int g = 0; g <= (int)TradeGood.Blueprints; g++)
            {
                var good = (TradeGood)g;
                _ui.Q("trade-" + g).RegisterCallback<ClickEvent>(_ => Run(Command.Trade(WorldSystem.Sites[_selected].Owner, good)));
            }

            _host.Ticked += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
            UiRoot.Instance.Frame += dt =>
            {
                if (_visible)
                {
                    RefreshOps();
                    if (_viewMoved)
                    {
                        _viewMoved = false;
                        RenderMap(false);
                    }

                    TickMap(dt);
                }
            };
        }

        public string Id => "map";

        public VisualElement Root { get; }

        public void OnShow()
        {
            _visible = true;
            _reason.text = string.Empty;
            MapView.Ensure();
            RenderMap(true);
            Refresh();
        }

        public void OnHide()
        {
            _visible = false;
        }

        /// <summary>
        /// Drag pans the sector, a pinch or the wheel zooms it (F-107). A press only becomes a drag past the slop, so a
        /// short tap still reaches the pin under it; a tap on open ground folds the sheet.
        /// </summary>
        private void BindPanZoom()
        {
            _plot.RegisterCallback<PointerDownEvent>(e =>
            {
                _touches[e.pointerId] = e.position;
                if (_touches.Count == 1)
                {
                    _dragged = false;
                    _dragFrom = e.position;
                }
                else if (_touches.Count == 2 && MapView.Instance != null)
                {
                    _dragged = true;
                    _pinchFrom = Spread();
                    _pinchZoom = MapView.Instance.Zoom;
                }
            }, TrickleDown.TrickleDown);
            _plot.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_touches.TryGetValue(e.pointerId, out Vector2 last))
                {
                    return;
                }

                Vector2 now = e.position;
                _touches[e.pointerId] = now;
                if (_touches.Count >= 2)
                {
                    float spread = Spread();
                    if (_pinchFrom > 1f && spread > 1f)
                    {
                        SetView(MapView.Instance.Focus, _pinchZoom * _pinchFrom / spread);
                    }

                    return;
                }

                if (!_dragged && (now - _dragFrom).magnitude > DragSlop)
                {
                    _dragged = true;
                    _plot.CapturePointer(e.pointerId);
                }

                if (_dragged)
                {
                    Pan(now - last);
                }
            }, TrickleDown.TrickleDown);
            _plot.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_touches.Remove(e.pointerId) && e.target == _plot && !_dragged)
                {
                    SetSheet(false);
                }

                _plot.ReleasePointer(e.pointerId);
            });
            _plot.RegisterCallback<PointerCancelEvent>(e => _touches.Remove(e.pointerId));
            _plot.RegisterCallback<PointerCaptureOutEvent>(e => _touches.Remove(e.pointerId));
            _plot.RegisterCallback<WheelEvent>(e =>
            {
                if (MapView.Instance != null && Mathf.Abs(e.delta.y) > 0.01f)
                {
                    float step = MapView.Instance.Look.zoomStep;
                    SetView(MapView.Instance.Focus, MapView.Instance.Zoom * (e.delta.y > 0f ? 1f + step : 1f - step));
                    e.StopPropagation();
                }
            });
        }

        private float Spread()
        {
            Vector2 a = Vector2.zero;
            Vector2 b = Vector2.zero;
            int i = 0;
            foreach (Vector2 p in _touches.Values)
            {
                if (i == 0)
                {
                    a = p;
                }
                else if (i == 1)
                {
                    b = p;
                }

                i++;
            }

            return (a - b).magnitude;
        }

        /// <summary>Moves the view so the ground under the finger follows it (scale measured from the current pose).</summary>
        private void Pan(Vector2 delta)
        {
            MapView v = MapView.Instance;
            Rect r = _plot.contentRect;
            if (v == null || r.width < 32f || r.height < 32f)
            {
                return;
            }

            System.Numerics.Vector3 t = v.Pose.Target;
            System.Numerics.Vector2 o = v.Pose.Project(t, v.Aspect);
            System.Numerics.Vector2 px = v.Pose.Project(t + System.Numerics.Vector3.UnitX, v.Aspect);
            System.Numerics.Vector2 pz = v.Pose.Project(t + System.Numerics.Vector3.UnitZ, v.Aspect);
            float perX = (px.X - o.X) * r.width;
            float perZ = (o.Y - pz.Y) * r.height;
            if (perX <= 0.01f || perZ <= 0.01f)
            {
                return;
            }

            SetView(new Vector2(v.Focus.x - (delta.x / perX), v.Focus.y + (delta.y / perZ)), v.Zoom);
        }

        /// <summary>
        /// Pans a tapped site that the open sheet covers (or nearly) to the middle of the strip of map above it. Runs
        /// after the sheet's layout so its real top edge is known.
        /// </summary>
        private void Reveal(int site)
        {
            if (_layer == null || site < 0 || site >= _layer.SiteAnchors.Length)
            {
                return;
            }

            Rect plot = _plot.worldBound;
            if (plot.height < 32f)
            {
                return;
            }

            float clear = Mathf.Clamp01((_pager.worldBound.yMin - plot.yMin) / plot.height);
            float y = _layer.SiteAnchors[site].Y;
            if (y > clear - RevealMargin)
            {
                Pan(new Vector2(0f, ((clear * 0.5f) - y) * plot.height));
            }
        }

        private void SetView(Vector2 focus, float zoom)
        {
            if (MapView.Instance == null)
            {
                return;
            }

            MapView.Instance.SetView(focus, zoom);
            _viewMoved = true;
        }

        /// <summary>
        /// The pager is a bottom sheet over the full-screen map (F-107): its tab row stays docked, a tab or a pin opens
        /// it, the open tab or a tap on open ground folds it.
        /// </summary>
        private void BindSheet()
        {
            Pager.PageShown += (pager, index) =>
            {
                if (pager != _pager)
                {
                    return;
                }

                bool fold = _sheetOpen && index == _sheetPage && !_openingSheet;
                _sheetPage = index;
                SetSheet(!fold);
            };
            SetSheet(false);
        }

        private void OpenSheet(string page)
        {
            _openingSheet = true;
            Pager.Show(_pager, page);
            _openingSheet = false;
            SetSheet(true);
        }

        private void SetSheet(bool open)
        {
            _sheetOpen = open;
            _pager.EnableInClassList("is-collapsed", !open);
        }

        private void BuildMarkers()
        {
            _sites.Clear();
            _markers.Clear();
            _estimates.Clear();
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                int index = i;
                VisualElement marker = Marker(d.Name, HazardSystem.Wild(d.Kind) ? "map-site--wild" : FactionClass[(int)d.Owner], out Label estimate);
                marker.RegisterCallback<ClickEvent>(_ =>
                {
                    if (_dragged)
                    {
                        return;
                    }

                    if (_longPressed)
                    {
                        _longPressed = false;
                        return;
                    }

                    _selected = index;
                    _reason.text = string.Empty;
                    OpenSheet("page-site");
                    _plot.schedule.Execute(() => Reveal(index)).ExecuteLater(RevealDelayMs);
                    Refresh();
                });

                // long-press quick action (doc 08 s4): send a two-person scout team straight from the pin
                marker.RegisterCallback<PointerDownEvent>(_ =>
                {
                    _pressAt = Time.realtimeSinceStartup;
                    _pressSite = index;
                });
                marker.RegisterCallback<PointerUpEvent>(_ =>
                {
                    bool held = !_dragged && _pressSite == index && _pressAt >= 0f && Time.realtimeSinceStartup - _pressAt >= 0.55f;
                    _pressAt = -1f;
                    if (held)
                    {
                        _longPressed = true;
                        _selected = index;
                        Run(Command.LaunchOp(index, OpKind.Scout, 2));
                        if (string.IsNullOrEmpty(_reason.text))
                        {
                            _reason.text = "Scouts out to " + WorldSystem.Sites[index].Name + ". Two of them.";
                        }
                    }
                });
                _sites.Add(marker);
                _markers.Add(marker);
                _estimates.Add(estimate);
            }

            _hubMarker = Marker("HUB S-17", "map-site--hub", out Label hubEstimate);
            hubEstimate.EnableInClassList("is-hidden", true);
            _sites.Add(_hubMarker);
        }

        /// <summary>A pin (touch target) with a tag: the site's name and, below it, the AI's defense estimate.</summary>
        private static VisualElement Marker(string name, string kindClass, out Label estimate)
        {
            var marker = new VisualElement();
            marker.AddToClassList("map-site");
            marker.AddToClassList(kindClass);
            marker.style.left = Length.Percent(-100f);
            var mark = new VisualElement();
            mark.AddToClassList("map-site__mark");
            marker.Add(mark);
            var tag = new VisualElement();
            tag.AddToClassList("map-site__tag");
            tag.pickingMode = PickingMode.Ignore;
            tag.Add(Kit.Label(name, "map-site__name"));
            estimate = Kit.Label(string.Empty, "map-site__est");
            tag.Add(estimate);
            marker.Add(tag);
            return marker;
        }

        /// <summary>Renders the 3D sector at the plot's pixel size (only when the size or the hour's light changed).</summary>
        private void RenderMap(bool force)
        {
            MapView view = MapView.Instance;
            Rect r = _plot.contentRect;
            if (view == null || float.IsNaN(r.width) || r.width < 32f || r.height < 32f || _plot.panel == null)
            {
                return;
            }

            float scale = Screen.height / Mathf.Max(1f, _plot.panel.visualTree.layout.height);
            if (view.Render(Mathf.RoundToInt(r.width * scale), Mathf.RoundToInt(r.height * scale), force) || force)
            {
                _render.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(view.Texture));
                _render.MarkDirtyRepaint();
                RefreshLayer(_host.Sim.State);
            }
        }

        /// <summary>Per frame while open: the haze drifts, estimates flicker by corruption, the light follows the hour.</summary>
        private void TickMap(float dt)
        {
            GameState s = _host.Sim.State;
            bool reduced = _host.Settings.ReducedMotion;
            _tickClock += dt;
            if (_tickClock < 0.12f)
            {
                return;
            }

            _tickClock = 0f;
            _frame++;
            if (_frame % 40 == 0)
            {
                RenderMap(false);
            }

            if (!reduced && s.FalloutSite >= 0 && _layer != null && MapView.Instance != null)
            {
                // only the haze moves; the rest of the layer waits for a state change
                _hazeTime += 0.12f;
                _layer.Redrift(s, MapView.Instance.Pose, MapView.Instance.Aspect, BaseView.Seed, _hazeTime);
                _overlay.MarkDirtyRepaint();
            }

            if (!reduced && GlitchWeight(s) > 0f)
            {
                RefreshEstimates(s);
            }
        }

        private float GlitchWeight(GameState s)
        {
            if (_host.Settings.ReducedMotion)
            {
                return 0f;
            }

            return GlitchText.BandWeight((int)CorruptionSystem.Band(_host.Sim.Config, s.CorruptionMilli)) * _host.Settings.Effects;
        }

        /// <summary>Rebuilds the illustrated layer and moves the pins onto the projected landmarks, labels laid out to miss each other.</summary>
        private void RefreshLayer(GameState s)
        {
            MapView view = MapView.Instance;
            if (view == null || view.Texture == null)
            {
                return;
            }

            _layer = SectorOverlay.Build(s, view.Pose, view.Aspect, BaseView.Seed, _hazeTime);
            _overlay.MarkDirtyRepaint();

            Rect r = _plot.contentRect;
            float font = _markers.Count > 0 && _markers[0].Q<Label>().resolvedStyle.fontSize > 0f ? _markers[0].Q<Label>().resolvedStyle.fontSize : 20f;
            int n = _markers.Count;
            var pins = new System.Numerics.Vector2[n + 1];
            var widths = new float[n + 1];
            for (int i = 0; i < n; i++)
            {
                pins[i] = _layer.SiteAnchors[i];
                widths[i] = ((Label(i).Length * 0.62f * font) + 24f) / r.width;
            }

            pins[n] = _layer.HubAnchor;
            widths[n] = ((8 * 0.62f * font) + 24f) / r.width;
            float tagHeight = (font * 2.9f) / r.height;
            float pinSize = 28f / r.height;
            int[] sides = SectorOverlay.PlaceLabels(pins, widths, tagHeight, pinSize);
            var final = new int[n + 1];
            for (int i = 0; i <= n; i++)
            {
                VisualElement m = i < n ? _markers[i] : _hubMarker;
                m.style.left = Length.Percent(pins[i].X * 100f);
                m.style.top = Length.Percent(pins[i].Y * 100f);
                // a tag that would run off the plot edge flips to the pin's other side
                bool left = sides[i] % 2 == 1;
                if (!left && pins[i].X + widths[i] > 1f)
                {
                    left = true;
                }
                else if (left && pins[i].X - widths[i] < 0f)
                {
                    left = false;
                }

                m.EnableInClassList("map-site--left", left);
                m.EnableInClassList("map-site--low", sides[i] >= 2);
                bool inView = pins[i].X >= 0f && pins[i].X <= 1f && pins[i].Y >= 0f && pins[i].Y <= 1f;
                m.style.display = inView ? DisplayStyle.Flex : DisplayStyle.None;
                final[i] = (left ? 1 : 0) + (sides[i] >= 2 ? 2 : 0);
            }

            HideCrowdedTags(pins, widths, final, tagHeight, pinSize);
        }

        /// <summary>
        /// Where the map is too crowded for every tag (zoomed out, large text), the Hub and then the selected site keep
        /// theirs and the rest, top to bottom, drop any tag that would still overlap one already shown. The pin stays
        /// tappable; zooming in brings the tag back.
        /// </summary>
        private void HideCrowdedTags(System.Numerics.Vector2[] pins, float[] widths, int[] sides, float height, float pin)
        {
            int n = _markers.Count;
            var order = new List<int> { n };
            if (_selected >= 0 && _selected < n)
            {
                order.Add(_selected);
            }

            var rest = new List<int>();
            for (int i = 0; i < n; i++)
            {
                if (i != _selected)
                {
                    rest.Add(i);
                }
            }

            rest.Sort((a, b) => pins[a].Y.CompareTo(pins[b].Y) != 0 ? pins[a].Y.CompareTo(pins[b].Y) : a.CompareTo(b));
            order.AddRange(rest);
            var shown = new List<System.Numerics.Vector4>();
            foreach (int i in order)
            {
                if (pins[i].X < 0f || pins[i].X > 1f || pins[i].Y < 0f || pins[i].Y > 1f)
                {
                    continue;
                }

                System.Numerics.Vector4 box = SectorOverlay.LabelBox(pins[i], widths[i], height, pin, sides[i]);
                bool clear = true;
                foreach (System.Numerics.Vector4 o in shown)
                {
                    clear &= box.Z <= o.X || box.X >= o.Z || box.W <= o.Y || box.Y >= o.W;
                }

                VisualElement m = i < n ? _markers[i] : _hubMarker;
                m.EnableInClassList("map-site--bare", !clear);
                if (clear)
                {
                    shown.Add(box);
                }
            }
        }

        /// <summary>The pin label for a site (state suffixes included), as shown.</summary>
        private string Label(int i)
        {
            SiteState st = _host.Sim.State.Sites[i];
            return WorldSystem.Sites[i].Name + (HazardSystem.Covered(_host.Sim.State, i) ? " // FALLOUT" : st.Outpost ? " // OUTPOST" : st.Cleared ? " // CLEARED" : string.Empty);
        }

        /// <summary>Under each pin: the scouted defense, or the AI's estimate, which flickers as the core corrupts.</summary>
        private void RefreshEstimates(GameState s)
        {
            SimConfig c = _host.Sim.Config;
            float weight = GlitchWeight(s);
            for (int i = 0; i < _estimates.Count; i++)
            {
                SiteState st = s.Sites[i];
                Label est = _estimates[i];
                est.EnableInClassList("is-hidden", st.Outpost);
                est.EnableInClassList("is-known", st.Scouted);
                string text = (st.Scouted ? "DEFENSE " : "~") + WorldSystem.EstimatedDefense(s, c, i);
                est.text = st.Scouted ? text : GlitchText.Flicker(text, weight, _frame + (i * 7));
            }
        }

        /// <summary>
        /// Draws the illustrated layer with raw UI meshes (fans for the blobs, quads for lines; works on every
        /// UI Toolkit version): shapes first, then a leader line from each pin to its landmark.
        /// </summary>
        private void DrawOverlay(MeshGenerationContext ctx)
        {
            if (_layer == null)
            {
                return;
            }

            Rect r = _overlay.contentRect;
            var pts = new List<Vector2>();
            foreach (OverlayShape shape in _layer.Shapes)
            {
                if (shape.Points.Count < 2)
                {
                    continue;
                }

                pts.Clear();
                foreach (System.Numerics.Vector2 q in shape.Points)
                {
                    pts.Add(new Vector2(q.X * r.width, q.Y * r.height));
                }

                Color32 color = new Color(shape.Color.X, shape.Color.Y, shape.Color.Z, shape.Color.W);
                if (shape.Fill)
                {
                    Fan(ctx, pts, color);
                }
                else
                {
                    Polyline(ctx, pts, Mathf.Max(1f, shape.Width * r.height), color);
                }
            }

            // leader lines: each pin floats over its landmark; the line shows where it stands
            for (int i = 0; i < _layer.SiteAnchors.Length; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                Vector3 tint = HazardSystem.Wild(d.Kind) ? new Vector3(0.62f, 0.85f, 0.8f) : ToUnity(SectorOverlay.FactionTint[(int)d.Owner]);
                pts.Clear();
                pts.Add(new Vector2(_layer.SiteAnchors[i].X * r.width, (_layer.SiteAnchors[i].Y * r.height) + 12f));
                pts.Add(new Vector2(_layer.GroundAnchors[i].X * r.width, _layer.GroundAnchors[i].Y * r.height));
                Polyline(ctx, pts, 2f, new Color(tint.x, tint.y, tint.z, 0.7f));
            }
        }

        /// <summary>A filled blob as a triangle fan around its centroid (the overlay's blobs are star-shaped).</summary>
        private static void Fan(MeshGenerationContext ctx, List<Vector2> pts, Color32 color)
        {
            Vector2 c = Vector2.zero;
            foreach (Vector2 q in pts)
            {
                c += q;
            }

            c /= pts.Count;

            // UI meshes want clockwise triangles (y down): follow the outline's own winding
            float area = 0f;
            for (int k = 0; k < pts.Count; k++)
            {
                Vector2 a = pts[k];
                Vector2 b = pts[(k + 1) % pts.Count];
                area += (a.x * b.y) - (b.x * a.y);
            }

            bool clockwise = area > 0f;
            MeshWriteData mesh = ctx.Allocate(pts.Count + 1, pts.Count * 3);
            mesh.SetNextVertex(new Vertex { position = new Vector3(c.x, c.y, Vertex.nearZ), tint = color });
            foreach (Vector2 q in pts)
            {
                mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, Vertex.nearZ), tint = color });
            }

            for (int k = 0; k < pts.Count; k++)
            {
                ushort here = (ushort)(1 + k);
                ushort next = (ushort)(1 + ((k + 1) % pts.Count));
                mesh.SetNextIndex(0);
                mesh.SetNextIndex(clockwise ? here : next);
                mesh.SetNextIndex(clockwise ? next : here);
            }
        }

        /// <summary>An open polyline as one quad per segment.</summary>
        private static void Polyline(MeshGenerationContext ctx, List<Vector2> pts, float width, Color32 color)
        {
            int segments = pts.Count - 1;
            MeshWriteData mesh = ctx.Allocate(segments * 4, segments * 6);
            float h = width * 0.5f;
            for (int k = 0; k < segments; k++)
            {
                Vector2 a = pts[k];
                Vector2 b = pts[k + 1];
                Vector2 dir = b - a;
                Vector2 n = dir.sqrMagnitude > 1e-6f ? new Vector2(-dir.y, dir.x).normalized * h : new Vector2(0f, h);
                ushort i0 = (ushort)(k * 4);
                mesh.SetNextVertex(new Vertex { position = new Vector3(a.x + n.x, a.y + n.y, Vertex.nearZ), tint = color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(b.x + n.x, b.y + n.y, Vertex.nearZ), tint = color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(b.x - n.x, b.y - n.y, Vertex.nearZ), tint = color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(a.x - n.x, a.y - n.y, Vertex.nearZ), tint = color });
                // clockwise in y-down UI space
                mesh.SetNextIndex(i0);
                mesh.SetNextIndex((ushort)(i0 + 2));
                mesh.SetNextIndex((ushort)(i0 + 1));
                mesh.SetNextIndex(i0);
                mesh.SetNextIndex((ushort)(i0 + 3));
                mesh.SetNextIndex((ushort)(i0 + 2));
            }
        }

        private static Vector3 ToUnity(System.Numerics.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        private void Pick(OpKind kind)
        {
            _kind = kind;
            if (kind == OpKind.Sabotage)
            {
                _squad = SimMath.Clamp(_squad, 1, _host.Sim.Config.World.SabotageMaxSquad);
            }

            Refresh();
        }

        private void Step(int d)
        {
            if (_kind == OpKind.Hack)
            {
                _compute = SimMath.Clamp(_compute + (d * 10), 10, _host.Sim.Config.Compute.Cap);
            }
            else
            {
                _squad = SimMath.Clamp(_squad + d, 1, _kind == OpKind.Sabotage ? _host.Sim.Config.World.SabotageMaxSquad : 20);
            }

            Refresh();
        }

        /// <summary>Ally with a faction, or end the alliance with two taps (it costs heat).</summary>
        private void Ally(Faction faction)
        {
            if (!DiplomacySystem.Allied(_host.Sim.State, faction))
            {
                Run(Command.ProposeAlliance(faction));
                return;
            }

            long now = System.Environment.TickCount;
            if (_allyArmedAt < 0 || now - _allyArmedAt > 4000)
            {
                _allyArmedAt = now;
                _reason.text = "Tap again to end the alliance. They will remember it.";
                Refresh();
                return;
            }

            _allyArmedAt = -1;
            Run(Command.EndAlliance());
        }

        private void Run(Command command)
        {
            CommandResult r = _host.Execute(command);
            _reason.text = r.Accepted ? string.Empty : Reason(r.Reason);
            Refresh();
        }

        private static string Reason(RejectReason reason)
        {
            switch (reason)
            {
                case RejectReason.OpsBusy: return "Every team is already out. Wait for one to come back.";
                case RejectReason.SiteCooldown: return "Nothing left there to take. Not yet.";
                case RejectReason.NotClaimable: return "Only ground we have just beaten can be taken: cleared ruins, or a faction outpost after a won raid.";
                case RejectReason.NotEnoughFuel: return "Not enough fuel.";
                case RejectReason.SpyActive: return "We already have someone in that camp.";
                case RejectReason.PactActive: return "One ceasefire at a time, and not so soon after the last.";
                case RejectReason.NoSpy: return "Nobody of ours is in that camp.";
                case RejectReason.NotTrusted: return "They only stand with a Hub that has given them no reason to hate it. Cool their heat first.";
                case RejectReason.AllianceActive: return "One alliance at a time.";
                default: return Texts.Reason(reason);
            }
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;

            _ui.Q<Label>("map-ops-count").text = "OPS " + s.Ops.Count + "/" + WorldSystem.MaxOps(s, c) + " // FUEL " + Fmt.Num(s.Fuel);
            Pager.Badge(_ui.Q("map-pager"), "page-ops", s.Ops.Count);
            _ui.Q("map-ops-empty").EnableInClassList("is-hidden", s.Ops.Count > 0);
            for (int f = 0; f < WorldSystem.FactionCount; f++)
            {
                HeatLevel level = WorldSystem.Level(s.Heat[f]);
                bool crippled = s.SabotageFaction == f && s.Tick < s.SabotageUntilTick;
                _ui.Q<Label>("heat-" + f + "-level").text = level.ToString().ToUpperInvariant() + " " + WorldSystem.Percent(s.Heat[f]) + (crippled ? " // CRIPPLED" : string.Empty);
                Kit.SetMeter(_ui.Q("heat-" + f + "-meter"), s.Heat[f] / 100_000f);
                _ui.Q("heat-" + f).EnableInClassList("is-hot", level >= HeatLevel.Hunted);

                // adaptive enemies (SPEC-027): what they learned about us, and how dug in their sites are
                string learned = string.Empty;
                foreach (Posture p in new[] { Posture.Turtle, Posture.Dark, Posture.Evacuate })
                {
                    int lv = AdaptSystem.Learned(s, (Faction)f, p);
                    learned += lv > 0 ? (learned.Length > 0 ? " " : "READ OUR ") + Fmt.PostureName(p) + " " + lv : string.Empty;
                }

                int fort = s.Fortified[f];
                string adapt = learned + (fort > 0 ? (learned.Length > 0 ? " // " : string.Empty) + "DUG IN " + fort : string.Empty);
                if (LuckSystem.Regrouping(s, (Faction)f))
                {
                    // an opportunity window (SPEC-028): their sites are thin right now
                    adapt = "REGROUPING " + Fmt.Countdown(_host.SecondsUntilTick(s.RegroupUntilTick)) + (adapt.Length > 0 ? " // " + adapt : string.Empty);
                }
                _ui.Q<Label>("adapt-" + f).text = adapt;
                _ui.Q("adapt-" + f).EnableInClassList("is-hidden", adapt.Length == 0);

                // spies (SPEC-019): loyalty is hidden; only a scout's cross-check or a backfire reveals it
                bool spy = IntelSystem.Has(s, (Faction)f);
                Label spyState = _ui.Q<Label>("spy-" + f + "-state");
                spyState.text = spy ? "AGENT INSIDE" : "NO AGENT";
                spyState.EnableInClassList("is-on", spy);
                _ui.Q<Label>("spy-" + f + "-a-label").text = spy ? "FRAME A RIVAL" : "SEND SPY // " + Fmt.Num(c.Intel.SpyEnergy) + " ENERGY";
                _ui.Q("spy-" + f + "-a").EnableInClassList("is-disabled", !spy && s.Energy < c.Intel.SpyEnergy);
                _ui.Q("spy-" + f + "-b").EnableInClassList("is-hidden", !spy);

                // ceasefire (SPEC-023): one at a time, priced by heat, none with a Marked faction
                bool peace = DiplomacySystem.Ceasefire(s, (Faction)f);
                bool talks = DiplomacySystem.Price(s, c, (Faction)f, out int pe, out int pf);
                bool cooling = s.Tick < s.CeasefireReadyTick || (s.CeasefireFaction >= 0 && !peace);
                _ui.Q("pact-" + f).EnableInClassList("is-on", peace);
                _ui.Q<Label>("pact-" + f + "-label").text = peace ? "PACT // " + Fmt.Countdown(_host.SecondsUntilTick(s.CeasefireUntilTick))
                    : !talks ? "WILL NOT TALK" : "CEASEFIRE // " + Fmt.Num(pe) + " ENERGY\n+ " + Fmt.Num(pf) + " FUEL";
                _ui.Q("pact-" + f).EnableInClassList("is-disabled", !peace && (!talks || cooling || s.CeasefireFaction >= 0 || s.Energy < pe || s.Fuel < pf));

                // alliance (SPEC-025): only with a Cold faction; their fighters man the wall for a daily share
                var d = c.Diplomacy;
                bool allied = DiplomacySystem.Allied(s, (Faction)f);
                bool armed = allied && _allyArmedAt >= 0 && System.Environment.TickCount - _allyArmedAt <= 4000;
                VisualElement ally = _ui.Q("ally-" + f);
                ally.EnableInClassList("is-on", allied);
                ally.EnableInClassList("is-armed", armed);
                _ui.Q("heat-" + f).EnableInClassList("is-allied", allied);
                _ui.Q<Label>("ally-" + f + "-label").text = armed ? "CONFIRM // END" : allied ? "ALLIED // +" + DiplomacySystem.AllyDefense(s, c) + " DEF"
                    : level != HeatLevel.Cold ? "ALLY // NEEDS COLD" : "ALLY // " + Fmt.Num(d.AllianceEnergy) + " ENERGY\n+ " + Fmt.Num(d.AllianceFuel) + " FUEL";
                ally.EnableInClassList("is-disabled", !allied && (level != HeatLevel.Cold || s.AllyFaction >= 0 || s.Energy < d.AllianceEnergy || s.Fuel < d.AllianceFuel));
            }

            var busy = new HashSet<int>();
            foreach (Operation op in s.Ops)
            {
                busy.Add(op.Site);
            }

            for (int i = 0; i < _markers.Count; i++)
            {
                SiteState st = s.Sites[i];
                _markers[i].EnableInClassList("is-selected", i == _selected);
                _markers[i].EnableInClassList("map-site--outpost", st.Outpost);
                _markers[i].EnableInClassList("map-site--cooldown", s.Tick < st.CooldownUntilTick);
                _markers[i].EnableInClassList("map-site--op", busy.Contains(i));
                _markers[i].EnableInClassList("map-site--fallout", HazardSystem.Covered(s, i));
                _markers[i].Q<Label>(className: "map-site__name").text = Label(i);
            }

            RefreshEstimates(s);
            RefreshLayer(s);

            // world event and the fallout front (SPEC-032) share the banner
            bool world = s.WorldEvent != WorldEventKind.None && s.Tick < s.WorldEventUntilTick;
            bool fallout = s.FalloutSite >= 0;
            _ui.Q("map-event").EnableInClassList("is-hidden", !world && !fallout);
            string banner = world ? LivingTexts.EventName(s.WorldEvent) + " // " + LivingTexts.EventEffect(s.WorldEvent, c).ToUpperInvariant() : string.Empty;
            if (fallout)
            {
                banner += (world ? "\n" : string.Empty) + "FALLOUT OVER " + WorldSystem.Sites[s.FalloutSite].Name + " // SHIFTS IN " + Fmt.Countdown(_host.SecondsUntilTick(s.NextFalloutTick));
            }

            _ui.Q<Label>("map-event-text").text = banner;

            RefreshSheet(s, c);
            RefreshTrade(s, c);
            RefreshOps();
        }

        private void RefreshTrade(GameState s, SimConfig c)
        {
            Faction owner = WorldSystem.Sites[_selected].Owner;
            HeatLevel level = WorldSystem.Level(s.Heat[(int)owner]);
            bool wild = HazardSystem.Wild(WorldSystem.Sites[_selected].Kind);
            bool hostile = level == HeatLevel.Marked || wild;
            int left = System.Math.Max(0, c.Living.TradesPerDay - s.TradesToday[(int)owner]);
            _ui.Q<Label>("trade-with").text = (wild ? "NO OWNER" : Names.Faction(owner)) + " // " + WorldSystem.Sites[_selected].Name;
            _ui.Q<Label>("trade-left").text = wild ? "NOBODY OUT THERE TO TRADE WITH" : hostile ? Names.Faction(owner) + " WILL NOT TRADE" : left + "/" + c.Living.TradesPerDay + " TODAY // " + level.ToString().ToUpperInvariant() + " // BLUEPRINTS " + s.Blueprints + "/" + c.Living.BlueprintMax;
            for (int g = 0; g <= (int)TradeGood.Blueprints; g++)
            {
                var good = (TradeGood)g;
                int price = LivingSystem.Price(s, c, owner, good);
                bool payFuel = good == TradeGood.EnergyCells;
                _ui.Q<Label>("trade-" + g + "-get").text = "+" + Fmt.Num(LivingSystem.Lot(c, good)) + " " + LivingTexts.Good(good);
                _ui.Q<Label>("trade-" + g + "-pay").text = hostile ? "-" : Fmt.Num(price) + (payFuel ? " FUEL" : " ENERGY");
                bool afford = price >= 0 && (payFuel ? s.Fuel >= price : s.Energy >= price);
                bool full = good == TradeGood.Blueprints && s.Blueprints >= c.Living.BlueprintMax;
                _ui.Q("trade-" + g).EnableInClassList("is-disabled", hostile || left == 0 || !afford || full);
            }
        }

        private void RefreshSheet(GameState s, SimConfig c)
        {
            SiteDef d = WorldSystem.Sites[_selected];
            SiteState st = s.Sites[_selected];
            string[] kinds = { "CONVOY", "OUTPOST", "DATA CENTER", "RUINS", "RADIATION ZONE", "PLAGUE ZONE", "MACHINE GRAVEYARD" };
            bool wild = HazardSystem.Wild(d.Kind);
            _ui.Q<Label>("site-name").text = d.Name;
            _ui.Q<Label>("site-owner").text = (wild ? "NO OWNER" : Names.Faction(d.Owner)) + " // " + kinds[(int)d.Kind] + (HazardSystem.Covered(s, _selected) ? " // UNDER FALLOUT" : string.Empty);
            _ui.Q<Label>("site-travel").text = d.TravelHours + " H";
            int estimate = WorldSystem.EstimatedDefense(s, c, _selected);
            _ui.Q<Label>("site-def-label").text = st.Scouted ? "DEFENSE (SCOUTED)" : !wild && IntelSystem.Has(s, d.Owner) ? "DEFENSE (AGENT)" : "DEFENSE (AI EST)";
            _ui.Q<Label>("site-def").text = (st.Scouted ? string.Empty : "~") + estimate + (d.Cyber > 0 ? "  CYBER " + d.Cyber : string.Empty);
            // one good per line: the narrow column must never split an amount from its name
            _ui.Q<Label>("site-loot").text = d.Energy + " ENERGY\n" + d.Fuel + " FUEL\n" + d.Compute + " COMPUTE" + (d.CleanData > 0 ? "\n+ CLEAN DATA" : string.Empty) + Bonus(d.Kind, c)
                + (d.Kind == SiteKind.DataCenter ? "\n+ FRAGMENT " + c.Modules.FragmentPctDataCenter + "%" : d.Kind == SiteKind.Ruins ? "\n+ FRAGMENT " + c.Modules.FragmentPctRuins + "%" : string.Empty);

            if ((_kind == OpKind.Hack && d.Cyber == 0) || (_kind == OpKind.Sabotage && wild))
            {
                _kind = OpKind.Raid;
            }

            _ui.Q("op-scout").EnableInClassList("is-selected", _kind == OpKind.Scout);
            _ui.Q("op-raid").EnableInClassList("is-selected", _kind == OpKind.Raid);
            _ui.Q("op-hack").EnableInClassList("is-selected", _kind == OpKind.Hack);
            _ui.Q("op-hack").EnableInClassList("is-disabled", d.Cyber == 0);
            _ui.Q("op-sabotage").EnableInClassList("is-selected", _kind == OpKind.Sabotage);
            _ui.Q("op-sabotage").EnableInClassList("is-disabled", wild);

            bool hack = _kind == OpKind.Hack;
            _ui.Q<Label>("op-amount").text = (hack ? _compute : _squad).ToString(System.Globalization.CultureInfo.InvariantCulture);
            _ui.Q<Label>("op-amount-unit").text = hack ? "COMPUTE" : "PEOPLE";
            int odds = WorldSystem.Odds(s, c, d, _kind, _squad, _compute, estimate);
            Label oddsLabel = _ui.Q<Label>("op-odds");
            oddsLabel.text = (st.Scouted ? "ODDS " : "AI ODDS ") + odds + "%";
            oddsLabel.EnableInClassList("t-amber", odds < 60);
            int back = hack ? 1 : 2 * d.TravelHours;
            _ui.Q<Label>("op-cost").text = (hack ? "COMPUTE " + _compute : "FUEL " + WorldSystem.FuelCost(s, c, _selected, _kind)) + " // BACK IN " + back + " H"
                + (_kind == OpKind.Sabotage ? " // " + Names.Faction(d.Owner) + " -" + c.World.SabotageStrengthPct + "% FOR " + c.World.SabotageHours + " H" : string.Empty)
                + (hack ? string.Empty : Risk(s, c, _selected));
            string[] verbs = { "SEND SCOUTS", "LAUNCH STRIKE", "START HACK", "SEND SABOTEURS" };
            Kit.SetButtonText(_ui.Q("op-launch"), verbs[(int)_kind]);
            bool cooling = _kind != OpKind.Scout && s.Tick < st.CooldownUntilTick;
            _ui.Q("op-launch").EnableInClassList("is-disabled", st.Outpost || cooling || s.Ops.Count >= WorldSystem.MaxOps(s, c));

            bool seize = d.Kind == SiteKind.Outpost;
            bool claimable = (d.Kind == SiteKind.Ruins || seize) && st.Cleared && !st.Outpost;
            _ui.Q("site-claim").EnableInClassList("is-hidden", !claimable);
            Kit.SetButtonText(_ui.Q("site-claim"), seize ? "SEIZE AND HOLD // " + c.World.SeizeEnergy + " ENERGY" : "CLAIM // " + c.World.OutpostClaimEnergy + " ENERGY");
        }

        /// <summary>What a hazard zone pays besides loot (SPEC-032).</summary>
        private static string Bonus(SiteKind kind, SimConfig c)
        {
            switch (kind)
            {
                case SiteKind.Plague: return "\n+ " + c.Hazards.PlaguePeople + " SURVIVORS";
                case SiteKind.Graveyard: return "\n+ PARTS (" + c.Hazards.GraveyardRepairPoints + " REPAIRS)";
                default: return string.Empty;
            }
        }

        /// <summary>The hazard risk on the op line; scouting first halves it (SPEC-032 rule 5).</summary>
        private static string Risk(GameState s, SimConfig c, int site)
        {
            int sick = HazardSystem.SickPct(s, c, site);
            int infection = HazardSystem.InfectionPct(s, c, site);
            string risk = (sick > 0 ? " // SICKNESS " + sick + "%" : string.Empty) + (infection > 0 ? " // INFECTION " + infection + "%" : string.Empty);
            if (WorldSystem.Sites[site].Kind == SiteKind.Graveyard)
            {
                risk += " // DRONE NESTS";
            }

            return risk.Length > 0 && s.Sites[site].Scouted ? risk + " (SCOUTED: REDUCED)" : risk;
        }

        /// <summary>Rebuilds the ops list only when the set of ops changes (buttons must survive between frames); timers update in place.</summary>
        private void RefreshOps()
        {
            GameState s = _host.Sim.State;
            string signature = string.Empty;
            foreach (Operation op in s.Ops)
            {
                signature += op.Id + (op.ByAi ? "a," : ",");
            }

            if (signature != _opsSignature)
            {
                _opsSignature = signature;
                _ops.Clear();
                _opTimes.Clear();
                string[] kinds = { "SCOUT", "STRIKE", "HACK", "SABOTAGE" };
                foreach (Operation op in s.Ops)
                {
                    var row = new VisualElement();
                    row.AddToClassList("row");
                    row.AddToClassList("map-op");
                    string who = op.Kind == OpKind.Hack ? op.Compute + " COMPUTE" : op.Squad + " SENT";
                    row.Add(Kit.Label((op.ByAi ? "AI // " : string.Empty) + kinds[(int)op.Kind] + " // " + WorldSystem.Sites[op.Site].Name + " // " + who, "map-op__text", "grow"));
                    Label time = Kit.Label(string.Empty, "map-op__time");
                    row.Add(time);
                    _opTimes.Add(time);
                    if (op.ByAi)
                    {
                        // the AI acted without orders (SPEC-030): the handler can call it back
                        int id = op.Id;
                        row.EnableInClassList("map-op--ai", true);
                        row.Add(Kit.Button("RECALL", () => Run(Command.RecallOp(id)), "ds-btn--ghost", "map-op__recall"));
                    }

                    _ops.Add(row);
                }
            }

            for (int i = 0; i < _opTimes.Count && i < s.Ops.Count; i++)
            {
                _opTimes[i].text = Fmt.Countdown(_host.SecondsUntilTick(s.Ops[i].ReturnTick));
            }
        }
    }
}
