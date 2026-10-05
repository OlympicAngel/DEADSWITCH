namespace Deadswitch.Game.Presentation
{
    /// <summary>
    /// The Field Guide (SPEC-043): every term the player meets, in one or two plain sentences, grouped the way the
    /// game is played. One source for the guide screen; hints reuse the same wording.
    /// </summary>
    public static class Glossary
    {
        public sealed class Entry
        {
            public Entry(string glyph, string term, string meaning)
            {
                Glyph = glyph;
                Term = term;
                Meaning = meaning;
            }

            public string Glyph { get; }

            public string Term { get; }

            public string Meaning { get; }
        }

        public sealed class Group
        {
            public Group(string glyph, string title, params Entry[] entries)
            {
                Glyph = glyph;
                Title = title;
                Entries = entries;
            }

            public string Glyph { get; }

            public string Title { get; }

            public Entry[] Entries { get; }
        }

        public static readonly Group[] Groups =
        {
            new Group("bolt", "RESOURCES",
                new Entry("bolt", "ENERGY", "Power for everything. Generators and solar fields make it; every building uses some. Run out and the Hub blacks out."),
                new Entry("chip", "COMPUTE", "The AI's thinking power. Spent on research, audits, hacks and battle tricks. Heavy use corrupts the AI."),
                new Entry("fuel", "FUEL", "Burned by strikes, vehicles and the reactor. It comes from outposts, salvage and trade on the map."),
                new Entry("battery", "STORAGE", "How much you can hold. A full store wastes what your buildings make, so spend it or build batteries.")),
            new Group("people", "PEOPLE",
                new Entry("people", "PEOPLE", "Everyone living in the Hub. Life support raises how many can live here."),
                new Entry("wrench", "WORKERS", "People running your buildings. A building with no workers is run by the AI at lower output."),
                new Entry("shield", "DEFENDERS", "People posted on the wall. Each one adds defense, but they are not working while they stand guard."),
                new Entry("flag", "LOYALTY", "How much the people trust you. Low loyalty cuts output; hard choices lower it.")),
            new Group("shield", "DEFENSE",
                new Entry("alert", "ATTACK TYPES", "RAID: a band after your stores. SIEGE: a long assault that wears you down. PURGE: a warlord's ultimatum enforced. VIRUS: an attack on the AI itself."),
                new Entry("shield", "STANCE", "How the Hub meets the next attack. NORMAL: everyone works. FORTIFY: more defense. HIDE: harder to find, costs energy. EVACUATE: no losses of life, they take more."),
                new Entry("target", "DEFENSE VS ATTACKERS", "Your defense against the AI's estimate of the attackers. The estimate can be wrong, and the AI can lie."),
                new Entry("hand", "OVERRIDE", "Emergency charges that force the AI's hand: a lockdown, silencing the AI, cancelling what it does. They refill slowly."),
                new Entry("wrench", "WRECKS AND DAMAGE", "Fights leave wrecks in the yard and damage on buildings. Both cost output until you clear or repair them.")),
            new Group("core", "THE AI",
                new Entry("core", "THE CORE", "The damaged AI that runs your Hub. It helps you, and it may not be telling you everything."),
                new Entry("eye", "CORRUPTION", "How far the AI has drifted. The number on screen is what it reports; an audit shows the truth."),
                new Entry("search", "AUDIT", "A scan that reads the AI directly: its real corruption, what it skims and what it hides."),
                new Entry("cycle", "AI CONTROL", "How much the AI does without asking. YOU DECIDE, AI ASSISTS, or AI DECIDES."),
                new Entry("memory", "MODULES", "Pieces of the AI's memory you restore with time and compute. Each one unlocks an upgrade; some exclude another.")),
            new Group("map", "THE WORLD",
                new Entry("flame", "HEAT", "How hard a faction is watching you: COLD, WATCHED, HUNTED, MARKED. More heat brings bigger attacks."),
                new Entry("target", "OUTPOST", "A site you hold on the map. It produces resources and can be attacked."),
                new Entry("swords", "STRIKE, SCOUT, HACK, SABOTAGE", "Orders for a team on the map: attack a site, look before you strike, break in with the AI, or cripple a faction's attacks for a while."),
                new Entry("eye", "SPIES", "Send a spy into a faction to read its plans, or frame a rival to turn factions on each other."),
                new Entry("mail", "CEASEFIRE AND ALLIANCE", "Pay a faction to stop attacking for a while, or to stand with you on the wall.")),
            new Group("up", "PROGRESS",
                new Entry("up", "TIER", "The size of your Hub. Reaching a new tier opens more plots, buildings and threats."),
                new Entry("cycle", "RELOCATION", "Leave this Hub at its peak and start over somewhere new. The AI carries part of what it learned."),
                new Entry("star", "LEGACY POINTS", "Your score, carried from one Hub to the next. Spend them on perks for every new start."),
                new Entry("check", "MASTERY", "Hard goals that award extra legacy points."),
                new Entry("skull", "HARDCORE", "One life. No vacation shield, less mercy, and if the core falls the run ends."),
                new Entry("book", "STORY", "Chapters and memory fragments: what happened, and what the AI is.")),
        };
    }
}
