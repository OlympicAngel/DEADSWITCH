using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Deadswitch.Art.Geometry;
using Deadswitch.Art.Models;
using Deadswitch.Art.World;
using Deadswitch.Host.Narrative;
using Deadswitch.Host.Reports;
using Deadswitch.Sim;
using Deadswitch.Sim.State;

namespace Deadswitch.Cli
{
    /// <summary>
    /// Writes the 3D Hub for the headless three.js preview (tools/basepreview). Converts Unity's left-handed
    /// space to three.js right-handed (z -> -z, triangle winding reversed).
    /// </summary>
    public static class ArtExport
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static void Write(Simulation sim, string path, uint seed, SlotView[]? layout = null, BattleReport? report = null, int tier = 1, int wreckage = 0, bool burning = false, int factionOverride = -1)
        {
            var scene = new SceneBuilder();
            void AddModel(string name, Model model, Vector3 pos, float yaw, bool powered, bool unmanned, ScarSet? fx = null) => scene.Add(name, model, pos, yaw, powered, unmanned, fx);

            int slots = layout?.Length ?? sim.State.Slots.Count;
            AddModel("terrain", new Model { Static = HubScene.Terrain(seed) }, Vector3.Zero, 0, true, false);
            AddModel("surroundings", HubScene.Surroundings(seed, slots, tier), Vector3.Zero, 0, true, false);
            AddModel("core", Core.Build(seed), Vector3.Zero, 0, true, false);
            for (int i = 0; i < slots; i++)
            {
                Vector3 pos = HubScene.SlotPosition(i, slots);
                float yaw = HubScene.SlotYaw(i, slots);
                SlotView v = layout != null ? layout[i] : SlotView.From(sim.State, i);
                AddModel("pad" + i, new Model { Static = Facilities.Pad(seed + (uint)i, v.Kind == FacilityKind.None && !v.UnderConstruction) }, pos, yaw, true, false);
                if (v.Kind != FacilityKind.None)
                {
                    Model facility = Facilities.Build(v.Kind, v.Level, seed + (uint)(i * 31));
                    AddModel("slot" + i, facility, pos, yaw, v.Powered, v.Unmanned);
                    if (v.Damage > 0)
                    {
                        facility.Static.Bounds(out Vector3 min, out Vector3 max);
                        ScarSet scars = Scars.Facility(v.Damage, min, max, seed + (uint)(i * 53));
                        AddModel("scars" + i, scars.Model, pos, yaw, true, false, scars);
                    }
                }

                if (v.UnderConstruction)
                {
                    AddModel("scaffold" + i, new Model { Static = Facilities.Scaffold(2.8f, seed + (uint)i) }, pos, yaw, true, false);
                }
            }

            for (int i = 0; i < Math.Min(wreckage, Scars.SpotCount); i++)
            {
                Vector3 spot = Scars.Spot(i);
                spot.Y = HubScene.Height(spot.X, spot.Z, seed);
                ScarSet wreck = Scars.Wreck(i, burning && i >= wreckage - 2, seed);
                AddModel("wreck" + i, wreck.Model, spot, Scars.SpotYaw(i), true, false, wreck);
            }

            Vector3[] walk = HubScene.WalkPoints(slots);
            int people = Math.Min(walk.Length, Math.Max(2, sim.State.People / 3));
            for (int i = 0; i < people; i++)
            {
                AddModel("person" + i, new Model { Static = Props.Person(seed + 900 + (uint)i) }, walk[(i * 5) % walk.Length], (i * 73) % 360, true, false);
            }

            var reportJson = new StringBuilder();
            if (report != null)
            {
                RaidGate gate = report.ContactGate != RaidGate.None ? report.ContactGate : report.PredictedGate;
                reportJson.Append(",\n\"report\":{\"title\":").Append(Str("AFTER-ACTION // " + Names.Attack(report.Kind) + " " + report.RaidId + " // " + report.Outcome.ToString().ToUpperInvariant()))
                    .Append(",\"vector\":").Append(Str("PREDICTED " + Names.Gate(report.PredictedGate) + "  //  CONTACT " + Names.Gate(report.ContactGate)))
                    .Append(",\"mismatch\":").Append(report.PredictionMismatch ? "true" : "false")
                    .Append(",\"summary\":").Append(Str(report.Summary))
                    .Append(",\"ledger\":").Append(Str(report.LossText(false)))
                    .Append(",\"shots\":[");
                for (int shot = 0; shot < ReportScene.Shots; shot++)
                {
                    ShotSpec spec = ReportScene.Shot(gate, shot, report.Outcome, seed);
                    reportJson.Append(shot > 0 ? "," : string.Empty).Append("{\"pos\":").Append(V(spec.Position)).Append(",\"target\":").Append(V(spec.Target))
                        .Append(",\"fov\":").Append(F(spec.Fov)).Append(",\"caption\":").Append(Str(report.Panels[shot].Caption)).Append('}');
                    int n = 0;
                    foreach (RaiderSpec r in ReportScene.Raiders(gate, shot, report.Outcome, seed))
                    {
                        AddModel("raider" + shot + "_" + n++, new Model { Static = Props.Raider(r.Seed, factionOverride >= 0 ? factionOverride : (int)report.Faction) }, r.Position, r.Yaw, true, false);
                    }
                }

                reportJson.Append("]}");
            }

            File.WriteAllText(path, scene.Json(reportJson.ToString()));
        }

