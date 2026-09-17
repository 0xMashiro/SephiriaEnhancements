using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.ItemCommunication;

namespace SephiriaEnhancements.ModelChecks.Features.ItemCommunication;

internal static class ItemCommunicationChecks
{
    internal static void Run()
    {
        foreach (string language in LocalizationLanguages.All)
        {
            var texts = new Dictionary<string, string>();
            ItemCommunicationLocalization.Register((lang, key, value) => texts[key] = value, new[] { language });
            foreach (string key in new[] { ItemCommunicationLocalization.RewardMessage,
                ItemCommunicationLocalization.BuyMessage, ItemCommunicationLocalization.ShareMessage })
            {
                string result = ItemCommunicationMessage.Compose(texts[key], new string('a', 150) + "😀",
                    "83 gold", "150", "Rare", "Category");
                if (result == null || result.Length > 120 || result.Contains('\uFFFD'))
                    throw new InvalidOperationException("Chat must fit the native limit in every language.");
                if (key != ItemCommunicationLocalization.RewardMessage && !result.Contains("83 gold"))
                    throw new InvalidOperationException("Shortening the item name must preserve its quote.");
                if (key == ItemCommunicationLocalization.BuyMessage && !result.Contains("150"))
                    throw new InvalidOperationException("Purchase request must preserve negotiation.");
            }
        }
        string emoji = ItemCommunicationMessage.Compose("{0}", new string('a', 118) + "😀xyz", "", "", "", "");
        if (emoji.Length > 120 || char.IsHighSurrogate(emoji[emoji.Length - 2]))
            throw new InvalidOperationException("Item shortening split a surrogate pair.");
        if (ItemCommunicationMessage.Compose("{0} {1}", "item", new string('x', 120), "", "", "") != null)
            throw new InvalidOperationException("Unrepresentable quotes must not be silently truncated.");
        var throttle = new ItemCommunicationThrottle();
        if (!throttle.CanSend("A", 0)) throw new InvalidOperationException("Initial send blocked.");
        throttle.Record("A", 0);
        if (throttle.CanSend("A", 1) || throttle.CanSend("B", .5) ||
            !throttle.CanSend("B", 1) || !throttle.CanSend("A", 2))
            throw new InvalidOperationException("Chat repetition limits failed.");
    }
}
