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
using GtkSharp;

using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

using Stock = MonoDevelop.Core.Gui.Stock;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public class BreakpointPad : IPadContent
	{
		BreakpointStore bps;
		
		MonoDevelop.Ide.Gui.Components.PadTreeView tree;
		Gtk.TreeStore store;
		VBox control;
		ScrolledWindow sw;
		CommandEntrySet menuSet;
		TreeViewState treeState;
		
		EventHandler<BreakpointEventArgs> breakpointUpdatedHandler;
		
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
		
		public BreakpointPad()
		{
			control = new VBox ();
			
			// Toolbar and menu definitions
			
			LocalCommandEntry gotoCmd = new LocalCommandEntry (LocalCommands.GoToFile, GettextCatalog.GetString ("Go to File"));
			LocalCommandEntry propertiesCmd = new LocalCommandEntry (LocalCommands.Properties, GettextCatalog.GetString ("Properties"), Gtk.Stock.Properties);
			
			menuSet = new CommandEntrySet ();
			menuSet.Add (gotoCmd);
			menuSet.AddSeparator ();
			menuSet.AddItem (DebugCommands.EnableDisableBreakpoint);
			menuSet.AddItem (DebugCommands.ClearAllBreakpoints);
			menuSet.AddItem (DebugCommands.DisableAllBreakpoints);
			menuSet.AddItem (EditCommands.Delete);
			menuSet.AddSeparator ();
			menuSet.Add (propertiesCmd);
			
			CommandEntrySet toolbarSet = new CommandEntrySet ();
			toolbarSet.AddItem (DebugCommands.EnableDisableBreakpoint);
			toolbarSet.AddItem (DebugCommands.ClearAllBreakpoints);
			toolbarSet.AddItem (DebugCommands.DisableAllBreakpoints);
			toolbarSet.AddItem (EditCommands.Delete);
			toolbarSet.AddSeparator ();
			toolbarSet.Add (propertiesCmd);
			
			Gtk.Toolbar toolbar = IdeApp.CommandService.CreateToolbar ("bps", toolbarSet, control);
			toolbar.IconSize = IconSize.Menu;
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.ShowArrow = false;
			
			control.PackStart (toolbar, false, false, 0);
			
			// The breakpoint list
			
			store = new TreeStore (typeof(string), typeof (bool), typeof(string), typeof(object), typeof(string), typeof(string), typeof(string), typeof(string));

			tree = new MonoDevelop.Ide.Gui.Components.PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			
			treeState = new TreeViewState (tree, (int) Columns.Breakpoint);
							
			TreeViewColumn col = new TreeViewColumn ();
			CellRenderer crp = new CellRendererPixbuf ();
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
			FrameCol.Title = GettextCatalog.GetString ("File");
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
			sw.ShadowType = ShadowType.In;
			sw.Add (tree);
			
			control.PackStart (sw, true, true, 0);
			
			control.ShowAll ();
			
			bps = DebuggingService.Breakpoints;
			
			UpdateDisplay ();
			
			tree.PopupMenu += new PopupMenuHandler (OnPopupMenu);
			tree.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPressed);

			breakpointUpdatedHandler = DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointUpdated));
			
			DebuggingService.Breakpoints.BreakpointAdded += OnBpAdded;
			DebuggingService.Breakpoints.BreakpointRemoved += OnBpRemoved;
			DebuggingService.Breakpoints.Changed += OnBpChanged;
			DebuggingService.Breakpoints.BreakpointUpdated += breakpointUpdatedHandler;
			
			tree.RowActivated += OnRowActivated;
			tree.KeyPressEvent += OnKeyPressed;
		}
		
		public void Dispose ()
		{
			DebuggingService.Breakpoints.BreakpointAdded -= OnBpAdded;
			DebuggingService.Breakpoints.BreakpointRemoved -= OnBpRemoved;
			DebuggingService.Breakpoints.Changed -= OnBpChanged;
			DebuggingService.Breakpoints.BreakpointUpdated -= breakpointUpdatedHandler;
		}

		
		[GLib.ConnectBefore]
		void OnButtonPressed (object o, ButtonPressEventArgs args)
		{
			// Show the menu with a small delay, since some options depend on a
			// tree item to be selected, and this click may select a row
			Gtk.Application.Invoke (delegate {
				if (args.Event.Button == 3)
					ShowPopup ();
			});
		}
		private void OnPopupMenu (object o, PopupMenuArgs args)
		{
			ShowPopup ();
		}

		private void ShowPopup ()
		{
			IdeApp.CommandService.ShowContextMenu (menuSet, tree);
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
				IdeApp.Workbench.OpenDocument (bp.FileName, bp.Line, 1, true);	
			}
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDeleted ()
		{
			TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				Breakpoint bp = (Breakpoint) store.GetValue (iter, (int) Columns.Breakpoint);	
				bps.Remove (bp);
			}	
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (LocalCommands.GoToFile)]
		[CommandUpdateHandler (LocalCommands.Properties)]
		[CommandUpdateHandler (DebugCommands.EnableDisableBreakpoint)]
		protected void UpdateBpCommand (CommandInfo cmd)
		{
			TreeIter iter;
			cmd.Enabled = tree.Selection.GetSelected (out iter);
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
		
		void IPadContent.Initialize (IPadWindow window)
		{
			window.Title = GettextCatalog.GetString ("Breakpoints");
			window.Icon = Stock.OutputIcon;
		}
		
		public void UpdateDisplay ()
		{
			treeState.Save ();
			
			store.Clear ();
			if (bps != null) {		
				foreach (Breakpoint bp in bps.GetBreakpoints () ){
					string traceExp = bp.HitAction == HitAction.PrintExpression ? bp.TraceExpression : "";
					string traceVal = bp.HitAction == HitAction.PrintExpression ? bp.LastTraceValue : "";
					string hitCount = bp.HitCount > 0 ? bp.HitCount.ToString () : "";
					if (bp.Enabled)
						store.AppendValues ("md-breakpoint", true, bp.FileName + ":" + bp.Line.ToString (), bp, bp.ConditionExpression, traceExp, hitCount, traceVal);
					else
						store.AppendValues ("md-breakpoint-disabled", false, bp.FileName + ":" + bp.Line.ToString (), bp, bp.ConditionExpression, traceExp, hitCount, traceVal);
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
		
		protected void OnBpAdded (object o, EventArgs args)
		{
			UpdateDisplay ();	
		}
		
		protected void OnBpRemoved (object o, EventArgs args)
		{
			UpdateDisplay ();	
		}
		
		protected void OnBpChanged (object o, EventArgs args)
		{
			UpdateDisplay ();	
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
				
		[GLib.ConnectBefore]
		void OnKeyPressed (object o, Gtk.KeyPressEventArgs e)
		{
			if (e.Event.Key != Gdk.Key.Delete){
				e.RetVal = true;
				return;
			}
			OnDeleted();
			e.RetVal = true;
		}
		
		protected void OnDeleteClicked (object o, EventArgs args)
		{
			OnDeleted ();
		}
	}
}
