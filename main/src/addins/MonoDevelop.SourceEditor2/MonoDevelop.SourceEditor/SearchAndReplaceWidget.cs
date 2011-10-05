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
		Mono.TextEditor.TextEditorContainer textEditorContainer;
		MonoDevelop.SourceEditor.ExtensibleTextEditor textEditor;
		
		bool isReplaceMode = true;
		Widget [] replaceWidgets;
		
		public bool IsCaseSensitive {
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
			set { entryReplace.Text = value; }
		}
		
		public string SearchPattern {
			get { return searchEntry.Entry.Text; }
			set { searchEntry.Entry.Text = value; }
		}
		
		public bool SearchFocused {
			get {
				return searchEntry.HasFocus;
			}
		}
		
		Widget container;
		void HandleViewTextEditorhandleSizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (widget == null || textEditor == null)
				return;
			int newX = textEditor.Allocation.Width - this.Allocation.Width - 8;
			TextEditorContainer.EditorContainerChild containerChild = ((Mono.TextEditor.TextEditorContainer.EditorContainerChild)textEditorContainer[container]);
			if (newX != containerChild.X) {
				this.searchEntry.WidthRequest = widget.Vbox.Allocation.Width / 3;
				containerChild.X = newX;
				textEditorContainer.QueueResize ();
			}
		}
		
		public SearchAndReplaceWidget (SourceEditorWidget widget, Widget container)
		{
			this.container = container;
			this.textEditor = widget.TextEditor;
			this.textEditorContainer = widget.TextEditorContainer;
			
			textEditorContainer.SizeAllocated += HandleViewTextEditorhandleSizeAllocated;
			textEditor.TextViewMargin.SearchRegionsUpdated += HandleWidgetTextEditorTextViewMarginSearchRegionsUpdated;
			textEditor.Caret.PositionChanged += HandleWidgetTextEditorCaretPositionChanged;
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
			this.searchEntry.WidthRequest = widget.Vbox.Allocation.Width / 3;
			this.searchEntry.ForceFilterButtonVisible = true;
			replaceWidgets = new Widget [] {
		//		labelReplace,
				entryReplace,
				buttonReplace,
				buttonReplaceAll
			};
			
			this.FocusChain = new Widget [] {
				this.searchEntry,
				this.buttonSearchForward,
				this.buttonSearchBackward,
				entryReplace,
				buttonReplace,
				buttonReplaceAll
			};
			
			this.widget = widget;
			FilterHistory (seachHistoryProperty);
			FilterHistory (replaceHistoryProperty);
			//HACK: GTK rendering issue on Mac, images don't repaint unless we put them in visible eventboxes
			if (Platform.IsMac) {
				foreach (var eb in new [] { eventbox2, eventbox3, eventbox4, eventbox5, eventbox6 }) {
					eb.VisibleWindow = true;
					eb.ModifyBg (StateType.Normal, new Gdk.Color (230, 230, 230));
				}
			}

			if (String.IsNullOrEmpty (textEditor.SearchPattern)) {
				textEditor.SearchPattern = searchPattern;
			} else if (textEditor.SearchPattern != searchPattern) {
				searchPattern = textEditor.SearchPattern;
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
				string oldPattern = searchPattern;
				searchPattern = SearchPattern;
				if (oldPattern != searchPattern)
					UpdateSearchEntry ();
				var history = GetHistory (seachHistoryProperty);
				if (history.Count > 0 && history[0] == oldPattern) {
					ChangeHistory (seachHistoryProperty, searchPattern);
				} else {
					UpdateSearchHistory (searchPattern);
				}
			};
			
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
				UpdateSearchHistory (SearchPattern);
				widget.SetLastActiveEditor (textEditor);
				widget.FindNext (false);
			};
			
			buttonSearchForward.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				widget.SetLastActiveEditor (textEditor);
				widget.FindNext (false);
			};
			
			buttonSearchBackward.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				widget.SetLastActiveEditor (textEditor);
				widget.FindPrevious (false);
			};
			
