using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Abilities in a live battle (SPEC-020). Stored in commands and events: never renumber.</summary>
    public enum BattleAbility
    {
        /// <summary>Turrets and defenders concentrate: defense up, costs compute.</summary>
        FocusFire = 0,

        /// <summary>Shells on the approach: attack strength down, costs energy.</summary>
        Barrage = 1,

        /// <summary>OVERRIDE: the handler takes direct control of the defenses.</summary>
        Takeover = 2,

        /// <summary>OVERRIDE: the AI seizes the attackers' machines; corrupts it further.</summary>
        Seize = 3,
    }

    /// <summary>
    /// Short live battles (SPEC-020, doc 04 s7): when an attack reaches the wall while the handler is present and has
    /// taken command, resolution waits <c>battle.battle_minutes</c> while the handler spends abilities (each once).
    /// Sieges, purges and the Warlord wave are commanded by default; raids on request. Away = auto-resolve as before.
    /// No RNG draws.
    /// </summary>
    public static class BattleSystem
    {
        public static bool InBattle(GameState s)
        {
            return s.RaidId != 0 && s.BattleEndTick != 0;
        }

        public static bool Used(GameState s, BattleAbility a)
        {
            return (s.BattleUsed & (1 << (int)a)) != 0;
        }

        /// <summary>
        /// Called when an attack reaches the wall. True while the live battle holds resolution back (it starts one
        /// if the handler took command and is here). The handler leaving ends the hold at once.
        /// </summary>
        public static bool Holds(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.Away)
            {
                return false;
            }

            if (s.BattleEndTick == 0)
            {
                if (!s.BattleLive)
                {
                    return false;
                }

                s.BattleEndTick = s.Tick + ctx.Config.Battle.BattleMinutes;
                ctx.Emit(EventKind.BattleStarted, s.RaidId, ctx.Config.Battle.BattleMinutes, (int)s.RaidKind);
                return true;
            }

            return s.Tick < s.BattleEndTick;
        }

        public static CommandResult TakeCommand(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A < 0 || cmd.A > 1 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.RaidId == 0 || s.BattleEndTick != 0)
            {
                return CommandResult.Reject(RejectReason.NoBattle);
            }

            bool on = cmd.A == 1;
            if (s.BattleLive == on)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            s.BattleLive = on;
            return CommandResult.Ok;
        }

        public static CommandResult Ability(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            BattleConfig b = c.Battle;
            if (cmd.A < 0 || cmd.A > (int)BattleAbility.Seize || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            var a = (BattleAbility)cmd.A;
            if (!InBattle(s))
            {
                return CommandResult.Reject(RejectReason.NoBattle);
            }

            if (Used(s, a))
            {
                return CommandResult.Reject(RejectReason.AbilityUsed);
            }

            switch (a)
            {
                case BattleAbility.FocusFire:
                    if (s.Compute < b.FocusCompute)
                    {
                        return CommandResult.Reject(RejectReason.NotEnoughCompute);
                    }

                    s.Compute -= b.FocusCompute;
                    s.BattleDefensePct += b.FocusDefensePct;
                    break;
                case BattleAbility.Barrage:
                    if (s.Energy < b.BarrageEnergy)
                    {
                        return CommandResult.Reject(RejectReason.NotEnoughEnergy);
                    }

                    s.Energy -= b.BarrageEnergy;
                    s.BattleStrengthCutPct = System.Math.Min(90, s.BattleStrengthCutPct + b.BarrageStrengthPct);
                    break;
                default:
                    RejectReason why = SpendOverride(ctx, a == BattleAbility.Takeover ? OverrideKind.Takeover : OverrideKind.Seize);
                    if (why != RejectReason.None)
                    {
                        return CommandResult.Reject(why);
                    }

                    if (a == BattleAbility.Takeover)
                    {
                        s.BattleDefensePct += b.TakeoverDefensePct;
                    }
                    else
                    {
                        s.BattleStrengthCutPct = System.Math.Min(90, s.BattleStrengthCutPct + b.SeizeStrengthPct);
                        CorruptionSystem.Add(ctx, b.SeizeCorruption);
                    }

                    break;
            }

            s.BattleUsed |= 1 << (int)a;
            ctx.Emit(EventKind.BattleAbilityUsed, s.RaidId, cmd.A);
            return CommandResult.Ok;
        }

        /// <summary>Defense and strength after the battle's abilities (applied at resolution).</summary>
        public static void Apply(GameState s, ref int strength, ref int defense)
        {
            if (s.BattleDefensePct > 0)
            {
                defense = SimMath.PctFloor(defense, 100 + s.BattleDefensePct);
            }

            if (s.BattleStrengthCutPct > 0)
            {
                strength = SimMath.PctFloor(strength, 100 - s.BattleStrengthCutPct);
            }
        }

        public static void Clear(GameState s)
        {
            s.BattleLive = false;
            s.BattleEndTick = 0;
            s.BattleDefensePct = 0;
            s.BattleStrengthCutPct = 0;
            s.BattleUsed = 0;
        }

        private static RejectReason SpendOverride(SimContext ctx, OverrideKind kind)
        {
            GameState s = ctx.State;
            if (s.OverrideCharges <= 0)
            {
                return RejectReason.NoCharges;
            }

            if (s.Tick < s.OverrideCooldownUntil)
            {
                return RejectReason.OnCooldown;
            }

            if (s.OverrideCharges >= OverrideSystem.MaxCharges(s, ctx.Config))
            {
                s.OverrideNextChargeTick = s.Tick + ctx.Config.Override.RegenMinutes;
            }

            s.OverrideCharges--;
            s.OverrideCooldownUntil = s.Tick + ctx.Config.Override.CooldownMinutes;
            int corruption = ctx.Config.Override.CorruptionMilliPerUse;
            ctx.Emit(EventKind.OverrideUsed, (int)kind, s.OverrideCharges, corruption);
            CorruptionSystem.Add(ctx, corruption);
            return RejectReason.None;
        }
    }
}
