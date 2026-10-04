using System.Collections.Generic;
using Deadswitch.Art.Models;
using Deadswitch.Art.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The 3D ground of the sector map (SPEC-033): built once from <see cref="SectorScene"/> on its own layer, far
    /// below the compound, and rendered by a fixed recon camera into a texture only when the map screen asks (on
    /// open, on resize, and as the hour's light changes). Same sun, sky and post as the base; thinner fog and a wider
    /// shadow reach for the far camera. The URP call is injected by the Rendering assembly (<see cref="RenderCamera"/>).
    /// </summary>
    public sealed class MapView : MonoBehaviour
    {
        /// <summary>Layer of the map world: the drone camera never draws it, the map camera draws nothing else.</summary>
        public const int Layer = 30;

        /// <summary>Where the map world sits (far from the compound's lights and the drone camera's far plane).</summary>
        public static readonly Vector3 Origin = new Vector3(0f, -2000f, 0f);

        private readonly List<(Light light, float baseIntensity)> _lights = new List<(Light, float)>();
        private Camera _cam;
        private Transform _world;
        private BaseLook _look;
        private float _renderedHour = -1f;

        public static MapView Instance { get; private set; }

        /// <summary>
        /// Renders a camera into a target with the map's render settings (post on, far shadows). The Rendering assembly
        /// replaces this with a URP render request; the fallback is a plain Camera.Render.
        /// </summary>
        public static System.Action<Camera, RenderTexture, float> RenderCamera = (cam, rt, shadowDistance) =>
        {
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
        };

        /// <summary>The last picture (null until the first render).</summary>
        public RenderTexture Texture { get; private set; }

        /// <summary>The camera pose of the last picture; the overlay and markers project with it.</summary>
        public CameraPose Pose { get; private set; }

        public float Aspect { get; private set; } = 1f;

        /// <summary>Creates the map view on first use (the map world is built only when the map is first opened).</summary>
        public static MapView Ensure()
        {
            if (Instance == null)
            {
                Instance = new GameObject("Sector Map").AddComponent<MapView>();
            }

            return Instance;
        }

        /// <summary>
        /// Renders the map at this pixel size if the size changed or the light moved on since the last picture.
        /// Returns true when <see cref="Texture"/> changed.
        /// </summary>
        public bool Render(int width, int height, bool force = false)
        {
            width = Mathf.Clamp(width, 64, 2048);
            height = Mathf.Clamp(height, 64, 2048);
            float hour = BaseView.Instance != null ? BaseView.Instance.Lighting.hour : 12f;
            bool resized = Texture == null || Texture.width != width || Texture.height != height;
            if (!force && !resized && Mathf.Abs(Mathf.DeltaAngle(hour * 15f, _renderedHour * 15f)) < 1.5f)
            {
                return false;
            }

            if (resized)
            {
                if (Texture != null)
                {
                    Texture.Release();
                    Destroy(Texture);
                }

                Texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "Sector Map", antiAliasing = 1 };
            }

            Aspect = width / (float)height;
            Pose = SectorScene.Camera(Aspect);
            _cam.fieldOfView = Pose.Fov;
            _cam.aspect = Aspect;
            _cam.transform.position = Origin + ArtBridge.V(Pose.Position);
            _cam.transform.LookAt(Origin + ArtBridge.V(Pose.Target));
            _cam.backgroundColor = RenderSettings.fogColor;

            // the same hour's light as the compound, with the beacons scaled like its lamps
            LightKey k = BaseView.Instance != null ? BaseView.Instance.Lighting : _look.At(12f);
            foreach ((Light light, float baseIntensity) in _lights)
            {
                light.intensity = baseIntensity * k.pointScale * _look.unityPointScale;
            }

            float fog = RenderSettings.fogDensity;
            RenderSettings.fogDensity = fog * _look.map.fogScale;
            try
            {
                RenderCamera(_cam, Texture, _look.map.shadowDistance);
            }
            finally
            {
                RenderSettings.fogDensity = fog;
            }

            _renderedHour = hour;
            return true;
        }

        private void Awake()
        {
            Instance = this;
            _look = BaseLook.Load();
            _world = new GameObject("Map World").transform;
            _world.SetParent(transform, false);
            _world.position = Origin;
            foreach (SectorObject o in SectorScene.Build(BaseView.Seed))
            {
                Spawn(o);
            }

            _cam = new GameObject("Map Camera").AddComponent<Camera>();
            _cam.transform.SetParent(transform, false);
            _cam.enabled = false;
            _cam.allowHDR = true;
            _cam.nearClipPlane = 1f;
            _cam.farClipPlane = 700f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.cullingMask = 1 << Layer;
        }

        private void OnDestroy()
        {
            if (Texture != null)
            {
                Texture.Release();
                Destroy(Texture);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Spawn(SectorObject o)
        {
            var root = new GameObject(o.Name);
            root.layer = Layer;
            root.transform.SetParent(_world, false);
            root.transform.localPosition = ArtBridge.V(o.Position);
            Mesh mesh = ArtBridge.ToMesh(o.Model.Static, o.Name, out Material[] mats);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = root.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
            foreach (LightSpec spec in o.Model.Lights)
            {
                var l = new GameObject("Beacon").AddComponent<Light>();
                l.gameObject.layer = Layer;
                l.transform.SetParent(root.transform, false);
                l.transform.localPosition = ArtBridge.V(spec.Position);
                l.type = LightType.Point;
                l.color = new Color(spec.Color.X, spec.Color.Y, spec.Color.Z);
                l.range = spec.Range * _look.rangeScale;
                l.shadows = LightShadows.None;
                l.cullingMask = 1 << Layer;
                _lights.Add((l, spec.Intensity));
            }
        }
    }
}
