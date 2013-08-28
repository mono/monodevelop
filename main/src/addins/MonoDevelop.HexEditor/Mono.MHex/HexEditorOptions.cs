// 
// DefaultHexEditorOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using Xwt.Drawing;

namespace Mono.MHex
{
	class HexEditorOptions : IHexEditorOptions, IDisposable
	{
		public const string DEFAULT_FONT = "Mono 10";
		static HexEditorOptions options = new HexEditorOptions ();
		public static HexEditorOptions DefaultOptions {
			get {
				return options;
			}
		}
		double zoom = 1.0;
		public double Zoom {
			get {
				return zoom;
			}
			set {
				zoom = value;
				DisposeFont ();
				OnChanged (EventArgs.Empty);
			}
		}
		public bool CanZoomIn {
			get {
				return zoom < 8.0;
			}
		}
		public bool CanZoomOut {
			get {
				return zoom > 0.7;
			}
		}
		public bool CanResetZoom {
			get {
				return zoom != 1.0;
			}
		}
		public void ZoomIn ()
		{
			zoom *= 1.1;
			Zoom = System.Math.Min (8.0, System.Math.Max (0.7, zoom));
		}
		public void ZoomOut ()
		{
			zoom *= 0.9;
			Zoom = System.Math.Min (8.0, System.Math.Max (0.7, zoom));
		}
		public void ZoomReset ()
		{
			Zoom = 1.0;
		}
		bool showIconMargin = true;
		public virtual bool ShowIconMargin {
			get {
				return showIconMargin;
			}
			set {
				showIconMargin = value;
				OnChanged (EventArgs.Empty);
			}
		}
		bool showLineNumberMargin = true;
		public virtual bool ShowLineNumberMargin {
			get {
				return showLineNumberMargin;
			}
			set {
				showLineNumberMargin = value;
				OnChanged (EventArgs.Empty);
			}
		}
		string fontName = DEFAULT_FONT;
		public virtual string FontName {
			get {
				return fontName;
			}
			set {
				if (fontName != value) {
					DisposeFont ();
					fontName = !String.IsNullOrEmpty (value) ? value : DEFAULT_FONT;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		int groupBytes = 1;
		public virtual int GroupBytes {
			get {
				return groupBytes;
			}
			set {
				if (groupBytes != value) {
					groupBytes = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		Font font;
		public Font Font {
			get {
				if (font == null) {
					try {
						font = Font.FromName (FontName);
					}
					catch {
						Console.WriteLine ("Could not load font: {0}", FontName);
					}
					if (font == null || String.IsNullOrEmpty (font.Family))
						font = Font.FromName (DEFAULT_FONT);
					if (font != null)
						font = font.WithSize (font.Size * Zoom);
				}
				return font;
			}
		}
		void DisposeFont ()
		{
			if (font != null) {
				//				font.Dispsoe ();
				font = null;
			}
		}
		public virtual void Dispose ()
		{
			DisposeFont ();
		}
		public void RaiseChanged ()
		{
			OnChanged (EventArgs.Empty);
		}
		protected void OnChanged (EventArgs args)
		{
			if (Changed != null)
				Changed (null, args);
		}
		public event EventHandler Changed;
	}
}
