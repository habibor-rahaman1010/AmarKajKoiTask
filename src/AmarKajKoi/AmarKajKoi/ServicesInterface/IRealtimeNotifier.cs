namespace AmarKajKoi.ServicesInterface
{
    /// <summary>
    /// Collects the users whose notification badge went stale during the current
    /// operation, then pushes their refreshed counts over SignalR.
    ///
    /// Queue and flush are separate on purpose: notifications are written inside a
    /// database transaction, so a count read before the commit would still show the
    /// old value. Callers queue while the work happens and the flush runs afterwards
    /// (for HTTP requests, automatically — see RealtimeNotificationFilter).
    /// </summary>
    public interface IRealtimeNotifier
    {
        void QueueUnreadRefresh(Guid userId);
        void QueueUnreadRefresh(IEnumerable<Guid> userIds);

        /// <summary>Sends every queued user their current count. Never throws.</summary>
        Task FlushAsync();
    }
}
