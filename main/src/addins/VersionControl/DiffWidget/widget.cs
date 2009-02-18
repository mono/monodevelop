using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

using Gtk;
using Pango;

namespace Algorithm.Diff.Gtk {

	public class DiffWidget : VBox {
	
		ScrolledWindow scroller;
	
		public double Position {
			get {
				return scroller.Vadjustment.Value;
			}
			set {
				scroller.Vadjustment.Value = value;
			}
		}
		 
		public class Options {
			public string LeftName = null;
			public string RightName = null;
			public bool SideBySide = false;
			public bool LineWrap = true;
			public bool LineNumbers = true;
			public string Font = "Mono 9";
		}
		
		Gdk.Color
			ColorChanged = new Gdk.Color (0xFF, 0x99, 0x99),
			ColorChangedHighlight = new Gdk.Color (0xFF, 0xBB, 0xBB),
			ColorAdded = new Gdk.Color (0xFF, 0xAA, 0x33),
			ColorAddedHighlight = new Gdk.Color (0xFF, 0xBB, 0x66),
			ColorRemoved = new Gdk.Color (0x33, 0xAA, 0xFF),
			ColorRemovedHighlight = new Gdk.Color (0x66, 0xBB, 0xFF),
			ColorDefault = new Gdk.Color (0xFF, 0xFF, 0xFF),
			ColorDefaultHighlight = new Gdk.Color (0xEE, 0xEE, 0xCC),
			ColorGrey = new Gdk.Color (0x99, 0x99, 0x99),
			ColorBlack = new Gdk.Color (0, 0, 0);

		public DiffWidget(Diff diff, Options options) : this(Hunkify(diff), options) 
		{
		}
		
		public DiffWidget(Merge merge, Options options) : this(Hunkify(merge), options) 
		{
		}
		
		protected override void OnStyleSet (global::Gtk.Style previous_style)
		{
			ColorBlack = this.Style.Text (StateType.Normal);
			ColorGrey  = this.Style.Text (StateType.Insensitive);
			ColorDefault = this.Style.Base (StateType.Normal);
			ColorDefaultHighlight = this.Style.Base (StateType.Prelight);
			base.OnStyleSet (previous_style);
		}

		private static Hunk[] Hunkify(IEnumerable e) {
			ArrayList a = new ArrayList();
			foreach (Hunk x in e)
				a.Add(x);
			return (Hunk[])a.ToArray(typeof(Hunk));
		}

