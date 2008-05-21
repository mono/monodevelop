// AddinDescriptionDisplayBinding.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinDescriptionDisplayBinding: IDisplayBinding
	{
		public string DisplayName {
			get {
				return AddinManager.CurrentLocalizer.GetString ("Add-in description editor");
			}
		}
		
		public bool CanCreateContentForFile (string fileName)
		{
			if (fileName.EndsWith (".addin.xml") || fileName.EndsWith (".xml")) {
				if (IdeApp.Workspace.IsOpen) {
					DotNetProject p = IdeApp.Workspace.GetProjectContainingFile (fileName) as DotNetProject;
					return p != null && AddinData.GetAddinData (p) != null;
				}
			}
			return false;
		}

		public bool CanCreateContentForMimeType (string mimetype)
		{
			return false;
		}

		public IViewContent CreateContentForFile (string fileName)
		{
			DotNetProject p = IdeApp.Workspace.GetProjectContainingFile (fileName) as DotNetProject;
			AddinData data = AddinData.GetAddinData (p);
			return new AddinDescriptionView (data);
		}

		public IViewContent CreateContentForMimeType (string mimeType, System.IO.Stream content)
		{
			throw new NotImplementedException();
		}

	}
}
