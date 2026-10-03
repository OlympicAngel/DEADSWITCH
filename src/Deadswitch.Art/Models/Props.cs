using System;
using System.Collections.Generic;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>Building blocks for the dense compound look: containers, stairs, rails, rocks, trees, people, signage.</summary>
    public static class Props
    {
        /// <summary>
        /// ISO-style shipping container (real proportions: 6.0 x 2.6 x 2.44 m at scale 1) on y = baseCenter.Y, long
        /// axis X, doors on the -X end, long side facing -Z. Corrugated walls and roof, corner castings, rails,
        /// optional cut-in doorway on the long side with a warm interior, stenciled label on the long side.
        /// </summary>
        public static void Container(MeshBuilder b, Model m, Vector3 baseCenter, float length, float height, float depth, Mat mat, bool open, uint seed, string label = "")
        {
            float hx = length * 0.5f;
            float hz = depth * 0.5f;
            const float post = 0.16f;
            b.Push(Matrix4x4.CreateTranslation(baseCenter));

            // inner core box (so gaps never show through) and corrugated skins
            b.BoxOn(0, 0.12f, 0, length - 0.1f, height - 0.24f, depth - 0.12f, mat, 0.02f);
            b.Corrugated(-hx + post, hx - post, 0.14f, height - 0.14f, -hz, mat);
            b.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(180f)));
            b.Corrugated(-hx + post, hx - post, 0.14f, height - 0.14f, -hz, mat);
            b.Pop();
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(90f)) * Matrix4x4.CreateTranslation(new Vector3(0, height - 0.04f, 0)));
            b.Corrugated(-hx + post, hx - post, -hz + 0.06f, hz - 0.06f, 0f, mat, 0.6f, 0.025f);
            b.Pop();

            // frame: corner posts, top and bottom side rails, corner castings
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    b.BoxOn(sx * (hx - (post * 0.5f)), 0, sz * (hz - (post * 0.5f)), post, height, post, mat, 0.02f);
                    b.Box(new Vector3(sx * (hx - 0.09f), 0.09f, sz * (hz - 0.09f)), new Vector3(0.2f, 0.18f, 0.2f), Mat.DarkSteel, 0.02f);
                    b.Box(new Vector3(sx * (hx - 0.09f), height - 0.09f, sz * (hz - 0.09f)), new Vector3(0.2f, 0.18f, 0.2f), Mat.DarkSteel, 0.02f);
                }

                b.Box(new Vector3(0, height - 0.07f, sx * (hz - 0.06f)), new Vector3(length, 0.14f, 0.14f), mat, 0.02f);
                b.Box(new Vector3(0, 0.09f, sx * (hz - 0.06f)), new Vector3(length, 0.18f, 0.16f), Mat.DarkSteel, 0.02f);
            }

            // cargo doors on the -X end: two leaves, four vertical locking bars, handles
            b.Box(new Vector3(-hx - 0.01f, height * 0.5f, 0), new Vector3(0.05f, height - 0.3f, depth - 0.3f), mat, 0.01f);
            b.Box(new Vector3(-hx - 0.04f, height * 0.5f, 0), new Vector3(0.03f, height - 0.3f, 0.03f), Mat.DarkSteel, 0f);
            foreach (float z in new[] { -0.75f, -0.35f, 0.35f, 0.75f })
            {
                b.Box(new Vector3(-hx - 0.07f, height * 0.5f, z * depth / 2.44f), new Vector3(0.04f, height - 0.4f, 0.04f), Mat.DarkSteel, 0.01f);
                b.Box(new Vector3(-hx - 0.1f, height * 0.42f, (z * depth / 2.44f) + 0.08f), new Vector3(0.05f, 0.05f, 0.18f), Mat.DarkSteel, 0.01f);
            }

            if (open)
            {
                // doorway cut into the long side, warm interior, plate door swung out
                float dx = -length * 0.18f;
                b.Box(new Vector3(dx, (height * 0.46f) + 0.05f, -hz - 0.07f), new Vector3(1.05f, (height * 0.8f) + 0.04f, 0.04f), Mat.Interior, 0.005f);
                b.Box(new Vector3(dx, (height * 0.87f) + 0.05f, -hz - 0.1f), new Vector3(1.25f, 0.1f, 0.06f), Mat.DarkSteel, 0.01f);
                b.Box(new Vector3(dx - 0.6f, (height * 0.46f) + 0.05f, -hz - 0.1f), new Vector3(0.08f, height * 0.82f, 0.06f), Mat.DarkSteel, 0.01f);
                b.Box(new Vector3(dx + 0.6f, (height * 0.46f) + 0.05f, -hz - 0.1f), new Vector3(0.08f, height * 0.82f, 0.06f), Mat.DarkSteel, 0.01f);
                b.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(-75f)) * Matrix4x4.CreateTranslation(new Vector3(dx + 0.58f, 0.12f, -hz - 0.12f)));
                b.BoxOn(0.5f, 0, 0, 1.0f, height * 0.8f, 0.05f, mat, 0.01f);
                b.Pop();
                b.BoxOn(dx, 0, -hz - 0.45f, 1.2f, 0.12f, 0.6f, Mat.DarkSteel, 0.02f);
                Lamp(b, m, new Vector3(dx, height * 0.95f, -hz - 0.35f), true, 1.0f);
                m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(dx, height * 0.5f, -hz - 0.9f)), Model.Amber, 1.4f, 4.5f, LightRole.Status));
            }

            if (label.Length > 0)
            {
                float px = Math.Min(0.11f, (length * 0.42f) / (label.Length * 6f));
                float x0 = (open ? 0.1f : -0.5f) * length * 0.5f - ((label.Length * 6f * px) * (open ? 0f : 0.5f));
                Stencil(b, label, new Vector3(Math.Max(x0, -hx + 0.4f), height * 0.56f, -hz - 0.065f), px, Mat.PaintWhite);
            }

            b.Pop();
        }

        /// <summary>Wall or post lamp: housing, emissive lens, a point light and an optional volumetric cone.</summary>
        public static void Lamp(MeshBuilder b, Model m, Vector3 p, bool cone, float intensity = 1.8f, LightRole role = LightRole.Ambient)
        {
            b.Box(p + new Vector3(0, 0.08f, 0), new Vector3(0.34f, 0.12f, 0.26f), Mat.DarkSteel, 0.03f);
            b.Box(p, new Vector3(0.26f, 0.05f, 0.18f), Mat.LampAmber, 0.01f);
            if (cone)
            {
                b.LightCone(p - new Vector3(0, 0.04f, 0), Math.Min(3.4f, b.TransformPoint(p).Y), 1.1f);
            }

            m.Lights.Add(new LightSpec(b.TransformPoint(p - new Vector3(0, 0.3f, 0)), Model.Amber, intensity, 7.5f, role));
        }

        public static void Stairs(MeshBuilder b, Vector3 bottom, float rise, float yawDeg, float width = 0.8f)
        {
            int steps = Math.Max(3, (int)(rise / 0.22f));
            float run = steps * 0.26f;
            b.Push(bottom, yawDeg);
            for (int i = 0; i < steps; i++)
            {
                b.BoxOn(0, (i + 1) * rise / steps - 0.05f, -(i * run / steps), width, 0.05f, 0.28f, Mat.DarkSteel, 0.01f);
            }

            b.Strut(new Vector3(-width / 2f, 0, 0.1f), new Vector3(-width / 2f, rise, -run), 0.06f, Mat.DarkSteel);
            b.Strut(new Vector3(width / 2f, 0, 0.1f), new Vector3(width / 2f, rise, -run), 0.06f, Mat.DarkSteel);
            b.Strut(new Vector3(width / 2f, 0.9f, 0.1f), new Vector3(width / 2f, rise + 0.9f, -run), 0.04f, Mat.Paint);
            b.Strut(new Vector3(width / 2f, 0, 0.1f), new Vector3(width / 2f, 0.9f, 0.1f), 0.04f, Mat.Paint);
            b.Pop();
        }

        /// <summary>Railing along a polyline at height y (posts every ~1 m).</summary>
        public static void Railing(MeshBuilder b, Vector3 a, Vector3 c, Mat mat = Mat.Paint)
        {
            float len = Vector3.Distance(a, c);
            int posts = Math.Max(2, (int)(len / 1.0f) + 1);
            for (int i = 0; i < posts; i++)
            {
                Vector3 p = Vector3.Lerp(a, c, i / (float)(posts - 1));
                b.Strut(p, p + new Vector3(0, 0.95f, 0), 0.045f, mat);
            }

            b.Strut(a + new Vector3(0, 0.95f, 0), c + new Vector3(0, 0.95f, 0), 0.05f, mat);
            b.Strut(a + new Vector3(0, 0.5f, 0), c + new Vector3(0, 0.5f, 0), 0.035f, mat);
        }

        /// <summary>Faceted boulder: a low-poly sphere with noisy radius, flattened at the base.</summary>
        public static void Boulder(MeshBuilder b, Vector3 center, Vector3 radii, uint seed, Mat mat = Mat.Rock)
        {
            const int lat = 5;
            const int lon = 7;
            var rng = new ArtRandom(seed);
            var pts = new Vector3[lat + 1, lon];
            for (int i = 0; i <= lat; i++)
            {
                float phi = (float)(Math.PI * i / lat);
                for (int j = 0; j < lon; j++)
                {
                    float th = (float)(Math.PI * 2 * j / lon) + (i % 2 == 0 ? 0f : 0.4f);
                    float k = 0.78f + (rng.Next() * 0.38f);
                    var d = new Vector3((float)(Math.Sin(phi) * Math.Cos(th)), (float)Math.Cos(phi), (float)(Math.Sin(phi) * Math.Sin(th)));
                    Vector3 p = center + (d * radii * k);
                    p.Y = Math.Max(p.Y, center.Y - (radii.Y * 0.35f));
                    pts[i, j] = p;
                }
            }

            for (int i = 0; i < lat; i++)
            {
                for (int j = 0; j < lon; j++)
                {
                    int jn = (j + 1) % lon;
                    b.Face(center, mat, 1f, pts[i, j], pts[i, jn], pts[i + 1, jn]);
                    b.Face(center, mat, 0.96f, pts[i, j], pts[i + 1, jn], pts[i + 1, j]);
                }
            }
        }

        /// <summary>Stylized pine: trunk and three stacked faceted cones.</summary>
        public static void Pine(MeshBuilder b, Vector3 basePos, float height, uint seed)
        {
            var rng = new ArtRandom(seed);
            b.Frustum(basePos, height * 0.04f, height * 0.03f, height * 0.3f, 5, Mat.Wood, 0.01f);
            float r = height * 0.26f;
            for (int i = 0; i < 3; i++)
            {
                float y = height * (0.18f + (i * 0.24f));
                float rr = r * (1f - (i * 0.22f)) * rng.Range(0.9f, 1.1f);
                b.Frustum(basePos + new Vector3(0, y, 0), rr, rr * 0.08f, height * 0.42f, 7, Mat.Foliage, 0.02f);
            }
        }

        /// <summary>A survivor with believable proportions (1.78 m), facing -Z: work clothes, vest, helmet or cap, pack.</summary>
        public static MeshData Person(uint seed)
        {
            var b = new MeshBuilder(seed) { AoHeight = 0.5f, AoFloor = 0.75f, FaceJitter = 0.03f };
            var rng = new ArtRandom(seed);
            Mat[] jackets = { Mat.OliveSteel, Mat.Tarp, Mat.TarpBlue, Mat.SandSteel, Mat.DarkSteel };
            Mat jacket = jackets[rng.Range(0, jackets.Length)];
            Mat pants = rng.Next() < 0.5f ? Mat.DarkSteel : Mat.Tarp;
            const float r = 0.072f;
            foreach (int s in new[] { -1, 1 })
            {
                b.Capsule(new Vector3(s * 0.1f, 0.12f, 0.02f), new Vector3(s * 0.1f, 0.86f, 0f), r, pants, 8);
                b.BoxOn(s * 0.1f, 0, -0.03f, 0.13f, 0.12f, 0.27f, Mat.Rubber, 0.04f);
                b.Capsule(new Vector3(s * 0.24f, 1.42f, 0f), new Vector3(s * 0.28f, 0.98f, -0.04f), 0.058f, jacket, 8);
                b.Sphere(new Vector3(s * 0.285f, 0.93f, -0.05f), new Vector3(0.055f), 4, 8, Mat.Skin);
            }

            b.Capsule(new Vector3(0, 0.98f, 0), new Vector3(0, 1.36f, 0), 0.19f, jacket, 12);
            b.BoxOn(0, 1.0f, 0f, 0.4f, 0.42f, 0.3f, rng.Next() < 0.5f ? Mat.OliveSteel : Mat.SandSteel, 0.06f);
            b.BoxOn(0, 0.95f, 0.2f, 0.3f, 0.42f, 0.16f, Mat.Tarp, 0.05f);
            b.Capsule(new Vector3(0, 1.48f, 0), new Vector3(0, 1.54f, 0), 0.05f, Mat.Skin, 8);
            b.Sphere(new Vector3(0, 1.64f, 0), new Vector3(0.1f, 0.115f, 0.105f), 5, 10, Mat.Skin);
            float hat = rng.Next();
            if (hat < 0.45f)
            {
                b.Sphere(new Vector3(0, 1.67f, 0), new Vector3(0.125f, 0.11f, 0.13f), 3, 10, Mat.OliveSteel, 0f, 0.5f);
            }
            else if (hat < 0.75f)
            {
                b.Sphere(new Vector3(0, 1.68f, 0), new Vector3(0.11f, 0.09f, 0.115f), 3, 10, Mat.DarkSteel, 0f, 0.5f);
            }

            return b.Mesh;
        }

        /// <summary>
        /// A raider for report panels: a hooded, gas-masked silhouette in dark rags with a rifle, leaning forward,
        /// facing -Z. Dark materials so the graphic-novel grade reads it as ink; one dim lens for menace, no gore.
        /// </summary>
        public static MeshData Raider(uint seed)
        {
            var b = new MeshBuilder(seed) { AoHeight = 0.5f, AoFloor = 0.7f, FaceJitter = 0.03f };
            var rng = new ArtRandom(seed);
            Mat coat = rng.Next() < 0.5f ? Mat.Rubber : Mat.DarkSteel;
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-8f)));
            foreach (int s in new[] { -1, 1 })
            {
                float stride = s * rng.Range(0.08f, 0.16f);
                b.Capsule(new Vector3(s * 0.11f, 0.12f, stride), new Vector3(s * 0.1f, 0.86f, 0f), 0.075f, Mat.Rubber, 8);
                b.BoxOn(s * 0.11f, 0, stride - 0.03f, 0.14f, 0.13f, 0.28f, Mat.Rubber, 0.04f);
            }

            b.Frustum(new Vector3(0, 0.62f, 0), 0.26f, 0.2f, 0.82f, 10, coat, 0.04f);
            b.Capsule(new Vector3(0, 1.0f, 0), new Vector3(0, 1.38f, 0.02f), 0.2f, coat, 12);
            b.BoxOn(0, 0.98f, 0.18f, 0.32f, 0.46f, 0.18f, Mat.Tarp, 0.05f);
            b.Sphere(new Vector3(0, 1.62f, 0.01f), new Vector3(0.13f, 0.14f, 0.13f), 5, 10, Mat.Rubber);
            b.Sphere(new Vector3(0, 1.66f, 0.03f), new Vector3(0.16f, 0.15f, 0.16f), 4, 10, coat, 0f, 0.6f);
            b.Box(new Vector3(0, 1.56f, -0.13f), new Vector3(0.1f, 0.1f, 0.08f), Mat.DarkSteel, 0.03f);
            foreach (int s in new[] { -1, 1 })
            {
                b.Box(new Vector3(s * 0.05f, 1.64f, -0.12f), new Vector3(0.05f, 0.035f, 0.02f), Mat.LampRed, 0f);
            }

            // rifle held across the body, pointing ahead
            b.Strut(new Vector3(0.18f, 1.12f, 0.12f), new Vector3(-0.08f, 1.24f, -0.62f), 0.045f, Mat.DarkSteel);
            b.Box(new Vector3(0.12f, 1.12f, 0.05f), new Vector3(0.06f, 0.14f, 0.2f), Mat.DarkSteel, 0.02f);
            foreach (int s in new[] { -1, 1 })
            {
                b.Capsule(new Vector3(s * 0.24f, 1.4f, 0f), new Vector3(s * 0.08f, 1.18f, -0.3f), 0.06f, coat, 8);
            }

            b.Pop();
            return b.Mesh;
        }

        /// <summary>Block letters (5x7 pixel font) painted on a wall facing -Z; origin at the text's bottom-left.</summary>
        public static void Stencil(MeshBuilder b, string text, Vector3 origin, float pixel, Mat mat)
        {
            float x = origin.X;
            foreach (char ch in text)
            {
                if (Font.TryGetValue(char.ToUpperInvariant(ch), out string[] rows))
                {
                    for (int r = 0; r < 7; r++)
                    {
                        for (int c = 0; c < 5; c++)
                        {
                            if (rows[r][c] == '#')
                            {
                                b.Box(new Vector3(x + ((c + 0.5f) * pixel), origin.Y + ((6 - r + 0.5f) * pixel), origin.Z), new Vector3(pixel * 1.02f, pixel * 1.02f, 0.03f), mat, 0f);
                            }
                        }
                    }
                }

                x += pixel * 6f;
            }
        }

        private static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".####", "#....", "#....", "#.###", "#...#", "#...#", ".###." },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
            ['3'] = new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
            ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
            ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            ['6'] = new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." },
            ['-'] = new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
            ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
            ['/'] = new[] { "....#", "...#.", "...#.", "..#..", ".#...", ".#...", "#...." },
        };
    }
}
