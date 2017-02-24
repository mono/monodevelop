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
using Mono.TextEditor;

namespace Mono.TextEditor.PopupWindow
{
	[Flags]
	enum ListWindowKeyAction { 
		Process = 1, 
		Ignore = 2, 
		CloseWindow = 4, 
		Complete = 8 
	}
	
	class ListWindow<T> : Gtk.Window
	{
		ScrolledWindow scrollbar;
		ListWidget<T> list;
		IListDataProvider<T> provider;
		Widget footer;
		VBox vbox;
		
		StringBuilder word;
		int curPos;
		
		public ListWindow () : base (Gtk.WindowType.Popup)
		{
			vbox = new VBox ();
			
			HBox box = new HBox ();
			list = new ListWidget<T> (this);
			list.SelectionChanged += new EventHandler (OnSelectionChanged);
			this.BorderWidth = 0;
			
			scrollbar = new Gtk.ScrolledWindow ();
			scrollbar.Child = list;
			box.PackStart (scrollbar, true, true, 0);
			list.ButtonPressEvent += delegate (object o, ButtonPressEventArgs args) {
				if (args.Event.Button == 1 && args.Event.Type == Gdk.EventType.TwoButtonPress)
					OnDoubleClicked (EventArgs.Empty);
			};
			vbox.PackStart (box, true, true, 0);
			Add (vbox);
			
			this.TypeHint = WindowTypeHint.Menu;
		}

		public event EventHandler DoubleClicked;

		protected virtual void OnDoubleClicked (EventArgs e)
		{
			EventHandler handler = this.DoubleClicked;
			if (handler != null)
				handler (this, e);
		}

		public new void Show ()
		{
			this.ShowAll ();
			ResetSizes ();
		}
		
		public void SetHeader (Widget w)
		{
			vbox.PackStart (w, false, false, 0);
			vbox.ReorderChild (w, 0);
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
			this.Resize(this.list.WidthRequest, this.list.HeightRequest);
		}
		
		public IListDataProvider<T> DataProvider
		{
			get { return provider; }
			set { provider = value; }
		}
		
		public T CurrentItem {
			get { 
				return (list.Selection != -1 && !SelectionDisabled) ? provider[list.Selection] : default(T);
			}
		}
		
		public int Selection {
			get {
				return list.Selection;
			}
		}
		
		public bool SelectionDisabled {
			get {
				return list.SelectionDisabled; 
			}
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
		
		public bool IsUniqueMatch {
			get {
				int pos = list.Selection + 1;
				if (provider.Count > pos && provider.GetText (pos).StartsWith (PartialWord, StringComparison.OrdinalIgnoreCase)
				    || !(provider.GetText (list.Selection).StartsWith (PartialWord, StringComparison.OrdinalIgnoreCase)))
					return false;
				
				return true;	
			}
		}
		
		internal ListWidget<T> List
		{
			get { return list; }
		}
		
		public ListWindowKeyAction ProcessKey (Gdk.Key key, Gdk.ModifierType modifier)
		{
			switch (key)
			{
				case Gdk.Key.Up:
					if (list.SelectionDisabled)
						list.SelectionDisabled = false;
					else
						list.Selection --;
					return ListWindowKeyAction.Ignore;
					
				case Gdk.Key.Down:
					if (list.SelectionDisabled)
						list.SelectionDisabled = false;
					else
						list.Selection ++;
					return ListWindowKeyAction.Ignore;
					
				case Gdk.Key.Page_Up:
					list.Selection -= list.VisibleRows - 1;
					return ListWindowKeyAction.Ignore;
					
				case Gdk.Key.Page_Down:
					list.Selection += list.VisibleRows - 1;
					return ListWindowKeyAction.Ignore;
					
				case Gdk.Key.Left:
					//if (curPos == 0) return KeyAction.CloseWindow | KeyAction.Process;
					//curPos--;
					return ListWindowKeyAction.Process;
					
				case Gdk.Key.BackSpace:
					if (curPos == 0 || (modifier & Gdk.ModifierType.ControlMask) != 0)
						return ListWindowKeyAction.CloseWindow | ListWindowKeyAction.Process;
					curPos--;
					word.Remove (curPos, 1);
					UpdateWordSelection ();
					return ListWindowKeyAction.Process;
					
				case Gdk.Key.Right:
					//if (curPos == word.Length) return KeyAction.CloseWindow | KeyAction.Process;
					//curPos++;
					return ListWindowKeyAction.Process;
				
				case Gdk.Key.Caps_Lock:
				case Gdk.Key.Num_Lock:
				case Gdk.Key.Scroll_Lock:
					return ListWindowKeyAction.Ignore;
					
				case Gdk.Key.Return:
				case Gdk.Key.ISO_Enter:
				case Gdk.Key.Key_3270_Enter:
				case Gdk.Key.KP_Enter:
					return (list.SelectionDisabled? ListWindowKeyAction.Process : (ListWindowKeyAction.Complete | ListWindowKeyAction.Ignore))
						| ListWindowKeyAction.CloseWindow;
				
				case Gdk.Key.Escape:
					return ListWindowKeyAction.CloseWindow | ListWindowKeyAction.Ignore;
				
				case Gdk.Key.Home:
				case Gdk.Key.End:
					return ListWindowKeyAction.CloseWindow | ListWindowKeyAction.Process;
					
				case Gdk.Key.Control_L:
				case Gdk.Key.Control_R:
				case Gdk.Key.Alt_L:
				case Gdk.Key.Alt_R:
				case Gdk.Key.Shift_L:
				case Gdk.Key.Shift_R:
				case Gdk.Key.ISO_Level3_Shift:	// AltGr
					return ListWindowKeyAction.Process;
			}
			
			
			return ListWindowKeyAction.CloseWindow | ListWindowKeyAction.Process;
		}
		
		void UpdateWordSelection ()
		{
			SelectEntry (word.ToString ());
		}
		
		//note: finds the full match, or the best partial match
		//returns -1 if there is no match at all
		int findMatchedEntry (string s, out bool hasMismatches)
		{
			int max = (provider == null ? 0 : provider.Count);
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
					int minLength = System.Math.Min (s.Length, txt.Length);
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
		/*
		void OnScrollChanged (object o, EventArgs args)
		{
			list.Page = (int) scrollbar.Value;
		}

		void OnScrolled (object o, ScrollEventArgs args)
		{
			if (!scrollbar.Visible)
				return;
			
			var adj = scrollbar.Adjustment;
			var alloc = Allocation;
			
			//This widget is a special case because it's always aligned to items as it scrolls.
			//Although this means we can't use the pixel deltas for true smooth scrolling, we 
			//can still make use of the effective scrolling velocity by basing the calculation 
			//on pixels and rounding to the nearest item.
			
			double dx, dy;
			args.Event.GetPageScrollPixelDeltas (0, alloc.Height, out dx, out dy);
			if (dy == 0)
				return;
			
			var itemDelta = dy / (alloc.Height / adj.PageSize);
			double discreteItemDelta = System.Math.Round (itemDelta);
			if (discreteItemDelta == 0.0 && dy != 0.0)
				discreteItemDelta = dy > 0? 1.0 : -1.0;
			
			adj.AddValueClamped (discreteItemDelta);
			args.RetVal = true;
		}
		*/
		void OnSelectionChanged (object o, EventArgs args)
		{
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
		
		public int TextOffset {
			get { return list.TextOffset + (int) this.BorderWidth; }
		}
	}

}