using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deadswitch.Game.Rendering
{
    /// <summary>
    /// Renders the sector map (SPEC-033) through URP: a render request into the map texture with the post stack on
    /// (same tonemapping and bloom as the drone feed), full render scale, and a shadow distance long enough for the
    /// far recon camera. Falls back to Camera.Render where render requests are unsupported.
    /// </summary>
    public static class MapRender
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Bootstrap.Booted += _ => MapView.RenderCamera = Render;
        }

        private static void Render(Camera cam, RenderTexture target, float shadowDistance)
        {
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float scale = urp != null ? urp.renderScale : 1f;
            float shadows = urp != null ? urp.shadowDistance : 0f;
            if (urp != null)
            {
                urp.renderScale = 1f;
                urp.shadowDistance = shadowDistance;
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
                    urp.shadowDistance = shadows;
                }
            }
        }
    }
}
