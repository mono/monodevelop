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
using System.Linq;
using System.Collections.Generic;
using Gtk;
using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor
{
	partial class SearchAndReplaceWidget : Bin
	{
		const char historySeparator = '\n';
		const int  historyLimit = 20;
		const string seachHistoryProperty = "MonoDevelop.FindReplaceDialogs.FindHistory";
		const string replaceHistoryProperty = "MonoDevelop.FindReplaceDialogs.ReplaceHistory";
		static Xwt.Drawing.Image SearchEntryFilterImage = Xwt.Drawing.Image.FromResource ("find-options-22x32.png");
		public ISegment SelectionSegment {
			get;
			set;
		}
		bool isInSelectionSearchMode;
		TextSegmentMarker selectionMarker;

		public bool IsInSelectionSearchMode {
			get {
				return isInSelectionSearchMode;
			}

			set {
				if (value) {
					selectionMarker = new SearchInSelectionMarker (SelectionSegment);
					this.textEditor.Document.AddMarker (selectionMarker);
				} else {
					RemoveSelectionMarker ();
				}
				isInSelectionSearchMode = value;
			}
		}

		void RemoveSelectionMarker ()
		{
			if (selectionMarker == null)
				return;
			this.textEditor.Document.RemoveMarker (selectionMarker);
			selectionMarker = null;
		}

		readonly MonoTextEditor textEditor;
		readonly Widget frame;
		bool isReplaceMode = true;
		Widget[] replaceWidgets;
		
		public bool IsCaseSensitive {
			get { return PropertyService.Get ("IsCaseSensitive", false); }
			set { 
				if (IsCaseSensitive != value)
					PropertyService.Set ("IsCaseSensitive", value); 
			}
		}
		
		public static bool IsWholeWordOnly {
			get { return PropertyService.Get ("IsWholeWordOnly", false); }
		}
		
		public const string DefaultSearchEngine = "default";
		public const string RegexSearchEngine = "regex";

		public static string SearchEngine {
			get {
				return PropertyService.Get ("BufferSearchEngine", DefaultSearchEngine);
			}
		}
		
		public string ReplacePattern {
			get { return entryReplace.Text; }
			set { entryReplace.Text = value ?? ""; }
		}
		
		public string SearchPattern {
			get { return searchEntry.Entry.Text; }
			set { searchEntry.Entry.Text = value ?? ""; }
		}
		
		public bool SearchFocused {
			get {
				return searchEntry.HasFocus;
			}
		}
		
		void HandleViewTextEditorhandleSizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (frame == null || textEditor == null)
				return;
			int newX = textEditor.Allocation.Width - Allocation.Width - 8;
			MonoTextEditor.EditorContainerChild containerChild = ((MonoTextEditor.EditorContainerChild)textEditor [frame]);
			if (newX != containerChild.X) {
				searchEntry.WidthRequest = textEditor.Allocation.Width / 3;
				containerChild.X = newX;
				textEditor.QueueResize ();
			}
		}

		static string GetShortcut (object commandId)
		{
			var key = IdeApp.CommandService.GetCommand (commandId).AccelKey;
			if (string.IsNullOrEmpty (key))
				return "";
			var nextShortcut = KeyBindingManager.BindingToDisplayLabel (key, false);
			return "(" + nextShortcut + ")";
		}
		
		internal SearchAndReplaceWidget (MonoTextEditor textEditor, Widget frame)
		{
			if (textEditor == null)
				throw new ArgumentNullException ("textEditor");
			this.textEditor = textEditor;
			this.frame = frame;
			textEditor.SizeAllocated += HandleViewTextEditorhandleSizeAllocated;
			textEditor.TextViewMargin.SearchRegionsUpdated += HandleWidgetTextEditorTextViewMarginSearchRegionsUpdated;
			textEditor.Caret.PositionChanged += HandleWidgetTextEditorCaretPositionChanged;
			SizeAllocated += HandleViewTextEditorhandleSizeAllocated;
			Name = "SearchAndReplaceWidget";
			Events = Gdk.EventMask.AllEventsMask;
			DisableAutomaticSearchPatternCaseMatch = false;
			Build ();
			buttonReplace.TooltipText = GettextCatalog.GetString ("Replace");
			buttonSearchForward.TooltipText = GettextCatalog.GetString ("Find next {0}", GetShortcut (SearchCommands.FindNext));
			buttonSearchBackward.TooltipText = GettextCatalog.GetString ("Find previous {0}", GetShortcut (SearchCommands.FindPrevious));
			buttonSearchMode.TooltipText = GettextCatalog.GetString ("Toggle between search and replace mode");
			searchEntry.Ready = true;
			searchEntry.Visible = true;
			searchEntry.WidthRequest = textEditor.Allocation.Width / 3;
			searchEntry.ForceFilterButtonVisible = true;
			replaceWidgets = new Widget [] {
			//		labelReplace,
				entryReplace,
				buttonReplace,
				buttonReplaceAll
			};
			
			FocusChain = new Widget [] {
				searchEntry,
				buttonSearchForward,
				buttonSearchBackward,
				entryReplace,
				buttonReplace,
				buttonReplaceAll
			};
			FilterHistory (seachHistoryProperty);
			FilterHistory (replaceHistoryProperty);

			if (String.IsNullOrEmpty (textEditor.SearchPattern)) {
				textEditor.SearchPattern = SearchAndReplaceOptions.SearchPattern;
			} else if (textEditor.SearchPattern != SearchAndReplaceOptions.SearchPattern) {
				SearchAndReplaceOptions.SearchPattern = textEditor.SearchPattern;
				//FireSearchPatternChanged ();
			}
			UpdateSearchPattern ();
			SetSearchOptions ();

			//searchEntry.Model = searchHistory;
			
			searchEntry.Entry.KeyReleaseEvent += delegate {
				CheckSearchPatternCasing (SearchPattern);
/*				widget.SetSearchPattern (SearchPattern);
				searchPattern = SearchPattern;
				UpdateSearchEntry ();*/
			};
			
			searchEntry.Entry.Changed += delegate {
				SetSearchPattern (SearchPattern);
				string oldPattern = SearchAndReplaceOptions.SearchPattern;
				SearchAndReplaceOptions.SearchPattern = SearchPattern;
				if (oldPattern != SearchAndReplaceOptions.SearchPattern)
					UpdateSearchEntry ();
				var history = GetHistory (seachHistoryProperty);

				// Don't do anything to the history if we have a blank search
				if (string.IsNullOrWhiteSpace (SearchPattern)) {
					return;
				}

				if (history.Count > 0 && history [0] == oldPattern) {
					// Only update the current history item if we're adding to the search string
					if (!oldPattern.StartsWith (SearchPattern)) {
						ChangeHistory (seachHistoryProperty, SearchAndReplaceOptions.SearchPattern);
					}
				} else {
					UpdateSearchHistory (SearchAndReplaceOptions.SearchPattern);
				}
			};
			
			entryReplace.Text = SearchAndReplaceOptions.ReplacePattern ?? "";
//			entryReplace.Model = replaceHistory;
//			RestoreReplaceHistory ();
			
			foreach (Gtk.Widget child in Children) {
				child.KeyPressEvent += delegate (object sender, Gtk.KeyPressEventArgs args) {
					if (args.Event.Key == Gdk.Key.Escape)
						RemoveSearchWidget ();
				};
			}
			
			closeButton.Clicked += delegate {
				RemoveSearchWidget ();
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
				UpdateSearchHistory (SearchPattern);
				FindNext (textEditor);
			};
			
			buttonSearchForward.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				FindNext (textEditor);
			};
			
			buttonSearchBackward.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				FindPrevious (textEditor);
			};
			
