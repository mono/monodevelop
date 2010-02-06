// 
// SourceEditorPrintOperation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System;
using Gtk;
using Mono.TextEditor;
using System.Text;

namespace MonoDevelop.SourceEditor
{
	class SourceEditorPrintOperation : PrintOperation
	{
		Document doc;
		ISourceEditorOptions options;
		Mono.TextEditor.Highlighting.Style style;
		
		public SourceEditorPrintOperation (Document doc, ISourceEditorOptions options, Mono.TextEditor.Highlighting.Style style)
		{
			this.doc = doc;
			this.options = options;
			this.style = style;
			
			this.Unit = Unit.Pixel;
		}
		
		protected override void OnBeginPrint (PrintContext context)
		{
			layout = context.CreatePangoLayout ();
			layout.FontDescription = options.Font;
			
			int charWidth;
			layout.FontDescription.Weight = Pango.Weight.Bold;
			layout.SetText (" ");
			layout.GetPixelSize (out charWidth, out this.lineHeight);
			layout.FontDescription.Weight = Pango.Weight.Normal;
			
			pageWidth = context.PageSetup.GetPageWidth (Unit.Pixel);
			pageHeight = context.PageSetup.GetPageHeight (Unit.Pixel);
			double contentHeight = pageHeight - (headerLines > 0? headerPadding : 0) - (footerLines > 0? footerPadding : 0);
			linesPerPage = (int)(contentHeight / lineHeight) - (headerLines + footerLines);
			totalPages = (int)Math.Ceiling ((double)doc.LineCount / linesPerPage); 
			
			NPages = totalPages;
			
			base.OnBeginPrint (context);
		}
		
		protected override void OnEndPrint (PrintContext context)
		{
			layout.Dispose ();
			layout = null;
			base.OnEndPrint (context);
		}
		
		int headerLines = 0;
		int footerLines = 0;
		double headerSeparatorWidth = 0.5;
		double footerSeparatorWidth = 0.5;
		const int headerPadding = 10;
		const int footerPadding = 10;
		
		int totalPages, linesPerPage, lineHeight;
		double pageWidth, pageHeight;
		
		Pango.Layout layout;
		
		string headerText;
		string footerText;
		
		protected override void OnDrawPage (PrintContext context, int pageNr)
		{
			using (var cr = context.CairoContext) {
			double xPos = 0, yPos = 0;
			
			PrintHeader (cr, context, pageNr, ref xPos, ref yPos);
			
			int startLine = pageNr * linesPerPage;
			int endLine = Math.Min (startLine + linesPerPage - 1, doc.LineCount);
			
			//FIXME: use proper 1-layout-per-line
			for (int i = startLine; i < endLine; i++) {
				var line = doc.GetLine (i);
				Chunk startChunk = doc.SyntaxMode.GetChunks (doc, style, line, line.Offset, line.Length);
				for (Chunk chunk = startChunk; chunk != null; chunk = chunk != null ? chunk.Next : null) {
					ChunkStyle chunkStyle = chunk != null ? chunk.GetChunkStyle (style) : null;
					string text = chunk.GetText (doc);
					text = text.Replace ("\t", new string (' ', options.TabSize));
					layout.SetText (text);
					
					var atts = ResetAttributes ();
					
					atts.Insert (new Pango.AttrForeground ( chunkStyle.Color.Red, chunkStyle.Color.Green, chunkStyle.Color.Blue));
					
					if (chunkStyle.Bold) {
						atts.Insert (new Pango.AttrWeight (Pango.Weight.Bold));
					}
					if (chunkStyle.Italic) {
						atts.Insert (new Pango.AttrStyle (Pango.Style.Italic));
					}
					if (chunkStyle.Underline) {
						atts.Insert (new Pango.AttrUnderline (Pango.Underline.Single));
					}
					
					cr.MoveTo (xPos, yPos);
					Pango.CairoHelper.ShowLayout (cr, layout);
					
					int w, h;
					layout.GetPixelSize (out w, out h);
					xPos += w;
					
					if (w > pageWidth)
						break;
				}
				xPos = 0;
				yPos += lineHeight;
			}
			
			PrintFooter (cr, context, pageNr, ref xPos, ref yPos);
			}
		}
		
		Pango.AttrList ResetAttributes ()
		{
			if (layout.Attributes != null) {
				layout.Attributes.Dispose ();
			}
			return layout.Attributes = new Pango.AttrList ();
		}
		
		void PrintHeader (Cairo.Context cr, PrintContext context, int page, ref double xPos, ref double yPos)
		{
			if (headerLines == 0)
				return;
			
			var atts = ResetAttributes ();
			
			layout.SetText (Subst (headerText, page));
			
			int w, h;
			layout.GetPixelSize (out w, out h);
			cr.MoveTo ((pageWidth - w) / 2, yPos);
			Pango.CairoHelper.ShowLayout (cr, layout);
			
			yPos += lineHeight * headerLines;
			
			if (headerSeparatorWidth > 0) {
				cr.LineWidth = 1;
				cr.MoveTo (pageWidth / 3, yPos + (headerPadding / 2));
				cr.LineTo (2 * pageWidth / 3, yPos + (headerPadding / 2));
				cr.Stroke ();
			}
			
			yPos += headerPadding;
		}

		string Subst (string text, int page)
		{
			var sb = new StringBuilder (text);
			sb.Replace ("%N", (page + 1).ToString ());
			sb.Replace ("%Q", totalPages.ToString ());
			return sb.ToString ();
		}
		
		void PrintFooter (Cairo.Context cr, PrintContext context, int page, ref double xPos, ref double yPos)
		{
			if (footerLines == 0)
				return;
			
			yPos = pageHeight - (lineHeight * footerLines) - footerPadding;
			
			if (footerSeparatorWidth > 0) {
				cr.LineWidth = footerSeparatorWidth;
				cr.MoveTo (pageWidth / 3, yPos + (footerPadding / 2));
				cr.LineTo (2 * pageWidth / 3, yPos + (footerPadding / 2));
				cr.Stroke ();
			}
			
			yPos += footerPadding;
			
			var atts = ResetAttributes ();
			
			layout.SetText (Subst (footerText, page));
			
			int w, h;
			layout.GetPixelSize (out w, out h);
			cr.MoveTo ((pageWidth - w) / 2, yPos);
			Pango.CairoHelper.ShowLayout (cr, layout);
		}
		
		public void SetHeaderFormat (string middle)
		{
			headerText = middle;
			headerLines = middle == null || middle.Length == 0? 0 : middle.Split ('\n').Length;
		}
		
		public void SetFooterFormat (string middle)
		{
			footerText = middle;
			footerLines = middle == null || middle.Length == 0? 0 : middle.Split ('\n').Length;
		}
		/*
		protected override Widget OnCreateCustomWidget ()
		{
			return base.OnCreateCustomWidget ();
		}*/
	}
}

