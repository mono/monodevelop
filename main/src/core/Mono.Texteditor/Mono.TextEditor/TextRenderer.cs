// 
// TextRenderer.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Gdk;
using Gtk;
using SD = System.Drawing;

namespace Mono.TextEditor
{
	abstract class TextRenderer: IDisposable
	{
		protected string family;
		protected Pango.Style style;
		protected Pango.Weight weight;
		protected int size;
		protected PropFlag changed;
		protected Gdk.Color color;
		
		[Flags]
		public enum PropFlag {
			None = 0,
			Family = 0x01,
			Weight = 0x02,
			Style = 0x04,
			Size = 0x08,
			Color = 0x10,
			All = 0x1f
		}
		
		public static TextRenderer Create (TextViewMargin textView, TextEditor editor)
		{
			if (Platform.IsWindows)
				return new GdiPlusTextRenderer ();
			else
				return new CairoTextRenderer ();
//			return new PangoTextRenderer (this, textEditor);
		}
		
		public virtual void BeginDraw (Gdk.Drawable drawable)
		{
		}
		
		public virtual void EndDraw ()
		{
		}
		
		public virtual void Dispose ()
		{
		}
		
		public void SentFont (Pango.FontDescription font)
		{
			Family = font.Family;
			Style = font.Style;
			Weight = font.Weight;
			Size = font.Size;
		}
		
		public abstract void SetText (string text);
		
		public string Family {
			get { return family; }
			set {
				if (family != value) {
					family = value;
					changed |= PropFlag.Family;
				}
			}
		}
		
		public Pango.Style Style {
			get {
				return style;
			}
			set {
				if (style != value) {
					style = value;
					changed |= PropFlag.Style;
				}
			}
		}
		
		public Pango.Weight Weight {
			get {
				return weight;
			}
			set {
				if (weight != value) {
					weight = value;
					changed |= PropFlag.Weight;
				}
			}
		}
		
		public int Size {
			get { return size; }
			set {
				if (size != value) {
					size = value;
					changed |= PropFlag.Size;
				}
			}
		}
		
		public Gdk.Color Color {
			get { return color; }
			set {
				// Don't use Equals since it requires an unmanaged transition, much slower than this
				if (value.Red != color.Red || value.Green != color.Green || value.Blue != color.Blue) {
					color = value;
					changed |= PropFlag.Color;
				}
			}
		}
		
		public abstract void SetClip (Gdk.Rectangle clip);
		
		public abstract void GetCharSize (out int w, out int h);
		
		public abstract void GetPixelSize (out int width, out int height, out int advanceX);
		
		public abstract void DrawText (Gdk.Drawable drawable, int x, int y);
	}
	
	class PangoTextRenderer: TextRenderer
	{
		Pango.Layout layout;
		TextViewMargin textView;
		
		public PangoTextRenderer (TextViewMargin textView, TextEditor editor)
		{
			this.textView = textView;
			layout = new Pango.Layout (editor.PangoContext);
			layout.Alignment = Pango.Alignment.Left;
			layout.FontDescription = editor.Options.Font;
		}
		
		public override void Dispose ()
		{
			layout = layout.Kill ();
		}
		
		void UpdateFont ()
		{
			if ((changed & PropFlag.Family) != 0)
				layout.FontDescription.Family = family;
			if ((changed & PropFlag.Style) != 0)
				layout.FontDescription.Style = style;
			if ((changed & PropFlag.Weight) != 0)
				layout.FontDescription.Weight = weight;
			if ((changed & PropFlag.Size) != 0)
				layout.FontDescription.Size = size;
			changed = PropFlag.None;
		}
		
		public override void SetClip (Gdk.Rectangle clip)
		{
			// All the GCs already have the clip set, so there is nothing else to do
		}
		
		public override void GetCharSize (out int w, out int h)
		{
			layout.SetText (" ");
			int d;
			GetPixelSize (out w, out h, out d);
		}

		
		public override void SetText (string text)
		{
			layout.SetText (text);
		}
		
		public override void GetPixelSize (out int width, out int height, out int advanceX)
		{
			UpdateFont ();
			layout.GetPixelSize (out width, out height);
			advanceX = width;
		}
		
