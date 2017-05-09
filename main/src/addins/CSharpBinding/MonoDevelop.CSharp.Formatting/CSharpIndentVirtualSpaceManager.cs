// 
// CSharpIndentVirtualSpaceManager.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp.Formatting
{
	class IndentVirtualSpaceManager : IndentationTracker
	{
		readonly TextEditor data;
		readonly CacheIndentEngine stateTracker;

        public override IndentationTrackerFeatures SupportedFeatures { 
            get {
                return IndentationTrackerFeatures.SmartBackspace | IndentationTrackerFeatures.CustomIndentationEngine;
            }
        }

        public IndentVirtualSpaceManager(TextEditor data, CacheIndentEngine stateTracker)
		{
			this.data = data;
			this.stateTracker = stateTracker;
		}

		#region IndentationTracker implementation
		public override string GetIndentationString (int lineNumber)
		{
			var line = data.GetLine (lineNumber);
			if (line == null)
				return "";
			// Get context to the end of the line w/o changing the main engine's state
			var offset = line.Offset;
			string curIndent = line.GetIndentation (data);
			try {
				stateTracker.Update (data, Math.Min (data.Length, offset + line.Length));
				int nlwsp = curIndent.Length;
				if (!stateTracker.LineBeganInsideMultiLineComment || (nlwsp < line.LengthIncludingDelimiter && data.GetCharAt (offset + nlwsp) == '*'))
					return stateTracker.ThisLineIndent;
			} catch (Exception e) {
				LoggingService.LogError ("Error while indenting at line " + lineNumber, e); 
			}
			return curIndent;
		}
		#endregion
	}
}
