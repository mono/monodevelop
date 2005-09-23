// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// The line tracker keeps track of all lines in a document.
	/// </summary>
	public interface ILineManager
	{
		/// <value>
		/// A collection of all line segments
		/// </value>
		ArrayList LineSegmentCollection {
			get;
		}
		
		/// <value>
		/// The total number of lines, this may be != ArrayList.Count 
		/// if the last line ends with a delimiter.
		/// </value>
		int TotalNumberOfLines {
			get;
		}
		
		/// <value>
		/// The current <see cref="IHighlightingStrategy"/> attached to this line manager
		/// </value>
		IHighlightingStrategy HighlightingStrategy {
			get;
			set;
		}
		
		/// <remarks>
		/// Returns a valid line number for the given offset.
		/// </remarks>
		/// <param name="offset">
		/// A offset which points to a character in the line which
		/// line number is returned.
		/// </param>
		/// <returns>
		/// An int which value is the line number.
		/// </returns>
		/// <exception cref="System.ArgumentException">If offset points not to a valid position</exception>
		int GetLineNumberForOffset(int offset);
		
		/// <remarks>
		/// Returns a <see cref="LineSegment"/> for the given offset.
		/// </remarks>
		/// <param name="offset">
		/// A offset which points to a character in the line which
		/// is returned.
		/// </param>
		/// <returns>
		/// A <see cref="LineSegment"/> object.
		/// </returns>
		/// <exception cref="System.ArgumentException">If offset points not to a valid position</exception>
		LineSegment GetLineSegmentForOffset(int offset);
		
		/// <remarks>
		/// Returns a <see cref="LineSegment"/> for the given line number.
		/// This function should be used to get a line instead of getting the
		/// line using the <see cref="ArrayList"/>.
		/// </remarks>
		/// <param name="lineNumber">
		/// The line number which is requested.
		/// </param>
		/// <returns>
		/// A <see cref="LineSegment"/> object.
		/// </returns>
		/// <exception cref="System.ArgumentException">If offset points not to a valid position</exception>
		LineSegment GetLineSegment(int lineNumber);
		
		/// <summary>
		/// Used internally, do not call yourself.
		/// </summary>
		void Insert(int offset, string text);
		
		/// <summary>
		/// Used internally, do not call yourself.
		/// </summary>
		void Remove(int offset, int length);
		
		/// <summary>
		/// Used internally, do not call yourself.
		/// </summary>
		void Replace(int offset, int length, string text);
		
		/// <remarks>
		/// Sets the content of this line manager = break the text
		/// into lines.
		/// </remarks>
		void SetContent(string text);
		
		/// <remarks>
		/// Get the logical line for a given visible line.
		/// example : lineNumber == 100 foldings are in the linetracker
		/// between 0..1 (2 folded, invisible lines) this method returns 102
		/// the 'logical' line number
		/// </remarks>
		int GetLogicalLine(int lineNumber);
		
		/// <remarks>
		/// Get the visible line for a given logical line.
		/// example : lineNumber == 100 foldings are in the linetracker
		/// between 0..1 (2 folded, invisible lines) this method returns 98
		/// the 'visible' line number
		/// </remarks>
		int GetVisibleLine(int lineNumber);
		
		/// <remarks>
		/// Get the next visible line after lineNumber
		/// </remarks>
		int GetNextVisibleLineAbove(int lineNumber, int lineCount);
		
		/// <remarks>
		/// Get the next visible line below lineNumber
		/// </remarks>
		int GetNextVisibleLineBelow(int lineNumber, int lineCount);
		
		/// <remarks>
		/// Is fired when lines are inserted or removed
		/// </remarks>
		event LineManagerEventHandler LineCountChanged;
	}
}
