// 
// UpdateRequest.cs
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
using System.Linq;
using Mono.MHex;
using Mono.MHex.Rendering;

namespace Mono.MHex.Data
{
	abstract class UpdateRequest
	{
		public abstract void AddRedraw (HexEditor editor);
	}
	
	class LineUpdateRequest : UpdateRequest
	{
		public long Line {
			get;
			set;
		}
		
		public LineUpdateRequest (long line)
		{
			this.Line = line;
		}
		
		public override void AddRedraw (HexEditor editor)
		{
			editor.RepaintArea (0, (int)(Line * editor.LineHeight - editor.HexEditorData.VAdjustment.Value), editor.Bounds.Width, editor.LineHeight);
		}
	}
	
	class MarginLineUpdateRequest : UpdateRequest
	{
		public Type MarginType {
			get;
			set;
		}
		public long Line {
			get;
			set;
		}
		
		public MarginLineUpdateRequest (Type marginType, long line)
		{
			this.MarginType = marginType;
			this.Line = line;
		}
		
		public override void AddRedraw (HexEditor editor)
		{
			Margin margin = editor.Margins.FirstOrDefault (m => m.GetType () == MarginType);
			if (margin != null)
				editor.RepaintMarginArea (margin, 
				                          margin.XOffset, 
				                          (int)(Line * editor.LineHeight - editor.HexEditorData.VAdjustment.Value), 
				                          margin.Width, 
				                          editor.LineHeight);
		}
	}
}
