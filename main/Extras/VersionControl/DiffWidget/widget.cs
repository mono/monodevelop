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
		
		static Gdk.Color
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

		public DiffWidget(Diff diff, Options options) : this(Hunkify(diff), options) {
		}
			
		public DiffWidget(Merge merge, Options options) : this(Hunkify(merge), options) {
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
			
			centerpanel.PackStart(new OverviewRenderer(scroller, hunks, options.SideBySide), false, false, 0);
			
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
			RangeEventBox rangebox = new RangeEventBox(new RangeRenderer(text.ToString(), style, wrap, font));
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

			Gdk.Color bg_color;
			Gdk.Color highlight_bg_color;

			public RangeRenderer(string text, char type, bool wrap, Pango.FontDescription font) {
				this.text = text;
				this.wrap = wrap;
				this.font = font;
				this.Realized += new EventHandler(OnRealized);
				this.ExposeEvent += new ExposeEventHandler(OnExposed);
				this.SizeAllocated += new SizeAllocatedHandler(SizeAllocatedHandler);
				
				switch (type) {
					case 'C':
						bg_color = DiffWidget.ColorChanged;
						highlight_bg_color = DiffWidget.ColorChangedHighlight;
						break;
					case '+':
						bg_color = DiffWidget.ColorAdded;
						highlight_bg_color = DiffWidget.ColorAddedHighlight;
						break;
					case '-':
						bg_color = DiffWidget.ColorRemoved;
						highlight_bg_color = DiffWidget.ColorRemovedHighlight;
						break;
					default:
						bg_color = DiffWidget.ColorDefault;
						highlight_bg_color = DiffWidget.ColorDefaultHighlight;
						break;
				}
				
				Colormap.AllocColor(ref bg_color, false, true);
				Colormap.AllocColor(ref highlight_bg_color, false, true);
				ClearHighlight();
			}
			
			public void Highlight() {
				ModifyBg(StateType.Normal, highlight_bg_color);
			}
			
			public void ClearHighlight() {
				ModifyBg(StateType.Normal, bg_color);
			}

			void OnExposed (object o, ExposeEventArgs args) {
				GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), 1, 1, layout);
			}
			
			void OnRealized (object o, EventArgs args) {
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
			
			void Render() {
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
			public OverviewRenderer(ScrolledWindow scroller, Hunk[] hunks, bool sidebyside) {
				this.scroller = scroller;
				this.ButtonPressEvent += new ButtonPressEventHandler(ButtonPressHandler);
				Add(new OverviewRenderer2(scroller, hunks, sidebyside));
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
			
			public OverviewRenderer2(ScrolledWindow scroller, Hunk[] hunks, bool sidebyside) {
				this.hunks = hunks;
				this.scroller = scroller;
				this.sidebyside = sidebyside;
				this.ExposeEvent += new ExposeEventHandler(OnExposed);
				scroller.ExposeEvent += new ExposeEventHandler(OnScroll);
				WidthRequest = 50;
			}
			
			void OnScroll (object o, ExposeEventArgs args) {
				QueueDrawArea(0, 0, Allocation.Width, Allocation.Height);
			}
			
			void OnExposed (object o, ExposeEventArgs args) {
				Gdk.GC gc = this.Style.BaseGC(StateType.Normal);
				
				gc.Colormap.AllocColor(ref DiffWidget.ColorChanged, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorChangedHighlight, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorAdded, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorAddedHighlight, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorRemoved, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorRemovedHighlight, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorDefault, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorDefaultHighlight, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorBlack, false, true);
				gc.Colormap.AllocColor(ref DiffWidget.ColorGrey, false, true);

				int count = 0;
				foreach (Hunk h in hunks) {
					IncPos(h, ref count);
				}
				
				int start = 0;
				foreach (Hunk h in hunks) {
					int size = 0;
					IncPos(h, ref size);
					
					if (h.Same)
						gc.Foreground = DiffWidget.ColorDefault;
					else if (h.Original().Count == 0)
						gc.Foreground = DiffWidget.ColorAdded;
					else if (h.ChangedLists == 1 && h.Changes(0).Count == 0)
						gc.Foreground = DiffWidget.ColorRemoved;
					else
						gc.Foreground = DiffWidget.ColorChanged;
					
					GdkWindow.DrawRectangle(gc, true, 0, Allocation.Height*start/count, Allocation.Width, Allocation.Height*size/count);
					
					start += size;
				}

				gc.Foreground = DiffWidget.ColorGrey;
				GdkWindow.DrawRectangle(gc, false,
					1,
					(int)(Allocation.Height*scroller.Vadjustment.Value/scroller.Vadjustment.Upper) + 1,
					Allocation.Width-3,
					(int)(Allocation.Height*((double)scroller.Allocation.Height/scroller.Vadjustment.Upper))-2);
				
				gc.Foreground = DiffWidget.ColorBlack;
				GdkWindow.DrawRectangle(gc, false,
					0,
					(int)(Allocation.Height*scroller.Vadjustment.Value/scroller.Vadjustment.Upper),
					Allocation.Width-1,
					(int)(Allocation.Height*((double)scroller.Allocation.Height/scroller.Vadjustment.Upper)));
				
				// Reset this otherwise MD colors get messed up.
				// But what should I really do here?
				gc.Foreground = DiffWidget.ColorDefault;
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
