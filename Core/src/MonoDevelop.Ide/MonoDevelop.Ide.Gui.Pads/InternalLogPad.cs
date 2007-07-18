//
// InternalLogPad.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Drawing;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

using Gtk;

namespace MonoDevelop.Ide.Gui.Pads
{
	internal class InternalLogPad : IPadContent
	{
		VBox control;
		ScrolledWindow sw;
		Gtk.TreeView view;
		ListStore store;
		TreeModelFilter filter;
		ToggleToolButton errorBtn, warnBtn, msgBtn, debugBtn;
		Gtk.Tooltips tips = new Gtk.Tooltips ();

		Clipboard clipboard;

		Gdk.Pixbuf iconWarning;
		Gdk.Pixbuf iconError;
		Gdk.Pixbuf iconInfo;
		Gdk.Pixbuf iconDebug;
		
		const string showErrorsPropertyName = "MonoDevelop.LogList.ShowErrors";
		const string showWarningsPropertyName = "MonoDevelop.LogList.ShowWarnings";
		const string showMessagesPropertyName = "MonoDevelop.LogList.ShowMessages";
		const string showDebugPropertyName = "MonoDevelop.LogList.ShowDebug";

		enum Columns
		{
			Type,
			Description,
			Time,
			TypeString,
			Message
		}

		void IPadContent.Initialize (IPadWindow window)
		{
			window.Title = GettextCatalog.GetString ("Internal Message Log");
			window.Icon = MonoDevelop.Core.Gui.Stock.OutputIcon;
		}
		
		public Gtk.Widget Control {
			get {
				return control;
			}
		}

		public string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.InternalLogPad"; }
		}
		
		public void RedrawContent()
		{
			// FIXME
		}

		public InternalLogPad ()
		{
			control = new VBox ();

			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.Menu;
			control.PackStart (toolbar, false, false, 0);
			
			errorBtn = new ToggleToolButton ();
			UpdateErrorsNum();
			errorBtn.Active = (bool)Runtime.Properties.GetProperty (showErrorsPropertyName, true);
			errorBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogError, Gtk.IconSize.Button);
			errorBtn.IsImportant = true;
			errorBtn.Toggled += new EventHandler (FilterChanged);
			errorBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Errors"), GettextCatalog.GetString ("Show Errors"));
			toolbar.Insert (errorBtn, -1);
			
