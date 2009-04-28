// 
// SearchResultWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using Gtk;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor.Highlighting;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using System.IO;


namespace MonoDevelop.Ide.FindInFiles
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class SearchResultWidget : Gtk.Bin
	{
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		ListStore store;
		ToolButton buttonStop;
		ToggleToolButton buttonPin;
		
		const int SearchResultColumn = 0;
		const int DidReadColumn      = 1;
		
		public string BasePath {
			get;
			set;
		}
		
		public IAsyncOperation AsyncOperation {
			get;
			set;
		}
		
		public bool AllowReuse {
			get { 
				return !buttonStop.Sensitive && !buttonPin.Active; 
			}
		}
		
		public SearchResultWidget ()
		{
			this.Build ();
			
			store = new ListStore (typeof (SearchResult), 
			                       typeof (bool)          // didRead
			                       );
			treeviewSearchResults.Model = store;
			treeviewSearchResults.Selection.Mode = Gtk.SelectionMode.Multiple;
			treeviewSearchResults.HeadersClickable = true;
			treeviewSearchResults.PopupMenu += OnPopupMenu;
			treeviewSearchResults.ButtonPressEvent += HandleButtonPressEvent;
			treeviewSearchResults.RulesHint = true;
			
			TreeViewColumn fileNameColumn = new TreeViewColumn ();
			fileNameColumn.SortIndicator = true;
			fileNameColumn.Resizable = true;
			fileNameColumn.SortColumnId  = 0;
			fileNameColumn.Title = GettextCatalog.GetString ("File");
			CellRendererPixbuf fileNamePixbufRenderer = new CellRendererPixbuf ();
			fileNameColumn.PackStart (fileNamePixbufRenderer, false);
			fileNameColumn.SetCellDataFunc (fileNamePixbufRenderer, new Gtk.TreeCellDataFunc (FileIconDataFunc));
			
			CellRendererText fileNameRenderer = new CellRendererText ();
			fileNameColumn.PackStart (fileNameRenderer, true);
			fileNameColumn.SetCellDataFunc (fileNameRenderer, new Gtk.TreeCellDataFunc (FileNameDataFunc));
			treeviewSearchResults.AppendColumn (fileNameColumn);
			store.SetSortColumnId (0, SortType.Ascending);
			store.SetSortFunc (0, new TreeIterCompareFunc (CompareFileNames));
			
			TreeViewColumn lineColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Line"), new Gtk.CellRendererText (), new Gtk.TreeCellDataFunc (ResultLineDataFunc));
			lineColumn.SortColumnId = 1;
			store.SetSortFunc (1, new TreeIterCompareFunc (CompareLineNumbers));
			
			TreeViewColumn textColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Text"), new Gtk.CellRendererText (), new Gtk.TreeCellDataFunc (ResultTextDataFunc));
			textColumn.SortColumnId = 2;
			textColumn.Resizable = true;
			
			TreeViewColumn pathColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Path"), new Gtk.CellRendererText (), new Gtk.TreeCellDataFunc (ResultPathDataFunc));
			pathColumn.SortColumnId = 3;
			pathColumn.Resizable = true;
			store.SetSortFunc (3, new TreeIterCompareFunc (CompareFilePaths));
			
			treeviewSearchResults.RowActivated += TreeviewSearchResultsRowActivated;
			
			buttonStop = new ToolButton ("gtk-stop");
			buttonStop.Sensitive = false;
			buttonStop.Clicked += ButtonStopClicked;
			
			buttonStop.SetTooltip (tips, GettextCatalog.GetString ("Stop"), "Stop");
			toolbar.Insert (buttonStop, -1);

			ToolButton buttonClear = new ToolButton ("gtk-clear");
			buttonClear.Clicked += ButtonClearClicked;
			buttonClear.SetTooltip (tips, GettextCatalog.GetString ("Clear results"), "Clear results");
			toolbar.Insert (buttonClear, -1);
			
			ToggleToolButton buttonOutput = new ToggleToolButton (MonoDevelop.Core.Gui.Stock.OutputIcon);
			buttonOutput.Clicked += ButtonOutputClicked;
			buttonOutput.SetTooltip (tips, GettextCatalog.GetString ("Show output"), "Show output");
			toolbar.Insert (buttonOutput, -1);
			
			buttonPin = new ToggleToolButton ("md-pin-up");
			buttonPin.Clicked += ButtonPinClicked;
			buttonPin.SetTooltip (tips, GettextCatalog.GetString ("Pin results pad"), GettextCatalog.GetString ("Pin results pad"));
			toolbar.Insert (buttonPin, -1);
			ShowAll ();
			
			scrolledwindowLogView.Hide ();
		}

		void ButtonPinClicked (object sender, EventArgs e)
		{
			buttonPin.StockId = buttonPin.Active ? "md-pin-down" : "md-pin-up";
		}

		void ButtonOutputClicked (object sender, EventArgs e)
		{
			if (((ToggleToolButton)sender).Active) {
				scrolledwindowLogView.Show ();
			} else {
				scrolledwindowLogView.Hide ();
			}
		}

		void ButtonClearClicked (object sender, EventArgs e)
		{
			this.Reset ();
		}

		void ButtonStopClicked (object sender, EventArgs e)
		{
			if (AsyncOperation != null)
				AsyncOperation.Cancel ();
		}

		void TreeviewSearchResultsRowActivated(object o, RowActivatedArgs args)
		{
			OpenSelectedMatches ();
		}
		
		public void BeginProgress ()
		{
			Reset ();
			buttonStop.Sensitive = true;
			treeviewSearchResults.Model = null;
		}
		
		public void EndProgress ()
		{
			buttonStop.Sensitive = false;
			treeviewSearchResults.Model = store;
		}
		
		public void Reset ()
		{
			foreach (var doc in documents.Values) {
				doc.Dispose ();
			}
			ResultCount = 0;
			documents.Clear ();
			store.Clear ();
			labelStatus.Text = "";
			textviewLog.Buffer.Clear ();
		}
		
		protected override void OnDestroyed ()
		{
			Reset ();
			base.OnDestroyed ();
		}

		[GLib.ConnectBefore]
		void HandleButtonPressEvent(object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				OnPopupMenu (this, null);
				args.RetVal = treeviewSearchResults.Selection.GetSelectedRows ().Length > 1;
			}
		}
		
		static double Brightness (Gdk.Color c)
		{
			double r = c.Red / (double)ushort.MaxValue;
			double g = c.Red / (double)ushort.MaxValue;
			double b = c.Red / (double)ushort.MaxValue;
			return Math.Sqrt (r * .241 + g * .691 + b * .068) / 1000.0;
		}
		
		Gdk.Color AdjustColor (Gdk.Color color)
		{
			double b1 = Brightness (color);
			double b2 = Brightness (Style.Base (StateType.Normal));
			double delta = Math.Abs (b1 - b2);
			if (delta < 0.0001) {
				HslColor color1 = color;
				if (color1.L + 0.5 > 1.0) {
					color1.L -= 0.5;
				} else {
					color1.L += 0.5;
				}
				return color1;
			}
			return color;
		}
		
		string AdjustColors (string markup)
		{
			Gdk.Color baseColor = Style.Base (StateType.Normal);
			StringBuilder result = new StringBuilder ();
			int idx = markup.IndexOf ("foreground=\"");
			int offset = 0;
			while (idx > 0) {
				idx += "foreground=\"".Length;
				result.Append (markup.Substring (offset, idx - offset));
				if (idx + 7 >= markup.Length) {
					offset = idx;
					break;
				}
				offset = idx + 7;
				string colorStr = markup.Substring (idx, 7);
				Gdk.Color color = Gdk.Color.Zero;
				if (Gdk.Color.Parse (colorStr, ref color)) {
					colorStr = SyntaxMode.ColorToPangoMarkup (AdjustColor (color));
				}
				result.Append (colorStr);
				idx = markup.IndexOf ("foreground=\"", idx);
			}
			result.Append (markup.Substring (offset, markup.Length - offset));
			return result.ToString ();
		}
		
		void OnPopupMenu (object sender, PopupMenuArgs args)
		{
			CommandEntrySet contextMenu = new CommandEntrySet ();
			contextMenu.AddItem (ViewCommands.Open);
			contextMenu.AddItem (EditCommands.Copy);
			contextMenu.AddItem (EditCommands.SelectAll);
			IdeApp.CommandService.ShowContextMenu (contextMenu, this);
		}
		
		public void ShowStatus (string text)
		{
			labelStatus.Text = text;
		}
		
		void FileIconDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererPixbuf fileNamePixbufRenderer = (CellRendererPixbuf)cell;
			SearchResult searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			fileNamePixbufRenderer.Pixbuf =IdeApp.Services.PlatformService.GetPixbufForFile (searchResult.FileName, Gtk.IconSize.Menu);
		}
		
		Gdk.Color GetColor (Gdk.Color baseColor)
		{
			return baseColor;
		}
		
		string MarkupText (string text, bool didRead, bool isSelected)
		{
			return string.Format ("<span weight=\"{1}\">{0}</span>", GLib.Markup.EscapeText (text), didRead ? "normal" : "bold");
		}
		
		void FileNameDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText fileNameRenderer = (CellRendererText)cell;
			bool didRead = (bool)store.GetValue (iter, DidReadColumn);
			SearchResult searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			
			bool isSelected = treeviewSearchResults.Selection.IterIsSelected (iter);
			
			fileNameRenderer.Markup = MarkupText (System.IO.Path.GetFileName (searchResult.FileName), didRead, isSelected);
		}
		
		int CompareLineNumbers (TreeModel model, TreeIter first, TreeIter second)
		{
			DocumentLocation loc1 = GetLocation ((SearchResult)model.GetValue (first, SearchResultColumn));
			DocumentLocation loc2 = GetLocation ((SearchResult)model.GetValue (second, SearchResultColumn));
			return loc1.Line.CompareTo (loc2.Line);
		}
		
		static int CompareFileNames (TreeModel model, TreeIter first, TreeIter second)
		{
			SearchResult searchResult1 = (SearchResult)model.GetValue (first, SearchResultColumn);
			SearchResult searchResult2 = (SearchResult)model.GetValue (second, SearchResultColumn);
			return System.IO.Path.GetFileName (searchResult1.FileName).CompareTo (System.IO.Path.GetFileName (searchResult2.FileName));
		}
		
		static int CompareFilePaths (TreeModel model, TreeIter first, TreeIter second)
		{
			SearchResult searchResult1 = (SearchResult)model.GetValue (first, SearchResultColumn);
			SearchResult searchResult2 = (SearchResult)model.GetValue (second, SearchResultColumn);
			return System.IO.Path.GetDirectoryName (searchResult1.FileName).CompareTo (System.IO.Path.GetDirectoryName (searchResult2.FileName));
		}
		
		void ResultPathDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText pathRenderer = (CellRendererText)cell;
			SearchResult searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			bool didRead = (bool)store.GetValue (iter, DidReadColumn);
			bool isSelected = treeviewSearchResults.Selection.IterIsSelected (iter);
			pathRenderer.Markup = MarkupText (System.IO.Path.GetDirectoryName (searchResult.FileName), didRead, isSelected);
		}
		
		void ResultLineDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText lineRenderer = (CellRendererText)cell;
			SearchResult searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			Mono.TextEditor.Document doc = GetDocument (searchResult);
			int lineNr = doc.OffsetToLineNumber (searchResult.Offset) + 1;
			bool didRead = (bool)store.GetValue (iter, DidReadColumn);
			bool isSelected = treeviewSearchResults.Selection.IterIsSelected (iter);
			lineRenderer.Markup = MarkupText (lineNr.ToString (), didRead, isSelected);
		}
		
		void ResultTextDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText textRenderer = (CellRendererText)cell;
			SearchResult searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			Mono.TextEditor.Document doc = GetDocument (searchResult);
			int lineNr = doc.OffsetToLineNumber (searchResult.Offset);
			LineSegment line = doc.GetLine (lineNr);
			bool isSelected = treeviewSearchResults.Selection.IterIsSelected (iter);
			Mono.TextEditor.Highlighting.Style style = SyntaxModeService.GetColorStyle (this, PropertyService.Get ("ColorScheme", "Default"));
			
			string markup;
			if (doc.SyntaxMode != null) {
				markup = doc.SyntaxMode.GetMarkup (doc, new TextEditorOptions (), style, line.Offset, line.EditableLength, true, !isSelected, false);
			} else {
				markup = GLib.Markup.EscapeText (doc.GetTextAt (line.Offset, line.EditableLength));
			}
			
			if (!isSelected) {
				int col = searchResult.Offset - line.Offset;
				string tag;
				int pos1 = FindPosition (markup, col, out tag);
				int pos2 = FindPosition (markup, col + searchResult.Length, out tag);
				if (pos1 >= 0 && pos2 >= 0) {
					if (tag.StartsWith ("span")) {
						markup = markup.Insert (pos2, "</span></span><" + tag + ">");
					} else {
						markup = markup.Insert (pos2, "</span>");
					}
					Gdk.Color searchColor = style.SearchTextBg;
					double b1 = Brightness (searchColor);
					double b2 = Brightness (AdjustColor (style.Default.Color));
					double delta = Math.Abs (b1 - b2);
					if (delta < 0.0001) {
						HslColor color1 = style.SearchTextBg;
						if (color1.L + 0.5 > 1.0) {
							color1.L -= 0.5;
						} else {
							color1.L += 0.5;
						}
						searchColor = color1;
					}
					markup = markup.Insert (pos1, "<span background=\"" + SyntaxMode.ColorToPangoMarkup (searchColor) + "\">");
				}
			}
			textRenderer.Markup = AdjustColors (markup.Replace ("\t", new string (' ', TextEditorOptions.DefaultOptions.TabSize)));
		}
		
		static int FindPosition (string markup, int pos, out string tag)
		{
			bool inTag = false;
			bool inChar = false;
			int realPos = 0;
			StringBuilder lastTag = new StringBuilder ();
			for (int i = 0; i < markup.Length; i++) {
				char ch = markup[i];
				if (ch != '<' && ch != '&' && !inTag && !inChar && realPos >= pos) {
					tag = lastTag.ToString ();
					return i;
				}
				switch (ch) {
				case '&':
					inChar = true;
					break;
				case ';':
					inChar = false;
					if (!inTag) 
						realPos++;
					break;
				case '<':
					lastTag.Length = 0;
					inTag = true;
					break;
				case '>':
					inTag = false;
					break;
				default:
					if (!inTag && !inChar) 
						realPos++;
					if (inTag)
						lastTag.Append (ch);
					break;
				}
			}
			tag = lastTag.ToString ();
			if (realPos >= pos) 
				return markup.Length;
			return -1;
		}
		
		Dictionary<string, Mono.TextEditor.Document> documents = new Dictionary<string, Mono.TextEditor.Document> ();
		
		Mono.TextEditor.Document GetDocument (SearchResult result)
		{
			Mono.TextEditor.Document doc;
			if (!documents.TryGetValue (result.FileName, out doc)) {
				doc = new Mono.TextEditor.Document ();
				doc.MimeType = IdeApp.Services.PlatformService.GetMimeTypeForUri (result.FileName);
				TextReader reader = result.FileProvider.Open ();
				doc.Text = reader.ReadToEnd ();
				reader.Close ();
				documents[result.FileName] = doc;
			}
			return doc;
		}
		
		public void WriteText (string text)
		{
			TextIter iter = textviewLog.Buffer.EndIter;
			textviewLog.Buffer.Insert (ref iter, text);
			if (text.EndsWith ("\n"))
				textviewLog.ScrollMarkOnscreen (textviewLog.Buffer.InsertMark);
		}
		
		public int ResultCount {
			get;
			private set;
		}
		
		public void Add (SearchResult result)
		{
			ResultCount++;
			store.AppendValues (result, false);
		}
		
		void OpenDocumentAt (Gtk.TreeIter iter)
		{
			SearchResult result = store.GetValue (iter, SearchResultColumn) as SearchResult;
			if (result != null) {
				DocumentLocation loc = GetLocation (result);
				store.SetValue (iter, DidReadColumn, true);
				IdeApp.Workbench.OpenDocument (result.FileName, loc.Line, loc.Column, true);
			}
		}
		DocumentLocation GetLocation (SearchResult searchResult)
		{
			Mono.TextEditor.Document doc = GetDocument (searchResult);
			int lineNr = doc.OffsetToLineNumber (searchResult.Offset);
			LineSegment line = doc.GetLine (lineNr);
			return new DocumentLocation (lineNr + 1, searchResult.Offset - line.Offset + 1);
		}
		
		public void OpenSelectedMatches ()
		{
			foreach (Gtk.TreePath path in treeviewSearchResults.Selection.GetSelectedRows ()) {
				Gtk.TreeIter iter;
				if (!store.GetIter (out iter, path))
					continue;
				OpenDocumentAt (iter);
			}
		}
		
		public void SelectAll ()
		{
			treeviewSearchResults.Selection.SelectAll ();
		}
		
		public void CopySelection ()
		{
			TreeModel model;
			StringBuilder sb = new StringBuilder ();
			foreach (Gtk.TreePath p in treeviewSearchResults.Selection.GetSelectedRows (out model)) {
				TreeIter iter;
				if (!model.GetIter (out iter, p))
					continue;
				SearchResult result = store.GetValue (iter, SearchResultColumn) as SearchResult;
				if (result == null)
					continue;
				DocumentLocation loc = GetLocation (result);
				Mono.TextEditor.Document doc = GetDocument (result);
				LineSegment line = doc.GetLine (loc.Line - 1);
				
				sb.AppendFormat ("{0} ({1}, {2}):{3}", result.FileName, loc.Line, loc.Column, doc.GetTextAt (line.Offset, line.EditableLength));
				sb.AppendLine ();
			}
			Gtk.Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = sb.ToString ();
			
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = sb.ToString ();
		}
	}
}
