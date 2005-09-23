// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System.Drawing;
using MonoDevelop.TextEditor.Undo;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// An interface representing a portion of the current selection.
	/// </summary>
	public interface ISelection
	{
		Point StartPosition {
			get;
			set;
		}
		
		Point EndPosition {
			get;
			set;
		}
		
		int Offset {
			get;
		}
		
		int EndOffset {
			get;
		}
		
		int Length {
			get;
		}
		
		/// <value>
		/// Returns true, if the selection is rectangular
		/// </value>
		bool IsRectangularSelection {
			get;
		}
		
		/// <value>
		/// Returns true, if the selection is empty
		/// </value>
		bool IsEmpty {
			get;
		}

		/// <value>
		/// The text which is selected by this selection.
		/// </value>
		string SelectedText {
			get;
		}
		
		bool ContainsOffset(int offset);
		
		bool ContainsPosition(Point position);
	}
}
