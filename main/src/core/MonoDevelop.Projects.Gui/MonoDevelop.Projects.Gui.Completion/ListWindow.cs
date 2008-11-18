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
using System.Text;

namespace MonoDevelop.Projects.Gui.Completion
{
	internal class ListWindow: Gtk.Window
	{
		VScrollbar scrollbar;
		ListWidget list;
		IListDataProvider provider;
		Widget footer;
		VBox vbox;
		
		StringBuilder word;
		int curPos;
		
		[Flags]
		public enum KeyAction { Process=1, Ignore=2, CloseWindow=4, Complete=8 } 

		public ListWindow (): base (Gtk.WindowType.Popup)
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
			
			this.TypeHint = WindowTypeHint.Menu;
		}
		protected virtual void DoubleClick ()
		{
			
		}
		
		public new void Show ()
		{
			this.ShowAll ();
			ResetSizes ();
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
			
			if (IsRealized) {
				ResetSizes ();
			}
		}
		
		void ResetSizes ()
		{
			scrollbar.Adjustment.Lower = 0;
			scrollbar.Adjustment.Upper = Math.Max(0, provider.ItemCount - list.VisibleRows);
			scrollbar.Adjustment.PageIncrement = list.VisibleRows - 1;
			scrollbar.Adjustment.StepIncrement = 1;
			
			if (list.VisibleRows >= provider.ItemCount) {
				this.scrollbar.Hide();
			}

			this.Resize(this.list.WidthRequest, this.list.HeightRequest);
		}
		
		public IListDataProvider DataProvider
		{
			get { return provider; }
			set { provider = value; }
		}
		
		public string CompleteWord
		{
			get { 
				if (list.Selection != -1 && !SelectionDisabled)
					return provider.GetCompletionText (list.Selection);
				else
					return null;
			}
		}
		
		public int Selection {
			get { return list.Selection; }
		}
		
		public bool SelectionDisabled {
			get { return list.SelectionDisabled; }
		}
		
		public string PartialWord
		{
			get { return word.ToString (); }
			set
			{
				string newword = value;
				if (newword.Trim ().Length == 0)
					return;
				
				word = new StringBuilder (newword);
				curPos = newword.Length;
				UpdateWordSelection ();
			}
		}
		
		public bool IsUniqueMatch
		{
			get
			{
				int pos = list.Selection + 1;
				if (provider.ItemCount > pos && provider.GetText (pos).ToLower ().StartsWith (PartialWord.ToLower ())
				    || !(provider.GetText (list.Selection).ToLower ().StartsWith (PartialWord.ToLower ())))
					return false;
				
				return true;	
			}
		}
		
		protected ListWidget List
		{
			get { return list; }
		}
		
		public KeyAction ProcessKey (Gdk.Key key, Gdk.ModifierType modifier)
		{
			switch (key)
			{
				case Gdk.Key.Up:
					if (list.SelectionDisabled)
						list.SelectionDisabled = false;
					else
						list.Selection --;
					return KeyAction.Ignore;
					
				case Gdk.Key.Down:
					if (list.SelectionDisabled)
						list.SelectionDisabled = false;
					else
						list.Selection ++;
					return KeyAction.Ignore;
					
				case Gdk.Key.Page_Up:
					list.Selection -= list.VisibleRows - 1;
					return KeyAction.Ignore;
					
				case Gdk.Key.Page_Down:
					list.Selection += list.VisibleRows - 1;
					return KeyAction.Ignore;
					
				case Gdk.Key.Left:
					//if (curPos == 0) return KeyAction.CloseWindow | KeyAction.Process;
					//curPos--;
					return KeyAction.Process;
					
				case Gdk.Key.BackSpace:
					if (curPos == 0 || (modifier & Gdk.ModifierType.ControlMask) != 0)
						return KeyAction.CloseWindow | KeyAction.Process;
					curPos--;
					word.Remove (curPos, 1);
					UpdateWordSelection ();
					return KeyAction.Process;
					
				case Gdk.Key.Right:
					//if (curPos == word.Length) return KeyAction.CloseWindow | KeyAction.Process;
					//curPos++;
					return KeyAction.Process;
				
				case Gdk.Key.Caps_Lock:
				case Gdk.Key.Num_Lock:
				case Gdk.Key.Scroll_Lock:
					return KeyAction.Ignore;
					
				case Gdk.Key.Tab:
					//tab always completes current item even if selection is disabled
					if (list.SelectionDisabled)
						list.SelectionDisabled = false;
					goto case Gdk.Key.Return;
				
				case Gdk.Key.Return:
				case Gdk.Key.ISO_Enter:
				case Gdk.Key.Key_3270_Enter:
				case Gdk.Key.KP_Enter:
					return KeyAction.Complete | KeyAction.Ignore | KeyAction.CloseWindow;
					
				case Gdk.Key.Escape:
					return KeyAction.CloseWindow | KeyAction.Ignore;
				
				case Gdk.Key.Home:
				case Gdk.Key.End:
					return KeyAction.CloseWindow | KeyAction.Process;
					
				case Gdk.Key.Control_L:
				case Gdk.Key.Control_R:
				case Gdk.Key.Alt_L:
				case Gdk.Key.Alt_R:
				case Gdk.Key.Shift_L:
				case Gdk.Key.Shift_R:
				case Gdk.Key.ISO_Level3_Shift:	// AltGr
					return KeyAction.Process;
			}
			
			char c = (char)(uint)key;
			
			if (System.Char.IsLetterOrDigit (c) || c == '_') {
				word.Insert (curPos, c);
				curPos++;
				UpdateWordSelection ();
				return KeyAction.Process;
			} else if (System.Char.IsPunctuation (c) || c == ' ' || c == '<') {
				//punctuation is only accepted if it actually matches an item in the list
				word.Insert (curPos, c);
				bool hasMismatches;
				int match = findMatchedEntry (word.ToString (), out hasMismatches);
				if (match >= 0 && !hasMismatches && c != '<') {
					curPos++;
					SelectEntry (match);
					return KeyAction.Process;
				} else {
					word.Remove (curPos, 1);
					return c == '<' ? KeyAction.Complete | KeyAction.Ignore | KeyAction.CloseWindow : 
						              KeyAction.Complete | KeyAction.Process | KeyAction.CloseWindow;
				}
			}
			
			return KeyAction.CloseWindow | KeyAction.Process;
		}
		
