//
// SqlQueryView.cs
//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//   Daniel Morgan <danielmorgan@verizon.net>
//
// Copyright (C) 2005 Christian Hergert
// Copyright (C) 2005 Daniel Morgan
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

using System;
using System.Data;

using Gtk;
using GtkSourceView;

using Mono.Data.Sql;

using MonoDevelop.Gui;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Widgets;

namespace MonoQuery
{
	public class SqlQueryView : AbstractViewContent
	{
		protected Frame control;
		protected SourceView sourceView;
		protected ComboBox providers;
		
		protected ListStore model;
		
		protected EventHandler changedHandler;
		
		protected MonoQueryService service;

		private int executeMode = 0;
		private int offset = 0;
		
		public SqlQueryView () : base ()
		{
			control = new Frame ();
			control.Show ();
			
			VBox vbox = new VBox ();
			vbox.Show ();
			
			Tooltips tips = new Tooltips ();
			
			Toolbar toolbar = new Toolbar ();
			vbox.PackStart 	(toolbar, false, true, 0);
			toolbar.Show ();
			
			Image image = new Image ();
			image.Pixbuf = Gdk.Pixbuf.LoadFromResource ("MonoQuery.Execute");
			image.Show ();
			
			Button execute = new Button (image);
			execute.Clicked += new EventHandler (OnExecute);
			execute.Relief = ReliefStyle.None;
			tips.SetTip (execute, "Execute", "");
			toolbar.Add (execute);
			execute.Show ();
			
			image = new Image ();
			image.Pixbuf = Gdk.Pixbuf.LoadFromResource ("MonoQuery.RunFromCursor");
			image.Show ();
			
			Button run = new Button (image);
			run.Clicked += new EventHandler (OnRunFromCursor);
			run.Relief = ReliefStyle.None;
			tips.SetTip (run, "Run from cursor", "");
			toolbar.Add (run);
			run.Show ();
			
			image = new Image ();
			image.Pixbuf = Gdk.Pixbuf.LoadFromResource ("MonoQuery.Explain");
			image.Show ();
			
			Button explain = new Button (image);
			explain.Clicked += new EventHandler (OnExplain);
			explain.Relief = ReliefStyle.None;
			tips.SetTip (explain, "Explain query", "");
			toolbar.Add (explain);
			explain.Show ();
			
			image = new Image ();
			image.Pixbuf = Gdk.Pixbuf.LoadFromResource ("MonoQuery.Stop");
			image.Show ();
			
			Button stop = new Button (image);
			stop.Clicked += new EventHandler (OnStop);
			stop.Relief = ReliefStyle.None;
			stop.Sensitive = false;
			tips.SetTip (stop, "Stop", "");
			toolbar.Add (stop);
			stop.Show ();

			VSeparator sep = new VSeparator ();
			toolbar.Add (sep);
			sep.Show ();
			
			model = new ListStore (typeof (string), typeof (DbProviderBase));
			
			providers = new ComboBox ();
			providers.Model = model;
			CellRendererText ctext = new CellRendererText ();
			providers.PackStart (ctext, true);
			providers.AddAttribute (ctext, "text", 0);
			toolbar.Add (providers);
			providers.Show ();
			
			SourceLanguagesManager lm = new SourceLanguagesManager ();
			SourceLanguage lang = lm.GetLanguageFromMimeType ("text/x-sql");
			SourceBuffer buf = new SourceBuffer (lang);
			buf.Highlight = true;
			sourceView = new SourceView (buf);
			sourceView.ShowLineNumbers = true;
			sourceView.Show ();
			
			ScrolledWindow scroller = new ScrolledWindow ();
			scroller.Add (sourceView);
			scroller.Show ();
			vbox.PackStart (scroller, true, true, 0);
			
			control.Add (vbox);
			
			service = (MonoQueryService)
				ServiceManager.GetService (typeof (MonoQueryService));
			changedHandler
				 = (EventHandler) Runtime.DispatchService.GuiDispatch (
					new EventHandler (OnProvidersChanged));
			service.Providers.Changed += changedHandler;
			
			foreach (DbProviderBase p in service.Providers) {
				model.AppendValues (p.Name, p);
			}
		}
		
		public string Text {
			get {
				return sourceView.Buffer.Text;
			}
			set {
				sourceView.Buffer.Text = value;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return control;
			}
		}
		
		public override string UntitledName {
			get {
				return "SQL Query";
			}
		}
		
		public override void Dispose ()
		{
			service.Providers.Changed -= changedHandler;
		}
		
		public override void Load (string filename)
		{
		}
		
		public DbProviderBase Connection {
			set {
				int i = 0;
				foreach (object[] row in model) {
					if (row[1] == value)
						providers.Active = i;
					i++;
				}
			}
			get {
				TreeIter iter;
				providers.GetActiveIter (out iter);
				return (DbProviderBase) model.GetValue (iter, 1);
			}
		}

