using System.Collections.Generic;
using Deadswitch.Art.Models;
using Deadswitch.Art.World;
using Deadswitch.Game.Base;
using Deadswitch.Host.Reports;
using Deadswitch.Sim.State;
using UnityEngine;

namespace Deadswitch.Game.Reports
{
    /// <summary>
    /// Renders the four battle report panels (SPEC-006 rule 6) from the live compound: places raider silhouettes
    /// for each shot, renders an HDR still from the shot's camera and grades it with Deadswitch/Novel.
    /// The URP render call is injected by the Rendering assembly (<see cref="RenderCamera"/>).
    /// </summary>
    public static class ReportStills
    {
        /// <summary>Panel sizes in px (wide, tall, tall, wide), matching Report.uss.</summary>
        public static readonly Vector2Int[] Sizes = { new Vector2Int(1000, 400), new Vector2Int(492, 500), new Vector2Int(492, 500), new Vector2Int(1000, 400) };

        /// <summary>Renders a camera into a target. The Rendering assembly replaces this with a URP render request.</summary>
        public static System.Action<Camera, RenderTexture> RenderCamera = (cam, rt) =>
        {
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
        };

        public static RenderTexture[] Capture(BattleReport report, uint seed)
        {
            RaidGate gate = report.ContactGate != RaidGate.None ? report.ContactGate : report.PredictedGate;
            var root = new GameObject("Report Stills");
            var cam = new GameObject("Report Camera").AddComponent<Camera>();
            cam.transform.SetParent(root.transform, false);
            cam.enabled = false;
            cam.allowHDR = true;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            Camera main = DroneCamera.Instance != null ? DroneCamera.Instance.Camera : Camera.main;
            cam.backgroundColor = main != null ? main.backgroundColor : Color.black;

            Shader shader = Shader.Find("Deadswitch/Novel");
            var grade = shader != null ? new Material(shader) : null;
            if (grade != null && BaseView.Instance != null)
            {
                // night stills get more exposure (BaseLook.reportNightBoost) or the grade crushes them to black
                float night = BaseView.Instance.Lighting.moon;
                grade.SetFloat("_Exposure", grade.GetFloat("_Exposure") * Mathf.Lerp(1f, BaseLook.Load().reportNightBoost, night));
            }
            var stills = new RenderTexture[ReportScene.Shots];
            var meshes = new List<Mesh>();
            for (int shot = 0; shot < ReportScene.Shots; shot++)
            {
                var cast = new GameObject("Raiders " + shot);
                cast.transform.SetParent(root.transform, false);
                foreach (RaiderSpec r in ReportScene.Raiders(gate, shot, report.Outcome, seed))
                {
                    Mesh mesh = ArtBridge.ToMesh(Props.Raider(r.Seed, (int)report.Faction), "Raider", out Material[] mats);
                    meshes.Add(mesh);
                    var go = new GameObject("Raider");
                    go.transform.SetParent(cast.transform, false);
                    go.transform.localPosition = ArtBridge.V(r.Position);
                    go.transform.localRotation = Quaternion.Euler(0, r.Yaw, 0);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials = mats;
                }

                ShotSpec spec = ReportScene.Shot(gate, shot, report.Outcome, seed);
                Vector2Int size = Sizes[shot];
                cam.fieldOfView = spec.Fov;
                cam.aspect = size.x / (float)size.y;
                cam.transform.position = ArtBridge.V(spec.Position);
                cam.transform.LookAt(ArtBridge.V(spec.Target));

                RenderTexture raw = RenderTexture.GetTemporary(size.x, size.y, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                RenderCamera(cam, raw);
                stills[shot] = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "Report Panel " + shot };
                if (grade != null)
                {
                    Graphics.Blit(raw, stills[shot], grade);
                }
                else
                {
                    Graphics.Blit(raw, stills[shot]);
                }

                RenderTexture.ReleaseTemporary(raw);
                Object.DestroyImmediate(cast);
            }

            foreach (Mesh m in meshes)
            {
                Object.Destroy(m);
            }

            if (grade != null)
            {
                Object.Destroy(grade);
            }

            Object.Destroy(root);
            return stills;
        }

        public static void Release(RenderTexture[] stills)
        {
            if (stills == null)
            {
                return;
            }

            foreach (RenderTexture rt in stills)
            {
                if (rt != null)
                {
                    rt.Release();
                    Object.Destroy(rt);
                }
            }
        }
    }
}
