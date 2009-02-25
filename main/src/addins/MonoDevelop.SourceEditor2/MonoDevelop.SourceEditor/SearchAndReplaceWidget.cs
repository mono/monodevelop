//
// SearchAndReplaceWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Aaron Bockover <abockover@novell.com>
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
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	
	partial class SearchAndReplaceWidget : Gtk.Bin
	{
		const char historySeparator = '\n';
		const int  historyLimit = 20;
		const string seachHistoryProperty = "MonoDevelop.FindReplaceDialogs.FindHistory";
		const string replaceHistoryProperty = "MonoDevelop.FindReplaceDialogs.ReplaceHistory";

		internal static string searchPattern = String.Empty;
		internal static string replacePattern = String.Empty;

		SourceEditorWidget widget;
		ListStore searchHistory = new ListStore (typeof (string));
		ListStore replaceHistory = new ListStore (typeof (string));
		
		bool isReplaceMode = true;
		Widget [] replaceWidgets;
		
		public static bool IsCaseSensitive {
			get { return PropertyService.Get ("IsCaseSensitive", true); }
		}
		
		public static bool IsWholeWordOnly {
			get { return PropertyService.Get ("IsWholeWordOnly", false); }
		}
		
		public const string DefaultSearchEngine = "default";
		public const string RegexSearchEngine   = "regex";
		public static string SearchEngine {
			get {
				return PropertyService.Get ("BufferSearchEngine", DefaultSearchEngine);
			}
		}
		
		public string ReplacePattern {
			get { return entryReplace.Entry.Text; }
		}
		
		public string SearchPattern {
			get { return entrySearch.Entry.Text; }
			set { entrySearch.Entry.Text = value; }
		}

		public SearchAndReplaceWidget (SourceEditorWidget widget)
		{
			Build();
			
			replaceWidgets = new Widget [] {
				labelReplace,
				entryReplace,
				buttonReplace,
				buttonReplaceAll
			};
			
			this.widget = widget;
			
			if (String.IsNullOrEmpty (widget.TextEditor.SearchPattern)) {
				widget.TextEditor.SearchPattern = searchPattern;
			} else if (widget.TextEditor.SearchPattern != searchPattern) {
				searchPattern = widget.TextEditor.SearchPattern;
				//FireSearchPatternChanged ();
			}
			
			entrySearch.Entry.Text = widget.TextEditor.SearchPattern;
			entrySearch.Model = searchHistory;
			
			RestoreSearchHistory ();
			
			entryReplace.Entry.Text = replacePattern;
			entryReplace.Model = replaceHistory;
			RestoreReplaceHistory ();
			
			foreach (Gtk.Widget child in Children) {
				child.KeyPressEvent += delegate (object sender, Gtk.KeyPressEventArgs args) {
					if (args.Event.Key == Gdk.Key.Escape)
						widget.RemoveSearchWidget ();
				};
			}
			
			closeButton.Clicked += delegate {
				widget.RemoveSearchWidget ();
			};
			
			buttonSearchMode.Clicked += delegate {
				IsReplaceMode = !IsReplaceMode;
			};
			
			// comboboxSearchAs.AppendText (GettextCatalog.GetString ("Text"));
			// comboboxSearchAs.AppendText (GettextCatalog.GetString ("Regular Expressions"));
			// comboboxSearchAs.Active = 0;
			// ReplacePatternChanged += UpdateReplacePattern;
			
			//SearchPatternChanged += UpdateSearchPattern;
			FocusChildSet += delegate {
				StoreWidgetState ();
			};
			
			entrySearch.Changed += delegate {
				widget.SetSearchPattern (SearchPattern);
				// if (!inSearchUpdate) {
					searchPattern = SearchPattern;
				// 	FireSearchPatternChanged ();
				// }
				UpdateSearchEntry ();
			};
			entrySearch.Entry.Activated += delegate {
				UpdateSearchHistory (SearchPattern);
				buttonSearchForward.GrabFocus ();
			};
			
			buttonSearchForward.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				widget.FindNext (false);
			};
			
			buttonSearchBackward.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				widget.FindPrevious (false);
			};
			
			optionsButton.Label = MonoDevelop.Core.GettextCatalog.GetString ("Options");
			optionsButton.MenuCreator =  delegate {
				Gtk.Menu menu = new Gtk.Menu ();
				
				Gtk.CheckMenuItem caseSensitive = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Case sensitive"));
				caseSensitive.Active = IsCaseSensitive;
				caseSensitive.Toggled += delegate {
					SetIsCaseSensitive (caseSensitive.Active);
					UpdateSearchEntry ();
				};
				
				caseSensitive.ButtonPressEvent += delegate {
					caseSensitive.Toggle ();
				};
				
				menu.Append (caseSensitive);
				
				Gtk.CheckMenuItem wholeWordsOnly = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Whole words only"));
				wholeWordsOnly.Active = IsWholeWordOnly;
				wholeWordsOnly.Toggled += delegate {
					SetIsWholeWordOnly (wholeWordsOnly.Active);
					UpdateSearchEntry ();
				};
				
				wholeWordsOnly.ButtonPressEvent += delegate {
					wholeWordsOnly.Toggle ();
				};
				
				menu.Append (wholeWordsOnly);
				Gtk.CheckMenuItem regexSearch = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Regex search"));
				regexSearch.Active = SearchEngine == RegexSearchEngine;
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
			
			entryReplace.Changed += delegate {
				replacePattern = ReplacePattern;
				if (!inReplaceUpdate) 
					FireReplacePatternChanged ();
			};
			
			entryReplace.Entry.Activated += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.Replace ();
				entryReplace.Entry.GrabFocus ();
			};
			
			buttonReplace.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.Replace ();
			};
			
			buttonReplaceAll.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.ReplaceAll ();
			};
			
			buttonSearchForward.KeyPressEvent += OnNavigateKeyPressEvent;
			buttonSearchBackward.KeyPressEvent += OnNavigateKeyPressEvent;
			entrySearch.KeyPressEvent += OnNavigateKeyPressEvent;
		}
		
		private void OnNavigateKeyPressEvent (object o, KeyPressEventArgs args)
		{
			args.RetVal = false;
			switch (args.Event.Key) {
				case Gdk.Key.Return:
				case Gdk.Key.KP_Enter:/*
I think this is not needed, this code leads to a search twice bug when you hit return. 
But I leave it in in the case I've missed something. Mike
					if (o == buttonSearchBackward || o == buttonSearchForward) {
						if (!((Button)o).HasFocus)
							((Button)o).Click ();
					}*/
					break;
				case Gdk.Key.N:
				case Gdk.Key.n:
					buttonSearchForward.GrabFocus ();
					buttonSearchForward.Click ();
					break;
				case Gdk.Key.P:
				case Gdk.Key.p:
					buttonSearchBackward.GrabFocus ();
					buttonSearchBackward.Click ();
					break;
				case Gdk.Key.Escape:
					widget.RemoveSearchWidget ();
					break;
				case Gdk.Key.Up:
					widget.TextEditor.GrabFocus ();
					break;
				case Gdk.Key.slash:
					entrySearch.Entry.GrabFocus ();
					break;
				case Gdk.Key.ISO_Left_Tab:
					if (o == entrySearch) {
						buttonSearchMode.GrabFocus ();
					} else if (o == buttonSearchBackward) {
						entrySearch.Entry.GrabFocus ();
					} else if (o == buttonSearchForward) {
						buttonSearchBackward.GrabFocus ();
					}
					break;
				case Gdk.Key.Tab: 
					if (o == entrySearch) {
						buttonSearchBackward.GrabFocus ();
					} else if (o == buttonSearchBackward) {
						buttonSearchForward.GrabFocus ();
					} else if (o == buttonSearchForward) {
						widget.TextEditor.GrabFocus ();
					}
					break;
				default:
					args.RetVal = true;
					break;
			}
		}
		
		public bool IsReplaceMode {
			get { return isReplaceMode; }
			set {
				if (value == isReplaceMode)
					return;
				
				isReplaceMode = value;
				searchButtonModeArrow.ArrowType = isReplaceMode ? ArrowType.Down : ArrowType.Up;
				table.RowSpacing = isReplaceMode ? 6u : 0u;
				foreach (Widget widget in replaceWidgets) {
					widget.Visible = isReplaceMode;
				}
			}
		}
		
		protected override void OnDestroyed ()
		{
			// SearchPatternChanged -= UpdateSearchPattern;
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
			base.OnDestroyed ();
		}
		
		public void Focus ()
		{
			entrySearch.GrabFocus ();
		}
		
		// double vSave, hSave;
		DocumentLocation caretSave;
		
		void StoreWidgetState ()
		{
			// vSave  = widget.TextEditor.VAdjustment.Value;
			// hSave  = widget.TextEditor.HAdjustment.Value;
			caretSave =  widget.TextEditor.Caret.Location;
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
		
		static List<string> GetHistory (string propertyKey)
		{
			string stringArray = PropertyService.Get<string> (propertyKey);
			if (String.IsNullOrEmpty (stringArray))
				return new List<string> ();
			return new List<string> (stringArray.Split (historySeparator));
		}
		
		void StoreHistory (string propertyKey, List<string> history)
		{
			PropertyService.Set (propertyKey, String.Join (historySeparator.ToString (), history.ToArray ()));
		}
		
		void UpdateHistory (string propertyKey, string item)
		{
			List<string> history = GetHistory (propertyKey);
			history.Remove (item);
			history.Insert (0, item);
			while (history.Count >= historyLimit) 
				history.RemoveAt (historyLimit - 1);
			
			StoreHistory (propertyKey, history);
		}
		
		void UpdateSearchHistory (string item)
		{
			UpdateHistory (seachHistoryProperty, item);
			RestoreSearchHistory ();
		}
		
		void RestoreSearchHistory ()
		{
			this.searchHistory.Clear ();
			foreach (string item in GetHistory (seachHistoryProperty)) {
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
			PropertyService.Set ("BufferSearchEngine", value ? RegexSearchEngine : 
			                                                   DefaultSearchEngine);
			widget.SetSearchOptions ();
		}
		
//		void UpdateSearchPattern (object sender, EventArgs args)
//		{
//			entrySearch.Entry.Text = searchPattern;
//		}
		
		string oldPattern;
		void UpdateSearchEntry ()
		{
			if (oldPattern == SearchPattern)
				return;
				
			oldPattern = SearchPattern;
			widget.SetSearchOptions ();
			SearchResult result = widget.TextEditor.SearchForward (widget.TextEditor.Document.LocationToOffset (caretSave));
			bool error = result == null && !String.IsNullOrEmpty (SearchPattern);
			string errorMsg;
			bool valid = widget.TextEditor.SearchEngine.IsValidPattern (searchPattern, out errorMsg);
			error |= !valid;
			
			if (!valid) {
				IdeApp.Workbench.StatusBar.ShowError (errorMsg);
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			
			if (error) {
				entrySearch.Entry.ModifyBase (Gtk.StateType.Normal, GotoLineNumberWidget.errorColor);
			} else {
				entrySearch.Entry.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
			}
			
			GotoResult (result);
		}
		
		void UpdateReplaceHistory (string item)
		{
			UpdateHistory (replaceHistoryProperty, item);
			RestoreReplaceHistory ();
		}
		
		void RestoreReplaceHistory ()
		{
			replaceHistory.Clear ();
			foreach (string item in GetHistory (replaceHistoryProperty)) {
				replaceHistory.AppendValues (item);
			}
		}
		
		void UpdateReplacePattern (object sender, EventArgs args)
		{
			entryReplace.Entry.Text = replacePattern;
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
