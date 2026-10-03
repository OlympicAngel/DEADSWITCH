using System.Collections.Generic;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Persistence
{
    /// <summary>Header fields readable without restoring the run (save slot lists, diagnostics).</summary>
    public readonly struct SaveInfo
    {
        public SaveInfo(int formatVersion, ulong seed, ulong configHash, long tick, ulong stateHash)
        {
            FormatVersion = formatVersion;
            Seed = seed;
            ConfigHash = configHash;
            Tick = tick;
            StateHash = stateHash;
        }

        public int FormatVersion { get; }

        public ulong Seed { get; }

        public ulong ConfigHash { get; }

        public long Tick { get; }

        public ulong StateHash { get; }
    }

    /// <summary>A restored run plus what the host should know about it.</summary>
    public sealed class LoadedGame
    {
        internal LoadedGame(Simulation simulation, SaveInfo info, bool configChanged)
        {
            Simulation = simulation;
            Info = info;
            ConfigChanged = configChanged;
        }

        public Simulation Simulation { get; }

        public SaveInfo Info { get; }

        /// <summary>
        /// True when the save was written with different balance values. The run continues with the
        /// current config (game updates must never brick saves); exact replay of the old segment needs the old config.
        /// </summary>
        public bool ConfigChanged { get; }
    }

    /// <summary>
    /// Versioned binary snapshot of a run (ADR-0008): header, state (via <see cref="GameState.Visit"/>),
    /// command log, event log, FNV-1a checksum. Pure bytes: hosts own file I/O.
    /// </summary>
    public static class SaveGame
    {
        public const ushort FormatVersion = 1;

        private const int HeaderBytes = 4 + 2 + 2 + 8 + 8 + 8 + 8;
        private const int ChecksumBytes = 8;
        private static readonly byte[] Magic = { (byte)'D', (byte)'S', (byte)'W', (byte)'S' };

        public static byte[] Write(Simulation sim)
        {
            var w = new ByteWriter();
            foreach (byte b in Magic)
            {
                w.U8(b);
            }

            w.U16(FormatVersion);
            w.U16((ushort)EventLog.SchemaVersion);
            w.U64(sim.Seed);
            w.U64(sim.Config.ComputeHash());
            w.I64(sim.State.Tick);
            w.U64(StateHasher.Hash(sim.State));

            sim.State.Visit(new StateWriter(w));

            IReadOnlyList<RecordedCommand> commands = sim.Commands.Commands;
            w.I32(commands.Count);
            foreach (RecordedCommand rc in commands)
            {
                w.I64(rc.Tick);
                w.I32((int)rc.Command.Kind);
                w.I32(rc.Command.A);
                w.I32(rc.Command.B);
                w.I32(rc.Command.C);
            }

            IReadOnlyList<SimEvent> events = sim.Log.Events;
            w.I64(sim.Log.NextSeq);
            w.I32(events.Count);
            foreach (SimEvent e in events)
            {
                w.I64(e.Seq);
                w.I64(e.Tick);
                w.I32((int)e.Kind);
                w.I32(e.A);
                w.I32(e.B);
                w.I32(e.C);
                w.I32(e.D);
            }

            w.U64(w.Checksum());
            return w.ToArray();
        }

        /// <summary>Reads only the header after verifying magic and checksum.</summary>
        public static SaveInfo ReadInfo(byte[] bytes)
        {
            ByteReader r = OpenVerified(bytes);
            return ReadHeader(r);
        }

        public static LoadedGame Load(byte[] bytes, SimConfig config)
        {
            ByteReader r = OpenVerified(bytes);
            SaveInfo info = ReadHeader(r);

            var state = new GameState(info.Seed, config);
            state.Visit(new StateReader(r));
            if (state.Tick != info.Tick || StateHasher.Hash(state) != info.StateHash)
            {
                throw new SaveLoadException(SaveLoadError.Corrupted, "State does not match its recorded hash.");
            }

            var commands = new CommandLog();
            int commandCount = r.Count(8 + 16);
            for (int i = 0; i < commandCount; i++)
            {
                long tick = r.I64();
                var command = new Command((CommandKind)r.I32(), r.I32(), r.I32(), r.I32());
                commands.Add(new RecordedCommand(tick, command));
            }

            long nextSeq = r.I64();
            int eventCount = r.Count(16 + 20);
            var events = new List<SimEvent>(eventCount);
            for (int i = 0; i < eventCount; i++)
            {
                events.Add(new SimEvent(r.I64(), r.I64(), (EventKind)r.I32(), r.I32(), r.I32(), r.I32(), r.I32()));
            }

            if (r.Remaining != 0)
            {
                throw new SaveLoadException(SaveLoadError.Corrupted, "Unexpected trailing data in save.");
            }

            var log = new EventLog();
            log.Restore(nextSeq, events);
            var sim = new Simulation(info.Seed, config, state, log, commands);
            return new LoadedGame(sim, info, info.ConfigHash != config.ComputeHash());
        }

        private static ByteReader OpenVerified(byte[] bytes)
        {
            if (bytes == null || bytes.Length < Magic.Length)
            {
                throw new SaveLoadException(SaveLoadError.NotASave, "Not a DEADSWITCH save (too short).");
            }

            for (int i = 0; i < Magic.Length; i++)
            {
                if (bytes[i] != Magic[i])
                {
                    throw new SaveLoadException(SaveLoadError.NotASave, "Not a DEADSWITCH save (bad magic).");
                }
            }

            if (bytes.Length < HeaderBytes + ChecksumBytes)
            {
                throw new SaveLoadException(SaveLoadError.Truncated, "Save is shorter than its header.");
            }

            int bodyEnd = bytes.Length - ChecksumBytes;
            ulong stored = new ByteReader(bytes, bodyEnd, bytes.Length).U64();
            if (Fnv.Hash(bytes, 0, bodyEnd) != stored)
            {
                throw new SaveLoadException(SaveLoadError.Corrupted, "Save checksum mismatch.");
            }

            return new ByteReader(bytes, Magic.Length, bodyEnd);
        }

        private static SaveInfo ReadHeader(ByteReader r)
        {
            ushort version = r.U16();
            if (version > FormatVersion || version == 0)
            {
                throw new SaveLoadException(SaveLoadError.UnsupportedVersion, "Save format " + version + " is not supported by this build (max " + FormatVersion + ").");
            }

            ushort eventSchema = r.U16();
            if (eventSchema > EventLog.SchemaVersion)
            {
                throw new SaveLoadException(SaveLoadError.UnsupportedVersion, "Event schema " + eventSchema + " is newer than this build.");
            }

            return new SaveInfo(version, r.U64(), r.U64(), r.I64(), r.U64());
        }

        private sealed class StateWriter : IStateVisitor
        {
            private readonly ByteWriter _w;

            public StateWriter(ByteWriter w)
            {
                _w = w;
            }

            public bool IsReading => false;

            public void Int(ref int value)
            {
                _w.I32(value);
            }

            public void Long(ref long value)
            {
                _w.I64(value);
            }

            public void ULong(ref ulong value)
            {
                _w.U64(value);
            }

            public void Bool(ref bool value)
            {
                _w.U8(value ? (byte)1 : (byte)0);
            }

            public int Count(int count)
            {
                _w.I32(count);
                return count;
            }
        }

        private sealed class StateReader : IStateVisitor
        {
            private readonly ByteReader _r;

            public StateReader(ByteReader r)
            {
                _r = r;
            }

            public bool IsReading => true;

            public void Int(ref int value)
            {
                value = _r.I32();
            }

            public void Long(ref long value)
            {
                value = _r.I64();
            }

            public void ULong(ref ulong value)
            {
                value = _r.U64();
            }

            public void Bool(ref bool value)
            {
                byte b = _r.U8();
                if (b > 1)
                {
                    throw new SaveLoadException(SaveLoadError.Corrupted, "Invalid boolean in state.");
                }

                value = b == 1;
            }

            public int Count(int count)
            {
                return _r.Count(1);
            }
        }
    }
}
