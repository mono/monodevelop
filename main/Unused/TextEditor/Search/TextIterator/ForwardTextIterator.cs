//  ForwardTextIterator.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Diagnostics;
using System.Collections;

using MonoDevelop.EditorBindings.Search;

namespace MonoDevelop.TextEditor.Document
{
	public class ForwardTextIterator : ITextIterator
	{
		enum TextIteratorState {
			Resetted,
			Iterating,
			Done,
		}
		
		TextIteratorState state;
		
		ITextBufferStrategy textBuffer;
		int                 currentOffset;
		int                 endOffset;
		int                 oldOffset = -1;
		
		public ITextBufferStrategy TextBuffer {
			get {
				return textBuffer;
			}
		}
		
		public char Current {
			get {
				switch (state) {
					case TextIteratorState.Resetted:
						throw new System.InvalidOperationException("Call moveAhead first");
					case TextIteratorState.Iterating:
						return textBuffer.GetCharAt(currentOffset);
					case TextIteratorState.Done:
						throw new System.InvalidOperationException("TextIterator is at the end");
					default:
						throw new System.InvalidOperationException("unknown text iterator state");
				}
			}
		}
		
		public int Position {
			get {
				return currentOffset;
			}
			set {
				currentOffset = value;
			}
		}
		
		public int Length { get { return textBuffer.Length; } }
		public string FullDocumentText { get { return textBuffer.GetText (0, textBuffer.Length); } }
		
		// doesnt this feel like java ;-)
		public char GetCharAt (int p)
		{
			return textBuffer.GetCharAt (p);
		}
		
		public bool IsWholeWordAt (int offset, int length)
		{
			return SearchReplaceUtilities.IsWholeWordAt (textBuffer, offset, length);
		}
		
		public ForwardTextIterator(ITextBufferStrategy textBuffer, int endOffset)
		{
			Debug.Assert(textBuffer != null);
			Debug.Assert(endOffset >= 0 && endOffset < textBuffer.Length);
			
			this.textBuffer = textBuffer;
			this.endOffset  = endOffset;
			Reset();
		}
		
		public char GetCharRelative(int offset)
		{
			if (state != TextIteratorState.Iterating) {
				throw new System.InvalidOperationException();
			}
			
			int realOffset = (currentOffset + (1 + Math.Abs(offset) / textBuffer.Length) * textBuffer.Length + offset) % textBuffer.Length;
			return textBuffer.GetCharAt(realOffset);
		}
		
		public bool MoveAhead(int numChars)
		{
			Debug.Assert(numChars > 0);
			
			switch (state) {
				case TextIteratorState.Resetted:
					currentOffset = endOffset;
					state = TextIteratorState.Iterating;
					return true;
				case TextIteratorState.Done:
					return false;
				case TextIteratorState.Iterating:
					currentOffset = (currentOffset + numChars) % textBuffer.Length;
					bool finish = oldOffset != -1 && (oldOffset > currentOffset || oldOffset < endOffset) && currentOffset >= endOffset;
					
					oldOffset = currentOffset;
					if (finish) {
						state = TextIteratorState.Done;
						return false;
					}
					return true;
				default:
					throw new Exception("Unknown text iterator state");
			}
		}
		
		public void InformReplace(int offset, int length, int newLength)
		{
			if (offset <= endOffset) {
				endOffset = endOffset - length + newLength;
			}
			
			if (offset <= currentOffset) {
				currentOffset = currentOffset - length + newLength;
			}
			
			if (offset <= oldOffset) {
				oldOffset = oldOffset - length + newLength;
			}
		}

		public void Reset()
		{
			state         = TextIteratorState.Resetted;
			currentOffset = endOffset;
			oldOffset     = -1;
		}
		
		public override string ToString()
		{
			return String.Format("[ForwardTextIterator: currentOffset={0}, endOffset={1}, state={2}]", currentOffset, endOffset, state);
		}
	}
}
