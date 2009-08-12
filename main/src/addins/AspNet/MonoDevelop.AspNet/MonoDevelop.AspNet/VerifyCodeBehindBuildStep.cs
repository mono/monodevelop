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
			
			CodeBehindWriter writer = CodeBehindWriter.CreateForProject (monitor, aspProject);
			if (!writer.SupportsPartialTypes) {
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
			
			bool updatedParseDb = false;
			
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
				
				//need parse DB to be up to date
				if (!updatedParseDb) {
					updatedParseDb = true;
					monitor.Log.Write (GettextCatalog.GetString ("Waiting for project type database to finish updating..."));
					ProjectDom dom = ProjectDomService.GetProjectDom (aspProject);
					dom.ForceUpdate (true);
					monitor.Log.WriteLine (GettextCatalog.GetString (" complete."));
				}
				
				//parse the ASP.NET file
				AspNetParsedDocument parsedDocument = ProjectDomService.Parse (aspProject, file.FilePath, null)
					as AspNetParsedDocument;
				if (parsedDocument == null || parsedDocument.Document == null)
					continue;
				
				System.CodeDom.CodeCompileUnit ccu = CodeBehind.GenerateCodeBehind (aspProject, parsedDocument, errors);
				if (ccu == null)
					continue;
				
				writer.Write (ccu, designerFile.FilePath);
			}
			
			writer.WriteOpenFiles ();
			
			//write out a friendly message aout what we did
			if (writer.WrittenCount > 0) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("{0} CodeBehind designer classes updated.", writer.WrittenCount));
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
	}
	
	public class CodeBehindWarning
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