		private DiffWidget(Hunk[] hunks, Options options) : base(false, 0) {
			if (hunks == null || hunks.Length == 0 || options == null)
				throw new ArgumentException();
			
			if (options.SideBySide && options.LeftName != null && options.RightName != null) {
				HBox filetitles = new HBox(true, 2);
				PackStart(filetitles, false, false, 2);
				Label leftlabel = new Label(options.LeftName);
				Label rightlabel = new Label(options.RightName);
				filetitles.PackStart(leftlabel);
				filetitles.PackStart(rightlabel);
			}
			
			HBox centerpanel = new HBox(false, 0);
			PackStart(centerpanel);

			scroller = new ScrolledWindow();
			
			centerpanel.PackStart(new OverviewRenderer(this, scroller, hunks, options.SideBySide), false, false, 0);
			
			Viewport textviewport = new Viewport();
			
			centerpanel.PackStart(scroller);
			scroller.Add(textviewport);
			
			int nRows = 0;
			foreach (Hunk hunk in hunks) {
				if (options.SideBySide) {
					nRows += hunk.MaxLines();
				} else {
					if (hunk.Same) {
						nRows += hunk.Original().Count;
					} else {
						for (int i = 0; i < hunk.ChangedLists; i++)
							nRows += hunk.Changes(i).Count;
					}
				}
			}
			
			uint nCols = 1 + (uint)hunks[0].ChangedLists;
			if (options.SideBySide) nCols += 2;
			if (options.LineNumbers) nCols++;
			
			VBox tablecontainer = new VBox(false, 0);
			textviewport.Add(tablecontainer);
			
			Table difftable = new Table((uint)nRows, (uint)nCols, false);
			tablecontainer.PackStart(difftable, false, false, 0);	
			
			uint row = 0;
			
			Pango.FontDescription font = null;
			if (options.Font != null)
				font = Pango.FontDescription.FromString(options.Font);
			
			foreach (Hunk hunk in hunks) {
				char leftmode = hunk.Same ? ' ' : (hunk.ChangedLists == 1 && hunk.Changes(0).Count == 0) ? '-' : 'C';
				uint inc = 0;
				
				if (options.SideBySide) {
					ComposeLines(hunk.Original(), leftmode, -1, difftable, row, false, 0, options.LineWrap, font, options.LineNumbers);
					inc = (uint)hunk.Original().Count;
				} else { 
					if (leftmode == 'C') leftmode = '-';
					int altlines = -1;
					if (hunk.ChangedLists == 1 && hunk.Same)
						altlines = hunk.Changes(0).Start;
					ComposeLines(hunk.Original(), leftmode, altlines, difftable, row, true, 0, options.LineWrap, font, options.LineNumbers);
					row += (uint)hunk.Original().Count;
				}

				for (int i = 0; i < hunk.ChangedLists; i++) {
					char rightmode = hunk.Same ? ' ' : hunk.Original().Count == 0 ? '+' : 'C';
					
					if (options.SideBySide) {
						int colsper = 1 + (options.LineNumbers ? 1 : 0);			
						ComposeLines(hunk.Changes(i), rightmode, -1, difftable, row, false, (uint)((i+1)*colsper), options.LineWrap, font, options.LineNumbers);
						if (hunk.Changes(i).Count > inc)
							inc = (uint)hunk.Changes(i).Count;
					} else {
						if (rightmode == 'C') rightmode = '+';
		
						if (!hunk.Same) 
							ComposeLines(hunk.Changes(i), rightmode, -1, difftable, row, true, 0, options.LineWrap, font, options.LineNumbers);
						
						if (!hunk.Same) row += (uint)hunk.Changes(i).Count;
					}
				}
				
				if (options.SideBySide)
					row += inc;
			}
		}
		
		void ComposeLines(Algorithm.Diff.Range range, char style, int otherStart, Table table, uint startRow, bool axn, uint col, bool wrap, Pango.FontDescription font, bool lineNumbers) {
			if (range.Count == 0) return;
			
			StringBuilder text = new StringBuilder();
			for (uint i = 0; i < range.Count; i++) {
				if (axn) {
					text.Append(style);
					text.Append(' ');
				}

				if (lineNumbers) {
					string lineNo = (range.Start + i + 1).ToString();
					if (otherStart != -1 && range.Start != otherStart)
					lineNo += "/" + (otherStart + i + 1).ToString();
					text.Append(lineNo);
					text.Append('\t');
				}
				
				text.Append((string)range[(int)i]);
				if (i < range.Count-1) text.Append('\n');
			}
			RangeEventBox rangebox = new RangeEventBox(new RangeRenderer(this, text.ToString(), style, wrap, font));
			table.Attach(rangebox, 0,1, startRow,(uint)(startRow+range.Count));
			
			/*
				int off1 = lineNumbers ? 1 : 0;
				int off2 = off1 + (axn ? 1 : 0);
				
				for (uint i = 0; i < range.Count; i++) {
					if (lineNumbers) {
						string lineNo = (range.Start + i + 1).ToString();
						if (otherStart != -1 && range.Start != otherStart)
						lineNo += "/" + (otherStart + i + 1).ToString();
					
						Label label = new Label(lineNo);
						label.ModifyFont( font );
						label.Yalign = 0;
						table.Attach(label, col,col+1, startRow+i,startRow+i+1, AttachOptions.Shrink, AttachOptions.Shrink, 1, 1);
					}
					
					if (axn) {
						Label actionlabel = new Label(style.ToString());
						table.Attach(actionlabel, (uint)(col+off1),(uint)(col+off1+1), startRow+i,startRow+i+1, AttachOptions.Shrink, AttachOptions.Shrink, 1, 1);
					}

					RangeEventBox line = new RangeEventBox(new RangeRenderer((string)range[(int)i], style, wrap, font));
					table.Attach(line, (uint)(col+off2),(uint)(col+off2+1), startRow+i,startRow+i+1);
				}
			*/
		}
		
