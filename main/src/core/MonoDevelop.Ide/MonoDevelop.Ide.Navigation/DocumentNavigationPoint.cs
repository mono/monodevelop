// 
// DocumentNavigationPoint.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Navigation
{
	public class DocumentNavigationPoint : NavigationPoint
	{
		Document doc;

		public Document Document {
			get {
				return doc;
			}
		}

		FilePath fileName;
		string project;
		
		public DocumentNavigationPoint (Document doc)
		{
			SetDocument (doc);
		}

		protected void SetDocument (Document doc)
		{
			this.doc = doc;
			doc.Closed += HandleClosed;
		}

		public DocumentNavigationPoint (FilePath fileName)
		{
			this.fileName = fileName;
		}
		
		public override void Dispose ()
		{
			if (doc != null) {
				doc.Closed -= HandleClosed;
				doc = null;
			}
			base.Dispose ();
		}

		protected virtual void OnDocumentClosing ()
		{
		}

		void HandleClosed (object sender, EventArgs e)
		{
			OnDocumentClosing ();
			fileName = doc.FileName;
			project = doc.HasProject ? doc.Project.ItemId : null;
			if (fileName == FilePath.Null) {
				// If the document is not a file, dispose the navigation point because the document can't be reopened
				Dispose ();
			} else {
				doc.Closed -= HandleClosed;
				doc = null;
			}
		}
		
		public FilePath FileName {
			get { return doc != null? doc.FileName : fileName; }
		}

		public override Task<Document> ShowDocument ()
		{
			return DoShow ();
		}
		
		protected virtual async Task<Document> DoShow ()
		{
			if (doc != null) {
				doc.Select ();
				return doc;
			}
			MonoDevelop.Projects.Project p = null;
			foreach (var curP in IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ()) {
				if (curP.ItemId == project) {
					p = curP;
					break;
				}
			}
			return await IdeApp.Workbench.OpenDocument (new FileOpenInformation (fileName, p, true));
		}
		
		public override string DisplayName {
			get {
				return System.IO.Path.GetFileName (doc != null? doc.Name : (string) fileName);
			}
		}
		
		public override bool Equals (object o)
		{
			DocumentNavigationPoint dp = o as DocumentNavigationPoint;
			return dp != null && ((doc != null && doc == dp.doc) || (FileName != FilePath.Null && FileName == dp.FileName));
		}
		
		public override int GetHashCode ()
		{
			return (FileName != FilePath.Null ? FileName.GetHashCode () : 0) + (doc != null ? doc.GetHashCode () : 0);
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
