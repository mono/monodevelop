//
// DocumentFactory.cs
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
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor
{
	public static class DocumentFactory
	{
		public static ITextDocument CreateNewDocument ()
		{
			throw new NotImplementedException ();
		}

		public static IReadonlyTextDocument CreateNewReadonlyDocument (string fileName, string text)
		{
			/*
			data.MimeType = DesktopService.GetMimeTypeForUri (filePath);
			data.FileName = filePath;
			 * */
			throw new NotImplementedException ();
		}

		public static TextEditor CreateNewEditor ()
		{
			throw new NotImplementedException ();
		}

		public static TextEditor CreateNewEditor (IReadonlyTextDocument document)
		{
			throw new NotImplementedException ();
		}

		public static IUrlTextLineMarker CreateUrlTextMarker (TextEditor doc, IDocumentLine line, string value, UrlType url, string syntax, int startCol, int endCol)
		{
			throw new NotImplementedException ();
		}

		public static ICurrentDebugLineTextMarker CreateCurrentDebugLineTextMarker (TextEditor iTextEditor)
		{
			throw new NotImplementedException ();
		}

		public static ITextLineMarker CreateAsmLineMarker ()
		{
			throw new NotImplementedException ();
		}

		public static IGenericTextSegmentMarker CreateGenericTextSegmentMarker (TextSegmentMarkerEffect effect, int offset, int length)
		{
			throw new NotImplementedException ();
		}

		public static IGenericTextSegmentMarker CreateGenericTextSegmentMarker (TextSegmentMarkerEffect effect, ISegment segment)
		{
			throw new NotImplementedException ();
		}
	}
}

