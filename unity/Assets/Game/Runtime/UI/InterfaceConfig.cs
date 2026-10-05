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
        public OpeningRules opening = new OpeningRules();
        public ToastRules toast = new ToastRules();
        public OrbRules orb = new OrbRules();
        public AmbientRules ambient = new AmbientRules();

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

            /// <summary>Real seconds between floating "+N" pickups over the top producer (idea 17).</summary>
            public float pickupSeconds = 20f;
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

        /// <summary>The opening film (SPEC-044): shots per beat, the staged Hub and how it falls, lights and waves.</summary>
        [System.Serializable]
        public sealed class OpeningRules
        {
            public int letterboxPct = 13;
            public float charsPerSecond = 30f;
            public float crtSeconds = 0.9f;
            public float waveDelay = 2.2f;
            public float waveSeconds = 2.6f;
            public float waveRadius = 70f;
            public float warImpactEvery = 0.55f;
            public float warShake = 0.45f;
            public float flareIntensity = 30f;
            public float flareRange = 36f;
            public float blastIntensity = 14f;
            public int mapSite = 2;
            public float mapPush = 0.28f;
            public float mapFog = 0.2f;
            public float mapFar = 1400f;
            public float restoreSeconds = 1.5f;
            public float restoreWalkX = 0.4f;
            public float restoreEyeHeight = 3.2f;
            public float restoreBack = 8f;
            public float restoreFov = 72f;
            public float restoreAimDrop = 2.4f;
            public float restoreFire = 0.35f;
            public float wakeHoldSeconds = 1.4f;
            public float wakeBreath = 1.6f;
            public Vector3 wakeCamera = new Vector3(0f, 40f, -40f);
            public float ringWidth = 0.9f;
            public string[] hubKinds = new string[0];
            public int[] hubLevels = new int[0];
            public int hubTier = 2;
            public int hubPeople = 40;
            public int ruinWreckage = 6;
            public OpeningShot[] shots = new OpeningShot[0];
        }

        /// <summary>
        /// One camera move (a beat may have several cuts): from, to, the point it watches, the lens, how long it runs,
        /// and what breaks on the staged Hub when it starts (<see cref="raze"/>: plots brought down; <see cref="hurt"/>:
        /// slot, damage pairs).
        /// </summary>
        [System.Serializable]
        public sealed class OpeningShot
        {
            public string mood = "signal";
            public Vector3 from;
            public Vector3 to;
            public Vector3 lookFrom;
            public Vector3 lookTo;
            public float fovFrom = 40f;
            public float fovTo = 40f;
            public float seconds = 4f;
            public int[] raze = new int[0];
            public int[] hurt = new int[0];
        }

        [System.Serializable]
        public sealed class ToastRules
        {
            public float seconds = 2.8f;
            public int max = 3;
        }

        [System.Serializable]
        public sealed class AmbientRules
        {
            /// <summary>Patrol drones circling the compound per Hub tier (idea 33).</summary>
            public int dronesPerTier = 2;
            public float droneHeight = 10f;
            public float droneRadius = 16f;
        }

        [System.Serializable]
        public sealed class OrbRules
        {
            public float ringSpeed = 0.35f;
            public float wobble = 0.08f;
        }
    }
}
