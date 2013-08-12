// 
// CaretMoveActions.cs
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

namespace Mono.MHex
{
	static class CaretMoveActions
	{
		public static void SwitchSide (HexEditorData data)
		{
			data.Caret.InTextEditor = !data.Caret.InTextEditor;
			data.UpdateLine (data.Caret.Line);
		}
		
		public static void Up (HexEditorData data)
		{
			data.Caret.Offset = System.Math.Max (0, data.Caret.Offset - data.BytesInRow);
		}
		
		public static void Down (HexEditorData data)
		{
			data.Caret.Offset = System.Math.Min (data.Length, data.Caret.Offset + data.BytesInRow);
		}
		
		public static void Left (HexEditorData data)
		{
			if (!data.Caret.InTextEditor && data.Caret.SubPosition > 0) {
				data.Caret.SubPosition--;
				return;
			}
			long newOffset = System.Math.Max (0, data.Caret.Offset - 1);
			if (newOffset != data.Caret.Offset) {
				data.Caret.Offset = newOffset;
				data.Caret.SubPosition = data.Caret.MaxSubPosition;
			}
		}
		
		public static void Right (HexEditorData data)
		{
			if (!data.Caret.InTextEditor && data.Caret.SubPosition < data.Caret.MaxSubPosition) {
				data.Caret.SubPosition++;
				return;
			}
			long newOffset = System.Math.Min (data.Length, data.Caret.Offset + 1);
			if (newOffset != data.Caret.Offset) {
				data.Caret.Offset = newOffset;
			}
		}
		
		public static void LineEnd (HexEditorData data)
		{
			data.Caret.Offset = System.Math.Min (data.Length - 1, data.Caret.Offset + data.BytesInRow - 1 - data.Caret.Offset % data.BytesInRow);
			data.Caret.SubPosition = 0;
		}
		
		public static void LineHome (HexEditorData data)
		{
			data.Caret.Offset -= data.Caret.Offset % data.BytesInRow;
			data.Caret.SubPosition = 0;
		}
		
		public static void ToDocumentStart (HexEditorData data)
		{
			data.Caret.Offset = 0;
		}
		
		public static void ToDocumentEnd (HexEditorData data)
		{
			data.Caret.Offset = data.Length - 1;
		}
		
		public static void PageUp (HexEditorData data)
		{
			data.VAdjustment.Value = System.Math.Max (data.VAdjustment.LowerValue, data.VAdjustment.Value - data.VAdjustment.PageSize); 
			int pageLines = (int)(data.VAdjustment.PageSize + ((int)data.VAdjustment.Value % data.LineHeight) / data.LineHeight);
			data.Caret.Offset = (long)System.Math.Max (0, data.Caret.Offset - data.BytesInRow * pageLines);
		}
		
		public static void PageDown (HexEditorData data)
		{
			data.VAdjustment.Value = System.Math.Min (data.VAdjustment.UpperValue - data.VAdjustment.PageSize, data.VAdjustment.Value + data.VAdjustment.PageSize); 
			int pageLines = (int)(data.VAdjustment.PageSize + ((int)data.VAdjustment.Value % data.LineHeight) / data.LineHeight);
			data.Caret.Offset = (long)System.Math.Min (data.Length, data.Caret.Offset + data.BytesInRow * pageLines);
		}
		
	}
}
