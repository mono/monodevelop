// PropertyPadTextEditorExtension.cs
//
// Author:
//   Lluis Sanchez Gual
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.DesignerSupport.PropertyGrid;

namespace MonoDevelop.DesignerSupport.Projects
{
	// This class implements a text editor extension which provides an
	// implementation of IPropertyPadProvider. Since text editor extensions
	// are chained in the command route, the designer service will find this
	// IPropertyPadProvider when the text editor has the focus.
	
	public class PropertyPadTextEditorExtension: TextEditorExtension, IPropertyPadProvider
	{
		public object GetActiveComponent ()
		{
			// Return the ProjectFile object of the file being edited
			
			if (Document.HasProject) {
				string file = Document.FileName;
				return Document.Project.Files.GetFile (file);
			}
			else
				return null;
		}

		public object GetProvider ()
		{
			return null;
		}

		public void OnEndEditing (object obj)
		{
		}

		public void OnChanged (object obj)
		{
			if (Document.HasProject)
				IdeApp.ProjectOperations.Save (Document.Project);
		}
	}
}
