using System;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>
    /// Assemblies built from <see cref="KitParts"/> and props: the vocabulary the compound is made of
    /// (docs/agents/environment-art.md). Structures are asymmetric and show their construction history.
    /// </summary>
    public static class KitModules
    {
        /// <summary>
        /// A lived-in container block: the container plus welded window cutouts (lit or dark), patch plates,
        /// a utility box and conduits, and rooftop clutter. Long axis X, front -Z, base at y.
        /// </summary>
        public static void ContainerBlock(MeshBuilder b, Model m, Vector3 baseCenter, float length, Mat mat, bool door, string label, ArtRandom rng, int windows, bool roofClutter = true)
        {
            const float h = 2.6f;
            const float d = 2.44f;
            Props.Container(b, m, baseCenter, length, h, d, mat, door, rng.NextUInt(), label);
            float z = baseCenter.Z - (d * 0.5f) - 0.07f;
            for (int i = 0; i < windows; i++)
            {
                float x = baseCenter.X + (length * (0.18f + (0.22f * i)));
                if (x > baseCenter.X + (length * 0.5f) - 0.6f)
                {
                    break;
                }

                bool lit = rng.Next() < 0.6f;
                b.Box(new Vector3(x, baseCenter.Y + 1.55f, z - 0.01f), new Vector3(0.8f, 0.55f, 0.03f), Mat.Glass, 0f);
                if (lit)
                {
                    // lamp-lit room behind dirty glass: a smaller warm core, not a glowing pane
                    b.Box(new Vector3(x + 0.06f, baseCenter.Y + 1.53f, z - 0.02f), new Vector3(0.46f, 0.32f, 0.02f), Mat.Interior, 0f);
                }

                b.Box(new Vector3(x, baseCenter.Y + 1.86f, z - 0.03f), new Vector3(0.96f, 0.08f, 0.06f), Mat.DarkSteel, 0.01f);
                b.Box(new Vector3(x, baseCenter.Y + 1.24f, z - 0.05f), new Vector3(0.96f, 0.06f, 0.12f), Mat.DarkSteel, 0.01f);
                for (int k = 0; k < 3; k++)
                {
                    b.Box(new Vector3(x - 0.3f + (k * 0.3f), baseCenter.Y + 1.55f, z - 0.04f), new Vector3(0.03f, 0.6f, 0.03f), Mat.DarkSteel, 0f);
                }
            }

            int patches = rng.Range(1, 4);
            for (int i = 0; i < patches; i++)
            {
                KitParts.Plate(b, new Vector3(baseCenter.X + rng.Range(-length * 0.45f, length * 0.45f), baseCenter.Y + rng.Range(0.4f, 2.1f), z - 0.06f), rng.Range(0.4f, 1.1f), rng.Range(0.3f, 0.8f), rng.Range(-6f, 6f), rng.Next() < 0.5f ? Mat.Rust : Mat.DarkSteel);
            }

            KitParts.UtilityBox(b, new Vector3(baseCenter.X + (length * 0.42f), baseCenter.Y + 1.3f, z - 0.12f));

            // about a third of containers carry a small salvaged neon accent: a tube under the roof edge or a
            // vertical sign by the door (position-hashed so the art seed sequence stays unchanged)
            float neon = MeshBuilder.PartVariation(b.TransformPoint(baseCenter) + new Vector3(0.37f, 0f, 0f));
            if (neon < 0.34f)
            {
                Mat tube = neon < 0.2f ? Mat.NeonCyan : neon < 0.28f ? Mat.LampRed : Mat.LampAmber;
                Vector3 color = neon < 0.2f ? Model.Neon : neon < 0.28f ? Model.Red : Model.Amber;
                if (neon < 0.17f)
                {
                    float x0 = baseCenter.X - (length * 0.3f);
                    b.Box(new Vector3(x0 + (length * 0.2f), baseCenter.Y + h - 0.22f, z - 0.05f), new Vector3(length * 0.4f, 0.035f, 0.035f), tube, 0f);
                    m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(x0 + (length * 0.2f), baseCenter.Y + h - 0.4f, z - 0.4f)), color, 0.55f, 3.2f, LightRole.Status));
                }
                else
                {
                    float x = baseCenter.X - (length * 0.5f) + 0.35f;
                    b.Box(new Vector3(x, baseCenter.Y + 1.7f, z - 0.08f), new Vector3(0.16f, 0.7f, 0.06f), Mat.DarkSteel, 0.01f);
                    b.Box(new Vector3(x, baseCenter.Y + 1.7f, z - 0.12f), new Vector3(0.05f, 0.58f, 0.02f), tube, 0f);
                    m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(x, baseCenter.Y + 1.7f, z - 0.45f)), color, 0.45f, 2.6f, LightRole.Status));
                }
            }

            if (roofClutter)
            {
                RoofClutter(b, m, new Vector3(baseCenter.X, baseCenter.Y + h, baseCenter.Z), length - 0.6f, d - 0.5f, rng, 2 + rng.Range(0, 3));
            }
        }

        /// <summary>Equipment and junk on a flat roof area (centered at <paramref name="top"/>).</summary>
        public static void RoofClutter(MeshBuilder b, Model m, Vector3 top, float w, float d, ArtRandom rng, int items)
        {
            for (int i = 0; i < items; i++)
            {
                var p = top + new Vector3(rng.Range(-w * 0.45f, w * 0.45f), 0, rng.Range(-d * 0.35f, d * 0.35f));
                switch (rng.Range(0, 6))
                {
                    case 0:
                        KitParts.AcUnit(b, p);
                        break;
                    case 1:
                        Shapes.SandbagWall(b, p - new Vector3(0.8f, 0, 0), p + new Vector3(0.8f, 0, 0.2f), 2);
                        break;
                    case 2:
                        KitParts.Barrel(b, p, rng.Next() < 0.5f ? Mat.PaintRed : Mat.OliveSteel);
                        KitParts.Barrel(b, p + new Vector3(0.62f, 0, 0.1f), Mat.Rust);
                        break;
                    case 3:
                        b.Frustum(p, 0.05f, 0.03f, rng.Range(1.6f, 3.2f), 6, Mat.DarkSteel, 0.01f);
                        break;
                    case 4:
                        TarpPile(b, p, rng.Range(0.8f, 1.4f), rng);
                        break;
                    default:
                        KitParts.Vent(b, p, 0.6f, 0.5f);
                        break;
                }
            }
        }

        /// <summary>Open steel shed: posts, eave beams, mono-pitch corrugated roof with an overhang.</summary>
        public static void Shed(MeshBuilder b, Vector3 baseCenter, float w, float d, float hFront, float hBack, Mat roof, bool trusses = true)
        {
            float hw = w * 0.5f;
            float hd = d * 0.5f;
            int bays = Math.Max(1, (int)Math.Round(w / 2.6f));
            for (int i = 0; i <= bays; i++)
            {
                float x = -hw + (i * w / bays);
                KitParts.IBeam(b, baseCenter + new Vector3(x, 0, -hd), baseCenter + new Vector3(x, hFront, -hd), 0.16f, 0.12f, Mat.DarkSteel);
                KitParts.IBeam(b, baseCenter + new Vector3(x, 0, hd), baseCenter + new Vector3(x, hBack, hd), 0.16f, 0.12f, Mat.DarkSteel);
                if (trusses)
                {
                    b.Strut(baseCenter + new Vector3(x, hFront, -hd), baseCenter + new Vector3(x, hBack, hd), 0.1f, Mat.DarkSteel);
                }
            }

            b.Strut(baseCenter + new Vector3(-hw, hFront, -hd), baseCenter + new Vector3(hw, hFront, -hd), 0.12f, Mat.DarkSteel);
            b.Strut(baseCenter + new Vector3(-hw, hBack, hd), baseCenter + new Vector3(hw, hBack, hd), 0.12f, Mat.DarkSteel);
            float pitch = (float)Math.Atan2(hBack - hFront, d);
            float len = (float)Math.Sqrt((d * d) + ((hBack - hFront) * (hBack - hFront))) + 0.9f;
            b.Push(Matrix4x4.CreateRotationX(-pitch) * Matrix4x4.CreateRotationX(MeshBuilder.Deg(-90f)) * Matrix4x4.CreateTranslation(baseCenter + new Vector3(0, ((hFront + hBack) * 0.5f) + 0.12f, -0.15f)));
            b.Corrugated(-hw - 0.4f, hw + 0.4f, -len * 0.5f, len * 0.5f, 0f, roof, 0.32f, 0.04f);
            b.Pop();
        }

        /// <summary>Lean-to roof against a wall: two posts, sloped sheet, a tarp side.</summary>
        public static void LeanTo(MeshBuilder b, Vector3 wallBase, float w, float depth, float hWall, float hOuter, Mat roof)
        {
            b.Strut(wallBase + new Vector3(-w * 0.5f, 0, -depth), wallBase + new Vector3(-w * 0.5f, hOuter, -depth), 0.09f, Mat.DarkSteel);
            b.Strut(wallBase + new Vector3(w * 0.5f, 0, -depth), wallBase + new Vector3(w * 0.5f, hOuter, -depth), 0.09f, Mat.DarkSteel);
            b.Strut(wallBase + new Vector3(-w * 0.5f, hOuter, -depth), wallBase + new Vector3(w * 0.5f, hOuter, -depth), 0.08f, Mat.DarkSteel);
            float pitch = (float)Math.Atan2(hWall - hOuter, depth);
            float len = (float)Math.Sqrt((depth * depth) + ((hWall - hOuter) * (hWall - hOuter))) + 0.4f;
            b.Push(Matrix4x4.CreateRotationX(-pitch) * Matrix4x4.CreateRotationX(MeshBuilder.Deg(-90f)) * Matrix4x4.CreateTranslation(wallBase + new Vector3(0, ((hWall + hOuter) * 0.5f) + 0.05f, -depth * 0.5f)));
            b.Corrugated(-w * 0.5f - 0.2f, w * 0.5f + 0.2f, -len * 0.5f, len * 0.5f, 0f, roof, 0.32f, 0.035f);
            b.Pop();
            b.Box(wallBase + new Vector3(w * 0.5f + 0.02f, hOuter * 0.55f, -depth * 0.5f), new Vector3(0.03f, hOuter * 0.8f, depth * 0.9f), Mat.TarpBlue, 0.005f);
        }

        /// <summary>Raised platform on I-beam legs with grating deck, railing on open sides and stairs.</summary>
        public static void Platform(MeshBuilder b, Vector3 baseCenter, float w, float d, float h, bool stairs, float stairYaw, bool railFront = true)
        {
            float hw = w * 0.5f;
            float hd = d * 0.5f;
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    KitParts.IBeam(b, baseCenter + new Vector3(sx * (hw - 0.1f), 0, sz * (hd - 0.1f)), baseCenter + new Vector3(sx * (hw - 0.1f), h, sz * (hd - 0.1f)), 0.14f, 0.12f, Mat.DarkSteel);
                }

                b.Strut(baseCenter + new Vector3(sx * (hw - 0.1f), 0.2f, -hd + 0.1f), baseCenter + new Vector3(sx * (hw - 0.1f), h - 0.2f, hd - 0.1f), 0.05f, Mat.DarkSteel);
            }

            b.BoxOn(baseCenter.X, baseCenter.Y + h - 0.12f, baseCenter.Z, w, 0.12f, d, Mat.DarkSteel, 0.01f);
            for (float x = -hw + 0.2f; x < hw; x += 0.3f)
            {
                b.Box(baseCenter + new Vector3(x, h + 0.005f, 0), new Vector3(0.03f, 0.01f, d - 0.1f), Mat.Rust, 0f);
            }

            if (railFront)
            {
                Props.Railing(b, baseCenter + new Vector3(-hw, h, -hd), baseCenter + new Vector3(hw, h, -hd));
            }

            Props.Railing(b, baseCenter + new Vector3(hw, h, -hd), baseCenter + new Vector3(hw, h, hd));
            if (stairs)
            {
                Props.Stairs(b, baseCenter + new Vector3(-hw - 0.5f, 0, -hd + 0.4f + (h * 1.2f)), h, stairYaw);
            }
        }

        /// <summary>Elevated catwalk between two points (same height), with posts and both railings.</summary>
        public static void Catwalk(MeshBuilder b, Vector3 a, Vector3 c, float width = 0.9f)
        {
            Vector3 dir = Vector3.Normalize(new Vector3(c.X - a.X, 0, c.Z - a.Z));
            Vector3 side = new Vector3(-dir.Z, 0, dir.X) * (width * 0.5f);
            float len = Vector3.Distance(a, c);
            int posts = Math.Max(1, (int)(len / 3f));
            for (int i = 0; i <= posts; i++)
            {
                Vector3 p = Vector3.Lerp(a, c, i / (float)posts);
                b.Strut(new Vector3(p.X, 0, p.Z) + side, p + side - new Vector3(0, 0.1f, 0), 0.08f, Mat.DarkSteel);
                b.Strut(new Vector3(p.X, 0, p.Z) - side, p - side - new Vector3(0, 0.1f, 0), 0.08f, Mat.DarkSteel);
                b.Strut(new Vector3(p.X, p.Y * 0.35f, p.Z) + side, p - side - new Vector3(0, 0.15f, 0), 0.04f, Mat.DarkSteel);
            }

            b.Strut(a - new Vector3(0, 0.06f, 0), c - new Vector3(0, 0.06f, 0), width, Mat.DarkSteel);
            Props.Railing(b, a + side, c + side);
            Props.Railing(b, a - side, c - side);
        }

        /// <summary>Terraced concrete retaining wall along X with seams, weep pipes, rebar at the broken end.</summary>
        public static void RetainingWall(MeshBuilder b, Vector3 baseCenter, float length, float height, ArtRandom rng)
        {
            int panels = Math.Max(1, (int)(length / 2.4f));
            float pw = length / panels;
            for (int i = 0; i < panels; i++)
            {
                float x = baseCenter.X - (length * 0.5f) + ((i + 0.5f) * pw);
                float ph = height * (i == panels - 1 ? rng.Range(0.55f, 0.8f) : rng.Range(0.94f, 1.02f));
                b.BoxOn(x, baseCenter.Y, baseCenter.Z, pw - 0.04f, ph, 0.6f, Mat.Concrete, 0.04f);
                b.Box(new Vector3(x, baseCenter.Y + (ph * 0.5f), baseCenter.Z - 0.31f), new Vector3(0.04f, ph * 0.95f, 0.02f), Mat.ConcreteDark, 0f);
                b.CylinderZ(new Vector3(x, baseCenter.Y + 0.5f, baseCenter.Z - 0.35f), 0.07f, 0.2f, 8, Mat.DarkSteel, 0.01f);
                if (i == panels - 1)
                {
                    KitParts.Rebar(b, new Vector3(x + (pw * 0.4f), baseCenter.Y + ph, baseCenter.Z), new Vector3(0.3f, 0.8f, 0), 6, rng.NextUInt());
                }
            }

            b.BoxOn(baseCenter.X, baseCenter.Y + height, baseCenter.Z + 0.1f, length - pw, 0.25f, 0.9f, Mat.ConcreteDark, 0.04f);
        }

        /// <summary>Guard tower: four legs with bracing, cabin with a corrugated roof, ladder, spotlight.</summary>
        public static void Watchtower(MeshBuilder b, Model m, Vector3 baseCenter, float height)
        {
            const float w = 1.2f;
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    b.Strut(baseCenter + new Vector3(sx * w * 1.2f, 0, sz * w * 1.2f), baseCenter + new Vector3(sx * w, height, sz * w), 0.12f, Mat.Wood);
                }
            }

            for (float y = 1.2f; y < height - 0.5f; y += 1.6f)
            {
                b.Strut(baseCenter + new Vector3(-w * 1.15f, y, -w * 1.15f), baseCenter + new Vector3(w * 1.15f, y + 0.8f, -w * 1.15f), 0.06f, Mat.Wood);
                b.Strut(baseCenter + new Vector3(w * 1.15f, y, -w * 1.15f), baseCenter + new Vector3(w * 1.15f, y + 0.8f, w * 1.15f), 0.06f, Mat.Wood);
            }

            b.BoxOn(baseCenter.X, baseCenter.Y + height, baseCenter.Z, w * 2.5f, 0.12f, w * 2.5f, Mat.Wood, 0.02f);
            Shapes.SandbagWall(b, baseCenter + new Vector3(-w * 1.1f, height + 0.12f, -w * 1.2f), baseCenter + new Vector3(w * 1.1f, height + 0.12f, -w * 1.2f), 2);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    b.Strut(baseCenter + new Vector3(sx * w * 1.15f, height, sz * w * 1.15f), baseCenter + new Vector3(sx * w * 1.15f, height + 2.0f, sz * w * 1.15f), 0.07f, Mat.Wood);
                }
            }

            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-8f)) * Matrix4x4.CreateTranslation(baseCenter + new Vector3(0, height + 2.1f, 0)));
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-90f)));
            b.Corrugated(-w * 1.5f, w * 1.5f, -w * 1.5f, w * 1.5f, 0f, Mat.Rust, 0.3f, 0.03f);
            b.Pop();
            b.Pop();
            KitParts.Ladder(b, baseCenter + new Vector3(w * 1.25f, 0, 0), height, 90f);
            KitParts.Spotlight(b, m, baseCenter + new Vector3(w * 0.9f, height + 1.4f, -w * 1.2f), 0f);
        }

        // ---------- dressing ----------

        public static void CrateStack(MeshBuilder b, Vector3 at, ArtRandom rng)
        {
            int n = rng.Range(3, 7);
            for (int i = 0; i < n; i++)
            {
                int layer = i < 3 ? 0 : (i < 5 ? 1 : 2);
                float s = rng.Range(0.6f, 0.95f);
                b.Push(at + new Vector3(((i % 3) - 1) * 0.85f * (layer == 0 ? 1f : 0.6f), layer * 0.72f, rng.Range(-0.1f, 0.1f)), rng.Range(-12f, 12f));
                Mat mat = rng.Next() < 0.6f ? Mat.Wood : Mat.OliveSteel;
                b.BoxOn(0, 0, 0, s, s * 0.8f, s, mat, 0.04f);
                if (mat == Mat.Wood)
                {
                    b.Box(new Vector3(0, s * 0.4f, -s * 0.51f), new Vector3(s * 0.95f, 0.06f, 0.02f), Mat.Wood, 0f);
                    b.Box(new Vector3(0, s * 0.4f, -s * 0.51f), new Vector3(0.06f, s * 0.75f, 0.02f), Mat.Wood, 0f);
                }
                else
                {
                    b.Box(new Vector3(0, s * 0.82f, 0), new Vector3(s * 1.02f, 0.05f, s * 1.02f), Mat.DarkSteel, 0.01f);
                }

                b.Pop();
            }
        }

        public static void Pallet(MeshBuilder b, Vector3 at, float yaw, int stack)
        {
            b.Push(at, yaw);
            for (int k = 0; k < stack; k++)
            {
                float y = k * 0.15f;
                foreach (float z in new[] { -0.45f, 0f, 0.45f })
                {
                    b.BoxOn(0, y, z, 1.2f, 0.08f, 0.1f, Mat.Wood, 0.01f);
                }

                for (int i = 0; i < 5; i++)
                {
                    b.BoxOn(-0.5f + (i * 0.25f), y + 0.08f, 0, 0.14f, 0.03f, 1.0f, Mat.Wood, 0.005f);
                }
            }

            b.Pop();
        }

        public static void Barrels(MeshBuilder b, Vector3 at, int count, ArtRandom rng)
        {
            for (int i = 0; i < count; i++)
            {
                float pick = rng.Next();
                Mat mat = pick < 0.3f ? Mat.PaintRed : pick < 0.5f ? Mat.PaintBlue : pick < 0.62f ? Mat.PaintYellow : pick < 0.82f ? Mat.OliveSteel : Mat.Rust;
                var p = at + new Vector3((i % 3) * 0.62f + rng.Range(-0.05f, 0.05f), 0, (i / 3) * 0.62f + rng.Range(-0.05f, 0.05f));
                KitParts.Barrel(b, p, mat, rng.Next() < 0.15f);
            }
        }

        /// <summary>A lumpy pile of goods under a tied-down tarp.</summary>
        public static void TarpPile(MeshBuilder b, Vector3 at, float size, ArtRandom rng)
        {
            Mat tarp = rng.Next() < 0.35f ? Mat.TarpBlue : Mat.Tarp;
            KitParts.Tarp(b, at, size * 1.5f, size * 1.15f, size * 0.5f, tarp, rng.NextUInt());
        }

        /// <summary>Rubble pile: concrete chunks, a bent beam, planks and rebar.</summary>
        public static void Debris(MeshBuilder b, Vector3 at, float size, ArtRandom rng)
        {
            for (int i = 0; i < 6; i++)
            {
                Props.Boulder(b, at + new Vector3(rng.Range(-size, size), rng.Range(0f, size * 0.3f), rng.Range(-size * 0.6f, size * 0.6f)), new Vector3(rng.Range(0.3f, 0.8f), rng.Range(0.2f, 0.5f), rng.Range(0.3f, 0.7f)) * size, rng.NextUInt(), i % 2 == 0 ? Mat.Concrete : Mat.ConcreteDark);
            }

            KitParts.IBeam(b, at + new Vector3(-size, 0.2f, 0.2f), at + new Vector3(size * 0.9f, size * 0.6f, -0.3f), 0.16f, 0.1f, Mat.Rust);
            b.Strut(at + new Vector3(0, 0.1f, size * 0.5f), at + new Vector3(size, 0.4f, size * 0.2f), 0.08f, Mat.Wood);
            KitParts.Rebar(b, at + new Vector3(0, size * 0.4f, 0), new Vector3(0, 1, 0.3f), 4, rng.NextUInt());
        }

        /// <summary>Workbench with a vise, toolbox, lamp and parts.</summary>
        public static void Workbench(MeshBuilder b, Model m, Vector3 at, float yaw)
        {
            b.Push(at, yaw);
            b.BoxOn(0, 0.85f, 0, 1.8f, 0.08f, 0.75f, Mat.Wood, 0.01f);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    b.Strut(new Vector3(sx * 0.82f, 0, sz * 0.32f), new Vector3(sx * 0.82f, 0.85f, sz * 0.32f), 0.06f, Mat.DarkSteel);
                }
            }

            b.BoxOn(0, 0.25f, 0, 1.7f, 0.04f, 0.65f, Mat.Wood, 0.005f);
            b.BoxOn(-0.6f, 0.93f, 0, 0.5f, 0.25f, 0.3f, Mat.PaintRed, 0.02f);
            b.BoxOn(0.6f, 0.93f, -0.2f, 0.18f, 0.16f, 0.2f, Mat.DarkSteel, 0.01f);
            b.BoxOn(0.2f, 0.93f, 0.1f, 0.35f, 0.08f, 0.25f, Mat.OliveSteel, 0.01f);
            b.BoxOn(0.4f, 0.29f, 0.05f, 0.6f, 0.35f, 0.4f, Mat.Wood, 0.03f);
            b.Pop();
        }

        /// <summary>Battered pickup truck (abandoned or used for hauling), long axis X.</summary>
        public static void Pickup(MeshBuilder b, Vector3 at, float yaw, Mat paint, bool wrecked)
        {
            b.Push(at, yaw);
            if (wrecked)
            {
                b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(4f)) * Matrix4x4.CreateRotationX(MeshBuilder.Deg(-3f)));
            }

            b.BoxOn(0, 0.45f, 0, 4.6f, 0.2f, 1.75f, Mat.DarkSteel, 0.03f);
            b.BoxOn(1.55f, 0.6f, 0, 1.4f, 0.55f, 1.8f, paint, 0.12f);
            b.BoxOn(0.3f, 0.6f, 0, 1.3f, 1.25f, 1.8f, paint, 0.1f);
            b.Box(new Vector3(0.95f, 1.45f, 0), new Vector3(0.06f, 0.45f, 1.5f), Mat.Glass, 0.01f);
            b.Box(new Vector3(0.3f, 1.5f, -0.91f), new Vector3(1.0f, 0.4f, 0.02f), Mat.Glass, 0f);
            b.BoxOn(-1.4f, 0.6f, 0, 2.0f, 0.12f, 1.75f, paint, 0.02f);
            foreach (int sz in new[] { -1, 1 })
            {
                b.BoxOn(-1.4f, 0.72f, sz * 0.85f, 2.0f, 0.45f, 0.06f, paint, 0.01f);
            }

            b.BoxOn(-2.37f, 0.72f, 0, 0.06f, 0.45f, 1.75f, paint, 0.01f);
            b.BoxOn(2.28f, 0.55f, 0, 0.12f, 0.25f, 1.8f, Mat.DarkSteel, 0.03f);
            foreach (float x in new[] { -1.5f, 1.45f })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    if (wrecked && x < 0 && sz > 0)
                    {
                        continue;
                    }

                    b.CylinderZ(new Vector3(x, 0.42f, sz * 0.85f), 0.42f, 0.32f, 16, Mat.Rubber, 0.08f);
                    b.CylinderZ(new Vector3(x, 0.42f, sz * 0.9f), 0.22f, 0.3f, 12, Mat.DarkSteel, 0.02f);
                }
            }

            if (!wrecked)
            {
                b.BoxOn(-1.2f, 0.72f, 0.1f, 0.8f, 0.6f, 0.7f, Mat.Wood, 0.04f);
                b.Frustum(new Vector3(-1.9f, 0.72f, -0.4f), 0.28f, 0.28f, 0.85f, 12, Mat.PaintRed, 0.03f);
            }
            else
            {
                b.Pop();
            }

            b.Pop();
        }

        /// <summary>Chain-link fence run between two ground points: posts, rails, diagonal mesh, barbed top.</summary>
        public static void Fence(MeshBuilder b, Vector3 a, Vector3 c, float height = 2.1f)
        {
            float len = Vector3.Distance(a, c);
            int posts = Math.Max(1, (int)(len / 2.4f));
            for (int i = 0; i <= posts; i++)
            {
                Vector3 p = Vector3.Lerp(a, c, i / (float)posts);
                b.Strut(p, p + new Vector3(0, height + 0.3f, 0), 0.07f, Mat.DarkSteel);
            }

            Vector3 up = new Vector3(0, height, 0);
            b.Strut(a + up, c + up, 0.04f, Mat.DarkSteel);
            b.Strut(a + new Vector3(0, 0.15f, 0), c + new Vector3(0, 0.15f, 0), 0.03f, Mat.DarkSteel);
            int n = (int)(len / 0.3f);
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = Vector3.Lerp(a, c, i / (float)n);
                Vector3 p1 = Vector3.Lerp(a, c, (i + 1) / (float)n);
                b.Strut(p0 + new Vector3(0, 0.15f, 0), p1 + up, 0.012f, Mat.DarkSteel);
                b.Strut(p1 + new Vector3(0, 0.15f, 0), p0 + up, 0.012f, Mat.DarkSteel);
            }

            b.Strut(a + up + new Vector3(0, 0.25f, 0), c + up + new Vector3(0, 0.25f, 0), 0.02f, Mat.Rust);
        }

        /// <summary>Czech hedgehog tank trap: three welded beams.</summary>
        public static void Hedgehog(MeshBuilder b, Vector3 at, float yaw)
        {
            b.Push(at, yaw);
            const float s = 0.65f;
            KitParts.IBeam(b, new Vector3(-s, 0.05f, 0), new Vector3(s, 1.3f, 0), 0.12f, 0.1f, Mat.Rust);
            KitParts.IBeam(b, new Vector3(0, 0.05f, -s), new Vector3(0, 1.3f, s), 0.12f, 0.1f, Mat.Rust);
            KitParts.IBeam(b, new Vector3(s * 0.7f, 0.05f, s * 0.7f), new Vector3(-s * 0.7f, 1.3f, -s * 0.7f), 0.12f, 0.1f, Mat.Rust);
            b.Pop();
        }
    }
}
