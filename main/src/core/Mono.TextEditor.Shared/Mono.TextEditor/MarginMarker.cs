//
// MarginMarker.cs
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

using System;
using Gdk;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	/// <summary>
	/// Contains all information about a margin draw event. This class is used inside <see cref="MarginMarker"/>.
	/// </summary>
	class MarginDrawMetrics
	{
		/// <summary>
		/// The margin that should be drawn.
		/// </summary>
		public Margin Margin { get; private set; }

		/// <summary>
		/// The area that needs to be redrawn.
		/// </summary>
		public Cairo.Rectangle Area { get; private set; }

		/// <summary>
		/// The line segment the margin should draw (can be null).
		/// </summary>
		public DocumentLine LineSegment { get; private set; }

		/// <summary>
		/// The line number of the line segment (can be negative, if the line segment is null).
		/// </summary>
		public long LineNumber { get; private set; }

		/// <summary>
		/// The X position of the margin.
		/// </summary>
		public double X { get; private set; }

		/// <summary>
		/// The Y postion of the margin draw event.
		/// </summary>
		public double Y { get; private set; }

		/// <summary>
		/// The Width of the margin.
		/// </summary>
		public double Width { get { return Margin.Width; } }

		/// <summary>
		/// The height of the current line.
		/// </summary>
		public double Height { get; private set; }

		/// <summary>
		/// X + Width
		/// </summary>
		public double Right  { get { return X + Width; } }

		/// <summary>
		/// Y + Height
		/// </summary>
		public double Bottom { get { return Y + Height; } }

		internal MarginDrawMetrics (Margin margin, Cairo.Rectangle area, DocumentLine lineSegment, long lineNumber, double x, double y, double height)
		{
			this.Margin = margin;
			this.Area = area;
			this.LineSegment = lineSegment;
			this.LineNumber = lineNumber;
			this.X = x;
			this.Y = y;
			this.Height = height;
		}
	}

	/// <summary>
	/// The margin marker is a specialized text line marker used to change how a margins of a line are drawn.
	/// (If the margin supports custom drawing plugins)
	/// Note: This is not used for the text view margin, which is handled by the basic TextLineMarker class.
	/// </summary>
	class MarginMarker : TextLineMarker
	{
		/// <summary>
		/// Determines whether this margin marker can draw the background of the specified margin.
		/// </summary>
		public virtual bool CanDrawBackground (Margin margin) 
		{
			return false;
		}

		/// <summary>
		/// Determines whether this margin marker can draw the foreground of the specified margin.
		/// </summary>
		public virtual bool CanDrawForeground (Margin margin)
		{
			return false;
		}

		internal bool CanDraw (Margin margin)
		{
			return CanDrawForeground (margin) || CanDrawBackground (margin);
		}

		/// <summary>
		/// Draws the foreground of the specified margin.
		/// </summary>
		public virtual void DrawForeground (MonoTextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
		}

		/// <summary>
		/// Draws the background of the specified margin.
		/// </summary>
		/// <returns>true, if the background is drawn. false if the margin should fallback to the default background renderer. </returns>
		public virtual bool DrawBackground (MonoTextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			return false;
		}

		/// <summary>
		/// Informs the margin marker of a mouse press event.
		/// </summary>
		/// <param name="editor">The text editor in which the event press occured.</param>
		/// <param name="margin">The margin in which the event occured.</param>
		/// <param name="args">The event arguments.</param>
		public virtual void InformMousePress (MonoTextEditor editor, Margin margin, MarginMouseEventArgs args)
		{
		}

		/// <summary>
		/// Informs the margin marker of a mouse release event.
		/// </summary>
		/// <param name="editor">The text editor in which the event press occured.</param>
		/// <param name="margin">The margin in which the event occured.</param>
		/// <param name="args">The event arguments.</param>
		public virtual void InformMouseRelease (MonoTextEditor editor, Margin margin, MarginMouseEventArgs args)
		{
		}

		/// <summary>
		/// Informs the margin marker of a mouse hover event.
		/// </summary>
		/// <param name="editor">The text editor in which the event press occured.</param>
		/// <param name="margin">The margin in which the event occured.</param>
		/// <param name="args">The event arguments.</param>
		public virtual void InformMouseHover (MonoTextEditor editor, Margin margin, MarginMouseEventArgs args)
		{
		}

		public virtual void UpdateAccessibilityDetails (out string label, out string help)
		{
			label = "";
			help = "";
		}
	}
}