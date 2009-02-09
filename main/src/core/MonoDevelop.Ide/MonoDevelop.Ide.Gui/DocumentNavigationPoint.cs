// 
// DocumentNavigationPoint.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace MonoDevelop.Ide.Gui
{
	
	
	public class DocumentNavigationPoint : NavigationPoint
	{
		Document doc;
		string fileName;
		
		public DocumentNavigationPoint (Document doc)
		{
			this.doc = doc;
			doc.Closed += HandleClosed;
		}

		void HandleClosed (object sender, EventArgs e)
		{
			doc.Closed -= HandleClosed;
			fileName = doc.FileName;
			if (fileName == null)
				RemoveSelfFromHistory ();
			doc = null;
		}
		
		public override string Tooltip {
			get { return doc != null? doc.Name : fileName; }
		}
		
		string FileName {
			get { return doc != null? doc.FileName : fileName; }
		}
		
		protected Document Document {
			get { return doc; }
		}
		
		protected override Document DoShow ()
		{
			if (doc != null) {
				doc.Select ();
				return doc;
			} else {
				return IdeApp.Workbench.OpenDocument (fileName, true);
			}
		}
		
		public override string DisplayName {
			get {
				return System.IO.Path.GetFileName (doc != null? doc.Name : fileName);
			}
		}
		
		public override bool Equals (object o)
		{
			DocumentNavigationPoint dp = o as DocumentNavigationPoint;
			return dp != null && ((doc != null && doc == dp.doc) || (FileName != null && FileName == dp.FileName));
		}
		
		public override int GetHashCode ()
		{
			return (FileName != null ? FileName.GetHashCode () : 0) + (doc != null ? doc.GetHashCode () : 0);
		}
		
		internal bool HandleRenameEvent (string oldName, string newName)
		{
			if (doc == null && oldName == fileName) {
				fileName = newName;
				return true;
			}
			return false;
		}
	}
}
