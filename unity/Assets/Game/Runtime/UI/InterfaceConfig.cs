using UnityEngine;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Interface tunables (SPEC-039): resource thresholds, camera focus, cinematic shot lengths, toasts and the core
    /// orb. Loaded once from <c>Resources/UI/Interface.json</c>; values are presentation only (tune).
    /// </summary>
    [System.Serializable]
    public sealed class InterfaceConfig
    {
        private static InterfaceConfig _current;

        public ResourceRules resources = new ResourceRules();
        public FocusRules focus = new FocusRules();
        public CameraRules camera = new CameraRules();
        public CinematicRules cinematic = new CinematicRules();
        public ToastRules toast = new ToastRules();
        public OrbRules orb = new OrbRules();

        public static InterfaceConfig Current => _current ??= Load();

        private static InterfaceConfig Load()
        {
            var asset = Resources.Load<TextAsset>("UI/Interface");
            if (asset == null)
            {
                Debug.LogError("InterfaceConfig: Resources/UI/Interface.json is missing; using defaults");
                return new InterfaceConfig();
            }

            return JsonUtility.FromJson<InterfaceConfig>(asset.text);
        }

        [System.Serializable]
        public sealed class ResourceRules
        {
            /// <summary>Below this share of capacity a pod reads LOW.</summary>
            public int lowPct = 20;

            /// <summary>A draining pod that runs dry within this many hours reads SHORT.</summary>
            public int shortHours = 2;

            /// <summary>At or above this share of capacity (and still producing) a pod reads FULL.</summary>
            public int fullPct = 98;
        }

        [System.Serializable]
        public sealed class FocusRules
        {
            /// <summary>Zoom factor while a facility is framed (1 = the default drone distance).</summary>
            public float zoom = 0.52f;

            /// <summary>Azimuth swing toward a three-quarter view of the framed facility.</summary>
            public float orbitDegrees = 16f;

            public float seconds = 0.75f;

            /// <summary>How far up the screen the framed facility sits (share of height) to leave room for the card.</summary>
            public float screenLift = 0.16f;
        }

        [System.Serializable]
        public sealed class CameraRules
        {
            public float minZoom = 0.45f;
            public float maxZoom = 1.3f;
            public float doubleTapZoom = 0.7f;
            public float doubleTapSeconds = 0.3f;
            public float inertiaDamping = 4.5f;
            public float idleOrbitAfter = 20f;
            public float idleOrbitDegreesPerSecond = 1.2f;
        }

        [System.Serializable]
        public sealed class CinematicRules
        {
            public int letterboxPct = 11;
            public float titleSeconds = 2.2f;
            public float gateSeconds = 2.8f;
            public float dollySeconds = 3f;
            public float pullbackSeconds = 1.6f;
            public float aftermathSeconds = 3.2f;
            public float shake = 0.35f;
            public float fov = 32f;
        }

        [System.Serializable]
        public sealed class ToastRules
        {
            public float seconds = 2.8f;
            public int max = 3;
        }

        [System.Serializable]
        public sealed class OrbRules
        {
            public float ringSpeed = 0.35f;
            public float wobble = 0.08f;
        }
    }
}
