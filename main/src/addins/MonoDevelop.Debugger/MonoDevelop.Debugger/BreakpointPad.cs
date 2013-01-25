// BreakpointPad.cs
//
// Author:
//   Alfonso Santos Luaces <asantosluaces@gmail.com>
//
// Copyright (c) 2008 Alfonso Santos Luaces
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

using GLib;
using Gtk;
using System;

using Stock = MonoDevelop.Ide.Gui.Stock;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using Mono.Debugging.Client;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	public class BreakpointPad : IPadContent
	{
		BreakpointStore bps;
		
		PadTreeView tree;
		Gtk.TreeStore store;
		Widget control;
		ScrolledWindow sw;
		CommandEntrySet menuSet;
		TreeViewState treeState;
		
		EventHandler<BreakpointEventArgs> breakpointUpdatedHandler;
		EventHandler<BreakpointEventArgs> breakpointRemovedHandler;
		EventHandler<BreakpointEventArgs> breakpointAddedHandler;
		EventHandler breakpointChangedHandler;
		
		enum Columns
		{
			Icon,
			Selected,
			FileName,
			Breakpoint,
			Condition,
			TraceExp,
			HitCount,
			LastTrace
		}
		
		enum LocalCommands
		{
			GoToFile,
			Properties
		}
		
		public void Initialize (IPadWindow window)
		{
			// Toolbar and menu definitions
			
			ActionCommand gotoCmd = new ActionCommand (LocalCommands.GoToFile, GettextCatalog.GetString ("Go to File"));
			ActionCommand propertiesCmd = new ActionCommand (LocalCommands.Properties, GettextCatalog.GetString ("Properties"), Gtk.Stock.Properties);
			
			menuSet = new CommandEntrySet ();
			menuSet.Add (gotoCmd);
			menuSet.AddSeparator ();
			menuSet.AddItem (DebugCommands.EnableDisableBreakpoint);
			menuSet.AddItem (DebugCommands.ClearAllBreakpoints);
			menuSet.AddItem (DebugCommands.DisableAllBreakpoints);
			menuSet.AddItem (EditCommands.DeleteKey);
			menuSet.AddSeparator ();
			menuSet.Add (propertiesCmd);
			
			CommandEntrySet toolbarSet = new CommandEntrySet ();
			toolbarSet.AddItem (DebugCommands.EnableDisableBreakpoint);
			toolbarSet.AddItem (DebugCommands.ClearAllBreakpoints);
			toolbarSet.AddItem (DebugCommands.DisableAllBreakpoints);
			toolbarSet.AddItem (EditCommands.Delete);
			toolbarSet.AddSeparator ();
			toolbarSet.Add (propertiesCmd);
			
			// The breakpoint list
			
			store = new TreeStore (typeof(string), typeof (bool), typeof(string), typeof(object), typeof(string), typeof(string), typeof(string), typeof(string));

			tree = new PadTreeView ();
			tree.Model = store;
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			tree.DoPopupMenu = ShowPopup;
			tree.KeyPressEvent += OnKeyPressEvent;
			
			treeState = new TreeViewState (tree, (int) Columns.Breakpoint);
							
			TreeViewColumn col = new TreeViewColumn ();
			CellRenderer crp = new CellRendererIcon ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", (int) Columns.Icon);
			tree.AppendColumn (col);
			
			Gtk.CellRendererToggle toggleRender = new Gtk.CellRendererToggle ();
			toggleRender.Toggled += new ToggledHandler (ItemToggled);
			col = new TreeViewColumn ();
			col.PackStart (toggleRender, false);
			col.AddAttribute (toggleRender, "active", (int) Columns.Selected);
			tree.AppendColumn (col);
			
			TreeViewColumn FrameCol = new TreeViewColumn ();
			CellRenderer crt = tree.TextRenderer;
			FrameCol.Title = GettextCatalog.GetString ("Name");
			FrameCol.PackStart (crt, true);
			FrameCol.AddAttribute (crt, "text", (int) Columns.FileName);
			FrameCol.Resizable = true;
			FrameCol.Alignment = 0.0f;
			tree.AppendColumn (FrameCol);

			col = tree.AppendColumn (GettextCatalog.GetString ("Condition"), crt, "text", (int) Columns.Condition);
			col.Resizable = true;
			
			col = tree.AppendColumn (GettextCatalog.GetString ("Trace Expression"), crt, "text", (int) Columns.TraceExp);
			col.Resizable = true;
			
			col = tree.AppendColumn (GettextCatalog.GetString ("Hit Count"), crt, "text", (int) Columns.HitCount);
			col.Resizable = true;
			
			col = tree.AppendColumn (GettextCatalog.GetString ("Last Trace"), crt, "text", (int) Columns.LastTrace);
			col.Resizable = true;
			
			sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add (tree);
			
			control = sw;
			
			control.ShowAll ();
			
			bps = DebuggingService.Breakpoints;
			
			UpdateDisplay ();

			breakpointUpdatedHandler = DispatchService.GuiDispatch<EventHandler<BreakpointEventArgs>> (OnBreakpointUpdated);
			breakpointRemovedHandler = DispatchService.GuiDispatch<EventHandler<BreakpointEventArgs>> (OnBreakpointRemoved);
			breakpointAddedHandler = DispatchService.GuiDispatch<EventHandler<BreakpointEventArgs>> (OnBreakpointAdded);
			breakpointChangedHandler = DispatchService.GuiDispatch<EventHandler> (OnBreakpointChanged);
			
			DebuggingService.Breakpoints.BreakpointAdded += breakpointAddedHandler;
			DebuggingService.Breakpoints.BreakpointRemoved += breakpointRemovedHandler;
			DebuggingService.Breakpoints.Changed += breakpointChangedHandler;
			DebuggingService.Breakpoints.BreakpointUpdated += breakpointUpdatedHandler;
			
			DebuggingService.PausedEvent += OnDebuggerStatusCheck;
			DebuggingService.ResumedEvent += OnDebuggerStatusCheck;
			DebuggingService.StoppedEvent += OnDebuggerStatusCheck;
			
			tree.RowActivated += OnRowActivated;
			
			DockItemToolbar toolbar = window.GetToolbar (PositionType.Top);
			toolbar.Add (toolbarSet, sw);
			toolbar.ShowAll ();
		}
		
		public void Dispose ()
		{
			DebuggingService.Breakpoints.BreakpointAdded -= breakpointAddedHandler;
			DebuggingService.Breakpoints.BreakpointRemoved -= breakpointRemovedHandler;
			DebuggingService.Breakpoints.Changed -= breakpointChangedHandler;
			DebuggingService.Breakpoints.BreakpointUpdated -= breakpointUpdatedHandler;
			
			DebuggingService.PausedEvent -= OnDebuggerStatusCheck;
			DebuggingService.ResumedEvent -= OnDebuggerStatusCheck;
			DebuggingService.StoppedEvent -= OnDebuggerStatusCheck;
		}

		void ShowPopup (Gdk.EventButton evt)
		{
			IdeApp.CommandService.ShowContextMenu (tree, evt, menuSet, tree);
		}
		
		[CommandHandler (LocalCommands.Properties)]
		protected void OnProperties ()
		{
			TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				Breakpoint bp = (Breakpoint) store.GetValue (iter, (int) Columns.Breakpoint);
				if (DebuggingService.ShowBreakpointProperties (bp, false))
					UpdateDisplay ();
			}
		}
		
		[CommandHandler (DebugCommands.EnableDisableBreakpoint)]
		protected void OnEnableDisable ()
		{
			TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				Breakpoint bp = (Breakpoint) store.GetValue (iter, (int) Columns.Breakpoint);
				bp.Enabled = !bp.Enabled;
			}
		}
		
		[CommandHandler (LocalCommands.GoToFile)]
		protected void OnBpJumpTo ()
		{
			TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				Breakpoint bp = (Breakpoint) store.GetValue (iter, (int) Columns.Breakpoint);
				if (!string.IsNullOrEmpty (bp.FileName))
					IdeApp.Workbench.OpenDocument (bp.FileName, bp.Line, 1);
			}
		}
		
		[CommandHandler (EditCommands.Delete)]
		[CommandHandler (EditCommands.DeleteKey)]
		protected void OnDeleted ()
		{
			bool deleted = false;

			DebuggingService.Breakpoints.BreakpointRemoved -= breakpointRemovedHandler;

			foreach (TreePath path in tree.Selection.GetSelectedRows ()) {
				TreeIter iter;
				
				if (!store.GetIter (out iter, path))
					continue;
				
				var bp = (Breakpoint) store.GetValue (iter, (int) Columns.Breakpoint);
				bps.Remove (bp);
				deleted = true;
			}
			
			DebuggingService.Breakpoints.BreakpointRemoved += breakpointRemovedHandler;
			
			if (deleted)
				UpdateDisplay ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (EditCommands.DeleteKey)]
		[CommandUpdateHandler (LocalCommands.GoToFile)]
		[CommandUpdateHandler (LocalCommands.Properties)]
		[CommandUpdateHandler (DebugCommands.EnableDisableBreakpoint)]
		protected void UpdateBpCommand (CommandInfo cmd)
		{
			TreeIter iter;
			cmd.Enabled = tree.Selection.GetSelected (out iter);
		}

		[GLib.ConnectBefore]
		void OnKeyPressEvent (object sender, KeyPressEventArgs args)
		{
			// Delete the currently selected breakpoint(s) with any delete key
			switch (args.Event.Key) {
			case Gdk.Key.Delete:
			case Gdk.Key.KP_Delete:
			case Gdk.Key.BackSpace:
				// Delete the selected breakpoints
				bool deleted = false;

				DebuggingService.Breakpoints.BreakpointRemoved -= breakpointRemovedHandler;

				foreach (TreePath path in tree.Selection.GetSelectedRows ()) {
					TreeIter iter;
					
					if (!store.GetIter (out iter, path))
						continue;
					
					var bp = (Breakpoint) store.GetValue (iter, (int) Columns.Breakpoint);
					bps.Remove (bp);
					deleted = true;
				}

				DebuggingService.Breakpoints.BreakpointRemoved += breakpointRemovedHandler;

				if (deleted) {
					args.RetVal = true;
					UpdateDisplay ();
				}

				break;
			}
		}
		
		private void ItemToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString(out iter, args.Path)) {
				bool val = (bool) store.GetValue(iter, (int)Columns.Selected);
				Breakpoint bp = (Breakpoint) store.GetValue (iter, (int) Columns.Breakpoint);
				store.SetValue (iter, (int)Columns.Selected, !val);
				bp.Enabled = !bp.Enabled;
			}
		}
		
		public void UpdateDisplay ()
		{
			treeState.Save ();
			
			store.Clear ();
			if (bps != null) {		
				foreach (Breakpoint bp in bps.GetBreakpoints ()) {
					string traceExp = bp.HitAction == HitAction.PrintExpression ? bp.TraceExpression : "";
					string traceVal = bp.HitAction == HitAction.PrintExpression ? bp.LastTraceValue : "";
					string hitCount = bp.HitCount > 0 ? bp.HitCount.ToString () : "";
					string name;
					
					if (bp is FunctionBreakpoint) {
						FunctionBreakpoint fb = (FunctionBreakpoint) bp;
						
						if (fb.ParamTypes != null)
							name = fb.FunctionName + "(" + string.Join (", ", fb.ParamTypes) + ")";
						else
							name = fb.FunctionName;
					} else {
						name = string.Format ("{0}:{1},{2}", ((Breakpoint) bp).FileName, bp.Line, bp.Column);
					}
					
					if (bp.Enabled)
						store.AppendValues ("md-breakpoint", true, name, bp, bp.ConditionExpression, traceExp, hitCount, traceVal);
					else
						store.AppendValues ("md-breakpoint-disabled", false, name, bp, bp.ConditionExpression, traceExp, hitCount, traceVal);
				}
			}
			treeState.Load ();
		}
		
		void OnBreakpointUpdated (object s, BreakpointEventArgs args)
		{
			TreeIter it;
			if (!store.GetIterFirst (out it))
				return;
			do {
				Breakpoint bp = (Breakpoint) store.GetValue (it, (int) Columns.Breakpoint);
				if (bp == args.Breakpoint) {
					string traceVal = bp.HitAction == HitAction.PrintExpression ? bp.LastTraceValue : "";
					string hitCount = bp.HitCount > 0 ? bp.HitCount.ToString () : "";
					store.SetValue (it, (int) Columns.HitCount, hitCount);
					store.SetValue (it, (int) Columns.LastTrace, traceVal);
					break;
				}
			} while (store.IterNext (ref it));
		}
		
		protected void OnBreakpointAdded (object o, EventArgs args)
		{
			UpdateDisplay ();	
		}
		
		protected void OnBreakpointRemoved (object o, EventArgs args)
		{
			UpdateDisplay ();	
		}
		
		protected void OnBreakpointChanged (object o, EventArgs args)
		{
			UpdateDisplay ();	
		}
		
		void OnDebuggerStatusCheck (object s, EventArgs a)
		{
			if (control != null)
				control.Sensitive = !DebuggingService.Breakpoints.IsReadOnly;
		}

		void OnRowActivated (object o, Gtk.RowActivatedArgs args)
		{
			OnBpJumpTo ();
		}
		
		public Gtk.Widget Control {
			get {
				return control;
			}
		}

		public string Id {
			get { return "MonoDevelop.Debugger.BreakpointPad"; }
		}

		public string DefaultPlacement {
			get { return "Bottom"; }
		}

		public void RedrawContent ()
		{
			UpdateDisplay ();
		}
		
		protected void OnDeleteClicked (object o, EventArgs args)
		{
			OnDeleted ();
		}
	}
}