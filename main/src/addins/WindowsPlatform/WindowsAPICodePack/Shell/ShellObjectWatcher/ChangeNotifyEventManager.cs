using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.WindowsAPICodePack.Shell
{
    internal class ChangeNotifyEventManager
    {
        #region Change order
        private static readonly ShellObjectChangeTypes[] _changeOrder = {
            ShellObjectChangeTypes.ItemCreate,
            ShellObjectChangeTypes.ItemRename,            
            ShellObjectChangeTypes.ItemDelete,

            ShellObjectChangeTypes.AttributesChange,

            ShellObjectChangeTypes.DirectoryCreate,
            ShellObjectChangeTypes.DirectoryDelete,
            ShellObjectChangeTypes.DirectoryContentsUpdate,
            ShellObjectChangeTypes.DirectoryRename,

            ShellObjectChangeTypes.Update,

            ShellObjectChangeTypes.MediaInsert,
            ShellObjectChangeTypes.MediaRemove,            
            ShellObjectChangeTypes.DriveAdd,
            ShellObjectChangeTypes.DriveRemove,            
            ShellObjectChangeTypes.NetShare,
            ShellObjectChangeTypes.NetUnshare,
            
            ShellObjectChangeTypes.ServerDisconnect,
            ShellObjectChangeTypes.SystemImageUpdate,
            
            ShellObjectChangeTypes.AssociationChange,
            ShellObjectChangeTypes.FreeSpace,
            
            ShellObjectChangeTypes.DiskEventsMask,
            ShellObjectChangeTypes.GlobalEventsMask,
            ShellObjectChangeTypes.AllEventsMask
        };
        #endregion

        private Dictionary<ShellObjectChangeTypes, Delegate> _events = new Dictionary<ShellObjectChangeTypes, Delegate>();

        public void Register(ShellObjectChangeTypes changeType, Delegate handler)
        {
            Delegate del;
            if (!_events.TryGetValue(changeType, out del))
            {
                _events.Add(changeType, handler);
            }
            else
            {
                del = MulticastDelegate.Combine(del, handler);
                _events[changeType] = del;
            }
        }

        public void Unregister(ShellObjectChangeTypes changeType, Delegate handler)
        {
            Delegate del;
            if (_events.TryGetValue(changeType, out del))
            {
                del = MulticastDelegate.Remove(del, handler);
                if (del == null) // It's a bug in .NET if del is non-null and has an empty invocation list.
                {
                    _events.Remove(changeType);
                }
                else
                {
                    _events[changeType] = del;
                }
            }
        }

        public void UnregisterAll()
        {
            _events.Clear();
        }

        public void Invoke(object sender, ShellObjectChangeTypes changeType, EventArgs args)
        {
            // Removes FromInterrupt flag if pressent
            changeType = changeType & ~ShellObjectChangeTypes.FromInterrupt;

            Delegate del;
            foreach (var change in _changeOrder.Where(x => (x & changeType) != 0))
            {
                if (_events.TryGetValue(change, out del))
                {
                    del.DynamicInvoke(sender, args);
                }
            }
        }

        public ShellObjectChangeTypes RegisteredTypes
        {
            get
            {
                return _events.Keys.Aggregate<ShellObjectChangeTypes, ShellObjectChangeTypes>(
                    ShellObjectChangeTypes.None,
                    (accumulator, changeType) => (changeType | accumulator));
            }
        }
    }
}
