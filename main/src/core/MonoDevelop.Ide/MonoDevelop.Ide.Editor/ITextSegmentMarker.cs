//
// ITextSegmentMarker.cs
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
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Editor
{
	public interface ITextSegmentMarker : ISegment
	{
		bool IsVisible {
			get;
			set;
		}

		object Tag {
			get;
			set;
		}

		event EventHandler<TextMarkerMouseEventArgs> MousePressed;
		event EventHandler<TextMarkerMouseEventArgs> MouseHover;
	}

	public enum TextSegmentMarkerEffect {
		/// <summary>
		/// The region is marked as waved underline.
		/// </summary>
		WavedLine,

		/// <summary>
		/// The region is marked as dotted line.
		/// </summary>
		DottedLine,

		/// <summary>
		/// The text is grayed out.
		/// </summary>
		GrayOut,

		/// <summary>
		/// Just a simple underline
		/// </summary>
		Underline
	}

	public interface IGenericTextSegmentMarker : ITextSegmentMarker
	{
		TextSegmentMarkerEffect Effect { get; }

		HslColor Color { get; set; }
	}

	public interface IErrorMarker : ITextSegmentMarker
	{
		Error Error { get; }
	}

	public interface ISmartTagMarker : ITextSegmentMarker
	{
		bool IsInsideSmartTag (double x, double y);

		event EventHandler ShowPopup;
		event EventHandler CancelPopup;
	}

	public interface ILinkTextMarker : ITextSegmentMarker
	{
		bool OnlyShowLinkOnHover { get; set; }
	}
}