        /// <summary>
        /// The sector map (SPEC-033) for the preview: terrain and landmarks, the fixed camera for a picture of
        /// <paramref name="aspect"/>, and the look's map block (fog scale, shadow extent).
        /// </summary>
        public static void WriteMap(Simulation sim, string path, uint seed, float aspect)
        {
            var scene = new SceneBuilder();
            foreach (SectorObject o in SectorScene.Build(seed))
            {
                scene.Add(o.Name.Replace(' ', '_'), o.Model, o.Position, 0, true, false);
            }

            CameraPose cam = SectorScene.Camera(aspect);
            var extra = new StringBuilder();
            extra.Append(",\n\"camera\":{\"pos\":").Append(V(cam.Position)).Append(",\"target\":").Append(V(cam.Target)).Append(",\"fov\":").Append(F(cam.Fov)).Append('}')
                .Append(",\n\"map\":true");
            File.WriteAllText(path, scene.Json(extra.ToString()));
        }

        /// <summary>Collects objects and meshes, then writes the preview's scene JSON.</summary>
        private sealed class SceneBuilder
        {
            private readonly List<MeshData> _meshes = new List<MeshData>();
            private readonly StringBuilder _objects = new StringBuilder();
            private int _count;

            public void Add(string name, Model model, Vector3 pos, float yaw, bool powered, bool unmanned, ScarSet? fx = null)
            {
                int meshIndex = _meshes.Count;
                _meshes.Add(model.Static);
                int coneIndex = -1;
                if (!model.Cones.IsEmpty)
                {
                    coneIndex = _meshes.Count;
                    _meshes.Add(model.Cones);
                }

                var parts = new StringBuilder();
                foreach (AnimPart p in model.Parts)
                {
                    _meshes.Add(p.Mesh);
                    parts.Append(parts.Length > 0 ? "," : string.Empty)
                        .Append("{\"mesh\":").Append(_meshes.Count - 1)
                        .Append(",\"pivot\":").Append(V(p.Pivot))
                        .Append(",\"kind\":").Append((int)p.Kind)
                        .Append(",\"speed\":").Append(F(p.Speed))
                        .Append(",\"range\":").Append(F(p.Range)).Append('}');
                }

                var lights = new StringBuilder();
                foreach (LightSpec l in model.Lights)
                {
                    lights.Append(lights.Length > 0 ? "," : string.Empty)
                        .Append("{\"pos\":").Append(V(l.Position))
                        .Append(",\"color\":[").Append(F(l.Color.X)).Append(',').Append(F(l.Color.Y)).Append(',').Append(F(l.Color.Z)).Append(']')
                        .Append(",\"intensity\":").Append(F(l.Intensity))
                        .Append(",\"range\":").Append(F(l.Range))
                        .Append(",\"role\":").Append((int)l.Role).Append('}');
                }

                _objects.Append(_count++ > 0 ? ",\n" : string.Empty)
                    .Append("{\"name\":\"").Append(name).Append("\",\"mesh\":").Append(meshIndex)
                    .Append(",\"cones\":").Append(coneIndex)
                    .Append(",\"position\":").Append(V(pos))
                    .Append(",\"yaw\":").Append(F(-yaw))
                    .Append(",\"powered\":").Append(powered ? "true" : "false")
                    .Append(",\"unmanned\":").Append(unmanned ? "true" : "false")
                    .Append(",\"parts\":[").Append(parts).Append("],\"lights\":[").Append(lights).Append(']');
                if (fx != null)
                {
                    _objects.Append(",\"fires\":[").Append(string.Join(",", fx.Fires.Select(V))).Append("],\"smokes\":[").Append(string.Join(",", fx.Smokes.Select(V)))
                        .Append("],\"smokeLevel\":").Append(fx.SmokeLevel);
                }

                _objects.Append('}');
            }

