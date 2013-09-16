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

namespace Mono.TextEditor
{
	public interface IIndentationTracker
	{
		string GetIndentationString (int offset);
		string GetIndentationString (int lineNumber, int column);
		
		int GetVirtualIndentationColumn (int offset);
		int GetVirtualIndentationColumn (int lineNumber, int column);
	}
	
	public static class IndentationTrackerExtensionMethods
	{
		public static string GetIndentationString (this IIndentationTracker thisObject, DocumentLocation loc)
		{
			return thisObject.GetIndentationString (loc.Line, loc.Column);
		}
		
		public static int GetVirtualIndentationColumn (this IIndentationTracker thisObject, DocumentLocation loc)
		{
			return thisObject.GetVirtualIndentationColumn (loc.Line, loc.Column);
		}
	}
	
	class DefaultIndentationTracker : IIndentationTracker
	{
		readonly TextDocument doc;
			
		public DefaultIndentationTracker (TextDocument doc)
		{
			this.doc = doc;
		}
		
		public string GetIndentationString (int offset)
		{
			var loc = doc.OffsetToLocation (offset);
			return GetIndentationString (loc.Line, loc.Column);
		}
		
		public string GetIndentationString (int lineNumber, int column)
		{
			DocumentLine line = doc.GetLine (lineNumber - 1);
			while (line != null) {
				var indent = line.GetIndentation (doc);
				if (indent.Length > 0)
					return indent;
				line = line.PreviousLine;
			}
			return "";
		}
		
		public int GetVirtualIndentationColumn (int offset)
		{
			return 1 + GetIndentationString (offset).Length;
		}
		
		public int GetVirtualIndentationColumn (int lineNumber, int column)
		{
			return 1 + GetIndentationString (lineNumber, column).Length;
		}
	}
}
