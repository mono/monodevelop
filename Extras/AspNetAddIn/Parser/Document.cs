//
// Document.cs: Parses an ASP.NET file, and provides a range of services for 
//     gathering information from it.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using AspNetAddIn.Parser.Tree;
using MonoDevelop.Projects;

namespace AspNetAddIn.Parser
{
	public class Document
	{
		private RootNode rootNode;
		private ProjectFile projectFile;
		private WebFormReferenceManager refMan;
		private PageInfoVisitor info;
		private MemberListVisitor memberList;
		
		public Document (ProjectFile file)
		{
			this.projectFile = file;
			rootNode = new RootNode ();
			
			using (StreamReader sr = new StreamReader (file.FilePath)) {
				try {
					rootNode.Parse (file.FilePath, sr);
				} catch (Exception e) {
					MonoDevelop.Ide.Gui.IdeApp.Services.MessageService.ShowWarning ("The ASP.NET file parser failed for parse a page because: "+e.Message);
				}	
			}
		}
		
		public RootNode RootNode {
			get { return rootNode; }
		}
		
		public WebFormReferenceManager WebFormReferenceManager {
			get {
				if (refMan == null)
					refMan = new WebFormReferenceManager (this);
				return refMan;
			}
		}
		
		public PageInfoVisitor Info {
			get {
				if (info == null) {
					info = new PageInfoVisitor ();
					rootNode.AcceptVisit (info);
				}
				return info;
			}
		}
		
		public ProjectFile ProjectFile {
			get { return projectFile; }
		}
		
		public MemberListVisitor MemberList {
			get {
				if (memberList == null) {
					memberList = new MemberListVisitor (this);
					rootNode.AcceptVisit (memberList);
				}
				return memberList;
			}
		}
	}
}
