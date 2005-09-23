//
// CommandService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.Collections;

using MonoDevelop.Core.Services;
using MonoDevelop.Commands;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Gui;

namespace MonoDevelop.Services
{
	public class CommandService : AbstractService
	{
		CommandManager manager = new CommandManager ();
		
		public override void InitializeService()
		{
			base.InitializeService();
			
			ArrayList commandCodons = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Commands").BuildChildItems (null);
			foreach (Command cmd in commandCodons)
				manager.RegisterCommand (cmd, null);
				
			manager.CommandError += new CommandErrorHandler (OnCommandError);
			manager.EnableIdleUpdate = true;
		}
		
		public CommandManager CommandManager {
			get { return manager; }
		}
		
		public void SetRootWindow (Gtk.Window root)
		{
			manager.SetRootWindow (root);
		}
		
		public void RegisterGlobalHandler (object handler)
		{
			manager.RegisterGlobalHandler (handler);
		}
		
		public void UnregisterGlobalHandler (object handler)
		{
			manager.UnregisterGlobalHandler (handler);
		}
		
		public Gtk.MenuBar CreateMenuBar (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return manager.CreateMenuBar (addinPath, cset);
		}
		
		public Gtk.Toolbar[] CreateToolbarSet (string addinPath)
		{
			ArrayList bars = new ArrayList ();
			
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			foreach (CommandEntry ce in cset) {
				CommandEntrySet ces = ce as CommandEntrySet;
				if (ces != null)
					bars.Add (manager.CreateToolbar (addinPath + "/" + ces.Name, ces));
			}
			return (Gtk.Toolbar[]) bars.ToArray (typeof(Gtk.Toolbar));
		}
		
		public Gtk.Toolbar CreateToolbar (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return manager.CreateToolbar (addinPath, cset);
		}
		
		public Gtk.Menu CreateMenu (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return manager.CreateMenu (cset);
		}
		
		public Gtk.Menu CreateMenu (CommandEntrySet cset)
		{
			return manager.CreateMenu (cset);
		}
		
		public void ShowContextMenu (CommandEntrySet cset)
		{
			manager.ShowContextMenu (cset);
		}
		
		public void ShowContextMenu (string addinPath)
		{
			ShowContextMenu (CreateCommandEntrySet (addinPath));
		}
		
		public CommandEntrySet CreateCommandEntrySet (string addinPath)
		{
			CommandEntrySet cset = new CommandEntrySet ();
			ArrayList items = AddInTreeSingleton.AddInTree.GetTreeNode (addinPath).BuildChildItems (null);
			foreach (CommandEntry e in items)
				cset.Add (e);
			return cset;
		}
		
		void OnCommandError (object sender, CommandErrorArgs args)
		{
			Runtime.MessageService.ShowError (args.Exception, args.ErrorMessage);
		}
	}
}