		public override void DrawText (Gdk.Drawable drawable, int x, int y)
		{
			UpdateFont ();
			drawable.DrawLayout (textView.GetGC (color), x, y, layout);
		}
	}

	class CairoTextRenderer: TextRenderer
	{
		Cairo.Context cc;
		string text;
		int lineh, ascent;
		
		static Cairo.Surface helperSurface;
		
		public override void BeginDraw (Gdk.Drawable drawable)
		{
			DisposeContext ();
			cc = Gdk.CairoHelper.Create (drawable);
			cc.SetSourceRGBA (0,0,0,1);
			changed = PropFlag.All;
		}
		
		public override void EndDraw ()
		{
			DisposeContext ();
		}
		
		public override void Dispose ()
		{
			DisposeContext ();
		}
		
		void DisposeContext ()
		{
			if (cc != null) {
				((IDisposable)cc).Dispose ();
				cc = null;
			}
		}
		
		public override void SetClip (Gdk.Rectangle clip)
		{
			cc.ResetClip ();
			Gdk.CairoHelper.Rectangle (cc, clip);
			cc.Clip ();
		}
		
		public override void SetText (string text)
		{
			this.text = text;
		}
		
		public bool UpdateFont ()
		{
			if (string.IsNullOrEmpty (family))
				return false;
			if (cc == null) {
				if (helperSurface == null) {
					helperSurface = new Cairo.ImageSurface (Cairo.Format.ARGB32, 10, 10);
				}
				cc = new Cairo.Context (helperSurface);
				cc.Scale (1, 1);
				changed = PropFlag.All;
			}
			if ((changed & (PropFlag.Family | PropFlag.Style | PropFlag.Weight)) != 0) {
				Cairo.FontSlant fontSlant;
				switch (style) {
					case Pango.Style.Italic: fontSlant = Cairo.FontSlant.Italic; break;
					case Pango.Style.Oblique: fontSlant = Cairo.FontSlant.Oblique; break;
					default: fontSlant = Cairo.FontSlant.Normal; break;
				}
				Cairo.FontWeight fontWeight;
				if (weight == Pango.Weight.Normal)
					fontWeight = Cairo.FontWeight.Normal;
				else
					fontWeight = Cairo.FontWeight.Bold;
				cc.SelectFontFace (family, fontSlant, fontWeight);
			}
			if ((changed & PropFlag.Size) != 0) {
				double ts = Pango.Units.ToPixels (size);
				ts *= (Gdk.Screen.Default.Resolution / 72);
				cc.SetFontSize (ts);
			}
			if ((changed & PropFlag.Color) != 0) {
				cc.Color = Mono.TextEditor.Highlighting.Style.ToCairoColor (color);
			}
			if (changed != PropFlag.None) {
				Cairo.FontExtents fext = cc.FontExtents;
				lineh = (int) (fext.Ascent + fext.Descent);
				ascent = (int) fext.Ascent;
			}
			changed = PropFlag.None;
			return true;
		}
		
		public override void GetCharSize (out int w, out int h)
		{
			if (!UpdateFont ()) {
				w = h = 0;
				return;
			}
			Cairo.TextExtents extents = cc.TextExtents (" ");
			w = (int) extents.XAdvance;
			h = lineh;
		}

		
		public override void GetPixelSize (out int width, out int height, out int advanceX)
		{
			if (!UpdateFont ()) {
				width = height = advanceX = 0;
				return;
			}
			
			Cairo.TextExtents extents = cc.TextExtents (text);
			
			// If the font uses kerning, XAdvance may be smaller than the width of the text.
			// In this case, return the width.
			width = (int) System.Math.Max (extents.XAdvance, (extents.XBearing + extents.Width));
			height = lineh;
			advanceX = (int) extents.XAdvance;
		}
		
		public override void DrawText (Gdk.Drawable drawable, int x, int y)
		{
			if (UpdateFont ()) {
				cc.MoveTo (x, y + ascent);
				cc.ShowText (text);
			}
		}
	}

	class GdiPlusTextRenderer: TextRenderer
	{
		SD.Graphics gr;
		SD.Font font;
		SD.Brush brush;
		string text;
		static SD.Image helperImage;

