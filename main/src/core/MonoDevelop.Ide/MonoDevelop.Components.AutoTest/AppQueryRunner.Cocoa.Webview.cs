//
// AppQueryRunner.Cocoa.Webview.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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

#if MAC
using MonoDevelop.Components.AutoTest.Results;
using MonoDevelop.Core;
using WebKit;
#endif

namespace MonoDevelop.Components.AutoTest
{
	partial class AppQueryRunner
	{
#if MAC
		void ProcessWebview (AppResult parent, ref AppResult lastChild, WebView webview)
		{
			if (webview == null)
				return;

			var frame = webview.MainFrame;
			var document = frame.DomDocument;

			ProcessDomElement (parent, ref lastChild, document.DocumentElement);
		}

		void ProcessDomElement (AppResult parent, ref AppResult lastChild, DomElement element)
		{
			AppResult node = AddDomElementResult (element);
			if (node == null)
				return;

			AddChild (parent, node, ref lastChild);

			AppResult nestedChild = null;
			DomElement currentChild = element.FirstElementChild;

			while (currentChild != null) {
				ProcessDomElement (node, ref nestedChild, currentChild);
				currentChild = currentChild.NextElementSibling;
			}
		}

		AppResult AddDomElementResult (DomElement element)
		{
			// If the view is hidden and we don't include hidden, don't add it.
			if (element == null || (!includeHidden && IsHidden (element))) {
				return null;
			}

			AppResult node = new WebViewResult (element) { SourceQuery = sourceQuery };
			return AddToResultSet (node);
		}

		static bool IsHidden (DomElement element)
		{
			return element.OffsetLeft < 0;
		}
#endif
	}
}
