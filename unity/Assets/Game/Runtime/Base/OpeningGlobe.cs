using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The opening's planet (SPEC-044), built far above the Hub where nothing else is drawn: a sphere with the
    /// generated surface map (continents, city lights, blackout regions, burn noise; made on a worker thread), an
    /// atmosphere halo and a field of stars. The film drives it: <see cref="Lights"/>, <see cref="Burn"/>,
    /// <see cref="Dark"/>, launches that arc across the sky and land as blooms. Presentation only.
    /// </summary>
    public sealed class OpeningGlobe : MonoBehaviour
    {
        /// <summary>Where the planet hangs (world space): far from the Hub, inside the drone camera's reach.</summary>
        public static readonly Vector3 Center = new Vector3(0f, 4000f, 0f);

        private const float Radius = 10f;
        private const int MapWidth = 1024;
        private const int MapHeight = 512;
        private const int MaxBlooms = 16;
        private static readonly int BloomsId = Shader.PropertyToID("_GlobeBlooms");

        private readonly Vector4[] _blooms = new Vector4[MaxBlooms];
        private readonly List<Launch> _launches = new List<Launch>();
        private Material _surface;
        private Material _halo;
        private Transform _planet;
        private static Task<Color32[]> _map;
        private static Texture2D _mapTexture;
        private int _nextBloom;

        /// <summary>City lights strength (0..1).</summary>
        public float Lights { get; set; } = 1f;

        /// <summary>How much of the land burns (0..1).</summary>
        public float Burn { get; set; }

        /// <summary>Blackout progress: the cities go out region by region (0..1).</summary>
        public float Dark { get; set; }

        /// <summary>Fires sunk to embers (0..1).</summary>
        public float Embers { get; set; }

        /// <summary>Raised when a warhead lands; the argument is how big it is (the big one whites out the frame).</summary>
        public event System.Action<float> Landed;

        public static OpeningGlobe Create(Transform parent)
        {
            var go = new GameObject("Opening Globe");
            go.transform.SetParent(parent, false);
            go.transform.position = Center;
            return go.AddComponent<OpeningGlobe>();
        }

        private void Awake()
        {
            for (int i = 0; i < MaxBlooms; i++)
            {
                _blooms[i] = new Vector4(0f, 1f, 0f, -1f);
            }

            var sunDir = new Vector4(0.78f, 0.22f, 0.58f, 0f);
            _surface = new Material(Resources.Load<Shader>("Shaders/DeadswitchGlobe")) { name = "DS Globe" };
            _surface.SetVector("_SunDir", sunDir);
            _halo = new Material(Resources.Load<Shader>("Shaders/DeadswitchGlobeHalo")) { name = "DS Globe Halo" };
            _halo.SetVector("_SunDir", sunDir);
            Mesh sphere = Sphere(96, 48);

            _planet = Part("Planet", sphere, _surface, Radius);
            // tilted like a real planet, turned so the night side faces the lens with a sunlit edge
            _planet.localRotation = Quaternion.Euler(-18f, 30f, 12f);
            Part("Halo", sphere, _halo, Radius * 1.06f);
            Part("Stars", Stars(900, 7), BattleFx.Additive, 1f);

            Prewarm();
        }

        /// <summary>
        /// Starts building the surface map off the main thread (pure math, once per session) so it is ready by the
        /// time the planet fades in.
        /// </summary>
        public static void Prewarm()
        {
            if (_map == null && _mapTexture == null)
            {
                _map = Task.Run(() => GlobeMap.Build(MapWidth, MapHeight, 2041));
            }
        }

        /// <summary>A random point of the surface on the side facing the lens (object space), for launch sites.</summary>
        public Vector3 FacingPoint(Vector3 cameraPosition, System.Random rng)
        {
            Vector3 toCamera = (cameraPosition - Center).normalized;
            for (int i = 0; i < 32; i++)
            {
                var d = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f).normalized;
                if (Vector3.Dot(d, toCamera) > 0.35f)
                {
                    return _planet.InverseTransformDirection(d);
                }
            }

            return _planet.InverseTransformDirection(toCamera);
        }

        /// <summary>Fires a warhead from one point of the surface to another (object space directions).</summary>
        public void Fire(Vector3 from, Vector3 to, float seconds, float size)
        {
            var line = new GameObject("Launch").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = BattleFx.Additive;
            line.useWorldSpace = true;
            line.positionCount = 24;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.01f), new Keyframe(1f, 0.09f));
            line.startColor = new Color(1f, 0.45f, 0.2f, 0f);
            line.endColor = new Color(1f, 0.9f, 0.7f, 1f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _launches.Add(new Launch { Line = line, From = from.normalized, To = to.normalized, Seconds = seconds, Size = size });
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_map != null && _map.IsCompleted)
            {
                if (_map.Status == TaskStatus.RanToCompletion)
                {
                    _mapTexture = new Texture2D(MapWidth, MapHeight, TextureFormat.RGBA32, false, true) { wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "DS Globe Map" };
                    _mapTexture.SetPixels32(_map.Result);
                    _mapTexture.Apply(false, true);
                }
                else
                {
                    Debug.LogError("OpeningGlobe: surface map failed: " + _map.Exception);
                }

                _map = null;
            }

            if (_mapTexture != null && _surface.GetTexture("_Map") != _mapTexture)
            {
                _surface.SetTexture("_Map", _mapTexture);
            }

            // a slow turn: the planet is alive
            _planet.Rotate(Vector3.up, -1.2f * dt, Space.Self);
            _surface.SetFloat("_Lights", Lights);
            _surface.SetFloat("_Burn", Burn);
            _surface.SetFloat("_Dark", Dark);
            _surface.SetFloat("_Embers", Embers);
            _halo.SetFloat("_Strength", Mathf.Lerp(0.6f, 0.35f, Dark));

            for (int i = 0; i < MaxBlooms; i++)
            {
                if (_blooms[i].w >= 0f)
                {
                    _blooms[i].w += dt;
                }
            }

            _surface.SetVectorArray(BloomsId, _blooms);
            TickLaunches(dt);
        }

        private void TickLaunches(float dt)
        {
            for (int i = _launches.Count - 1; i >= 0; i--)
            {
                Launch l = _launches[i];
                l.Age += dt;
                float t = Mathf.Clamp01(l.Age / l.Seconds);
                float tail = Mathf.Max(0f, t - 0.45f);
                for (int p = 0; p < l.Line.positionCount; p++)
                {
                    float u = Mathf.Lerp(tail, t, p / (float)(l.Line.positionCount - 1));
                    l.Line.SetPosition(p, ArcPoint(l, u));
                }

                if (t < 1f)
                {
                    continue;
                }

                _blooms[_nextBloom] = new Vector4(l.To.x, l.To.y, l.To.z, 0f);
                _nextBloom = (_nextBloom + 1) % MaxBlooms;
                Landed?.Invoke(l.Size);
                Destroy(l.Line.gameObject);
                _launches.RemoveAt(i);
            }
        }

        /// <summary>A point along a launch: the great circle between the two sites, lifted into a ballistic arc.</summary>
        private Vector3 ArcPoint(Launch l, float u)
        {
            Vector3 dir = Vector3.Slerp(l.From, l.To, u);
            float height = 1f + (0.35f * Mathf.Sin(u * Mathf.PI) * Vector3.Angle(l.From, l.To) / 90f);
            return _planet.TransformPoint(dir * height);
        }

        private Transform Part(string name, Mesh mesh, Material material, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        private static Mesh Sphere(int lon, int lat)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int y = 0; y <= lat; y++)
            {
                float v = y / (float)lat;
                float phi = (v - 0.5f) * Mathf.PI;
                for (int x = 0; x <= lon; x++)
                {
                    float theta = x / (float)lon * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(phi) * Mathf.Cos(theta), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(theta)));
                }
            }

            for (int y = 0; y < lat; y++)
            {
                for (int x = 0; x < lon; x++)
                {
                    int a = (y * (lon + 1)) + x;
                    int b = a + lon + 1;
                    tris.Add(a);
                    tris.Add(b);
                    tris.Add(a + 1);
                    tris.Add(a + 1);
                    tris.Add(b);
                    tris.Add(b + 1);
                }
            }

            var mesh = new Mesh { name = "DS Sphere" };
            mesh.SetVertices(verts);
            mesh.SetNormals(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Stars(int count, int seed)
        {
            var rng = new System.Random(seed);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var tris = new List<int>();
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f).normalized;
                Vector3 p = dir * (220f + (float)(rng.NextDouble() * 120.0));
                float size = 0.35f + ((float)rng.NextDouble() * (float)rng.NextDouble() * 1.6f);
                Vector3 right = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized * size;
                Vector3 up = Vector3.Cross(right, dir).normalized * size;
                float warm = (float)rng.NextDouble();
                var c = Color.Lerp(new Color(0.7f, 0.8f, 1f), new Color(1f, 0.85f, 0.7f), warm) * (0.4f + (0.6f * (float)rng.NextDouble()));
                c.a = 1f;
                int b = verts.Count;
                verts.Add(p - right - up);
                verts.Add(p + right - up);
                verts.Add(p + right + up);
                verts.Add(p - right + up);
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                for (int k = 0; k < 4; k++)
                {
                    colors.Add(c);
                }

                tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }

            var mesh = new Mesh { name = "DS Stars" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private sealed class Launch
        {
            public LineRenderer Line;
            public Vector3 From;
            public Vector3 To;
            public float Seconds;
            public float Size;
            public float Age;
        }
    }

    /// <summary>
    /// The planet's surface map (SPEC-044), pure math so it can run on a worker thread: R land, G city lights,
    /// B blackout region order, A burn and terrain noise. Equirectangular, seamless (sampled on the sphere).
    /// </summary>
    public static class GlobeMap
    {
        public static Color32[] Build(int width, int height, int seed)
        {
            var px = new Color32[width * height];
            float s = seed * 0.001f;
            Parallel.For(0, height, y =>
            {
                float lat = ((y + 0.5f) / height - 0.5f) * Mathf.PI;
                float cl = Mathf.Cos(lat);
                float sl = Mathf.Sin(lat);
                for (int x = 0; x < width; x++)
                {
                    float lon = ((x + 0.5f) / width - 0.5f) * Mathf.PI * 2f;
                    var d = new Vector3(cl * Mathf.Cos(lon), sl, cl * Mathf.Sin(lon));

                    // continents: warped fractal noise, a little less land toward the poles
                    Vector3 w = d + (0.35f * new Vector3(Fbm(d * 1.7f + new Vector3(s, 3.1f, 1.7f), 3), Fbm(d * 1.7f + new Vector3(5.2f, s, 8.3f), 3), Fbm(d * 1.7f + new Vector3(2.9f, 7.7f, s), 3)));
                    float h = Fbm(w * 1.6f, 5) - (0.12f * Mathf.Abs(sl));
                    float land = Smooth(0.02f, 0.06f, h);
                    float coast = Mathf.Clamp01(1f - (Mathf.Abs(h - 0.04f) * 9f));

                    // cities: fine points of light, dense in metro clusters and along coasts, a faint glow under them
                    float cluster = Mathf.Clamp01((Fbm(d * 7f + new Vector3(11f, s, 4f), 3) + 0.08f) * 2.6f);
                    float dots = Mathf.Pow(Noise((d * 230f) + new Vector3(3.3f, 1.1f, s)), 6f) * 4f;
                    float towns = Hash(x * 7 + (y * 13 * width) + seed) > 0.992f ? 0.5f : 0f;
                    float city = land * Mathf.Clamp01((cluster * cluster * (0.08f + dots) * (0.5f + coast)) + (towns * cluster));
                    city *= 1f - Smooth(0.55f, 0.75f, Mathf.Abs(sl));

                    // blackout order: whole regions go together, with a ragged edge
                    float region = Mathf.Clamp01(0.5f + (0.9f * Fbm(d * 1.3f + new Vector3(s, 21f, 9f), 2)) + (0.08f * Fbm(d * 12f, 1)));
                    float terrain = Mathf.Clamp01(0.5f + (1.2f * Fbm(d * 6f + new Vector3(4f, 4f, s), 3)));
                    px[(y * width) + x] = new Color32(Byte(land), Byte(city), Byte(region), Byte(terrain));
                }
            });

            return px;
        }

        private static byte Byte(float v)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
        }

        private static float Smooth(float a, float b, float v)
        {
            float t = Mathf.Clamp01((v - a) / (b - a));
            return t * t * (3f - (2f * t));
        }

        private static float Fbm(Vector3 p, int octaves)
        {
            float sum = 0f;
            float amp = 0.5f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * (Noise(p) - 0.5f) * 2f;
                p = (p * 2.03f) + new Vector3(1.7f, 9.2f, 3.3f);
                amp *= 0.5f;
            }

            return sum;
        }

        private static float Noise(Vector3 p)
        {
            int xi = Mathf.FloorToInt(p.x);
            int yi = Mathf.FloorToInt(p.y);
            int zi = Mathf.FloorToInt(p.z);
            float fx = p.x - xi;
            float fy = p.y - yi;
            float fz = p.z - zi;
            fx = fx * fx * (3f - (2f * fx));
            fy = fy * fy * (3f - (2f * fy));
            fz = fz * fz * (3f - (2f * fz));
            float x00 = Mathf.Lerp(Hash3(xi, yi, zi), Hash3(xi + 1, yi, zi), fx);
            float x10 = Mathf.Lerp(Hash3(xi, yi + 1, zi), Hash3(xi + 1, yi + 1, zi), fx);
            float x01 = Mathf.Lerp(Hash3(xi, yi, zi + 1), Hash3(xi + 1, yi, zi + 1), fx);
            float x11 = Mathf.Lerp(Hash3(xi, yi + 1, zi + 1), Hash3(xi + 1, yi + 1, zi + 1), fx);
            return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
        }

        private static float Hash3(int x, int y, int z)
        {
            return Hash((x * 73856093) ^ (y * 19349663) ^ (z * 83492791));
        }

        private static float Hash(int n)
        {
            unchecked
            {
                uint h = (uint)n;
                h ^= h >> 16;
                h *= 0x7feb352dU;
                h ^= h >> 15;
                h *= 0x846ca68bU;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }
    }
}
