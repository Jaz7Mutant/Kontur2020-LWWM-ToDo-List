using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        private readonly SortedDictionary<long, List<Command>> commandsByTimestamps;
        private Dictionary<int, Entry> notes;
        private readonly HashSet<int> bannedUsers;
        private readonly HashSet<int> removedNotes;

        public int Count
        {
            get
            {
                UpdateNotes();
                return notes.Count;
            }
        }

        public ToDoList()
        {
            commandsByTimestamps = new SortedDictionary<long, List<Command>>();
            notes = new Dictionary<int, Entry>();
            bannedUsers = new HashSet<int>();
            removedNotes = new HashSet<int>();
        }

        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            AddCommand(timestamp, new Command(userId, AddEntry, entryId, name));
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            AddCommand(timestamp, new Command(userId, RemoveEntry, entryId, null));
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
            AddCommand(timestamp, new Command(userId, MarkDone, entryId, null));
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            AddCommand(timestamp, new Command(userId, MarkUndone, entryId, null));
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
            UpdateNotes();
            return notes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void AddCommand(long timestamp, Command command)
        {
            if (commandsByTimestamps.ContainsKey(timestamp))
            {
                ResolveConflict(timestamp, command);
            }
            else
            {
                commandsByTimestamps.Add(timestamp, new List<Command> {command});
            }
        }

        private void ResolveConflict(long timestamp, Command newCommand)
        {
            var oldCommand = commandsByTimestamps[timestamp].Last(x => x.EntryId == newCommand.EntryId);
            if (oldCommand == null)
            {
                commandsByTimestamps[timestamp].Add(newCommand);
                return;
            }

            if (newCommand.Action == oldCommand.Action && newCommand.UserId < oldCommand.UserId)
            {
                commandsByTimestamps[timestamp].Remove(oldCommand);
                commandsByTimestamps[timestamp].Add(newCommand);
            }

            FixCommandOrder(timestamp, oldCommand, newCommand, MarkDone, MarkUndone);
            FixCommandOrder(timestamp, oldCommand, newCommand, AddEntry, RemoveEntry);
            FixCommandOrder(timestamp, oldCommand, newCommand, AddEntry, MarkDone);
            FixCommandOrder(timestamp, oldCommand, newCommand, MarkDone, RemoveEntry);
        }

        private void FixCommandOrder(
            long timestamp,
            Command oldCommand,
            Command newCommand,
            Action<int, string> firstCommand,
            Action<int, string> secondCommand)
        {
            if (oldCommand.Action == firstCommand && newCommand.Action == secondCommand)
            {
                commandsByTimestamps[timestamp].Remove(oldCommand);
                commandsByTimestamps[timestamp].Add(oldCommand);
                commandsByTimestamps[timestamp].Add(newCommand);
            }
            else if (oldCommand.Action == secondCommand && newCommand.Action == firstCommand)
            {
                commandsByTimestamps[timestamp].Remove(oldCommand);
                commandsByTimestamps[timestamp].Add(newCommand);
                commandsByTimestamps[timestamp].Add(oldCommand);
            }
        }

        private void AddEntry(int entryId, string name)
        {
            if (notes.ContainsKey(entryId))
            {
                removedNotes.Remove(entryId);
                notes[entryId] = new Entry(entryId, name, notes[entryId].State);
            }
            else
            {
                notes.Add(entryId, new Entry(entryId, name, EntryState.Undone));
            }
        }

        private void RemoveEntry(int entryId, string _)
        {
            if (notes.ContainsKey(entryId))
            {
                removedNotes.Add(entryId);
            }
        }

        private void MarkDone(int entryId, string _)
        {
            if (notes.ContainsKey(entryId))
            {
                notes[entryId] = notes[entryId].MarkDone();
            }
            else
            {
                notes.Add(entryId, new Entry(entryId, null, EntryState.Done));
            }
        }

        private void MarkUndone(int entryId, string _)
        {
            if (notes.ContainsKey(entryId))
            {
                notes[entryId] = notes[entryId].MarkUndone();
            }
            else
            {
                notes.Add(entryId, new Entry(entryId, null, EntryState.Undone));
            }
        }

        private void UpdateNotes()
        {
            notes.Clear();

            foreach (var commandList in commandsByTimestamps.Values)
            {
                foreach (var command in commandList.Where(command => !bannedUsers.Contains(command.UserId)))
                {
                    command.Action.Invoke(command.EntryId, command.Text);
                }
            }

            notes = notes
                .Where(x => x.Value.Name != null && !removedNotes.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public class Command
    {
        public int UserId;
        public Action<int, string> Action;
        public int EntryId;
        public string Text;

        public Command(int userId, Action<int, string> action, int entryId, string text)
        {
            UserId = userId;
            Action = action;
            EntryId = entryId;
            Text = text;
        }
    }
}