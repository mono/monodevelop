// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This class paints the textarea.
	/// </summary>
	//[ToolboxItem(false)]
	public class TextAreaControl : Gtk.Table
	{
		TextEditorControl         motherTextEditorControl;
		
		Gtk.VScrollbar vScrollBar = new Gtk.VScrollbar(null);
		Gtk.HScrollbar hScrollBar = new Gtk.HScrollbar(null);
		TextArea   textArea;
		Size size;
		Gtk.Menu contextMenu = null;
		
		public TextArea TextArea {
			get {
				return textArea;
			}
		}
		
		public Gtk.Menu ContextMenu {
			get {
				return contextMenu;
			}
			
			set {
				contextMenu = value;
			}
		}
		
		public SelectionManager SelectionManager {
			get {
				return textArea.SelectionManager;
			}
		}
		public Caret Caret {
			get {
				return textArea.Caret;
			}
		}

		
		//[Browsable(false)]
		public IDocument Document {
			get {
				return motherTextEditorControl.Document;
			}
		}
		
		public ITextEditorProperties TextEditorProperties {
			get {
				return motherTextEditorControl.TextEditorProperties;
			}
		}
		
		public TextAreaControl(TextEditorControl motherTextEditorControl): base(2, 2, false)
		{
			this.motherTextEditorControl = motherTextEditorControl;
			
			this.textArea                = new TextArea(motherTextEditorControl, this);
#if GTK
			vScrollBar.ValueChanged += new EventHandler(VScrollBarValueChanged);
			hScrollBar.ValueChanged += new EventHandler(HScrollBarValueChanged);

			textArea.HasFocus = true;
			//Gtk.Table table = new Gtk.Table(2, 2, false);
			Gtk.Table table = this;
			//table.Homogeneous = false;
			table.Attach(textArea, 0, 1, 0, 1, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, 0);
			table.Attach(vScrollBar, 1, 2, 0, 1, Gtk.AttachOptions.Shrink, Gtk.AttachOptions.Fill, 0, 0);
			table.Attach(hScrollBar, 0, 1, 1, 2, Gtk.AttachOptions.Fill, Gtk.AttachOptions.Shrink, 0, 0);
			table.Attach(new Gtk.Label(""), 1, 2, 1, 2, Gtk.AttachOptions.Fill, Gtk.AttachOptions.Shrink, 0, 0);
			table.ShowAll();
			//Add(table);
			
			ScrollEvent += new GtkSharp.ScrollEventHandler(OnScroll);
			ButtonPressEvent += new GtkSharp.ButtonPressEventHandler(OnButtonPress);
#else
                        Controls.Add(textArea);
						
			vScrollBar.ValueChanged += new EventHandler(VScrollBarValueChanged);
			Controls.Add(this.vScrollBar);
			
			hScrollBar.ValueChanged += new EventHandler(HScrollBarValueChanged);
                        Controls.Add(this.hScrollBar);
                        ResizeRedraw = true;
#endif
			
			Document.DocumentChanged += new DocumentEventHandler(AdjustScrollBars);
		}

#if GTK

#else
                protected override void OnResize(System.EventArgs e)
                {
                        base.OnResize(e);
                        textArea.Bounds = new Rectangle(0, 0,
                                                        Width - SystemInformation.HorizontalScrollBarArrowWidth,
                                                        Height - SystemInformation.VerticalScrollBarArrowHeight);
                        SetScrollBarBounds();
                }
#endif
		
		public void AdjustScrollBars(object sender, DocumentEventArgs e)
		{
			double v_curval = vScrollBar.Adjustment.Value;
			int v_min = 0;
			int v_max = (Document.TotalNumberOfLines + textArea.TextView.VisibleLineCount - 2) * textArea.TextView.FontHeight;
			int v_lc = Math.Max(0, textArea.TextView.DrawingPosition.Height);
			int v_sc = Math.Max(0, textArea.TextView.FontHeight);
			
			double h_curval = hScrollBar.Adjustment.Value;
			int h_min = 0;
			int h_max = (int)(1000 * textArea.TextView.GetWidth(' ')) ; //Math.Max(0, max + textArea.TextView.VisibleColumnCount - 1);
			int h_lc = Math.Max(0, textArea.TextView.DrawingPosition.Width);
			int h_sc = Math.Max(0, (int)textArea.TextView.GetWidth(' '));
			
			
			Gtk.Adjustment ha = new Gtk.Adjustment(h_curval, h_min, h_max, h_sc, h_lc, 100);
			Gtk.Adjustment va = new Gtk.Adjustment(v_curval, v_min, v_max, v_sc, v_lc, 100);
			
			hScrollBar.Adjustment = ha;
			vScrollBar.Adjustment = va;
		}
		
		public void OptionsChanged()
		{
			textArea.OptionsChanged();
			
			AdjustScrollBars(null, null);
		}
		
		void VScrollBarValueChanged(object sender, EventArgs e)
		{
			textArea.VirtualTop = new System.Drawing.Point((int)textArea.VirtualTop.X, (int)vScrollBar.Value);
		}
		
		void HScrollBarValueChanged(object sender, EventArgs e)
		{
			textArea.VirtualTop = new System.Drawing.Point((int)hScrollBar.Value, textArea.VirtualTop.Y);
		}
		
#if GTK
		// FIXME: GTKize
		protected void OnScroll (object o, GtkSharp.ScrollEventArgs args)
		{
			//Console.WriteLine (args.Event.y);
			switch (args.Event.direction) {
				case Gdk.ScrollDirection.Up:
					//Console.WriteLine (this.vScrollBar.Value);
					this.vScrollBar.Value -= args.Event.y;
					break;
				case Gdk.ScrollDirection.Down:
					//Console.WriteLine (this.vScrollBar.Value);
					this.vScrollBar.Value += args.Event.y;
					break;
				case Gdk.ScrollDirection.Right:
				case Gdk.ScrollDirection.Left:
					break;
				default:
					break;
			}
		}
#else
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			int MAX_DELTA  = 120; // basically it's constant now, but could be changed later by MS
			int multiplier = Math.Abs(e.Delta) / MAX_DELTA;
			
			int newValue;
			if (System.Windows.Forms.SystemInformation.MouseWheelScrollLines > 0) {
				newValue = this.vScrollBar.Value - (TextEditorProperties.MouseWheelScrollDown ? 1 : -1) * Math.Sign(e.Delta) * System.Windows.Forms.SystemInformation.MouseWheelScrollLines * vScrollBar.SmallChange * multiplier ;
			} else {
				newValue = this.vScrollBar.Value - (TextEditorProperties.MouseWheelScrollDown ? 1 : -1) * Math.Sign(e.Delta) * vScrollBar.LargeChange;
			}
			vScrollBar.Value = Math.Max(vScrollBar.Minimum, Math.Min(vScrollBar.Maximum, newValue));
		}
