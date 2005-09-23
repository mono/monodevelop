// <file>                          
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Text;

using MonoDevelop.TextEditor.Document;

using Gdk;
using Pango;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This class paints the textarea.
	/// </summary>
	public class TextView : AbstractMargin
	{
		int          fontHeight;
		Hashtable    charWidth           = new Hashtable();
		Highlight    highlight;
	
		Layout _layout; // Avoid building new layouts all the time
		
		public Highlight Highlight {
			get {
				return highlight;
			}
			set {
				highlight = value;
			}
		}
		
		/*
		public Cursor Cursor {
			get {
				return Cursors.IBeam;
			}
		}
		*/
		
		public int FirstVisibleLine {
			get {
				return textArea.VirtualTop.Y / fontHeight;
			}
			set {
				if (FirstVisibleLine != value) {
					textArea.VirtualTop = new System.Drawing.Point(textArea.VirtualTop.X, value * fontHeight);
				}
			}
		}
		
		public int VisibleLineDrawingRemainder {
			get {
				return textArea.VirtualTop.Y % fontHeight;
			}
		}
		
		public int FontHeight {
			get {
				return fontHeight;
			}
		}
		
		public int VisibleLineCount {
			get {
				return 1 + DrawingPosition.Height / fontHeight;
			}
		}
		
		public int VisibleColumnCount {
			get {
				return (int)(DrawingPosition.Width / GetWidth(' ')) - 1;
			}
		}
		
		public TextView(TextArea textArea) : base(textArea)
		{
			_layout = new Layout(textArea.PangoContext);
			OptionsChanged();
		}
		
		public void OptionsChanged()
		{
			this.fontHeight = (int) Math.Ceiling(GetHeight(TextEditorProperties.Font));
			this.charWidth  = new Hashtable();
		}
		
#region Paint functions
		public override void Paint(Gdk.Drawable g, System.Drawing.Rectangle rect)
		{
			using (Gdk.GC backgroundGC = new Gdk.GC(g)) {
			using (Gdk.GC gc = new Gdk.GC(g)) {
			
			int horizontalDelta = (int)(textArea.VirtualTop.X * GetWidth(g, ' '));
			if (horizontalDelta > 0) {
				Gdk.Rectangle r = new Gdk.Rectangle(this.DrawingPosition.X, this.DrawingPosition.Y, this.DrawingPosition.Width, this.DrawingPosition.Height);
				gc.ClipRectangle = r;
				backgroundGC.ClipRectangle = r;
			}
			
			for (int y = 0; y < (DrawingPosition.Height + VisibleLineDrawingRemainder) / fontHeight + 1; ++y) {
				System.Drawing.Rectangle lineRectangle = new System.Drawing.Rectangle(DrawingPosition.X - horizontalDelta,
				                                        DrawingPosition.Top + y * fontHeight - VisibleLineDrawingRemainder,
				                                        DrawingPosition.Width + horizontalDelta,
				                                        fontHeight);
				if (rect.IntersectsWith(lineRectangle)) {
					int currentLine = FirstVisibleLine + y;
					PaintDocumentLine(g, gc, backgroundGC, currentLine, lineRectangle);
				}
			}
			}} // using
		}
		
		void PaintDocumentLine(Gdk.Drawable g, Gdk.GC gc, Gdk.GC backgroundGC, int lineNumber, System.Drawing.Rectangle lineRectangle)
		{
			HighlightBackground background = (HighlightBackground)textArea.Document.HighlightingStrategy.GetColorFor("DefaultColor");
			//Brush               backgroundBrush = textArea.Sensitive ? new SolidBrush(background.BackgroundColor) : SystemBrushes.InactiveBorder;
			
			if (textArea.Sensitive) {
				backgroundGC.RgbFgColor = new Gdk.Color(background.BackgroundColor);
			} else {
				Gdk.Color grey = new Gdk.Color ();
				Gdk.Color.Parse ("grey", ref grey);
				backgroundGC.RgbFgColor = grey;
			}
			
			if (lineNumber >= textArea.Document.TotalNumberOfLines) {
				g.DrawRectangle(backgroundGC, true, lineRectangle);
				if (TextEditorProperties.ShowInvalidLines) {
					DrawInvalidLineMarker(g, lineRectangle.Left, lineRectangle.Top);
				}
				if (TextEditorProperties.ShowVerticalRuler) {
					DrawVerticalRuler(g, lineRectangle);
				}
				return;
			}
			HighlightColor selectionColor = textArea.Document.HighlightingStrategy.GetColorFor("Selection");
			ColumnRange    selectionRange = textArea.SelectionManager.GetSelectionAtLine(lineNumber);
			HighlightColor defaultColor = textArea.Document.HighlightingStrategy.GetColorFor("DefaultColor");
			HighlightColor tabMarkerColor   = textArea.Document.HighlightingStrategy.GetColorFor("TabMarker");
			HighlightColor spaceMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("SpaceMarker");
			
			float       spaceWidth   = GetWidth(g, ' ');
			
			int         logicalColumn  = 0;
			int         physicalColumn = 0;
			
			float       physicalXPos   = lineRectangle.X;
			LineSegment currentLine    = textArea.Document.GetLineSegment(lineNumber);
			
			if (currentLine.Words != null) {
				for (int i = 0; i <= currentLine.Words.Count + 1; ++i) {
					// needed to draw fold markers beyond the logical end of line
					if (i >= currentLine.Words.Count) {
						++logicalColumn;
						continue;
					}
					
					TextWord currentWord = ((TextWord)currentLine.Words[i]);
					switch (currentWord.Type) {
						case TextWordType.Space:
							if (ColumnRange.WholeColumn.Equals(selectionRange) || logicalColumn >= selectionRange.StartColumn && logicalColumn <= selectionRange.EndColumn - 1) {
								gc.RgbFgColor = new Gdk.Color(selectionColor.BackgroundColor);
								//gc.RgbBgColor = new Gdk.Color(selectionColor.BackgroundColor);
								g.DrawRectangle(gc, true, new Gdk.Rectangle((int) Math.Round(physicalXPos), lineRectangle.Y, (int) Math.Round(spaceWidth), lineRectangle.Height));
							} else {
								g.DrawRectangle(backgroundGC, true,
								                new Gdk.Rectangle((int) Math.Round(physicalXPos), lineRectangle.Y, (int) Math.Round(spaceWidth), lineRectangle.Height));
							}
							if (TextEditorProperties.ShowSpaces) {
								DrawSpaceMarker(g, spaceMarkerColor.Color, physicalXPos, lineRectangle.Y);
							}
							
							physicalXPos += spaceWidth;
							
							++logicalColumn;
							++physicalColumn;
							break;
						
						case TextWordType.Tab:
							int oldPhysicalColumn = physicalColumn;
							physicalColumn += TextEditorProperties.TabIndent;
							physicalColumn = (physicalColumn / TextEditorProperties.TabIndent) * TextEditorProperties.TabIndent;
							
							float tabWidth = (physicalColumn - oldPhysicalColumn) * spaceWidth;
							
							if (ColumnRange.WholeColumn.Equals(selectionRange) || logicalColumn >= selectionRange.StartColumn && logicalColumn <= selectionRange.EndColumn - 1) {
								gc.RgbBgColor = new Gdk.Color(selectionColor.Color);
								gc.RgbFgColor = new Gdk.Color(selectionColor.BackgroundColor);
								
								g.DrawRectangle(gc, true,
								                new Gdk.Rectangle((int) Math.Round(physicalXPos), lineRectangle.Y, (int) Math.Round(tabWidth), lineRectangle.Height));
							} else {
								g.DrawRectangle(backgroundGC, true,
								                new Gdk.Rectangle((int) Math.Round(physicalXPos), lineRectangle.Y, (int) Math.Round(tabWidth), lineRectangle.Height));
							}
							if (TextEditorProperties.ShowTabs) {
								DrawTabMarker(g, tabMarkerColor.Color, physicalXPos, lineRectangle.Y);
							}
							
							physicalXPos += tabWidth;
							
							++logicalColumn;
							break;
						
						case TextWordType.Word:
							string word    = currentWord.Word;
							float  lastPos = physicalXPos;
							
							if (ColumnRange.WholeColumn.Equals(selectionRange) || selectionRange.EndColumn - 1  >= word.Length + logicalColumn &&
							                                                      selectionRange.StartColumn <= logicalColumn) {
								gc.RgbFgColor = new Gdk.Color(selectionColor.BackgroundColor);
								//gc.RgbBgColor = new Gdk.Color(selectionColor.BackgroundColor);
								
								physicalXPos += DrawDocumentWord(g, word, new PointF(physicalXPos, lineRectangle.Y), currentWord.Font, selectionColor.HasForgeground ? selectionColor.Color : currentWord.Color, gc);
							} else {
								if (ColumnRange.NoColumn.Equals(selectionRange)  /* || selectionRange.StartColumn > logicalColumn + word.Length || selectionRange.EndColumn  - 1 <= logicalColumn */) {
									physicalXPos += DrawDocumentWord(g, word, new PointF(physicalXPos, lineRectangle.Y), currentWord.Font, currentWord.Color, backgroundGC);
								} else {
									int offset1 = Math.Min(word.Length, Math.Max(0, selectionRange.StartColumn - logicalColumn ));
									int offset2 = Math.Max(offset1, Math.Min(word.Length, selectionRange.EndColumn - logicalColumn));
									
									string word1 = word.Substring(0, offset1);
									string word2 = word.Substring(offset1, offset2 - offset1);
									string word3 = word.Substring(offset2);
									
									physicalXPos += DrawDocumentWord(g,
									                                      word1,
									                                      new PointF(physicalXPos, lineRectangle.Y),
									                                      currentWord.Font,
									                                      currentWord.Color,
									                                      backgroundGC);
																	gc.RgbFgColor = new Gdk.Color(selectionColor.Color);
									gc.RgbFgColor = new Gdk.Color(selectionColor.BackgroundColor);
									//gc.RgbBgColor = new Gdk.Color(selectionColor.BackgroundColor);
									
									physicalXPos += DrawDocumentWord(g,
									                                      word2,
									                                      new PointF(physicalXPos, lineRectangle.Y),
									                                      currentWord.Font,
									                                      selectionColor.HasForgeground ? selectionColor.Color : currentWord.Color,
									                                      gc);
							
									physicalXPos += DrawDocumentWord(g,
									                                      word3,
									                                      new PointF(physicalXPos, lineRectangle.Y),
									                                      currentWord.Font,
									                                      currentWord.Color,
									                                      backgroundGC);
								}
							}
							
							// draw bracket highlight
							if (highlight != null) {
								if (highlight.OpenBrace.Y == lineNumber && highlight.OpenBrace.X == logicalColumn ||
								    highlight.CloseBrace.Y == lineNumber && highlight.CloseBrace.X == logicalColumn) {
									DrawBracketHighlight(g, new System.Drawing.Rectangle((int)lastPos, lineRectangle.Y, (int)(physicalXPos - lastPos) - 1, lineRectangle.Height - 1));
								}
							}
							physicalColumn += word.Length;
							logicalColumn += word.Length;
							break;
					}
				}
			}

			bool selectionBeyondEOL = selectionRange.EndColumn > currentLine.Length || ColumnRange.WholeColumn.Equals(selectionRange);
			
				
			if (TextEditorProperties.ShowEOLMarker) {
				HighlightColor eolMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("EolMarker");
				// selectionBeyondEOL ? selectionColor.Color: eolMarkerColor.Color
				//physicalXPos += DrawEOLMarker(g, eolMarkerColor.Color, selectionBeyondEOL ? new SolidBrush(selectionColor.BackgroundColor) : backgroundBrush, physicalXPos, lineRectangle.Y);
				physicalXPos += DrawEOLMarker(g, eolMarkerColor.Color, backgroundGC, physicalXPos, lineRectangle.Y); // FIXME beyond EOL color
			} else {
				if (selectionBeyondEOL && !TextEditorProperties.AllowCaretBeyondEOL) {
					gc.RgbFgColor = new Gdk.Color(selectionColor.BackgroundColor);
					gc.RgbBgColor = new Gdk.Color(selectionColor.Color);
					g.DrawRectangle(gc, true,
					                new Gdk.Rectangle((int) Math.Round(physicalXPos), lineRectangle.Y, (int) Math.Round(spaceWidth), lineRectangle.Height));
			
					physicalXPos += spaceWidth;
				}
			}

			if (selectionBeyondEOL && TextEditorProperties.AllowCaretBeyondEOL) {
//				gc.RgbBgColor = new Gdk.Color(selectionColor.BackgroundColor);
			} 
			
			g.DrawRectangle(backgroundGC, true,
			                new Gdk.Rectangle((int) Math.Round(physicalXPos), lineRectangle.Y, (int) Math.Round(lineRectangle.Width - physicalXPos + lineRectangle.X), lineRectangle.Height));

			if (TextArea.Caret.Line == lineNumber) {
				DrawCaret(g, gc, new PointF(GetDrawingXPos(TextArea.Caret.Line, TextArea.Caret.Column) + lineRectangle.X, lineRectangle.Y));
			}

			if (TextEditorProperties.ShowVerticalRuler) {
				DrawVerticalRuler(g, lineRectangle);
			}
		}
		
		// FIXME: draw the whole line using Pango
		float DrawDocumentWord(Gdk.Drawable g, string word, PointF position, Pango.FontDescription font, System.Drawing.Color foreColor, Gdk.GC gc)
		{
			if (word == null || word.Length == 0) {
				return 0f;
			}
			float wordWidth = MeasureString(font, word);
			g.DrawRectangle(gc, true, new Gdk.Rectangle((int) Math.Abs(position.X), (int) position.Y, (int) Math.Abs(wordWidth), (int) Math.Abs(FontHeight)));
			using (Gdk.GC tgc = new Gdk.GC(g)) {
				tgc.Copy(gc);
				tgc.RgbFgColor = new Gdk.Color(foreColor);
				return DrawString(g, tgc, position.X, position.Y, word);
			}
		}
		void DrawCaret(Gdk.Drawable g, Gdk.GC gc, PointF point) {
			TextArea.Caret.PhysicalPosition = new Gdk.Point((int)point.X, (int)point.Y);
			TextArea.Caret.Paint(g, gc);
			//g.DrawLine(gc, (int)point.X, (int)point.Y, (int)point.X, (int)(point.Y + fontHeight));
		}
		
#endregion
		
#region Conversion Functions
		private float GetHeight(Pango.FontDescription font) {
				Pango.Layout ly = _layout;
				ly.FontDescription = font;
				ly.SetText("Wwgq|$%?*_-");
				return ly.Size.Height/1024.0f;
		}

		public float GetWidth(char ch)
		{
			return GetWidth(TextArea.GdkWindow, ch);
		}
		
		public float GetWidth(Gdk.Drawable g, char ch)
		{
			if (ch == ' ') {
				return GetWidth(g, 'w'); // Hack! FIXME PEDRO
			}		
			object width = charWidth[ch];
			if (width == null) {
				Pango.Layout ly = _layout;
				ly.SetText(ch.ToString());
				
				charWidth[ch] = (float) (ly.Size.Width/1024.0f - 1); // Hack! I don't know why it works substracting 1. FIXME PEDRO
				return (float)charWidth[ch];
			}
			return (float)width;
		}
		
		public int GetVisualColumn(int logicalLine, int logicalColumn)
		{
			return GetVisualColumn(Document.GetLineSegment(logicalLine), logicalColumn);
		}
		public int GetVisualColumn(LineSegment line, int logicalColumn)
		{
			int tabIndent = Document.TextEditorProperties.TabIndent;
			int column    = 0;
			for (int i = 0; i < logicalColumn; ++i) {
				char ch;
				if (i >= line.Length) {
					ch = ' ';
				} else {
					ch = Document.GetCharAt(line.Offset + i);
				}
				
				switch (ch) {
					case '\t':
						int oldColumn = column;
						column += tabIndent;
						column = (column / tabIndent) * tabIndent;
						break;
					default:
						++column;
						break;
				}
			}
			return column;
		}
		
		/// <summary>
		/// returns line/column for a visual point position
		/// </summary>
		public System.Drawing.Point GetLogicalPosition(int xPos, int yPos)
		{
			xPos -= DrawingPosition.X;
			yPos -= DrawingPosition.Y;
			int clickedVisualLine = (yPos + this.textArea.VirtualTop.Y) / fontHeight;
			int logicalLine       = clickedVisualLine; // todo : folding
			
			return new System.Drawing.Point(GetLogicalColumn(logicalLine < Document.TotalNumberOfLines ? Document.GetLineSegment(logicalLine) : null, xPos), 
			                 logicalLine);
		}
		
		int GetLogicalColumn(LineSegment line, int xPos)
		{
			int currentColumn = 0;
			int realColumn = 0;
			float spaceWidth = GetWidth(' ');
			float physicalXPos = 0;
			int tabIndent  = Document.TextEditorProperties.TabIndent;
			LineSegment currentLine = line;
			
			if (currentLine == null || currentLine.Words == null) {
				return 0;
			}
 			for (int i = 0; i < currentLine.Words.Count && xPos + spaceWidth/2 > physicalXPos; ++i) {
				TextWord currentWord = ((TextWord)currentLine.Words[i]);
				switch (currentWord.Type) {
					case TextWordType.Space:
						physicalXPos += spaceWidth;
						currentColumn++;
						realColumn++;
						break;

					case TextWordType.Tab:
						int ind = realColumn % tabIndent;
						int hop = tabIndent - ind;
						physicalXPos += hop * spaceWidth;
						currentColumn++;
						realColumn += hop;
						break;

					case TextWordType.Word:
						string word    = currentWord.Word;
						
						if (physicalXPos + MeasureString(FontContainer.DefaultFont, word) > xPos + spaceWidth/2) {
							do {
								word = word.Substring(0, word.Length - 1);
							} while (physicalXPos + MeasureString(FontContainer.DefaultFont, word) > xPos + spaceWidth/2);
							return currentColumn + word.Length;
						}
						physicalXPos += MeasureString(FontContainer.DefaultFont, word);
						currentColumn += word.Length;
						realColumn += word.Length;
						break;
				}
			}
			return currentColumn; // FIXME!!!
//			return (int)(physicalXPos - textArea.VirtualTop.X * spaceWidth);
		}
		
		public int GetDrawingXPos(int logicalLine, int logicalColumn)
		{
			return GetDrawingXPos(Document.GetLineSegment(logicalLine), logicalColumn);
		}
		
		public int GetDrawingXPos(LineSegment line, int logicalColumn)
		{
			int currentColumn = 0;
			int realColumn = 0;
			float physicalXPos = 0;
			float spaceWidth = GetWidth(' ');
			int tabIndent  = Document.TextEditorProperties.TabIndent;
			LineSegment currentLine = line;
			if (currentLine.Words == null) {
				return (int)(physicalXPos - textArea.VirtualTop.X * spaceWidth);
			}
			for (int i = 0; i < currentLine.Words.Count && currentColumn < logicalColumn; ++i) {
				TextWord currentWord = ((TextWord)currentLine.Words[i]);
				switch (currentWord.Type) {
					case TextWordType.Space:
						physicalXPos += spaceWidth;
						currentColumn++;
						realColumn++;
						break;

					case TextWordType.Tab:
						int ind = realColumn % tabIndent;
						int hop = tabIndent - ind;
						physicalXPos += hop * spaceWidth;
						currentColumn++;
						realColumn += hop;
						break;

					case TextWordType.Word:
						string word    = currentWord.Word;
						if (currentColumn + word.Length > logicalColumn) {
							word = word.Substring(0, logicalColumn - currentColumn);
						}
						float  lastPos = physicalXPos;

						physicalXPos += MeasureString(FontContainer.DefaultFont, word);
						currentColumn += word.Length;
						realColumn += word.Length;
						break;
				}
			}
			return (int)(physicalXPos /*- textArea.VirtualTop.X * spaceWidth*/);
		}
#endregion
		
#region DrawHelper functions
		void DrawBracketHighlight(Gdk.Drawable g, System.Drawing.Rectangle rect)
		{
			using (Gdk.GC gc = new Gdk.GC(g)) {
				gc.RgbFgColor = new Gdk.Color(System.Drawing.Color.FromArgb(50, 0, 0, 255));
				g.DrawRectangle(gc, false, rect);
			}
		}
		
		float MeasureString(Pango.FontDescription font, string s) 
		{
			//Pango.Layout ly = new Pango.Layout(TextArea.PangoContext);
			Pango.Layout ly = _layout;
			ly.SetText(s);
			ly.FontDescription = font;
			int size = (int)Math.Round(ly.Size.Width/1024.0f);
			return size;		
		}
		
        float DrawString(Gdk.Drawable g, Gdk.GC gc, float x, float y, string s) 
		{
			return DrawString(g, gc, FontContainer.DefaultFont, x, y, s);
        }
		
		float DrawString(Gdk.Drawable g, Gdk.GC gc, Pango.FontDescription font, float x, float y, string s) 
		{
			Pango.Layout ly = _layout;
			ly.FontDescription = font;
			ly.SetText(s);
			g.DrawLayout(gc, (int) Math.Round(x), (int) Math.Round(y), ly);
			int size = (int)Math.Round(ly.Size.Width/1024.0f);
			return size;
		}
        
		void DrawInvalidLineMarker(Gdk.Drawable g, float x, float y)
		{
			HighlightColor invalidLinesColor = textArea.Document.HighlightingStrategy.GetColorFor("InvalidLines");
			//g.DrawString("~", invalidLinesColor.Font, new SolidBrush(invalidLinesColor.Color), x, y, measureStringFormat);
            using (Gdk.GC gc = new Gdk.GC(g)) {
				gc.RgbFgColor = new Gdk.Color(invalidLinesColor.Color);
				DrawString(g, gc, x, y, "~");
			}
		}
		
		void DrawSpaceMarker(Gdk.Drawable g, System.Drawing.Color color, float x, float y)
		{
			HighlightColor spaceMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("SpaceMarker");
			//g.DrawString("\u00B7", spaceMarkerColor.Font, new SolidBrush(color), x, y, measureStringFormat);
            using (Gdk.GC gc = new Gdk.GC(g)) {
				gc.RgbFgColor = new Gdk.Color(spaceMarkerColor.Color);
				//DrawString(g, gc, x, y, "\u00B7");
				DrawString(g, gc, x, y, " ");
			}
			
		}
		
		void DrawTabMarker(Gdk.Drawable g, System.Drawing.Color color, float x, float y)
		{
			HighlightColor tabMarkerColor   = textArea.Document.HighlightingStrategy.GetColorFor("TabMarker");
			//g.DrawString("\u00BB", tabMarkerColor.Font, new SolidBrush(color), x, y, measureStringFormat);
            using (Gdk.GC gc = new Gdk.GC(g)) {
				gc.RgbFgColor = new Gdk.Color(tabMarkerColor.Color);
				//DrawString(g, gc, x, y, "\u00BB");
				DrawString(g, gc, x, y, ">");
			}
		}
		
		float DrawEOLMarker(Gdk.Drawable g, System.Drawing.Color color, Gdk.GC gc, float x, float y)
		{
			//string EOLMarker = "\u00B6";
			string EOLMarker = "|";
			float width = MeasureString(FontContainer.DefaultFont, EOLMarker);   
			g.DrawRectangle(gc, true,
			                new Gdk.Rectangle((int)Math.Round(x), (int) Math.Round(y), (int) Math.Round(width), fontHeight));
			HighlightColor eolMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("EolMarker");
			//g.DrawString("\u00B6", eolMarkerColor.Font, new SolidBrush(color), x, y, measureStringFormat);
			using (Gdk.GC tgc = new Gdk.GC(g)) {
				tgc.Copy(gc);
				tgc.RgbFgColor = new Gdk.Color(eolMarkerColor.Color);
				return DrawString(g, tgc, x, y, EOLMarker);
			}
		}
		
		void DrawVerticalRuler(Gdk.Drawable g, System.Drawing.Rectangle lineRectangle)
		{
			if (TextEditorProperties.VerticalRulerRow < textArea.VirtualTop.X) {
				return;
			}
			HighlightColor vRulerColor = textArea.Document.HighlightingStrategy.GetColorFor("VRulerColor");
			int xpos = (int)MeasureString(FontContainer.DefaultFont, "12345678901234567890123456789012345678901234567890123456789012345678901234567890");
			//int xpos = (int)(drawingPosition.Left + GetWidth(g, ' ') * (TextEditorProperties.VerticalRulerRow - textArea.VirtualTop.X));
			using (Gdk.GC gc = new Gdk.GC(g)) {
				gc.RgbFgColor = new Gdk.Color(vRulerColor.Color);

				g.DrawLine(gc,
						xpos,
						lineRectangle.Top,
						xpos,
						lineRectangle.Bottom);
			}
		}
#endregion
	}
}
