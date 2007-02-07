
using System;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class CustomCommandCollection: List<CustomCommand>
	{
		public CustomCommandCollection Clone ()
		{
			CustomCommandCollection col = new CustomCommandCollection ();
			col.CopyFrom (this);
			return col;
		}
		
		public void CopyFrom (CustomCommandCollection col)
		{
			Clear ();
			foreach (CustomCommand cmd in col)
				Add (cmd.Clone ());
		}
		
		public void ExecuteCommand (IProgressMonitor monitor, CombineEntry entry, CustomCommandType type)
		{
			foreach (CustomCommand cmd in this) {
				if (cmd.Type == type)
					cmd.Execute (monitor, entry);
				if (monitor.IsCancelRequested)
					break;
			}
		}
		
		public bool HasCommands (CustomCommandType type)
		{
			foreach (CustomCommand cmd in this)
				if (cmd.Type == type)
					return true;
			return false;
		}
	}
}
