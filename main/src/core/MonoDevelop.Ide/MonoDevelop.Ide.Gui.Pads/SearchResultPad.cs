//  SearchResultPad.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Commands;

using Gtk;

namespace MonoDevelop.Ide.Gui.Pads
{
	public class SearchResultPad : IPadContent, ILocationListPad
	{
		ScrolledWindow sw;
		Gtk.TreeView view;
		ListStore store;
		string basePath;
		IAsyncOperation asyncOperation;
		string originalTitle;
		string id;
		int matchCount;
		string statusText;
		int instanceNum;
		bool customStatusSet;
		Dictionary<string,Result> results = new Dictionary<string, Result> ();

		Clipboard clipboard;
		
		const int COL_TYPE = 0, COL_LINE = 1, COL_COLUMN = 2, COL_DESC = 3, COL_FILE = 4, COL_PATH = 5, COL_FULLPATH = 6, COL_READ = 7, COL_READ_WEIGHT = 8, COL_ISFILE = 9;

		Gtk.TextBuffer logBuffer;
		Gtk.TextView logTextView;
		Gtk.ScrolledWindow logScroller;
		ToggleToolButton buttonOutput;
		ToolButton buttonStop;
		ToggleToolButton buttonPin;
		Label status;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		StringBuilder log = new StringBuilder ();
		Widget control;
		IPadWindow window;

		class Result
		{
			public Gtk.TreeIter Iter;
			public string Text;
			public List<int> Positions;

			public void AddPosition (int pos, int len)
			{
				for (int n=0; n < Positions.Count; n+=2) {
					if (Positions [n] == pos) {
						throw new ArgumentException ("There is already a match at this location", "pos");
					}
					if (Positions [n] > pos) {
						Positions.Insert (n, pos);
						Positions.Insert (n + 1, len);
						return;
					}
				}
				Positions.Add (pos);
				Positions.Add (len);
			}
		}
		
		public SearchResultPad (int instanceNum)
		{
			this.instanceNum = instanceNum;
			
			// Toolbar
			
			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.Menu;
			toolbar.Orientation = Orientation.Vertical;
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			toolbar.ShowArrow = true;

			buttonStop = new ToolButton ("gtk-stop");
			buttonStop.Clicked += new EventHandler (OnButtonStopClick);
			buttonStop.SetTooltip (tips, GettextCatalog.GetString ("Stop"), "Stop");
			toolbar.Insert (buttonStop, -1);

			ToolButton buttonClear = new ToolButton ("gtk-clear");
			buttonClear.Clicked += new EventHandler (OnButtonClearClick);
			buttonClear.SetTooltip (tips, GettextCatalog.GetString ("Clear results"), "Clear results");
			toolbar.Insert (buttonClear, -1);
			
			buttonOutput = new ToggleToolButton (MonoDevelop.Core.Gui.Stock.OutputIcon);
			buttonOutput.Clicked += new EventHandler (OnButtonOutputClick);
			buttonOutput.SetTooltip (tips, GettextCatalog.GetString ("Show output"), "Show output");
			toolbar.Insert (buttonOutput, -1);

			buttonPin = new ToggleToolButton ("md-pin-up");
			buttonPin.Clicked += new EventHandler (OnButtonPinClick);
			buttonPin.SetTooltip (tips, GettextCatalog.GetString ("Pin results pad"), GettextCatalog.GetString ("Pin results pad"));
			toolbar.Insert (buttonPin, -1);
			
			// Results list
			
			store = new Gtk.ListStore (
				typeof (Gdk.Pixbuf), // image
				typeof (int),        // line
				typeof (int),        // column
				typeof (string),     // desc
				typeof (string),     // file
				typeof (string),     // path
				typeof (string),     // full path
				typeof (bool),       // read?
				typeof (int),       // read? -- use Pango weight
				typeof (bool));       // is file

			view = new Gtk.TreeView (store);
			view.RulesHint = true;
			view.PopupMenu += new PopupMenuHandler (OnPopupMenu);
			view.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPressed);
			view.HeadersClickable = true;
			view.Selection.Mode = SelectionMode.Multiple;
			AddColumns ();
			
			sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.In;
			sw.Add (view);
			
			// Log view
			
			logBuffer = new Gtk.TextBuffer (new Gtk.TextTagTable ());
			logTextView = new Gtk.TextView (logBuffer);
			logTextView.Editable = false;
			logScroller = new Gtk.ScrolledWindow ();
			logScroller.ShadowType = ShadowType.In;
			logScroller.Add (logTextView);

