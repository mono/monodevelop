//
// WavedLineMarker.cs
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
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using MonoDevelop.Debugger;
using Pango;

namespace MonoDevelop.SourceEditor
{
	class GenericUnderlineMarker : UnderlineTextSegmentMarker, MonoDevelop.Ide.Editor.IGenericTextSegmentMarker
	{
		HslColor MonoDevelop.Ide.Editor.IGenericTextSegmentMarker.Color {
			get {
				return Color;
			}
			set {
				Color = value;
			}
		}

		public GenericUnderlineMarker (ISegment segment, MonoDevelop.Ide.Editor.TextSegmentMarkerEffect effect) : base (null, segment, effect)
		{
		}

		#pragma warning disable 0067
		public event EventHandler<MonoDevelop.Ide.Editor.TextMarkerMouseEventArgs> MousePressed;

		public event EventHandler<MonoDevelop.Ide.Editor.TextMarkerMouseEventArgs> MouseHover;
		#pragma warning restore 0067

		object MonoDevelop.Ide.Editor.ITextSegmentMarker.Tag {
			get;
			set;
		}


	}
}

