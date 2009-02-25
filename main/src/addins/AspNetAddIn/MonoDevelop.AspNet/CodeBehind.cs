// 
// CodeBehind.cs:
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2007 Michael Hutchinson
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
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.DesignerSupport;

using MonoDevelop.AspNet.Parser;

namespace MonoDevelop.AspNet
{
	
	
	public static class CodeBehind
	{
		public static string GetCodeBehindClassName (ProjectFile file)
		{
			AspNetAppProject proj = file.Project as AspNetAppProject;
			if (proj == null)
				return null;
			
			AspNetParsedDocument cu = ProjectDomService.Parse (file.Project, file.FilePath, null) as AspNetParsedDocument;
			
			if (cu != null && string.IsNullOrEmpty (cu.PageInfo.InheritedClass))
				return cu.PageInfo.InheritedClass;
			else
				return null;
		}
		
		public static System.CodeDom.CodeCompileUnit GenerateCodeBehind (
			AspNetAppProject aspProject, AspNetParsedDocument parsedDocument, List<CodeBehindWarning> errors)
		{
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
						    parsedDocument.FileName);
					errors.Add (cbw);
				}
			}
			
			if (string.IsNullOrEmpty (className))
				return null;
			
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
			
			return ccu;
		}
	}
	
	public class CodeBehindWriter
	{
		List<string> openFiles;
		List<KeyValuePair<string,string>> filesToWrite = new List<KeyValuePair<string,string>> ();
		System.CodeDom.Compiler.CodeDomProvider provider;
		System.CodeDom.Compiler.CodeGeneratorOptions options;
		MonoDevelop.Core.IProgressMonitor monitor;
		
		public System.CodeDom.Compiler.CodeDomProvider Provider { get { return provider; } }
		
		public bool SupportsPartialTypes {
			get {
				return provider.Supports (System.CodeDom.Compiler.GeneratorSupport.PartialTypes);
			}
		}
		
		CodeBehindWriter (MonoDevelop.Core.IProgressMonitor monitor, System.CodeDom.Compiler.CodeDomProvider provider,
		                         System.CodeDom.Compiler.CodeGeneratorOptions options)
		{
			this.provider = provider;
			this.options = options;
			this.monitor = monitor;
		}
		
		public static CodeBehindWriter CreateForProject (MonoDevelop.Core.IProgressMonitor monitor, AspNetAppProject project)
		{
			System.CodeDom.Compiler.CodeGeneratorOptions options = new System.CodeDom.Compiler.CodeGeneratorOptions ();
			MonoDevelop.Ide.Gui.Content.TextStylePolicy pol = project.Policies.Get<MonoDevelop.Ide.Gui.Content.TextStylePolicy> ();
			options.IndentString = pol.TabsToSpaces? new string (' ', pol.TabWidth) : "\t";
			options.BlankLinesBetweenMembers = true;
			System.CodeDom.Compiler.CodeDomProvider provider = project.LanguageBinding.GetCodeDomProvider ();
			
			return new CodeBehindWriter (monitor, provider, options);
		}
		
		List<string> OpenFiles {
			get {
				if (openFiles == null) {
					openFiles = new List<string> ();
					MonoDevelop.Core.Gui.DispatchService.GuiSyncDispatch (delegate {
						foreach (MonoDevelop.Ide.Gui.Document doc in MonoDevelop.Ide.Gui.IdeApp.Workbench.Documents)
						if (doc.GetContent<MonoDevelop.Ide.Gui.Content.IEditableTextBuffer> () != null)
							openFiles.Add (doc.FileName);
					});
				}
				return openFiles;
			}
		}
		
		public void Write (System.CodeDom.CodeCompileUnit ccu, string path)
		{
			//no? just write out to disc
			if (!OpenFiles.Contains (path)) {
				try {
					using (StreamWriter sw = new StreamWriter (path)) {
						provider.GenerateCodeFromCompileUnit (ccu, sw, options);
					}
					WrittenCount++;
				} catch (IOException ex) {
					monitor.ReportError (GettextCatalog.GetString ("Failed to write file '{0}'.", path), ex);
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Failed to generate code for file '{0}'.", path), ex);
				}
			}
			//file is open, so generate code and queue up for a write in the GUI thread
			else {
				try {
					using (StringWriter sw = new StringWriter ()) {
						provider.GenerateCodeFromCompileUnit (ccu, sw, options);
						filesToWrite.Add (new KeyValuePair<string, string> (
							path, sw.ToString ()));
					}
				} catch (Exception ex) {
					monitor.ReportError (
						GettextCatalog.GetString ("Failed to generate code for file '{0}'.", path),	ex);
				}
			}
		}
		
		public void WriteOpenFiles ()
		{
			if (filesToWrite == null)
				return;
			
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
						
						WrittenCount++;
						
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
			
			filesToWrite = null;
		}
		
		public int WrittenCount { get; private set; }
	}
}