			// HPaned
			
			Gtk.HPaned paned = new Gtk.HPaned ();
			paned.Pack1 (sw, true, true);
			paned.Pack2 (logScroller, true, true);
			
			// HBox
			
			status = new Label ();
			status.Xalign = 0.0f;
			
			VBox vbox = new VBox ();
			vbox.PackStart (paned, true, true, 0);
			vbox.PackStart (status, false, false, 3);
			
			HBox hbox = new HBox ();
			hbox.PackStart (vbox, true, true, 0);
			hbox.PackStart (toolbar, false, false, 0);
			
			control = hbox;
			
			Control.ShowAll ();
			
			logScroller.Hide ();
			
			view.RowActivated += new RowActivatedHandler (OnRowActivated);
		}
		
		public void Initialize (IPadWindow window)
		{
			this.window = window;
			window.Icon = MonoDevelop.Core.Gui.Stock.FindIcon;
		}
		
		public void BeginProgress (string title)
		{
			originalTitle = window.Title;
			window.Title = "<span foreground=\"blue\">" + originalTitle + "</span>";
			
			matchCount = 0;
			store.Clear ();
			results.Clear ();
			logBuffer.Clear ();
			if (!logScroller.Visible)
				log = new StringBuilder ();
				
			buttonStop.Sensitive = true;
			status.Text = string.Empty;
			statusText = GettextCatalog.GetString ("Searching...");
		}
		
		public void EndProgress ()
		{
			window.Title = originalTitle;
			buttonStop.Sensitive = false;
			if (customStatusSet)
				status.Text = " " + statusText;
			else {
				statusText = GettextCatalog.GetString("Search completed");
				status.Text = " " + statusText + " - " + string.Format(GettextCatalog.GetPluralString("{0} match.", "{0} matches.", matchCount), matchCount);
			}
		}
		
		public bool AllowReuse {
			get { return !buttonStop.Sensitive && !buttonPin.Active; }
		}
		
		public IAsyncOperation AsyncOperation {
			get {
				return asyncOperation;
			}
			set {
				asyncOperation = value;
			}
		}
		
		public void SetBasePath (string path)
		{
			basePath = path;
		}
		
		public Gtk.Widget Control {
			get {
				return control;
			}
		}

		public string DefaultPlacement {
			get { return "Bottom"; }
		}
		
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public void RedrawContent()
		{
		}

		void OnButtonClearClick (object sender, EventArgs e)
		{
			matchCount = 0;
			store.Clear ();
			logBuffer.Clear ();
			status.Text = string.Empty;
			if (log != null) log = new StringBuilder ();
		}

		void OnButtonStopClick (object sender, EventArgs e)
		{
			asyncOperation.Cancel ();
		}

		void OnButtonOutputClick (object sender, EventArgs e)
		{
			if (buttonOutput.Active) {
				if (log != null) {
					logBuffer.Text = log.ToString ();
					log = null;
				}
				logScroller.Show ();
			} else {
				logScroller.Hide ();
			}
		}
		
		void OnButtonPinClick (object sender, EventArgs e)
		{
			if (buttonPin.Active)
				buttonPin.StockId = "md-pin-down";
			else
				buttonPin.StockId = "md-pin-up";
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
			opset.AddItem (ViewCommands.Open);
			opset.AddItem (EditCommands.Copy);
			opset.AddItem (EditCommands.SelectAll);
			IdeApp.CommandService.ShowContextMenu (opset, this);
		}

