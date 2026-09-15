using SephiriaEnhancements.StageRewardAutoClaim;

namespace SephiriaEnhancements.ModelChecks.Features.StageRewardAutoClaim;

internal static class StageRewardAutoClaimChecks
{
    internal static void Run()
    {
        var claimed = StageRewardAutoClaimPolicy.ReadClaimed("early,early,,other");
        Require(claimed.SetEquals(new[] { "early", "other" }), "Empty entries and duplicates are not additional rewards.");
        bool Eligible(int highest, int tier = 18, bool target = true, bool talent = true, string id = "next") =>
            StageRewardAutoClaimPolicy.CanClaim(highest, tier, target, talent, id, claimed);
        Require(!Eligible(0) && !Eligible(-1), "No recorded clear cannot unlock rewards.");
        Require(!Eligible(17) && Eligible(18) && Eligible(60), "Reward tiers use an inclusive cleared-tier threshold.");
        Require(!Eligible(60, target: false), "Highest tier does not prove a different clear target.");
        Require(!Eligible(60, talent: false), "Do not imply support for a different reward mechanism.");
        Require(!Eligible(60, id: "early") && !Eligible(60, id: ""), "Already claimed and missing identities are excluded.");
        Require(!Eligible(60, tier: 0), "Invalid tier cannot be awarded.");
        Require(Eligible(60, id: "gap") && Eligible(60, id: "later"), "A gap does not block later earned rewards.");
        claimed.Add("next");
        Require(!Eligible(60), "Repeating the same native clear cannot grant a reward twice.");
        Require(StageRewardAutoClaimPolicy.ReadClaimed("").Count == 0, "New save has no claimed rewards.");
        var english = new Dictionary<string, string>();
        var fallback = new Dictionary<string, string>();
        StageRewardAutoClaimLocalization.Register((_, k, v) => english.Add(k, v), new[] { "en-US" });
        StageRewardAutoClaimLocalization.Register((_, k, v) => fallback.Add(k, v), new[] { "unsupported-language" });
        Require(english.Count == 7 && english.All(x => fallback[x.Key] == x.Value), "The whole UI group falls back together.");
        Console.WriteLine("Stage reward policy and complete-group localization checks passed.");
    }

    private static void Require(bool value, string reason)
    { if (!value) throw new InvalidOperationException(reason); }
}
