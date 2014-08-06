// UnknownEntryNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using System;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class UnknownEntryNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(UnknownSolutionItem); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(UnknownEntryCommandHandler); }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			UnknownSolutionItem entry = (UnknownSolutionItem) dataObject;
			
			if (entry is UnloadedSolutionItem) {
				nodeInfo.Icon = Context.GetIcon (MonoDevelop.Ide.Gui.Stock.Project);
				Xwt.Drawing.Image gicon = Context.GetComposedIcon (nodeInfo.Icon, "fade");
				if (gicon == null) {
					gicon = nodeInfo.Icon.WithAlpha (0.5);
					Context.CacheComposedIcon (nodeInfo.Icon, "fade", gicon);
				}
				nodeInfo.Icon = gicon;
				nodeInfo.Label = GettextCatalog.GetString ("<span foreground='grey'>{0} <span size='small'>(Unavailable)</span></span>", GLib.Markup.EscapeText (entry.Name));
			}
			else if (entry.LoadError.Length > 0) {
				nodeInfo.Icon = Context.GetIcon (MonoDevelop.Ide.Gui.Stock.Project).WithAlpha (0.5);
				nodeInfo.Label = entry.Name;
				nodeInfo.StatusSeverity = TaskSeverity.Error;
				nodeInfo.StatusMessage = GettextCatalog.GetString ("Load failed: ") + entry.LoadError;
			} else {
				nodeInfo.Icon = Context.GetIcon (MonoDevelop.Ide.Gui.Stock.Project);
				var gicon = Context.GetComposedIcon (nodeInfo.Icon, "fade");
				if (gicon == null) {
					gicon = nodeInfo.Icon.WithAlpha (0.5);
					Context.CacheComposedIcon (nodeInfo.Icon, "fade", gicon);
				}
				nodeInfo.Icon = gicon;
				nodeInfo.Label = GLib.Markup.EscapeText (entry.Name);
			}
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((UnknownSolutionItem)dataObject).Name;
		}
	}
	
	public class UnknownEntryCommandHandler: NodeCommandHandler
	{
		[CommandHandler (ProjectCommands.Reload)]
		[AllowMultiSelection]
		public void OnReload ()
		{
			var solutions = new HashSet<Solution> ();
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
				m.BeginTask (null, CurrentNodes.Length);
				foreach (ITreeNavigator node in CurrentNodes) {
					UnknownSolutionItem entry = (UnknownSolutionItem) node.DataItem;
					if (!entry.Enabled) {
						entry.Enabled = true;
						solutions.Add (entry.ParentSolution);
					}
					entry.ParentFolder.ReloadItem (m, entry);
					m.Step (1);
				}
				m.EndTask ();
			}
			IdeApp.ProjectOperations.Save (solutions);
		}
		
		[CommandUpdateHandler (ProjectCommands.Reload)]
		public void OnUpdateReload (CommandInfo info)
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				UnknownSolutionItem entry = (UnknownSolutionItem) node.DataItem;
				if (entry.ParentFolder == null) {
					info.Enabled = false;
					return;
				}
			}
		}

		[CommandHandler (ProjectCommands.Unload)]
		[AllowMultiSelection]
		public void OnUnload ()
		{
			HashSet<Solution> solutions = new HashSet<Solution> ();
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
				m.BeginTask (null, CurrentNodes.Length);
				foreach (ITreeNavigator nav in CurrentNodes) {
					UnknownSolutionItem p = (UnknownSolutionItem) nav.DataItem;
					p.Enabled = false;
					p.ParentFolder.ReloadItem (m, p);
					m.Step (1);
					solutions.Add (p.ParentSolution);
				}
				m.EndTask ();
			}
			IdeApp.ProjectOperations.Save (solutions);
		}

		[CommandUpdateHandler (ProjectCommands.Unload)]
		public void OnUpdateUnload (CommandInfo info)
		{
			info.Enabled = CurrentNodes.All (nav => ((UnknownSolutionItem)nav.DataItem).Enabled);
		}

		[CommandHandler (ProjectCommands.EditSolutionItem)]
		public void OnEditUnknownSolutionItem ()
		{
			UnknownSolutionItem si = (UnknownSolutionItem) CurrentNode.DataItem;
			IdeApp.Workbench.OpenDocument (si.FileName);
		}

		[CommandUpdateHandler (ProjectCommands.EditSolutionItem)]
		public void OnEditUnknownSolutionItemUpdate (CommandInfo info)
		{
			var si = (UnknownSolutionItem) CurrentNode.DataItem;
			info.Visible = !IdeApp.Workbench.Documents.Any (d => d.FileName == si.FileName);
		}
		
		public override void DeleteItem ()
		{
			UnknownSolutionItem item = (UnknownSolutionItem) CurrentNode.DataItem;
			SolutionFolder cmb = item.ParentFolder;
			if (cmb == null)
				return;
			
			bool yes = MessageService.Confirm (GettextCatalog.GetString (
				"Do you really want to remove project '{0}' from solution '{1}'", item.FileName, cmb.Name), AlertButton.Remove);

			if (yes) {
				cmb.Items.Remove (item);
				item.Dispose ();
				IdeApp.ProjectOperations.Save (cmb.ParentSolution);
			}
		}
	}
}