            public string Json(string extra)
            {
                var sb = new StringBuilder();
                sb.Append("{\"palette\":[");
                for (int i = 0; i < Palette.Count; i++)
                {
                    MaterialDef d = Palette.Get((Mat)i);
                    sb.Append(i > 0 ? "," : string.Empty)
                        .Append("{\"name\":\"").Append(d.Name).Append("\",\"color\":").Append(V3(d.BaseColor))
                        .Append(",\"metallic\":").Append(F(d.Metallic)).Append(",\"smoothness\":").Append(F(d.Smoothness))
                        .Append(",\"emission\":").Append(V3(d.EmissionColor)).Append(",\"emissionIntensity\":").Append(F(d.EmissionIntensity))
                        .Append(",\"lamp\":").Append(d.IsLamp ? "true" : "false")
                        .Append(",\"wear\":{\"chip\":").Append(F(d.Wear.Chip)).Append(",\"rust\":").Append(F(d.Wear.Rust))
                        .Append(",\"dirt\":").Append(F(d.Wear.Dirt)).Append(",\"streak\":").Append(F(d.Wear.Streak))
                        .Append(",\"bump\":").Append(F(d.Wear.Bump)).Append(",\"scale\":").Append(F(d.Wear.Scale))
                        .Append(",\"bare\":").Append(V3(d.Wear.Bare)).Append(",\"ground\":").Append(d.Wear.Ground ? "true" : "false").Append("}}");
                }

                sb.Append("],\n\"objects\":[\n").Append(_objects).Append("],\n\"meshes\":[\n");
                for (int i = 0; i < _meshes.Count; i++)
                {
                    sb.Append(i > 0 ? ",\n" : string.Empty);
                    Mesh(sb, _meshes[i]);
                }

                sb.Append(']').Append(extra).Append("}\n");
                return sb.ToString();
            }
        }

        private static void Mesh(StringBuilder sb, MeshData m)
        {
            sb.Append("{\"positions\":[");
            for (int i = 0; i < m.Positions.Count; i++)
            {
                Vector3 p = m.Positions[i];
                sb.Append(i > 0 ? "," : string.Empty).Append(F(p.X)).Append(',').Append(F(p.Y)).Append(',').Append(F(-p.Z));
            }

            sb.Append("],\"normals\":[");
            for (int i = 0; i < m.Normals.Count; i++)
            {
                Vector3 n = m.Normals[i];
                sb.Append(i > 0 ? "," : string.Empty).Append(F(n.X)).Append(',').Append(F(n.Y)).Append(',').Append(F(-n.Z));
            }

            sb.Append("],\"colors\":[");
            for (int i = 0; i < m.Colors.Count; i++)
            {
                Vector4 c = m.Colors[i];
                sb.Append(i > 0 ? "," : string.Empty).Append(F(c.X)).Append(',').Append(F(c.Y)).Append(',').Append(F(c.Z));
            }

            sb.Append("],\"groups\":[");
            bool first = true;
            for (int mat = 0; mat < m.Indices.Length; mat++)
            {
                List<int> idx = m.Indices[mat];
                if (idx.Count == 0)
                {
                    continue;
                }

                sb.Append(first ? string.Empty : ",").Append("{\"mat\":").Append(mat).Append(",\"indices\":[");
                first = false;
                for (int t = 0; t < idx.Count; t += 3)
                {
                    sb.Append(t > 0 ? "," : string.Empty).Append(idx[t]).Append(',').Append(idx[t + 2]).Append(',').Append(idx[t + 1]);
                }

                sb.Append("]}");
            }

            sb.Append("]}");
        }

        private static string F(float v)
        {
            return Math.Round(v, 4).ToString("0.####", Inv);
        }

        private static string V(Vector3 v)
        {
            return "[" + F(v.X) + "," + F(v.Y) + "," + F(-v.Z) + "]";
        }

        private static string Str(string text)
        {
            return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string V3(Vector3 v)
        {
            return "[" + F(v.X) + "," + F(v.Y) + "," + F(v.Z) + "]";
        }
    }
}
