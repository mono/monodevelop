//
// ParserService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using Mono.Addins;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects
{
	public static class RefactoryService
	{
		static IParserDatabase parserDatabase;
		static CodeRefactorer  codeRefactorer;
		static IParserService  parserService;
		
		public static IParserDatabase ParserDatabase {
			get { return parserDatabase; }
		}
		
		public static IParserService ParserService {
			get { return parserService; }
		}
		
		static RefactoryService()
		{
			parserService = new DefaultParserService ();
			
			parserDatabase = ParserService.CreateParserDatabase ();
			parserDatabase.TrackFileChanges = true;
			parserDatabase.ParseProgressMonitorFactory = new ParseProgressMonitorFactory ();
		}
		
		public static CodeRefactorer CodeRefactorer {
			get {
				if (codeRefactorer == null) {
					codeRefactorer = new CodeRefactorer (ProjectService.Solution, ParserDatabase);
					codeRefactorer.TextFileProvider = new OpenDocumentFileProvider ();
				}
				return codeRefactorer;
			}
		}

		class ParseProgressMonitorFactory: IProgressMonitorFactory
		{
			public IProgressMonitor CreateProgressMonitor ()
			{
				return IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor ("Code Completion Database Generation", "md-parser");
			}
		}
		
		class OpenDocumentFileProvider: ITextFileProvider
		{
			public IEditableTextFile GetEditableTextFile (string filePath)
			{
				foreach (Document doc in IdeApp.Workbench.Documents) {
					if (doc.FileName == filePath) {
						IEditableTextFile ef = doc.GetContent<IEditableTextFile> ();
						if (ef != null) 
							return ef;
					}
				}
				return null;
			}
		}
	}
}
