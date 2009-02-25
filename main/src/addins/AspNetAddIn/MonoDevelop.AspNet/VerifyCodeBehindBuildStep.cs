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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.AspNet.Parser;

namespace MonoDevelop.AspNet
{
	
	public class VerifyCodeBehindBuildStep : ProjectServiceExtension
	{
		
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem project, string configuration)
		{
			AspNetAppProject aspProject = project as AspNetAppProject;
			
			if (aspProject == null || aspProject.LanguageBinding == null)
				return base.Build (monitor, project, configuration);
			
			//get the config object and validate
			AspNetAppProjectConfiguration config = (AspNetAppProjectConfiguration) aspProject.GetConfiguration (configuration);
			if (config == null) {
				monitor.Log.WriteLine (GettextCatalog.GetString
					("Project configuration is invalid. Skipping CodeBehind member generation."));
				return base.Build (monitor, project, configuration);
			}
			
			if (config.DisableCodeBehindGeneration) {
				monitor.Log.WriteLine (GettextCatalog.GetString
					("Skipping updating of CodeBehind partial classes, because this feature is disabled."));
				return base.Build (monitor, project, configuration);
			}
			
			System.CodeDom.Compiler.CodeDomProvider provider = aspProject.LanguageBinding.GetCodeDomProvider ();
			bool supportsPartialTypes = provider.Supports (System.CodeDom.Compiler.GeneratorSupport.PartialTypes);
			if (!supportsPartialTypes) {
				monitor.Log.WriteLine (GettextCatalog.GetString 
					("The code generator for {0} does not support partial classes. Skipping CodeBehind member generation.", 
					aspProject.LanguageBinding.Language));;
				return base.Build (monitor, project, configuration);
			}
			
			//get the extension used for codebehind files
			string langExt = aspProject.LanguageBinding.GetFileName ("a");
			langExt = langExt.Substring (1, langExt.Length - 1);
			
			List<CodeBehindWarning> errors = new List<CodeBehindWarning> ();
			
			monitor.Log.WriteLine (GettextCatalog.GetString ("Generating CodeBehind members..."));
			int nUpdated = 0;
			List<string> openFiles = null;
			List<KeyValuePair<string,string>> filesToWrite = new List<KeyValuePair<string,string>> ();
			
			//go over all the files generating members where necessary
			foreach (ProjectFile file in aspProject.Files)
			{
				WebSubtype type = AspNetAppProject.DetermineWebSubtype (file.FilePath);
				if (type != WebSubtype.WebForm && type != WebSubtype.WebControl && type != WebSubtype.MasterPage)
						continue;
				
				//find the designer file
				ProjectFile designerFile = aspProject.Files.GetFile (file.Name + ".designer" + langExt);
				if (designerFile == null)
					aspProject.Files.GetFile (file.Name + ".Designer" + langExt);
				if (designerFile == null)
					continue;
				
				//only regenerate the designer class if it's older than the aspx (etc) file
				if (System.IO.File.GetLastWriteTimeUtc (designerFile.FilePath)
				    > System.IO.File.GetLastWriteTimeUtc (file.FilePath))
					continue;
				
				//parse the ASP.NET file
				AspNetParsedDocument parsedDocument = ProjectDomService.Parse (aspProject, file.FilePath, null)
					as AspNetParsedDocument;
				if (parsedDocument == null || parsedDocument.Document == null)
					continue;
				
				string className = parsedDocument.PageInfo.InheritedClass;
				
				//initialising this list may generate more errors so we do it here
				MemberListVisitor memberList = null;
				if (!string.IsNullOrEmpty (className))
					memberList = parsedDocument.Document.MemberList;
				
				//log errors
				if (parsedDocument.Document.ParseErrors.Count > 0) {
					foreach (Exception e in parsedDocument.Document.ParseErrors) {
						CodeBehindWarning cbw;
						ErrorInFileException eife = e as ErrorInFileException;
						if (eife != null)
							cbw = new CodeBehindWarning (eife);
						else
							cbw = new CodeBehindWarning (
							    GettextCatalog.GetString ("Parser failed with error {0}. CodeBehind members for this file will not be added.", e.ToString ()),
							    file.FilePath
							    );
						errors.Add (cbw);
					}
				}
				
				if (string.IsNullOrEmpty (className))
					continue;
				
				//initialise the generated type
				System.CodeDom.CodeCompileUnit ccu = new System.CodeDom.CodeCompileUnit ();
				System.CodeDom.CodeNamespace namespac = new System.CodeDom.CodeNamespace ();
				ccu.Namespaces.Add (namespac); 
				System.CodeDom.CodeTypeDeclaration typeDecl = new System.CodeDom.CodeTypeDeclaration ();
				typeDecl.IsClass = true;
				typeDecl.IsPartial = true;
				namespac.Types.Add (typeDecl);
				
				//name the class and namespace
				int namespaceSplit = className.LastIndexOf ('.');
				string namespaceName = null;
				if (namespaceSplit > -1) {
					namespac.Name = className.Substring (0, namespaceSplit);
					typeDecl.Name = className.Substring (namespaceSplit + 1);
				} else {
					typeDecl.Name = className;
				}
				
				string masterTypeName = null;
				if (!String.IsNullOrEmpty (parsedDocument.PageInfo.MasterPageTypeName)) {
					masterTypeName = parsedDocument.PageInfo.MasterPageTypeName;
				} else if (!String.IsNullOrEmpty (parsedDocument.PageInfo.MasterPageTypeVPath)) {
					try {
						ProjectFile resolvedMaster = aspProject.ResolveVirtualPath (parsedDocument.PageInfo.MasterPageTypeVPath, parsedDocument.FileName);
						AspNetParsedDocument masterParsedDocument = null;
						if (resolvedMaster != null)
							masterParsedDocument = ProjectDomService.Parse (aspProject, resolvedMaster.FilePath, null)	as AspNetParsedDocument;
						if (masterParsedDocument != null && !String.IsNullOrEmpty (masterParsedDocument.PageInfo.InheritedClass)) {
							masterTypeName = masterParsedDocument.PageInfo.InheritedClass;
						} else {
							errors.Add (new CodeBehindWarning (String.Format ("Could not find type for master '{0}'",
							                                                  parsedDocument.PageInfo.MasterPageTypeVPath),
							                                   parsedDocument.FileName));
						}
					} catch (Exception ex) {
						errors.Add (new CodeBehindWarning (String.Format ("Could not find type for master '{0}'",
						                                                  parsedDocument.PageInfo.MasterPageTypeVPath),
						                                   parsedDocument.FileName));
						LoggingService.LogWarning ("Error resolving master page type", ex);
					}
				}
				
				if (masterTypeName != null) {
					System.CodeDom.CodeMemberProperty masterProp = new System.CodeDom.CodeMemberProperty ();
					masterProp.Name = "Master";
					masterProp.Type = new System.CodeDom.CodeTypeReference (masterTypeName);
					masterProp.HasGet = true;
					masterProp.HasSet = false;
					masterProp.Attributes = System.CodeDom.MemberAttributes.Public | System.CodeDom.MemberAttributes.New 
						| System.CodeDom.MemberAttributes.Final;
					masterProp.GetStatements.Add (new System.CodeDom.CodeMethodReturnStatement (
							new System.CodeDom.CodeCastExpression (masterTypeName, 
								new System.CodeDom.CodePropertyReferenceExpression (
									new System.CodeDom.CodeBaseReferenceExpression (), "Master"))));
					typeDecl.Members.Add (masterProp);
				}
				
				//add fields for each control in the page
				foreach (System.CodeDom.CodeMemberField member in memberList.Members.Values)
					typeDecl.Members.Add (member);
				
				System.CodeDom.Compiler.CodeGeneratorOptions options = new System.CodeDom.Compiler.CodeGeneratorOptions ();
				options.BlankLinesBetweenMembers = false;
				
				//check if designer files are open in the GUI. if they are, we want to edit them in-place
				if (openFiles == null)
					openFiles = GetOpenEditableFilesList ();
				
				//no? just write out to disc
				if (!openFiles.Contains (designerFile.FilePath)) {
					try {
						using (StreamWriter sw = new StreamWriter (designerFile.FilePath)) {
							provider.GenerateCodeFromCompileUnit (ccu, sw, options);
						}
						nUpdated++;
					} catch (IOException ex) {
						monitor.ReportError (
							GettextCatalog.GetString ("Failed to write file '{0}'.",
								designerFile.FilePath),
							ex);
					} catch (Exception ex) {
						monitor.ReportError (
							GettextCatalog.GetString ("Failed to generate code for file '{0}'.",
								designerFile.FilePath),
							ex);
					}
				}
				//file is open, so generate code and queue up for a write in the GUI thread
				else {
					try {
						using (StringWriter sw = new StringWriter ()) {
							provider.GenerateCodeFromCompileUnit (ccu, sw, options);
							filesToWrite.Add (new KeyValuePair<string, string> (
								designerFile.FilePath, sw.ToString ()));
						}
					} catch (Exception ex) {
						monitor.ReportError (
							GettextCatalog.GetString ("Failed to generate code for file '{0}'.",
								designerFile.FilePath),
							ex);
					}
				}
			}
			
			
			//these documents are open, so needs to run in GUI thread
			MonoDevelop.Core.Gui.DispatchService.GuiSyncDispatch (delegate {
				foreach (KeyValuePair<string, string> item in filesToWrite) {
					try {
						//get an interface to edit the file
						MonoDevelop.Projects.Text.IEditableTextFile textFile = 
							MonoDevelop.DesignerSupport.
							OpenDocumentFileProvider.Instance.GetEditableTextFile (item.Key);
						
						if (textFile == null)
							textFile = MonoDevelop.Projects.Text.TextFile.ReadFile (item.Key);
						
						//change the contents
						textFile.Text = item.Value;
						
						//save the file
						MonoDevelop.Projects.Text.TextFile tf = textFile as MonoDevelop.Projects.Text.TextFile;
						if (tf != null)
							tf.Save ();
						
						nUpdated++;
						
					} catch (IOException ex) {
						monitor.ReportError (
							GettextCatalog.GetString ("Failed to write file '{0}'.", item.Key),
							ex);
					}
					
					//save the changes
					foreach (MonoDevelop.Ide.Gui.Document doc in MonoDevelop.Ide.Gui.IdeApp.Workbench.Documents)
						doc.Save ();
				}
			});
			
			//write out a friendly message aout what we did
			if (nUpdated > 0) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("{0} CodeBehind designer classes updated.", nUpdated));
			} else {
				monitor.Log.WriteLine (GettextCatalog.GetString ("No changes made to CodeBehind classes."));
			}
			
			//and construct and return a build result
			BuildResult baseResult = base.Build (monitor, project, configuration);
			foreach (CodeBehindWarning cbw in errors) {
				if (cbw.FileName != null)
					baseResult.AddWarning (cbw.FileName, cbw.Line, cbw.Column, null, cbw.WarningText);
				else
					baseResult.AddWarning (cbw.WarningText);
					
			}
			return baseResult;
		}
		
		List<string> GetOpenEditableFilesList ()
		{
			List<string> list = new List<string> ();
			MonoDevelop.Core.Gui.DispatchService.GuiSyncDispatch (delegate {
				foreach (MonoDevelop.Ide.Gui.Document doc in MonoDevelop.Ide.Gui.IdeApp.Workbench.Documents)
					if (doc.GetContent<MonoDevelop.Ide.Gui.Content.IEditableTextBuffer> () != null)
						list.Add (doc.FileName);
			});
			return list;
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
