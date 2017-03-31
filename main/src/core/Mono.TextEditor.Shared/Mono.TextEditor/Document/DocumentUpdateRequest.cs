// DocumentUpdateRequest.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

namespace Mono.TextEditor
{
	abstract class DocumentUpdateRequest
	{
		public abstract void Update (MonoTextEditor editor);
	}
	
	class SinglePositionUpdate : DocumentUpdateRequest
	{
		int line, column;
		
		public SinglePositionUpdate (int line, int column)
		{
			this.line = line;
			this.column = column;
		}
		
		public override void Update (MonoTextEditor editor)
		{
			editor.RedrawPosition (line, column);
		}
	}
	
	class UpdateAll : DocumentUpdateRequest
	{
		public override void Update (MonoTextEditor editor)
		{
			editor.QueueDraw ();
		}
	}
	
	class LineUpdate : DocumentUpdateRequest
	{
		int line;
		
		public LineUpdate (int line)
		{
			this.line = line;
		}
		
		public override void Update (MonoTextEditor editor)
		{
			editor.RedrawLine (line);
		}
	}
	
	class MultipleLineUpdate : DocumentUpdateRequest
	{
		int start, end;
		
		public MultipleLineUpdate (int start, int end)
		{
			this.start = start;
			this.end   = end;
		}
		
		public override void Update (MonoTextEditor editor)
		{
			if (start == end) {
				editor.TextViewMargin.RemoveCachedLine (start);
				editor.RedrawLine (start);
			} else {
				for (int i = start; i <= end; i++) {
					editor.TextViewMargin.RemoveCachedLine (i);
				}
				editor.RedrawLines (start, end);
			}
		}
	}
}
