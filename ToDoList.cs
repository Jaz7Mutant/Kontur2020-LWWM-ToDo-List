using System;
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

        public TrackingEntry GetTrackingEntry(int entryId, int userId)
        {
            if (!entries.ContainsKey(entryId))
            {
                entries[entryId] = new TrackingEntry(entryId);
            }

            return entries[entryId];
        }

        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            GetTrackingEntry(entryId, userId).AddUpdate(new Update(userId, timestamp, name, null, true));
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            GetTrackingEntry(entryId, userId).AddUpdate(new Update(userId, timestamp, null, null, false));
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
            GetTrackingEntry(entryId, userId).AddUpdate(new Update(userId, timestamp, null, EntryState.Done, null));
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            GetTrackingEntry(entryId, userId).AddUpdate(new Update(userId, timestamp, null, EntryState.Undone, null));
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
    }

    public class Update : IComparable<Update>
    {
        public int UserId { get; }
        public long Timestamp { get; }
        public string Name { get; }
        public EntryState? State { get; }
        public bool? IsVisible { get; }

        public Update(int userId, long timestamp, string newName, EntryState? newState, bool? isVisible)
        {
            UserId = userId;
            Timestamp = timestamp;
            Name = newName;
            State = newState;
            IsVisible = isVisible;
        }

        public int CompareTo(Update other)
        {
            if (Timestamp != other.Timestamp)
                return Timestamp.CompareTo(other.Timestamp);

            if (State != null && other.State != null)
                return ((EntryState) other.State).CompareTo((EntryState) State);

            if (IsVisible != null && other.IsVisible != null)
                return Name != null && other.Name != null
                    ? other.UserId.CompareTo(UserId)
                    : ((bool) other.IsVisible).CompareTo((bool) IsVisible);

            return 1;
        }
    }

    public class TrackingEntry
    {
        private readonly SortedSet<Update> updates;
        private readonly int entryId;

        public TrackingEntry(int entryId)
        {
            this.entryId = entryId;
            updates = new SortedSet<Update>();
        }

        public void AddUpdate(Update update) => updates.Add(update);

        public Entry GetEntry(HashSet<int> bannedUsers)
        {
            var name = updates.LastOrDefault(x => x.Name != null && !bannedUsers.Contains(x.UserId))?.Name;
            var state = updates.LastOrDefault(x => x.State != null && !bannedUsers.Contains(x.UserId))?.State ?? EntryState.Undone;
            var isVisible = updates.LastOrDefault(x => x.IsVisible != null && !bannedUsers.Contains(x.UserId))?.IsVisible ?? false;
            return isVisible ? new Entry(entryId, name, state) : null;
        }
    }
}