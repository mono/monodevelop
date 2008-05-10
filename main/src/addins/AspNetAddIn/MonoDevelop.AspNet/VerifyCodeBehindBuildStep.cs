// 
// VerifyCodeBehindBuildStep.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2006-2007 Michael Hutchinson
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.AspNet.Parser;

namespace MonoDevelop.AspNet
{
	
	public class VerifyCodeBehindBuildStep : ProjectServiceExtension
	{
		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry project)
		{
			AspNetAppProject aspProject = project as AspNetAppProject;
			List<CodeBehindWarning> errors = new List<CodeBehindWarning> ();
			
			if (aspProject == null || aspProject.LanguageBinding == null || aspProject.LanguageBinding.Refactorer == null)
				return base.Build (monitor, project);
			
			RefactorOperations ops = aspProject.LanguageBinding.Refactorer.SupportedOperations;
			if ((ops & RefactorOperations.AddField) != RefactorOperations.AddField)
				return base.Build (monitor, project);
			
			//lists of members to be added 
			List<System.CodeDom.CodeMemberField> membersToAdd = new List<System.CodeDom.CodeMemberField> ();
			List<IClass> classesForMembers = new List<IClass> ();
			
			AspNetAppProjectConfiguration config = (AspNetAppProjectConfiguration) aspProject.ActiveConfiguration;
			
			//get an updated parser database
			IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (aspProject);
			ctx.UpdateDatabase ();
			
			monitor.Log.WriteLine (GettextCatalog.GetString ("Generating CodeBehind members..."));
			if (!config.GenerateNonPartialCodeBehindMembers)
				monitor.Log.WriteLine (GettextCatalog.GetString ("Auto-generation of CodeBehind members is disabled for non-partial classes."));
			
			//find the members that need to be added to CodeBehind classes
			foreach (ProjectFile file in aspProject.ProjectFiles) {
				
				WebSubtype type = AspNetAppProject.DetermineWebSubtype (Path.GetExtension (file.FilePath));
				if ((type != WebSubtype.WebForm) && (type != WebSubtype.WebControl) && (type != WebSubtype.MasterPage))
						continue;
				
				IParseInformation pi = ctx.GetParseInformation (file.FilePath);
				AspNetCompilationUnit cu = pi.MostRecentCompilationUnit as AspNetCompilationUnit;
				if (cu == null || cu.Document == null)
					continue;
				
				if (cu.ErrorsDuringCompile) {
					foreach (Exception e in cu.Document.ParseErrors) {
						CodeBehindWarning cbw;
						ErrorInFileException eife = e as ErrorInFileException;
						if (eife != null)
							cbw = new CodeBehindWarning (eife);
						else
							cbw = new CodeBehindWarning (
							    GettextCatalog.GetString ("Parser failed with error {0}. CodeBehind members for this file will not be added.", e.ToString ()),
							    file.FilePath
							    );
						monitor.Log.WriteLine (cbw.ToString ());
						errors.Add (cbw);
					}
				}
				
				string className = cu.PageInfo.InheritedClass;
				if (className == null)
					continue;
				
				IClass cls = ctx.GetClass (className);
				if (cls == null) {
					CodeBehindWarning cbw = new CodeBehindWarning (
						GettextCatalog.GetString ("Cannot find CodeBehind class '{0}'.", className),
					    file.FilePath
					);
					monitor.Log.WriteLine (cbw.ToString ());
					errors.Add (cbw);
					continue;
				}
				
				//handle partial designer classes; skip if non-partial and this is disabled
				IClass partToAddTo = CodeBehind.GetDesignerClass (cls);
				if (partToAddTo == null) {
					if (!config.GenerateNonPartialCodeBehindMembers)
						continue;
					else
						partToAddTo = cls;
				}
				
				//parse the ASP document 
				Document doc = cu.Document;
				
				//collect the members to be added
				try {
					foreach (System.CodeDom.CodeMemberField member in doc.MemberList.Members.Values) {
						try {
							MonoDevelop.Projects.Parser.IMember existingMember = BindingService.GetCompatibleMemberInClass (cls, member);
							if (existingMember == null) {
								classesForMembers.Add (partToAddTo);
								membersToAdd.Add (member);
							}
						} catch (ErrorInFileException ex) {
							CodeBehindWarning cbw = new CodeBehindWarning (ex);
							monitor.Log.WriteLine (cbw.ToString ());
							errors.Add (cbw);
						}
					}
				} catch (Exception e) {
					CodeBehindWarning cbw = new CodeBehindWarning (
					    GettextCatalog.GetString ("CodeBehind member generation failed with error {0}. Further CodeBehind members for this file will not be added.", e.ToString ()),
					    file.FilePath
					    );
					monitor.Log.WriteLine (cbw.ToString ());
					errors.Add (cbw);
				}
			}
			
			//add all the members
			//documents may be open, so needs to run in GUI thread
			MonoDevelop.Core.Gui.DispatchService.GuiSyncDispatch (delegate {
				for (int i = 0; i < membersToAdd.Count; i++)
				try {
					BindingService.GetCodeGenerator (project).AddMember (classesForMembers[i], membersToAdd[i]);
				} catch (MemberExistsException m) {
					CodeBehindWarning cbw = new CodeBehindWarning (m);
					monitor.Log.WriteLine (cbw.ToString ());
					errors.Add (cbw);
				}
			});
			
			if (membersToAdd.Count > 0) {
				monitor.Log.WriteLine (GettextCatalog.GetPluralString (
				    "Added {0} member to CodeBehind classes. Saving updated source files.",
				    "Added {0} members to CodeBehind classes. Saving updated source files.",
				    membersToAdd.Count, membersToAdd.Count
				));
				
				//make sure updated files are saved before compilation
				MonoDevelop.Core.Gui.DispatchService.GuiSyncDispatch (delegate {
					foreach (MonoDevelop.Ide.Gui.Document guiDoc in MonoDevelop.Ide.Gui.IdeApp.Workbench.Documents)
						if (guiDoc.IsDirty)
							guiDoc.Save ();
				});
			} else {
				monitor.Log.WriteLine (GettextCatalog.GetString ("No changes made to CodeBehind classes."));
			}
			
			ICompilerResult baseResult = base.Build (monitor, project);
			foreach (CodeBehindWarning cbw in errors) {
				if (cbw.FileName != null)
					baseResult.AddWarning (cbw.FileName, cbw.Line, cbw.Column, null, cbw.WarningText);
				else
					baseResult.AddWarning (cbw.WarningText);
					
			}
			return baseResult;
		}
		
		class CodeBehindWarning
		{
			string fileName = null;
			int line = 0;
			int col = 0;
			string warningText;
			
			public CodeBehindWarning (string warningText)
				: this (warningText, null)
			{
			}
			
			public CodeBehindWarning (string warningText, string fileName)
				: this (warningText, fileName, 0, 0)
			{
			}
			
			public CodeBehindWarning (string warningText, string fileName, int line, int col)
			{
				this.warningText = warningText;
				this.fileName = fileName;
				this.line = line;
				this.col = col;
			}
			
			public CodeBehindWarning (ErrorInFileException ex)
				: this (ex.ToString (), ex.FileName, ex.Line, ex.Column)
			{
			}
			
			public string FileName {
				get { return fileName; }
			}
			
			public string WarningText {
				get { return warningText; }
			}
			
			public int Line {
				get { return line; }
			}
			
			public int Column {
				get { return col; }
			}
			
			public override string ToString ()
			{
				return string.Format ("{0}({1},{2}): {3}", FileName, Line, Column, WarningText);
			}
		}
	}
}
