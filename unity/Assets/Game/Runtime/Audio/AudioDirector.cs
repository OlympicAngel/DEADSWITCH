using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Sim;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine;

namespace Deadswitch.Game.Audio
{
    /// <summary>
    /// The sound of the Hub (F-035, doc 07 s8, doc 10 s4 alert table). Ambient dread (wind, bunker drone, distant
    /// shelling, fire crackle near scars), tension music that rises with heat and threats and falls silent just
    /// before a big attack, signature alerts (raid triple pulse, siege rumble, purge siren, virus silence),
    /// battle sounds, and the AI's glitchy synthetic voice that degrades with corruption. All procedural.
    /// Volume follows the SOUND and MUSIC settings. Presentation only: reads the sim, never changes it.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private const int Voices = 6;

        private GameHost _host;
        private AudioSource _wind;
        private AudioSource _drone;
        private AudioSource _pad;
        private AudioSource _crackle;
        private AudioSource _siren;
        private readonly List<AudioSource> _oneShots = new List<AudioSource>();
        private int _nextOneShot;

        private AudioClip _raid;
        private AudioClip _siege;
        private AudioClip[] _far;
        private AudioClip[] _close;
        private AudioClip[] _guns;
        private AudioClip[] _static;
        private AudioClip _tick;
        private AudioClip _chime;
        private AudioClip _heartbeat;
        private AudioClip _powerUp;
        private AudioClip _restore;
        private AudioClip _subDrop;
        private AudioClip[][] _syllables;

        private float _nextShelling;
        private float _silenceUntil;
        private float _ambient = 1f;
        private float _music;
        private float _battleUntil;
        private float _nextBattleSound;
        private readonly Queue<float> _speech = new Queue<float>();
        private float _nextSyllable;
        private int _voicePack;
        private int _band;

