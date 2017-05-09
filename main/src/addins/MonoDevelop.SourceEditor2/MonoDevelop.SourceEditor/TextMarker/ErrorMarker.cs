//
// ErrorMarker.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.SourceEditor
{
	class ErrorMarker : UnderlineTextSegmentMarker, IErrorMarker
	{
		readonly static Cairo.Color defaultColor = new Cairo.Color(0, 0, 0);

		readonly Error info;

		public ErrorMarker (Error info, int offset, int length) : base (defaultColor, new TextSegment (offset, length))
		{
			this.info = info;
			this.Wave = true;
		}

		public override void Draw (Mono.TextEditor.MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics, int startOffset, int endOffset)
		{
			Color = SyntaxHighlightingService.GetColor (editor.EditorTheme, info.ErrorType == ErrorType.Warning ? EditorThemeColors.UnderlineWarning : EditorThemeColors.UnderlineError);
			base.Draw (editor, cr, metrics, startOffset, endOffset);
		}

		event EventHandler<TextMarkerMouseEventArgs> ITextSegmentMarker.MousePressed {
			add {
				throw new NotImplementedException ();
			}
			remove {
				throw new NotImplementedException ();
			}
		}

		event EventHandler<TextMarkerMouseEventArgs> ITextSegmentMarker.MouseHover {
			add {
				throw new NotImplementedException ();
			}
			remove {
				throw new NotImplementedException ();
			}
		}

		object ITextSegmentMarker.Tag {
			get;
			set;
		}

		public Error Error {
			get {
				return info;
			}
		}
	}
}

