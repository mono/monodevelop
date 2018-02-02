//
// TextMarkerFactory.cs
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
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// The text marker factory creates line and segment markers for the text editor.
	/// Note that this is the only valid way of creating markers for the editor.
	/// </summary>
	public static class TextMarkerFactory
	{
		#region Line marker
		public static IUrlTextLineMarker CreateUrlTextMarker (TextEditor editor, string value, UrlType url, string syntax, int startCol, int endCol)
		{
			return editor.TextMarkerFactory.CreateUrlTextMarker (editor, value, url, syntax, startCol, endCol);
		}

		public static ICurrentDebugLineTextMarker CreateCurrentDebugLineTextMarker (TextEditor editor, int offset, int length)
		{
			return editor.TextMarkerFactory.CreateCurrentDebugLineTextMarker (editor, offset, length);
		}

		public static ITextLineMarker CreateAsmLineMarker (TextEditor editor)
		{
			return editor.TextMarkerFactory.CreateAsmLineMarker (editor);
		}

		public static IUnitTestMarker CreateUnitTestMarker (TextEditor editor, UnitTestMarkerHost host, UnitTestLocation unitTestLocation)
		{
			return editor.TextMarkerFactory.CreateUnitTestMarker (editor, host, unitTestLocation);
		}

		public static IMessageBubbleLineMarker CreateMessageBubbleLineMarker (TextEditor editor)
		{
			return editor.TextMarkerFactory.CreateMessageBubbleLineMarker (editor);
		}


		#endregion

		#region Segment marker
		public static ITextSegmentMarker CreateUsageMarker (TextEditor editor, Usage usage)
		{
			return editor.TextMarkerFactory.CreateUsageMarker (editor, usage);
		}

		public static ILinkTextMarker CreateLinkMarker (TextEditor editor, int offset, int length, Action<LinkRequest> activateLink)
		{
			return editor.TextMarkerFactory.CreateLinkMarker (editor, offset, length, activateLink);
		}

		public static ILinkTextMarker CreateLinkMarker (TextEditor editor, ISegment segment, Action<LinkRequest> activateLink)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return editor.TextMarkerFactory.CreateLinkMarker (editor, segment.Offset, segment.Length, activateLink);
		}

		public static IGenericTextSegmentMarker CreateGenericTextSegmentMarker (TextEditor editor, TextSegmentMarkerEffect effect, int offset, int length)
		{
			return editor.TextMarkerFactory.CreateGenericTextSegmentMarker (editor, effect, null, offset, length);
		}

		public static IGenericTextSegmentMarker CreateGenericTextSegmentMarker (TextEditor editor, TextSegmentMarkerEffect effect, ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return editor.TextMarkerFactory.CreateGenericTextSegmentMarker (editor, effect, null, segment.Offset, segment.Length);
		}

		public static IGenericTextSegmentMarker CreateGenericTextSegmentMarker (TextEditor editor, TextSegmentMarkerEffect effect, HslColor color, int offset, int length)
		{
			return editor.TextMarkerFactory.CreateGenericTextSegmentMarker (editor, effect, color, offset, length);
		}

		public static IGenericTextSegmentMarker CreateGenericTextSegmentMarker (TextEditor editor, TextSegmentMarkerEffect effect, HslColor color, ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return editor.TextMarkerFactory.CreateGenericTextSegmentMarker (editor, effect, color, segment.Offset, segment.Length);
		}

		public static ISmartTagMarker CreateSmartTagMarker (TextEditor editor, int offset, DocumentLocation realLocation)
		{
			return editor.TextMarkerFactory.CreateSmartTagMarker (editor, offset, realLocation);
		}

		static bool IsIdentifierPart (char ch)
		{
			return char.IsLetterOrDigit (ch) || ch == '_';
		}

		public static IErrorMarker CreateErrorMarker (TextEditor editor, Error info)
		{
			int offset    = editor.LocationToOffset (info.Region.BeginLine, info.Region.BeginColumn);
			int endOffset = editor.LocationToOffset (info.Region.EndLine, info.Region.EndColumn);
			if (endOffset <= offset) {
				endOffset = offset + 1;
				while (endOffset < editor.Length && IsIdentifierPart (editor.GetCharAt (endOffset))) {
					endOffset++;
				}
				if (endOffset == offset + 1) {
					if (endOffset > 0 && endOffset - 1 < editor.Length) {
						var c = editor.GetCharAt (endOffset - 1);
						while ((c == '\n' || c == '\r') && endOffset < editor.Length) {
							c = editor.GetCharAt (endOffset);
							endOffset++;
						}
					} else {
						endOffset = editor.Length;
					}
				}
			}
			return editor.TextMarkerFactory.CreateErrorMarker (editor, info, offset, endOffset - offset);
		}
		#endregion
	}
}