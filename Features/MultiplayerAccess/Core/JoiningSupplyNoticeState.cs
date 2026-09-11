namespace SephiriaEnhancements.MultiplayerAccess
{
    // Owned by one server connection. Unsolicited failure state is distinct from request replies.
    internal sealed class JoiningSupplyNoticeState
    {
        private long lastReply;
        private int? failedRevision;

        internal void Reset() { lastReply = 0; failedRevision = null; }

        internal bool TakeReply(long reply)
        {
            if (reply <= lastReply) return false;
            lastReply = reply;
            return true;
        }

        internal bool TakeFailure(int revision)
        {
            if (failedRevision == revision) return false;
            failedRevision = revision;
            return true;
        }
    }
}
