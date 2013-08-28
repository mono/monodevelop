// 
// EditAction.cs
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
using Xwt;

namespace Mono.MHex.Data
{
	abstract class EditMode
	{
		protected HexEditor Editor {
			get;
			private set;
		}
		protected HexEditorData HexEditorData {
			get { return Editor.HexEditorData; } 
		}
		
		internal void InternalHandleKeypress (HexEditor editor, Key key, uint unicodeChar, ModifierKeys modifier)
		{
			this.Editor = editor; 
			
			HandleKeypress (key, unicodeChar, modifier);
			
			//make sure that nothing funny goes on when the mode should have finished
			this.Editor = null;
		}

		protected abstract void HandleKeypress (Key key, uint unicodeChar, ModifierKeys modifier);
	}
}