//			optionsButton.Label = MonoDevelop.Core.GettextCatalog.GetString ("Options");
			
			this.searchEntry.RequestMenu += HandleSearchEntryhandleRequestMenu;
			
			entryReplace.Changed += delegate {
				replacePattern = ReplacePattern;
				if (!inReplaceUpdate) 
					FireReplacePatternChanged ();
			};
			
			entryReplace.Activated += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.SetLastActiveEditor (textEditor);
				widget.Replace ();
				entryReplace.GrabFocus ();
			};
			
			buttonReplace.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.SetLastActiveEditor (textEditor);
				widget.Replace ();
			};
			
			buttonReplaceAll.Clicked += delegate {
				UpdateSearchHistory (SearchPattern);
				UpdateReplaceHistory (ReplacePattern);
				widget.SetLastActiveEditor (textEditor);
				widget.ReplaceAll ();
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
			searchEntry.FilterButtonPixbuf = new Gdk.Pixbuf (typeof(SearchAndReplaceWidget).Assembly, "searchoptions.png");
		}

		void HandleSearchEntryhandleRequestMenu (object sender, EventArgs e)
		{
			if (searchEntry.Menu != null)
				searchEntry.Menu.Destroy ();
			
			searchEntry.Menu = new Menu ();
			
			CheckMenuItem caseSensitive = new CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Case sensitive"));
			caseSensitive.Active = IsCaseSensitive;
			caseSensitive.DrawAsRadio = false;
			caseSensitive.Toggled += delegate {
				SetIsCaseSensitive (caseSensitive.Active);
				UpdateSearchEntry ();
			};
			searchEntry.Menu.Add (caseSensitive);
			
			CheckMenuItem wholeWordsOnly = new CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Whole words only"));
			wholeWordsOnly.Active = IsWholeWordOnly;
			wholeWordsOnly.DrawAsRadio = false;
			wholeWordsOnly.Toggled += delegate {
				SetIsWholeWordOnly (wholeWordsOnly.Active);
				UpdateSearchEntry ();
			};
			searchEntry.Menu.Add (wholeWordsOnly);
				
			
			CheckMenuItem regexSearch = new CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Regex search"));
			regexSearch.Active = SearchEngine == RegexSearchEngine;
			regexSearch.DrawAsRadio = false;
			regexSearch.Toggled += delegate {
				SetIsRegexSearch (regexSearch.Active);
				UpdateSearchEntry ();
			};
			searchEntry.Menu.Add (regexSearch);
			List<string> history = GetHistory (seachHistoryProperty);
			if (history.Count > 0) {
				searchEntry.Menu.Add (new SeparatorMenuItem ());
				MenuItem recentSearches = new MenuItem (MonoDevelop.Core.GettextCatalog.GetString ("Recent Searches"));
				recentSearches.Sensitive = false;
				searchEntry.Menu.Add (recentSearches);
				
				foreach (string item in history) {
					if (item == searchEntry.Entry.Text)
						continue;
					MenuItem recentItem = new MenuItem (item);
					recentItem.Name = item;
					recentItem.Activated += delegate (object mySender, EventArgs myE) {
						MenuItem cur = (MenuItem)mySender;
						searchPattern = ""; // force that the current pattern is stored in history and not replaced
						searchEntry.Entry.Text = cur.Name;
						FilterHistory (seachHistoryProperty);
					};
					searchEntry.Menu.Add (recentItem);
				}
				searchEntry.Menu.Add (new SeparatorMenuItem ());
				MenuItem clearRecentSearches = new MenuItem (MonoDevelop.Core.GettextCatalog.GetString ("Clear Recent Searches"));
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
		
		static Gdk.Cursor arrowCursor = new Gdk.Cursor (Gdk.CursorType.Arrow);
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = arrowCursor;
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
			searchEntry.Entry.Text = textEditor.SearchPattern;
			widget.SetSearchPattern (textEditor.SearchPattern);
			searchPattern = textEditor.SearchPattern;
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
								
								searchEntry.Entry.Text = history[curSearchResult];
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
					widget.RemoveSearchWidget ();
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
		protected override void OnFocusChildSet (Widget widget)
		{
			base.OnFocusChildSet (widget);
			ISegment mainResult = this.textEditor.TextViewMargin.MainSearchResult;
			this.textEditor.TextViewMargin.HideSelection = widget == table && mainResult != null &&
				this.textEditor.IsSomethingSelected && this.textEditor.SelectionRange.Offset == mainResult.Offset && this.textEditor.SelectionRange.EndOffset == mainResult.EndOffset;
			
			if (this.textEditor.TextViewMargin.HideSelection)
				this.textEditor.QueueDraw ();
		}
		
		protected override void OnDestroyed ()
		{
			this.textEditor.TextViewMargin.HideSelection = false;
			textEditor.Caret.PositionChanged -= HandleWidgetTextEditorCaretPositionChanged;
			textEditor.TextViewMargin.SearchRegionsUpdated -= HandleWidgetTextEditorTextViewMarginSearchRegionsUpdated;
			this.SizeAllocated -= HandleViewTextEditorhandleSizeAllocated;
			textEditorContainer.SizeAllocated -= HandleViewTextEditorhandleSizeAllocated;
			
			// SearchPatternChanged -= UpdateSearchPattern;
			ReplacePatternChanged -= UpdateReplacePattern;
			
			if (widget != null) {
				textEditor.QueueDraw ();
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
			// vSave  = textEditor.VAdjustment.Value;
			// hSave  = textEditor.HAdjustment.Value;
			caretSave =  textEditor.Caret.Location;
		}
		
		void GotoResult (SearchResult result)
		{
			try {
				if (result == null) {
					textEditor.ClearSelection ();
					return;
				}
				textEditor.StopSearchResultAnimation ();
				textEditor.Caret.Offset = result.EndOffset;
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
				result = textEditor.SearchForward (textEditor.Document.LocationToOffset (caretSave));
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
			
		//	bool error = result == null && !String.IsNullOrEmpty (SearchPattern);
			string errorMsg;
			bool valid = textEditor.SearchEngine.IsValidPattern (searchPattern, out errorMsg);
		//	error |= !valid;
			
			if (!valid) {
				IdeApp.Workbench.StatusBar.ShowError (errorMsg);
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			
			if (!valid || textEditor.TextViewMargin.SearchResultMatchCount == 0) {
				//resultInformLabel.Markup = "<span foreground=\"#000000\" background=\"" + MonoDevelop.Components.PangoCairoHelper.GetColorString (GotoLineNumberWidget.errorColor) + "\">" + GettextCatalog.GetString ("Not found") + "</span>";
				resultInformLabel.Text = GettextCatalog.GetString ("Not found");
				resultInformLabelEventBox.ModifyBg (StateType.Normal, GotoLineNumberWidget.errorColor);
				resultInformLabel.ModifyFg (StateType.Normal, searchEntry.Entry.Style.Foreground (StateType.Normal));
			} else {
				int resultIndex = 0;
				int foundIndex = -1;
				int caretOffset = textEditor.Caret.Offset;
				ISegment foundSegment = null;
				foreach (ISegment searchResult in textEditor.TextViewMargin.SearchResults) {
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
				resultInformLabelEventBox.ModifyBg (StateType.Normal, searchEntry.Entry.Style.Base (searchEntry.Entry.State));
				resultInformLabel.ModifyFg (StateType.Normal, searchEntry.Entry.Style.Foreground (StateType.Insensitive));
				textEditor.TextViewMargin.HideSelection = FocusChild == this.table;
				textEditor.TextViewMargin.MainSearchResult = foundSegment;
			}
		} 
		
		void UpdateReplaceHistory (string item)
		{
			UpdateHistory (replaceHistoryProperty, item);
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
