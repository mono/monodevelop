// ListWindow.cs
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

using Gtk;
using Gdk;
using Pango;
using System;
using System.Xml;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.CodeCompletion
{
	[Flags()]
	public enum KeyActions
	{
		None = 0,
		Process = 1,
		Ignore = 2,
		CloseWindow = 4,
		Complete = 8
	}

	public class ListWindow : Gtk.Window
	{
		internal VScrollbar scrollbar;
		ListWidget list;
		Widget footer;
		VBox vbox;

		StringBuilder word = new StringBuilder();
		int curPos;
		
		public List<int> FilteredItems {
			get {
				return list.filteredItems;
			}
		}
		
		public ListWindow () : base(Gtk.WindowType.Popup)
		{
			vbox = new VBox ();
			HBox box = new HBox ();
			list = new ListWidget (this);
			list.SelectionChanged += new EventHandler (OnSelectionChanged);
			list.ScrollEvent += new ScrollEventHandler (OnScrolled);
			
			box.PackStart (list, true, true, 0);
			this.BorderWidth = 1;

			scrollbar = new VScrollbar (null);
			scrollbar.ValueChanged += new EventHandler (OnScrollChanged);
			box.PackStart (scrollbar, false, false, 0);
			list.ButtonPressEvent += delegate(object o, ButtonPressEventArgs args) {
				if (args.Event.Button == 1 && args.Event.Type == Gdk.EventType.TwoButtonPress)
					DoubleClick ();
			};
			list.WordsFiltered += delegate {
				UpdateScrollBar ();
			};
			list.SizeAllocated += delegate {
				UpdateScrollBar ();
			};
			vbox.PackStart (box, true, true, 0);
			Add (vbox);
			this.AutoSelect = true;
			this.TypeHint = WindowTypeHint.Menu;
		}

		protected virtual void DoubleClick ()
		{

		}
		
		public void ShowFooter (Widget w)
		{
			HideFooter ();
			vbox.PackStart (w, false, false, 0);
			footer = w;
		}

		public void HideFooter ()
		{
			if (footer != null) {
				vbox.Remove (footer);
				footer = null;
			}
		}

		protected void Reset (bool clearWord)
		{
			if (clearWord) {
				word = new StringBuilder ();
				curPos = 0;
			}

			list.Reset ();
			if (DataProvider != null)
				ResetSizes ();
		}
		
		protected int curXPos, curYPos;
		
		public void ResetSizes ()
		{
			list.CompletionString = PartialWord;
			
			if (IsRealized && !Visible)
				Show ();
			
			int width = list.WidthRequest;
			int height = list.HeightRequest + (footer != null ? footer.Allocation.Height : 0);
			
			SetSizeRequest (width, height);
			if (IsRealized) 
				Resize (width, height);
		}

		void UpdateScrollBar ()
		{
			double pageSize = Math.Max (0, list.VisibleRows);
			double upper = Math.Max (0, list.filteredItems.Count - 1);
			scrollbar.Adjustment.SetBounds (0, upper, 1, pageSize, pageSize);
			if (pageSize >= upper) {
				this.scrollbar.Value = -1;
				this.scrollbar.Visible = false;
			} else {
				this.scrollbar.Value = list.Page;
				this.scrollbar.Visible = true;
			}
		}


		public IListDataProvider DataProvider {
			get;
			set;
		}

		public string CurrentCompletionText {
			get {
				if (list.SelectionIndex != -1 && list.AutoSelect)
					return DataProvider.GetCompletionText (list.SelectionIndex);
				return null;
			}
		}

		public int SelectionIndex {
			get { return list.SelectionIndex; }
		}
		
		public bool AutoSelect {
			get { return list.AutoSelect; }
			set { list.AutoSelect = value; }
		}
		
		public bool SelectionEnabled {
			get { return list.SelectionEnabled; }
		}
		
		public bool AutoCompleteEmptyMatch {
			get { return list.AutoCompleteEmptyMatch; }
			set { list.AutoCompleteEmptyMatch = value; }
		}
		
		public string DefaultCompletionString {
			get {
				return list.DefaultCompletionString;
			}
			set {
				list.DefaultCompletionString = value;
			}
		}
		
		public string PartialWord {
			get { return word.ToString (); }
			set {
				string newword = value;
				if (newword.Trim ().Length == 0)
					return;
				if (word.ToString () != newword) {
					word = new StringBuilder (newword);
					curPos = newword.Length;
					UpdateWordSelection ();
				}
			}
		}
	
		public string CurrentPartialWord {
			get {
				return !string.IsNullOrEmpty (PartialWord) ? PartialWord : DefaultCompletionString;
			}
		}

		public bool IsUniqueMatch {
			get {
/*				int pos = list.Selection + 1;
				if (DataProvider.ItemCount > pos && 
					DataProvider.GetText (pos).ToLower ().StartsWith (CurrentPartialWord.ToLower ()) || 
					!(DataProvider.GetText (list.Selection).ToLower ().StartsWith (CurrentPartialWord.ToLower ())))
					return false;
				*/
				return list.filteredItems.Count == 1;
			}
		}

		public ListWidget List {
			get { return list; }
		}

		public bool CompleteWithSpaceOrPunctuation {
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

		public KeyActions ProcessKey (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			switch (key) {
			case Gdk.Key.Home:
				if ((modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask)
					return KeyActions.Process;
				List.Selection = 0;
				return KeyActions.Ignore;
			case Gdk.Key.End:
				if ((modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask)
					return KeyActions.Process;
				List.Selection = List.filteredItems.Count - 1;
				return KeyActions.Ignore;
				
			case Gdk.Key.Up:
				if ((modifier & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (!SelectionEnabled /*&& !CompletionWindowManager.ForceSuggestionMode*/)
						AutoCompleteEmptyMatch = AutoSelect = true;
					if (!List.InCategoryMode) {
						List.InCategoryMode = true;
						return KeyActions.Ignore;
					}
					List.MoveToCategory (-1);
					return KeyActions.Ignore;
				}
				if (SelectionEnabled && list.filteredItems.Count < 2)
					return KeyActions.CloseWindow | KeyActions.Process;
				if (!SelectionEnabled /*&& !CompletionWindowManager.ForceSuggestionMode*/) {
					AutoCompleteEmptyMatch = AutoSelect = true;
				} else {
					list.MoveCursor (-1);
				}
				return KeyActions.Ignore;

			case Gdk.Key.Down:
				if ((modifier & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (!SelectionEnabled /*&& !CompletionWindowManager.ForceSuggestionMode*/)
						AutoCompleteEmptyMatch = AutoSelect = true;
					if (!List.InCategoryMode) {
						List.InCategoryMode = true;
						return KeyActions.Ignore;
					}
					List.MoveToCategory (1);
					return KeyActions.Ignore;
				}
				if (SelectionEnabled && list.filteredItems.Count < 2)
					return KeyActions.CloseWindow | KeyActions.Process;
				
				if (!SelectionEnabled /*&& !CompletionWindowManager.ForceSuggestionMode*/) {
					AutoCompleteEmptyMatch = AutoSelect = true;
				} else {
					list.MoveCursor (1);
				}
				return KeyActions.Ignore;

			case Gdk.Key.Page_Up:
				if ((modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask)
					return KeyActions.Process;
				if (list.filteredItems.Count < 2)
					return KeyActions.CloseWindow | KeyActions.Process;
				list.MoveCursor (-(list.VisibleRows - 1));
				return KeyActions.Ignore;

			case Gdk.Key.Page_Down:
				if ((modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask)
					return KeyActions.Process;
				if (list.filteredItems.Count < 2)
					return KeyActions.CloseWindow | KeyActions.Process;
				list.MoveCursor (list.VisibleRows - 1);
				return KeyActions.Ignore;

			case Gdk.Key.Left:
				//if (curPos == 0) return KeyActions.CloseWindow | KeyActions.Process;
				//curPos--;
				return KeyActions.Process;

			case Gdk.Key.BackSpace:
				if (curPos == 0 || (modifier & Gdk.ModifierType.ControlMask) != 0)
					return KeyActions.CloseWindow | KeyActions.Process;
				curPos--;
				word.Remove (curPos, 1);
				ResetSizes ();
				UpdateWordSelection ();
				if (word.Length == 0)
					return KeyActions.CloseWindow | KeyActions.Process;
				return KeyActions.Process;

			case Gdk.Key.Right:
				//if (curPos == word.Length) return KeyActions.CloseWindow | KeyActions.Process;
				//curPos++;
				return KeyActions.Process;

			case Gdk.Key.Caps_Lock:
			case Gdk.Key.Num_Lock:
			case Gdk.Key.Scroll_Lock:
				return KeyActions.Ignore;

			case Gdk.Key.Tab:
				//tab always completes current item even if selection is disabled
				if (!AutoSelect)
					AutoSelect = true;
				goto case Gdk.Key.Return;

			case Gdk.Key.Return:
			case Gdk.Key.ISO_Enter:
			case Gdk.Key.Key_3270_Enter:
			case Gdk.Key.KP_Enter:
				WasShiftPressed = (modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask;
				return (!list.AutoSelect ? KeyActions.Process : (KeyActions.Complete | KeyActions.Ignore)) | KeyActions.CloseWindow;

			case Gdk.Key.Escape:
				return KeyActions.CloseWindow | KeyActions.Ignore;

			case Gdk.Key.Control_L:
			case Gdk.Key.Control_R:
			case Gdk.Key.Alt_L:
			case Gdk.Key.Alt_R:
			case Gdk.Key.Shift_L:
			case Gdk.Key.Shift_R:
			case Gdk.Key.ISO_Level3_Shift:
				// AltGr
				return KeyActions.Process;
			}
			if (keyChar == '\0')
				return KeyActions.Process;

			if (keyChar == ' ' && (modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask)
				return KeyActions.CloseWindow | KeyActions.Process;
			
			//don't input letters/punctuation etc when non-shift modifiers are active
			bool nonShiftModifierActive = ((Gdk.ModifierType.ControlMask | Gdk.ModifierType.MetaMask
				| Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.SuperMask)
				& modifier) != 0;
			if (nonShiftModifierActive)
				return KeyActions.Ignore;
			const string commitChars = " <>()[]{}=";
			if (System.Char.IsLetterOrDigit (keyChar) || keyChar == '_') {
				word.Insert (curPos, keyChar);
				ResetSizes ();
				UpdateWordSelection ();
				curPos++;
				return KeyActions.Process;
			} else if (System.Char.IsPunctuation (keyChar) || commitChars.Contains (keyChar)) {
				//punctuation is only accepted if it actually matches an item in the list
				word.Insert (curPos, keyChar);

				bool hasMismatches;
				int match = FindMatchedEntry (CurrentPartialWord, out hasMismatches);
				if (match >= 0 && System.Char.IsPunctuation (keyChar)) {
					string text = DataProvider.GetCompletionText (FilteredItems [match]);
					if (!text.ToUpper ().StartsWith (word.ToString ().ToUpper ()))
						match =-1;	 
				}
				if (match >= 0 && !hasMismatches && keyChar != '<') {
					ResetSizes ();
					UpdateWordSelection ();
					curPos++;
					return KeyActions.Process;
				} else {
					word.Remove (curPos, 1);
				}
				
				if (CompleteWithSpaceOrPunctuation && list.SelectionEnabled)
					return KeyActions.Complete | KeyActions.Process | KeyActions.CloseWindow;
				return KeyActions.CloseWindow | KeyActions.Process;
			}

			return KeyActions.CloseWindow | KeyActions.Process;
		}

		public void UpdateWordSelection ()
		{
			SelectEntry (CurrentPartialWord);
		}

		//note: finds the full match, or the best partial match
		//returns -1 if there is no match at all
		
		class WordComparer : IComparer <KeyValuePair<int, string>>
		{
			string filterWord;
			StringMatcher matcher;

			public WordComparer (string filterWord)
			{
				this.filterWord = filterWord ?? "";
				matcher = CompletionMatcher.CreateCompletionMatcher (filterWord);
			}
			
			public int Compare (KeyValuePair<int, string> xpair, KeyValuePair<int, string> ypair)
			{
				string x = xpair.Value;
				string y = ypair.Value;
				int[] xMatches = matcher.GetMatch (x) ?? new int[0];
				int[] yMatches = matcher.GetMatch (y) ?? new int[0];
				if (xMatches.Length < yMatches.Length) 
					return 1;
				if (xMatches.Length > yMatches.Length) 
					return -1;
				
				int xExact = 0;
				int yExact = 0;
				for (int i = 0; i < filterWord.Length; i++) {
					if (i < xMatches.Length && filterWord[i] == x[xMatches[i]])
						xExact++;
					if (i < yMatches.Length && filterWord[i] == y[yMatches[i]])
						yExact++;
				}
				
				if (xExact < yExact)
					return 1;
				if (xExact > yExact)
					return -1;
				
				// favor words where the match starts sooner
				if (xMatches.Length > 0 && yMatches.Length > 0 && xMatches[0] != yMatches[0])
					return xMatches[0].CompareTo (yMatches[0]);
				
				int xIndex = xpair.Key;
				int yIndex = ypair.Key;
				
				if (x.Length == y.Length)
					return xIndex.CompareTo (yIndex);
				
				return x.Length.CompareTo (y.Length);
			}
		}
		
		protected int FindMatchedEntry (string partialWord, out bool hasMismatches)
		{
			// default - word with highest match rating in the list.
			hasMismatches = true;
			if (partialWord == null)
				return -1;
			
			int idx = -1;
			var matcher = CompletionMatcher.CreateCompletionMatcher (partialWord);
			
			string bestWord = null;
			int bestRank = int.MinValue;
			int bestIndex = 0;
			
			if (!string.IsNullOrEmpty (partialWord)) {
				for (int i = 0; i < list.filteredItems.Count; i++) {
					int index = list.filteredItems[i];
					string text = DataProvider.GetCompletionText (index);
					int rank;
					if (!matcher.CalcMatchRank (text, out rank))
						continue;
					if (rank > bestRank) {
						bestWord = text;
						bestRank = rank;
						bestIndex = i;
					}
				}
			}
			
			if (bestWord != null) {
				idx = bestIndex;
				hasMismatches = false;
				// exact match found.
				if (string.Compare (bestWord, partialWord ?? "", true) == 0) 
					return idx;
			}
			
			if (string.IsNullOrEmpty (partialWord) || partialWord.Length <= 2) {
				// Search for history matches.
				string historyWord;
				if (wordHistory.TryGetValue (partialWord, out historyWord)) {
					for (int xIndex = 0; xIndex < list.filteredItems.Count; xIndex++) {
						string currentWord = DataProvider.GetCompletionText (list.filteredItems[xIndex]);
						if (currentWord == historyWord) {
							idx = xIndex;
							break;
						}
					}
				}
			}
			return idx;
		}

		static Dictionary<string,string> wordHistory = new Dictionary<string,string> ();
		static List<string> partalWordHistory = new List<string> ();
		const int maxHistoryLength = 500;
		protected void AddWordToHistory (string partialWord, string word)
		{
			if (!wordHistory.ContainsKey (partialWord)) {
				wordHistory.Add (partialWord, word);
				partalWordHistory.Add (partialWord);
				while (partalWordHistory.Count > maxHistoryLength) {
					string first = partalWordHistory [0];
					partalWordHistory.RemoveAt (0);
					wordHistory.Remove (first);
				}
			} else {
				partalWordHistory.Remove (partialWord);
				partalWordHistory.Add (partialWord);
				wordHistory [partialWord] = word;
			}
		}
		public static void ClearHistory ()
		{
			wordHistory.Clear ();
			partalWordHistory.Clear ();
		}

		void SelectEntry (int n)
		{
			if (n >= 0)
				list.Selection = n;
		}

		public virtual void SelectEntry (string s)
		{
			list.FilterWords ();
			/* // disable this, because we select now the last selected entry by default (word history mode)
			//when the list is empty, disable the selection or users get annoyed by it accepting
			//the top entry automatically
			if (string.IsNullOrEmpty (s)) {
				ResetSizes ();
				list.Selection = 0;
				return;
			}*/
			bool hasMismatches;
			
			int matchedIndex = FindMatchedEntry (s, out hasMismatches);
			ResetSizes ();
			SelectEntry (matchedIndex);
		}

		void OnScrollChanged (object o, EventArgs args)
		{
			list.Page = (int)scrollbar.Value;
		}

		void OnScrolled (object o, ScrollEventArgs args)
		{
			switch (args.Event.Direction) {
			case ScrollDirection.Up:
				scrollbar.Value--; 
				break;
			case ScrollDirection.Down:
				scrollbar.Value++; 
				break;
			}
		}

		void OnSelectionChanged (object o, EventArgs args)
		{
			scrollbar.Value = list.Page;
			OnSelectionChanged ();
		}

		protected virtual void OnSelectionChanged ()
		{
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			base.OnExposeEvent (args);
			
			int winWidth, winHeight;
			this.GetSize (out winWidth, out winHeight);
			this.GdkWindow.DrawRectangle (this.Style.ForegroundGC (StateType.Insensitive), false, 0, 0, winWidth - 1, winHeight - 1);
			return true;
		}
		
		public int TextOffset {
			get { return list.TextOffset + (int)this.BorderWidth; }
		}
	}

	public interface IListDataProvider
	{
		int ItemCount { get; }
		string GetText (int n);
		string GetMarkup (int n);
		CompletionCategory GetCompletionCategory (int n);
		bool HasMarkup (int n);
		string GetCompletionText (int n);
		string GetDescription (int n);
		Gdk.Pixbuf GetIcon (int n);
	}
}

