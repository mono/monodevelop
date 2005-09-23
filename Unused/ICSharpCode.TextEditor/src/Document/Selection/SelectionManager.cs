// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Drawing;
using System.Collections;
using System.Text;
using MonoDevelop.TextEditor.Undo;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This class manages the selections in a document.
	/// </summary>
	public class SelectionManager
	{
		IDocument document;
		SelectionCollection selectionCollection = new SelectionCollection();
		
		/// <value>
		/// A collection containing all selections.
		/// </value>
		public SelectionCollection SelectionCollection {
			get {
				return selectionCollection;
			}
		}
		
		/// <value>
		/// true if the <see cref="SelectionCollection"/> is not empty, false otherwise.
		/// </value>
		public bool HasSomethingSelected {
			get {
				return selectionCollection.Count > 0;
			}
		}
		
		/// <value>
		/// The text that is currently selected.
		/// </value>
		public string SelectedText {
			get {
				StringBuilder builder = new StringBuilder();
				
//				PriorityQueue queue = new PriorityQueue();
				
				foreach (ISelection s in selectionCollection) {
					builder.Append(s.SelectedText);
//					queue.Insert(-s.Offset, s);
				}
				
//				while (queue.Count > 0) {
//					ISelection s = ((ISelection)queue.Remove());
//					builder.Append(s.SelectedText);
//				}
				
				return builder.ToString();
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="SelectionManager"/>
		/// </summary>
		public SelectionManager(IDocument document)
		{
			this.document = document;
			document.DocumentChanged += new DocumentEventHandler(DocumentChanged);
		}
		
		void DocumentChanged(object sender, DocumentEventArgs e)
		{
			if (e.Text == null) {
				Remove(e.Offset, e.Length);
			} else {
				if (e.Length < 0) {
					Insert(e.Offset, e.Text);
				} else {
					Replace(e.Offset, e.Length, e.Text);
				}
			}
		}
		
		/// <remarks>
		/// Clears the selection and sets a new selection
		/// using the given <see cref="ISelection"/> object.
		/// </remarks>
		public void SetSelection(ISelection selection)
		{
//			autoClearSelection = false;
			ClearSelection();
			
			if (selection != null) {
				selectionCollection.Add(selection);
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, selection.StartPosition.Y, selection.EndPosition.Y));
				document.CommitUpdate();
				OnSelectionChanged(EventArgs.Empty);
			}
		}
		
		bool GreaterEqPos(Point p1, Point p2)
		{
			return p1.Y > p2.Y || p1.Y == p2.Y && p1.X >= p2.X;
		}
		
		public void ExtendSelection(Point oldPosition, Point newPosition)
		{
			//TODO: Andrea darf basteln
			if (oldPosition == newPosition) {
				return;
			}
			Point min;
			Point max;
			bool  oldIsGreater = GreaterEqPos(oldPosition, newPosition);
			if (oldIsGreater) {
				min = newPosition;
				max = oldPosition;
			} else {
				min = oldPosition;
				max = newPosition;
			}
			if (!HasSomethingSelected) {
				SetSelection(new DefaultSelection(document, min, max));
				return;
			}
			ISelection selection = this.selectionCollection[0];
			if (selection.ContainsPosition(newPosition)) {
				if (oldIsGreater) {
					selection.EndPosition = newPosition;
				} else {
					selection.StartPosition = newPosition;
				}
			} else {
				if (oldPosition == selection.StartPosition) {
					if (GreaterEqPos(newPosition, selection.EndPosition)) {
						selection.StartPosition = selection.EndPosition;
						selection.EndPosition   = newPosition;
					} else {
						selection.StartPosition = newPosition;
					}
				} else {
					if (GreaterEqPos(selection.StartPosition, newPosition)) {
						selection.EndPosition = selection.StartPosition;
						selection.StartPosition   = newPosition;
					} else {
						selection.EndPosition = newPosition;
					}
				} 
			}
			
//			if (GreaterEqPos(selection.StartPosition, min) && GreaterEqPos(selection.EndPosition, max)) {
//				if (oldIsGreater) {
//					selection.StartPosition = min;
//				} else {				
//					selection.StartPosition = max;
//				}
//			} else {
//				if (oldIsGreater) {
//					selection.EndPosition = min;
//				} else {
//					selection.EndPosition = max;
//				}
//			}
			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, min.Y, max.Y));
			document.CommitUpdate();
			OnSelectionChanged(EventArgs.Empty);
		}
		
		/// <remarks>
		/// Clears the selection.
		/// </remarks>
		public void ClearSelection()
		{
			while (selectionCollection.Count > 0) {
				ISelection selection = selectionCollection[selectionCollection.Count - 1];
				selectionCollection.RemoveAt(selectionCollection.Count - 1);
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, selection.StartPosition.Y, selection.EndPosition.Y));
				OnSelectionChanged(EventArgs.Empty);
			}
			document.CommitUpdate();
		}
		
		/// <remarks>
		/// Removes the selected text from the buffer and clears
		/// the selection.
		/// </remarks>
		public void RemoveSelectedText()
		{
			ArrayList lines = new ArrayList();
			int offset = -1;
			bool oneLine = true;
//			PriorityQueue queue = new PriorityQueue();
			foreach (ISelection s in selectionCollection) {
//				ISelection s = ((ISelection)queue.Remove());
				if (oneLine) {
					int lineBegin = s.StartPosition.Y;
					if (lineBegin != s.EndPosition.Y) {
						oneLine = false;
					} else {
						lines.Add(lineBegin);
					}
				}
				offset = s.Offset;
				document.Remove(s.Offset, s.Length);

//				queue.Insert(-s.Offset, s);
			}
			ClearSelection();
			if (offset >= 0) {
//             TODO:
//				document.Caret.Offset = offset;
			}
			if (offset != -1) {
				if (oneLine) {
					foreach (int i in lines) {
						document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, i));
					}
				} else {
					document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
				}
				document.CommitUpdate();
			}
		}
		
		
		bool SelectionsOverlap(ISelection s1, ISelection s2)
		{
			return (s1.Offset <= s2.Offset && s2.Offset <= s1.Offset + s1.Length)                         ||
			       (s1.Offset <= s2.Offset + s2.Length && s2.Offset + s2.Length <= s1.Offset + s1.Length) ||
			       (s1.Offset >= s2.Offset && s1.Offset + s1.Length <= s2.Offset + s2.Length);
		}
		
		/// <remarks>
		/// Returns true if the given offset points to a section which is
		/// selected.
		/// </remarks>
		public bool IsSelected(int offset)
		{
			return GetSelectionAt(offset) != null;
		}
		
		/// <remarks>
		/// Returns a <see cref="ISelection"/> object giving the selection in which
		/// the offset points to.
		/// </remarks>
		/// <returns>
		/// <code>null</code> if the offset doesn't point to a selection
		/// </returns>
		public ISelection GetSelectionAt(int offset)
		{
			foreach (ISelection s in selectionCollection) {
				if (s.ContainsOffset(offset)) {
					return s;
				}
			}
			return null;
		}
		
		/// <remarks>
		/// Used internally, do not call.
		/// </remarks>
		public void Insert(int offset, string text)
		{
//			foreach (ISelection selection in SelectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset += text.Length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length += text.Length;
//				}
//			}
		}
		
		/// <remarks>
		/// Used internally, do not call.
		/// </remarks>
		public void Remove(int offset, int length)
		{
//			foreach (ISelection selection in selectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset -= length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length -= length;
//				}
//			}
		}
		
		/// <remarks>
		/// Used internally, do not call.
		/// </remarks>
		public void Replace(int offset, int length, string text)
		{
//			foreach (ISelection selection in selectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset = selection.Offset - length + text.Length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length = selection.Length - length + text.Length;
//				}
//			}
		}
		
		public ColumnRange GetSelectionAtLine(int lineNumber)
		{
			foreach (ISelection selection in selectionCollection) {
				int startLine = selection.StartPosition.Y;
				int endLine   = selection.EndPosition.Y;
				if (startLine < lineNumber && lineNumber < endLine) {
					return ColumnRange.WholeColumn;
				}
				
				if (startLine == lineNumber) {
					LineSegment line = document.GetLineSegment(startLine);
					int startColumn = selection.StartPosition.X;
					int endColumn   = endLine == lineNumber ? selection.EndPosition.X : line.Length + 1;
					return new ColumnRange(startColumn, endColumn);
				}
				
				if (endLine == lineNumber) {
					int endColumn   = selection.EndPosition.X;
					return new ColumnRange(0, endColumn);
				}
			}
			
			return ColumnRange.NoColumn;
		}
		
		protected virtual void OnSelectionChanged(EventArgs e)
		{
			if (SelectionChanged != null) {
				SelectionChanged(this, e);
			}
		}
		
		public event EventHandler SelectionChanged;
	}
}
