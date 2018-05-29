// 
// TestDocument.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	public class TestDocument : MonoDevelop.Ide.Gui.Document
	{
		public ParsedDocument HiddenParsedDocument;

		public override ParsedDocument ParsedDocument {
			get {
				return HiddenParsedDocument;
			}
		}
		
		public TestDocument (MonoDevelop.Ide.Gui.IWorkbenchWindow window) : base(window)
		{
		}

		public void UpdateProject (Project project)
		{
			SetProject (project);
		}
		
		public IProjectContent HiddenProjectContent;

//		public override IProjectContent GetProjectContext ()
//		{
//			if (HiddenProjectContent != null)
//				return HiddenProjectContent;
//			return base.GetProjectContext ();
//		}
	}
}

