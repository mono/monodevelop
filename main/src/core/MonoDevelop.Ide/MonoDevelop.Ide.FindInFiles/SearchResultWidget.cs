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

using System.Collections.Generic;
using MonoDevelop.Core;
using System.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Components;
using System.Threading;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Threading.Tasks;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.FindInFiles
{
	class SearchResultWidget : HBox, ILocationList
	{
		ListStore store;

		readonly ToolButton buttonStop;

		readonly ToggleToolButton buttonPin;
		
		const int SearchResultColumn = 0;
		const int DidReadColumn      = 1;
		
		EditorTheme highlightStyle;

		internal EditorTheme HighlightStyle {
			get {
				return highlightStyle;
			}
		}

		ScrolledWindow scrolledwindowLogView; 
		PadTreeView treeviewSearchResults;
		Label labelStatus;
		TextView textviewLog;
		TreeViewColumn pathColumn;

		public CancellationTokenSource CancellationTokenSource {
			get;
			set;
		}

		private PathMode pathMode;
		internal PathMode PathMode {
			set {
				pathMode = value;

				pathColumn.Visible = (value != PathMode.Hidden);
			}
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

			var projectColumn = new TreeViewColumn {
				Resizable = true,
				SortColumnId = 1,
				Title = GettextCatalog.GetString ("Project"),
				Sizing = TreeViewColumnSizing.Fixed,
				FixedWidth = 100
			};

			var projectPixbufRenderer = new CellRendererImage ();
			projectColumn.PackStart (projectPixbufRenderer, false);
			projectColumn.SetCellDataFunc (projectPixbufRenderer, ResultProjectIconDataFunc);

			var renderer = treeviewSearchResults.TextRenderer;
			renderer.Ellipsize = Pango.EllipsizeMode.End;
			projectColumn.PackStart (renderer, true);
			projectColumn.SetCellDataFunc (renderer, ResultProjectDataFunc);
			treeviewSearchResults.AppendColumn (projectColumn);

			var fileNameColumn = new TreeViewColumn {
				Resizable = true,
				SortColumnId = 2,
				Title = GettextCatalog.GetString ("File"),
				Sizing = TreeViewColumnSizing.Fixed,
				FixedWidth = 200
			};

			var fileNamePixbufRenderer = new CellRendererImage ();
			fileNameColumn.PackStart (fileNamePixbufRenderer, false);
			fileNameColumn.SetCellDataFunc (fileNamePixbufRenderer, FileIconDataFunc);
			
			fileNameColumn.PackStart (renderer, true);
			fileNameColumn.SetCellDataFunc (renderer, FileNameDataFunc);
			treeviewSearchResults.AppendColumn (fileNameColumn);


			TreeViewColumn textColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Text"),
				renderer, ResultTextDataFunc);
			textColumn.Resizable = true;
			textColumn.Sizing = TreeViewColumnSizing.Fixed;
			textColumn.FixedWidth = 300;

			pathColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Path"),
				                            renderer, ResultPathDataFunc);
			pathColumn.SortColumnId = 3;
			pathColumn.Resizable = true;
			pathColumn.Sizing = TreeViewColumnSizing.Fixed;
			pathColumn.FixedWidth = 500;

			store.DefaultSortFunc = DefaultSortFunc;
			store.SetSortFunc (1, CompareProjectFileNames);
			store.SetSortFunc (2, CompareFileNames);
			store.SetSortFunc (3, CompareFilePaths);

			treeviewSearchResults.RowActivated += TreeviewSearchResultsRowActivated;
			
			buttonStop = new ToolButton (new ImageView (Gui.Stock.Stop, Gtk.IconSize.Menu), null) { Sensitive = false };
			buttonStop.Clicked += ButtonStopClicked;
			buttonStop.TooltipText = GettextCatalog.GetString ("Stop");
			toolbar.Insert (buttonStop, -1);

			var buttonClear = new ToolButton (new ImageView (Gui.Stock.Clear, Gtk.IconSize.Menu), null);
			buttonClear.Clicked += ButtonClearClicked;
			buttonClear.TooltipText = GettextCatalog.GetString ("Clear results");
			toolbar.Insert (buttonClear, -1);
			
			var buttonOutput = new ToggleToolButton ();
			buttonOutput.IconWidget = new ImageView (Gui.Stock.OutputIcon, Gtk.IconSize.Menu);
			buttonOutput.Clicked += ButtonOutputClicked;
			buttonOutput.TooltipText = GettextCatalog.GetString ("Show output");
			toolbar.Insert (buttonOutput, -1);
			
			buttonPin = new ToggleToolButton ();
			buttonPin.IconWidget = new ImageView (Gui.Stock.PinUp, Gtk.IconSize.Menu);
			buttonPin.Clicked += ButtonPinClicked;
			buttonPin.TooltipText = GettextCatalog.GetString ("Pin results pad");
			toolbar.Insert (buttonPin, -1);

			// store.SetSortColumnId (3, SortType.Ascending);
			ShowAll ();
			
			scrolledwindowLogView.Hide ();
			treeviewSearchResults.FixedHeightMode = true;

			UpdateStyles ();
			IdeApp.Preferences.ColorScheme.Changed += UpdateStyles;
		}

		void UpdateStyles (object sender = null, EventArgs e = null)
		{
			highlightStyle = SyntaxHighlightingService.GetEditorTheme (IdeApp.Preferences.ColorScheme);
			if (!highlightStyle.FitsIdeTheme (IdeApp.Preferences.UserInterfaceTheme))
				highlightStyle = SyntaxHighlightingService.GetDefaultColorStyle (Ide.IdeApp.Preferences.UserInterfaceTheme);

			if (IsRealized) {
				store.Foreach ((model, path, iter) => {
					model.EmitRowChanged (path, iter);
					return false;
				});
			}
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
			if (CancellationTokenSource != null)
				CancellationTokenSource.Cancel ();
		}

		void TreeviewSearchResultsRowActivated(object o, RowActivatedArgs args)
		{
			OpenSelectedMatches ();
		}
		
		public void BeginProgress ()
		{
			IdeApp.Workbench.ActiveLocationList = this;
			newStore = new ListStore (typeof (SearchResult), typeof (bool));
			newStore.DefaultSortFunc = DefaultSortFunc;
			newStore.SetSortFunc (1, CompareProjectFileNames);
			newStore.SetSortFunc (2, CompareFileNames);
			newStore.SetSortFunc (3, CompareFilePaths);
			newStore.SetSortColumnId (2, SortType.Ascending); 
			Reset ();
			buttonStop.Sensitive = true;
			treeviewSearchResults.FreezeChildNotify ();
		}
		
		ListStore newStore;

		public void EndProgress ()
		{
			buttonStop.Sensitive = false;
			store = newStore;
			newStore = null;
			treeviewSearchResults.Model = store;
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
			if (treeviewSearchResults.IsRealized)
				treeviewSearchResults.ScrollToPoint (0, 0);

			ResultCount = 0;
			store.Clear ();
			labelStatus.Text = "";
			textviewLog.Buffer.Clear ();
		}
		
		protected override void OnDestroyed ()
		{
			Reset ();
			base.OnDestroyed ();
		}

		internal static Color AdjustColor (Color baseColor, Color color)
		{
			double b1 = HslColor.Brightness (color);
			double b2 = HslColor.Brightness (baseColor);
			double delta = Math.Abs (b1 - b2);
			if (delta < 0.1) {
				HslColor color1 = color;
				color1.L += IdeApp.Preferences.UserInterfaceTheme == Theme.Light ? -0.5 : 0.5;
				if (Math.Abs (HslColor.Brightness (color1) - b2) < delta) {
					color1 = color;
					color1.L += IdeApp.Preferences.UserInterfaceTheme == Theme.Light ? 0.5 : -0.5;
				}
				return color1;
			}
			return color;
		}
		
		internal string AdjustColors (string markup)
		{
			var result = new StringBuilder ();
			int idx = markup.IndexOf ("foreground=\"", StringComparison.Ordinal);
			int offset = 0;


			// This is a workaround for Bug 559804 - Strings in search result pad are near-invisible
			// On mac it's not possible to get the white background color with the Base or Background
			// methods. If this bug is fixed or a better work around is found - remove this hack.
			Color baseColor = Platform.IsMac ?treeviewSearchResults.Style.Light (treeviewSearchResults.State) : treeviewSearchResults.Style.Base (treeviewSearchResults.State);
			
			while (idx > 0) {
				idx += "foreground=\"".Length;
				result.Append (markup, offset, idx - offset);
				if (idx + 7 >= markup.Length) {
					offset = idx;
					break;
				}
				offset = idx + 7;
				string colorStr = markup.Substring (idx, 7);
				
				Color color = Color.Zero;

				if (Color.Parse(colorStr, ref color))
					colorStr = ColorToPangoMarkup(AdjustColor(baseColor, color));

				result.Append (colorStr);
				idx = markup.IndexOf ("foreground=\"", idx, StringComparison.Ordinal);
			}
			result.Append (markup, offset, markup.Length - offset);
			return result.ToString ();
		}
		public static string ColorToPangoMarkup (Gdk.Color color)
		{
			return string.Format ("#{0:X2}{1:X2}{2:X2}", color.Red >> 8, color.Green >> 8, color.Blue >> 8);
		}
		void DoPopupMenu (Gdk.EventButton evt)
		{ 
			IdeApp.CommandService.ShowContextMenu (this.treeviewSearchResults, evt, new CommandEntrySet {
				new CommandEntry (ViewCommands.Open),
				new CommandEntry (EditCommands.Copy),
				new CommandEntry (EditCommands.SelectAll),
			}, this);
		}
		
		public void ShowStatus (string text)
		{
			labelStatus.Text = text;
		}
		
		static void FileIconDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter))
				return;
			var fileNamePixbufRenderer = (CellRendererImage) cell;
			var searchResult = (SearchResult)model.GetValue (iter, SearchResultColumn);
			if (searchResult == null)
				return;
			fileNamePixbufRenderer.Image = DesktopService.GetIconForFile (searchResult.FileName, IconSize.Menu);
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
			bool didRead = (bool)model.GetValue (iter, DidReadColumn);
			var searchResult = (SearchResult)model.GetValue (iter, SearchResultColumn);
			if (searchResult == null)
				return;
			fileNameRenderer.Markup = MarkupText (System.IO.Path.GetFileName (searchResult.FileName) + ":" + searchResult.GetLineNumber (this), didRead);
		}
		
		int CompareLineNumbers (TreeModel model, TreeIter first, TreeIter second)
		{
			var loc1 = ((SearchResult)model.GetValue (first, SearchResultColumn)).GetLocation (this);
			var loc2 = ((SearchResult)model.GetValue (second, SearchResultColumn)).GetLocation (this);
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
			var strCompare = string.Compare (System.IO.Path.GetFileName (searchResult1.FileName), System.IO.Path.GetFileName (searchResult2.FileName), StringComparison.Ordinal);
			if (strCompare == 0) {
				return searchResult1.Offset.CompareTo (searchResult2.Offset);
			} else {
				return strCompare;
			}
		}

		static int CompareProjectFileNames (TreeModel model, TreeIter first, TreeIter second)
		{
			var searchResult1 = (SearchResult)model.GetValue (first, SearchResultColumn);
			var searchResult2 = (SearchResult)model.GetValue (second, SearchResultColumn);
			if (searchResult1 == null || searchResult2 == null ||
				searchResult1.Projects == null || searchResult2.Projects == null ||
				searchResult1.Projects.Count == 0 || searchResult2.Projects.Count == 0)
				return -1;
			return string.Compare (System.IO.Path.GetFileName (searchResult1.Projects [0].FileName), System.IO.Path.GetFileName (searchResult2.Projects [0].FileName), StringComparison.Ordinal);
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
			var searchResult = (SearchResult)model.GetValue (iter, SearchResultColumn);
			if (searchResult == null)
				return;
			bool didRead = (bool)model.GetValue (iter, DidReadColumn);

			var fileName = searchResult.FileName;
			string baseSolutionPath = null;
			if (pathMode == PathMode.Relative) {
				var workspace = IdeApp.Workspace;
				var solutions = workspace != null ? workspace.GetAllSolutions () : null;
				baseSolutionPath = solutions != null && solutions.Count () == 1 ? solutions.First ().BaseDirectory : null;
			}
			var finalFileName = baseSolutionPath == null ? fileName :
				FileService.AbsoluteToRelativePath (baseSolutionPath, fileName);
			var directory = System.IO.Path.GetDirectoryName (finalFileName);

			pathRenderer.Markup = MarkupText (directory, didRead);
		}

		static void ResultProjectIconDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter))
				return;
			var fileNamePixbufRenderer = (CellRendererImage) cell;
			var searchResult = (SearchResult)model.GetValue (iter, SearchResultColumn);
			if (searchResult == null)
				return;
			if (searchResult.Projects.Count > 0) {
				fileNamePixbufRenderer.Image = ImageService.GetIcon (searchResult.Projects [0].StockIcon).WithSize (Gtk.IconSize.Menu);
			} else {
				fileNamePixbufRenderer.Image = null;
			}
		}

		static void ResultProjectDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter))
				return;
			var pathRenderer = (CellRendererText)cell;
			var searchResult = (SearchResult)model.GetValue (iter, SearchResultColumn);
			if (searchResult == null)
				return;
			bool didRead = (bool)model.GetValue (iter, DidReadColumn);
			string projectNameMarkup;
			if (searchResult.Projects.Count > 0)
				projectNameMarkup = MarkupText (String.Join (", ", searchResult.Projects.Select (p => p.Name)), didRead);
			else
				projectNameMarkup = "";
			pathRenderer.Markup = projectNameMarkup;
		}


		void ResultTextDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter))
				return;
			var textRenderer = (CellRendererText)cell;
			var searchResult = (SearchResult)model.GetValue (iter, SearchResultColumn);
			if (searchResult == null || searchResult.Offset < 0) {
				textRenderer.Markup = "Invalid search result";
				return;
			}
			bool isSelected = treeviewSearchResults.Selection.IterIsSelected (iter);
			string textMarkup = searchResult.GetMarkup (this, isSelected);
			textRenderer.Markup = textMarkup;
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

		public void WriteText (string text)
		{
			TextIter iter = textviewLog.Buffer.EndIter;
			textviewLog.Buffer.Insert (ref iter, text);
			if (text.EndsWith ("\n", StringComparison.Ordinal))
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
				var loc = result.GetLocation (this);
				store.SetValue (iter, DidReadColumn, true);
				IdeApp.Workbench.OpenDocument (result.FileName, null, loc.Line, loc.Column);
			}
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

				sb.AppendFormat (result.GetCopyData (this));
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
			var location = searchResult.GetLocation (this);
			if (location.IsEmpty)
				return null;
			return new SearchTextFileNavigationPoint (searchResult.FileName, location.Line, location.Column);
		}
		
		class SearchTextFileNavigationPoint : TextFileNavigationPoint 
		{
			public SearchTextFileNavigationPoint (FilePath file, int line, int column) : base (file, line, column)
			{
			}
			
			protected override async Task<Gui.Document> DoShow ()
			{
				var doc = await base.DoShow ();
				if (doc == null)
					return null;
				
				var buf = doc.Editor;
				if (buf != null) {
					doc.DisableAutoScroll ();
					buf.RunWhenLoaded (() => {
						JumpToCurrentLocation (buf);
					});
				}
				
				return doc;
			}
			
		}
	}

	public static class PangoHelper
	{
		public static string ColorToPangoMarkup (Gdk.Color color)
		{
			return string.Format ("#{0:X2}{1:X2}{2:X2}", color.Red >> 8, color.Green >> 8, color.Blue >> 8);
		}

		public static string ColorMarkupBackground (string textMarkup, int startIndex, int endIndex, HslColor searchColor)
		{
			var markupBuilder = new StringBuilder();
			bool inMarkup = false, inEntity = false, closed = false, opened = false;
			int i = 0;
			for (int j = 0; j < textMarkup.Length; j++) {
				var ch = textMarkup [j];
				if (inEntity) {
					if (ch == ';') {
						inEntity = false;
						i++;
					}
					markupBuilder.Append (ch);
					continue;
				}
				if (inMarkup) {
					if (ch == '>') {
						inMarkup = false;
						markupBuilder.Append (ch);
						if (i > startIndex && markupBuilder.ToString ().EndsWith("</span>")) {
							if (opened && !closed) {
								markupBuilder.Append ("</span>");
								opened = false;
							}
							markupBuilder.Append (textMarkup, j + 1, textMarkup.Length - j - 1);
							return ColorMarkupBackground (markupBuilder.ToString (), i, endIndex, searchColor);
						}
						continue;
					}
					markupBuilder.Append (ch);
					continue;
				}
				if (i == endIndex) {
					if (opened) {
						markupBuilder.Append ("</span>");
						opened = false;
					}
					markupBuilder.Append (textMarkup, j, textMarkup.Length - j);
					closed = true;
					break;
				}

				if (ch == '<') {
					inMarkup = true;
					markupBuilder.Append (ch);
					continue;
				}
				if (i == startIndex) {
					opened = true;
					markupBuilder.Append ("<span background=\"").Append (ColorToPangoMarkup (searchColor)).Append ("\">");
				}
				if (ch == '&') {
					inEntity = true;
					markupBuilder.Append (ch);
					continue;
				}
				markupBuilder.Append (ch);
				i++;
			}
			if (!closed && opened)
				markupBuilder.Append ("</span>");
			return markupBuilder.ToString ();
		}
	}
}
