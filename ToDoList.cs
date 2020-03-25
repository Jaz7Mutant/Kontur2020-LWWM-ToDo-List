using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        private Dictionary<int, Entry> _notes = new Dictionary<int, Entry>();

        private readonly SortedDictionary<long, List<Command>> _commandsByTimestamps =
            new SortedDictionary<long, List<Command>>();

        private readonly HashSet<int> _bannedUsers = new HashSet<int>();
        private readonly HashSet<int> _removedNotes = new HashSet<int>();

        public int Count
        {
            get
            {
                UpdateNotes();
                return _notes.Count;
            }
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
            _bannedUsers.Add(userId);
        }

        public void AllowUser(int userId)
        {
            if (_bannedUsers.Contains(userId))
            {
                _bannedUsers.Remove(userId);
            }
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            UpdateNotes();
            return _notes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void AddCommand(long timestamp, Command command)
        {
            if (_commandsByTimestamps.ContainsKey(timestamp))
            {
                ResolveConflict(timestamp, command);
            }
            else
            {
                _commandsByTimestamps.Add(timestamp, new List<Command> {command});
            }
        }

        private void ResolveConflict(long timestamp, Command newCommand)
        {
            var oldCommand = _commandsByTimestamps[timestamp].Last(x => x.EntryId == newCommand.EntryId);
            if (oldCommand == null)
            {
                _commandsByTimestamps[timestamp].Add(newCommand);
                return;
            }

            if (newCommand.Action == oldCommand.Action && newCommand.UserId < oldCommand.UserId)
            {
                _commandsByTimestamps[timestamp].Remove(oldCommand);
                _commandsByTimestamps[timestamp].Add(newCommand);
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
                _commandsByTimestamps[timestamp].Remove(oldCommand);
                _commandsByTimestamps[timestamp].Add(oldCommand);
                _commandsByTimestamps[timestamp].Add(newCommand);
            }
            else if (oldCommand.Action == secondCommand && newCommand.Action == firstCommand)
            {
                _commandsByTimestamps[timestamp].Remove(oldCommand);
                _commandsByTimestamps[timestamp].Add(newCommand);
                _commandsByTimestamps[timestamp].Add(oldCommand);
            }
        }

        private void AddEntry(int entryId, string name)
        {
            if (_notes.ContainsKey(entryId))
            {
                _removedNotes.Remove(entryId);
                _notes[entryId] = new Entry(entryId, name, _notes[entryId].State);
            }
            else
            {
                _notes.Add(entryId, new Entry(entryId, name, EntryState.Undone));
            }
        }

        private void RemoveEntry(int entryId, string _)
        {
            if (_notes.ContainsKey(entryId))
            {
                _removedNotes.Add(entryId);
            }
        }

        private void MarkDone(int entryId, string _)
        {
            if (_notes.ContainsKey(entryId))
            {
                _notes[entryId] = _notes[entryId].MarkDone();
            }
            else
            {
                _notes.Add(entryId, new Entry(entryId, null, EntryState.Done));
            }
        }

        private void MarkUndone(int entryId, string _)
        {
            if (_notes.ContainsKey(entryId))
            {
                _notes[entryId] = _notes[entryId].MarkUndone();
            }
            else
            {
                _notes.Add(entryId, new Entry(entryId, null, EntryState.Undone));
            }
        }

        private void UpdateNotes()
        {
            _notes.Clear();

            foreach (var commandList in _commandsByTimestamps.Values)
            {
                foreach (var command in commandList.Where(command => !_bannedUsers.Contains(command.UserId)))
                {
                    command.Action.Invoke(command.EntryId, command.Text);
                }
            }

            _notes = _notes
                .Where(x => x.Value.Name != null && !_removedNotes.Contains(x.Key))
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