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
using System.Linq;

using Gdk;

using Gtk;
using Mono.TextEditor;

using Mono.TextEditor.Highlighting;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using System.IO;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Components;


namespace MonoDevelop.Ide.FindInFiles
{
	class SearchResultWidget : HBox, ILocationList
	{
		ListStore store;

		readonly ToolButton buttonStop;

		readonly ToggleToolButton buttonPin;
		
		const int SearchResultColumn = 0;
		const int DidReadColumn      = 1;
		
		Mono.TextEditor.Highlighting.ColorScheme highlightStyle;
		
		ScrolledWindow scrolledwindowLogView; 
		PadTreeView treeviewSearchResults;
		Label labelStatus;
		TextView textviewLog;
		
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
			var vbox = new VBox ();
			var toolbar = new Toolbar () {
				Orientation = Orientation.Vertical,
				IconSize = IconSize.Menu,
				ToolbarStyle = ToolbarStyle.Icons,
			};
			this.PackStart (vbox, true, true, 0);
			this.PackStart (toolbar, false, false, 0);
			labelStatus = new Label () {
				Xalign = 0,
				Justify = Justification.Left,
			};
			var hpaned = new HPaned ();
			vbox.PackStart (hpaned, true, true, 0);
			vbox.PackStart (labelStatus, false, false, 0);
			var resultsScroll = new CompactScrolledWindow ();
			hpaned.Pack1 (resultsScroll, true, true);
			scrolledwindowLogView = new CompactScrolledWindow ();
			hpaned.Pack2 (scrolledwindowLogView, true, true);
			textviewLog = new TextView () {
				Editable = false,
			};
			scrolledwindowLogView.Add (textviewLog);
			
			store = new ListStore (typeof (SearchResult),
				typeof (bool) // didRead
				);
			
			treeviewSearchResults = new PadTreeView () {
				Model = store,
				HeadersClickable = true,
				RulesHint = true,
			};
			treeviewSearchResults.Selection.Mode = Gtk.SelectionMode.Multiple;
			resultsScroll.Add (treeviewSearchResults);
			
			this.ShowAll ();
			
			var fileNameColumn = new TreeViewColumn {
				Resizable = false,
				SortColumnId = 0,
				Title = GettextCatalog.GetString("File")
			};

			fileNameColumn.FixedWidth = 200;

			var fileNamePixbufRenderer = new CellRendererPixbuf ();
			fileNameColumn.PackStart (fileNamePixbufRenderer, false);
			fileNameColumn.SetCellDataFunc (fileNamePixbufRenderer, FileIconDataFunc);
			
			fileNameColumn.PackStart (treeviewSearchResults.TextRenderer, true);
			fileNameColumn.SetCellDataFunc (treeviewSearchResults.TextRenderer, FileNameDataFunc);
			treeviewSearchResults.AppendColumn (fileNameColumn);
			
//			TreeViewColumn lineColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Line"), new CellRendererText (), ResultLineDataFunc);
//			lineColumn.SortColumnId = 1;
//			lineColumn.FixedWidth = 50;
//			
			
			TreeViewColumn textColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Text"),
				treeviewSearchResults.TextRenderer, ResultTextDataFunc);
			textColumn.SortColumnId = 2;
			textColumn.Resizable = false;
			textColumn.FixedWidth = 300;

			
			TreeViewColumn pathColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Path"),
				treeviewSearchResults.TextRenderer, ResultPathDataFunc);
			pathColumn.SortColumnId = 3;
			pathColumn.Resizable = false;
			pathColumn.FixedWidth = 500;

			
			store.SetSortFunc (0, CompareFileNames);