		[CommandHandler (ViewCommands.Open)]
		internal void OnOpen ()
		{
			TreeModel model;
			foreach (Gtk.TreePath p in view.Selection.GetSelectedRows (out model))
				OpenFile (p);
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
				string file = (string) model.GetValue (it, COL_FILE);
				string path = (string) model.GetValue (it, COL_PATH);
				int line = (int) model.GetValue (it, COL_LINE);
				string text = (string) model.GetValue (it, COL_DESC);

				if (txt.Length > 0)
					txt.Append ("\n");
				txt.AppendFormat ("{0} ({1}):{2}", Path.Combine (path, file), line, text);
			}
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = txt.ToString ();
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = txt.ToString ();
		}

		[CommandHandler (EditCommands.SelectAll)]
		internal void OnSelectAll ()
		{
			view.Selection.SelectAll ();
		}

		public int InstanceNum {
			get {
				return instanceNum;
			}
		}

		public void WriteText (string text)
		{
			if (log != null)
				log.Append (text);
			else {
				TextIter it = logBuffer.EndIter;
				logBuffer.Insert (ref it, text);

				if (text.EndsWith ("\n"))
					logTextView.ScrollMarkOnscreen (logBuffer.InsertMark);
			}
		}

		void AddColumns ()
		{
			Gtk.CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf ();
			
			Gtk.CellRendererText line = new Gtk.CellRendererText (), desc = new Gtk.CellRendererText () , path = new Gtk.CellRendererText (),
			file = new Gtk.CellRendererText ();
			
			TreeViewColumn col;
			col = view.AppendColumn ("", iconRender, "pixbuf", COL_TYPE);
			col = view.AppendColumn (GettextCatalog.GetString ("Line"), line, "text", COL_LINE, "weight", COL_READ_WEIGHT, "visible", COL_ISFILE);
			col.SortColumnId = COL_LINE;
			col = view.AppendColumn (GettextCatalog.GetString ("File"), file, "text", COL_FILE, "weight", COL_READ_WEIGHT, "visible", COL_ISFILE);
			col.SortColumnId = COL_FILE;
			col.Resizable = true;
			col = view.AppendColumn (GettextCatalog.GetString ("Text"), desc, "markup", COL_DESC, "weight", COL_READ_WEIGHT);
			col.SortColumnId = COL_DESC;
			col.Resizable = true;
			col = view.AppendColumn (GettextCatalog.GetString ("Path"), path, "text", COL_PATH, "weight", COL_READ_WEIGHT, "visible", COL_ISFILE);
			col.SortColumnId = COL_FULLPATH;
			col.Resizable = true;

			store.SetSortFunc (COL_FULLPATH, PathFunc);
			store.SetSortFunc (COL_FILE, FileFunc);
		}
		
		public void Dispose ()
		{
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			OpenFile (args.Path);
		}

		void OpenFile (Gtk.TreePath tree_path)
		{
			Gtk.TreeIter iter;
			if (!store.GetIter (out iter, tree_path))
				return;

			if (!(bool) store.GetValue (iter, COL_ISFILE))
				return;
			store.SetValue (iter, COL_READ, true);
			store.SetValue (iter, COL_READ_WEIGHT, (int) Pango.Weight.Normal);

			string path = (string) store.GetValue (iter, COL_FULLPATH);
			int line = (int) store.GetValue (iter, COL_LINE);
			/*int line = (int)*/ store.GetValue (iter, COL_COLUMN);

			IdeApp.Workbench.OpenDocument (path, line, 1, true);
		}
		
		public void ReportStatus (string resultMessage)
		{
			statusText = resultMessage;
			customStatusSet = true;
		}

		public void AddResult (string file, int line, int column, string text, int matchLength)
		{
			matchCount++;
			string mkey = file + " " + line;
			
			Result res;
			if (results.TryGetValue (mkey, out res)) {
				if (matchLength <= 0)
					return;
				res.AddPosition (column-1, matchLength);
				string tline = RenderResult (res);
				store.SetValue (res.Iter, COL_DESC, tline);
				return;
			}
			
			Gdk.Pixbuf stock;
			stock = sw.RenderIcon (Services.Icons.GetImageForFile (file), Gtk.IconSize.Menu, "");

			string tmpPath = file;
			if (basePath != null)
				tmpPath = FileService.AbsoluteToRelativePath (basePath, file);
			
			string fileName = tmpPath;
			string path = tmpPath;
			
			fileName = Path.GetFileName (file);
			
			try {
				path = Path.GetDirectoryName (tmpPath);
			} catch (IOException) {}

			res = new Result ();
			res.Text = text;
			res.Positions = new List<int> ();
			if (matchLength > 0)
				res.AddPosition (column-1, matchLength);
			
			text = RenderResult (res);
			Gtk.TreeIter it = store.AppendValues (stock, line, column, text, fileName, path, file, false, (int) Pango.Weight.Bold, file != null);
			res.Iter = it;
			results [mkey] = res;
			
			status.Text = " " + statusText + " - " + string.Format(GettextCatalog.GetPluralString("{0} match.", "{0} matches.", matchCount), matchCount);
			customStatusSet = false;
		}

		string RenderResult (Result res)
		{
			if (res.Positions.Count == 0)
				return GLib.Markup.EscapeText (res.Text).Trim ();
			
			StringBuilder sb = new StringBuilder (res.Text.Length);
			int pos = 0, len = 0;
			try {
				for (int n = 0; n < res.Positions.Count; n += 2) {
					int prevPosEnd = pos + len;
					pos = res.Positions [n];
					len = res.Positions [n + 1];
					sb.Append (GLib.Markup.EscapeText (res.Text.Substring (prevPosEnd, pos - prevPosEnd)));
					sb.Append ("<span background='yellow'>");
					sb.Append (GLib.Markup.EscapeText (res.Text.Substring (pos, len)));
					sb.Append ("</span>");
				}
				
				if (res.Text.Length - pos - len > 0)
					sb.Append (GLib.Markup.EscapeText (res.Text.Substring (pos + len, (res.Text.Length - pos - len))));
			}
			//old implementation seemed to think that pos+len might be out of bounds,
			//but I think catching & logging an exception's more appropriate than a range check
			catch (Exception ex)
			{
				LoggingService.LogWarning ("Error escaping search result '{0}' @{1}x{2};\n{3}", res.Text, pos, len, ex);
				return GLib.Markup.EscapeText (res.Text).Trim ();
			}
			
			return sb.ToString ().Trim ();
		}

		public virtual bool GetNextLocation (out string file, out int line, out int column)
		{
			bool hasNext;
			TreeIter iter;
			
			TreePath[] rows = view.Selection.GetSelectedRows ();
			if (rows.Length > 0 && store.GetIter (out iter, rows[0]))
				hasNext = store.IterNext (ref iter);
			else
				hasNext = store.GetIterFirst (out iter);
			
			if (!hasNext) {
				file = null;
				line = 0;
				column = 0;
				view.Selection.UnselectAll ();
				return false;
			} else {
				view.Selection.UnselectAll ();
				view.Selection.SelectIter (iter);
				file = (string) store.GetValue (iter, COL_FULLPATH);
				if (file == null)
					return GetNextLocation (out file, out line, out column);
				line = (int) store.GetValue (iter, COL_LINE);
				column = 1;
				view.ScrollToCell (store.GetPath (iter), view.Columns[0], false, 0, 0);
				store.SetValue (iter, COL_READ, true);
				store.SetValue (iter, COL_READ_WEIGHT, (int) Pango.Weight.Normal);
				return true;
			}
		}

		public virtual bool GetPreviousLocation (out string file, out int line, out int column)
		{
			bool hasNext, hasSel;
			TreeIter iter;
			TreeIter selIter = TreeIter.Zero;
			TreeIter prevIter = TreeIter.Zero;
			
			TreePath[] rows = view.Selection.GetSelectedRows ();
			hasSel = rows.Length > 0 && store.GetIter (out selIter, rows[0]);
			hasNext = store.GetIterFirst (out iter);
			
			while (hasNext) {
				if (hasSel && iter.Equals (selIter))
					break;
				prevIter = iter;
				hasNext = store.IterNext (ref iter);
			}
			
			if (prevIter.Equals (TreeIter.Zero)) {
				file = null;
				line = 0;
				column = 0;
				view.Selection.UnselectAll ();
				return false;
			} else {
				view.Selection.UnselectAll ();
				view.Selection.SelectIter (prevIter);
				file = (string) store.GetValue (prevIter, COL_FULLPATH);
				if (file == null)
					return GetPreviousLocation (out file, out line, out column);
				line = (int) store.GetValue (prevIter, COL_LINE);
				column = 1;
				view.ScrollToCell (store.GetPath (prevIter), view.Columns[0], false, 0, 0);
				store.SetValue (prevIter, COL_READ, true);
				store.SetValue (prevIter, COL_READ_WEIGHT, (int) Pango.Weight.Normal);
				return true;
			}
		}
		
		int PathFunc (TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			string s1 = (string) model.GetValue (iter1, COL_FULLPATH);
			string s2 = (string) model.GetValue (iter2, COL_FULLPATH);
			s1 += ((int) model.GetValue (iter1, COL_LINE)).ToString ("000000");
			s2 += ((int) model.GetValue (iter2, COL_LINE)).ToString ("000000");
			return s1.CompareTo (s2);
		}
		
		int FileFunc (TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			string s1 = (string) model.GetValue (iter1, COL_FILE);
			string s2 = (string) model.GetValue (iter2, COL_FILE);
			s1 += ((int) model.GetValue (iter1, COL_LINE)).ToString ("000000");
			s2 += ((int) model.GetValue (iter2, COL_LINE)).ToString ("000000");
			return s1.CompareTo (s2);
		}
	}
}
