using System;

namespace SephiriaEnhancements.ItemCommunication
{
    internal enum ItemCommunicationIntent { OfferReward, RequestPurchase, ShareProduct }

    internal static class ItemCommunicationMessage
    {
        internal const int MaximumLength = 120;

        // Keep the request and quote intact; optional item details use remaining space.
        internal static string Compose(string template, string name, string quote,
            string negotiation, string rarity, string categories)
        {
            int available = MaximumLength - string.Format(template, "", quote, negotiation).Length;
            if (available < 2) return null;
            string message = string.Format(template, Shorten(name, available), quote, negotiation);
            foreach (string detail in new[] { rarity, categories })
                if (!string.IsNullOrWhiteSpace(detail) && message.Length + detail.Length + 3 <= MaximumLength)
                    message += " | " + detail;
            return message;
        }

        private static string Shorten(string value, int length)
        {
            if (value.Length <= length) return value;
            int end = length - 1;
            if (end > 0 && char.IsHighSurrogate(value[end - 1])) end--;
            return value.Substring(0, end) + "…";
        }
    }

    internal sealed class ItemCommunicationThrottle
    {
        private string last;
        private double sentAt = double.NegativeInfinity;
        internal bool CanSend(string message, double now) =>
            now - sentAt >= .75 && (message != last || now - sentAt >= 2);
        internal void Record(string message, double now) { last = message; sentAt = now; }
    }
}
