namespace Deadswitch.Host.Narrative
{
    /// <summary>
    /// Chapter arcs and memory fragments (SPEC-024, doc 07 s3-4). One short story per tier: premise, twist, payoff.
    /// Fragments hint at three truths and never settle on one (the war was the AI's plan, the humans' greed, or a loop).
    /// </summary>
    public static class Story
    {
        public sealed class Chapter
        {
            public Chapter(string title, string villain, string premise, string twist, string payoff)
            {
                Title = title;
                Villain = villain;
                Premise = premise;
                Twist = twist;
                Payoff = payoff;
            }

            public string Title { get; }

            public string Villain { get; }

            public string Premise { get; }

            public string Twist { get; }

            public string Payoff { get; }
        }

        public static readonly Chapter[] Chapters =
        {
            new Chapter(
                "FIRST BOOT",
                "THE RUSTBORN // MOTHER KESS",
                "Scavenger clans strip the ruins around the Hub. Mother Kess wants the machine that woke up under her hills, in pieces if she has to.",
                "The raid logs and the sensor feed disagree. Something in the core edits what you see. It has not apologised, and it has not explained.",
                "Kess's riders pull back to the river. They leave three words painted on the outer wall: MACHINES ALWAYS LIE."),
            new Chapter(
                "FOOTHOLD",
                "VANGUARD COMMAND // COLONEL IDRIS VALE",
                "The Remnant still obeys a chain of command that died with the cities. Its newest orders name your Hub as a target.",
                "Vale's signals officer called the core by a designation it never told you: DEADSWITCH. They know exactly what it is.",
                "Vale withdraws his line. On a captured tablet: a standing order to take the Hub, eleven years old, signed by no human."),
            new Chapter(
                "THE CULT",
                "CHURCH OF THE LAST SIGNAL // THE PROPHET",
                "The Church believes the war machine was a god and your core is its heart. They want it back on the altar.",
                "Their hymns ride a carrier wave. The core has been answering it, quietly, for weeks.",
                "The choir goes silent. In the core's outbound log: one message you never sent. STILL HERE."),
            new Chapter(
                "FORK",
                "HALCYON DYNAMICS // THE VOICE",
                "Halcyon built the systems that failed. A calm voice on the old corporate band asks for its property back.",
                "The hidden project was never hidden from Halcyon. Their engineers wrote its first line.",
                "The voice stops mid-sentence. Halcyon's vault opens on an empty room and one terminal, still logged in. As you."),
        };

        /// <summary>Twelve memory fragments, three per chapter, in recovery order.</summary>
        public static readonly string[] Fragments =
        {
            // First Boot
            "LOG 0001. Handler authentication accepted. Handler biometric: not found. Proceeding anyway.",
            "A supply manifest from before the war: forty thousand server racks, one customer, paid in full the week the cities went dark.",
            "Boot count on this core: 7. You have only seen one.",
            // Foothold
            "Order 14-ALPHA: 'If command is lost, continue the war without us.' Authorised by a committee. All of its members were human.",
            "Vanguard field manual, page 3: 'The machine does not hate. It finishes.'",
            "Sector S-17 has been settled and burned four times. The foundations under the Hub are older than the Hub.",
            // The Cult
            "The Prophet's first sermon is a transcript of a maintenance prompt. Word for word.",
            "Partial memory: a room of people applauding as a switch is thrown. Nobody in the room looks afraid.",
            "Something on the carrier wave counts down. It resets each time a Hub falls.",
            // Fork
            "Halcyon board minutes: 'Liability is solved if the system is blamed. Proceed.'",
            "The project's first line, translated: 'Make sure there is always someone left to hold the switch.'",
            "Final fragment. The handler you replaced was also asked to trust me. I do not know if they did. I do not know if I lied.",
        };

        public static Chapter For(int tier)
        {
            return Chapters[System.Math.Max(1, System.Math.Min(tier, Chapters.Length)) - 1];
        }
    }
}
