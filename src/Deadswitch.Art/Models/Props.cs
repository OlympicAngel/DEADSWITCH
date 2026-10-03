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
        /// Corrugated shipping container on y = <paramref name="baseCenter"/>.Y, long side along X, door side facing -Z.
        /// The door stands open on a warm lit interior when <paramref name="open"/> is set.
        /// </summary>
        public static void Container(MeshBuilder b, Model m, Vector3 baseCenter, float length, float height, float depth, Mat mat, bool open, uint seed)
        {
            float x0 = baseCenter.X - (length * 0.5f);
            float z0 = baseCenter.Z - (depth * 0.5f);
            float y0 = baseCenter.Y;
            b.BoxOn(baseCenter.X, y0, baseCenter.Z, length, height, depth, mat, 0.07f);

            // frame: corner posts and rails
            foreach (float x in new[] { x0 + 0.08f, x0 + length - 0.08f })
            {
                foreach (float z in new[] { z0 + 0.08f, z0 + depth - 0.08f })
                {
                    b.BoxOn(x, y0, z, 0.2f, height + 0.02f, 0.2f, Mat.DarkSteel, 0.03f);
                }
            }

            b.BoxOn(baseCenter.X, y0 + height - 0.12f, z0 + 0.05f, length, 0.14f, 0.14f, Mat.DarkSteel, 0.02f);
            b.BoxOn(baseCenter.X, y0, z0 + 0.05f, length, 0.14f, 0.14f, Mat.DarkSteel, 0.02f);

            // corrugation on the long front and back faces
            int ribs = (int)(length / 0.26f);
            for (int i = 1; i < ribs; i++)
            {
                float x = x0 + (i * length / ribs);
                b.Box(new Vector3(x, y0 + (height * 0.5f), z0 - 0.015f), new Vector3(0.07f, height * 0.86f, 0.05f), mat, 0.01f);
                b.Box(new Vector3(x, y0 + (height * 0.5f), z0 + depth + 0.015f), new Vector3(0.07f, height * 0.86f, 0.05f), mat, 0.01f);
            }

            // roof dents and rust patches via darker plates
            var rng = new ArtRandom(seed);
            for (int i = 0; i < 3; i++)
            {
                b.Box(new Vector3(x0 + rng.Range(0.6f, length - 0.6f), y0 + height + 0.01f, baseCenter.Z + rng.Range(-depth * 0.3f, depth * 0.3f)), new Vector3(rng.Range(0.5f, 1.2f), 0.02f, rng.Range(0.4f, 0.9f)), Mat.Rust, 0.005f);
            }

            if (open)
            {
                float dx = baseCenter.X - (length * 0.18f);
                b.Box(new Vector3(dx, y0 + (height * 0.47f), z0 - 0.03f), new Vector3(1.3f, height * 0.78f, 0.06f), Mat.Interior, 0.01f);
                b.Box(new Vector3(dx, y0 + (height * 0.47f), z0 - 0.06f), new Vector3(0.06f, height * 0.78f, 0.04f), Mat.DarkSteel, 0.005f);
                // swung door leaf
                b.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(-70f)) * Matrix4x4.CreateTranslation(new Vector3(dx - 0.66f, y0, z0 - 0.05f)));
                b.BoxOn(-0.33f, 0.05f, 0, 0.66f, height * 0.82f, 0.06f, mat, 0.02f);
                b.Pop();
                m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(dx, y0 + (height * 0.5f), z0 - 0.9f)), Model.Amber, 1.4f, 4.5f, LightRole.Status));
            }
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

        /// <summary>A chunky stylized person (about 1.8 m) facing -Z before yaw.</summary>
        public static MeshData Person(uint seed)
        {
            var b = new MeshBuilder(seed) { AoHeight = 0.6f, AoFloor = 0.7f };
            var rng = new ArtRandom(seed);
            Mat[] jackets = { Mat.OliveSteel, Mat.Tarp, Mat.TarpBlue, Mat.SandSteel, Mat.Rust };
            Mat jacket = jackets[rng.Range(0, jackets.Length)];
            Mat pants = rng.Next() < 0.5f ? Mat.DarkSteel : Mat.Wood;
            b.BoxOn(-0.13f, 0, 0, 0.2f, 0.82f, 0.24f, pants, 0.05f);
            b.BoxOn(0.13f, 0, 0, 0.2f, 0.82f, 0.24f, pants, 0.05f);
            b.BoxOn(-0.13f, 0, -0.04f, 0.22f, 0.14f, 0.32f, Mat.Rubber, 0.04f);
            b.BoxOn(0.13f, 0, -0.04f, 0.22f, 0.14f, 0.32f, Mat.Rubber, 0.04f);
            b.BoxOn(0, 0.78f, 0, 0.56f, 0.62f, 0.34f, jacket, 0.1f);
            b.BoxOn(-0.36f, 0.82f, 0, 0.16f, 0.56f, 0.18f, jacket, 0.06f);
            b.BoxOn(0.36f, 0.82f, 0, 0.16f, 0.56f, 0.18f, jacket, 0.06f);
            b.BoxOn(0, 0.85f, 0.24f, 0.4f, 0.45f, 0.18f, Mat.Tarp, 0.06f);
            b.BoxOn(0, 1.38f, 0, 0.3f, 0.32f, 0.3f, Mat.Skin, 0.09f);
            if (rng.Next() < 0.6f)
            {
                b.BoxOn(0, 1.62f, 0, 0.36f, 0.14f, 0.36f, rng.Next() < 0.5f ? Mat.OliveSteel : Mat.DarkSteel, 0.06f);
            }

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
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['-'] = new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['C'] = new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
        };
    }
}
