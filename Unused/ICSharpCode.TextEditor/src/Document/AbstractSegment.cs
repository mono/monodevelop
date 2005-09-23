// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using MonoDevelop.TextEditor.Undo;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This interface is used to describe a span inside a text sequence
	/// </summary>
	public class AbstractSegment : ISegment
	{
		protected int offset = -1;
		protected int length = -1;
		
#region MonoDevelop.TextEditor.Document.ISegment interface implementation
		public virtual int Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}
		
		public virtual int Length {
			get {
				return length;
			}
			set {
				length = value;
			}
		}
#endregion
	}
}