//			store.SetSortFunc (1, CompareLineNumbers);
			store.SetSortFunc (3, CompareFilePaths);

			treeviewSearchResults.RowActivated += TreeviewSearchResultsRowActivated;
			
			buttonStop = new ToolButton (Stock.Stop) { Sensitive = false };

			buttonStop.Clicked += ButtonStopClicked;
			
			buttonStop.TooltipText = GettextCatalog.GetString ("Stop");
			toolbar.Insert (buttonStop, -1);

			var buttonClear = new ToolButton (Gtk.Stock.Clear);
			buttonClear.Clicked += ButtonClearClicked;
			buttonClear.TooltipText = GettextCatalog.GetString ("Clear results");
			toolbar.Insert (buttonClear, -1);
			
			var buttonOutput = new ToggleToolButton (Gui.Stock.OutputIcon);
			buttonOutput.Clicked += ButtonOutputClicked;
			buttonOutput.TooltipText = GettextCatalog.GetString ("Show output");
			toolbar.Insert (buttonOutput, -1);
			
			buttonPin = new ToggleToolButton (Gui.Stock.PinUp);
			buttonPin.Clicked += ButtonPinClicked;
			buttonPin.TooltipText = GettextCatalog.GetString ("Pin results pad");
			toolbar.Insert (buttonPin, -1);
			
			store.SetSortColumnId (3, SortType.Ascending);
			ShowAll ();
			
			scrolledwindowLogView.Hide ();
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			highlightStyle = SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme);
		}
		
		protected override void OnStyleSet (Gtk.Style previousStyle)
		{
			base.OnStyleSet (previousStyle);
		}

		void ButtonPinClicked (object sender, EventArgs e)
		{
			buttonPin.StockId = buttonPin.Active? Gui.Stock.PinDown : Gui.Stock.PinUp;
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
			Reset ();
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
			IdeApp.Workbench.ActiveLocationList = this;
			newStore = new ListStore (typeof (SearchResult), typeof (bool));
			Reset ();
			buttonStop.Sensitive = true;
			treeviewSearchResults.FreezeChildNotify ();
		}
		
		ListStore newStore;

		public void EndProgress ()
		{
			buttonStop.Sensitive = false;
			newStore.SetSortFunc (0, CompareFileNames);
			newStore.SetSortFunc (1, CompareLineNumbers);
			newStore.SetSortFunc (3, CompareFilePaths);

			treeviewSearchResults.Model = newStore;

			store.Dispose ();
			store = newStore;

			treeviewSearchResults.ThawChildNotify ();
		}

		public void FocusPad ()
		{
			treeviewSearchResults.GrabFocus ();
			TreeIter iter;
			if (store.GetIterFirst (out iter)) 
				treeviewSearchResults.Selection.SelectIter (iter);
		}
		
		public void Reset ()
		{
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

		static Color AdjustColor (Color baseColor, Color color)
		{
			double b1 = Mono.TextEditor.HslColor.Brightness (color);
			double b2 = Mono.TextEditor.HslColor.Brightness (baseColor);
			double delta = Math.Abs (b1 - b2);
			if (delta < 0.1) {
				Mono.TextEditor.HslColor color1 = color;
				color1.L -= 0.5;
				if (Math.Abs (Mono.TextEditor.HslColor.Brightness (color1) - b2) < delta) {
					color1 = color;
					color1.L += 0.5;
				}
				return color1;
			}
			return color;
		}
		
		string AdjustColors (string markup)
		{
			var result = new StringBuilder ();
			int idx = markup.IndexOf ("foreground=\"");
			int offset = 0;



			// This is a workaround for Bug 559804 - Strings in search result pad are near-invisible
			// On mac it's not possible to get the white background color with the Base or Background
			// methods. If this bug is fixed or a better work around is found - remove this hack.
			Color baseColor = Platform.IsMac ?treeviewSearchResults.Style.Light (treeviewSearchResults.State) : treeviewSearchResults.Style.Base (treeviewSearchResults.State);
			
			while (idx > 0) {
				idx += "foreground=\"".Length;
				result.Append (markup.Substring (offset, idx - offset));
				if (idx + 7 >= markup.Length) {
					offset = idx;
					break;
				}
				offset = idx + 7;
				string colorStr = markup.Substring (idx, 7);
				
				Color color = Color.Zero;

				if (Color.Parse(colorStr, ref color))

					colorStr = SyntaxMode.ColorToPangoMarkup(AdjustColor(baseColor, color));

				result.Append (colorStr);
				idx = markup.IndexOf ("foreground=\"", idx);
			}
			result.Append (markup.Substring (offset, markup.Length - offset));
			return result.ToString ();
		}
		
		void DoPopupMenu (Gdk.EventButton evt)
		{ 
			IdeApp.CommandService.ShowContextMenu (this.treeviewSearchResults, evt, new CommandEntrySet () {
				new CommandEntry (ViewCommands.Open),
				new CommandEntry (EditCommands.Copy),
				new CommandEntry (EditCommands.SelectAll),
			}, this);
		}
		
		public void ShowStatus (string text)
		{
			labelStatus.Text = text;
		}
		
		void FileIconDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter))
				return;
			var fileNamePixbufRenderer = (CellRendererPixbuf) cell;
			var searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			if (searchResult == null)
				return;
			if (searchResult.Pixbuf == null)
				searchResult.Pixbuf = DesktopService.GetPixbufForFile (searchResult.FileName, IconSize.Menu);
			fileNamePixbufRenderer.Pixbuf = searchResult.Pixbuf;
		}



		static string MarkupText (string text, bool didRead)
		{
			return string.Format ("<span weight=\"{1}\">{0}</span>", GLib.Markup.EscapeText (text), didRead ? "normal" : "bold");
		}
		
		void FileNameDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter))
				return;
			var fileNameRenderer = (CellRendererText)cell;
			bool didRead = (bool)store.GetValue (iter, DidReadColumn);
			var searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			if (searchResult == null)
				return;
			if (searchResult.LineNumber <= 0) {
				var doc = GetDocument (searchResult);
				if (doc == null)
					return;
				searchResult.LineNumber = doc.OffsetToLineNumber (searchResult.Offset);
			}
			fileNameRenderer.Markup = MarkupText (System.IO.Path.GetFileName (searchResult.FileName) + ":" + searchResult.LineNumber, didRead);
		}
		
		int CompareLineNumbers (TreeModel model, TreeIter first, TreeIter second)
		{
			var loc1 = GetLocation ((SearchResult)model.GetValue (first, SearchResultColumn));
			var loc2 = GetLocation ((SearchResult)model.GetValue (second, SearchResultColumn));
			return loc1.Line.CompareTo (loc2.Line);
		}
		
		static int DefaultSortFunc (TreeModel model, TreeIter first, TreeIter second)
		{
			return 0;
		}
		
		static int CompareFileNames (TreeModel model, TreeIter first, TreeIter second)
		{
			var searchResult1 = (SearchResult)model.GetValue (first, SearchResultColumn);
			var searchResult2 = (SearchResult)model.GetValue (second, SearchResultColumn);
			if (searchResult1 == null || searchResult2 == null || searchResult1.FileName == null || searchResult2.FileName == null)
				return -1;
			return System.IO.Path.GetFileName (searchResult1.FileName).CompareTo (System.IO.Path.GetFileName (searchResult2.FileName));
		}
		
		static int CompareFilePaths (TreeModel model, TreeIter first, TreeIter second)
		{
			var searchResult1 = (SearchResult)model.GetValue (first, SearchResultColumn);
			var searchResult2 = (SearchResult)model.GetValue (second, SearchResultColumn);
			if (searchResult1 == null || searchResult2 == null || searchResult1.FileName == null || searchResult2.FileName == null)
				return -1;

			return System.IO.Path.GetDirectoryName (searchResult1.FileName).CompareTo (System.IO.Path.GetDirectoryName (searchResult2.FileName));

		}
		
		void ResultPathDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter))
				return;
			var pathRenderer = (CellRendererText)cell;
			var searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			if (searchResult == null)
				return;
			bool didRead = (bool)store.GetValue (iter, DidReadColumn);
			pathRenderer.Markup = MarkupText (System.IO.Path.GetDirectoryName (searchResult.FileName), didRead);
		}
		
