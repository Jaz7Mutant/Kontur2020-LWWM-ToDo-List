using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        private readonly Dictionary<int, TrackingEntry> entries;
        private readonly HashSet<int> bannedUsers;

        public int Count => entries.Values
            .Select(trackingEntry => trackingEntry.GetEntry(bannedUsers))
            .Count(entry => entry != null);

        public ToDoList()
        {
            entries = new Dictionary<int, TrackingEntry>();
            bannedUsers = new HashSet<int>();
        }

        public TrackingEntry GeTrackingEntry(int entryId, int userId)
        {
            if (!entries.ContainsKey(entryId))
            {
                entries[entryId] = new TrackingEntry(entryId);
            }
            return entries[entryId];
        }

        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            var trackingEntry = GeTrackingEntry(entryId, userId);
            trackingEntry.AddNameUpdate(new Update<string>(userId, name, timestamp));
            trackingEntry.AddVisibilityStatusUpdate(new Update<bool>(userId, true, timestamp));
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            GeTrackingEntry(entryId, userId).AddVisibilityStatusUpdate(new Update<bool>(userId, false, timestamp));
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
           GeTrackingEntry(entryId, userId).AddStateUpdate(
               new Update<EntryState>(userId, EntryState.Done, timestamp));
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            GeTrackingEntry(entryId, userId).AddStateUpdate(
                new Update<EntryState>(userId, EntryState.Undone, timestamp));
        }

        public void DismissUser(int userId)
        {
            bannedUsers.Add(userId);
        }

        public void AllowUser(int userId)
        {
            bannedUsers.Remove(userId);
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            return entries.Values
                .Select(trackingEntry => trackingEntry.GetEntry(bannedUsers))
                .Where(entry => entry != null).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class Update<T>
        {
            public int UserId { get; }
            public T Value { get; }
            public long Timestamp { get; }

            public Update(int userId, T value, long timestamp)
            {
                UserId = userId;
                Value = value;
                Timestamp = timestamp;
            }
        }

        public class TrackingEntry
        {
            private readonly SortedSet<Update<string>> nameUpdates;
            private readonly SortedSet<Update<EntryState>> stateUpdates;
            private readonly SortedSet<Update<bool>> visibilityStatusUpdates;
            private readonly int entryId;

            public TrackingEntry(int entryId)
            {
                this.entryId = entryId;
                nameUpdates = new SortedSet<Update<string>>(new NameUpdateComparer());
                stateUpdates = new SortedSet<Update<EntryState>>(new StateUpdateComparer());
                visibilityStatusUpdates = new SortedSet<Update<bool>>(new VisibilityStatusUpdateComparer());
            }

            public void AddNameUpdate(Update<string> update) => nameUpdates.Add(update);

            public void AddStateUpdate(Update<EntryState> update) => stateUpdates.Add(update);

            public void AddVisibilityStatusUpdate(Update<bool> update) => visibilityStatusUpdates.Add(update);

            public Entry GetEntry(HashSet<int> bannedUsers)
            {
                var name = nameUpdates.LastOrDefault(x => !bannedUsers.Contains(x.UserId))?.Value;
                var state = stateUpdates.LastOrDefault(x => !bannedUsers.Contains(x.UserId))?.Value ?? EntryState.Undone;
                var isVisible = visibilityStatusUpdates.LastOrDefault(x => !bannedUsers.Contains(x.UserId))?.Value ?? false;
                return isVisible ? new Entry(entryId, name, state) : null;
            }
        }

        public class NameUpdateComparer : Comparer<Update<string>>
        {
            public override int Compare(Update<string> x, Update<string> y)
            {
                if (x == null || y == null) 
                    return 0;
                return x.Timestamp != y.Timestamp ? x.Timestamp.CompareTo(y.Timestamp) : y.UserId.CompareTo(x.UserId);
            }
        }

        public class VisibilityStatusUpdateComparer : Comparer<Update<bool>>
        {
            public override int Compare(Update<bool> x, Update<bool> y)
            {
                if (x == null || y == null)
                    return 0;
                return x.Timestamp != y.Timestamp ? x.Timestamp.CompareTo(y.Timestamp) : y.Value.CompareTo(x.Value);
            }
        }

        public class StateUpdateComparer : Comparer<Update<EntryState>>
        {
            public override int Compare(Update<EntryState> x, Update<EntryState> y)
            {
                if (x == null || y == null)
                    return 0;
                return x.Timestamp != y.Timestamp ? x.Timestamp.CompareTo(y.Timestamp) : x.Value.CompareTo(y.Value);
            }
        }
    }
}