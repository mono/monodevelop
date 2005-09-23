// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This class handles the bookmarks for a buffer
	/// </summary>
	public class BookmarkManager : IBookMarkManager
	{
		ArrayList bookmark = new ArrayList();
		
		/// <value>
		/// Contains all bookmarks as int values
		/// </value>
		public ArrayList Marks {
			get {
				return bookmark;
			}
		}
	
		/// <summary>
		/// Creates a new instance of <see cref="BookmarkManager"/>
		/// </summary>
		public BookmarkManager(ILineManager lineTracker)
		{
			lineTracker.LineCountChanged += new LineManagerEventHandler(MoveIndices);
		}
		
		void OnChanged() 
		{
			if (Changed != null) {
				Changed(this, null);
			}
		}
		void OnBeforeChanged() 
		{
			if (BeforeChanged != null) {
				BeforeChanged(this, null);
			}
		}
			
		/// <remarks>
		/// Sets the mark at the line <code>lineNr</code> if it is not set, if the
		/// line is already marked the mark is cleared.
		/// </remarks>
		public void ToggleMarkAt(int lineNr)
		{
			OnBeforeChanged();
			for (int i = 0; i < bookmark.Count; ++i) {
				if ((int)bookmark[i] == lineNr) {
					bookmark.RemoveAt(i);
					OnChanged();
					return;
				}
			}
			bookmark.Add(lineNr);
			OnChanged();
		}
		
		/// <returns>
		/// true, if a mark at mark exists, otherwise false
		/// </returns>
		public bool IsMarked(int lineNr)
		{
			for (int i = 0; i < bookmark.Count; ++i) {
				if ((int)bookmark[i] == lineNr) {
					return true;
				}
			}
			return false;
		}
		
		/// <summary>
		/// This method moves all indices from index upward count lines
		/// (useful for deletion/insertion of text)
		/// </summary>
		void MoveIndices(object sender,LineManagerEventArgs e)
		{
			bool changed = false;
			OnBeforeChanged();
			for (int i = 0; i < bookmark.Count; ++i) {
				int mark = (int)bookmark[i];
				if (e.LinesMoved < 0 && mark == e.LineStart) {
					bookmark.RemoveAt(i);
					--i;
					changed = true;
				} else if (mark > e.LineStart + 1 || (e.LinesMoved < 0 && mark > e.LineStart))  {
					changed = true;
					bookmark[i] = mark + e.LinesMoved;
				}
			}
			
			if (changed) {
				OnChanged();
			}
		}
//		
//		/// <remarks>
//		/// Creates a new memento
//		/// </remarks>
//		public IXmlConvertable CreateMemento()
//		{
//			return new BookmarkManagerMemento((ArrayList)bookmark.Clone());
//		}
//		
//		/// <remarks>
//		/// Sets a memento
//		/// </remarks>
//		public void SetMemento(IXmlConvertable memento)
//		{
//			bookmark = ((BookmarkManagerMemento)memento).Bookmarks;
//		}
		
		/// <remarks>
		/// Clears all bookmark
		/// </remarks>
		public void Clear()
		{
			OnBeforeChanged();
			bookmark.Clear();
			OnChanged();
		}
		
		/// <value>
		/// The lowest mark, if no marks exists it returns -1
		/// </value>
		public int FirstMark {
			get {
				if (bookmark.Count < 1) {
					return -1;
				}
				int first = (int)bookmark[0];
				for (int i = 1; i < bookmark.Count; ++i) {
					first = Math.Min(first, (int)bookmark[i]);
				}
				return first;
			}
		}
		
		/// <value>
		/// The highest mark, if no marks exists it returns -1
		/// </value>
		public int LastMark {
			get {
				if (bookmark.Count < 1) {
					return -1;
				}
				int last = (int)bookmark[0];
				for (int i = 1; i < bookmark.Count; ++i) {
					last = Math.Max(last, (int)bookmark[i]);
				}
				return last;
			}
		}
		
		/// <remarks>
		/// returns first mark higher than <code>lineNr</code>
		/// </remarks>
		/// <returns>
		/// returns the next mark > cur, if it not exists it returns FirstMark()
		/// </returns>
		public int GetNextMark(int curLineNr)
		{
			int next = -1;
			for (int i = 0; i < bookmark.Count; ++i) {
				int j = (int)bookmark[i];
				if (j > curLineNr) {
					next = next == -1 ? j : Math.Min(next, j);
				}
			}
			return next == -1 ? FirstMark : next;
		}
		
		/// <remarks>
		/// returns first mark lower than <code>lineNr</code>
		/// </remarks>
		/// <returns>
		/// returns the next mark lower than cur, if it not exists it returns LastMark()
		/// </returns>
		public int GetPrevMark(int curLineNr)
		{
			int prev = -1;
			for (int i = 0; i < bookmark.Count; ++i) {
				int j = (int)bookmark[i];
				if (j < curLineNr) {
					prev = prev == -1 ? j : Math.Max(prev, j);
				}
			}
			return prev == -1 ? LastMark : prev;
		}
		
		/// <remarks>
		/// Is fired before the bookmarks change
		/// </remarks>
		public event EventHandler BeforeChanged;
		
		/// <remarks>
		/// Is fired after the bookmarks change
		/// </remarks>
		public event EventHandler Changed;
	}
}
