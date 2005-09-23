// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using MonoDevelop.TextEditor.Document;

using Gdk;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This class views the line numbers and folding markers.
	/// </summary>
	public class GutterMargin : AbstractMargin
	{
		
		public static Cursor RightLeftCursor;
		
		static GutterMargin()
		{
			Stream cursorStream = Assembly.GetCallingAssembly().GetManifestResourceStream("RightArrow.cur");
			//RightLeftCursor = new Cursor(cursorStream);
			cursorStream.Close();
		}
		
		
		public override Cursor Cursor {
			get {
				return RightLeftCursor;
			}
		}
		
		public override Size Size {
			get {
				return new Size((int)(textArea.TextView.GetWidth('w') * Math.Max(3, (int)Math.Log10(textArea.Document.TotalNumberOfLines) + 1)),
				                -1);
			}
		}
		
		public override bool IsVisible {
			get {
				return textArea.TextEditorProperties.ShowLineNumbers;
			}
		}
		
		
		
		public GutterMargin(TextArea textArea) : base(textArea)
		{
		}
		
		public override void Paint(Gdk.Drawable wnd, System.Drawing.Rectangle rect)
		{
			int one_width = (int) textArea.TextView.GetWidth ('w');
			
			using (Gdk.GC gc = new Gdk.GC (wnd)) {
			using (Pango.Layout ly = new Pango.Layout (TextArea.PangoContext)) {
				ly.FontDescription = FontContainer.DefaultFont;
				ly.Width = drawingPosition.Width;
				ly.Alignment = Pango.Alignment.Right;
				
				HighlightColor lineNumberPainterColor = textArea.Document.HighlightingStrategy.GetColorFor("LineNumbers");
				
				gc.RgbBgColor = new Gdk.Color (lineNumberPainterColor.BackgroundColor);
				gc.RgbFgColor = TextArea.Style.White;
				wnd.DrawRectangle (gc, true, drawingPosition);
				
				gc.RgbFgColor = new Gdk.Color (lineNumberPainterColor.Color);
				gc.SetLineAttributes (1, LineStyle.OnOffDash, CapStyle.NotLast, JoinStyle.Miter);
				wnd.DrawLine (gc, drawingPosition.X + drawingPosition.Width, drawingPosition.Y, drawingPosition.X + drawingPosition.Width, drawingPosition.Height);
					
				
				//FIXME: This doesnt allow different fonts and what not
				int fontHeight = TextArea.TextView.FontHeight;
		
				for (int y = 0; y < (DrawingPosition.Height + textArea.TextView.VisibleLineDrawingRemainder) / fontHeight + 1; ++y) {
					int ypos = drawingPosition.Y + fontHeight * y  - textArea.TextView.VisibleLineDrawingRemainder;
				

					int curLine = y + textArea.TextView.FirstVisibleLine;
					if (curLine < textArea.Document.TotalNumberOfLines) {
						ly.SetText ((curLine + 1).ToString ());
						wnd.DrawLayout (gc, drawingPosition.X + drawingPosition.Width - one_width, ypos, ly);
					}
				}
			}}
		}
	}
}
