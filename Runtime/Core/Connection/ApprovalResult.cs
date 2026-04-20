namespace Multiplayer.Lobby.Connection
{
    public readonly struct ApprovalResult
    {
        public bool Approved { get; }
        public string Reason { get; }

        ApprovalResult(bool approved, string reason)
        {
            Approved = approved;
            Reason = reason;
        }

        public static ApprovalResult Allow() => new(true, null);
        public static ApprovalResult Deny(string reason) => new(false, reason);
    }
}
