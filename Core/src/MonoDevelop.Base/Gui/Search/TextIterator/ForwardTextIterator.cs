// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.Collections;

using MonoDevelop.Gui;

namespace MonoDevelop.Gui.Search
{
	public class ForwardTextIterator : ITextIterator
	{
		enum TextIteratorState {
			Resetted,
			Iterating,
			Done,
		}
		
		TextIteratorState state;
		
		Gtk.TextBuffer textBuffer;
		int currentOffset;
		int endOffset;
		IDocumentInformation docInfo;
		
		public ForwardTextIterator (IDocumentInformation docInfo, Gtk.TextView document, int endOffset)
		{
			Debug.Assert(endOffset >= 0 && endOffset < BufferLength);
			
			this.docInfo = docInfo;
			this.textBuffer = document.Buffer;
			this.endOffset = endOffset;
			Reset();
		}
		
		public IDocumentInformation DocumentInformation {
			get { return docInfo; }
		}
		
		public virtual char Current {
			get {
				switch (state) {
					case TextIteratorState.Resetted:
						throw new System.InvalidOperationException("Call moveAhead first");
					case TextIteratorState.Iterating:
						return GetCharAt(currentOffset);
					case TextIteratorState.Done:
						throw new System.InvalidOperationException("TextIterator is at the end");
					default:
						throw new System.InvalidOperationException("unknown text iterator state");
				}
			}
		}
		
		public virtual int Position {
			get { return BufferToIterPos (DocumentOffset); }
			set { DocumentOffset = IterToBufferPos (value); }
		}
		
		public virtual int DocumentOffset {
			get {
				if (state == TextIteratorState.Done) return -1;
				else return currentOffset;
			}
			set {
				if (value == -1) {
					state = TextIteratorState.Done;
					currentOffset = endOffset;
					return;
				}
				if (state == TextIteratorState.Done)
					state = TextIteratorState.Iterating;
				currentOffset = value;
			}
		}
		
		public virtual int Line {
			get {
				int pos = DocumentOffset;
				if (pos == -1) return -1;
				return textBuffer.GetIterAtOffset (pos).Line;
			}
		}
		public virtual int Column {
			get {
				int pos = DocumentOffset;
				if (pos == -1) return -1;
				return textBuffer.GetIterAtOffset (pos).LineOffset;
			}
		}
		
		public virtual char GetCharRelative (int offset)
		{
			if (state != TextIteratorState.Iterating) {
				throw new System.InvalidOperationException();
			}
			
			if (currentOffset + offset < 0 || currentOffset + offset >= BufferLength) 
				return char.MinValue;
				
			int realOffset = (currentOffset + (1 + Math.Abs(offset) / BufferLength) * BufferLength + offset) % BufferLength;
			
			return GetCharAt(realOffset);
		}
		
		public virtual bool MoveAhead(int numChars)
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
					int oldOffset = currentOffset;
					currentOffset = (currentOffset + numChars) % BufferLength;
					bool finish = (oldOffset > currentOffset || oldOffset < endOffset) && currentOffset >= endOffset;
					if (finish) {
						state = TextIteratorState.Done;
						currentOffset = endOffset;
						return false;
					}
					return true;
				default:
					throw new Exception("Unknown text iterator state");
			}
		}
		
		public virtual void MoveToEnd ()
		{
			if (endOffset > 0)
				currentOffset = endOffset - 1;
			else
				currentOffset = BufferLength - 1;

			state = TextIteratorState.Iterating;
		}
		
		public virtual string ReadToEnd ()
		{
			if (state == TextIteratorState.Done) return "";
			
			string doc = "";
			if (currentOffset >= endOffset) {
				doc = GetText (currentOffset, BufferLength - currentOffset);
				currentOffset = 0;
			}
				
			doc += GetText (currentOffset, endOffset - currentOffset);
			currentOffset = endOffset;
			return doc;
		}

		public virtual void Replace (int length, string pattern)
		{
			Gtk.TextIter start = textBuffer.GetIterAtOffset (currentOffset);
			Gtk.TextIter end = textBuffer.GetIterAtOffset (currentOffset + length);
			textBuffer.Delete (ref start, ref end);
			
			Gtk.TextIter put = textBuffer.GetIterAtOffset (currentOffset);
			textBuffer.Insert (ref put, pattern);
			
			if (currentOffset <= endOffset) {
				endOffset = endOffset - length + pattern.Length;
			}
			
			currentOffset = currentOffset - length + pattern.Length;
		}
		
		public virtual bool SupportsSearch (SearchOptions options, bool reverse)
		{
			return false;
		}
		
		public virtual bool SearchNext (string text, SearchOptions options, bool reverse)
		{
			throw new NotSupportedException ();
		}
		
		char GetCharAt (int offset)
		{
			if (offset < 0)
				offset = 0;
			Gtk.TextIter iter = textBuffer.GetIterAtOffset (offset);
			if (iter.Equals (Gtk.TextIter.Zero))
				return ' ';
			if (iter.Char == null || iter.Char.Length == 0)
				return ' ';
			return iter.Char[0];
		}
		
		string GetText (int start, int length)
		{
			Gtk.TextIter begin_iter = textBuffer.GetIterAtOffset (start);
			Gtk.TextIter end_iter = textBuffer.GetIterAtOffset (start + length);
			return textBuffer.GetText (begin_iter, end_iter, true);
		}
		
		int IterToBufferPos (int pos)
		{
			if (pos == -1)
				return -1;
			else if (pos >= (BufferLength - endOffset))
				return pos - (BufferLength - endOffset);
			else
				return endOffset + pos;
		}
		
		int BufferToIterPos (int pos)
		{
			if (pos == -1)
				return pos;
			else if (pos >= endOffset) 
				return pos - endOffset;
			else
				return (BufferLength - endOffset) + pos;
		}
		
		protected int BufferLength
		{
			get { return textBuffer.EndIter.Offset + 1; }
		}

		public virtual void Reset()
		{
			state = TextIteratorState.Resetted;
			currentOffset = endOffset;
		}
		
		public virtual void Close ()
		{
		}
		
		public override string ToString()
		{
			return String.Format("[ForwardTextIterator: currentOffset={0}, endOffset={1}, state={2}]", currentOffset, endOffset, state);
		}
		
		protected Gtk.TextBuffer Buffer {
			get { return textBuffer; }
		}
		
		protected int EndOffset {
			get { return endOffset; }
		}
	}
}