		Dictionary<string, SD.Font> fontCache = new Dictionary<string, SD.Font> ();
		Dictionary<SD.Color, SD.Brush> brushCache = new Dictionary<SD.Color, SD.Brush> ();
		static SD.StringFormat format;

		static GdiPlusTextRenderer ( )
		{
			format = SD.StringFormat.GenericTypographic;
//			SD.StringFormat f = new SD.StringFormat ();
//			f = (SD.StringFormat) SD.StringFormat.GenericTypographic.Clone ();
//			f.FormatFlags = SD.StringFormatFlags.MeasureTrailingSpaces | SD.StringFormatFlags.LineLimit | SD.StringFormatFlags.FitBlackBox | SD.StringFormatFlags.NoClip | SD.StringFormatFlags.NoWrap;
		}

		public GdiPlusTextRenderer ( )
		{
			font = new SD.Font ("Courier New", 10);
			brush = new SD.SolidBrush (SD.Color.Black);
			changed = PropFlag.All;
		}

		public override void Dispose ( )
		{
			DisposeGraphics ();
			foreach (SD.Font f in fontCache.Values)
				f.Dispose ();
			foreach (SD.Brush b in brushCache.Values)
				b.Dispose ();
		}

		void DisposeGraphics ( )
		{
			if (gr != null) {
				gr.Dispose ();
				gr = null;
			}
		}

		public override void BeginDraw (Drawable drawable)
		{
			DisposeGraphics ();
			gr = Gtk.DotNet.Graphics.FromDrawable (drawable);
		}

		public override void EndDraw ( )
		{
			DisposeGraphics ();
		}

		public override void SetText (string text)
		{
			this.text = text;
		}

		public override void SetClip (Rectangle clip)
		{
			gr.SetClip (new SD.Rectangle (clip.X, clip.Y, clip.Width, clip.Height));
		}

		public override void GetCharSize (out int w, out int h)
		{
			if (!UpdateFont ()) {
				w = h = 0;
				return;
			}

			// MeasureString can't mesure whitespace, so we need to do a trick to measure it
			SD.SizeF size = gr.MeasureString ("||", font, new SD.PointF (0, 0), format);
			SD.SizeF size2 = gr.MeasureString ("| |", font, new SD.PointF (0, 0), format);
			w = (int) (size2.Width - size.Width);
			h = (int) size.Height;
		}

		public override void GetPixelSize (out int width, out int height, out int advanceX)
		{
			if (!UpdateFont ()) {
				width = height = advanceX = 0;
				return;
			}
			SD.SizeF size = gr.MeasureString (text, font, new SD.PointF (0, 0), format);
			width = (int) size.Width;
			height = (int) size.Height;
			advanceX = width;
		}

		public override void DrawText (Drawable drawable, int x, int y)
		{
			if (UpdateFont ())
				gr.DrawString (text, font, brush, (float) x, (float) y, format);
		}

		bool UpdateFont ( )
		{
			if (family == null)
				return false;

			if (gr == null) {
				if (helperImage == null)
					helperImage = new SD.Bitmap (1, 1);
				gr = System.Drawing.Graphics.FromImage (helperImage);
			}
			if ((changed & PropFlag.Color) != 0) {
				SD.Color col = SD.Color.FromArgb ((byte) (color.Red / 256), (byte) (color.Green / 256), (byte) (color.Blue / 256));
				if (!brushCache.TryGetValue (col, out brush)) {
					brush = new SD.SolidBrush (col);
					brushCache[col] = brush;
				}
				changed &= ~PropFlag.Color;
			}
			if (changed != PropFlag.None) {
				string key = family + "-" + style + "-" + weight + " " + size;
				if (!fontCache.TryGetValue (key, out font)) {
					SD.FontStyle sdstyle = SD.FontStyle.Regular;
					if (style == Pango.Style.Italic || style == Pango.Style.Oblique)
						sdstyle = SD.FontStyle.Italic;

					if (weight != Pango.Weight.Normal)
						sdstyle |= System.Drawing.FontStyle.Bold;

					font = new SD.Font (family, Pango.Units.ToPixels (size), sdstyle);
					fontCache[key] = font;
				}

				changed = PropFlag.None;
			}
			return true;
		}
	}
}
