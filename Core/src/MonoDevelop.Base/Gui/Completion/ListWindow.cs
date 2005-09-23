using Gtk;
using Gdk;
using Pango;
using System;
using System.Text;

namespace MonoDevelop.Gui.Completion
{
	public class ListWindow: Gtk.Window
	{
		VScrollbar scrollbar;
		ListWidget list;
		IListDataProvider provider;
		
		StringBuilder word;
		int curPos;
		
		[Flags]
		public enum KeyAction { Process=1, Ignore=2, CloseWindow=4, Complete=8 } 

		public ListWindow (): base (Gtk.WindowType.Popup)
		{
			HBox box = new HBox ();
			
			list = new ListWidget (this);
			list.SelectionChanged += new EventHandler (OnSelectionChanged);
			list.ScrollEvent += new ScrollEventHandler (OnScrolled);
			box.PackStart (list, true, true, 0);
			this.BorderWidth = 1;
			
			scrollbar = new VScrollbar (null);
			scrollbar.ValueChanged += new EventHandler (OnScrollChanged); 
			box.PackStart (scrollbar, false, false, 0);
			
			Add (box);
			this.TypeHint = WindowTypeHint.Menu;
		}
		
		public new void Show ()
		{
			this.ShowAll ();
			Reset ();
		}
		
		public void Reset ()
		{
			word = new StringBuilder ();
			curPos = 0;
			list.Reset ();

			if (list.VisibleRows >= provider.ItemCount) {
				this.scrollbar.Hide();
			}
			else {
				scrollbar.Adjustment.Lower = 0;
				scrollbar.Adjustment.Upper = provider.ItemCount - list.VisibleRows;
				scrollbar.Adjustment.PageIncrement = list.VisibleRows - 1;
				scrollbar.Adjustment.StepIncrement = 1;
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
			get { return provider.GetText (list.Selection);	}
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
				if (provider.ItemCount > pos && provider.GetText (pos).ToLower ().StartsWith (PartialWord.ToLower ()) || !(provider.GetText (list.Selection).ToLower ().StartsWith (PartialWord.ToLower ())))
					return false;
				
				return true;	
			}
		}
		
		protected ListWidget List
		{
			get { return list; }
		}
		
		public KeyAction ProcessKey (EventKey e)
		{
			switch (e.Key)
			{
				case Gdk.Key.Up:
					list.Selection --;
					return KeyAction.Ignore;
					
				case Gdk.Key.Down:
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
					if (curPos == 0) return KeyAction.CloseWindow | KeyAction.Process;
					curPos--;
					word.Remove (curPos, 1);
					UpdateWordSelection ();
					return KeyAction.Process;
					
				case Gdk.Key.Right:
					//if (curPos == word.Length) return KeyAction.CloseWindow | KeyAction.Process;
					//curPos++;
					return KeyAction.Process;
					
				case Gdk.Key.Tab:
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
			
			char c = (char)e.KeyValue;
			
			if (System.Char.IsLetterOrDigit (c) || c == '_') {
				word.Insert (curPos++, c);
				UpdateWordSelection ();
				return KeyAction.Process;
			}
			else if ((System.Char.IsPunctuation (c) || c == ' ') && !list.SelectionDisabled) {
				return KeyAction.Complete | KeyAction.Process | KeyAction.CloseWindow;
			}
			
			return KeyAction.CloseWindow | KeyAction.Process;
		}
		
		void UpdateWordSelection ()
		{
			string s = word.ToString ();
			int max = (provider == null ? 0 : provider.ItemCount);
			
			int bestMatch = -1;
			for (int n=0; n<max; n++) 
			{
				string txt = provider.GetText (n);
				if (txt.StartsWith (s)) {
					list.Selection = n;
					return;
				}
				else if (bestMatch == -1 && txt.ToLower().StartsWith (s.ToLower()))
					bestMatch = n;
			}
			
			if (bestMatch != -1) {
				list.Selection = bestMatch;
				return;
			}
			
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

	public class ListWidget: Gtk.DrawingArea
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
			selection = 0;
			page = 0;
			disableSelection = false;
			UpdateStyle ();
			QueueDraw ();
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
				else if (value >= win.DataProvider.ItemCount)
					value = win.DataProvider.ItemCount - 1;
					
				if (value != selection) 
				{
					selection = value;
						
					if (selection < page)
						page = selection;
					else if (selection >= page + VisibleRows) {
						page = selection - VisibleRows + 1;
						if (page < 0) page = 0;
					}
					
					if (SelectionChanged != null) SelectionChanged (this, EventArgs.Empty);
				}
				
				if (disableSelection)
					disableSelection = false;

				this.QueueDraw ();
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
				layout.SetMarkup (win.DataProvider.GetText (page + n));
				Gdk.Pixbuf icon = win.DataProvider.GetIcon (page + n);
				
				int wi, he, typos, iypos;
				layout.GetPixelSize (out wi, out he);
				typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
				iypos = icon.Height < rowHeight ? ypos + (rowHeight - icon.Height) / 2 : ypos;
				
				if (page + n == selection) {
					if (!disableSelection) {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected), true, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Selected), xpos + icon.Width + 2, typos, layout);
					}
					else {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected), false, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), xpos + icon.Width + 2, typos, layout);
					}
				}
				else
					this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), xpos + icon.Width + 2, typos, layout);
					
				this.GdkWindow.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), icon, 0, 0, xpos, iypos, icon.Width, icon.Height, Gdk.RgbDither.None, 0, 0);
				
				ypos += rowHeight;
				n++;
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
			
			this.GdkWindow.GetSize (out lvWidth, out lvHeight);

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
		Gdk.Pixbuf GetIcon (int n);
	}
}

