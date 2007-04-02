// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Alpert" email="david@spinthemoose.com"/>
//     <version>$Revision: 2247 $</version>
// </file>

using System;
using System.Drawing;
using System.IO;

using MonoDevelop.Ide.Gui;

namespace MonoDevelop.SourceEditor.Gui {
	/// <summary>
	/// Description of TextNavigationPoint.
	/// </summary>
	public class TextNavigationPoint : DefaultNavigationPoint {
		const int THREASHOLD = 5;
		string content;
		int lineNumber;
		int column;
		
#region constructors
		public TextNavigationPoint () : this (String.Empty, 1, 1)
		{
			
		}
		
		public TextNavigationPoint (string fileName) : this (fileName, 1, 1)
		{
			
		}
		
		public TextNavigationPoint (string fileName, int lineNumber, int column)
			: this (fileName, lineNumber, column, String.Empty)
		{
			
		}
		
		public TextNavigationPoint (string fileName, int lineNumber, int column, string content)
			: base (fileName)
		{
			this.column = column;
			this.lineNumber = lineNumber;
			this.content = content.Trim ();
		}
#endregion
		
		// TODO: Navigation - eventually, we'll store a reference to the document
		//       itself so we can track filename changes, inserts (that affect
		//       line numbers), and dynamically retrieve the text at this.lineNumber
		//
		//       what happens to the doc reference when the document is closed?
		//
		public int LineNumber {
			get { return lineNumber; }
		}
		
		public int Column {
			get { return column; }
		}
		
		public override void JumpTo ()
		{
			IdeApp.Workbench.OpenDocument (FileName, lineNumber, column, true);
		}
		
		public override void ContentChanging (object sender, EventArgs e)
		{
			// TODO: Navigation - finish ContentChanging
//			if (e is DocumentEventArgs) {
//				DocumentEventArgs de = (DocumentEventArgs)e;
//				if (this.LineNumber >= 
//			}
		}
		
#region IComparable
		public override int CompareTo (object obj)
		{
			int cmp;
			
			if ((cmp = base.CompareTo (obj)) != 0)
				return cmp;
			
			TextNavigationPoint b = obj as TextNavigationPoint;
			
			if (this.LineNumber == b.LineNumber)
				return 0;
			
			if (this.LineNumber > b.LineNumber)
				return 1;
			
			return -1;
		}
#endregion
		
#region Equality
		public override bool Equals (object obj)
		{
			TextNavigationPoint b = obj as TextNavigationPoint;
			
			if (b == null)
				return false;
			
			return this.FileName.Equals (b.FileName)
				&& (Math.Abs (this.LineNumber - b.LineNumber) <= THREASHOLD);
		}
		
		public override int GetHashCode ()
		{
			return this.FileName.GetHashCode () ^ this.LineNumber.GetHashCode ();
		}
#endregion
		
		public override string Description {
			get {
				return String.Format ("{0}: {1}", LineNumber, content);
			}
		}
		
		public override string FullDescription {
			get {
				return String.Format ("{0} - {1}",
				                      Path.GetFileName (FileName),
				                      Description);
			}
		}
	}
}
