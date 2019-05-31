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
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Navigation
{
	public class DocumentNavigationPoint : NavigationPoint
	{
		Document document;

		public Document Document {
			get {
				return document;
			}
		}

		FilePath fileName;
		string project;
		
		public DocumentNavigationPoint (Document doc)
		{
			SetDocument (doc);
		}

		public DocumentNavigationPoint (FilePath fileName)
		{
			this.fileName = fileName;
		}

		protected void SetDocument (Document doc)
		{
			if (this.document != null)
				this.document.Closed -= HandleClosed;
			this.document = doc;
			doc.Closed += HandleClosed;
		}

		public override void Dispose ()
		{
			if (document != null) {
				document.Closed -= HandleClosed;
				document = null;
			}
			base.Dispose ();
		}

		protected virtual void OnDocumentClosing ()
		{
		}

		void HandleClosed (object sender, EventArgs e)
		{
			OnDocumentClosing ();
			fileName = document.FileName;
			project = document.Owner is SolutionItem item ? item.ItemId : null;
			if (fileName == FilePath.Null) {
				// If the document is not a file, dispose the navigation point because the document can't be reopened
				Dispose ();
			} else {
				document.Closed -= HandleClosed;
				document = null;
			}
		}
		
		public FilePath FileName {
			get { return document != null? document.FileName : fileName; }
		}

		public override Task<Document> ShowDocument ()
		{
			return DoShow ();
		}
		
		protected virtual async Task<Document> DoShow ()
		{
			if (document != null) {
				document.Select ();
				return document;
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
				return System.IO.Path.GetFileName (document != null? document.Name : (string) fileName);
			}
		}
		
		public override bool Equals (object o)
		{
			DocumentNavigationPoint dp = o as DocumentNavigationPoint;
			return dp != null && ((document != null && document == dp.document) || (FileName != FilePath.Null && FileName == dp.FileName));
		}
		
		public override int GetHashCode ()
		{
			return (FileName != FilePath.Null ? FileName.GetHashCode () : 0) + (document != null ? document.GetHashCode () : 0);
		}
		
		internal bool HandleRenameEvent (string oldName, string newName)
		{
			if (document == null && oldName == fileName) {
				fileName = newName;
				return true;
			}
			return false;
		}
	}
}
