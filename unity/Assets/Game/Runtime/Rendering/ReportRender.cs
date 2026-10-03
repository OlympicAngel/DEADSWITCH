using Deadswitch.Game.Core;
using Deadswitch.Game.Reports;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deadswitch.Game.Rendering
{
    /// <summary>
    /// Renders battle report stills through URP (render request into the HDR target, no post: the Novel grade
    /// does exposure and tonemapping). Falls back to Camera.Render where render requests are unsupported.
    /// </summary>
    public static class ReportRender
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Bootstrap.Booted += _ => ReportStills.RenderCamera = Render;
        }

        private static void Render(Camera cam, RenderTexture target)
        {
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                RenderPipeline.SubmitRenderRequest(cam, request);
                return;
            }

            cam.targetTexture = target;
            cam.Render();
            cam.targetTexture = null;
        }
    }
}
