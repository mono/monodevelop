//
// ISegmentMarkerHost.cs
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

namespace MonoDevelop.Ide.Editor
{
	public enum LinkRequest
	{
		SameView,
		RequestNewView
	}

	public interface IMarkerHost
	{
		#region Line marker
		IUrlTextLineMarker CreateUrlTextMarker (IDocumentLine line, string value, UrlType url, string syntax, int startCol, int endCol);
		ICurrentDebugLineTextMarker CreateCurrentDebugLineTextMarker ();
		ITextLineMarker CreateAsmLineMarker ();
		IUnitTestMarker CreateUnitTestMarker (UnitTestMarkerHost host, UnitTestLocation unitTestLocation);
		#endregion

		#region Segment marker
		ITextSegmentMarker CreateUsageMarker (Usage usage);
		ITextSegmentMarker CreateLinkMarker (int offset, int length, Action<LinkRequest> activateLink);

		IGenericTextSegmentMarker CreateGenericTextSegmentMarker (TextSegmentMarkerEffect effect, int offset, int length);
		ISmartTagMarker CreateSmartTagMarker (int offset, DocumentLocation realLocation);

		#endregion
//
//		public class BreakpointTextMarker : DebugTextMarker
//		{
//			public BreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
//			{
//				IsTracepoint = tracepoint;
//			}
//
//			public bool IsTracepoint {
//				get; private set;
//			}
//
//			protected override Cairo.Color BackgroundColor {
//				get { return Editor.ColorStyle.BreakpointText.Background; }
//			}
//
//			protected override void SetForegroundColor (ChunkStyle style)
//			{
//				style.Foreground = Editor.ColorStyle.BreakpointText.Foreground;
//			}
//
//			protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
//			{
//				Cairo.Color color1 = Editor.ColorStyle.BreakpointMarker.Color;
//				Cairo.Color color2 = Editor.ColorStyle.BreakpointMarker.SecondColor;
//				if (IsTracepoint)
//					DrawDiamond (cr, x, y, size);
//				else
//					DrawCircle (cr, x, y, size);
//				FillGradient (cr, color1, color2, x, y, size);
//				DrawBorder (cr, color2, x, y, size);
//			}
//		}
//
//		public class DisabledBreakpointTextMarker : DebugTextMarker
//		{
//			public DisabledBreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
//			{
//				IsTracepoint = tracepoint;
//			}
//
//			public bool IsTracepoint {
//				get; private set;
//			}
//
//			protected override Cairo.Color BackgroundColor {
//				get { return Editor.ColorStyle.BreakpointMarkerDisabled.Color; }
//			}
//
//			protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
//			{
//				Cairo.Color border = Editor.ColorStyle.BreakpointText.Background;
//				if (IsTracepoint)
//					DrawDiamond (cr, x, y, size);
//				else
//					DrawCircle (cr, x, y, size);
//				//FillGradient (cr, new Cairo.Color (1,1,1), new Cairo.Color (1,0.8,0.8), x, y, size);
//				DrawBorder (cr, border, x, y, size);
//			}
//		}
//
//		public class InvalidBreakpointTextMarker : DebugTextMarker
//		{
//			public InvalidBreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
//			{
//				IsTracepoint = tracepoint;
//			}
//
//			public bool IsTracepoint {
//				get; private set;
//			}
//
//			protected override Cairo.Color BackgroundColor {
//				get { return Editor.ColorStyle.BreakpointTextInvalid.Background; }
//			}
//
//			protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
//			{
//				Cairo.Color color1 = Editor.ColorStyle.InvalidBreakpointMarker.Color;
//				Cairo.Color color2 = color1;
//				Cairo.Color border = Editor.ColorStyle.InvalidBreakpointMarker.SecondColor;
//
//				if (IsTracepoint)
//					DrawDiamond (cr, x, y, size);
//				else
//					DrawCircle (cr, x, y, size);
//
//				FillGradient (cr, color1, color2, x, y, size);
//				DrawBorder (cr, border, x, y, size);
//			}
//		}
//
//		public class CurrentDebugLineTextMarker : DebugTextMarker
//		{
//			public CurrentDebugLineTextMarker (TextEditor editor) : base (editor)
//			{
//			}
//
//			protected override Cairo.Color BackgroundColor {
//				get { return Editor.ColorStyle.DebuggerCurrentLine.Background; }
//			}
//
//			protected override void SetForegroundColor (ChunkStyle style)
//			{
//				style.Foreground = Editor.ColorStyle.DebuggerCurrentLine.Foreground;
//			}
//
//			protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
//			{
//				Cairo.Color color1 = Editor.ColorStyle.DebuggerCurrentLineMarker.Color;
//				Cairo.Color color2 = Editor.ColorStyle.DebuggerCurrentLineMarker.SecondColor;
//				Cairo.Color border = Editor.ColorStyle.DebuggerCurrentLineMarker.BorderColor;
//
//				DrawArrow (cr, x, y, size);
//				FillGradient (cr, color1, color2, x, y, size);
//				DrawBorder (cr, border, x, y, size);
//			}
//		}
//
//		public class DebugStackLineTextMarker : DebugTextMarker
//		{
//			public DebugStackLineTextMarker (TextEditor editor) : base (editor)
//			{
//			}
//
//			protected override Cairo.Color BackgroundColor {
//				get { return Editor.ColorStyle.DebuggerStackLine.Background; }
//			}
//
//			protected override void SetForegroundColor (ChunkStyle style)
//			{
//				style.Foreground = Editor.ColorStyle.DebuggerStackLine.Foreground;
//			}
//
//			protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
//			{
//				Cairo.Color color1 = Editor.ColorStyle.DebuggerStackLineMarker.Color;
//				Cairo.Color color2 = Editor.ColorStyle.DebuggerStackLineMarker.SecondColor;
//				Cairo.Color border = Editor.ColorStyle.DebuggerStackLineMarker.BorderColor;
//
//				DrawArrow (cr, x, y, size);
//				FillGradient (cr, color1, color2, x, y, size);
//				DrawBorder (cr, border, x, y, size);
//			}
//		}
	}

	public static class MarkerHostExtension
	{
		public static ITextSegmentMarker CreateLinkMarker (this IMarkerHost host, ISegment segment, Action<LinkRequest> activateLink)
		{
			if (host == null)
				throw new ArgumentNullException ("host");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return host.CreateLinkMarker (segment.Offset, segment.Length, activateLink);
		}

		public static IGenericTextSegmentMarker CreateGenericTextSegmentMarker (this IMarkerHost host, TextSegmentMarkerEffect effect, ISegment segment)
		{
			if (host == null)
				throw new ArgumentNullException ("host");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return host.CreateGenericTextSegmentMarker (effect, segment.Offset, segment.Length);
		}
	}
}