		/*string GetRangeText(Algorithm.Diff.Range range) {
			string text = "";
			foreach (string line in range)
				text += line + "\n";
			return text;
		}*/

		class RangeEventBox : EventBox {
			RangeRenderer renderer;
			
			public RangeEventBox(RangeRenderer r) {
				renderer = r;
				Add(r);
				this.EnterNotifyEvent += new EnterNotifyEventHandler(EnterNotifyEventHandler);
				this.LeaveNotifyEvent += new LeaveNotifyEventHandler(LeaveNotifyEventHandler);
			}

			void EnterNotifyEventHandler (object o, EnterNotifyEventArgs args) {
				renderer.Highlight();
			}		
			void LeaveNotifyEventHandler (object o, LeaveNotifyEventArgs args) {
				renderer.ClearHighlight();
			}		
		}
		
		class RangeRenderer : DrawingArea {
			Pango.Layout layout;
			Pango.FontDescription font;
			string text;
			bool wrap;
			char type;
			DiffWidget widget;
				
			public RangeRenderer(DiffWidget widget, string text, char type, bool wrap, Pango.FontDescription font)
			{
				this.widget = widget;
				this.text = text;
				this.wrap = wrap;
				this.font = font;
				this.type = type;
				this.Realized += new EventHandler(OnRealized);
				this.SizeAllocated += new SizeAllocatedHandler(SizeAllocatedHandler);
				ClearHighlight();
			}
			
			Gdk.Color bg_color {
				get {
					switch (type) {
						case 'C':
							return widget.ColorChanged;
						case '+':
							return widget.ColorAdded;
						case '-':
							return  widget.ColorRemoved;
						default:
							return widget.ColorDefault;
					}
				}
			}
			
			Gdk.Color highlight_bg_color {
				get {
					switch (type) {
						case 'C':
							return widget.ColorChangedHighlight;
						case '+':
							return widget.ColorAddedHighlight;
						case '-':
							return widget.ColorRemovedHighlight;
						default:
							return widget.ColorDefaultHighlight;
					}
				}
				
			}
			bool inStyleSet = false;
			protected override void OnStyleSet (global::Gtk.Style previous_style)
			{
				base.OnStyleSet (previous_style);
				if (!inStyleSet) {
					inStyleSet = true;
					if (highlight) {
						Highlight ();
					} else {
						ClearHighlight();
					}
					QueueDraw ();
					inStyleSet = false;
				}
			}
			
			bool highlight = false;
			public void Highlight() 
			{
				highlight = true;
				ModifyBg (StateType.Normal, highlight_bg_color);
			}
			
			public void ClearHighlight() 
			{
				highlight = false;
				ModifyBg (StateType.Normal, bg_color);
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				GdkWindow.DrawLayout (this.Style.TextGC (highlight ? StateType.Prelight : StateType.Normal), 1, 1, layout);
				return true;
			}
			
			void OnRealized (object o, EventArgs args)
			{
				layout = new Pango.Layout(PangoContext);
				layout.SingleParagraphMode = false;
				layout.FontDescription = font;
				layout.Indent = (int)(-Pango.Scale.PangoScale * 20);
				layout.SetText(text);
				Render();
			}
			
			void SizeAllocatedHandler (object o, SizeAllocatedArgs args) {
				Render();
			}
			
