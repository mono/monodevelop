// 
// ICompletionWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CodeCompletionContext
	{
		public int TriggerOffset { get; set; }
		public int TriggerLine { get; set; }
		public int TriggerLineOffset { get; set; }
		public int TriggerXCoord { get; set; }
		public int TriggerYCoord { get; set; }
		public int TriggerTextHeight { get; set; }
		public int TriggerWordLength { get; set; }

		public override string ToString ()
		{
			return string.Format ("[CodeCompletionContext: TriggerOffset={0}, TriggerLine={1}, TriggerLineOffset={2}, TriggerXCoord={3}, TriggerYCoord={4}, TriggerTextHeight={5}, TriggerWordLength={6}]", TriggerOffset, TriggerLine, TriggerLineOffset, TriggerXCoord, TriggerYCoord, TriggerTextHeight, TriggerWordLength);
		}
	}
}
