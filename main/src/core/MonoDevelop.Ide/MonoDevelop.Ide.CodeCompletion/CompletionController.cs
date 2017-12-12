//
// CodeCompletionSession.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Gdk;
using Gtk;
using MonoDevelop.Ide.Editor.Extension;
using Xwt.Drawing;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeCompletion
{
	/// <summary>
	/// This is the controller of the code completion window.
	/// The controller takes code completion data and keystrokes as input, and shows the
	/// results in the code completion view.
	/// </summary>
	class CompletionController: IDisposable, IListDataProvider, ICompletionViewEventSink
	{
		ICompletionView view;
		ICompletionDataList dataList;

		/// <summary>
		/// A list that contains the indices of the dataList items that will be shown in the completion window,
		/// and in the order they need to be shown.
		/// </summary>
		List<int> filteredItems = new List<int> ();

		/// <summary>
		/// List of categories referenced in dataList. For each category there is a list of item indices,
		/// which are the items to be shown for each category.
		/// </summary>
		List<CategorizedCompletionItems> filteredCategories = new List<CategorizedCompletionItems> ();

		/// <summary>
		/// Text typed so far
		/// </summary>
		string completionString;

		/// <summary>
		/// Front end for the completion window
		/// </summary>
		CompletionListWindow listWindow;

		/// <summary>
		/// Completion context provided by the completion widget. Has information about the location of the caret.
		/// </summary>
		CodeCompletionContext context;

		/// <summary>
		/// The widget for which the completion window is shown
		/// </summary>
		ICompletionWidget completionWidget;

		/// <summary>
		/// A cache that keeps a list of the most recently used completion items
		/// </summary>
		internal MruCache cache = new MruCache ();

		/// <summary>
		/// If the completion data list is mutable, this contains the reference to the interface that handles mutability
		/// </summary>
		IMutableCompletionDataList mutableList;

		int initialWordLength;

		bool currentVisible;

		TooltipInformationWindow declarationViewWindow;
		CompletionData currentData;
		CancellationTokenSource declarationViewCancelSource;
		bool declarationViewHidden = true;
		uint declarationViewTimer;

		bool usingPreviewEntry;
		string previewCompletionEntryText;

		public CompletionController (CompletionListWindow listWindow, ICompletionView view)
		{
			this.listWindow = listWindow;
			this.view = view;
			AutoSelect = true;
			DefaultCompletionString = "";
			currentVisible = view.Visible;
		}

		/// <summary>
		/// Initializes the code completion session. After this method is called, the controller is ready to start
		/// keeping track of keystrokes. When ShowListWindow is called, the controller will take into account
		/// what has been typed since InitializeSession.
		/// </summary>
		public void InitializeSession (ICompletionWidget completionWidget, CodeCompletionContext completionContext)
		{
			if (completionWidget == null)
				throw new ArgumentNullException (nameof (completionWidget));
			if (completionContext == null)
				throw new ArgumentNullException (nameof (completionContext));

			view.Initialize (this, this);

			if (mutableList != null) {
				mutableList.Changing -= OnCompletionDataChanging;
				mutableList.Changed -= OnCompletionDataChanged;
				view.HideLoadingMessage();
			}
			ResetState ();
			HideDeclarationView ();

			CompletionWidget = completionWidget;
			CodeCompletionContext = completionContext;

			string text = CompletionWidget.GetCompletionText (CodeCompletionContext);
			initialWordLength = CompletionWidget.SelectedLength > 0 ? 0 : text.Length;
			StartOffset = CompletionWidget.CaretOffset - initialWordLength;
		}

		public bool ShowListWindow (ICompletionDataList list, CodeCompletionContext completionContext)
		{
			if (list == null)
				throw new ArgumentNullException (nameof (list));

			CodeCompletionContext = completionContext;
			dataList = list;
			ResetState ();

			EndOffset = CompletionWidget.CaretOffset;

			if (dataList.CompletionSelectionMode == CompletionSelectionMode.OwnTextField) {
				view.ShowPreviewCompletionEntry ();
				usingPreviewEntry = true;
				previewCompletionEntryText = "";
			}

			mutableList = dataList as IMutableCompletionDataList;
			if (mutableList != null) {
				mutableList.Changing += OnCompletionDataChanging;
				mutableList.Changed += OnCompletionDataChanged;

				if (mutableList.IsChanging)
					OnCompletionDataChanging (null, null);
			}

			// If completion data list is changing we always want to show the completion window
			// to inform the user that information is being generated

			if (dataList.Count == 0 && !IsChanging) {
				HideWindow ();
				return false;
			}

			view.Reposition (CodeCompletionContext.TriggerXCoord, CodeCompletionContext.TriggerYCoord, CodeCompletionContext.TriggerTextHeight, true);

			// Initialize the completion window behavior options
			AutoSelect = list.AutoSelect;
			AutoCompleteEmptyMatch = list.AutoCompleteEmptyMatch;
			AutoCompleteEmptyMatchOnCurlyBrace = list.AutoCompleteEmptyMatchOnCurlyBrace;
			CloseOnSquareBrackets = list.CloseOnSquareBrackets;

			DefaultCompletionString = dataList.DefaultCompletionString ?? "";

			// Makes control-space in midle of words to work
			string text = CompletionWidget.GetCompletionText (CodeCompletionContext);
			if (text.Length == 0) {
				initialWordLength = 0;
				StartOffset = completionContext.TriggerOffset;
				ResetSizes ();
				ShowWindow ();
				UpdateWordSelection ();
				UpdateDeclarationView ();

				// If there is only one matching result we take it by default
				if (dataList.AutoCompleteUniqueMatch && IsUniqueMatch && !IsChanging) {
					CompleteWord ();
					HideWindow ();
					return false;
				}
				return true;
			}

			initialWordLength = CompletionWidget.SelectedLength > 0 ? 0 : text.Length;
			StartOffset = CompletionWidget.CaretOffset - initialWordLength;
			HideWhenWordDeleted = initialWordLength != 0;

			ResetSizes ();
			UpdateWordSelection ();

			// If there is only one matching result we take it by default
			if (dataList.AutoCompleteUniqueMatch && IsUniqueMatch && !IsChanging) {
				CompleteWord ();
				HideWindow ();
				return false;
			}

			ShowWindow ();
			UpdateDeclarationView ();
			return true;
		}

		public void HideWindow ()
		{
			HideDeclarationView ();
			view.Hide ();
			ReleaseObjects ();
			NotifyVisibilityChange ();
		}

		public void ShowWindow ()
		{
			view.Show ();
			NotifyVisibilityChange ();
		}

		void NotifyVisibilityChange ()
		{
			if (currentVisible != view.Visible) {
				currentVisible = view.Visible;
				VisibleChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		void ResetState ()
		{
			usingPreviewEntry = false;
			previewCompletionEntryText = "";
			StartOffset = 0;
			HideWhenWordDeleted = false;
			SelectedItemCompletionText = null;
			ResetViewState();
		}

		void UpdateDeclarationView ()
		{
			if (dataList == null || SelectedItem == null) {
				HideDeclarationView ();
				return;
			}
			if (!view.Visible)
				return;
			RemoveDeclarationViewTimer ();
			// no selection, try to find a selection
			if (SelectedItemIndex < 0 || SelectedItemIndex >= dataList.Count) {
				CompletionString = PartialWord;
				SelectEntry (CompletionString);
			}
			// no success, hide declaration view
			if (SelectedItemIndex < 0 || SelectedItemIndex >= dataList.Count) {
				HideDeclarationView ();
				return;
			}

			var data = dataList [SelectedItemIndex];
			if (data != currentData)
				HideDeclarationView ();

			declarationViewTimer = GLib.Timeout.Add (150, DelayedTooltipShow);
		}

		void HideDeclarationView ()
		{
			if (declarationViewCancelSource != null) {
				declarationViewCancelSource.Cancel ();
				declarationViewCancelSource = null;
			}
			RemoveDeclarationViewTimer ();
			if (declarationViewWindow != null) {
				declarationViewWindow.Hide ();
			}
			declarationViewHidden = true;
		}

		void RemoveDeclarationViewTimer ()
		{
			if (declarationViewTimer != 0) {
				GLib.Source.Remove (declarationViewTimer);
				declarationViewTimer = 0;
			}
		}

		bool DelayedTooltipShow ()
		{
			DelayedTooltipShowAsync ();
			return false;
		}

		async void DelayedTooltipShowAsync ()
		{
			try {
				var selectedItem = SelectedItemIndex;
				if (selectedItem < 0 || selectedItem >= dataList.Count)
					return;

				var data = dataList [selectedItem];

				IEnumerable<CompletionData> filteredOverloads;
				if (data.HasOverloads) {
					filteredOverloads = data.OverloadedData;
				} else {
					filteredOverloads = new CompletionData [] { data };
				}

				EnsureDeclarationViewWindow ();
				if (data != currentData) {
					declarationViewWindow.Clear ();
					currentData = data;
					var cs = new CancellationTokenSource ();
					declarationViewCancelSource = cs;
					var overloads = new List<CompletionData> (filteredOverloads);
					overloads.Sort (MonoDevelop.Ide.CodeCompletion.CompletionDataList.overloadComparer);
					foreach (var overload in overloads) {
						await declarationViewWindow.AddOverload ((CompletionData)overload, cs.Token);
					}

					if (cs.IsCancellationRequested)
						return;

					if (declarationViewCancelSource == cs)
						declarationViewCancelSource = null;

					if (data.HasOverloads) {
						for (int i = 0; i < overloads.Count; i++) {
							if (!overloads [i].DisplayFlags.HasFlag (DisplayFlags.Obsolete)) {
								declarationViewWindow.CurrentOverload = i;
								break;
							}
						}
					}
				}

				if (declarationViewWindow.Overloads == 0) {
					HideDeclarationView ();
					return;
				}

				if (declarationViewHidden && view.Visible) {
					RepositionDeclarationViewWindow ();
				}

				declarationViewTimer = 0;
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
		}

		void EnsureDeclarationViewWindow ()
		{
			if (declarationViewWindow == null) {
				declarationViewWindow = new TooltipInformationWindow ();
				declarationViewWindow.LabelMaxWidth = 380;
			} else {
				declarationViewWindow.SetDefaultScheme ();
			}
			declarationViewWindow.Theme.SetBackgroundColor (Gui.Styles.CodeCompletion.BackgroundColor);
		}

		void RepositionDeclarationViewWindow ()
		{
			if (!view.Visible)
				return;
			EnsureDeclarationViewWindow ();
			if (declarationViewWindow.Overloads == 0)
				return;
			var selectedItem = SelectedItemIndex;
			declarationViewWindow.ShowArrow = true;

			if (view.RepositionDeclarationViewWindow (declarationViewWindow, selectedItem))
				declarationViewHidden = false;
		}

		void OnCompletionDataChanged (object sender, EventArgs e)
		{
			if (!object.ReferenceEquals (sender, mutableList))
				return;
			
			ResetSizes ();

			// Only hide the footer if it's finished changing
			if (!mutableList.IsChanging)
				view.HideLoadingMessage ();

			// Try to capture full selection state so as not to interrupt user
			// SelectedItemCompletionText is updated on every selection change
			// so doesn't depend on the current state of the list, which changed
			// immediately before this event was fired
			string lastSelection = AutoSelect ? SelectedItemCompletionText : null;

			var selectState = AutoSelect;

			// Clear the current filter state before doing anything else
			// because most other things depend on it
			ResetState ();

			// This sets List.CompletionString, which refilters it
			ResetSizes ();

			AutoSelect = selectState;

			// Try to select the last selected item
			var match = CompletionSelectionStatus.Empty;
			if (lastSelection != null)
				match = FindMatchedEntry (lastSelection);

			// If that fails, use the partial word
			if (match.Index < 0)
				match = FindMatchedEntry (PartialWord);

			SelectEntry (match);
		}

		void OnCompletionDataChanging (object sender, EventArgs e)
		{
			if (!object.ReferenceEquals (sender, mutableList))
				return;
			
			view.ShowLoadingMessage ();
		}

		public void Dispose ()
		{
			if (mutableList != null) {
				mutableList.Changing -= OnCompletionDataChanging;
				mutableList.Changed -= OnCompletionDataChanged;
				mutableList = null;
			}

			if (dataList != null) {
				if (dataList is IDisposable)
					((IDisposable)dataList).Dispose ();
				CloseCompletionList ();
				dataList = null;
			}

			HideDeclarationView ();

			if (declarationViewWindow != null) {
				declarationViewWindow.Destroy ();
				declarationViewWindow = null;
			}
			ReleaseObjects ();
			view.Destroy ();
		}

		void ReleaseObjects ()
		{
			CompletionWidget = null;
			dataList = null;
			CodeCompletionContext = null;
			currentData = null;
			Extension = null;
			view.ResetState ();
		}

		public List<int> FilteredItems {
			get {
				return filteredItems;
			}
		}

		public string CompletionString {
			get { return completionString; }
			set {
				if (completionString != value) {
					var oldCompletionString = completionString;
					completionString = value;
					FilterItems (oldCompletionString);
					UpdateViewSelectionEnabled ();
				}
			}
		}

		public int InitialWordLength {
			get { return initialWordLength; }
		}

		bool HideWhenWordDeleted {
			get; set;
		}

		public CompletionTextEditorExtension Extension {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating that shift was pressed during enter.
		/// </summary>
		/// <value>
		/// <c>true</c> if was shift pressed; otherwise, <c>false</c>.
		/// </value>
		public bool WasShiftPressed {
			get;
			private set;
		}

		/// <summary>
		/// Occurs when the selected item in completion window changes
		/// </summary>
		public event EventHandler SelectionChanged;

		void OnSelectionChanged ()
		{
			SelectedItemCompletionText = SelectedItemIndex >= 0 ? dataList [SelectedItemIndex].DisplayText : null;
			SelectionChanged?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Occurs when the selected code completion item is committed
		/// </summary>
		public event EventHandler<CodeCompletionContextEventArgs> WordCompleted;

		void OnWordCompleted (CodeCompletionContextEventArgs e)
		{
			WordCompleted?.Invoke (this, e);
			if (usingPreviewEntry)
				Dispose ();
		}

		/// <summary>
		/// Occurs when the Visible property changes
		/// </summary>
		public event EventHandler VisibleChanged;

		void ResetViewState ()
		{
			filteredItems.Clear ();
			completionString = null;
			AutoSelect = false;
			view.ResetState();
		}

		public ICompletionDataList CompletionDataList => dataList;

		public ICompletionWidget CompletionWidget {
			get {
				return completionWidget;
			}
			set {
				completionWidget = value;
			}
		}

		bool autoCompleteEmptyMatch;
		public bool AutoCompleteEmptyMatch {
			get {
				return autoCompleteEmptyMatch;
			}
			set {
				autoCompleteEmptyMatch = value;
				UpdateViewSelectionEnabled ();
			}
		}

		public bool AutoCompleteEmptyMatchOnCurlyBrace {
			get;
			set;
		}

		public bool SelectionEnabled {
			get {
				// If this condition changes, UpdateViewSelectionEnabled may require additional calls
				return AutoSelect && (AutoCompleteEmptyMatch || !IsEmptyMatch (CompletionString));
			}
		}

		public bool IsUniqueMatch {
			get {
				return filteredItems.Count == 1;
			}
		}

		bool IsChanging {
			get { return mutableList != null && mutableList.IsChanging; }
		}

		static bool IsEmptyMatch (string completionString)
		{
			if (string.IsNullOrEmpty (completionString))
				return true;
			var ch = completionString [0];
			return char.IsDigit (ch);
		}

		bool autoSelect;
		public bool AutoSelect {
			get { return autoSelect; }
			set {
				autoSelect = value;
				UpdateViewSelectionEnabled ();
			}
		}

		bool showCategories;
		public bool ShowCategories {
			get { return showCategories && filteredCategories.Count > 1; }
			set {
				if (value != showCategories) {
					showCategories = value;
					UpdateCategoryMode ();
				}
			}
		}

		public bool InCategoryMode {
			get { return showCategories && filteredCategories.Count > 1; }
		}

		string CurrentCompletionText {
			get {
				if (SelectedItemIndex != -1 && AutoSelect)
					return ((CompletionData)dataList [SelectedItemIndex]).CompletionText;
				return null;
			}
		}

		/// <summary>
		/// Item currently selected in the code completion window
		/// </summary>
		public CompletionData SelectedItem {
			get {
				return SelectedItemIndex >= 0 ? dataList [SelectedItemIndex] : null;
			}
		}

		int ViewIndexToItemIndex (int viewIndex)
		{
			if (viewIndex < 0 || viewIndex >= filteredItems.Count)
				return -1;
			
			if (InCategoryMode) {
				foreach (var c in filteredCategories) {
					if (viewIndex < c.Items.Count)
						return c.Items [viewIndex];
					viewIndex -= c.Items.Count;
				}
				return -1;
			} else {
				return filteredItems [viewIndex];
			}
		}

		/// <summary>
		/// Gets or sets the index inside CompletionDataList of the currently selected item
		/// </summary>
		/// <value>The index of the selected item.</value>
		public int SelectedItemIndex {
			get => view.SelectedItemIndex;
			set => view.SelectedItemIndex = value;
		}

		// This is precalculated instead of calling DataProvider.GetCompletionText (SelectedItemIndex)
 		// it's called from CompletionListWindow.OnChanged after the list changes and the existing index no longer applies
		string SelectedItemCompletionText { get; set; }

		public string PartialWord {
			get {
				if (usingPreviewEntry)
					return previewCompletionEntryText;
				if (completionWidget == null)
					return "";
				return completionWidget.GetText (StartOffset, EndOffset);
			}
		}

		public string CurrentPartialWord {
			get {
				return !string.IsNullOrEmpty (PartialWord) ? PartialWord : DefaultCompletionString;
			}
		}

		public string DefaultCompletionString {
			get;
			set;
		}

		public bool CloseOnSquareBrackets {
			get;
			set;
		}

		int startOffset;
		internal int StartOffset {
			get {
				return startOffset;
			}
			set {
				startOffset = value;
			}
		}

		public int EndOffset {
			get;
			set;
		}

		void UpdateViewSelectionEnabled ()
		{
			if (SelectionEnabled != view.SelectionEnabled)
				view.SelectionEnabled = SelectionEnabled;
		}

		public void ClearMruCache ()
		{
			cache.Clear ();
		}

		CompletionCharacters CompletionCharacters {
			get {
				var ext = Extension;
				if (ext == null) // May happen in unit tests.
					return MonoDevelop.Ide.CodeCompletion.CompletionCharacters.FallbackCompletionCharacters;
				return MonoDevelop.Ide.CodeCompletion.CompletionCharacters.Get (ext.CompletionLanguage);
			}
		}

		/// <summary>
		/// Filters and sorts the list of items to be shown to the user
		/// </summary>
		/// <param name="oldCompletionString">Old completion string value before current one was set.</param>
		void FilterItems (string oldCompletionString)
		{
			if (dataList == null)
				return;
			
			Counters.ProcessCodeCompletion.Trace ("Begin filtering and sorting completion data");

			var filterResult = dataList.FilterCompletionList (new CompletionListFilterInput (dataList, filteredItems, oldCompletionString, completionString));

			// If the data list doesn't have a custom filter method, use the default one
			if (filterResult == null)
				filterResult = MonoDevelop.Ide.CodeCompletion.CompletionDataList.DefaultFilterItems (dataList, filteredItems, oldCompletionString, completionString);

			filteredItems = filterResult.FilteredItems;
			filteredCategories = filterResult.CategorizedItems;

			Counters.ProcessCodeCompletion.Trace ("End filtering and sorting completion data");

			// Show the filtered items in the view
			view.ShowFilteredItems (filterResult);

			// If the previously selected item is not visible anymore, select the first item in the view
			if (SelectedItemIndex == -1 && filteredItems.Count > 0)
				SelectedItemIndex = filteredItems [0];
			
			RepositionDeclarationViewWindow ();

			// InCategoryMode can change if the new results include new categories
			if (view.InCategoryMode != InCategoryMode)
				view.InCategoryMode = InCategoryMode;
		}

		public bool PreProcessKeyEvent (KeyDescriptor descriptor)
		{
			if (descriptor.SpecialKey == SpecialKey.Escape) {
				HideWindow ();
				return true;
			}

			KeyActions ka = KeyActions.None;
			bool keyHandled = false;
			if (dataList != null) {
				// Give a chance to completion key handlers to pre-process the key
				foreach (ICompletionKeyHandler handler in dataList.KeyHandler) {
					if (handler.PreProcessKey (listWindow, descriptor, out ka)) {
						keyHandled = true;
						break;
					}
				}
			}

			// If a handler did not pre-process the key, do it now
			if (!keyHandled)
				ka = PreProcessKey (descriptor);
			
			if ((ka & KeyActions.Complete) != 0)
				CompleteWord (ref ka, descriptor);

			if ((ka & KeyActions.CloseWindow) != 0) {
				HideWindow ();
			}

			if ((ka & KeyActions.Ignore) != 0)
				return true;
			
			if ((ka & KeyActions.Process) != 0) {
				if (descriptor.SpecialKey == SpecialKey.Left || descriptor.SpecialKey == SpecialKey.Right) {
					// Close if there's a modifier active EXCEPT lock keys and Modifiers
					// Makes an exception for Mod1Mask (usually alt), shift, control, meta and super
					// This prevents the window from closing if the num/scroll/caps lock are active
					// FIXME: modifier mappings depend on X server settings

					//					if ((modifier & ~(Gdk.ModifierType.LockMask | (Gdk.ModifierType.ModifierMask & ~(Gdk.ModifierType.ShiftMask | Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.ControlMask | Gdk.ModifierType.MetaMask | Gdk.ModifierType.SuperMask)))) != 0) {
					// this version doesn't work for my system - seems that I've a modifier active
					// that gdk doesn't know about. How about the 2nd version - should close on left/rigt + shift/mod1/control/meta/super
					if ((descriptor.ModifierKeys & (ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Command)) != 0) {
						HideWindow ();
						return false;
					}

					if (declarationViewWindow != null && declarationViewWindow.Multiple) {
						if (descriptor.SpecialKey == SpecialKey.Left)
							declarationViewWindow.OverloadLeft ();
						else
							declarationViewWindow.OverloadRight ();
					} else {
						HideWindow ();
						return false;
					}
					return true;
				}
				if (dataList != null && dataList.CompletionSelectionMode == CompletionSelectionMode.OwnTextField)
					return true;
			}
			return false;
		}


		public KeyActions PreProcessKey (KeyDescriptor descriptor)
		{
			switch (descriptor.SpecialKey) {
			case SpecialKey.Home:
				if ((descriptor.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
					return KeyActions.Process;
				SelectedItemIndex = ViewIndexToItemIndex (0);
				return KeyActions.Ignore;

			case SpecialKey.End:
				if ((descriptor.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
					return KeyActions.Process;
				SelectedItemIndex = ViewIndexToItemIndex (filteredItems.Count - 1);
				return KeyActions.Ignore;

			case SpecialKey.Up:
				if ((descriptor.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift) {
					if (!SelectionEnabled /*&& !CompletionWindowManager.ForceSuggestionMode*/)
						AutoCompleteEmptyMatch = AutoSelect = true;
					if (!InCategoryMode) {
						IdeApp.Preferences.EnableCompletionCategoryMode.Set (true);
						return KeyActions.Ignore;
					}
					MoveToCategory (-1);
					return KeyActions.Ignore;
				}
				if (SelectionEnabled && filteredItems.Count < 1)
					return KeyActions.CloseWindow | KeyActions.Process;
				if (!SelectionEnabled /*&& !CompletionWindowManager.ForceSuggestionMode*/) {
					AutoCompleteEmptyMatch = AutoSelect = true;
				} else {
					view.MoveCursor (-1);
				}
				return KeyActions.Ignore;

			case SpecialKey.Tab:
				//tab always completes current item even if selection is disabled
				if (!AutoSelect)
					AutoSelect = true;
				goto case SpecialKey.Return;

			case SpecialKey.Return:
				if (descriptor.ModifierKeys != ModifierKeys.None && descriptor.ModifierKeys != ModifierKeys.Shift)
					return KeyActions.CloseWindow;
				if (dataList == null || dataList.Count == 0)
					return KeyActions.CloseWindow;
				WasShiftPressed = (descriptor.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift;

				if (SelectedItem != null) {
					switch (SelectedItem.Rules.EnterKeyRule) {
					case Microsoft.CodeAnalysis.Completion.EnterKeyRule.Always:
						return KeyActions.Complete | KeyActions.Process | KeyActions.CloseWindow;
					case Microsoft.CodeAnalysis.Completion.EnterKeyRule.AfterFullyTypedWord:
						if (PartialWord.Length == SelectedItem.CompletionText.Length)
							return KeyActions.Complete | KeyActions.Ignore | KeyActions.CloseWindow;
						return KeyActions.Complete | KeyActions.Process | KeyActions.CloseWindow;
					case Microsoft.CodeAnalysis.Completion.EnterKeyRule.Never:
					case Microsoft.CodeAnalysis.Completion.EnterKeyRule.Default:
					default:
						return KeyActions.Complete | KeyActions.Ignore | KeyActions.CloseWindow;
					}
				}
				return KeyActions.Complete | KeyActions.Ignore | KeyActions.CloseWindow;

			case SpecialKey.Down:
				if ((descriptor.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift) {
					if (!SelectionEnabled /*&& !CompletionWindowManager.ForceSuggestionMode*/)
						AutoCompleteEmptyMatch = AutoSelect = true;
					if (!InCategoryMode) {
						IdeApp.Preferences.EnableCompletionCategoryMode.Set (true);
						return KeyActions.Ignore;
					}
					MoveToCategory (1);
					return KeyActions.Ignore;
				}
				if (SelectionEnabled && filteredItems.Count < 1)
					return KeyActions.CloseWindow | KeyActions.Process;

				if (!SelectionEnabled /*&& !CompletionWindowManager.ForceSuggestionMode*/) {
					AutoCompleteEmptyMatch = AutoSelect = true;
				} else {
					view.MoveCursor (1);
				}
				return KeyActions.Ignore;

			case SpecialKey.PageUp:
				if ((descriptor.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
					return KeyActions.Process;
				if (filteredItems.Count < 2)
					return KeyActions.CloseWindow | KeyActions.Process;
				view.PageUp ();
				return KeyActions.Ignore;

			case SpecialKey.PageDown:
				if ((descriptor.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
					return KeyActions.Process;
				if (filteredItems.Count < 2)
					return KeyActions.CloseWindow | KeyActions.Process;
				view.PageDown ();
				return KeyActions.Ignore;

			case SpecialKey.Left:
				//if (curPos == 0) return KeyActions.CloseWindow | KeyActions.Process;
				//curPos--;
				return KeyActions.Process;

			case SpecialKey.Right:
				//if (curPos == word.Length) return KeyActions.CloseWindow | KeyActions.Process;
				//curPos++;
				return KeyActions.Process;

				//			case Gdk.Key.Caps_Lock:
				//			case Gdk.Key.Num_Lock:
				//			case Gdk.Key.Scroll_Lock:
				//				return KeyActions.Ignore;
				//
				//			case Gdk.Key.Control_L:
				//			case Gdk.Key.Control_R:
				//			case Gdk.Key.Alt_L:
				//			case Gdk.Key.Alt_R:
				//			case Gdk.Key.Shift_L:
				//			case Gdk.Key.Shift_R:
				//			case Gdk.Key.ISO_Level3_Shift:
				//				// AltGr
				//				return KeyActions.Process;
			}
			var data = SelectedItem;

			if (descriptor.KeyChar == '\0')
				return KeyActions.Process;

			if (descriptor.KeyChar == ' ' && (descriptor.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
				return KeyActions.CloseWindow | KeyActions.Process;

			if (char.IsDigit (descriptor.KeyChar) && string.IsNullOrEmpty (CurrentCompletionText))
				return KeyActions.CloseWindow | KeyActions.Process;

			if (data != null && data.MuteCharacter (descriptor.KeyChar, PartialWord)) {
				if (data.IsCommitCharacter (descriptor.KeyChar, PartialWord)) {
					return KeyActions.CloseWindow | KeyActions.Ignore | KeyActions.Complete;
				}
				return KeyActions.CloseWindow | KeyActions.Ignore;
			}

			// Special case end with punctuation like 'param:' -> don't input double punctuation, otherwise we would end up with 'param::'
			if (char.IsPunctuation (descriptor.KeyChar) && descriptor.KeyChar != '_') {
				if (descriptor.KeyChar == ':') {
					foreach (var item in filteredItems) {
						if (dataList [item].DisplayText.EndsWith (descriptor.KeyChar.ToString (), StringComparison.Ordinal)) {
							SelectedItemIndex = item;
							return KeyActions.Complete | KeyActions.CloseWindow | KeyActions.Ignore;
						}
					}
				} else {
					var selectedItem = SelectedItemIndex;
					if (selectedItem < 0 || (dataList != null && selectedItem >= dataList.Count)) {
						return KeyActions.CloseWindow;
					}
					if (dataList [selectedItem].DisplayText.EndsWith (descriptor.KeyChar.ToString (), StringComparison.Ordinal)) {
						return KeyActions.Complete | KeyActions.CloseWindow | KeyActions.Ignore;
					}
				}
			}

			if (data != null && data.IsCommitCharacter (descriptor.KeyChar, PartialWord)) {
				var curword = PartialWord;
				var match = FindMatchedEntry (curword).Index;
				if (match >= 0 && System.Char.IsPunctuation (descriptor.KeyChar)) {
					string text = ((CompletionData)dataList [filteredItems [match]]).CompletionText;
					if (!text.StartsWith (curword, StringComparison.OrdinalIgnoreCase))
						match = -1;
				}

				if (SelectionEnabled && CompletionCharacters.CompleteOn (descriptor.KeyChar)) {
					if (descriptor.KeyChar == '{' && !AutoCompleteEmptyMatchOnCurlyBrace && string.IsNullOrEmpty (CompletionString))
						return KeyActions.CloseWindow | KeyActions.Process;
					return KeyActions.Complete | KeyActions.Process | KeyActions.CloseWindow;
				}
				return KeyActions.CloseWindow | KeyActions.Process;
			}

			if ((char.IsWhiteSpace (descriptor.KeyChar) || char.IsPunctuation (descriptor.KeyChar)) && SelectedItem == null)
				return KeyActions.CloseWindow | KeyActions.Process;

			return KeyActions.Process;
		}

		public void PostProcessKeyEvent (KeyDescriptor descriptor)
		{
			if (dataList == null)
				return;
			KeyActions ka = KeyActions.None;
			bool keyHandled = false;
			foreach (var handler in dataList.KeyHandler) {
				if (handler.PostProcessKey (listWindow, descriptor, out ka)) {
					keyHandled = true;
					break;
				}
			}
			if (!keyHandled)
				ka = PostProcessKey (descriptor);
			if ((ka & KeyActions.Complete) != 0)
				CompleteWord (ref ka, descriptor);
			UpdateLastWordChar ();
			if ((ka & KeyActions.CloseWindow) != 0) {
				HideWindow ();
			}
		}

		public KeyActions PostProcessKey (KeyDescriptor descriptor)
		{
			if (CompletionWidget == null || StartOffset > CompletionWidget.CaretOffset) {// CompletionWidget == null may happen in unit tests.
				return KeyActions.CloseWindow | KeyActions.Process;
			}

			if (HideWhenWordDeleted && StartOffset >= CompletionWidget.CaretOffset) {
				return KeyActions.CloseWindow | KeyActions.Process;
			}
			switch (descriptor.SpecialKey) {
			case SpecialKey.BackSpace:
				ResetSizes ();
				UpdateWordSelection ();
				return KeyActions.Process;
			}
			var keyChar = descriptor.KeyChar;

			if (keyChar == '[' && CloseOnSquareBrackets) {
				return KeyActions.Process | KeyActions.CloseWindow;
			}

			if (char.IsLetterOrDigit (keyChar) || keyChar == '_') {
				ResetSizes ();
				UpdateWordSelection ();
				return KeyActions.Process;
			}
			if (SelectedItemIndex < 0) {
				return KeyActions.Process;
			}
			var data = dataList [SelectedItemIndex];

			if (char.IsPunctuation (descriptor.KeyChar) && descriptor.KeyChar != '_') {
				if (descriptor.KeyChar == ':') {
					foreach (var item in filteredItems) {
						if (dataList [item].DisplayText.EndsWith (descriptor.KeyChar.ToString (), StringComparison.Ordinal)) {
							SelectedItemIndex = item;
							return KeyActions.Complete | KeyActions.CloseWindow | KeyActions.Ignore;
						}
					}
				} else {
					var selectedItem = SelectedItemIndex;
					if (selectedItem < 0 || selectedItem >= dataList.Count)
						return KeyActions.CloseWindow;
					if (descriptor.SpecialKey == SpecialKey.None) {
						ResetSizes ();
						UpdateWordSelection ();
					}
				}
			}

			return KeyActions.Process;
		}

		public bool CompleteWord ()
		{
			KeyActions ka = KeyActions.None;
			return CompleteWord (ref ka, KeyDescriptor.Tab);
		}

		internal bool IsInCompletion { get; set; }

		public CodeCompletionContext CodeCompletionContext { get; set; }

		public bool CompleteWord (ref KeyActions ka, KeyDescriptor descriptor)
		{
			if (SelectedItemIndex == -1 || dataList == null)
				return false;
			var item = dataList [SelectedItemIndex];
			if (item == null)
				return false;
			IsInCompletion = true;
			try {
				// first close the completion list, then insert the text.
				// this is required because that's the logical event chain, otherwise things could be messed up
				CloseCompletionList ();
				/*			var cdItem = (CompletionData)item;
							cdItem.InsertCompletionText (this, ref ka, closeChar, keyChar, modifier);
							AddWordToHistory (PartialWord, cdItem.CompletionText);
							OnWordCompleted (new CodeCompletionContextEventArgs (CompletionWidget, CodeCompletionContext, cdItem.CompletionText));
							*/
				if (item.HasOverloads && declarationViewWindow != null && declarationViewWindow.CurrentOverload >= 0 && declarationViewWindow.CurrentOverload < item.OverloadedData.Count) {
					item.OverloadedData [declarationViewWindow.CurrentOverload].InsertCompletionText (listWindow, ref ka, descriptor);
				} else {
					item.InsertCompletionText (listWindow, ref ka, descriptor);
				}
				cache.CommitCompletionData (item);
				OnWordCompleted (new CodeCompletionContextEventArgs (completionWidget, context, item.DisplayText));
			} finally {
				IsInCompletion = false;
				HideWindow ();
			}
			return true;
		}

		CompletionSelectionStatus FindMatchedEntry (string partialWord)
		{
			if (dataList == null)
				return CompletionSelectionStatus.Empty;
			return dataList.FindMatchedEntry (dataList, cache, partialWord, filteredItems);
		}

		public void UpdateWordSelection ()
		{
			UpdateLastWordChar ();
			SelectEntry (CurrentPartialWord);
		}

		void UpdateLastWordChar ()
		{
			if (CompletionWidget != null)
				EndOffset = CompletionWidget.CaretOffset;
		}

		void SelectEntry (string s)
		{
			var match = FindMatchedEntry (s);
			SelectEntry (match);
		}

		void SelectEntry (CompletionSelectionStatus match)
		{
			if (match.IsSelected.HasValue)
				AutoSelect = match.IsSelected.Value;
			if (match.Index >= 0)
				// Don't use ViewIndexToItemIndex() to convert from match index to item index.
				// Indices in 'match' are always relative to the filteredItems list.
				SelectedItemIndex = filteredItems [match.Index];
			UpdateDeclarationView ();
		}

		bool completionListClosed;
		void CloseCompletionList ()
		{
			if (!completionListClosed) {
				dataList.OnCompletionListClosed (EventArgs.Empty);
				completionListClosed = true;
			}
		}

		public void ResetSizes ()
		{
			UpdateLastWordChar ();
			CompletionString = PartialWord;
			view.ResetSizes ();
		}

		public void ToggleCategoryMode ()
		{
			IdeApp.Preferences.EnableCompletionCategoryMode.Set (!IdeApp.Preferences.EnableCompletionCategoryMode.Value); 
			ResetSizes ();
		}

		void UpdateCategoryMode ()
		{
			if (view.InCategoryMode != InCategoryMode) {
				view.InCategoryMode = InCategoryMode;
				if (InCategoryMode) {
					// Select the first item in the first category
					if (string.IsNullOrEmpty (CompletionString) && IdeApp.Preferences.EnableCompletionCategoryMode)
						SelectedItemIndex = filteredCategories.First ().Items.First ();
				}
			}
		}

		void MoveToCategory (int relative)
		{
			var currentCategory = SelectedItem?.CompletionCategory;
			int current = filteredCategories.FindIndex (c => c.CompletionCategory == currentCategory);
			int next = Math.Min (filteredCategories.Count - 1, Math.Max (0, current + relative));
			if (next < 0 || next >= filteredCategories.Count)
				return;
			CategorizedCompletionItems newCategory = filteredCategories[next];
			SelectedItemIndex = newCategory.Items[0];
		}

		#region IListDataProvider

		int IListDataProvider.ItemCount {
			get { return dataList != null ? dataList.Count : 0; }
		}

		string IListDataProvider.GetText (int n)
		{
			return dataList [n].DisplayText;
		}

		int [] IListDataProvider.GetHighlightedTextIndices (int n)
		{
			return dataList.GetHighlightedIndices (dataList [n], CompletionString);
		}

		string IListDataProvider.GetDescription (int n, bool isSelected)
		{
			return ((CompletionData)dataList [n]).GetDisplayDescription (isSelected);
		}

		string IListDataProvider.GetRightSideDescription (int n, bool isSelected)
		{
			return ((CompletionData)dataList [n]).GetRightSideDescription (isSelected);
		}

		bool IListDataProvider.HasMarkup (int n)
		{
			return true;
		}

		//NOTE: we only ever return markup for items marked as obsolete
		string IListDataProvider.GetMarkup (int n)
		{
			var completionData = dataList [n];
			return completionData.GetDisplayTextMarkup ();
		}

		string IListDataProvider.GetCompletionText (int n)
		{
			return ((CompletionData)dataList [n]).CompletionText;
		}

		CompletionData IListDataProvider.GetCompletionData (int n)
		{
			return dataList [n];
		}

		int IListDataProvider.CompareTo (int n, int m)
		{
			return MonoDevelop.Ide.CodeCompletion.CompletionDataList.CompareTo (dataList, n, m);
		}

		Xwt.Drawing.Image IListDataProvider.GetIcon (int n)
		{
			string iconName = ((CompletionData)dataList [n]).Icon;
			if (string.IsNullOrEmpty (iconName))
				return null;
			return ImageService.GetIcon (iconName, Gtk.IconSize.Menu);
		}

		#endregion

		void ICompletionViewEventSink.OnDoubleClick ()
		{
			CompleteWord ();
			HideWindow ();
		}

		void ICompletionViewEventSink.OnListScrolled ()
		{
			HideDeclarationView ();
			UpdateDeclarationView ();
		}

		void ICompletionViewEventSink.OnPreviewCompletionEntryChanged (string text)
		{
			CompletionString = previewCompletionEntryText = text;
			UpdateWordSelection ();
		}

		void ICompletionViewEventSink.OnPreviewCompletionEntryActivated ()
		{
			CompleteWord ();
		}

		void ICompletionViewEventSink.OnPreviewCompletionEntryLostFocus ()
		{
			Dispose ();
		}

		bool ICompletionViewEventSink.OnPreProcessPreviewCompletionEntryKey (KeyDescriptor descriptor)
		{
			var keyAction = PreProcessKey (descriptor);
			if (keyAction.HasFlag (KeyActions.Complete))
				CompleteWord ();

			if (keyAction.HasFlag (KeyActions.CloseWindow)) {
				Dispose ();
			}
			return keyAction.HasFlag (KeyActions.Process);
		}

		void ICompletionViewEventSink.OnAllocationChanged ()
		{
			UpdateDeclarationView ();
		}

		void ICompletionViewEventSink.OnSelectedItemChanged ()
		{
			OnSelectionChanged ();
			UpdateDeclarationView ();
		}
	}
}