		void OnExecute (object sender, EventArgs args)
		{
			TextBuffer buf = (TextBuffer) sourceView.Buffer;
			TextIter iter = buf.StartIter;
			TextIter end_iter = buf.EndIter;
			string query = String.Empty;

			if (buf.GetSelectionBounds (out iter, out end_iter) == true) {
				query = buf.GetText (iter, end_iter, false);
				executeMode = 2; // as-is
			} else {
				query = GetSqlStatementAtCursor (buf, out iter);
				executeMode = 0; // one single statement at cursor
			}

			if (query.Trim ().Length > 0) {
				Runtime.Gui.StatusBar.BeginProgress (
					GettextCatalog.GetString("Execuing sql query on")
					+ String.Format (" {0}", Connection.Name));
				Runtime.Gui.StatusBar.SetProgressFraction (0.1);

				Runtime.Gui.StatusBar.SetMessage (
					GettextCatalog.GetString ("Query sent, waiting for response."));
				Runtime.Gui.StatusBar.SetProgressFraction (0.5);

				SQLCallback callback = (SQLCallback)
					Runtime.DispatchService.GuiDispatch (
					new SQLCallback (OnExecuteReturn));

				buf.MoveMark (buf.InsertMark, iter);
				buf.MoveMark (buf.SelectionBound, iter);
			
				offset = 0;
				Connection.ExecuteSQL (query, callback);
			}
		}
		
		void OnRunFromCursor (object sender, EventArgs args)
		{
			TextBuffer buf = (TextBuffer) sourceView.Buffer;
			TextIter iter = buf.StartIter;
			TextIter end_iter = buf.EndIter;
			string query = String.Empty;

			if (buf.GetSelectionBounds (out iter, out end_iter) == true) {
				query = buf.GetText (iter, end_iter, false);
				executeMode = 2; // as-is
			} else {
				query = GetSqlStatementAtCursor (buf, out iter);
				executeMode = 1; // one multiple statements one-at-a-time starting at cursor
			}

			if (query.Trim ().Length > 0) {
				Runtime.Gui.StatusBar.BeginProgress (
					GettextCatalog.GetString("Execuing sql query on")
					+ String.Format (" {0}", Connection.Name));
				Runtime.Gui.StatusBar.SetProgressFraction (0.1);

				Runtime.Gui.StatusBar.SetMessage (
					GettextCatalog.GetString ("Query sent, waiting for response."));
				Runtime.Gui.StatusBar.SetProgressFraction (0.5);

				SQLCallback callback = (SQLCallback)
					Runtime.DispatchService.GuiDispatch (
					new SQLCallback (OnExecuteReturn));

				buf.MoveMark (buf.InsertMark, iter);
				buf.MoveMark (buf.SelectionBound, iter);

				executeMode = 1; // Execute multiple statements one-at-a-time starting at cursor
				offset = iter.Offset;
				Connection.ExecuteSQL (query, callback);
			}
		}
		
		void OnExecuteReturn (object sender, object results)
		{
			Runtime.Gui.StatusBar.SetMessage (
				GettextCatalog.GetString ("Query results received"));
			Runtime.Gui.StatusBar.SetProgressFraction (0.9);
			
			TextBuffer buf = (TextBuffer) sourceView.Buffer;
			if (results == null) {
				Runtime.Gui.StatusBar.ShowErrorMessage (
					GettextCatalog.GetString ("Invalid select query"));
				if (executeMode == 1)
					sourceView.ScrollToMark (buf.InsertMark, 0.4, true, 0.0, 1.0);
			} else {
				DataGridView dataView = new DataGridView (results as DataTable);
				Runtime.Gui.Workbench.ShowView (dataView, true);

				if (executeMode == 1) { 
					// execute multiple SQL
					TextIter iter = buf.StartIter;
 					iter.Offset = offset;
					string query = GetNextSqlStatement (buf, ref iter);
					if (query.Trim ().Length > 0) {
						SQLCallback callback = (SQLCallback)
							Runtime.DispatchService.GuiDispatch (
							new SQLCallback (OnExecuteReturn));

						// move insert mark to end of SQL statement to be executed
						buf.MoveMark (buf.InsertMark, iter);
						buf.MoveMark (buf.SelectionBound, iter);

						Runtime.Gui.StatusBar.SetMessage (
							GettextCatalog.GetString ("Query sent, waiting for response."));
						Runtime.Gui.StatusBar.SetProgressFraction (0.5);

						executeMode = 1;
						offset = iter.Offset;
						Connection.ExecuteSQL (query, callback);
					}
					else {
						sourceView.ScrollToMark (buf.InsertMark, 0.4, true, 0.0, 1.0);
						Runtime.Gui.StatusBar.EndProgress ();
					}
				}
				else {
					sourceView.ScrollToMark (buf.InsertMark, 0.4, true, 0.0, 1.0);
					Runtime.Gui.StatusBar.EndProgress ();
				}			
			}
		}

