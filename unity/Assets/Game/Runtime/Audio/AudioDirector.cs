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
        private AudioClip _whine;
        private AudioClip _swell;
        private OpeningBed _bed;
        private System.Threading.Tasks.Task<float[][]> _scoreTask;
        private AudioSource[] _score;
        private float[] _scoreLevel;
        private readonly float[] _bedLevel = new float[5];
        private float _nextBedShot;
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
            _whine = Synth.Whine();
            _swell = Synth.Swell(15);

            BuildVoice();

            // the opening's score is pure math: compose it off the main thread while the app starts
            _scoreTask = System.Threading.Tasks.Task.Run(Score.Render);
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
                case OpeningCue.Whine:
                    OneShot(_whine, 0.5f * master, 1f);
                    break;
                case OpeningCue.Swell:
                    OneShot(_swell, 0.8f * master, 1f);
                    break;
                case OpeningCue.Blip:
                    OneShot(_tick, 0.35f * master, Random.Range(1.3f, 1.8f));
                    break;
                case OpeningCue.Radio:
                    OneShot(_static[Random.Range(0, _static.Length)], 0.5f * master, Random.Range(1.05f, 1.25f));
                    _speech.Clear();
                    for (int i = 0; i < 7; i++)
                    {
                        _speech.Enqueue(Random.Range(0.05f, 0.09f));
                    }

                    _nextSyllable = Time.time + 0.25f;
                    break;
            }
        }

        /// <summary>
        /// The opening film's sound bed (SPEC-044): while set, the ambient loops follow the beat instead of the run
        /// (pad, drone, wind, crackle, siren; battle adds guns and blasts). None hands them back to the run.
        /// </summary>
        public void SetOpeningBed(OpeningBed bed)
        {
            _bed = bed;
            _nextBedShot = Time.time + 0.3f;
        }

        /// <summary>Loop levels for a bed: pad, drone, wind, crackle, siren.</summary>
        private static float[] BedLevels(OpeningBed bed)
        {
            switch (bed)
            {
                case OpeningBed.Space: return new[] { 0.55f, 0.05f, 0f, 0f, 0f };
                case OpeningBed.Room: return new[] { 0.2f, 0.12f, 0f, 0f, 0f };
                case OpeningBed.Alarm: return new[] { 0f, 0.15f, 0f, 0f, 0.3f };
                case OpeningBed.War: return new[] { 0f, 0.2f, 0f, 0f, 0f };
                case OpeningBed.Cold: return new[] { 0.15f, 0.05f, 0.25f, 0f, 0f };
                case OpeningBed.Night: return new[] { 0f, 0.05f, 0.3f, 0.2f, 0f };
                case OpeningBed.Calm: return new[] { 0.4f, 0f, 0.15f, 0f, 0f };
                case OpeningBed.Battle: return new[] { 0f, 0.1f, 0.1f, 0.3f, 0.25f };
                case OpeningBed.Ash: return new[] { 0f, 0.05f, 0.4f, 0.35f, 0f };
                case OpeningBed.Dread: return new[] { 0f, 0.15f, 0.2f, 0.2f, 0f };
                case OpeningBed.Ruin: return new[] { 0.15f, 0.05f, 0.25f, 0.2f, 0f };
                case OpeningBed.Hope: return new[] { 0.2f, 0f, 0.25f, 0.2f, 0f };
                case OpeningBed.Wake: return new[] { 0.3f, 0f, 0.1f, 0.1f, 0f };
                default: return new[] { 0f, 0f, 0f, 0f, 0f };
            }
        }

        /// <summary>How loud each score stem plays under a bed (see <see cref="Score.Stem"/>).</summary>
        private static float[] ScoreLevels(OpeningBed bed)
        {
            var l = new float[9];
            switch (bed)
            {
                case OpeningBed.Space: l[(int)Score.Stem.Orbit] = 1f; break;
                case OpeningBed.Room: l[(int)Score.Stem.Command] = 1f; l[(int)Score.Stem.Orbit] = 0.25f; break;
                case OpeningBed.Alarm: l[(int)Score.Stem.Launch] = 1f; l[(int)Score.Stem.Command] = 0.4f; break;
                case OpeningBed.War: l[(int)Score.Stem.Launch] = 1f; l[(int)Score.Stem.Battle] = 0.5f; break;
                case OpeningBed.Cold: l[(int)Score.Stem.Dark] = 1f; break;
                case OpeningBed.Night: l[(int)Score.Stem.Dark] = 0.6f; l[(int)Score.Stem.Ash] = 0.4f; break;
                case OpeningBed.Calm: l[(int)Score.Stem.Hold] = 1f; break;
                case OpeningBed.Battle: l[(int)Score.Stem.Battle] = 1f; l[(int)Score.Stem.Launch] = 0.45f; break;
                case OpeningBed.Ash: l[(int)Score.Stem.Ash] = 1f; break;
                case OpeningBed.Dread: l[(int)Score.Stem.Ash] = 1f; l[(int)Score.Stem.Dark] = 0.35f; break;
                case OpeningBed.Ruin: l[(int)Score.Stem.Restore] = 0.9f; l[(int)Score.Stem.Ash] = 0.35f; break;
                case OpeningBed.Hope: l[(int)Score.Stem.Ash] = 0.5f; l[(int)Score.Stem.Orbit] = 0.55f; break;
                case OpeningBed.Wake: l[(int)Score.Stem.Wake] = 1f; break;
            }

            return l;
        }

        /// <summary>Starts every stem in the same DSP instant once the score is composed, so crossfades stay in time.</summary>
        private void TickScore(float music)
        {
            if (_score == null)
            {
                if (_scoreTask == null || !_scoreTask.IsCompleted)
                {
                    return;
                }

                if (_scoreTask.Status != System.Threading.Tasks.TaskStatus.RanToCompletion)
                {
                    Debug.LogError("AudioDirector: score failed: " + _scoreTask.Exception);
                    _scoreTask = null;
                    return;
                }

                float[][] stems = _scoreTask.Result;
                _scoreTask = null;
                _score = new AudioSource[stems.Length];
                _scoreLevel = new float[stems.Length];
                double at = AudioSettings.dspTime + 0.2;
                for (int i = 0; i < stems.Length; i++)
                {
                    AudioClip clip = AudioClip.Create("Score " + (Score.Stem)i, stems[i].Length, 1, Score.Rate, false);
                    clip.SetData(stems[i], 0);
                    var src = gameObject.AddComponent<AudioSource>();
                    src.clip = clip;
                    src.loop = true;
                    src.volume = 0f;
                    src.playOnAwake = false;
                    src.PlayScheduled(at);
                    _score[i] = src;
                }
            }

            float[] target = ScoreLevels(_bed);
            for (int i = 0; i < _score.Length; i++)
            {
                // cross-fades over about a bar; the end of the film fades the score out slower
                float rate = _bed == OpeningBed.None ? 0.25f : 0.6f;
                _scoreLevel[i] = Mathf.MoveTowards(_scoreLevel[i], target[i], Time.deltaTime * rate);
                _score[i].volume = _scoreLevel[i] * music * 0.75f;
            }
        }

        private void TickBed(float master, float music)
        {
            float[] target = BedLevels(_bed);
            for (int i = 0; i < _bedLevel.Length; i++)
            {
                _bedLevel[i] = Mathf.MoveTowards(_bedLevel[i], target[i], Time.deltaTime * 0.5f);
            }

            // the score carries the music while it plays; the old pad only stands in until it is composed
            _pad.volume = _bedLevel[0] * music * (_score != null ? 0f : 1f);
            _pad.pitch = _bed == OpeningBed.Dread ? 0.8f : 1f;
            _drone.volume = _bedLevel[1] * master;
            _drone.pitch = _bed == OpeningBed.Dread ? 0.7f : 1f;
            _wind.volume = _bedLevel[2] * master;
            _crackle.volume = _bedLevel[3] * master;
            _siren.volume = _bedLevel[4] * master;
            if (_bedLevel[4] > 0.01f && !_siren.isPlaying)
            {
                _siren.Play();
            }
            else if (_bedLevel[4] <= 0.01f && _siren.isPlaying)
            {
                _siren.Stop();
            }

            // the fight at the wall: guns, close blasts
            if (_bed == OpeningBed.Battle && Time.time >= _nextBedShot)
            {
                _nextBedShot = Time.time + Random.Range(0.25f, 0.7f);
                OneShot(_guns[Random.Range(0, _guns.Length)], 0.25f * master, Random.Range(0.9f, 1.1f));
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

            TickScore(music);
            if (_bed != OpeningBed.None)
            {
                TickBed(master, music);
                TickSpeech();
                return;
            }

            _wind.volume = 0.32f * _ambient * master;
            _drone.volume = 0.18f * _ambient * master;
            _pad.volume = _music * music;
            _pad.pitch = 1f + (0.06f * tension);
            _drone.pitch = 1f;

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

            TickSpeech();
        }

        /// <summary>The AI's voice: syllables while the line types out.</summary>
        private void TickSpeech()
        {
            if (_speech.Count == 0 || Time.time < _nextSyllable)
            {
                return;
            }

            float master = _host.Settings.SoundPct / 100f;
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
        Whine,
        Swell,
        Blip,
        Radio,
    }

    /// <summary>The opening film's ambient beds (SPEC-044).</summary>
    public enum OpeningBed
    {
        None,
        Space,
        Room,
        Alarm,
        War,
        Cold,
        Night,
        Calm,
        Battle,
        Ash,
        Dread,
        Ruin,
        Silence,
        Hope,
        Wake,
    }
}
