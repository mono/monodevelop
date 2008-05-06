//
// SearchAndReplaceWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	public partial class SearchAndReplaceWidget : Gtk.Bin
	{
		const string replaceHistoryProperty = "MonoDevelop.FindReplaceDialogs.ReplaceHistory";

		SourceEditorWidget widget;
		ListStore searchHistory = new ListStore (typeof (string));
		ListStore replaceHistory = new ListStore (typeof (string));
		static string replacePattern = ""; // used to store the replace text between dialogs
		
		public string ReplacePattern {
			get {
				return this.entryReplace.Entry.Text;
			}
		}
		
		public string SearchPattern {
			get {
				return this.entrySearch.Entry.Text;
			}
			set {
				this.entrySearch.Entry.Text = value;
			}
		}
		
		public SearchAndReplaceWidget (SourceEditorWidget widget)
		{
			this.Build();
			
			this.widget = widget;
			
			#region Cut & Paste from SearchWidget
			if (String.IsNullOrEmpty (widget.TextEditor.SearchPattern)) {
				widget.TextEditor.SearchPattern = SearchWidget.searchPattern;
			} else if (widget.TextEditor.SearchPattern != SearchWidget.searchPattern) {
				SearchWidget.searchPattern = widget.TextEditor.SearchPattern;
				SearchWidget.FireSearchPatternChanged ();
			}
			this.entrySearch.Entry.Text = widget.TextEditor.SearchPattern;
			this.entrySearch.Model = searchHistory;
			RestoreSearchHistory ();
			#endregion
			
			this.entryReplace.Entry.Text = replacePattern;
			this.entryReplace.Model = replaceHistory;
			RestoreReplaceHistory ();
			
			foreach (Gtk.Widget child in this.Children) {
				child.KeyPressEvent += delegate (object sender, Gtk.KeyPressEventArgs args) {
					if (args.Event.Key == Gdk.Key.Escape)
						widget.RemoveSearchWidget ();
				};
			}
			this.closeButton.Clicked += delegate {
				widget.RemoveSearchWidget ();
			};
			this.buttonSearchMode.Clicked += delegate {
				widget.ShowSearchWidget ();
			};
//			this.comboboxSearchAs.AppendText (GettextCatalog.GetString ("Text"));
//			this.comboboxSearchAs.AppendText (GettextCatalog.GetString ("Regular Expressions"));
//			this.comboboxSearchAs.Active = 0;
			ReplacePatternChanged += UpdateReplacePattern;
			#region Cut & Paste from SearchWidget
			SearchWidget.SearchPatternChanged += UpdateSearchPattern;
			this.FocusChildSet += delegate {
				StoreWidgetState ();
			};
			this.entrySearch.Changed += delegate {
				widget.SetSearchPattern (SearchPattern);
				if (!SearchWidget.inSearchUpdate) {
					SearchWidget.searchPattern = SearchPattern;
					SearchWidget.FireSearchPatternChanged ();
				}
				UpdateSearchEntry ();
			};
			this.entrySearch.Entry.Activated += delegate {
				UpdateSearchHistory (SearchPattern);
				widget.TextEditor.GrabFocus ();
			};
			this.buttonSearchForward.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				widget.FindNext ();
			};
			this.buttonSearchBackward.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				widget.FindPrevious ();
			};
			optionsButton.Label = MonoDevelop.Core.GettextCatalog.GetString ("Options");
			optionsButton.MenuCreator =  delegate {
				Gtk.Menu menu = new Gtk.Menu ();
				
				Gtk.CheckMenuItem caseSensitive = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Case sensitive"));
				caseSensitive.Active = SearchWidget.IsCaseSensitive;
				caseSensitive.Toggled += delegate {
					SetIsCaseSensitive (caseSensitive.Active);
					UpdateSearchEntry ();
				};
				caseSensitive.ButtonPressEvent += delegate {
					caseSensitive.Toggle ();
				};
				menu.Append (caseSensitive);
				
				Gtk.CheckMenuItem wholeWordsOnly = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Whole words only"));
				wholeWordsOnly.Active = SearchWidget.IsWholeWordOnly;
				wholeWordsOnly.Toggled += delegate {
					SetIsWholeWordOnly (wholeWordsOnly.Active);
					UpdateSearchEntry ();
				};
				wholeWordsOnly.ButtonPressEvent += delegate {
					wholeWordsOnly.Toggle ();
				};
				menu.Append (wholeWordsOnly);
				Gtk.CheckMenuItem regexSearch = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Regex search"));
				regexSearch.Active = SearchWidget.SearchEngine == SearchWidget.RegexSearchEngine;
				regexSearch.Toggled += delegate {
					SetIsRegexSearch (regexSearch.Active);
					UpdateSearchEntry ();
				};
				regexSearch.ButtonPressEvent += delegate {
					regexSearch.Toggle ();
				};
				menu.Append (regexSearch);
				menu.Hidden += delegate {
					menu.Destroy ();
				};
				
				menu.ShowAll ();
				return menu;
			};
			optionsButton.ShowAll ();
			#endregion
			
			this.entryReplace.Changed += delegate {
				replacePattern = ReplacePattern;
				if (!inReplaceUpdate) 
					FireReplacePatternChanged ();
			};
			
			this.entryReplace.Entry.Activated += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.Replace ();
				this.entryReplace.Entry.GrabFocus ();
			};
			
			this.buttonReplace.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.Replace ();
			};
			
			this.buttonReplaceAll.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.ReplaceAll ();
			};
		}
		
		public override void Dispose ()
		{
			SearchWidget.SearchPatternChanged -= UpdateSearchPattern;
			ReplacePatternChanged -= UpdateReplacePattern;
			
			if (searchHistory != null) {
				searchHistory.Dispose ();
				searchHistory = null;
			}
			if (replaceHistory != null) {
				replaceHistory.Dispose ();
				replaceHistory = null;
			}
			widget = null;
			base.Dispose ();
		}
		
		public void Focus ()
		{
			this.entrySearch.GrabFocus ();
		}
		#region Cut & Paste from SearchWidget
		#region search preview
		//double vSave, hSave;
		DocumentLocation caretSave;
		
		void StoreWidgetState ()
		{
			//this.vSave  = widget.TextEditor.VAdjustment.Value;
			//this.hSave  = widget.TextEditor.HAdjustment.Value;
			this.caretSave =  widget.TextEditor.Caret.Location;
		}
		
		void GotoResult (SearchResult result)
		{
			try {
				if (result == null) {
					widget.TextEditor.ClearSelection ();
					return;
				}
				widget.TextEditor.Caret.Offset = result.EndOffset;
				widget.TextEditor.SelectionRange = result;
				widget.TextEditor.CenterToCaret ();
			} catch (System.Exception) { 
			}
		}		
		#endregion
		void UpdateSearchHistory (string item)
		{
			SearchWidget.UpdateHistory (SearchWidget.seachHistoryProperty, item);
			RestoreSearchHistory ();
		}
		
		void RestoreSearchHistory ()
		{
			this.searchHistory.Clear ();
			foreach (string item in SearchWidget.GetHistory (SearchWidget.seachHistoryProperty)) {
				this.searchHistory.AppendValues (item);
			}
		}
		
		void SetIsCaseSensitive (bool value)
		{
			PropertyService.Set ("IsCaseSensitive", value);
			widget.SetSearchOptions ();
		}
		
		void SetIsWholeWordOnly (bool value)
		{
			PropertyService.Set ("IsWholeWordOnly", value);
			widget.SetSearchOptions ();
		}
		
		void SetIsRegexSearch (bool value)
		{
			PropertyService.Set ("BufferSearchEngine", value ? SearchWidget.RegexSearchEngine : 
			                                                   SearchWidget.DefaultSearchEngine);
			widget.SetSearchOptions ();
		}
		
		void UpdateSearchPattern (object sender, EventArgs args)
		{
			this.entrySearch.Entry.Text = SearchWidget.searchPattern;
		}
		
		string oldPattern = null;
		void UpdateSearchEntry ()
		{
			if (oldPattern == SearchPattern)
				return;
			oldPattern = SearchPattern;
			widget.SetSearchOptions ();
			SearchResult result = widget.TextEditor.SearchForward (widget.TextEditor.Document.LocationToOffset (caretSave));
			if (result == null && !String.IsNullOrEmpty (SearchPattern)) {
				this.entrySearch.Entry.ModifyBase (Gtk.StateType.Normal, GotoLineNumberWidget.errorColor);
			} else {
				this.entrySearch.Entry.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
			}
			GotoResult (result);
		}
		#endregion
		
		void UpdateReplaceHistory (string item)
		{
			SearchWidget.UpdateHistory (replaceHistoryProperty, item);
			RestoreReplaceHistory ();
		}
		
		void RestoreReplaceHistory ()
		{
			this.replaceHistory.Clear ();
			foreach (string item in SearchWidget.GetHistory (replaceHistoryProperty)) {
				this.replaceHistory.AppendValues (item);
			}
		}
		
		void UpdateReplacePattern (object sender, EventArgs args)
		{
			this.entryReplace.Entry.Text = replacePattern;
		}
		
		internal static bool inReplaceUpdate = false;
		internal static void FireReplacePatternChanged ()
		{
			inReplaceUpdate = true;
			if (ReplacePatternChanged != null)
				ReplacePatternChanged (null, EventArgs.Empty);
			inReplaceUpdate = false;
		}
		
		internal static event EventHandler ReplacePatternChanged;
	}
}
