using System.Collections.Generic;
using System.Numerics;

namespace Deadswitch.Art.Geometry
{
    /// <summary>
    /// Plain mesh data in Unity's convention (left-handed, Y up, triangles clockwise from the front, i.e.
    /// cross(b - a, c - a) points outward). One index list per material slot. Vertex colors carry weathering.
    /// </summary>
    public sealed class MeshData
    {
        public MeshData()
        {
            Indices = new List<int>[Palette.Count];
            for (int i = 0; i < Indices.Length; i++)
            {
                Indices[i] = new List<int>();
            }
        }

        public List<Vector3> Positions { get; } = new List<Vector3>();

        public List<Vector3> Normals { get; } = new List<Vector3>();

        public List<Vector4> Colors { get; } = new List<Vector4>();

        public List<int>[] Indices { get; }

        public int VertexCount => Positions.Count;

        public bool IsEmpty => Positions.Count == 0;

        public int AddVertex(Vector3 p, Vector3 n, Vector4 c)
        {
            Positions.Add(p);
            Normals.Add(n);
            Colors.Add(c);
            return Positions.Count - 1;
        }

        public void AddTriangle(Mat m, int a, int b, int c)
        {
            List<int> list = Indices[(int)m];
            list.Add(a);
            list.Add(b);
            list.Add(c);
        }

        /// <summary>Axis-aligned bounds (min, max). Returns zero vectors when empty.</summary>
        public void Bounds(out Vector3 min, out Vector3 max)
        {
            min = Vector3.Zero;
            max = Vector3.Zero;
            for (int i = 0; i < Positions.Count; i++)
            {
                min = i == 0 ? Positions[i] : Vector3.Min(min, Positions[i]);
                max = i == 0 ? Positions[i] : Vector3.Max(max, Positions[i]);
            }
        }
    }
}
