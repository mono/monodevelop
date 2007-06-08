
using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;

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
		
		public void ExecuteCommand (IProgressMonitor monitor, IProject entry, CustomCommandType type)
		{
			ExecuteCommand (monitor, entry, type, null);
		}
		
		public void ExecuteCommand (IProgressMonitor monitor, IProject entry, CustomCommandType type, ExecutionContext context)
		{
			foreach (CustomCommand cmd in this) {
				if (cmd.Type == type)
					cmd.Execute (monitor, entry, context);
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
