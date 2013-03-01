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
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor
{
	class SourceEditorPrintOperation : PrintOperation
	{
		TextDocument doc;
		FilePath filename;
		SourceEditorPrintSettings settings;
		
		public SourceEditorPrintOperation (TextDocument doc, FilePath filename)
		{
			this.doc = doc;
			this.filename = filename;
			this.settings = SourceEditorPrintSettings.Load ();
			
			this.Unit = Unit.Pixel;
		}
		
		protected override void OnBeginPrint (PrintContext context)
		{
			layout = PangoUtil.CreateLayout (context);
			layout.FontDescription = settings.Font;
			
			layout.FontDescription.Weight = Pango.Weight.Bold;
			layout.SetText (" ");
			int w, h;
			layout.GetSize (out w, out h);
			this.lineHeight = h / Pango.Scale.PangoScale;
			layout.FontDescription.Weight = Pango.Weight.Normal;
			
			SetHeaderFormat (settings.HeaderFormat);
			SetFooterFormat (settings.FooterFormat);
			
			style = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (settings.ColorScheme);
			
			pageWidth = context.PageSetup.GetPageWidth (Unit.Pixel);
			pageHeight = context.PageSetup.GetPageHeight (Unit.Pixel);
			double contentHeight = pageHeight
				- (headerLines > 0? settings.HeaderPadding : 0) 
				- (footerLines > 0? settings.FooterPadding : 0);
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
		
		int totalPages, linesPerPage;
		double lineHeight;
		double pageWidth, pageHeight;
		
		Pango.Layout layout;
		Mono.TextEditor.Highlighting.ColorScheme style;
		
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
					var line = doc.GetLine (i + 1);
					if (!settings.UseHighlighting) {
						string text = doc.GetTextAt (line);
						text = text.Replace ("\t", new string (' ', settings.TabSize));
					
						layout.SetText (text);
						cr.MoveTo (xPos, yPos);
						Pango.CairoHelper.ShowLayout (cr, layout);
					
						yPos += lineHeight;
						continue;
					}
					
					var startChunk = doc.SyntaxMode.GetChunks (style, line, line.Offset, line.LengthIncludingDelimiter);
					foreach (Chunk chunk in startChunk) {
						var chunkStyle = chunk != null ? style.GetChunkStyle (chunk) : null;
						string text = doc.GetTextAt (chunk);
						text = text.Replace ("\t", new string (' ', settings.TabSize));
						layout.SetText (text);
					
						var atts = ResetAttributes ();
					
						atts.Insert (new Pango.AttrForeground ((ushort)(chunkStyle.Foreground.R * ushort.MaxValue), (ushort)(chunkStyle.Foreground.G * ushort.MaxValue), (ushort)(chunkStyle.Foreground.B * ushort.MaxValue)));
					
						if (chunkStyle.FontWeight != Xwt.Drawing.FontWeight.Normal) {
							atts.Insert (new Pango.AttrWeight ((Pango.Weight)chunkStyle.FontWeight));
						}
						if (chunkStyle.FontStyle != Xwt.Drawing.FontStyle.Normal) {
							atts.Insert (new Pango.AttrStyle ((Pango.Style)chunkStyle.FontStyle));
						}
						if (chunkStyle.Underline) {
							atts.Insert (new Pango.AttrUnderline (Pango.Underline.Single));
						}
					
						cr.MoveTo (xPos, yPos);
						Pango.CairoHelper.ShowLayout (cr, layout);
					
						int wout, hout;
						layout.GetSize (out wout, out hout);
						double w = wout / Pango.Scale.PangoScale;
						
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
			
			ResetAttributes ();
			
			layout.SetText (Subst (headerText, page));
			
			int wout, hout;
			layout.GetSize (out wout, out hout);
			double w = wout / Pango.Scale.PangoScale;
			
			cr.MoveTo ((pageWidth - w) / 2, yPos);
			Pango.CairoHelper.ShowLayout (cr, layout);
			
			yPos += lineHeight * headerLines;
			
			if (settings.HeaderSeparatorWeight > 0) {
				cr.LineWidth = settings.HeaderSeparatorWeight;
				cr.MoveTo (pageWidth / 3, yPos + (settings.HeaderPadding / 2));
				cr.LineTo (2 * pageWidth / 3, yPos + (settings.HeaderPadding / 2));
				cr.Stroke ();
			}
			
			yPos += settings.HeaderPadding;
		}

		string Subst (string text, int page)
		{
			var sb = new StringBuilder (text);
			sb.Replace ("%N", (page + 1).ToString ());
			sb.Replace ("%Q", totalPages.ToString ());
			//FIXME: use font width for ellipsizing better 
			sb.Replace ("%F", SourceEditorWidget.EllipsizeMiddle (filename, 60));
			return sb.ToString ();
		}
		
		void PrintFooter (Cairo.Context cr, PrintContext context, int page, ref double xPos, ref double yPos)
		{
			if (footerLines == 0)
				return;
			
			yPos = pageHeight - (lineHeight * footerLines) - settings.FooterPadding;
			
			if (settings.FooterSeparatorWeight > 0) {
				cr.LineWidth = settings.FooterSeparatorWeight;
				cr.MoveTo (pageWidth / 3, yPos + (settings.FooterPadding / 2));
				cr.LineTo (2 * pageWidth / 3, yPos + (settings.FooterPadding / 2));
				cr.Stroke ();
			}
			
			yPos += settings.FooterPadding;
			
			ResetAttributes ();
			
			layout.SetText (Subst (footerText, page));
			
			int wout, hout;
			layout.GetSize (out wout, out hout);
			double w = wout / Pango.Scale.PangoScale;
			
			cr.MoveTo ((pageWidth - w) / 2, yPos);
			Pango.CairoHelper.ShowLayout (cr, layout);
		}
		
		void SetHeaderFormat (string middle)
		{
			headerText = middle;
			headerLines = middle == null || middle.Length == 0? 0 : middle.Split ('\n').Length;
		}
		
		void SetFooterFormat (string middle)
		{
			footerText = middle;
			footerLines = middle == null || middle.Length == 0? 0 : middle.Split ('\n').Length;
		}
		
		protected override void OnDone (PrintOperationResult result)
		{
			if (result == PrintOperationResult.Apply) {
				settings.Save ();
			}
			base.OnDone (result);
		}

		/*
		//FIXME: implement custom print settings widget
		protected override Widget OnCreateCustomWidget ()
		{
			return new PrintSettingsWidget (this.settings);
		}
		
		protected override void OnCustomWidgetApply (Widget widget)
		{
			((PrintSettingsWidget)widget).ApplySettings ();
		}*/
	}
	
	//these should be stored
	class SourceEditorPrintSettings
	{
		public static SourceEditorPrintSettings Load ()
		{
			return new SourceEditorPrintSettings ();
		}
		
		public void Save ()
		{
		}
		
		private SourceEditorPrintSettings ()
		{
			Font = DefaultSourceEditorOptions.Instance.Font;
			TabSize = DefaultSourceEditorOptions.Instance.TabSize;
			HeaderFormat = "%F";
			FooterFormat = GettextCatalog.GetString ("Page %N of %Q");
			ColorScheme = "default";
			HeaderSeparatorWeight = FooterSeparatorWeight = 0.5;
			HeaderPadding = FooterPadding = 6;
			UseHighlighting = true;
		}
		
		public bool UseHighlighting { get; private set; }
		public Pango.FontDescription Font { get; private set; }
		public int TabSize { get; private set; }
		public string ColorScheme { get; private set; }
		
		public string HeaderFormat { get; private set; }
		public string FooterFormat { get; private set; }
		public double HeaderSeparatorWeight { get; private set; }
		public double FooterSeparatorWeight { get; private set; }
		public double HeaderPadding { get; private set; }
		public double FooterPadding { get; private set; }
		
		//not yet implemented
		public Pango.FontDescription HeaderFooterFont { get; private set; }
		public Pango.FontDescription LineNumberFont { get; private set; }
		public bool ShowLineNumbers  { get; private set; }
		public bool WrapLines { get; private set; }
		
	}
}

