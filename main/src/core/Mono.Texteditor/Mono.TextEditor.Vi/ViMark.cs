// 
// ViMark.cs
//  
// Author:
//       Sanjoy Das <sanjoy@playingwithpointers.com>
// 
// Copyright (c) 2010 Sanjoy Das
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
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor.Vi
{
	public class ViMark : Mono.TextEditor.TextLineMarker
	{
	
		public char MarkCharacter {get; set;}
		
		/// <summary>
		/// Only way to construct a ViMark.
		/// </summary>
		/// <param name="markCharacter">
		/// The <see cref="System.Char"/> with which the ViMark object needs to be
		/// associated.
		/// </param>
		public ViMark (char markCharacter) {
			MarkCharacter = MarkCharacter;
		}
		
		public int ColumnNumber {get; protected set;}
		
		public void SaveMark (TextEditorData data) {
		
			if (base.LineSegment != null) {
				// Remove the marker first
				data.Document.RemoveMarker (this);
			}
		
			// Is there a better way of doing this?
			int lineNumber = data.IsSomethingSelected ? data.MainSelection.MinLine : data.Caret.Line;
			base.LineSegment = data.Document.GetLine (lineNumber);
			ColumnNumber = data.Caret.Column;
			data.Document.AddMarker(lineNumber, this);
			
			data.Document.RequestUpdate (new UpdateAll ());
			data.Document.CommitDocumentUpdate ();
			
		}
		
		public void LoadMark (TextEditorData data) {
			int x = base.LineSegment.LineNumber;
			data.Caret.Line = x;
			int len = base.LineSegment.LengthIncludingDelimiter;
			if (ColumnNumber >= len) {
				// Check if the line has been truncated after the setting the mark
				data.Caret.Column = len - 1;
			} else {
				data.Caret.Column = ColumnNumber;
			}
		}
		
		public override ChunkStyle GetStyle (ChunkStyle baseStyle) {
			return baseStyle;
		}
		
	}
	
}
