#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.MultiplayerAccess
{
    internal enum JoiningSupplyKind
    {
        LevelReward, ArtifactReward, MiracleChoice, InventorySpace,
        MaximumHealth, RerollDice, TabletAltar, EnchantAltar, Anvil
    }

    // Serializable values only: network objects belong to the integration layer.
    [Serializable]
    internal sealed class JoiningSupplyOpportunity
    {
        public string Id = "";
        public string Floor = "";
        public JoiningSupplyKind Kind;
        public uint Prefab = 0;
        public int Seed = 0;
        public int Amount = 0;
        public int Variant = 0;
        public List<int> Recipients = new List<int>();
    }

    [Serializable]
    internal sealed class JoiningSupplyClaim
    {
        public JoiningSupplyOpportunity Opportunity = null!;
        public bool Completed;
        public bool DeliveredByGame = false;
        public bool Failed = false;
        // The native adapter owns this payload; it contains no Unity references.
        public string Selection = "";
        public string ClientSelection = "";
        public int SelectionRevision = 0;

        internal bool AcceptNativeDelivery(bool active)
        {
            if (active || Completed && !DeliveredByGame) return false;
            Completed = true;
            DeliveredByGame = true;
            return true;
        }
    }

    [Serializable]
    internal sealed class JoiningSupplyParticipant
    {
        public int Slot;
        public int TargetLevel;
        public int TargetMoney;
        public int MoneyGrant;
        public bool MoneyGranted;
        public bool BasicsComplete;
        public bool Failed = false;
        public bool RecordedFromExplorationStart;
        public List<JoiningSupplyClaim> Claims = new List<JoiningSupplyClaim>();

        internal int Remaining => Claims.Count(claim => !claim.Completed);
        internal JoiningSupplyClaim? Next => Claims.FirstOrDefault(claim => !claim.Completed);
    }

    [Serializable]
    internal sealed class JoiningSupplyLedger
    {
        public bool RecordedFromExplorationStart;
        public List<JoiningSupplyOpportunity> Opportunities = new List<JoiningSupplyOpportunity>();
        public List<JoiningSupplyParticipant> Participants = new List<JoiningSupplyParticipant>();
        [NonSerialized] private Dictionary<string, JoiningSupplyOpportunity>? byId;

        internal JoiningSupplyParticipant? Find(int slot) =>
            Participants.Find(participant => participant.Slot == slot);

        internal void Record(JoiningSupplyOpportunity opportunity, int? recipient)
        {
            if (byId == null) byId = Opportunities.ToDictionary(item => item.Id, StringComparer.Ordinal);
            if (!byId.TryGetValue(opportunity.Id, out var saved))
            {
                saved = opportunity;
                Opportunities.Add(saved);
                byId.Add(saved.Id, saved);
            }
            if (recipient.HasValue && !saved.Recipients.Contains(recipient.Value))
                saved.Recipients.Add(recipient.Value);
        }

        internal JoiningSupplyParticipant Admit(int slot, int targetLevel, int targetMoney,
            int referenceSlot, ISet<string> referenceFloors, ISet<string> availableHere, int initialMoney = 0)
        {
            var existing = Find(slot);
            if (existing != null) return existing;
            var reference = Find(referenceSlot);
            var inherited = new HashSet<string>(reference?.Claims.Select(claim => claim.Opportunity.Id) ?? Enumerable.Empty<string>());
            var participant = new JoiningSupplyParticipant
            {
                Slot = slot,
                TargetLevel = targetLevel,
                TargetMoney = targetMoney,
                MoneyGrant = Math.Max(0, targetMoney - initialMoney),
                RecordedFromExplorationStart = RecordedFromExplorationStart && (reference?.RecordedFromExplorationStart ?? true)
            };
            foreach (var opportunity in Opportunities)
            {
                bool onRoute = opportunity.Recipients.Count > 0 ? opportunity.Recipients.Contains(referenceSlot) :
                    referenceFloors.Contains(opportunity.Floor);
                if ((!onRoute && !inherited.Contains(opportunity.Id)) || opportunity.Recipients.Contains(slot) ||
                    availableHere.Contains(opportunity.Id)) continue;
                participant.Claims.Add(new JoiningSupplyClaim { Opportunity = opportunity });
            }
            Participants.Add(participant);
            return participant;
        }

        internal static int Mean(IEnumerable<int> values)
        {
            long total = 0;
            int count = 0;
            foreach (int value in values) { total += value; count++; }
            if (count == 0) throw new InvalidOperationException("No initialized teammates.");
            return checked((int)(total / count));
        }
    }
}
