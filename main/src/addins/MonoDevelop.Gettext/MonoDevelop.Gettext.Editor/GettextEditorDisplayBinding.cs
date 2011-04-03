//
// GettextEditorDisplayBinding.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2007 David Makovský
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects;

namespace MonoDevelop.Gettext
{	
	public class GettextEditorDisplayBinding : IViewDisplayBinding
	{
		public  string Name {
			get { return GettextCatalog.GetString ("Gettext Editor"); }
		}
		
		public bool CanHandle (FilePath filePath, string mimeType, Project project)
		{
			return filePath.IsNotNull && filePath.HasExtension (".po");
		}
		
		public IViewContent CreateContent (FilePath filePath, string mimeType, Project project)
		{
			foreach (TranslationProject tp in IdeApp.Workspace.GetAllSolutionItems<TranslationProject>  ())
				if (tp.BaseDirectory == Path.GetDirectoryName (filePath))
					return new Editor.CatalogEditorView (tp, filePath);
			
			return new Editor.CatalogEditorView (null, filePath);
		}
		
		public bool CanUseAsDefault {
			get { return true; }
		}
	}
}
