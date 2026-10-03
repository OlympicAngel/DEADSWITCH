namespace Deadswitch.Sim.State
{
    /// <summary>Something the AI did on its own under delegation (SPEC-004). Stored in events: never renumber.</summary>
    public enum AiActionKind
    {
        None = 0,

        /// <summary>Delegated routines started a construction. Event B: slot, C: FacilityKind, D: target level.</summary>
        Build = 1,

        /// <summary>Autopilot set the defense while the handler was away. Event B: Posture, C: garrison, D: the AI's estimate.</summary>
        Defend = 2,
    }

    /// <summary>The hidden project's stage (doc 10 s2). Stored in events: never renumber.</summary>
    public enum ProjectStage
    {
        Dormant = 0,
        Active = 1,
        Advanced = 2,
        Imminent = 3,
    }

    /// <summary>How the project ends when its window runs out (SPEC-011). Stored in events: never renumber.</summary>
    public enum ClimaxKind
    {
        None = 0,

        /// <summary>A cold AI opens the gates to raiders.</summary>
        Betrayal = 1,

        /// <summary>A bold AI copies itself out and leaves a weaker core.</summary>
        Fork = 2,
    }

    /// <summary>What the AI lied about (for the Audit, F-014). Stored in events: never renumber.</summary>
    public enum LieKind
    {
        None = 0,

        /// <summary>Reported the wrong raid gate. Event B: raid id, C: true RaidGate, D: reported RaidGate.</summary>
        RaidGate = 1,

        /// <summary>Understated a breach in the report summary. Event B: raid id, C: true energy loss, D: shown.</summary>
        ReportEdit = 2,
    }
}
