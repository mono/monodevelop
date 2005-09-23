// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.Gui.Search
{
	/// <summary>
	/// This iterator iterates on a text buffer strategy.
	/// </summary>
	public interface ITextIterator
	{
		/// <value>
		/// Gets the current char this is the same as 
		/// GetCharRelative(0)
		/// </value>
		/// <exception cref="System.InvalidOperationException">
		/// If this method is called before the first MoveAhead or after 
		/// MoveAhead or after MoveAhead returns false.
		/// </exception>
		char Current {
			get;
		}
		
		/// <value>
		/// The current position of the text iterator cursor. It always begins
		/// at 0. It may be different from the real offset in the document.
		/// </value>
		int Position {
			get;
			set;
		}
		
		/// <value>
		/// The current line in the document
		/// </value>
		int Line { get; }
		
		/// <value>
		/// The current column in the document
		/// </value>
		int Column {get; }
		
		/// <value>
		/// The current offset in the document
		/// </value>
		int DocumentOffset { get; }
		
		/// <remarks>
		/// Gets a char relative to the current position (negative values
		/// will work too).
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		/// If this method is called before the first MoveAhead or after 
		/// MoveAhead or after MoveAhead returns false.
		/// Returns Char.MinValue if the relative position is outside the
		/// text limits.
		/// </exception>
		char GetCharRelative(int offset);
		
		/// <remarks>
		/// Moves the iterator position numChars
		/// </remarks>
		bool MoveAhead(int numChars);
		
		/// <remarks>
		/// Moves the iterator to the last valid position
		/// </remarks>
		void MoveToEnd ();
		
		string ReadToEnd ();
		
		/// <remarks>
		/// Rests the iterator
		/// </remarks>
		void Reset();
		
		void Replace (int length, string pattern);
		
		void Close ();
		
		IDocumentInformation DocumentInformation { get; }
		
		bool SupportsSearch (SearchOptions options, bool reverse);
		
		bool SearchNext (string text, SearchOptions options, bool reverse);
	}
}
