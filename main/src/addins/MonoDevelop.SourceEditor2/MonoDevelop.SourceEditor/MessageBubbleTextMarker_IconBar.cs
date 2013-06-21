//
// MessageBubbleTextMarker_IconBar.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	partial class MessageBubbleTextMarker : IIconBarMarker
	{ 
		bool IIconBarMarker.CanDrawBackground { get { return true; } }
		
		void IIconBarMarker.DrawBackground (TextEditor ed, Cairo.Context cr, DocumentLine line, int lineNumber, double x, double y, double width, double height)
		{
			cr.Rectangle (x, y, width, height);
			cr.Color = LineColor.SecondColor;
			cr.Fill ();

			cr.MoveTo (x + width - 0.5, y);
			cr.LineTo (x + width - 0.5, y + height);
			cr.Color = LineColor.BorderColor;
			cr.Stroke ();

			if (cache.CurrentSelectedTextMarker != null && cache.CurrentSelectedTextMarker != this) {
				cr.Rectangle (x, y, width, height);
				cr.Color = new Cairo.Color (ed.ColorStyle.IndicatorMargin.Color.R,
				                            ed.ColorStyle.IndicatorMargin.Color.G,
				                            ed.ColorStyle.IndicatorMargin.Color.B, 0.5);
				cr.Fill ();

			}

		}

		void IIconBarMarker.DrawIcon (TextEditor ed, Cairo.Context cr, DocumentLine line, int lineNumber, double x, double y, double width, double height)
		{
			cr.Save ();
			cr.Translate (
				x + 0.5  + (width - cache.errorPixbuf.Width) / 2,
				y + 0.5 + (height - cache.errorPixbuf.Height) / 2
			);
			Gdk.CairoHelper.SetSourcePixbuf (
				cr,
				errors.Any (e => e.IsError) ? cache.errorPixbuf : cache.warningPixbuf, 0, 0);
			cr.Paint ();
			cr.Restore ();

		}

		void IIconBarMarker.MousePress (MarginMouseEventArgs args)
		{
		}

		void IIconBarMarker.MouseRelease (MarginMouseEventArgs args)
		{
		}

		void IIconBarMarker.MouseHover (MarginMouseEventArgs args)
		{
			var sb = new System.Text.StringBuilder ();
			foreach (var error in errors) {
				if (sb.Length > 0)
					sb.AppendLine ();
				sb.Append (error.ErrorMessage);
			}
			args.Editor.TooltipText = sb.ToString ();
		}
	}
}

