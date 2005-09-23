// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	internal class DefaultLineManager : ILineManager
	{
		ArrayList lineCollection = new ArrayList();
		
		IDocument document;
		IHighlightingStrategy highlightingStrategy;
		
		// keep track of the textlength ourselves
		int textLength;
		
		public ArrayList LineSegmentCollection {
			get {
				return lineCollection;
			}
		}
			
		public int TotalNumberOfLines {
			get {
				if (lineCollection.Count == 0) {
					return 1;
				}
			
				return ((LineSegment)lineCollection[lineCollection.Count - 1]).DelimiterLength > 0 ? lineCollection.Count + 1 : lineCollection.Count;
			}
		}
		
		public IHighlightingStrategy HighlightingStrategy {
			get {
				return highlightingStrategy;
			}
			set {
				if (highlightingStrategy != value) {
					highlightingStrategy = value;
					if (highlightingStrategy != null) {							
						highlightingStrategy.MarkTokens(document);		
					}
				}
			}
		}
		
		public DefaultLineManager(IDocument document, IHighlightingStrategy highlightingStrategy) 
		{
			this.document = document;
			this.highlightingStrategy  = highlightingStrategy;
		}
		
		public int GetLineNumberForOffset(int offset) 
		{
			if (offset < 0 || offset > textLength) {
				throw new ArgumentOutOfRangeException("offset", offset, "should be between 0 and " + textLength);
			}
			
			if (offset == textLength) {
				if (lineCollection.Count == 0) {
					return 0;
				}
				
				LineSegment lastLine = (LineSegment)lineCollection[lineCollection.Count - 1];
				return lastLine.DelimiterLength > 0 ? lineCollection.Count : lineCollection.Count - 1;
			}
			
			return FindLineNumber(offset);
		}
		
		public LineSegment GetLineSegmentForOffset(int offset) 
		{
			if (offset < 0 || offset > textLength) {
				throw new ArgumentOutOfRangeException("offset", offset, "should be between 0 and " + textLength);
			}
			
			if (offset == textLength) {
				if (lineCollection.Count == 0) {
					return new LineSegment(0, 0);
				}
				LineSegment lastLine = (LineSegment)lineCollection[lineCollection.Count - 1];
				return lastLine.DelimiterLength > 0 ? new LineSegment(textLength, 0) : lastLine;
			}
			
			return GetLineSegment(FindLineNumber(offset));
		}
		
		public LineSegment GetLineSegment(int lineNr) 
		{
			if (lineNr < 0 || lineNr > lineCollection.Count) {
				throw new ArgumentOutOfRangeException("lineNr", lineNr, "should be between 0 and " + lineCollection.Count);
			}
			
			if (lineNr == lineCollection.Count) {
				if (lineCollection.Count == 0) {
					return new LineSegment(0, 0);
				}
				LineSegment lastLine = (LineSegment)lineCollection[lineCollection.Count - 1];
				return lastLine.DelimiterLength > 0 ? new LineSegment(lastLine.Offset + lastLine.TotalLength, 0) : lastLine;
			}
			
			return (LineSegment)lineCollection[lineNr];
		}
		
		int Insert(int lineNumber, int offset, string text) 
		{
			if (text == null || text.Length == 0) {
				return 0;
			}
			
			textLength += text.Length;
			
			if (lineCollection.Count == 0 || lineNumber >= lineCollection.Count) {
				return CreateLines(text, lineCollection.Count, offset);
			}
			
			LineSegment line = (LineSegment)lineCollection[lineNumber];
			
			ISegment nextDelimiter = NextDelimiter(text, 0);
			if (nextDelimiter == null || nextDelimiter.Offset < 0) {
				line.TotalLength += text.Length;
				markLines.Add(line);
				return 0;
			}
			
			int restLength = line.Offset + line.TotalLength - offset;
			
			if (restLength > 0) {
				LineSegment lineRest = new LineSegment(offset, restLength);
				lineRest.DelimiterLength = line.DelimiterLength;
				
				lineRest.Offset += text.Length;
				markLines.Add(lineRest);
				
				if (restLength - line.DelimiterLength < 0) {
					throw new ApplicationException("tried to insert inside delimiter string " + lineRest.ToString() + "!!!");
				}
				
				lineCollection.Insert(lineNumber + 1, lineRest);
				OnLineCountChanged(new LineManagerEventArgs(document, lineNumber - 1, 1));
			}
			
			line.DelimiterLength = nextDelimiter.Length;
			int nextStart = offset + nextDelimiter.Offset + nextDelimiter.Length;
			line.TotalLength = nextStart - line.Offset;
			
			markLines.Add(line);
			text = text.Substring(nextDelimiter.Offset + nextDelimiter.Length);
			
			return CreateLines(text, lineNumber + 1, nextStart) + 1;
		}
		
// OLD SAFE & TESTED REmove
//		bool Remove(int lineNumber, int offset, int length)
//		{
//			if (length == 0) {
//				return false;
//			}
//			
//			int removedLineEnds = GetNumberOfLines(lineNumber, offset, length) - 1;
//			
//			LineSegment line = (LineSegment)lineCollection[lineNumber];
//			if ((lineNumber == lineCollection.Count - 1) && removedLineEnds > 0) {
//				line.TotalLength -= length;
//				line.DelimiterLength = 0;
//			} else {
//				++lineNumber;
//				for (int i = 1; i <= removedLineEnds; ++i) {
//					
//					if (lineNumber == lineCollection.Count) {
//						line.DelimiterLength = 0;
//						break;
//					}
//					
//					LineSegment line2 = (LineSegment)lineCollection[lineNumber];
//					
//					line.TotalLength += line2.TotalLength;
//					line.DelimiterLength = line2.DelimiterLength;
//					lineCollection.RemoveAt(lineNumber);
//				}
//				line.TotalLength -= length;
//				
//				if (lineNumber < lineCollection.Count && removedLineEnds > 0) {
//					markLines.Add(lineCollection[lineNumber]);
//				}
//			}
//			
//			textLength -= length;
//			
//			if (line.TotalLength == 0) {
//				lineCollection.Remove(line);
//				OnLineCountChanged(new LineManagerEventArgs(document, lineNumber, -removedLineEnds));
//				return true;
//			}
//			
//			markLines.Add(line);
//			OnLineCountChanged(new LineManagerEventArgs(document, lineNumber, -removedLineEnds));
//			return false;
//		}
	
		bool Remove(int lineNumber, int offset, int length) 
		{
			if (length == 0) {
				return false;
			}
			
			int removedLineEnds = GetNumberOfLines(lineNumber, offset, length) - 1;
			
			LineSegment line = (LineSegment)lineCollection[lineNumber];
			if ((lineNumber == lineCollection.Count - 1) && removedLineEnds > 0) {
				line.TotalLength -= length;
				line.DelimiterLength = 0;
			} else {
				++lineNumber;
				LineSegment line2 = null;
				
				// todo: take out this unneccessary loop...
				for (int i = 0; i < removedLineEnds && lineNumber + i < lineCollection.Count; ++i) {
					line2 = (LineSegment)lineCollection[lineNumber + i];
					
					line.TotalLength += line2.TotalLength;
					line.DelimiterLength = line2.DelimiterLength;
				}

				if (lineNumber + removedLineEnds >= lineCollection.Count) {
					line.DelimiterLength = 0;
				} else {
					if (line2 != null) {
						line.DelimiterLength = line2.DelimiterLength;
					}
				}
					
				lineCollection.RemoveRange(lineNumber, Math.Max(0, Math.Min(lineCollection.Count - lineNumber, removedLineEnds)));
				
				line.TotalLength -= length;
				if (lineNumber < lineCollection.Count && removedLineEnds > 0) {
					markLines.Add(lineCollection[lineNumber]);
				}
			}
			
			textLength -= length;
			
			if (line.TotalLength == 0) {
				lineCollection.Remove(line);
				OnLineCountChanged(new LineManagerEventArgs(document, lineNumber, -removedLineEnds));
				return true;
			}
			
			markLines.Add(line);
			OnLineCountChanged(new LineManagerEventArgs(document, lineNumber, -removedLineEnds));
			return false;
		}
		
		ArrayList markLines = new ArrayList();
		
		public void Insert(int offset, string text)
		{
			Replace(offset, 0, text);
		}
		
		public void Remove(int offset, int length)
		{
			Replace(offset, length, String.Empty);
		}
		
		
		public void Replace(int offset, int length, string text) 
		{
			int lineNumber = GetLineNumberForOffset(offset);			
			int insertLineNumber = lineNumber;
			if (Remove(lineNumber, offset, length)) {
				--lineNumber;
			}
			
			lineNumber += Insert(insertLineNumber, offset, text);
			
			int delta = -length;
			if (text != null) {
				delta = text.Length + delta;
			}
			
			if (delta != 0) {
				AdaptLineOffsets(lineNumber, delta);		
			}
			
			RunHighlighter();
		}
		
		void RunHighlighter()
		{
			DateTime time = DateTime.Now;
			if (highlightingStrategy != null) {
				highlightingStrategy.MarkTokens(document, markLines);
			}
			markLines.Clear();
		}
		
		public void SetContent(string text)
		{
			lineCollection.Clear();
			if (text != null) {
				textLength = text.Length;
				CreateLines(text, 0, 0);
				RunHighlighter();
			}
		}
		
		void AdaptLineOffsets(int lineNumber, int delta) 
		{
			for (int i = lineNumber + 1; i < lineCollection.Count; ++i) {
				((LineSegment)lineCollection[i]).Offset += delta;
			}
		}
		
		int GetNumberOfLines(int startLine, int offset, int length) 
		{
			if (length == 0) {
				return 1;
			}
			
			int target = offset + length;
			
			LineSegment l = (LineSegment)lineCollection[startLine];
			
			if (l.DelimiterLength == 0) {
				return 1;
			}
			
			if (l.Offset + l.TotalLength > target) {
				return 1;
			}
			
			if (l.Offset + l.TotalLength == target) {
				return 2;
			}
			
			return GetLineNumberForOffset(target) - startLine + 1;
		}
		
		int FindLineNumber(int offset) 
		{
			if (lineCollection.Count == 0) {
				return - 1;
			}
			
			int leftIndex  = 0;
			int rightIndex = lineCollection.Count - 1;
			
			LineSegment curLine = null;
			
			while (leftIndex < rightIndex) {
				int pivotIndex = (leftIndex + rightIndex) / 2;
				
				curLine = (LineSegment)lineCollection[pivotIndex];
				
				if (offset < curLine.Offset) {
					rightIndex = pivotIndex - 1;
				} else if (offset > curLine.Offset) {
					leftIndex = pivotIndex + 1;
				} else {
					leftIndex = pivotIndex;
					break;
				}
			}
			
			return ((LineSegment)lineCollection[leftIndex]).Offset > offset ? leftIndex - 1 : leftIndex;
		}
		
		
// OLD 'SAFE & TESTED' CreateLines
//		int CreateLines(string text, int insertPosition, int offset) 
//		{
//			int count = 0;
//			int start = 0;
//			ISegment nextDelimiter = NextDelimiter(text, 0);
//			while (nextDelimiter != null && nextDelimiter.Offset >= 0) {
//				int index = nextDelimiter.Offset + (nextDelimiter.Length - 1);
//				
//				LineSegment newLine = new LineSegment(offset + start, offset + index, nextDelimiter.Length);
//				
//				markLines.Add(newLine);
//				
//				if (insertPosition + count >= lineCollection.Count) {
//					lineCollection.Add(newLine);
//				} else {
//					lineCollection.Insert(insertPosition + count, newLine);
//				}
//				
//				++count;
//				start = index + 1;
//				nextDelimiter = NextDelimiter(text, start);
//			}
//			
//			if (start < text.Length) {
//				if (insertPosition + count < lineCollection.Count) {
//					LineSegment l = (LineSegment)lineCollection[insertPosition + count];
//					
//					int delta = text.Length - start;
//					
//					l.Offset -= delta;
//					l.TotalLength += delta;
//				} else {
//					LineSegment newLine = new LineSegment(offset + start, text.Length - start);
//					
//					markLines.Add(newLine);
//					lineCollection.Add(newLine);
//					++count;
//				}
//			}
//			OnLineCountChanged(new LineManagerEventArgs(document, insertPosition, count));
//			return count;
//		}
		
		int CreateLines(string text, int insertPosition, int offset)
		{
			int start = 0;
			ISegment nextDelimiter = NextDelimiter(text, 0);
			
			ArrayList newLines = new ArrayList();
			while (nextDelimiter != null && nextDelimiter.Offset >= 0) {
				int index = nextDelimiter.Offset + (nextDelimiter.Length - 1);
				
				LineSegment newLine = new LineSegment(offset + start, offset + index, nextDelimiter.Length);
				newLines.Add(newLine);
				
				start = index + 1;
				nextDelimiter = NextDelimiter(text, start);
			}
			
			if (start < text.Length) {
				if (insertPosition + newLines.Count < lineCollection.Count) {
					LineSegment l = (LineSegment)lineCollection[insertPosition];
					
					int delta = text.Length - start;
					l.Offset      -= delta;
					l.TotalLength += delta;
				} else {
					LineSegment newLine = new LineSegment(offset + start, text.Length - start);
					newLines.Add(newLine);
				}
			}
			
			if (insertPosition >= lineCollection.Count) {
				lineCollection.AddRange(newLines);
			} else {
				lineCollection.InsertRange(insertPosition, newLines);
			}
			markLines.AddRange(newLines);
			
			OnLineCountChanged(new LineManagerEventArgs(document, insertPosition, newLines.Count));
			return newLines.Count;
		}
		
		public int GetLogicalLine(int lineNumber)
		{
			int invisibleLines = 0;
			for (int i = 0; i < lineCollection.Count; ++i) {
				if (!document.FoldingManager.IsLineVisible(i)) {
					++invisibleLines;
				}
				if (i >= lineNumber) {
					return i - invisibleLines;
				}
			}
			return lineCollection.Count - invisibleLines;
		}

		public int GetVisibleLine(int lineNumber)
		{
			int visibleLines = 0;
			for (int i = 0; i < lineCollection.Count; ++i) {
				if (document.FoldingManager.IsLineVisible(i)) {
					++visibleLines;
				}
				if (visibleLines > lineNumber) {
					return i;
				}
			}
			return lineCollection.Count;
		}
		
		// TODO : speedup the next/prev visible line search
		// HOW? : save the foldings in a sorted list and lookup the 
		//        line numbers in this list
		public int GetNextVisibleLineAbove(int lineNumber, int lineCount)
		{
			int curLineNumber = lineNumber;
			for (int i = 0; i < lineCount; ++i) {
				++curLineNumber;
				while (curLineNumber < TotalNumberOfLines && (curLineNumber >= lineCollection.Count || !document.FoldingManager.IsLineVisible(curLineNumber))) {
					++curLineNumber;
				}
			}
			return Math.Min(TotalNumberOfLines - 1, curLineNumber);
		}
		
		public int GetNextVisibleLineBelow(int lineNumber, int lineCount)
		{
			int curLineNumber = lineNumber;
			for (int i = 0; i < lineCount; ++i) {
				--curLineNumber;
				while (curLineNumber >= 0 && !document.FoldingManager.IsLineVisible(curLineNumber)) {
					--curLineNumber;
				}
			}
			return Math.Max(0, curLineNumber);
		}
		
		protected virtual void OnLineCountChanged(LineManagerEventArgs e)
		{
			if (LineCountChanged != null) {
				LineCountChanged(this, e);
			}
		}
				
		// use always the same ISegment object for the DelimiterInfo
		DelimiterSegment delimiterSegment = new DelimiterSegment(); 
		ISegment NextDelimiter(string text, int offset) 
		{
			for (int i = offset; i < text.Length; i++) {
				switch (text[i]) {
					case '\r':
						if (i + 1 < text.Length) {
							if (text[i + 1] == '\n') {
								delimiterSegment.Offset = i;
								delimiterSegment.Length = 2;
								return delimiterSegment;
							}
						}
						goto case '\n';
					case '\n':
						delimiterSegment.Offset = i;
						delimiterSegment.Length = 1;
						return delimiterSegment;
				}
			}
			return null;
		}
		
		public event LineManagerEventHandler LineCountChanged;
		
		public class DelimiterSegment : ISegment
		{
			int offset;
			int length;
			
			public int Offset {
				get {
					return offset;
				}
				set {
					offset = value;
				}
			}
			
			public int Length {
				get {
					return length;
				}
				set {
					length = value;
				}
			}
		}
	}
}
