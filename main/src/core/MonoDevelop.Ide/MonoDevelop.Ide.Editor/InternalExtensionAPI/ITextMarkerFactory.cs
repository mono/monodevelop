//
// ITextMarkerFactory.cs
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
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Editor
{
	public enum LinkRequest
	{
		SameView,
		RequestNewView
	}

	interface ITextMarkerFactory
	{
		#region Line marker
		IUrlTextLineMarker CreateUrlTextMarker (TextEditor editor, string value, UrlType url, string syntax, int startCol, int endCol);
		ICurrentDebugLineTextMarker CreateCurrentDebugLineTextMarker (TextEditor editor, int offset, int length);
		ITextLineMarker CreateAsmLineMarker (TextEditor editor);
		IUnitTestMarker CreateUnitTestMarker (TextEditor editor, UnitTestMarkerHost host, UnitTestLocation unitTestLocation);
		IMessageBubbleLineMarker CreateMessageBubbleLineMarker (TextEditor editor);
		ITextLineMarker CreateLineSeparatorMarker (TextEditor editor);
		#endregion

		#region Segment marker
		ITextSegmentMarker CreateUsageMarker (TextEditor editor, Usage usage);
		ILinkTextMarker CreateLinkMarker (TextEditor editor, int offset, int length, Action<LinkRequest> activateLink);

		IGenericTextSegmentMarker CreateGenericTextSegmentMarker (TextEditor editor, TextSegmentMarkerEffect effect, int offset, int length);
		ISmartTagMarker CreateSmartTagMarker (TextEditor editor, int offset, DocumentLocation realLocation);
		IErrorMarker CreateErrorMarker (TextEditor editor, Error info, int offset, int length);
		#endregion
	}
}