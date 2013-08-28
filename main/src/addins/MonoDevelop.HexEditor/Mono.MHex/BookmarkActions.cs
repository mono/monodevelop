// 
// BookmarkActions.cs
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
using Mono.MHex.Data;
using Mono.MHex.Rendering;

namespace Mono.MHex
{
	static class BookmarkActions
	{
		public static void Toggle (HexEditorData data)
		{
			long line = data.Caret.Offset / data.BytesInRow;
			foreach (long bookmark in data.Bookmarks) {
				if (line * data.BytesInRow <= bookmark && bookmark < line * data.BytesInRow + data.BytesInRow) {
					data.Bookmarks.Remove (bookmark);
					return;
				}
			}
			data.Bookmarks.Add (data.Caret.Offset);
			data.UpdateMargin (typeof (IconMargin), data.Caret.Line);
		}
		
		public static void GotoNext (HexEditorData data)
		{
			data.Bookmarks.Sort ();
			long cur = long.MaxValue;
			for (int i = 0; i < data.Bookmarks.Count; i++) {
				if (data.Bookmarks[i] > data.Caret.Offset && cur > data.Bookmarks[i])
					cur = data.Bookmarks[i];
			}
			if (cur == long.MaxValue && data.Bookmarks.Count > 0)
				cur = data.Bookmarks[0];
			
			if (cur != long.MaxValue) {
				data.Caret.Offset = cur;
				data.UpdateLine (data.Caret.Offset / data.BytesInRow);
			}
		}
		
		public static void GotoPrevious (HexEditorData data)
		{
			data.Bookmarks.Sort ();
			long cur = -1;
			for (int i = 0; i < data.Bookmarks.Count; i++) {
				if (data.Bookmarks[i] < data.Caret.Offset && cur < data.Bookmarks[i])
					cur = data.Bookmarks[i];
			}
			if (cur == -1 && data.Bookmarks.Count > 0)
				cur = data.Bookmarks[data.Bookmarks.Count - 1];
			
			if (cur != -1) {
				data.Caret.Offset = cur;
				data.UpdateLine (data.Caret.Offset / data.BytesInRow);
			}
		}
	}
}
