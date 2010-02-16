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
using System.Linq;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	
	partial class SearchAndReplaceWidget : Bin
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
			set { 
				if (IsCaseSensitive != value)
					PropertyService.Set ("IsCaseSensitive", value); 
			}
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
			get { return entryReplace.Text; }
		}
		
		public string SearchPattern {
			get { return searchEntry.Query; }
			set { searchEntry.Query = value; }
		}
		
		public bool SearchFocused {
			get {
				return searchEntry.HasFocus;
			}
		}
		
		Widget container;
		void HandleViewTextEditorhandleSizeAllocated (object o, SizeAllocatedArgs args)
		{
			int newX = widget.TextEditor.Allocation.Width - this.Allocation.Width - 8;
			TextEditorContainer.EditorContainerChild containerChild = ((Mono.TextEditor.TextEditorContainer.EditorContainerChild)widget.TextEditorContainer[container]);
			if (newX != containerChild.X) {
				this.searchEntry.WidthRequest = widget.Allocation.Width / 3;
				containerChild.X = newX;
				widget.TextEditorContainer.QueueResize ();
			}
		}
		
		public SearchAndReplaceWidget (SourceEditorWidget widget, Widget container)
		{
			this.container = container;
			widget.TextEditorContainer.SizeAllocated += HandleViewTextEditorhandleSizeAllocated;
			widget.TextEditor.TextViewMargin.SearchRegionsUpdated += HandleWidgetTextEditorTextViewMarginSearchRegionsUpdated;
			widget.TextEditor.Caret.PositionChanged += HandleWidgetTextEditorCaretPositionChanged;
			this.SizeAllocated += HandleViewTextEditorhandleSizeAllocated;
			this.Name = "SearchAndReplaceWidget";
			this.Events = Gdk.EventMask.AllEventsMask;
			widget.DisableAutomaticSearchPatternCaseMatch = false;
			Build();
			this.buttonReplace.TooltipText = GettextCatalog.GetString ("Replace");
			this.buttonSearchForward.TooltipText = GettextCatalog.GetString ("Find next");
			this.buttonSearchBackward.TooltipText = GettextCatalog.GetString ("Find previous");
			this.buttonSearchMode.TooltipText = GettextCatalog.GetString ("Toggle between search and replace mode");
			this.searchEntry.Ready = true;
			this.searchEntry.Visible = true;
			this.searchEntry.WidthRequest = widget.Allocation.Width / 3;
			replaceWidgets = new Widget [] {
		//		labelReplace,
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
			UpdateSearchPattern ();
			
			//searchEntry.Model = searchHistory;
			
			searchEntry.Entry.KeyReleaseEvent += delegate {
				widget.CheckSearchPatternCasing (SearchPattern);
/*				widget.SetSearchPattern (SearchPattern);
				searchPattern = SearchPattern;
				UpdateSearchEntry ();*/
			};
			
			searchEntry.Entry.Changed += delegate {
				widget.SetSearchPattern (SearchPattern);
				searchPattern = SearchPattern;
				UpdateSearchEntry ();
			};
			
			RestoreSearchHistory ();
			
			entryReplace.Text = replacePattern;
//			entryReplace.Model = replaceHistory;
//			RestoreReplaceHistory ();
			
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
			
			searchEntry.Entry.Activated += delegate {
//				UpdateSearchHistory (SearchPattern);
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
			
//			optionsButton.Label = MonoDevelop.Core.GettextCatalog.GetString ("Options");
			
			this.searchEntry.IsCheckMenu = true;
			
			CheckMenuItem caseSensitive = searchEntry.AddFilterOption (0, MonoDevelop.Core.GettextCatalog.GetString ("_Case sensitive"));
			caseSensitive.Active = IsCaseSensitive;
			caseSensitive.DrawAsRadio = false;
			caseSensitive.Toggled += delegate {
				SetIsCaseSensitive (caseSensitive.Active);
				UpdateSearchEntry ();
			};
			/*
			caseSensitive.ButtonPressEvent += delegate {
				caseSensitive.Toggle ();
				widget.DisableAutomaticSearchPatternCaseMatch = true;
			};*/
			
			CheckMenuItem wholeWordsOnly = searchEntry.AddFilterOption (1, MonoDevelop.Core.GettextCatalog.GetString ("_Whole words only"));
			wholeWordsOnly.Active = IsWholeWordOnly;
			wholeWordsOnly.DrawAsRadio = false;
			wholeWordsOnly.Toggled += delegate {
				SetIsWholeWordOnly (wholeWordsOnly.Active);
				UpdateSearchEntry ();
			};
			/*wholeWordsOnly.ButtonPressEvent += delegate {
				wholeWordsOnly.Toggle ();
			};*/
				
			
			CheckMenuItem regexSearch = searchEntry.AddFilterOption (2, MonoDevelop.Core.GettextCatalog.GetString ("_Regex search"));
			regexSearch.Active = SearchEngine == RegexSearchEngine;
			regexSearch.DrawAsRadio = false;
			regexSearch.Toggled += delegate {
				SetIsRegexSearch (regexSearch.Active);
				UpdateSearchEntry ();
			};
			/*
			regexSearch.ButtonPressEvent += delegate {
				regexSearch.Toggle ();
			};*/
			
			entryReplace.Changed += delegate {
				replacePattern = ReplacePattern;
				if (!inReplaceUpdate) 
					FireReplacePatternChanged ();
			};
			
			entryReplace.Activated += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.Replace ();
				entryReplace.GrabFocus ();
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
			searchEntry.Entry.KeyPressEvent += OnNavigateKeyPressEvent;
			entryReplace.KeyPressEvent += OnNavigateKeyPressEvent;
			buttonReplace.KeyPressEvent += OnNavigateKeyPressEvent;
			
			resultInformLabelEventBox = this.searchEntry.AddLabelWidget (resultInformLabel);
			resultInformLabelEventBox.BorderWidth = 2;
			resultInformLabel.Xpad = 2;
			resultInformLabel.Show ();
			searchEntry.FilterButtonPixbuf = new Gdk.Pixbuf (typeof(SearchAndReplaceWidget).Assembly, "searchoptions.png");
		}

		void HandleWidgetTextEditorCaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			UpdateResultInformLabel ();
		}

		void HandleWidgetTextEditorTextViewMarginSearchRegionsUpdated (object sender, EventArgs e)
		{
			UpdateResultInformLabel ();
		}
		
		Gtk.Label resultInformLabel = new Gtk.Label ();
		Gtk.EventBox resultInformLabelEventBox;
		
		static Gdk.Cursor arrowCursor = new Gdk.Cursor (Gdk.CursorType.Arrow);
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = arrowCursor;
			return base.OnEnterNotifyEvent (evnt);
		}

		public void UpdateSearchPattern ()
		{
			searchEntry.Query = widget.TextEditor.SearchPattern;
			widget.SetSearchPattern (widget.TextEditor.SearchPattern);
			searchPattern = widget.TextEditor.SearchPattern;
//			UpdateSearchEntry ();
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
					searchEntry.GrabFocus ();
					break;
				case Gdk.Key.ISO_Left_Tab:
					if (this.IsReplaceMode) {
						if (o == searchEntry) {
							buttonSearchForward.GrabFocus ();
						} else if (o == entryReplace) {
							searchEntry.GrabFocus ();
						} else if (o == buttonReplace) {
							entryReplace.GrabFocus ();
						} else if (o == buttonSearchBackward) {
							buttonReplace.GrabFocus ();
						} else if (o == buttonSearchForward) {
							buttonReplace.GrabFocus ();
						}
						args.RetVal = true;
					} else {
						if (o == searchEntry) {
							buttonSearchForward.GrabFocus ();
						} else if (o == buttonSearchBackward) {
							searchEntry.GrabFocus ();
							args.RetVal = true;
						} else if (o == buttonSearchForward) {
							buttonSearchBackward.GrabFocus ();
						}
					}
					break;
				case Gdk.Key.Tab: 
					if (this.IsReplaceMode) {
						if (o == searchEntry) {
							entryReplace.GrabFocus ();
						} if (o == entryReplace) {
							buttonReplace.GrabFocus ();
						} else if (o == buttonReplace) {
							buttonSearchForward.GrabFocus ();
						} else if (o == buttonSearchBackward) {
							buttonSearchForward.GrabFocus ();
						} else if (o == buttonSearchForward) {
							widget.TextEditor.GrabFocus ();
						}
						args.RetVal = true;
					} else {
						if (o == searchEntry) {
							buttonSearchBackward.GrabFocus ();
						} else if (o == buttonSearchBackward) {
							buttonSearchForward.GrabFocus ();
						} else if (o == buttonSearchForward) {
							widget.TextEditor.GrabFocus ();
							args.RetVal = true;
						}
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
				searchButtonModeArrow.ArrowType = isReplaceMode ? ArrowType.Up : ArrowType.Down;
				table.RowSpacing = isReplaceMode ? 6u : 0u;
				foreach (Widget widget in replaceWidgets) {
					widget.Visible = isReplaceMode;
				}
			}
		}
		
		protected override void OnDestroyed ()
		{
			widget.TextEditor.Caret.PositionChanged -= HandleWidgetTextEditorCaretPositionChanged;
			widget.TextEditor.TextViewMargin.SearchRegionsUpdated -= HandleWidgetTextEditorTextViewMarginSearchRegionsUpdated;
			widget.TextEditorContainer.SizeAllocated -= HandleViewTextEditorhandleSizeAllocated;
			
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
			if (widget != null) {
				widget.TextEditor.Repaint ();
				widget = null;
			}
			base.OnDestroyed ();
		}
		
		public void Focus ()
		{
			searchEntry.GrabFocusEntry ();
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
//				int oldOffset = widget.TextEditor.Caret.Offset;
				widget.TextEditor.Caret.Offset = result.EndOffset;
				var oldRange = widget.TextEditor.SelectionRange;
				widget.TextEditor.SelectionRange = result;
				widget.TextEditor.CenterToCaret ();
				if (oldRange == null || oldRange.Offset != result.Offset)
					widget.TextEditor.AnimateSearchResult (result);
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
			IsCaseSensitive = value;
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
//			searchEntry.Entry.Text = searchPattern;
//		}
		
		string oldPattern;
		SearchResult result;
		void UpdateSearchEntry ()
		{
			if (oldPattern != SearchPattern) {
				oldPattern = SearchPattern;
				widget.SetSearchOptions ();
				result = widget.TextEditor.SearchForward (widget.TextEditor.Document.LocationToOffset (caretSave));
			}
			
			GotoResult (result);
			UpdateResultInformLabel ();
		}
		
		void UpdateResultInformLabel ()
		{
			if (string.IsNullOrEmpty (SearchPattern)) {
				resultInformLabel.Text = "";
				resultInformLabelEventBox.ModifyBg (StateType.Normal, searchEntry.Entry.Style.Base (searchEntry.Entry.State));
				resultInformLabel.ModifyFg (StateType.Normal, searchEntry.Entry.Style.Foreground (StateType.Insensitive));
				return;
			}
			
			bool error = result == null && !String.IsNullOrEmpty (SearchPattern);
			string errorMsg;
			bool valid = widget.TextEditor.SearchEngine.IsValidPattern (searchPattern, out errorMsg);
			error |= !valid;
			
			if (!valid) {
				IdeApp.Workbench.StatusBar.ShowError (errorMsg);
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			
			if (error || widget.TextEditor.TextViewMargin.SearchResultMatchCount == 0) {
				//resultInformLabel.Markup = "<span foreground=\"#000000\" background=\"" + MonoDevelop.Components.PangoCairoHelper.GetColorString (GotoLineNumberWidget.errorColor) + "\">" + GettextCatalog.GetString ("Not found") + "</span>";
				resultInformLabel.Text = GettextCatalog.GetString ("Not found");
				resultInformLabelEventBox.ModifyBg (StateType.Normal, GotoLineNumberWidget.errorColor);
				resultInformLabel.ModifyFg (StateType.Normal, searchEntry.Entry.Style.Foreground (StateType.Normal));
			} else {
				int resultIndex = 0;
				int foundIndex = -1;
				int caretOffset = widget.TextEditor.Caret.Offset;
				foreach (ISegment searchResult in widget.TextEditor.TextViewMargin.SearchResults) {
					if (searchResult.Offset <= caretOffset && caretOffset <= searchResult.EndOffset) {
						foundIndex = resultIndex + 1;
						break;
					}
					resultIndex++;
				}
				if (foundIndex != -1) {
					resultInformLabel.Text = String.Format (GettextCatalog.GetString ("{0} of {1}"), foundIndex, widget.TextEditor.TextViewMargin.SearchResultMatchCount);
				} else {
					resultInformLabel.Text = String.Format (GettextCatalog.GetPluralString ("{0} match", "{0} matches", widget.TextEditor.TextViewMargin.SearchResultMatchCount), widget.TextEditor.TextViewMargin.SearchResultMatchCount);
				}
				resultInformLabelEventBox.ModifyBg (StateType.Normal, searchEntry.Entry.Style.Base (searchEntry.Entry.State));
				resultInformLabel.ModifyFg (StateType.Normal, searchEntry.Entry.Style.Foreground (StateType.Insensitive));
			}
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
			entryReplace.Text = replacePattern;
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