		void UpdateWordSelection ()
		{
			SelectEntry (word.ToString ());
		}
		
		//note: finds the full match, or the best partial match
		//returns -1 if there is no match at all
		int findMatchedEntry (string s, out bool hasMismatches)
		{
			int max = (provider == null ? 0 : provider.ItemCount);
			string sLower = s.ToLower ();
			
			int bestMatch = -1;
			int bestMatchLength = 0;
			for (int n=0; n<max; n++) 
			{
				string txt = provider.GetText (n);
				if (txt.StartsWith (s)) {
					hasMismatches = false;
					return n;
				} else {
					//try to match as many characters at the beginning of the words as possible
					int matchLength = 0;
					int minLength = Math.Min (s.Length, txt.Length);
					while (matchLength < minLength && char.ToLower (txt[matchLength]) == sLower [matchLength]) {
						matchLength++;
					}
					if (matchLength > bestMatchLength) {
						bestMatchLength = matchLength;
						bestMatch = n;
					}
				}
			}
			hasMismatches = (bestMatch > -1) && (bestMatchLength != s.Length);
			return bestMatch;
		}
		
		void SelectEntry (int n)
		{
			if (n < 0) {
				list.SelectionDisabled = true;
			} else {
				list.Selection = n;
			}
		}
		
		public void SelectEntry (string s)
		{
			//when the list is empty, disable the selection or users get annoyed by it accepting
			//the top entry automatically
			if (string.IsNullOrEmpty (s)) {
				list.Selection = 0;
				list.SelectionDisabled = true;
				return;
			}
				
			bool hasMismatches;
			int n = findMatchedEntry (s, out hasMismatches);
			SelectEntry (n);
			if (hasMismatches)
				list.SelectionDisabled = true;
		}
		
		void OnScrollChanged (object o, EventArgs args)
		{
			list.Page = (int) scrollbar.Value;
		}

