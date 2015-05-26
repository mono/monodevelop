// CompletionListWindow.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;

using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components;
using System.Linq;
using MonoDevelop.Ide.Editor.Extension;
using System.ComponentModel;
using System.Threading;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CompletionListWindow : ListWindow, IListDataProvider
	{
		const int declarationWindowMargin = 3;

		TooltipInformationWindow declarationviewwindow;
		CompletionData currentData;
		CancellationTokenSource declarationViewCancelSource;
		Widget parsingMessage;
		int initialWordLength;
		int previousWidth = -1, previousHeight = -1;

		public CodeCompletionContext CodeCompletionContext {
			get;
			set;
		}

		public int X { get; private set; }
		public int Y { get; private set; }
		
		public int InitialWordLength {
			get { return initialWordLength; }
		}
		
		IMutableCompletionDataList mutableList;
		public ICompletionDataList CompletionDataList {
			get { return completionDataList; }
			set {
				completionDataList = value;
				defaultComparer = null;
			}
		}

		bool previewCompletionString;
		Entry previewEntry;
		public override string PartialWord {
			get {
				if (previewEntry != null)
					return previewEntry.Text;
				return base.PartialWord;
			}
		}

		public bool PreviewCompletionString {
			get {
				return previewCompletionString;
			}
			set {
				if (value) {
					previewEntry = new Entry ();
					previewEntry.Changed += delegate (object sender, EventArgs e) {
						List.CompletionString = previewEntry.Text;

						UpdateWordSelection ();
						List.QueueDraw ();
					};
					previewEntry.KeyPressEvent += delegate(object o, KeyPressEventArgs args) {
						var keyAction = PreProcessKey (KeyDescriptor.FromGtk (args.Event.Key, (char)args.Event.KeyValue, args.Event.State));
						if (keyAction.HasFlag (KeyActions.Complete))
							CompleteWord ();

						if (keyAction.HasFlag (KeyActions.CloseWindow)) {
							Destroy ();
						}

						args.RetVal = !keyAction.HasFlag (KeyActions.Process);
					};
					WordCompleted += delegate (object sender, CodeCompletionContextEventArgs e) {
						Destroy ();
					};
					vbox.PackStart (previewEntry, false, true, 0);

					previewEntry.Activated += (sender, e) => CompleteWord ();
					previewEntry.Show ();
					FocusOutEvent += (o, args) => Destroy ();
					GLib.Timeout.Add (10, delegate {
						previewEntry.GrabFocus ();
						return false;
					});

				}
				previewCompletionString = value;
			}
		}

		public CompletionListWindow (WindowType type = WindowType.Popup) : base(type) 
		{
			if (IdeApp.Workbench != null)
				this.TransientFor = IdeApp.Workbench.RootWindow;
			TypeHint = Gdk.WindowTypeHint.Combo;
			SizeAllocated += new SizeAllocatedHandler (ListSizeChanged);
			Events = Gdk.EventMask.PropertyChangeMask;
			WindowTransparencyDecorator.Attach (this);
			DataProvider = this;
			HideDeclarationView ();
			List.ListScrolled += (object sender, EventArgs e) => {
				HideDeclarationView ();
				UpdateDeclarationView ();
			};
			List.WordsFiltered += delegate {
				RepositionDeclarationViewWindow ();
			};
		}

		bool completionListClosed;
		void CloseCompletionList ()
		{
			if (!completionListClosed) {
				completionDataList.OnCompletionListClosed (EventArgs.Empty);
				completionListClosed = true;
			}
		}

		void ReleaseObjects ()
		{
			CompletionWidget = null;
			CompletionDataList = null;
			CodeCompletionContext = null;
			currentData = null;
			Extension = null;
		}

		protected override void OnDestroyed ()
		{
			if (declarationviewwindow != null) {
				declarationviewwindow.Destroy ();
				declarationviewwindow = null;
			}
			
			if (mutableList != null) {
				mutableList.Changing -= OnCompletionDataChanging;
				mutableList.Changed -= OnCompletionDataChanged;
				mutableList = null;
			}

			if (completionDataList != null) {
				if (completionDataList is IDisposable) 
					((IDisposable)completionDataList).Dispose ();
				CloseCompletionList ();
				completionDataList = null;
				defaultComparer = null;
			}

			HideDeclarationView ();
			
			if (declarationviewwindow != null) {
				declarationviewwindow.Destroy ();
				declarationviewwindow = null;
			}
			ReleaseObjects ();
			base.OnDestroyed ();
		}

		public void PostProcessKeyEvent (KeyDescriptor descriptor)
		{
			KeyActions ka = KeyActions.None;
			bool keyHandled = false;
			if (CompletionDataList != null) {
				foreach (var handler in CompletionDataList.KeyHandler) {
					if (handler.PostProcessKey (this, descriptor, out ka)) {
						keyHandled = true;
						break;
					}
				}
			}
			
			if (!keyHandled)
				ka = PostProcessKey (descriptor);
			if ((ka & KeyActions.Complete) != 0) 
				CompleteWord (ref ka, descriptor);
			if ((ka & KeyActions.CloseWindow) != 0) {
				CompletionWindowManager.HideWindow ();
				OnWindowClosed (EventArgs.Empty);
			}
		}

		/// <summary>
		/// For unit test purposes.
		/// </summary>
		[EditorBrowsableAttribute(EditorBrowsableState.Never)]
		internal event EventHandler WindowClosed;

		protected virtual void OnWindowClosed (EventArgs e)
		{
			var handler = WindowClosed;
			if (handler != null)
				handler (this, e);
		}
		
		public void ToggleCategoryMode ()
		{
			IdeApp.Preferences.EnableCompletionCategoryMode.Set (!IdeApp.Preferences.EnableCompletionCategoryMode.Value); 
			List.UpdateCategoryMode ();
			ResetSizes ();
			List.QueueDraw ();
		}
		
		public bool PreProcessKeyEvent (KeyDescriptor descriptor)
		{
			if (descriptor.SpecialKey == SpecialKey.Escape) {
				CompletionWindowManager.HideWindow ();
				return true;
			}

			KeyActions ka = KeyActions.None;
			bool keyHandled = false;
			if (CompletionDataList != null) {
				foreach (ICompletionKeyHandler handler in CompletionDataList.KeyHandler) {
					if (handler.PreProcessKey (this, descriptor, out ka)) {
						keyHandled = true;
						break;
					}
				}
			}
			if (!keyHandled)
				ka = PreProcessKey (descriptor);
			if ((ka & KeyActions.Complete) != 0)
				CompleteWord (ref ka, descriptor);

			if ((ka & KeyActions.CloseWindow) != 0) {
				CompletionWindowManager.HideWindow ();
				OnWindowClosed (EventArgs.Empty);
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
						CompletionWindowManager.HideWindow ();
						OnWindowClosed (EventArgs.Empty);
						return false;
					}
					
					if (declarationviewwindow != null && declarationviewwindow.Multiple) {
						if (descriptor.SpecialKey == SpecialKey.Left)
							declarationviewwindow.OverloadLeft ();
						else
							declarationviewwindow.OverloadRight ();
						UpdateDeclarationView ();
					} else {
						CompletionWindowManager.HideWindow ();
						OnWindowClosed (EventArgs.Empty);
						return false;
					}
					return true;
				}
				if (completionDataList != null && completionDataList.CompletionSelectionMode == CompletionSelectionMode.OwnTextField)
					return true;
			}
			return false;
		}
		
		public override void SelectEntry (string s)
		{
			base.SelectEntry (s);
			UpdateDeclarationView ();
		}


		internal bool ShowListWindow (char firstChar, ICompletionDataList list, ICompletionWidget completionWidget, CodeCompletionContext completionContext)
		{
			InitializeListWindow (completionWidget, completionContext);
			return ShowListWindow (list, completionContext);
		}

		internal void InitializeListWindow (ICompletionWidget completionWidget, CodeCompletionContext completionContext)
		{
			if (completionWidget == null)
				throw new ArgumentNullException ("completionWidget");
			if (completionContext == null)
				throw new ArgumentNullException ("completionContext");
			if (mutableList != null) {
				mutableList.Changing -= OnCompletionDataChanging;
				mutableList.Changed -= OnCompletionDataChanged;
				HideFooter ();
			}
			ResetState ();
			CompletionWidget = completionWidget;
			CodeCompletionContext = completionContext;

			string text = CompletionWidget.GetCompletionText (CodeCompletionContext);
			initialWordLength = CompletionWidget.SelectedLength > 0 ? 0 : text.Length;
			StartOffset = CompletionWidget.CaretOffset - initialWordLength;
		}

		internal bool ShowListWindow (ICompletionDataList list, CodeCompletionContext completionContext)
		{
			if (list == null)
				throw new ArgumentNullException ("list");
			
			CodeCompletionContext = completionContext;
			CompletionDataList = list;
			ResetState ();

			mutableList = completionDataList as IMutableCompletionDataList;
			PreviewCompletionString = completionDataList.CompletionSelectionMode == CompletionSelectionMode.OwnTextField;

			if (mutableList != null) {
				mutableList.Changing += OnCompletionDataChanging;
				mutableList.Changed += OnCompletionDataChanged;

				if (mutableList.IsChanging)
					OnCompletionDataChanging (null, null);
			}

			if (FillList ()) {
				AutoSelect = list.AutoSelect;
				AutoCompleteEmptyMatch = list.AutoCompleteEmptyMatch;
				AutoCompleteEmptyMatchOnCurlyBrace = list.AutoCompleteEmptyMatchOnCurlyBrace;
				CloseOnSquareBrackets = list.CloseOnSquareBrackets;
				// makes control-space in midle of words to work
				string text = CompletionWidget.GetCompletionText (CodeCompletionContext);
				DefaultCompletionString = completionDataList.DefaultCompletionString ?? "";
				if (text.Length == 0) {
					UpdateWordSelection ();
					initialWordLength = 0;
					//completionWidget.SelectedLength;
					StartOffset = CompletionWidget.CaretOffset;
					ResetSizes ();
					ShowAll ();
					UpdateWordSelection ();
					UpdateDeclarationView ();
					//if there is only one matching result we take it by default
					if (completionDataList.AutoCompleteUniqueMatch && IsUniqueMatch && !IsChanging) {
						CompleteWord ();
						CompletionWindowManager.HideWindow ();
						return false;
					}
					return true;
				}

				initialWordLength = CompletionWidget.SelectedLength > 0 ? 0 : text.Length;
				StartOffset = CompletionWidget.CaretOffset - initialWordLength;
				HideWhenWordDeleted = initialWordLength != 0;
				ResetSizes ();
				UpdateWordSelection ();
				//if there is only one matching result we take it by default
				if (completionDataList.AutoCompleteUniqueMatch && IsUniqueMatch && !IsChanging) {
					CompleteWord ();
					CompletionWindowManager.HideWindow ();
					return false;
				}
				ShowAll ();
				UpdateDeclarationView ();
				return true;
			}
			CompletionWindowManager.HideWindow ();
			return false;
		}
		
		class DataItemComparer : IComparer<CompletionData>
		{
			public int Compare (CompletionData a, CompletionData b)
			{
				if (a is IComparable && b is IComparable)
					return ((IComparable)a).CompareTo (b);
				return CompletionData.Compare (a, b);
			}
		}

		IComparer<CompletionData> GetComparerForCompletionList (ICompletionDataList dataList)
		{
			var concrete = dataList as CompletionDataList;
			return concrete != null && concrete.Comparer != null ? concrete.Comparer : new DataItemComparer ();
		}
		
		bool FillList ()
		{
			if ((completionDataList.Count == 0) && !IsChanging)
				return false;
			
			Style = CompletionWidget.GtkStyle;
			
			//sort, sinking obsolete items to the bottoms
			//the string comparison is ordinal as that makes it an order of magnitude faster, which 
			//which makes completion triggering noticeably more responsive
			var list = completionDataList as CompletionDataList;
			if (!completionDataList.IsSorted)
				completionDataList.Sort (GetComparerForCompletionList (completionDataList));

			Reposition (true);
			return true;
		}
		
		enum WindowPositonY {
			None,
			Top,
			Bottom
		}
		WindowPositonY yPosition;
		
		void Reposition (bool force)
		{
			X = CodeCompletionContext.TriggerXCoord - TextOffset;
			Y = CodeCompletionContext.TriggerYCoord;

			int w, h;
			GetSize (out w, out h);

			if (!force && previousHeight != h && previousWidth != w)
				return;
			
			// Note: we add back the TextOffset here in case X and X+TextOffset are on different monitors.
			Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (X + TextOffset, Y));
			
			previousHeight = h;
			previousWidth = w;
			
			if (X + w > geometry.Right)
				X = geometry.Right - w;
			else if (X < geometry.Left)
				X = geometry.Left;
			
			if (Y + h > geometry.Bottom || yPosition == WindowPositonY.Top) {
				// Put the completion-list window *above* the cursor
				Y = Y - CodeCompletionContext.TriggerTextHeight - h;
				yPosition = WindowPositonY.Top;
			} else {
				yPosition = WindowPositonY.Bottom;
			}
			
			curXPos = X;
			curYPos = Y;
			Move (X, Y);
			UpdateDeclarationView ();
		}
		
		//smaller lists get size reallocated after FillList, so we have to reposition them
		//if the window size has changed since we last positioned it
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			Reposition (true);
		}
		
		public bool CompleteWord ()
		{
			KeyActions ka = KeyActions.None;
			return CompleteWord (ref ka, KeyDescriptor.Empty);
		}

		internal bool IsInCompletion { get; set;  }

		public bool CompleteWord (ref KeyActions ka, KeyDescriptor descriptor)
		{
			if (SelectedItem == -1 || completionDataList == null)
				return false;
			var item = completionDataList [SelectedItem];
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
				if (item.HasOverloads && declarationviewwindow.CurrentOverload >= 0 && declarationviewwindow.CurrentOverload < item.OverloadedData.Count) {
					item.OverloadedData[declarationviewwindow.CurrentOverload].InsertCompletionText (this, ref ka, descriptor);
				} else {
					item.InsertCompletionText (this, ref ka, descriptor);
				}
				cache.CommitCompletionData (item);
				OnWordCompleted (new CodeCompletionContextEventArgs (CompletionWidget, CodeCompletionContext, item.DisplayText));
			} finally {
				IsInCompletion = false;
				CompletionWindowManager.HideWindow ();
			}
            return true;
		}
		
		protected virtual void OnWordCompleted (CodeCompletionContextEventArgs e)
		{
			var handler = WordCompleted;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler<CodeCompletionContextEventArgs> WordCompleted;
		
		void ListSizeChanged (object obj, SizeAllocatedArgs args)
		{
			UpdateDeclarationView ();
		}

		protected override void OnHidden ()
		{
			HideDeclarationView ();
			base.OnHidden ();
		}

		public void HideWindow ()
		{
			Hide ();
			HideDeclarationView ();
			ReleaseObjects ();
		}

		protected override void DoubleClick ()
		{
			CompleteWord ();
			CompletionWindowManager.HideWindow ();
		}
		
		protected override void OnSelectionChanged ()
		{
			base.OnSelectionChanged ();
			UpdateDeclarationView ();
		}
		
		bool declarationViewHidden = true;
		uint declarationViewTimer;

		void UpdateDeclarationView ()
		{
			if (completionDataList == null || List.SelectionFilterIndex >= completionDataList.Count || List.SelectionFilterIndex == -1) {
				HideDeclarationView ();
				return;
			}
			if (List.GdkWindow == null)
				return;
			RemoveDeclarationViewTimer ();
			// no selection, try to find a selection
			if (List.SelectedItem < 0 || List.SelectedItem >= completionDataList.Count) {
				List.CompletionString = PartialWord;
				bool hasMismatches;
				List.SelectionFilterIndex = FindMatchedEntry (List.CompletionString, out hasMismatches);
			}
			// no success, hide declaration view
			if (List.SelectedItem < 0 || List.SelectedItem >= completionDataList.Count) {
				HideDeclarationView ();
				return;
			}

			var data = completionDataList [List.SelectedItem];
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
			if (declarationviewwindow != null) {
				declarationviewwindow.Hide ();
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

		void EnsureDeclarationViewWindow ()
		{
			if (declarationviewwindow == null) {
				declarationviewwindow = new TooltipInformationWindow ();
			} else {
				declarationviewwindow.SetDefaultScheme ();
			}
			var style = Editor.Highlighting.SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme);
			declarationviewwindow.Theme.SetFlatColor (style.CompletionTooltipWindow.Color);
			if (style.CompletionWindow.HasBorderColor)
				declarationviewwindow.Theme.BorderColor = style.CompletionTooltipWindow.BorderColor;
		}

		void RepositionDeclarationViewWindow ()
		{
			if (base.GdkWindow == null)
				return;
			EnsureDeclarationViewWindow ();
			if (declarationviewwindow.Overloads == 0)
				return;
			var selectedItem = List.SelectedItem;
			Gdk.Rectangle rect = List.GetRowArea (selectedItem);
			if (rect.IsEmpty || rect.Bottom < (int)List.vadj.Value || rect.Y > List.Allocation.Height + (int)List.vadj.Value)
				return;

			declarationviewwindow.ShowArrow = true;
			int ox;
			int oy;
			base.GdkWindow.GetOrigin (out ox, out oy);
			declarationviewwindow.MaximumYTopBound = oy;
			int y = rect.Y + Theme.Padding - (int)List.vadj.Value;
			declarationviewwindow.ShowPopup (this, new Gdk.Rectangle (Gui.Styles.TooltipInfoSpacing, Math.Min (Allocation.Height, Math.Max (0, y)), Allocation.Width, rect.Height), PopupPosition.Left);
			declarationViewHidden = false;
		}

		bool DelayedTooltipShow ()
		{
			DelayedTooltipShowAsync ();
			return false;
		}

		async void DelayedTooltipShowAsync ()
		{
			var selectedItem = List.SelectedItem;
			if (selectedItem < 0 || selectedItem >= completionDataList.Count)
				return;
			
			var data = completionDataList [selectedItem];

			IEnumerable<CompletionData> filteredOverloads;
			if (data.HasOverloads) {
				filteredOverloads = data.OverloadedData;
			} else {
				filteredOverloads = new CompletionData[] { data };
			}

			EnsureDeclarationViewWindow ();
			if (data != currentData) {
				declarationviewwindow.Clear ();
				currentData = data;
				var cs = new CancellationTokenSource ();
				declarationViewCancelSource = cs;
				var overloads = new List<CompletionData> (filteredOverloads);
				foreach (var overload in overloads) {
					await declarationviewwindow.AddOverload ((CompletionData)overload, cs.Token);
				}

				if (cs.IsCancellationRequested)
					return;

				if (declarationViewCancelSource == cs)
					declarationViewCancelSource = null;
				
				if (data.HasOverloads) {
					for (int i = 0; i < overloads.Count; i++) {
						if (!overloads[i].DisplayFlags.HasFlag (DisplayFlags.Obsolete)) {
							declarationviewwindow.CurrentOverload = i;
							break;
						}
					}
				}
			}

			if (declarationviewwindow.Overloads == 0) {
				HideDeclarationView ();
				return;
			}

			if (declarationViewHidden && Visible) {
				RepositionDeclarationViewWindow ();
			}
			
			declarationViewTimer = 0;
		}
		
		protected internal override void ResetState ()
		{
			StartOffset = 0;
			previousWidth = previousHeight = -1;
			yPosition = WindowPositonY.None;
			base.ResetState ();
		}
		
		#region IListDataProvider
		
		int IListDataProvider.ItemCount 
		{ 
			get { return completionDataList != null ? completionDataList.Count : 0; } 
		}
		
		CompletionCategory IListDataProvider.GetCompletionCategory (int n)
		{
			return completionDataList[n].CompletionCategory;
		}
		
		string IListDataProvider.GetText (int n)
		{
			return completionDataList[n].DisplayText;
		}
		
		string IListDataProvider.GetDescription (int n, bool isSelected)
		{
			return ((CompletionData)completionDataList[n]).GetDisplayDescription (isSelected);
		}

		string IListDataProvider.GetRightSideDescription (int n, bool isSelected)
		{
			return ((CompletionData)completionDataList[n]).GetRightSideDescription (isSelected);
		}

		bool IListDataProvider.HasMarkup (int n)
		{
			return true;
		}
		
		//NOTE: we only ever return markup for items marked as obsolete
		string IListDataProvider.GetMarkup (int n)
		{
			var completionData = completionDataList[n];
			return completionData.GetDisplayTextMarkup ();
		}
		
		string IListDataProvider.GetCompletionText (int n)
		{
			return ((CompletionData)completionDataList[n]).CompletionText;
		}

		CompletionData IListDataProvider.GetCompletionData (int n)
		{
			return completionDataList[n];
		}

		IComparer<CompletionData> defaultComparer;

		int IListDataProvider.CompareTo (int n, int m)
		{
			var item1 = completionDataList [n];
			var item2 = completionDataList [m];
			return (defaultComparer ?? (defaultComparer = GetComparerForCompletionList (completionDataList))).Compare (item1, item2);
		}
		
		Xwt.Drawing.Image IListDataProvider.GetIcon (int n)
		{
			string iconName = ((CompletionData)completionDataList[n]).Icon;
			if (string.IsNullOrEmpty (iconName))
				return null;
			return ImageService.GetIcon (iconName, IconSize.Menu);
		}
		
		#endregion
		
		internal bool IsChanging {
			get { return mutableList != null && mutableList.IsChanging; }
		}
		
		void OnCompletionDataChanging (object s, EventArgs args)
		{
			if (parsingMessage == null) {
				var box = new VBox ();
				box.PackStart (new HSeparator (), false, false, 0);
				var hbox = new HBox ();
				hbox.BorderWidth = 3;
				hbox.PackStart (new Image ("md-parser", IconSize.Menu), false, false, 0);
				var lab = new Label (GettextCatalog.GetString ("Gathering class information..."));
				lab.Xalign = 0;
				hbox.PackStart (lab, true, true, 3);
				hbox.ShowAll ();
				parsingMessage = hbox;
			}
			ShowFooter (parsingMessage);
		}
		
		void OnCompletionDataChanged (object s, EventArgs args)
		{
			ResetSizes ();
			HideFooter ();
			
			//try to capture full selection state so as not to interrupt user

			if (Visible) {
				string last = List.AutoSelect ? CurrentCompletionText : PartialWord;
				//don't reset the user-entered word when refilling the list
				var tmp = List.AutoSelect;
				// Fill the list before resetting so that we get the correct size
				FillList ();
				ResetSizes ();
				List.AutoSelect = tmp;
				if (last != null)
					SelectEntry (last);
			}
		}
	}
}
