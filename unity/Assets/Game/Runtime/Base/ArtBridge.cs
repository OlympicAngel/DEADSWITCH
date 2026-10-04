using System.Collections.Generic;
using Deadswitch.Art.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// Converts engine-agnostic art (Deadswitch.Art) to Unity meshes and materials. One material per palette
    /// slot, plus "off" variants for lamps (unpowered facilities go dark).
    /// </summary>
    public static class ArtBridge
    {
        private static Material[] _on;
        private static Material[] _off;

        public static Mesh ToMesh(MeshData data, string name, out Material[] materials, bool powered = true)
        {
            var mesh = new Mesh { name = name };
            if (data.VertexCount > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            var positions = new List<Vector3>(data.VertexCount);
            var normals = new List<Vector3>(data.VertexCount);
            var colors = new List<Color>(data.VertexCount);
            for (int i = 0; i < data.VertexCount; i++)
            {
                System.Numerics.Vector3 p = data.Positions[i];
                System.Numerics.Vector3 n = data.Normals[i];
                System.Numerics.Vector4 c = data.Colors[i];
                positions.Add(new Vector3(p.X, p.Y, p.Z));
                normals.Add(new Vector3(n.X, n.Y, n.Z));
                colors.Add(new Color(c.X, c.Y, c.Z, c.W));
            }

            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);

            var used = new List<int>();
            for (int m = 0; m < data.Indices.Length; m++)
            {
                if (data.Indices[m].Count > 0)
                {
                    used.Add(m);
                }
            }

            mesh.subMeshCount = used.Count;
            materials = new Material[used.Count];
            Material[] set = powered ? On : Off;
            for (int s = 0; s < used.Count; s++)
            {
                mesh.SetTriangles(data.Indices[used[s]], s, false);
                materials[s] = set[used[s]];
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Material set with lamps lit.</summary>
        public static Material[] On
        {
            get
            {
                Build();
                return _on;
            }
        }

        /// <summary>Material set with lamps dark.</summary>
        public static Material[] Off
        {
            get
            {
                Build();
                return _off;
            }
        }

        /// <summary>Same submesh layout, other lamp state.</summary>
        public static Material[] Swap(Material[] current, bool powered)
        {
            Build();
            var result = new Material[current.Length];
            for (int i = 0; i < current.Length; i++)
            {
                int slot = System.Array.IndexOf(_on, current[i]);
                if (slot < 0)
                {
                    slot = System.Array.IndexOf(_off, current[i]);
                }

                result[i] = slot < 0 ? current[i] : (powered ? _on[slot] : _off[slot]);
            }

            return result;
        }

        private static void Build()
        {
            if (_on != null)
            {
                return;
            }

            Shader shader = Resources.Load<Shader>("Shaders/DeadswitchLit");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[DEADSWITCH] Vertex-color shader unavailable; falling back to URP Lit (no weathering).");
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            const float emissionScale = 1f; // time of day scales emission globally (_DsEmissionScale, BaseView)
            Shader coneShader = Resources.Load<Shader>("Shaders/DeadswitchLightCone");
            _on = new Material[Palette.Count];
            _off = new Material[Palette.Count];
            for (int i = 0; i < Palette.Count; i++)
            {
                MaterialDef d = Palette.Get((Mat)i);
                if ((Mat)i == Mat.LightCone && coneShader != null)
                {
                    var cone = new Material(coneShader) { name = "DS LightCone" };
                    cone.SetVector("_Color", Linear(d.EmissionColor.X, d.EmissionColor.Y, d.EmissionColor.Z, 1f));
                    cone.SetFloat("_Intensity", 1f);
                    _on[i] = cone;
                    _off[i] = cone;
                    continue;
                }

                _on[i] = Make(shader, d, true, emissionScale);
                _off[i] = d.IsLamp ? Make(shader, d, false, emissionScale) : _on[i];
            }
        }

        private static Material Make(Shader shader, MaterialDef d, bool lit, float emissionScale)
        {
            var m = new Material(shader) { name = "DS " + d.Name + (lit ? string.Empty : " (off)"), enableInstancing = true };
            m.SetVector("_BaseColor", Linear(d.BaseColor.X, d.BaseColor.Y, d.BaseColor.Z, 1f));
            m.SetFloat("_Metallic", d.Metallic);
            m.SetFloat("_Smoothness", d.Smoothness);
            Wear w = d.Wear;
            bool procedural = w.Ground || (w.Chip + w.Rust + w.Dirt + w.Streak + w.Bump) > 0f;
            m.SetVector("_BareColor", Linear(w.Bare.X, w.Bare.Y, w.Bare.Z, 1f));
            m.SetVector("_Wear", new Vector4(w.Chip, w.Rust, w.Dirt, w.Streak));
            m.SetVector("_WearB", new Vector4(w.Bump, w.Scale, w.Ground ? 1f : 0f, procedural ? 1f : 0f));
            if (lit && d.EmissionIntensity > 0f)
            {
                Vector4 e = Linear(d.EmissionColor.X, d.EmissionColor.Y, d.EmissionColor.Z, 1f) * (d.EmissionIntensity * emissionScale);
                m.SetVector("_EmissionColor", e);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                m.SetVector("_EmissionColor", Vector4.zero);
            }

            return m;
        }

        /// <summary>sRGB -> shader value (SetVector bypasses Unity's own color conversion).</summary>
        public static Vector4 Linear(float r, float g, float b, float a)
        {
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                return new Vector4(Mathf.GammaToLinearSpace(r), Mathf.GammaToLinearSpace(g), Mathf.GammaToLinearSpace(b), a);
            }

            return new Vector4(r, g, b, a);
        }

        public static Vector3 V(System.Numerics.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
    }
}