		void OnScrolled (object o, ScrollEventArgs args)
		{
			if (args.Event.Direction == Gdk.ScrollDirection.Up)
				scrollbar.Value --;
			else if (args.Event.Direction == Gdk.ScrollDirection.Down)
				scrollbar.Value ++;
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
			this.GdkWindow.DrawRectangle (this.Style.ForegroundGC (StateType.Insensitive), false, 0, 0, winWidth-1, winHeight-1);
			return false;
		}		
	}

	internal class ListWidget: Gtk.DrawingArea
	{
		int margin = 0;
		int padding = 4;
		int listWidth = 300;
		
		Pango.Layout layout;
		ListWindow win;
		int selection = 0;
		int page = 0;
		int visibleRows = -1;
		int rowHeight;
		bool buttonPressed;
		bool disableSelection;

		public event EventHandler SelectionChanged;
				
		public ListWidget (ListWindow win)
		{
			this.win = win;
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask; 
		}
		
		public void Reset ()
		{
			if (win.DataProvider == null) {
				selection = -1;
				return;
			}
			
			if (win.DataProvider.ItemCount == 0)
				selection = -1;
			else
				selection = 0;

			page = 0;
			disableSelection = false;
			if (IsRealized) {
				UpdateStyle ();
				QueueDraw ();
			}
			if (SelectionChanged != null) SelectionChanged (this, EventArgs.Empty);
		}
		
		public int Selection
		{
			get {
				return selection;
			}
			
			set {
				if (value < 0)
					value = 0;
				if (value >= win.DataProvider.ItemCount)
					value = win.DataProvider.ItemCount - 1;
				
				if (value != selection) 
				{
					selection = value;
					UpdatePage ();
					
					if (SelectionChanged != null)
						SelectionChanged (this, EventArgs.Empty);
				}
				
				if (disableSelection)
					disableSelection = false;

				this.QueueDraw ();
			}
		}
		
		void UpdatePage ()
		{
			if (!IsRealized) {
				page = 0;
				return;
			}
			
			if (selection < page || selection >= page + VisibleRows) {
				page = selection - (VisibleRows / 2);
				if (page < 0) page = 0;
			}
		}
		
		public bool SelectionDisabled
		{
			get { return disableSelection; }
			
			set {
				disableSelection = value; 
				this.QueueDraw ();
			}
		}
		
		public int Page
		{
			get { 
				return page; 
			}
			
			set {
				page = value;
				this.QueueDraw ();
			}
		}
		
		protected override bool OnButtonPressEvent (EventButton e)
		{
			Selection = GetRowByPosition ((int) e.Y);
			buttonPressed = true;
			return base.OnButtonPressEvent (e);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton e)
		{
			buttonPressed = false;
			return base.OnButtonReleaseEvent (e);
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion e)
		{
			if (!buttonPressed)
				return base.OnMotionNotifyEvent (e);
			
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			
	/*		int ypos = (int) e.Y;
			if (ypos < 0) {
			}
			else if (ypos >= winHeight) {
			}
			else
	*/			Selection = GetRowByPosition ((int) e.Y);
			
			return true;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			base.OnExposeEvent (args);
			DrawList ();
	  		return true;
		}

		void DrawList ()
		{
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			
			int ypos = margin;
			int lineWidth = winWidth - margin*2;
			int xpos = margin + padding;
				
			int n = 0;
			while (ypos < winHeight - margin && (page + n) < win.DataProvider.ItemCount)
			{
				bool hasMarkup = win.DataProvider.HasMarkup (page + n);
				
				if (hasMarkup)
					layout.SetMarkup (win.DataProvider.GetMarkup (page + n) ?? "&lt;null&gt;");
				else
					layout.SetText (win.DataProvider.GetText (page + n) ?? "<null>");
				
				Gdk.Pixbuf icon = win.DataProvider.GetIcon (page + n);
				int iconHeight = icon != null? icon.Height : 24;
				int iconWidth = icon != null? icon.Width : 24;
				
				int wi, he, typos, iypos;
				layout.GetPixelSize (out wi, out he);
				typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
				iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
				
				if (page + n == selection) {
					if (!disableSelection) {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected),
						                              true, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Selected),
							                           xpos + iconWidth + 2, typos, layout);
					}
					else {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected),
						                              false, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), 
						                           xpos + iconWidth + 2, typos, layout);
					}
				}
				else
					this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal),
					                           xpos + iconWidth + 2, typos, layout);
				
				if (icon != null)
					this.GdkWindow.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), icon, 0, 0,
					                           xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
				
				ypos += rowHeight;
				n++;
				
				//reset the markup or it carries over to the next SetText
				if (hasMarkup)
					layout.SetMarkup (string.Empty);
			}
		}
		
		int GetRowByPosition (int ypos)
		{
			if (visibleRows == -1) CalcVisibleRows ();
			return page + (ypos-margin) / rowHeight;
		}
		
		public Gdk.Rectangle GetRowArea (int row)
		{
			row -= page;
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			
			return new Gdk.Rectangle (margin, margin + rowHeight * row, winWidth, rowHeight);
		}
		
		public int VisibleRows
		{
			get {
				if (visibleRows == -1) CalcVisibleRows ();
				return visibleRows;
			}
		}
		
		void CalcVisibleRows ()
		{
			int winHeight = 200;
			int lvWidth, lvHeight;
			int rowWidth;
			
			this.GetSizeRequest (out lvWidth, out lvHeight);

			layout.GetPixelSize (out rowWidth, out rowHeight);
			rowHeight += padding;
			visibleRows = (winHeight + padding - margin * 2) / rowHeight;
			
			int newHeight;

			if (this.win.DataProvider.ItemCount > this.visibleRows)
				newHeight = (rowHeight * visibleRows) + margin * 2;
			else
				newHeight = (rowHeight * this.win.DataProvider.ItemCount) + margin * 2;
			
			if (lvWidth != listWidth || lvHeight != newHeight)
				this.SetSizeRequest (listWidth, newHeight);
		} 

		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateStyle ();
			UpdatePage ();
		}
		
		void UpdateStyle ()
		{
			this.GdkWindow.Background = this.Style.Base (StateType.Normal);
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			
			FontDescription des = this.Style.FontDescription.Copy();
			layout.FontDescription = des;
			CalcVisibleRows ();
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

