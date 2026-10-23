namespace FootballRunner.Adventure
{
    /// <summary>
    /// Selectable runner slots. Only Rukajja is playable in this slice; extra
    /// slots exist so later licensed characters can drop in without a new menu.
    /// </summary>
    public static class CharacterRoster
    {
        public const string RukajjaId = "rukajja";

        public readonly struct Slot
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Blurb;
            public readonly bool Unlocked;

            public Slot(string id, string displayName, string blurb, bool unlocked)
            {
                Id = id;
                DisplayName = displayName;
                Blurb = blurb;
                Unlocked = unlocked;
            }
        }

        public static readonly Slot[] Slots =
        {
            new Slot(RukajjaId, "RUKAJJA", "Ready for Green Hills.", true),
            new Slot("runner_2", "COMING SOON", "A new friend joins later.", false),
            new Slot("runner_3", "COMING SOON", "A new friend joins later.", false)
        };

        public static bool IsUnlocked(string id)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].Id == id)
                {
                    return Slots[i].Unlocked;
                }
            }

            return false;
        }

        public static Slot Get(string id)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].Id == id)
                {
                    return Slots[i];
                }
            }

            return Slots[0];
        }
    }
}
