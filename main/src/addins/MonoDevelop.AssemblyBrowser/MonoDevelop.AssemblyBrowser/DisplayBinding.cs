//
// DisplayBinding.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.IO;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.AssemblyBrowser
{
	public class AssemblyBrowserDisplayBinding : IViewDisplayBinding
	{
		public string Name {
			get {
				return GettextCatalog.GetString ("Assembly Browser");
			}
		}
		
		public bool CanUseAsDefault {
			get { return true; }
		}
		
		AssemblyBrowserViewContent viewContent = null;
		
		internal AssemblyBrowserViewContent GetViewContent ()
		{
			if (viewContent == null || viewContent.IsDisposed) {
				viewContent = new AssemblyBrowserViewContent ();
				viewContent.Control.Destroyed += HandleDestroyed;
			}
			return viewContent;
		}

		void HandleDestroyed (object sender, EventArgs e)
		{
			((Gtk.Widget)sender).Destroyed -= HandleDestroyed;
			this.viewContent = null;
		}
		
		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			return mimeType == "application/x-ms-dos-executable"
				|| mimeType == "application/x-executable"
				|| mimeType == "application/x-msdownload";
		}
		
		public IViewContent CreateContent (FilePath fileName, string mimeType, Project ownerProject)
		{
			return GetViewContent ();
		}
	}
}