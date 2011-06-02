// 
// MessageBubbleCache.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Fonts;
using Mono.TextEditor.Highlighting;

namespace MonoDevelop.SourceEditor
{
	class MessageBubbleCache : IDisposable
	{
		internal Gdk.Pixbuf errorPixbuf;
		internal Gdk.Pixbuf warningPixbuf;
		
		internal Dictionary<string, LayoutDescriptor> textWidthDictionary = new Dictionary<string, LayoutDescriptor> ();
		internal Dictionary<LineSegment, double> lineWidthDictionary = new Dictionary<LineSegment, double> ();
		
		internal TextEditor editor;
		
		internal Cairo.Color[,,,,] warningMatrix, errorMatrix;
		internal Cairo.Color errorGc, warningGc;
		internal Cairo.Color gcLight, gcSelected;
		
		internal Pango.FontDescription fontDescription;
		internal Gdk.Cursor arrowCursor = new Gdk.Cursor (Gdk.CursorType.Arrow);

		public MessageBubbleCache (TextEditor editor)
		{
			this.editor = editor;
			errorPixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
			warningPixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Warning, Gtk.IconSize.Menu);
			
			editor.EditorOptionsChanged += HandleEditorEditorOptionsChanged;
			fontDescription = FontService.GetFontDescription ("MessageBubbles");
			
			SetColors ();
		}

		public bool RemoveLine (LineSegment line)
		{
			if (!lineWidthDictionary.ContainsKey (line))
				return false;
			lineWidthDictionary.Remove (line);
			return true;
		}

		public void Dispose ()
		{
			editor.EditorOptionsChanged -= HandleEditorEditorOptionsChanged;
			if (textWidthDictionary != null) {
				foreach (var l in textWidthDictionary.Values) {
					l.Layout.Dispose ();
				}
			}
			if (fontDescription != null) {
				fontDescription.Dispose ();
				fontDescription = null;
			}
			arrowCursor.Dispose ();
		}

		static string GetFirstLine (ErrorText errorText)
		{
			string firstLine = errorText.ErrorMessage ?? "";
			int idx = firstLine.IndexOfAny (new [] {'\n', '\r'});
			if (idx > 0)
				firstLine = firstLine.Substring (0, idx);
			return firstLine;
		}

		internal LayoutDescriptor CreateLayoutDescriptor (ErrorText errorText)
		{
			LayoutDescriptor result;
			if (!textWidthDictionary.TryGetValue (errorText.ErrorMessage, out result)) {
				Pango.Layout layout = new Pango.Layout (editor.PangoContext);
				layout.FontDescription = fontDescription;
				layout.SetText (GetFirstLine (errorText));
				int w, h;
				layout.GetPixelSize (out w, out h);
				textWidthDictionary[errorText.ErrorMessage] = result = new LayoutDescriptor (layout, w, h);
			}
			return result;
		}
		
		void SetColors ()
		{
			ColorSheme style = editor.ColorStyle;
			if (style == null)
				style = new DefaultStyle (editor.Style);
			errorGc = (HslColor)(style.GetChunkStyle ("bubble.error").Color);
			warningGc = (HslColor)(style.GetChunkStyle ("bubble.warning").Color);
			errorMatrix = CreateColorMatrix (editor, true);
			warningMatrix = CreateColorMatrix (editor, false);
			
			gcSelected = (HslColor)style.Selection.Color;
			gcLight = new Cairo.Color (1, 1, 1);
		}
		
		void HandleEditorEditorOptionsChanged (object sender, EventArgs e)
		{
			SetColors ();
			lineWidthDictionary.Clear ();
			OnChanged (EventArgs.Empty);
		}	
		
		static void AdjustColorMatrix (Cairo.Color[,,,,] colorMatrix , int side, Cairo.Color baseColor)
		{
			var hsl = (HslColor)baseColor;
			hsl.L *= 1.2;
			colorMatrix [side, 0, 0, 0, 0] = hsl; // light top
			colorMatrix [side, 1, 0, 0, 0] = baseColor; // light below
			
			hsl = (HslColor)baseColor;
			hsl.L *= 1.05;
			colorMatrix [side, 0, 1, 0, 0] = hsl; // dark top
			
			hsl = (HslColor)baseColor;
			hsl.L *= 0.95;
			colorMatrix [side, 1, 1, 0, 0] = hsl; // dark below
			
			hsl = (HslColor)baseColor;
			hsl.L *= 0.98;
			colorMatrix [side, 0, 2, 0, 0] = hsl; // line top 
			
			hsl = (HslColor)baseColor;
			hsl.L *= 0.92;
			colorMatrix [side, 1, 2, 0, 0] = hsl; // line below
			
		}
		
		static Cairo.Color[,,,,] CreateColorMatrix (TextEditor editor, bool isError)
		{
			string typeString = isError ? "error" : "warning";
			Cairo.Color[,,,,] colorMatrix = new Cairo.Color[2, 2, 3, 2, 2];
			
			ColorSheme style = editor.ColorStyle;
			if (style == null)
				style = new DefaultStyle (editor.Style);
			
			var baseColor = style.GetChunkStyle ("bubble." + typeString + "").CairoBackgroundColor;
			
			AdjustColorMatrix (colorMatrix, 0, baseColor);
			
			var hsl = (HslColor)baseColor;
			hsl.S *= 0.6;
			baseColor = hsl;
			AdjustColorMatrix (colorMatrix, 1, hsl);
			
			double factor = 1.03;
			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < 2; j++) {
					for (int k = 0; k < 3; k++) {
						HslColor color = colorMatrix [i, j, k, 0, 0];
						color.L *= factor;
						colorMatrix [i, j, k, 1, 0] = color;
					}
				}
			}
			var selectionColor = ColorSheme.ToCairoColor (style.Selection.BackgroundColor);
			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < 2; j++) {
					for (int k = 0; k < 3; k++) {
						for (int l = 0; l < 2; l++) {
							var color = colorMatrix [i, j, k, l, 0];
							colorMatrix [i, j, k, l, 1] = new Cairo.Color ((color.R + selectionColor.R * 1.5) / 2.5, (color.G + selectionColor.G * 1.5) / 2.5, (color.B + selectionColor.B * 1.5) / 2.5);
						}
					}
				}
			}
			return colorMatrix;
		}
		
		internal class LayoutDescriptor
		{
			public Pango.Layout Layout { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }

			public LayoutDescriptor (Pango.Layout layout, int width, int height)
			{
				this.Layout = layout;
				this.Width = width;
				this.Height = height;
			}
		}
	
		protected virtual void OnChanged (EventArgs e)
		{
			EventHandler handler = this.Changed;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Changed;
	}
}