//			optionsButton.Label = MonoDevelop.Core.GettextCatalog.GetString ("Options");
			
			this.searchEntry.RequestMenu += HandleSearchEntryhandleRequestMenu;
			
			entryReplace.Changed += delegate {
				SearchAndReplaceOptions.ReplacePattern = ReplacePattern;
				if (!inReplaceUpdate) 
					FireReplacePatternChanged ();
			};
			
			entryReplace.Activated += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				Replace ();
				entryReplace.GrabFocus ();
			};
			
			buttonReplace.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				Replace ();
			};
			
			buttonReplaceAll.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				ReplaceAll ();
			};
			
			buttonSearchForward.KeyPressEvent += OnNavigateKeyPressEvent;
			buttonSearchBackward.KeyPressEvent += OnNavigateKeyPressEvent;
			searchEntry.Entry.KeyPressEvent += OnNavigateKeyPressEvent;
			entryReplace.KeyPressEvent += OnNavigateKeyPressEvent;
			buttonReplace.KeyPressEvent += OnNavigateKeyPressEvent;
			buttonReplaceAll.KeyPressEvent += OnNavigateKeyPressEvent;
			
			resultInformLabelEventBox = this.searchEntry.AddLabelWidget (resultInformLabel);
			resultInformLabelEventBox.BorderWidth = 2;
			resultInformLabel.Xpad = 2;
			resultInformLabel.Show ();
			searchEntry.FilterButtonPixbuf = SearchEntryFilterImage;

			if (textEditor.IsSomethingSelected) {
				if (textEditor.MainSelection.MinLine == textEditor.MainSelection.MaxLine || ClipboardContainsSelection()) {
					SetSearchPattern ();
				} else {
					SelectionSegment = textEditor.SelectionRange;
					IsInSelectionSearchMode = true;
					SetSearchOptions ();
				}
			}
			SetSearchPattern (SearchAndReplaceOptions.SearchPattern);
			textEditor.HighlightSearchPattern = true;
			textEditor.TextViewMargin.RefreshSearchMarker ();
			if (textEditor.Document.IsReadOnly) {
				buttonSearchMode.Visible = false;
				IsReplaceMode = false;
			}

			SearchAndReplaceOptions.SearchPatternChanged += HandleSearchPatternChanged;
			SearchAndReplaceOptions.ReplacePatternChanged += HandleReplacePatternChanged;
		}

		bool ClipboardContainsSelection ()
		{
			return textEditor.SelectedText == ClipboardActions.GetClipboardContent ();
		}

		void HandleReplacePatternChanged (object sender, EventArgs e)
		{
			ReplacePattern = SearchAndReplaceOptions.ReplacePattern;
		}

		void HandleSearchPatternChanged (object sender, EventArgs e)
		{
			SearchPattern = SearchAndReplaceOptions.SearchPattern;
		}

		public bool DisableAutomaticSearchPatternCaseMatch {
			get;
			set;
		}
		
		internal void CheckSearchPatternCasing (string searchPattern)
		{
			if (!DisableAutomaticSearchPatternCaseMatch && PropertyService.Get ("AutoSetPatternCasing", true)) {
				IsCaseSensitive = searchPattern.Any (ch => Char.IsUpper (ch));
				SetSearchOptions ();
			}
		}
		
		void SetSearchOptions ()
		{
			if (SearchAndReplaceWidget.SearchEngine == SearchAndReplaceWidget.DefaultSearchEngine) {
				if (!(textEditor.SearchEngine is BasicSearchEngine))
					textEditor.SearchEngine = new BasicSearchEngine ();
			} else {
				if (!(textEditor.SearchEngine is RegexSearchEngine))
					textEditor.SearchEngine = new RegexSearchEngine ();
			}
			textEditor.IsCaseSensitive = IsCaseSensitive;
			textEditor.IsWholeWordOnly = SearchAndReplaceWidget.IsWholeWordOnly;
			textEditor.SearchRegion = IsInSelectionSearchMode ? SelectionSegment : TextSegment.Invalid;
			string error;
			string pattern = SearchPattern;
			
			bool valid = textEditor.SearchEngine.IsValidPattern (pattern, out error);
			
			if (valid) {
				textEditor.SearchPattern = pattern;
			}
			textEditor.QueueDraw ();
		}

		void HandleSearchEntryhandleRequestMenu (object sender, EventArgs e)
		{
			if (searchEntry.Menu != null)
				searchEntry.Menu.Destroy ();
			
			searchEntry.Menu = new Menu ();
			
			CheckMenuItem caseSensitive = new CheckMenuItem (GettextCatalog.GetString ("_Case sensitive"));
			caseSensitive.Active = IsCaseSensitive;
			caseSensitive.DrawAsRadio = false;
			caseSensitive.Toggled += delegate {
				SetIsCaseSensitive (caseSensitive.Active);
				UpdateSearchEntry ();
			};
			searchEntry.Menu.Add (caseSensitive);
			
			CheckMenuItem wholeWordsOnly = new CheckMenuItem (GettextCatalog.GetString ("_Whole words only"));
			wholeWordsOnly.Active = IsWholeWordOnly;
			wholeWordsOnly.DrawAsRadio = false;
			wholeWordsOnly.Toggled += delegate {
				SetIsWholeWordOnly (wholeWordsOnly.Active);
				UpdateSearchEntry ();
			};
			searchEntry.Menu.Add (wholeWordsOnly);
				
			
			CheckMenuItem regexSearch = new CheckMenuItem (GettextCatalog.GetString ("_Regex search"));
			regexSearch.Active = SearchEngine == RegexSearchEngine;
			regexSearch.DrawAsRadio = false;
			regexSearch.Toggled += delegate {
				SetIsRegexSearch (regexSearch.Active);
				UpdateSearchEntry ();
			};
			searchEntry.Menu.Add (regexSearch);
			
			CheckMenuItem inselectionSearch = new CheckMenuItem (GettextCatalog.GetString ("_Search In Selection"));
			inselectionSearch.Active = IsInSelectionSearchMode;
			inselectionSearch.DrawAsRadio = false;
			inselectionSearch.Toggled += delegate {
				IsInSelectionSearchMode = inselectionSearch.Active;
				UpdateSearchEntry ();
			};
			searchEntry.Menu.Add (inselectionSearch);

			List<string> history = GetHistory (seachHistoryProperty);
			if (history.Count > 0) {
				searchEntry.Menu.Add (new SeparatorMenuItem ());
				MenuItem recentSearches = new MenuItem (GettextCatalog.GetString ("Recent Searches"));
				recentSearches.Sensitive = false;
				searchEntry.Menu.Add (recentSearches);
				
				foreach (string item in history) {
					if (item == searchEntry.Entry.Text)
						continue;
					MenuItem recentItem = new MenuItem (item);
					recentItem.Name = item;
					recentItem.Activated += delegate (object mySender, EventArgs myE) {
						MenuItem cur = (MenuItem)mySender;
						SearchAndReplaceOptions.SearchPattern = ""; // force that the current pattern is stored in history and not replaced
						searchEntry.Entry.Text = cur.Name;
						FilterHistory (seachHistoryProperty);
					};
					searchEntry.Menu.Add (recentItem);
				}
				searchEntry.Menu.Add (new SeparatorMenuItem ());
				MenuItem clearRecentSearches = new MenuItem (GettextCatalog.GetString ("Clear Recent Searches"));
				clearRecentSearches.Activated += delegate {
					StoreHistory (seachHistoryProperty, null);
				};
				searchEntry.Menu.Add (clearRecentSearches);
			}
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

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = null;
			return base.OnEnterNotifyEvent (evnt);
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		public void SelectAllCommand ()
		{
			if (searchEntry.HasFocus) {
				searchEntry.Entry.SelectRegion (0, searchEntry.Entry.Text.Length);
			} else if (IsReplaceMode && entryReplace.HasFocus) {
				entryReplace.SelectRegion (0, entryReplace.Text.Length);
			}
		}
		
		public void UpdateSearchPattern ()
		{
			searchEntry.Entry.Text = textEditor.SearchPattern ?? "";
			SetSearchPattern (textEditor.SearchPattern);
			SearchAndReplaceOptions.SearchPattern = textEditor.SearchPattern;
//			UpdateSearchEntry ();
		}

		int curSearchResult = -1;
		string curSearchPattern = null;

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
				if (o == searchEntry.Entry && (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					UpdateSearchHistory (SearchPattern);
					FindPrevious (textEditor);
				}
				break;
			case Gdk.Key.Down:
			case Gdk.Key.Up:
				if (o != searchEntry.Entry) {
					args.RetVal = true;
					return;
				}
				if ((args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask && o == searchEntry.Entry) {
					searchEntry.PopupFilterMenu ();
				} else {
					if (curSearchResult == -1)
						curSearchPattern = searchEntry.Entry.Text;
						
					List<string> history = GetHistory (seachHistoryProperty);
					if (history.Count > 0) {
						curSearchResult += args.Event.Key == Gdk.Key.Up ? -1 : 1;
						if (curSearchResult >= history.Count)
							curSearchResult = -1;
						if (curSearchResult == -1) {
							searchEntry.Entry.Text = curSearchPattern;
						} else {
							if (curSearchResult < -1)
								curSearchResult = history.Count - 1;
								
							searchEntry.Entry.Text = history [curSearchResult];
						}
						searchEntry.Entry.Position = -1;
					}
				}
				args.RetVal = true;
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
				RemoveSearchWidget ();
				break;
			case Gdk.Key.slash:
				searchEntry.GrabFocus ();
				break;
			case Gdk.Key.ISO_Left_Tab:
				if (this.IsReplaceMode) {
					if (o == entryReplace) {
						searchEntry.Entry.GrabFocus ();
					} else if (o == buttonReplace) {
						entryReplace.GrabFocus ();
					} else if (o == buttonReplaceAll) {
						buttonReplace.GrabFocus ();
					} else if (o == buttonSearchBackward) {
						buttonReplaceAll.GrabFocus ();
					} else if (o == buttonSearchForward) {
						buttonSearchBackward.GrabFocus ();
					} else {
						buttonSearchForward.GrabFocus ();
					}
					args.RetVal = true;
				} else {
					if (o == buttonSearchBackward) {
						searchEntry.Entry.GrabFocus ();
					} else if (o == buttonSearchForward) {
						buttonSearchBackward.GrabFocus ();
					} else {
						buttonSearchForward.GrabFocus ();
					}
					args.RetVal = true;
				}
				break;
			case Gdk.Key.Tab: 
				if (this.IsReplaceMode) {
					if (o == entryReplace) {
						buttonReplace.GrabFocus ();
					} else if (o == buttonReplace) {
						buttonReplaceAll.GrabFocus ();
					} else if (o == buttonReplaceAll) {
						buttonSearchBackward.GrabFocus ();
					} else if (o == buttonSearchBackward) {
						buttonSearchForward.GrabFocus ();
					} else if (o == buttonSearchForward) {
//							textEditor.GrabFocus ();
						searchEntry.Entry.GrabFocus ();
					} else {
						entryReplace.GrabFocus ();
					}
					args.RetVal = true;
				} else {
					if (o == buttonSearchBackward) {
						buttonSearchForward.GrabFocus ();
					} else if (o == buttonSearchForward) {
						searchEntry.Entry.GrabFocus ();
//							textEditor.GrabFocus ();
					} else {
						buttonSearchBackward.GrabFocus ();
					}
					args.RetVal = true;
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
				isReplaceMode = value;
				searchButtonModeArrow.ArrowType = isReplaceMode ? ArrowType.Up : ArrowType.Down;
				table.RowSpacing = isReplaceMode ? 6u : 0u;
				foreach (Widget widget in replaceWidgets) {
					widget.Visible = isReplaceMode;
				}
				if (textEditor.Document.IsReadOnly)
					buttonSearchMode.Visible = false;
			}
		}
		
		protected override void OnDestroyed ()
		{
			RemoveSelectionMarker ();
			SearchAndReplaceOptions.SearchPatternChanged -= HandleSearchPatternChanged;
			SearchAndReplaceOptions.ReplacePatternChanged -= HandleReplacePatternChanged;

			textEditor.Caret.PositionChanged -= HandleWidgetTextEditorCaretPositionChanged;
			textEditor.TextViewMargin.SearchRegionsUpdated -= HandleWidgetTextEditorTextViewMarginSearchRegionsUpdated;
			SizeAllocated -= HandleViewTextEditorhandleSizeAllocated;
			textEditor.SizeAllocated -= HandleViewTextEditorhandleSizeAllocated;
			
			// SearchPatternChanged -= UpdateSearchPattern;
			ReplacePatternChanged -= UpdateReplacePattern;
			
			if (frame != null) {
				textEditor.QueueDraw ();
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
			// vSave  = textEditor.VAdjustment.Value;
			// hSave  = textEditor.HAdjustment.Value;
			caretSave = textEditor.Caret.Location;
		}
		
		void GotoResult (SearchResult result)
		{
			try {
				if (result == null) {
					textEditor.ClearSelection ();
					return;
				}
				textEditor.StopSearchResultAnimation ();
				textEditor.Caret.Location = textEditor.OffsetToLocation (result.EndOffset);
				textEditor.SetSelection (result.Offset, result.EndOffset);
				textEditor.CenterToCaret ();
				textEditor.AnimateSearchResult (result);
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
		
		static void StoreHistory (string propertyKey, List<string> history)
		{
			PropertyService.Set (propertyKey, history != null ? String.Join (historySeparator.ToString (), history.ToArray ()) : null);
		}
		
		static void UpdateHistory (string propertyKey, string item)
		{
			List<string> history = GetHistory (propertyKey);
			history.Remove (item);
			history.Insert (0, item);
			while (history.Count >= historyLimit) 
				history.RemoveAt (historyLimit - 1);
			
			StoreHistory (propertyKey, history);
		}
		
		static void FilterHistory (string propertyKey)
		{
			List<string> history = GetHistory (propertyKey);
			List<string> newHistory = new List<string> ();
			HashSet<string> filter = new HashSet<string> ();
			foreach (string str in history) {
				if (filter.Contains (str))
					continue;
				filter.Add (str);
				newHistory.Add (str);
			}
			StoreHistory (propertyKey, newHistory);
		}
		
		internal static void UpdateSearchHistory (string item)
		{
			UpdateHistory (seachHistoryProperty, item);
		}
		
		static void ChangeHistory (string propertyKey, string item)
		{
			List<string> history = GetHistory (propertyKey);
			history.RemoveAt (0);
			history.Insert (0, item);
			StoreHistory (propertyKey, history);
		}
		
		void SetIsCaseSensitive (bool value)
		{
			IsCaseSensitive = value;
			SetSearchOptions ();
		}
		
		void SetIsWholeWordOnly (bool value)
		{
			PropertyService.Set ("IsWholeWordOnly", value);
			SetSearchOptions ();
		}
		
		void SetIsRegexSearch (bool value)
		{
			PropertyService.Set ("BufferSearchEngine", value ? RegexSearchEngine : 
			                                                   DefaultSearchEngine);
			SetSearchOptions ();
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
				SetSearchOptions ();
				result = textEditor.SearchForward (textEditor.Document.LocationToOffset (caretSave));
			}
			
			GotoResult (result);
			UpdateResultInformLabel ();
		}
		
		void UpdateResultInformLabel ()
		{
			try {
				var entry = searchEntry.Entry;
				if (entry == null) {
					LoggingService.LogError ("SearchAndReplaceWidget.UpdateResultInformLabel called with null entry.");
					return;
				}
				if (string.IsNullOrEmpty (SearchPattern)) {
					resultInformLabel.Text = "";
					resultInformLabelEventBox.ModifyBg (StateType.Normal, entry.Style.Base (entry.State));
					resultInformLabel.ModifyFg (StateType.Normal, entry.Style.Foreground (StateType.Insensitive));
					return;
				}

				//	bool error = result == null && !String.IsNullOrEmpty (SearchPattern);
				string errorMsg;
				bool valid = textEditor.SearchEngine.IsValidPattern (SearchAndReplaceOptions.SearchPattern, out errorMsg);
				//	error |= !valid;

				if (!valid) {
					IdeApp.Workbench.StatusBar.ShowError (errorMsg);
				} else {
					IdeApp.Workbench.StatusBar.ShowReady ();
				}

				if (!valid || textEditor.TextViewMargin.SearchResultMatchCount == 0) {
					//resultInformLabel.Markup = "<span foreground=\"#000000\" background=\"" + MonoDevelop.Components.PangoCairoHelper.GetColorString (GotoLineNumberWidget.errorColor) + "\">" + GettextCatalog.GetString ("Not found") + "</span>";
					resultInformLabel.Text = GettextCatalog.GetString ("Not found");
					resultInformLabel.ModifyFg (StateType.Normal, Ide.Gui.Styles.Editor.SearchErrorForegroundColor.ToGdkColor ());
				} else {
					int resultIndex = 0;
					int foundIndex = -1;
					int caretOffset = textEditor.Caret.Offset;
					ISegment foundSegment = TextSegment.Invalid;
					foreach (var searchResult in textEditor.TextViewMargin.SearchResults) {
						if (searchResult.Offset <= caretOffset && caretOffset <= searchResult.EndOffset) {
							foundIndex = resultIndex + 1;
							foundSegment = searchResult;
							break;
						}
						resultIndex++;
					}
					if (foundIndex != -1) {
						resultInformLabel.Text = String.Format (GettextCatalog.GetString ("{0} of {1}"), foundIndex, textEditor.TextViewMargin.SearchResultMatchCount);
					} else {
						resultInformLabel.Text = String.Format (GettextCatalog.GetPluralString ("{0} match", "{0} matches", textEditor.TextViewMargin.SearchResultMatchCount), textEditor.TextViewMargin.SearchResultMatchCount);
					}
					resultInformLabelEventBox.ModifyBg (StateType.Normal, entry.Style.Base (entry.State));
					resultInformLabel.ModifyFg (StateType.Normal, entry.Style.Foreground (StateType.Insensitive));
					textEditor.TextViewMargin.MainSearchResult = foundSegment;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while updating result inform label.", ex);
			}
		}
		
		void UpdateReplaceHistory (string item)
		{
			UpdateHistory (replaceHistoryProperty, item);
		}
		
		void UpdateReplacePattern (object sender, EventArgs args)
		{
			entryReplace.Text = SearchAndReplaceOptions.ReplacePattern ?? "";
		}

		internal void SetSearchPattern ()
		{
			string selectedText = SourceEditorWidget.FormatPatternToSelectionOption (textEditor.SelectedText);
			
			if (!String.IsNullOrEmpty (selectedText)) {
				SetSearchPattern (selectedText);
				SearchAndReplaceOptions.SearchPattern = selectedText;
				SearchAndReplaceWidget.UpdateSearchHistory (selectedText);
				textEditor.TextViewMargin.MainSearchResult = textEditor.SelectionRange;
			}
		}
		
		public void SetSearchPattern (string searchPattern)
		{
			textEditor.SearchPattern = searchPattern;
		}

		internal static SearchResult FindNext (MonoTextEditor textEditor)
		{
			textEditor.SearchPattern = SearchAndReplaceOptions.SearchPattern;
			SearchResult result = textEditor.FindNext (true);
			if (result == null)
				return null;
			textEditor.CenterToCaret ();

			if (result.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (
					Stock.Find,
					GettextCatalog.GetString ("Reached bottom, continued from top")
				);
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			return result;
		}

		public static SearchResult FindPrevious (MonoTextEditor textEditor)
		{
			textEditor.SearchPattern = SearchAndReplaceOptions.SearchPattern;
			SearchResult result = textEditor.FindPrevious (true);
			if (result == null)
				return null;
			textEditor.CenterToCaret ();
			if (result.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (
					Stock.Find,
					GettextCatalog.GetString ("Reached top, continued from bottom")
				);
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			return result;
		}

		public void Replace ()
		{
			textEditor.Replace (ReplacePattern);
			textEditor.CenterToCaret ();
			textEditor.GrabFocus ();
		}
		
		public void ReplaceAll ()
		{
			int number = textEditor.ReplaceAll (ReplacePattern);
			if (number == 0) {
				IdeApp.Workbench.StatusBar.ShowError (GettextCatalog.GetString ("Search pattern not found"));
			} else {
				IdeApp.Workbench.StatusBar.ShowMessage (
					GettextCatalog.GetPluralString ("Found and replaced one occurrence",
					                                "Found and replaced {0} occurrences", number, number));
			}
			textEditor.GrabFocus ();
			textEditor.CenterToCaret ();
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

		void RemoveSearchWidget ()
		{
			textEditor.HighlightSearchPattern = false;
			Destroy ();
		}
	}
}