		// Execute first SQL statement at cursor
		public string GetSqlStatementAtCursor (TextBuffer sqlTextBuffer, out TextIter iter) 
		{
			TextIter start_iter, end_iter, insert_iter;
			TextIter match_start1, match_end1, match_start2, match_end2;
			TextIter begin_iter, finish_iter;
			string text = String.Empty;
			int char_count = 0;
			TextMark insert_mark;

			insert_mark = sqlTextBuffer.InsertMark;
			insert_iter = sqlTextBuffer.GetIterAtMark (insert_mark);
			start_iter = sqlTextBuffer.GetIterAtOffset (0);
			
			char_count = sqlTextBuffer.CharCount;
			end_iter = sqlTextBuffer.GetIterAtOffset (char_count);
			iter = end_iter;

			match_start1 = sqlTextBuffer.GetIterAtOffset (0);
			match_end1 = sqlTextBuffer.GetIterAtOffset (char_count);
			match_start2 = sqlTextBuffer.GetIterAtOffset (0);
			match_end2 = sqlTextBuffer.GetIterAtOffset (char_count);

			begin_iter = sqlTextBuffer.GetIterAtOffset (0);
			finish_iter = sqlTextBuffer.GetIterAtOffset (char_count);

			if (start_iter.IsEnd == false) 
			{
				if (insert_iter.BackwardSearch (";", TextSearchFlags.TextOnly, 
						out match_start1, out match_end1, start_iter) == true) {
					begin_iter = match_start1;
					begin_iter.ForwardChars (1);
				}
				
				if (insert_iter.ForwardSearch (";",	TextSearchFlags.TextOnly,
						out match_start2, out match_end2, end_iter) == true) {
					finish_iter = match_end2;
					finish_iter.BackwardChars (1);
				}
				iter = finish_iter;
				text = sqlTextBuffer.GetText (begin_iter, finish_iter, false);	

				// FIXME: for this to work.  GetSqlStatement has to rewritten to be line-based
				if (text.Length > 0) {
					// search does not work if what you are searching for is 
					// at the end of the buffer,
					// this compensates for this
					int j = text.Length;
					int cont = 1;
					for(int i = text.Length - 1; cont == 1 && i >= 0; i--) {
						char ch = text[i];
						switch(ch) {
						case ' ':
						case ';':
							j--;
							break;
						default:
							cont = 0;
							break;
						}
					}
					
					if (j != text.Length) {
						string t = text.Substring(0, j);
						text = t;
					}
				}
			}

			return text;
		}

		// get next SQL statement.  Requires GetSqlStatementAtCursor having been called first
		public string GetNextSqlStatement (TextBuffer sqlTextBuffer, ref TextIter iter) 
		{
			TextIter start_iter, end_iter;
			TextIter match_start2, match_end2;
			TextIter finish_iter;
			string text = String.Empty;
			int char_count = 0;

			char_count = sqlTextBuffer.CharCount;
			end_iter = sqlTextBuffer.GetIterAtOffset (char_count);
			if (iter.IsEnd == false) {
				iter.ForwardChars (1);
				if (sqlTextBuffer.GetText (iter, end_iter, false).Equals (";"))
					iter.ForwardChars (1);
			}

			if (iter.IsEnd == true) 
				return "";

			start_iter = iter;
			match_start2 = iter;
			match_end2 = sqlTextBuffer.GetIterAtOffset (char_count);
			finish_iter = sqlTextBuffer.GetIterAtOffset (char_count);

			if (start_iter.IsEnd == false) {
				if (iter.ForwardSearch (";", TextSearchFlags.TextOnly,
						out match_start2, out match_end2, end_iter) == true) 	{
					finish_iter = match_end2;
					finish_iter.BackwardChars (1);
				}

				text = sqlTextBuffer.GetText (iter, finish_iter, false);
				iter = finish_iter;

				if(text.Length > 0) {
					// search does not work if what you are searching for is 
					// at the end of the buffer,
					// this compensates for this
					int j = text.Length;
					int cont = 1;
					for(int i = text.Length - 1; cont == 1 && i >= 0; i--) {
						char ch = text[i];
						switch(ch) {
						case ' ':
						case ';':
							j--;
							break;
						default:
							cont = 0;
							break;
						}
					}
					
					if(j != text.Length) {
						string t = text.Substring(0, j);
						text = t;
					}
				}
			}

			return text;
		}
		
		void OnExplain (object sender, EventArgs args)
		{
			Runtime.Gui.StatusBar.BeginProgress (
				GettextCatalog.GetString("Execuing sql query on")
				+ String.Format (" {0}", Connection.Name));
			Runtime.Gui.StatusBar.SetProgressFraction (0.1);
			
			string query = sourceView.Buffer.Text;
			SQLCallback callback = (SQLCallback)
				Runtime.DispatchService.GuiDispatch (
				new SQLCallback (OnExecuteReturn));
			
			Runtime.Gui.StatusBar.SetMessage (
				GettextCatalog.GetString ("Query sent, waiting for response."));
			Runtime.Gui.StatusBar.SetProgressFraction (0.5);
			
			Connection.ExplainSQL (query, callback);
		}
		
		void OnStop (object sender, EventArgs args)
		{
		}
		
		void OnProvidersChanged (object sender, EventArgs args)
		{
			DbProviderBase current = Connection;
			model.Clear ();
			
			foreach (DbProviderBase p in service.Providers) {
				TreeIter cur = model.AppendValues (p.Name, p);
				if (p == current)
					providers.SetActiveIter (cur);
			}
		}
	}
}