			void Render()
			{
				if (layout == null) return;
				
				if (wrap)
					layout.Width = (int)(Allocation.Width * Pango.Scale.PangoScale);
				else
					layout.Width = int.MaxValue;
				
				Rectangle ink, log;
				layout.GetPixelExtents(out ink, out log);
				HeightRequest = ink.Y + ink.Height + 3;
				if (!wrap)
					WidthRequest = ink.X + ink.Width + 3;
			}
		}
		
		class OverviewRenderer : EventBox {
			ScrolledWindow scroller;
			public OverviewRenderer(DiffWidget widget, ScrolledWindow scroller, Hunk[] hunks, bool sidebyside) {
				this.scroller = scroller;
				this.ButtonPressEvent += new ButtonPressEventHandler(ButtonPressHandler);
				Add(new OverviewRenderer2(widget, scroller, hunks, sidebyside));
			}
			
			void ButtonPressHandler(object o, ButtonPressEventArgs args) {
				double position = ((double)args.Event.Y / Allocation.Height - (double)scroller.Allocation.Height/scroller.Vadjustment.Upper/2) * scroller.Vadjustment.Upper;
				if (position < 0) position = 0;
				if (position + scroller.Allocation.Height > scroller.Vadjustment.Upper) position = scroller.Vadjustment.Upper - scroller.Allocation.Height;
				scroller.Vadjustment.Value = position;
			}
		}
		
		class OverviewRenderer2 : DrawingArea {
			Hunk[] hunks;
			ScrolledWindow scroller;
			bool sidebyside;
			DiffWidget widget;
			
			public OverviewRenderer2(DiffWidget widget, ScrolledWindow scroller, Hunk[] hunks, bool sidebyside) {
				this.widget = widget;
				this.hunks = hunks;
				this.scroller = scroller;
				this.sidebyside = sidebyside;
				scroller.ExposeEvent += new ExposeEventHandler(OnScroll);
				WidthRequest = 50;
			}
			
			void OnScroll (object o, ExposeEventArgs args)
			{
				QueueDrawArea(0, 0, Allocation.Width, Allocation.Height);
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose e)
			{
				Gdk.GC gc = new Gdk.GC (e.Window);
				
				int count = 0;
				foreach (Hunk h in hunks) {
					IncPos(h, ref count);
				}
				
				int start = 0;
				foreach (Hunk h in hunks) {
					int size = 0;
					IncPos(h, ref size);
					if (h.Same)
						gc.RgbFgColor = widget.ColorDefault;
					else if (h.Original().Count == 0)
						gc.RgbFgColor = widget.ColorAdded;
					else if (h.ChangedLists == 1 && h.Changes(0).Count == 0)
						gc.RgbFgColor = widget.ColorRemoved;
					else
						gc.RgbFgColor = widget.ColorChanged;
					
					GdkWindow.DrawRectangle(gc, true, 0, Allocation.Height*start/count, Allocation.Width, Allocation.Height*size/count);
					
					start += size;
				}

				gc.RgbFgColor = widget.ColorGrey;
				e.Window.DrawRectangle(gc, false,
					1,
					(int)(Allocation.Height*scroller.Vadjustment.Value/scroller.Vadjustment.Upper) + 1,
					Allocation.Width-3,
					(int)(Allocation.Height*((double)scroller.Allocation.Height/scroller.Vadjustment.Upper))-2);
				
				gc.RgbFgColor = widget.ColorBlack;
				e.Window.DrawRectangle(gc, false,
					0,
					(int)(Allocation.Height*scroller.Vadjustment.Value/scroller.Vadjustment.Upper),
					Allocation.Width-1,
					(int)(Allocation.Height*((double)scroller.Allocation.Height/scroller.Vadjustment.Upper)));
				
				gc.Dispose ();
				return true;
			}
			
			private void IncPos(Hunk h, ref int pos) {
				if (sidebyside)
					pos += h.MaxLines();
				else if (h.Same)
					pos += h.Original().Count;
				else {
					pos += h.Original().Count;
					for (int i = 0; i < h.ChangedLists; i++)
						pos += h.Changes(i).Count;
				}
			}
		}	
	}
}
