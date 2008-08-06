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
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public class BreakpointPad : Gtk.ScrolledWindow, IPadContent
	{
		
		BreakpointStore bps;
		
		Gtk.TreeView tree;
		Gtk.TreeStore store;
		VBox control;
		ToggleToolButton enableDisableBtn;
		ToolButton deleteBtn;
		ScrolledWindow sw;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		enum Columns
		{
			Icon,
			Selected,
			FileName,
			Line
		}
		
		public BreakpointPad()
		{
			control = new VBox ();
			
			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.Menu;
			control.PackStart (toolbar, false, false, 0);
			
			enableDisableBtn = new ToggleToolButton ();
			enableDisableBtn.Label = " " + GettextCatalog.GetString ("Enable / Disable");
			enableDisableBtn.Active = false;
			enableDisableBtn.Visible = true;
			enableDisableBtn.IsImportant = true;
			enableDisableBtn.IconWidget = new Gtk.Image (Gtk.Stock.ColorPicker, Gtk.IconSize.Button);
			enableDisableBtn.SetTooltip (tips, GettextCatalog.GetString ("Enable / Disable"), GettextCatalog.GetString ("Enable / Disable"));
			enableDisableBtn.Toggled += OnEnableDisableClicked;
			toolbar.Insert (enableDisableBtn, -1);
			
			toolbar.Insert (new SeparatorToolItem (), -1);
			
			deleteBtn = new ToolButton ("deleteBtn");
			deleteBtn.Label = " " + GettextCatalog.GetString ("Delete");
			deleteBtn.Visible = true;
			deleteBtn.IsImportant = true;
			deleteBtn.IconWidget = new Gtk.Image (Gtk.Stock.Delete, Gtk.IconSize.Button);
			deleteBtn.SetTooltip (tips, GettextCatalog.GetString ("Delete"), GettextCatalog.GetString ("Delete"));
			deleteBtn.Clicked += OnDeleteClicked;
			toolbar.Insert (deleteBtn, -1);
			
			this.ShadowType = ShadowType.In;

			store = new TreeStore (typeof(string), typeof (bool), typeof(string), typeof(string), typeof(string));

			tree = new TreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
							
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
			CellRenderer FrameRenderer = new CellRendererText ();
			FrameCol.Title = GettextCatalog.GetString ("File");
			FrameCol.PackStart (FrameRenderer, true);
			FrameCol.AddAttribute (FrameRenderer, "text", (int) Columns.FileName);
			FrameCol.Resizable = true;
			FrameCol.Alignment = 0.0f;
			tree.AppendColumn (FrameCol);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Line");
			CellRenderer crt = new CellRendererText ();
			col.PackStart (crt, false);
			col.AddAttribute (crt, "text", (int) Columns.Line);
			tree.AppendColumn (col);

			sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add (tree);
			
			control.Add (sw);
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.ShowArrow = false;
			
			Add (control);
			ShowAll ();
			
			bps = IdeApp.Services.DebuggingService.Breakpoints;
			
			UpdateDisplay ();
			
			tree.PopupMenu += new PopupMenuHandler (OnPopupMenu);
			tree.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPressed);
					
			IdeApp.Services.DebuggingService.Breakpoints.BreakpointAdded += OnBpAdded;
			IdeApp.Services.DebuggingService.Breakpoints.BreakpointRemoved += OnBpRemoved;
			IdeApp.Services.DebuggingService.Breakpoints.Changed += OnBpChanged;
			
			tree.RowActivated += OnRowActivated;
			tree.KeyPressEvent += OnKeyPressed;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			IdeApp.Services.DebuggingService.Breakpoints.BreakpointAdded -= OnBpAdded;
			IdeApp.Services.DebuggingService.Breakpoints.BreakpointRemoved -= OnBpRemoved;
			IdeApp.Services.DebuggingService.Breakpoints.Changed -= OnBpChanged;
		}

		
		[GLib.ConnectBefore]
		void OnButtonPressed (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				ShowPopup ();
		}
		private void OnPopupMenu (object o, PopupMenuArgs args)
		{
			ShowPopup ();
		}

		private void ShowPopup ()
		{
			Menu menu = new Menu ();
			
			MenuItem GoToBp = new MenuItem (GettextCatalog.GetString ("_Go to"));
			GoToBp.Activated += OnBpJumpTo;
		 	
			MenuItem Delete = new MenuItem (GettextCatalog.GetString ("Delete"));
			Delete.Activated += OnDeleteClicked;
			
			MenuItem EnableDisable = new MenuItem (GettextCatalog.GetString ("Enable/Disable"));
			EnableDisable.Activated += OnEnableDisable;
				
			menu.Append (GoToBp);
			menu.Append (Delete);
			menu.Append (EnableDisable);
			menu.Popup ();
			menu.ShowAll ();
		}
		
		protected void OnEnableDisable (object o, EventArgs args)
		{
			TreeIter iter;
			TreeModel model; 	
			if (tree.Selection.GetSelected (out model, out iter)) {
				string filename = (string) model.GetValue (iter, (int) Columns.FileName);
				string line = (string) model.GetValue (iter, (int) Columns.Line);
				EnableDisable(filename, int.Parse (line));
			}
		}
		
		private void ItemToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString(out iter, args.Path)) {
				bool val = (bool) store.GetValue(iter, (int)Columns.Selected);
				string filename = (string) store.GetValue (iter, (int) Columns.FileName);
				string line = (string) store.GetValue (iter, (int) Columns.Line);
				store.SetValue(iter, (int)Columns.Selected, !val);
				EnableDisable (filename, int.Parse (line));
			}
			
		}
		
		private void EnableDisable (string filename, int line)
		{
			foreach (Breakpoint bp in bps.GetBreakpointsAtFileLine (filename, line) )
			         bp.Enabled = !bp.Enabled;
		}
		
		void IPadContent.Initialize (IPadWindow window)
		{
			window.Title = "Breakpoint List";
			window.Icon = Stock.OutputIcon;
		}
		
		public void UpdateDisplay ()
		{
			store.Clear ();
			if (bps != null) {		
				foreach (Breakpoint bp in bps.GetBreakpoints () ){
					if (bp.Enabled)
						store.AppendValues (Gtk.Stock.No, true, bp.FileName, bp.Line.ToString () );
					else
						store.AppendValues (Gtk.Stock.Yes, false, bp.FileName, bp.Line.ToString () );
				}
			}			
		}
		
		void OnBpJumpTo (object o, EventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			if (tree.Selection.GetSelected (out model, out iter)) {
				string filename = (string) model.GetValue (iter, (int) Columns.FileName);
				string line = (string) model.GetValue (iter, (int) Columns.Line);
				IdeApp.Workbench.OpenDocument (filename, int.Parse(line), 1, true);	
			}
			UpdateDisplay ();
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
			OnBpJumpTo (null, null);
		}
		
		public Gtk.Widget Control {
			get {
				return this;
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
		
		void OnDeleted ()
		{
			TreeIter iter;
			TreeModel model; 
			if (tree.Selection.GetSelected (out model, out iter)) {
				string filename = (string) model.GetValue (iter, (int) Columns.FileName);	
				string line = (string) model.GetValue (iter, (int) Columns.Line);
				foreach (Breakpoint bp in bps.GetBreakpointsAtFileLine(filename, int.Parse(line)))
					bps.Remove(bp);		
			}	
		}
		
		protected void OnDeleteClicked (object o, EventArgs args)
		{
			OnDeleted ();
		}
		
		protected void OnEnableDisableClicked (object o, EventArgs args)
		{
			foreach (Breakpoint bp in bps.GetBreakpoints ()){
				if (enableDisableBtn.Active)
					bp.Enabled = false;
				else
					bp.Enabled = true;
			}
		}
		
	}
}