//		void ResultLineDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
//		{
//			if (TreeIter.Zero.Equals (iter))
//				return;
//			var lineRenderer = (CellRendererText)cell;
//			var searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
//			if (searchResult == null)
//				return;
//			
//			Document doc = GetDocument (searchResult);
//			int lineNr = doc.OffsetToLineNumber (searchResult.Offset) + 1;
//			bool didRead = (bool)store.GetValue (iter, DidReadColumn);
//			lineRenderer.Markup = MarkupText (lineNr.ToString (), didRead);
//		}
//		
		void ResultTextDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter))
				return;
			var textRenderer = (CellRendererText)cell;
			var searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			if (searchResult == null || searchResult.Offset < 0) {
				textRenderer.Markup = "Invalid search result";
				return;
			}
			
			var doc = GetDocument (searchResult);
			if (doc == null) {
				textRenderer.Markup = "Can't create document for:" + searchResult.FileName;
				return;
			}
			bool isSelected = treeviewSearchResults.Selection.IterIsSelected (iter);

			if (searchResult.Markup == null) {
				if (searchResult.LineNumber <= 0)
					searchResult.LineNumber = doc.OffsetToLineNumber (searchResult.Offset); 
				DocumentLine line = doc.GetLine (searchResult.LineNumber );
				if (line == null) {
					textRenderer.Markup = "Invalid line number " + searchResult.LineNumber + " from offset: " + searchResult.Offset;
					return;
				}
				int indent = line.GetIndentation (doc).Length;
				var data = new Mono.TextEditor.TextEditorData (doc);
				data.ColorStyle = highlightStyle;
				var lineText = doc.GetTextAt (line.Offset + indent, line.Length - indent);
				int col = searchResult.Offset - line.Offset - indent;
				// search result contained part of the indent.
				if (col + searchResult.Length < lineText.Length)
					lineText = doc.GetTextAt (line.Offset, line.Length);

				var markup = doc.SyntaxMode != null ?
				data.GetMarkup (line.Offset + indent, line.Length - indent, true, !isSelected, false) :
				GLib.Markup.EscapeText (lineText);
				searchResult.Markup = AdjustColors (markup.Replace ("\t", new string (' ', TextEditorOptions.DefaultOptions.TabSize)));

				uint start;
				uint end;
				try {
					start = (uint)TextViewMargin.TranslateIndexToUTF8 (lineText, col);
					end = (uint)TextViewMargin.TranslateIndexToUTF8 (lineText, col + searchResult.Length);
				} catch (Exception e) {
					LoggingService.LogError ("Exception while translating index to utf8 (column was:" +col + " search result length:" + searchResult.Length + " line text:" + lineText + ")", e);
					return;
				}
				searchResult.StartIndex = start;
				searchResult.EndIndex = end;
			}


			try {
				textRenderer.Markup = searchResult.Markup;

				if (!isSelected) {
					var searchColor = highlightStyle.SearchResult.GetColor("color");
					double b1 = Mono.TextEditor.HslColor.Brightness (searchColor);
					double b2 = Mono.TextEditor.HslColor.Brightness (AdjustColor (Style.Base (StateType.Normal), (Mono.TextEditor.HslColor)highlightStyle.PlainText.Foreground));
					double delta = Math.Abs (b1 - b2);
					if (delta < 0.1) {
						Mono.TextEditor.HslColor color1 = highlightStyle.SearchResult.GetColor("color");
						if (color1.L + 0.5 > 1.0) {
							color1.L -= 0.5;
						} else {
							color1.L += 0.5;
						}
						searchColor = color1;
					}
					var attr = new Pango.AttrBackground ((ushort)(searchColor.R * ushort.MaxValue), (ushort)(searchColor.G * ushort.MaxValue), (ushort)(searchColor.B * ushort.MaxValue));
					attr.StartIndex = searchResult.StartIndex;
					attr.EndIndex = searchResult.EndIndex;

					using (var list = textRenderer.Attributes.Copy ()) {
						list.Insert (attr);
						textRenderer.Attributes = list;
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error whil setting the text renderer markup to: " + searchResult.Markup, e);
			}
		}
		
		static int FindPosition (string markup, int pos, out string tag)
		{
			bool inTag = false;
			bool inChar = false;
			int realPos = 0;
			var lastTag = new StringBuilder ();
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



		readonly Dictionary<string, TextDocument> documents = new Dictionary<string, TextDocument> ();
		
		TextDocument GetDocument (SearchResult result)
		{
			TextDocument doc;
			if (!documents.TryGetValue (result.FileName, out doc)) {
				var content = result.FileProvider.ReadString ();
				if (content == null)
					return null;
				doc = TextDocument.CreateImmutableDocument (content);
				doc.MimeType = DesktopService.GetMimeTypeForUri (result.FileName);
				
				documents [result.FileName] = doc;	
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
			newStore.InsertWithValues (ResultCount++, result, false);
		}
		
		public void AddRange (IEnumerable<SearchResult> results)
		{
			foreach (var result in results) {
				Add (result);
			}
		}
		
		void OpenDocumentAt (TreeIter iter)
		{
			var result = store.GetValue (iter, SearchResultColumn) as SearchResult;
			if (result != null) {
				DocumentLocation loc = GetLocation (result);
				store.SetValue (iter, DidReadColumn, true);
				IdeApp.Workbench.OpenDocument (result.FileName, loc.Line, loc.Column);
			}
		}
		
		DocumentLocation GetLocation (SearchResult searchResult)
		{
			var doc = GetDocument (searchResult);
			if (doc == null)
				return DocumentLocation.Empty;
			int lineNr = doc.OffsetToLineNumber (searchResult.Offset);
			DocumentLine line = doc.GetLine (lineNr);
			return new DocumentLocation (lineNr, searchResult.Offset - line.Offset + 1);
		}
		
		public void OpenSelectedMatches ()
		{
			foreach (TreePath path in treeviewSearchResults.Selection.GetSelectedRows ()) {
				TreeIter iter;
				if (!store.GetIter (out iter, path))
					continue;
				OpenDocumentAt (iter);
			}
			IdeApp.Workbench.ActiveLocationList = this;
		}
		
		public void SelectAll ()
		{
			treeviewSearchResults.Selection.SelectAll ();
		}
		
		public void CopySelection ()
		{
			TreeModel model;
			var sb = new StringBuilder ();
			foreach (TreePath p in treeviewSearchResults.Selection.GetSelectedRows (out model)) {
				TreeIter iter;
				if (!model.GetIter (out iter, p))
					continue;
				var result = store.GetValue (iter, SearchResultColumn) as SearchResult;
				if (result == null)
					continue;
				DocumentLocation loc = GetLocation (result);
				var doc = GetDocument (result);
				if (doc == null)
					continue;
				DocumentLine line = doc.GetLine (loc.Line);
				
				sb.AppendFormat ("{0} ({1}, {2}):{3}", result.FileName, loc.Line, loc.Column, doc.GetTextAt (line.Offset, line.Length));
				sb.AppendLine ();
			}
			Clipboard clipboard = Clipboard.Get (Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = sb.ToString ();
			
			clipboard = Clipboard.Get (Atom.Intern ("PRIMARY", false));
			clipboard.Text = sb.ToString ();
		}
		
		public string ItemName {
			get {
				return GettextCatalog.GetString ("Search Result");
			}
		}
		
		public NavigationPoint GetNextLocation ()
		{
			TreeIter iter;
			TreePath[] path = treeviewSearchResults.Selection.GetSelectedRows ();
			if (path != null && path.Length > 0 && store.GetIter (out iter, path[0])) {
				if (!store.IterNext (ref iter)) 
					store.GetIterFirst (out iter);
			} else {
				store.GetIterFirst (out iter);
			}
			
			return GetLocation (iter);
		}
		
		public NavigationPoint GetPreviousLocation ()
		{
			TreeIter iter;
			TreeIter prevIter = TreeIter.Zero;
			TreePath selPath = treeviewSearchResults.Selection.GetSelectedRows ().LastOrDefault ();
			
			bool hasNext = store.GetIterFirst (out iter);
			if (hasNext && IsIterSelected (selPath, iter))
				selPath = null;
			while (hasNext) {
				if (IsIterSelected (selPath, iter))
					break;
				prevIter = iter;
				hasNext = store.IterNext (ref iter);
			}
			
			return GetLocation (prevIter);
		}

		bool IsIterSelected (TreePath selPath, TreeIter iter)
		{
			return selPath != null && store.GetPath (iter).Equals (selPath);
		}

		NavigationPoint GetLocation (TreeIter iter)
		{
			treeviewSearchResults.Selection.UnselectAll ();
			if (!store.IterIsValid (iter))
				return null;

			treeviewSearchResults.Selection.SelectIter (iter);
			treeviewSearchResults.ScrollToCell (store.GetPath (iter), treeviewSearchResults.Columns [0], false, 0, 0);
			var searchResult = (SearchResult)store.GetValue (iter, SearchResultColumn);
			var doc = GetDocument (searchResult);
			if (doc == null)
				return null;
			DocumentLocation location = doc.OffsetToLocation (searchResult.Offset);
			return new SearchTextFileNavigationPoint (searchResult.FileName, location.Line, location.Column);
		}
		
		class SearchTextFileNavigationPoint : TextFileNavigationPoint 
		{
			public SearchTextFileNavigationPoint (FilePath file, int line, int column) : base (file, line, column)
			{
			}
			
			protected override Gui.Document DoShow ()
			{
				var doc = base.DoShow ();
				if (doc == null)
					return null;
				
				var buf = doc.GetContent<IEditableTextBuffer> ();
				if (buf != null)
					buf.SetCaretTo (Math.Max (Line, 1), Math.Max (Column, 1));
				
				return doc;
			}
			
		}
	}
}