#endif
		
		public void ScrollToCaret()
		{
			int curCharMin  = (int)(this.hScrollBar.Value - this.hScrollBar.Adjustment.Lower);
			int curCharMax  = curCharMin + textArea.TextView.VisibleColumnCount;
			
			int pos         = textArea.TextView.GetVisualColumn(textArea.Caret.Line, textArea.Caret.Column);
			
			if (textArea.TextView.VisibleColumnCount < 0) {
				hScrollBar.Adjustment.Value = 0;
			} else {
				if (pos < curCharMin) {
					hScrollBar.Adjustment.Value = (int)(Math.Max(0, pos - scrollMarginHeight));
				} else {
					if (pos > curCharMax) {
						hScrollBar.Adjustment.Value = (int)Math.Max(0, Math.Min(hScrollBar.Adjustment.Upper, (pos - textArea.TextView.VisibleColumnCount + scrollMarginHeight)));
					}
				}
			}
			ScrollTo(textArea.Caret.Line);
		}
		
		int scrollMarginHeight  = 3;
		
		public void ScrollTo(int line)
		{
			line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
			line = Document.GetLogicalLine(line);
			
			int curLineMin = textArea.TextView.FirstVisibleLine;
			if (line - scrollMarginHeight < curLineMin) {
				this.vScrollBar.Adjustment.Value =  Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line - scrollMarginHeight)) * textArea.TextView.FontHeight;
			} else {
				int curLineMax = curLineMin + this.textArea.TextView.VisibleLineCount;
				if (line + scrollMarginHeight > curLineMax) {
					this.vScrollBar.Adjustment.Value = Math.Min(Document.TotalNumberOfLines - 1, 
					                                 line - this.textArea.TextView.VisibleLineCount + scrollMarginHeight) * textArea.TextView.FontHeight;
				}
			}
		}
		
		public void JumpTo(int line, int column)
		{
			textArea.SelectionManager.ClearSelection();
			textArea.Caret.Position = new System.Drawing.Point(column, line);
			textArea.SetDesiredColumn();
			ScrollToCaret();
#if GTK
			textArea.GrabFocus();
#else
			textArea.Focus();
#endif
		}
		
		private void OnButtonPress(object sender, GtkSharp.ButtonPressEventArgs args) {
			if (args.Event.button == 3) {
				if (contextMenu != null) {
					args.RetVal = true;
					contextMenu.Popup(null, null, null, IntPtr.Zero, 3, Gtk.Global.CurrentEventTime);
				}
			}
		}
	}
}
