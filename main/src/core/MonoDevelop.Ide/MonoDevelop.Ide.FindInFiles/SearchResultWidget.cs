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


namespace MonoDevelop.Ide.FindInFiles
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class SearchResultWidget : Bin, ILocationList
	{

		ListStore store;

		readonly ToolButton buttonStop;

		readonly ToggleToolButton buttonPin;
		
		const int SearchResultColumn = 0;
		const int DidReadColumn      = 1;
		
		Mono.TextEditor.Highlighting.Style highlightStyle;
		
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
			Build ();
			
			store = new ListStore (typeof (SearchResult), 
			                       typeof (bool)          // didRead
			                       );
			treeviewSearchResults.Model = store;
			treeviewSearchResults.Selection.Mode = Gtk.SelectionMode.Multiple;
			treeviewSearchResults.HeadersClickable = true;
			treeviewSearchResults.PopupMenu += OnPopupMenu;
			treeviewSearchResults.ButtonPressEvent += HandleButtonPressEvent;
			treeviewSearchResults.RulesHint = true;
			
			var fileNameColumn = new TreeViewColumn {
				Resizable = false,
				SortColumnId = 0,
				Title = GettextCatalog.GetString("File")
			};

			fileNameColumn.FixedWidth = 200;

			var fileNamePixbufRenderer = new CellRendererPixbuf ();
			fileNameColumn.PackStart (fileNamePixbufRenderer, false);
			fileNameColumn.SetCellDataFunc (fileNamePixbufRenderer, FileIconDataFunc);
			
			var fileNameRenderer = new CellRendererText ();
			fileNameColumn.PackStart (fileNameRenderer, true);
			fileNameColumn.SetCellDataFunc (fileNameRenderer, FileNameDataFunc);
			treeviewSearchResults.AppendColumn (fileNameColumn);
			
//			TreeViewColumn lineColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Line"), new CellRendererText (), ResultLineDataFunc);
//			lineColumn.SortColumnId = 1;
//			lineColumn.FixedWidth = 50;
//			
			
			TreeViewColumn textColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Text"), new CellRendererText (), ResultTextDataFunc);
			textColumn.SortColumnId = 2;
			textColumn.Resizable = false;
			textColumn.FixedWidth = 300;

			
			TreeViewColumn pathColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Path"), new CellRendererText (), ResultPathDataFunc);
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
			highlightStyle = SyntaxModeService.GetColorStyle (Style, PropertyService.Get ("ColorScheme", "Default"));
		}
		
		protected override void OnStyleSet (Gtk.Style previousStyle)
		{
			base.OnStyleSet (previousStyle);
			if (highlightStyle != null)
				highlightStyle.UpdateFromGtkStyle (Style);
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

		[GLib.ConnectBefore]
		void HandleButtonPressEvent(object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				OnPopupMenu (this, null);
				args.RetVal = treeviewSearchResults.Selection.GetSelectedRows ().Length > 1;
			}
		}



		static Color AdjustColor (Color baseColor, Color color)
		{
			double b1 = HslColor.Brightness (color);
			double b2 = HslColor.Brightness (baseColor);
			double delta = Math.Abs (b1 - b2);
			if (delta < 0.1) {
				HslColor color1 = color;
				color1.L -= 0.5;
				if (Math.Abs (HslColor.Brightness (color1) - b2) < delta) {
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
		
		void OnPopupMenu (object sender, PopupMenuArgs args)
		{
			var contextMenu = new CommandEntrySet ();
			contextMenu.AddItem (ViewCommands.Open);
			contextMenu.AddItem (EditCommands.Copy);
			contextMenu.AddItem (EditCommands.SelectAll);
			IdeApp.CommandService.ShowContextMenu (contextMenu, this);
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
			fileNamePixbufRenderer.Pixbuf = DesktopService.GetPixbufForFile (searchResult.FileName, IconSize.Menu);
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
			Document doc = GetDocument (searchResult);
			if (doc == null)
				return;
			int lineNr = doc.OffsetToLineNumber (searchResult.Offset);
			fileNameRenderer.Markup = MarkupText (System.IO.Path.GetFileName (searchResult.FileName) + ":" + lineNr, didRead);
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
			
			Document doc = GetDocument (searchResult);
			if (doc == null) {
				textRenderer.Markup = "Can't create document for:" + searchResult.FileName;
				return;
			}
			int lineNr = doc.OffsetToLineNumber (searchResult.Offset);
			LineSegment line = doc.GetLine (lineNr);
			if (line == null) {
				textRenderer.Markup = "Invalid line number " + lineNr + " from offset: " + searchResult.Offset;
				return;
			}
			bool isSelected = treeviewSearchResults.Selection.IterIsSelected (iter);
			int indent = line.GetIndentation (doc).Length;
			string markup = doc.SyntaxMode != null ? 
				doc.SyntaxMode.GetMarkup (doc, new TextEditorOptions (), highlightStyle, line.Offset + indent, line.EditableLength - indent, true, !isSelected, false) : 
				GLib.Markup.EscapeText (doc.GetTextAt (line.Offset, line.EditableLength));
			
			if (!isSelected) {
				int col = searchResult.Offset - line.Offset - indent;
				string tag;
				int pos1 = FindPosition (markup, col, out tag);
				int pos2 = FindPosition (markup, col + searchResult.Length, out tag);
				if (pos1 >= 0 && pos2 >= 0) {
					markup = tag.StartsWith ("span") ? markup.Insert (pos2, "</span></span><" + tag + ">") : markup.Insert (pos2, "</span>");
					Color searchColor = Mono.TextEditor.Highlighting.Style.ToGdkColor (highlightStyle.SearchTextBg);
					double b1 = HslColor.Brightness (searchColor);
					double b2 = HslColor.Brightness (AdjustColor (Style.Base (StateType.Normal), highlightStyle.Default.Color));
					double delta = Math.Abs (b1 - b2);
					if (delta < 0.1) {
						HslColor color1 = highlightStyle.SearchTextBg;
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
			string markupText = AdjustColors (markup.Replace ("\t", new string (' ', TextEditorOptions.DefaultOptions.TabSize)));
			try {
				textRenderer.Markup = markupText;
			} catch (Exception e) {
				LoggingService.LogError ("Error whil setting the text renderer markup to: " + markup, e);
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



		readonly Dictionary<string, Document> documents = new Dictionary<string, Document> ();
		
		Document GetDocument (SearchResult result)
		{
			Document doc;
			if (!documents.TryGetValue (result.FileName, out doc)) {
				TextReader reader = result.FileProvider.Open ();
				if (reader == null)
					return null;
					doc = Document.CreateImmutableDocument (reader.ReadToEnd ());
				doc.MimeType = DesktopService.GetMimeTypeForUri (result.FileName);
				
				reader.Close ();
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
				IdeApp.Workbench.OpenDocument (result.FileName, loc.Line, loc.Column, true);
			}
		}
		
		DocumentLocation GetLocation (SearchResult searchResult)
		{
			Document doc = GetDocument (searchResult);
			if (doc == null)
				return DocumentLocation.Empty;
			int lineNr = doc.OffsetToLineNumber (searchResult.Offset);
			LineSegment line = doc.GetLine (lineNr);
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
				Document doc = GetDocument (result);
				if (doc == null)
					continue;
				LineSegment line = doc.GetLine (loc.Line);
				
				sb.AppendFormat ("{0} ({1}, {2}):{3}", result.FileName, loc.Line, loc.Column, doc.GetTextAt (line.Offset, line.EditableLength));
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
			Document doc = GetDocument (searchResult);
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