			warnBtn = new ToggleToolButton ();
			UpdateWarningsNum();
			warnBtn.Active = (bool)Runtime.Properties.GetProperty (showWarningsPropertyName, true);
			warnBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogWarning, Gtk.IconSize.Button);
			warnBtn.IsImportant = true;
			warnBtn.Toggled += new EventHandler (FilterChanged);
			warnBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Warnings"), GettextCatalog.GetString ("Show Warnings"));
			toolbar.Insert (warnBtn, -1);
			
			msgBtn = new ToggleToolButton ();
			UpdateMessagesNum();
			msgBtn.Active = (bool)Runtime.Properties.GetProperty (showMessagesPropertyName, true);
			msgBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogInfo, Gtk.IconSize.Button);
			msgBtn.IsImportant = true;
			msgBtn.Toggled += new EventHandler (FilterChanged);
			msgBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Messages"), GettextCatalog.GetString ("Show Messages"));
			toolbar.Insert (msgBtn, -1);
			
			debugBtn = new ToggleToolButton ();
			UpdateDebugNum();
			debugBtn.Active = (bool)Runtime.Properties.GetProperty (showDebugPropertyName, true);
			debugBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogQuestion, Gtk.IconSize.Button);
			debugBtn.IsImportant = true;
			debugBtn.Toggled += new EventHandler (FilterChanged);
			debugBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Debug"), GettextCatalog.GetString ("Show Debug"));
			toolbar.Insert (debugBtn, -1);
			
			toolbar.Insert (new SeparatorToolItem (), -1);
			
			Gtk.ToolButton clearBtn = new Gtk.ToolButton (Gtk.Stock.Clear);
			clearBtn.IsImportant = true;
			clearBtn.Clicked += new EventHandler (OnClearList);
			toolbar.Insert (clearBtn, -1);
			
			store = new Gtk.ListStore (typeof (Gdk.Pixbuf),      // image - type
			                           typeof (string),          // desc
			                           typeof (string),          // time
			                           typeof (string),          // type string
			                           typeof(LogAppendedArgs)); // message

			TreeModelFilterVisibleFunc filterFunct = new TreeModelFilterVisibleFunc (FilterTaskTypes);
			filter = new TreeModelFilter (store, null);
            filter.VisibleFunc = filterFunct;
			
			view = new Gtk.TreeView (new Gtk.TreeModelSort (filter));
			view.RulesHint = true;
			view.PopupMenu += new PopupMenuHandler (OnPopupMenu);
			view.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPressed);
			view.HeadersClickable = true;
			view.Selection.Mode = SelectionMode.Multiple;
			
			AddColumns ();
			
			sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.In;
			sw.Add (view);
			
			Runtime.LoggingService.LogAppended += (LogAppendedHandler) Services.DispatchService.GuiDispatch (new LogAppendedHandler (MessageAdded));
						
			iconWarning = sw.RenderIcon (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu, "");
			iconError = sw.RenderIcon (Gtk.Stock.DialogError, Gtk.IconSize.Menu, "");
			iconInfo = sw.RenderIcon (Gtk.Stock.DialogInfo, Gtk.IconSize.Menu, "");
			iconDebug = sw.RenderIcon (Gtk.Stock.DialogQuestion, Gtk.IconSize.Menu, "");
			
			control.Add (sw);
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.ShowArrow = false;
			Control.ShowAll ();
			
			lock (InternalLog.Messages) {
				// Load existing messages
				foreach (LogAppendedArgs msg in InternalLog.Messages) {
					AddMessage (msg);
				}
			}
			store.SetSortFunc ((int)Columns.Time, TimeSortFunc);
			((TreeModelSort)view.Model).SetSortColumnId ((int)Columns.Time, SortType.Descending);
		}

		void AddColumns ()
		{
			Gtk.CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf ();
			Gtk.CellRendererText desc = new Gtk.CellRendererText ();
			Gtk.CellRendererText time = new Gtk.CellRendererText ();
			iconRender.Yalign = 0;
			iconRender.Ypad = 2;
			time.Yalign = 0;
			desc.Yalign = 0;

			TreeViewColumn col;
			col = view.AppendColumn ("", iconRender, "pixbuf", Columns.Type);
			col.SortColumnId = (int) Columns.TypeString;
			col = view.AppendColumn (GettextCatalog.GetString ("Time"), time, "text", Columns.Time);
			col.SortColumnId = (int) Columns.Time;
			col = view.AppendColumn (GettextCatalog.GetString ("Description"), desc, "text", Columns.Description);
			col.SortColumnId = (int) Columns.Description;
		}

		[GLib.ConnectBefore]
		void OnButtonPressed (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				OnPopupMenu (null, null);
				args.RetVal = view.Selection.GetSelectedRows ().Length > 1;
			}
		}

		void OnPopupMenu (object o, PopupMenuArgs args)
		{
			CommandEntrySet opset = new CommandEntrySet ();
			opset.AddItem (EditCommands.Copy);
			opset.AddItem (EditCommands.SelectAll);
			IdeApp.CommandService.ShowContextMenu (opset, this);
		}

		[CommandHandler (EditCommands.SelectAll)]
		internal void OnSelectAll ()
		{
			view.Selection.SelectAll ();
		}
		
		[CommandHandler (EditCommands.Copy)]
		internal void OnCopy ()
		{
			TreeModel model;
			StringBuilder txt = new StringBuilder ();
			foreach (Gtk.TreePath p in view.Selection.GetSelectedRows (out model)) {
				TreeIter it;
				if (!model.GetIter (out it, p))
					continue;
				LogAppendedArgs msg = (LogAppendedArgs) model.GetValue (it, (int) Columns.Message);
				if (txt.Length > 0)
					txt.Append ('\n');
				txt.AppendFormat ("{0} - {1} - {2}", msg.Level, msg.Timestamp.ToLongTimeString (), msg.Message);
			}
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = txt.ToString ();
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = txt.ToString ();
		}

		public void Dispose ()
		{
		}
		
		void FilterChanged (object sender, EventArgs e)
		{
			Runtime.Properties.SetProperty (showErrorsPropertyName, errorBtn.Active);
			Runtime.Properties.SetProperty (showWarningsPropertyName, warnBtn.Active);
			Runtime.Properties.SetProperty (showMessagesPropertyName, msgBtn.Active);
			Runtime.Properties.SetProperty (showDebugPropertyName, debugBtn.Active);
			
			filter.Refilter ();
		}

		bool FilterTaskTypes (TreeModel model, TreeIter iter)
		{
			try {
				LogAppendedArgs msg = (LogAppendedArgs) store.GetValue (iter, (int)Columns.Message);
				if (msg == null)
					return true;
				if ((msg.Level == InternalLog.Error || msg.Level == InternalLog.Fatal) && errorBtn.Active)
					return true;
				else if (msg.Level == InternalLog.Warning && warnBtn.Active)
					return true;
				else if (msg.Level == InternalLog.Info && msgBtn.Active)
					return true;
				else if (msg.Level == InternalLog.Debug && debugBtn.Active)
					return true;
			} catch {
				//Not yet fully added
			}
			return false;
		}

		public void OnClearList (object sender, EventArgs e)
		{
			InternalLog.Reset ();
			store.Clear ();
			UpdateErrorsNum ();
			UpdateWarningsNum ();
			UpdateMessagesNum ();
			UpdateDebugNum ();
		}

		void MessageAdded (object sender, LogAppendedArgs args)
		{
			if (args.Level == InternalLog.Debug)
				return;
			AddMessage (args);
		}
		
		public void AddMessage (LogAppendedArgs args)
		{
			Gdk.Pixbuf stock;
			
			switch (args.Level) {
				case InternalLog.Fatal:
				case InternalLog.Error:
					stock = iconError;
					UpdateErrorsNum ();
					break; 
				case InternalLog.Warning:
					stock = iconWarning;
					UpdateWarningsNum ();	
					break;
				case InternalLog.Info:
					stock = iconInfo;
					UpdateWarningsNum ();	
					break;
				case InternalLog.Debug:
					stock = iconDebug;
					UpdateDebugNum ();
					break;
				default:
					stock = iconDebug;
					break;
			}

			store.AppendValues (stock,
			                    args.Message,
			                    args.Timestamp.ToLongTimeString (),
			                    args.Level,
			                    args);
			filter.Refilter ();
		}

		void UpdateErrorsNum () 
		{
			errorBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Error", "{0} Errors", InternalLog.ErrorCount), InternalLog.ErrorCount);
		}

		void UpdateWarningsNum ()
		{
			warnBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Warning", "{0} Warnings", InternalLog.WarningCount), InternalLog.WarningCount); 
		}

		void UpdateMessagesNum ()
		{
			msgBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Message", "{0} Messages", InternalLog.InfoCount), InternalLog.InfoCount);
		}

		void UpdateDebugNum ()
		{
			debugBtn.Label = " " + string.Format(GettextCatalog.GetString("{0} Debug", InternalLog.DebugCount));
		}

		private int TimeSortFunc (TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			LogAppendedArgs m1 = (LogAppendedArgs) model.GetValue (iter1, (int)Columns.Message);
			LogAppendedArgs m2 = (LogAppendedArgs) model.GetValue (iter2, (int)Columns.Message);

			if (m1 == null || m2 == null)
				return 0;
			
			SortType order;
			int sid;
			store.GetSortColumnId (out sid, out order);
			
			Console.WriteLine ("pp1: ");
			
			if (order == SortType.Ascending)
				return DateTime.Compare (m1.Timestamp, m2.Timestamp);
			else
				return DateTime.Compare (m2.Timestamp, m1.Timestamp);
		}
	}
}
