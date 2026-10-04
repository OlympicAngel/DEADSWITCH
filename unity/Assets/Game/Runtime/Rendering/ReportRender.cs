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

            // The mobile asset renders at 0.8 scale; into an off-screen target some passes then use the scaled size
            // and others the full one, so additional lights land only in an 80% rectangle. Stills render at 1.
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float scale = urp != null ? urp.renderScale : 1f;
            if (urp != null)
            {
                urp.renderScale = 1f;
            }

            try
            {
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
            finally
            {
                if (urp != null)
                {
                    urp.renderScale = scale;
                }
            }
        }
    }
}
