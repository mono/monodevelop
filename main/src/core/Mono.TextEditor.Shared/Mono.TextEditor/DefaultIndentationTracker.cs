// 
// IndentationTracker.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

namespace Mono.TextEditor
{
	class DefaultIndentationTracker : IndentationTracker
	{
		readonly TextDocument doc;

		public override IndentationTrackerFeatures SupportedFeatures {
			get {
				return IndentationTrackerFeatures.SkipFixVirtualIndentation;
			}
		}

		public DefaultIndentationTracker (TextDocument doc)
		{
			this.doc = doc;
		}

		public override string GetIndentationString (int lineNumber)
		{
			return GetIndentationString (lineNumber, 1);
		}
		
		string GetIndentationString (int lineNumber, int column)
		{
			var line = doc.GetLine (lineNumber);
			while (line != null) {
				if (line.Length == 0) {
					line = line.PreviousLine;
					continue;
				}
				return line.GetIndentation (doc);
			}
			return "";
		}

	}
}