        public static AudioDirector Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            gameObject.AddComponent<AudioListener>();
        }

        private void Start()
        {
            _host = GameHost.Instance;
            _wind = Loop(Synth.Wind(14f, 11));
            _drone = Loop(Synth.Drone(8f));
            _pad = Loop(Synth.Pad(16f));
            _crackle = Loop(Synth.Crackle(5f, 23));
            _siren = Loop(Synth.Siren());
            for (int i = 0; i < Voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                _oneShots.Add(s);
            }

            _raid = Synth.TriplePulse();
            _siege = Synth.Rumble(5);
            _far = new[] { Synth.Explosion(1, false), Synth.Explosion(2, false), Synth.Explosion(3, false) };
            _close = new[] { Synth.Explosion(4, true), Synth.Explosion(5, true) };
            _guns = new[] { Synth.Gunfire(6), Synth.Gunfire(7), Synth.Gunfire(8) };
            _static = new[] { Synth.Static(9), Synth.Static(10) };
            _tick = Synth.Tick(1800f, 0.05f);
            _chime = Synth.Tick(660f, 0.5f);
            _heartbeat = Synth.Heartbeat();
            _powerUp = Synth.PowerUp(12);
            _restore = Synth.Restore(13);
            _subDrop = Synth.SubDrop(14);

            BuildVoice();
            _host.Settings.Changed += () =>
            {
                if (_voicePack != _host.Settings.VoicePack)
                {
                    BuildVoice();
                }
            };

            _wind.Play();
            _drone.Play();
            _pad.Play();
            _crackle.Play();
            _nextShelling = Time.time + Random.Range(25f, 60f);
            _host.EventRaised += OnSimEvent;
        }

        private void OnDestroy()
        {
            if (_host != null)
            {
                _host.EventRaised -= OnSimEvent;
            }
        }

        /// <summary>A soft UI tick (buttons and toggles).</summary>
        public void Tick()
        {
            OneShot(_tick, 0.25f, Random.Range(0.97f, 1.03f));
        }

        /// <summary>A sound of the opening film (SPEC-043 s4).</summary>
        public void Opening(OpeningCue cue)
        {
            float master = _host != null ? _host.Settings.SoundPct / 100f : 1f;
            switch (cue)
            {
                case OpeningCue.Heartbeat:
                    OneShot(_heartbeat, 0.9f * master, 1f);
                    break;
                case OpeningCue.Static:
                    OneShot(_static[Random.Range(0, _static.Length)], 0.4f * master, Random.Range(0.9f, 1.1f));
                    break;
                case OpeningCue.FarImpact:
                    OneShot(_far[Random.Range(0, _far.Length)], 0.6f * master, Random.Range(0.85f, 1.1f));
                    break;
                case OpeningCue.Impact:
                    OneShot(_close[Random.Range(0, _close.Length)], 0.75f * master, Random.Range(0.9f, 1.05f));
                    break;
                case OpeningCue.PowerUp:
                    OneShot(_powerUp, 0.85f * master, 1f);
                    break;
                case OpeningCue.Restore:
                    OneShot(_restore, 0.7f * master, Random.Range(0.96f, 1.04f));
                    break;
                case OpeningCue.SubDrop:
                    OneShot(_subDrop, 1f * master, 1f);
                    break;
            }
        }

        /// <summary>Drains the ambience for this long from now (0 lets it back in), for the opening film.</summary>
        public void Hush(float seconds)
        {
            _silenceUntil = Time.time + seconds;
        }

        /// <summary>The AI starts speaking a line: a burst of synthetic syllables, more broken with corruption.</summary>
        public void Speak(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            _speech.Clear();
            int syllables = Mathf.Clamp(line.Length / 6, 2, 14);
            for (int i = 0; i < syllables; i++)
            {
                _speech.Enqueue(Random.Range(0.06f, 0.11f));
            }

            _nextSyllable = Time.time;
        }

        private void Update()
        {
            if (_host == null || !_host.IsReady)
            {
                return;
            }

            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;
            float master = _host.Settings.SoundPct / 100f;
            float music = _host.Settings.Music ? master : 0f;
            _band = (int)CorruptionSystem.Band(c, s.CorruptionMilli);

            // the quiet before (doc 07 s8): ambience and music drain away before a big attack lands
            bool bigInbound = s.RaidId != 0 && s.RaidKind != AttackKind.Raid && s.RaidArriveTick - s.Tick <= 15;
            bool hush = Time.time < _silenceUntil || bigInbound || s.PurgeStage == PurgeStage.Ultimatum;
            float ambientTarget = hush ? 0.06f : 1f;
            _ambient = Mathf.MoveTowards(_ambient, ambientTarget, Time.deltaTime * (hush ? 0.4f : 0.15f));

            // tension: hottest faction, an incoming attack, a waiting ultimatum
            int hottest = 0;
            for (int f = 0; f < s.Heat.Length; f++)
            {
                hottest = Mathf.Max(hottest, s.Heat[f]);
            }

            float tension = Mathf.Clamp01((hottest / 100_000f) + (s.RaidId != 0 ? 0.45f : 0f) + (s.Ultimatum == UltimatumStage.Issued ? 0.3f : 0f));
            float musicTarget = hush ? 0f : 0.08f + (0.42f * tension);
            _music = Mathf.MoveTowards(_music, musicTarget, Time.deltaTime * 0.08f);

            _wind.volume = 0.32f * _ambient * master;
            _drone.volume = 0.18f * _ambient * master;
            _pad.volume = _music * music;
            _pad.pitch = 1f + (0.06f * tension);

            int fires = 0;
            foreach (FacilitySlot f in s.Slots)
            {
                fires += f.Damage >= 2 ? f.Damage : 0;
            }

            bool fresh = s.ScarredAtTick > 0 && s.Tick - s.ScarredAtTick < (long)c.Scars.BurnHours * SimConfig.TicksPerHour;
            fires += fresh ? s.Wreckage : 0;
            _crackle.volume = Mathf.Clamp01(fires / 6f) * 0.35f * _ambient * master;

            bool purgeStrike = s.RaidId != 0 && s.RaidKind == AttackKind.Purge;
            if (purgeStrike && !_siren.isPlaying)
            {
                _siren.Play();
            }
            else if (!purgeStrike && _siren.isPlaying)
            {
                _siren.Stop();
            }

            _siren.volume = 0.5f * master;

            // distant shelling: the world beyond the wall is never quiet for long
            if (Time.time >= _nextShelling)
            {
                _nextShelling = Time.time + Random.Range(35f, 110f) / (1f + tension);
                if (!hush)
                {
                    OneShot(_far[Random.Range(0, _far.Length)], 0.35f * master, Random.Range(0.85f, 1.1f));
                }
            }

            // the fight at the wall
            if (Time.time < _battleUntil && Time.time >= _nextBattleSound)
            {
                _nextBattleSound = Time.time + Random.Range(0.6f, 1.6f);
                bool boom = Random.value < 0.35f;
                OneShot(boom ? _close[Random.Range(0, _close.Length)] : _guns[Random.Range(0, _guns.Length)], (boom ? 0.55f : 0.45f) * master, Random.Range(0.9f, 1.1f));
            }

            // the AI's voice: syllables while the line types out
            if (_speech.Count > 0 && Time.time >= _nextSyllable)
            {
                float gap = _speech.Dequeue();
                _nextSyllable = Time.time + gap;
                bool dropout = _band >= 2 && Random.value < 0.12f * _band;
                if (!dropout)
                {
                    AudioClip[] set = _syllables[Mathf.Clamp(_band, 0, 3)];
                    float waver = 1f + (Random.Range(-0.03f, 0.03f) * (1 + (_band * 2)));
                    OneShot(set[Random.Range(0, set.Length)], 0.22f * master, waver);
                }
            }
        }

        private void OnSimEvent(SimEvent e)
        {
            float master = _host.Settings.SoundPct / 100f;
            switch (e.Kind)
            {
                case EventKind.RaidWarning:
                    var kind = (AttackKind)e.D;
                    if (kind == AttackKind.Siege || kind == AttackKind.Warlord)
                    {
                        OneShot(_siege, 0.8f * master, 1f);
                    }

                    if (kind != AttackKind.Siege && kind != AttackKind.Purge)
                    {
                        OneShot(_raid, 0.55f * master, 1f);
                    }

                    break;
                case EventKind.BattleStarted:
                    _battleUntil = Time.time + (float)_host.SecondsUntilTick(_host.Sim.State.BattleEndTick) + 2f;
                    _nextBattleSound = Time.time;
                    break;
                case EventKind.BattleAbilityUsed:
                    OneShot(_close[Random.Range(0, _close.Length)], 0.7f * master, 0.9f);
                    OneShot(_far[Random.Range(0, _far.Length)], 0.5f * master, 1.2f);
                    break;
                case EventKind.RaidContact:
                    _battleUntil = Time.time + 6f;
                    _nextBattleSound = Time.time;
                    break;
                case EventKind.RaidResolved:
                    _battleUntil = Mathf.Min(_battleUntil, Time.time + 1f);
                    if (e.B == (int)RaidOutcome.Breached)
                    {
                        OneShot(_close[0], 0.75f * master, 0.85f);
                    }

                    break;
                case EventKind.FacilityScarred:
                    OneShot(_close[1], 0.6f * master, 0.95f);
                    break;
                case EventKind.VirusStruck:
                    // virus signature (doc 10 s4): silence, then the screen glitches
                    _silenceUntil = Time.time + 3f;
                    _ambient = 0.02f;
                    break;
                case EventKind.UltimatumIssued:
                case EventKind.DilemmaOffered:
                case EventKind.SpyLost:
                    OneShot(_static[Random.Range(0, _static.Length)], 0.4f * master, 1f);
                    break;
                case EventKind.BuildCompleted:
                case EventKind.ResearchCompleted:
                case EventKind.RepairDone:
                    OneShot(_chime, 0.3f * master, e.Kind == EventKind.ResearchCompleted ? 1.5f : 1f);
                    break;
            }
        }

        private AudioSource Loop(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0f;
            return s;
        }

        /// <summary>
        /// The AI's voice per corruption band: clean, then crushed and wavering. Voice packs (F-048, cosmetic) shift
        /// the register: LOW CARRIER sits a fourth lower, STATIC CHOIR higher and grainier from the start.
        /// </summary>
        private void BuildVoice()
        {
            _voicePack = _host.Settings.VoicePack;
            float[] shift = { 1f, 0.75f, 1.15f };
            float[] grain = { 0f, 0.05f, 0.25f };
            float[] pitches = { 196f, 220f, 247f, 262f, 294f, 330f };
            _syllables = new AudioClip[4][];
            for (int band = 0; band < 4; band++)
            {
                _syllables[band] = new AudioClip[pitches.Length];
                for (int p = 0; p < pitches.Length; p++)
                {
                    _syllables[band][p] = Synth.Syllable(pitches[p] * shift[_voicePack], Mathf.Min(1f, (band / 3f) + grain[_voicePack]), (band * 31) + p);
                }
            }
        }

        private void OneShot(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || volume <= 0.001f)
            {
                return;
            }

            AudioSource s = _oneShots[_nextOneShot];
            _nextOneShot = (_nextOneShot + 1) % _oneShots.Count;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume);
        }
    }

    /// <summary>The opening film's sounds (SPEC-043 s4).</summary>
    public enum OpeningCue
    {
        Heartbeat,
        Static,
        FarImpact,
        Impact,
        PowerUp,
        Restore,
        SubDrop,
    }
}
