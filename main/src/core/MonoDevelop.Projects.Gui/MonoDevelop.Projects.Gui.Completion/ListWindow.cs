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
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Gui.Completion
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

	internal class ListWindow : Gtk.Window
	{
		internal VScrollbar scrollbar;
		ListWidget list;
		IListDataProvider provider;
		Widget footer;
		VBox vbox;

		StringBuilder word;
		int curPos;

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
			vbox.PackStart (box, true, true, 0);
			Add (vbox);
			this.AutoSelect = true;
			this.TypeHint = WindowTypeHint.Menu;
		}

		protected virtual void DoubleClick ()
		{

		}

		public new void Show ()
		{
			list.filteredItems.Clear ();
			this.ShowAll ();
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
			if (provider == null)
				return;

			ResetSizes ();
		}
		protected int curXPos, curYPos;

		protected void ResetSizes ()
		{
			if (!IsRealized)
				return;
			list.CompletionString = word.ToString ();
			if (list.filteredItems.Count == 0 && !list.PreviewCompletionString) {
				Hide ();
			} else {
				if (!Visible)
					Show ();
			}
			double pageSize = Math.Max (0, list.VisibleRows - 1);
			double upper = Math.Max (0, list.filteredItems.Count - 1);
			scrollbar.Adjustment.SetBounds (0, upper, 1, pageSize, pageSize);
			if (pageSize >= upper) {
				this.scrollbar.Hide ();
			} else {
				scrollbar.Value = list.Page;
				this.scrollbar.Show ();
			}
			this.Resize (this.list.WidthRequest, this.list.HeightRequest);
		}

		public IListDataProvider DataProvider {
			get { return provider; }
			set { provider = value; }
		}

		public string CompleteWord {
			get {
				if (list.SelectionIndex != -1 && !SelectionDisabled)
					return provider.GetCompletionText (list.SelectionIndex);
				else
					return null;
			}
		}

		public int Selection {
			get { return list.Selection; }
		}

		public virtual bool SelectionDisabled {
			get { return list.SelectionDisabled; }
			set { list.SelectionDisabled = value; }
		}

		public bool AutoSelect { get; set; }

		public string PartialWord {
			get { return word.ToString (); }
			set {
				string newword = value;
				if (newword.Trim ().Length == 0)
					return;

				word = new StringBuilder (newword);
				curPos = newword.Length;
				UpdateWordSelection ();
			}
		}

		public bool IsUniqueMatch {
			get {
				int pos = list.Selection + 1;
				if (provider.ItemCount > pos && provider.GetText (pos).ToLower ().StartsWith (PartialWord.ToLower ()) || !(provider.GetText (list.Selection).ToLower ().StartsWith (PartialWord.ToLower ())))
					return false;

				return true;
			}
		}

		protected ListWidget List {
			get { return list; }
		}

		bool CompleteWithSpaceOrPunctuation {
			get { return MonoDevelop.Core.PropertyService.Get ("CompleteWithSpaceOrPunctuation", true); }
		}

		public KeyActions ProcessKey (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			switch (key) {
			case Gdk.Key.Up:
				if (list.SelectionDisabled)
					list.SelectionDisabled = false;
				else
					list.Selection--;
				return KeyActions.Ignore;

			case Gdk.Key.Down:
				if (list.SelectionDisabled)
					list.SelectionDisabled = false;
				else
					list.Selection++;
				return KeyActions.Ignore;

			case Gdk.Key.Page_Up:
				list.Selection -= list.VisibleRows - 1;
				return KeyActions.Ignore;

			case Gdk.Key.Page_Down:
				list.Selection += list.VisibleRows - 1;
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
				if (list.SelectionDisabled)
					list.SelectionDisabled = false;
				goto case Gdk.Key.Return;

			case Gdk.Key.Return:
			case Gdk.Key.ISO_Enter:
			case Gdk.Key.Key_3270_Enter:
			case Gdk.Key.KP_Enter:
				return (list.SelectionDisabled ? KeyActions.Process : (KeyActions.Complete | KeyActions.Ignore)) | KeyActions.CloseWindow;

			case Gdk.Key.Escape:
				return KeyActions.CloseWindow | KeyActions.Ignore;

			case Gdk.Key.Home:
			case Gdk.Key.End:
				return KeyActions.CloseWindow | KeyActions.Process;

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

			if (System.Char.IsLetterOrDigit (keyChar) || keyChar == '_') {
				word.Insert (curPos, keyChar);
				ResetSizes ();
				SelectionDisabled = false;
				curPos++;
				if (!SelectionDisabled || AutoSelect)
					UpdateWordSelection ();
				return KeyActions.Process;
			} else if (System.Char.IsPunctuation (keyChar) || keyChar == ' ' || keyChar == '<') {
				//punctuation is only accepted if it actually matches an item in the list
				word.Insert (curPos, keyChar);
				ResetSizes ();
				SelectionDisabled = false;
				bool hasMismatches;
				int match = FindMatchedEntry (word.ToString (), out hasMismatches);

				if (match >= 0 && !hasMismatches && keyChar != '<') {
					curPos++;
					if (!SelectionDisabled || AutoSelect)
						SelectEntry (match);
					return KeyActions.Process;
				} else if (CompleteWithSpaceOrPunctuation) {
					word.Remove (curPos, 1);
					SelectionDisabled = word.Length == 0;
					return KeyActions.Complete | KeyActions.Process | KeyActions.CloseWindow;
				} else {
					return KeyActions.CloseWindow | KeyActions.Process;
				}
			}

			return KeyActions.CloseWindow | KeyActions.Process;
		}

		void UpdateWordSelection ()
		{
			SelectEntry (word.ToString ());
		}

		//note: finds the full match, or the best partial match
		//returns -1 if there is no match at all
		protected int FindMatchedEntry (string s, out bool hasMismatches)
		{
			// Search for exact matches.
			for (int i = 0; i < list.filteredItems.Count; i++) {
				string txt = provider.GetText (list.filteredItems[i]);
				if (txt == s) {
					hasMismatches = false;
					return i;
				}
			}

			// default - word with highest match rating in the list.
			hasMismatches = true;
			int idx = -1;
			int curRating = -1;
			for (int n = 0; n < list.filteredItems.Count; n++) {
				string txt = provider.GetText (list.filteredItems[n]);
				int rating = ListWidget.MatchRating (s, txt);
				if (curRating < rating) {
					curRating = rating;
					idx = n;
					hasMismatches = false;
				}
			}

			// Search for history matches.
			string historyWord = null;
			for (int i = wordHistory.Count - 1; i >= 0; i--) {
				string word = wordHistory[i];
				if (ListWidget.Matches (s, word)) {
					historyWord = word;
					break;
				}
			}

			if (historyWord != null) {
				for (int n = 0; n < list.filteredItems.Count; n++) {
					string txt = provider.GetText (list.filteredItems[n]);
					if (txt == historyWord) {
						if (curRating <= ListWidget.MatchRating (s, txt)) {
							hasMismatches = false;
							return n;
						}
						break;
					}
				}
			}
			return idx;
		}

		List<string> wordHistory = new List<string> ();
		const int maxHistoryLength = 500;
		protected void AddWordToHistory (string word)
		{
			if (!wordHistory.Contains (word)) {
				wordHistory.Add (word);
				while (wordHistory.Count > maxHistoryLength)
					wordHistory.RemoveAt (0);
			} else {
				wordHistory.Remove (word);
				wordHistory.Add (word);
			}
		}

		void SelectEntry (int n)
		{
			if (n < 0) {
				list.SelectionDisabled = true;
			} else {
				list.Selection = n;
			}
		}

		public virtual void SelectEntry (string s)
		{
			list.CompletionString = s;
			//when the list is empty, disable the selection or users get annoyed by it accepting
			//the top entry automatically
			if (string.IsNullOrEmpty (s)) {
				ResetSizes ();
				list.Selection = 0;
				list.SelectionDisabled = true;
				return;
			}

			bool hasMismatches;
			int n = FindMatchedEntry (s, out hasMismatches);
			ResetSizes ();
			SelectEntry (n);
			if (hasMismatches)
				list.SelectionDisabled = true;
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
			return false;
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
		bool HasMarkup (int n);
		string GetCompletionText (int n);
		Gdk.Pixbuf GetIcon (int n);
	}
}

