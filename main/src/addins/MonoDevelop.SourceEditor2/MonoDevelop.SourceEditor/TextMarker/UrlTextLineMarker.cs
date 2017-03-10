//
// UrlTextLineMarker.cs
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
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor
{
	class UrlTextLineMarker : UrlMarker, IUrlTextLineMarker
    {
		public UrlTextLineMarker (TextDocument doc,string url, Mono.TextEditor.UrlType urlType, string style, int startColumn, int endColumn) : base (doc, url, urlType, style, startColumn, endColumn)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
		}
		
		MonoDevelop.Ide.Editor.UrlType IUrlTextLineMarker.UrlType {
			get {
				return (MonoDevelop.Ide.Editor.UrlType)UrlType;
			}
		}

		IDocumentLine ITextLineMarker.Line {
			get {
				return LineSegment;
			}
		}
    }
}

