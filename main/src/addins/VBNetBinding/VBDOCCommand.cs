//  VBDOCCommand.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Reflection;

using ICSharpCode.Core.Properties;
using ICSharpCode.Core.Services;

using ICSharpCode.Core.AddIns;
using ICSharpCode.Core.AddIns.Codons;

using ICSharpCode.SharpDevelop.Internal.Project;
using ICSharpCode.SharpDevelop.Services;

namespace VBBinding
{
	////<summary>
	/// Provides functions to run VB.DOC and to read the configuration of VB.DOC.
	/// </summary>
	public class VBDOCCommand : AbstractMenuCommand
	{
		///<summary>
		/// Runs VB.DOC for the given project
		/// </summary>
		public override void Run()
		{
			IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			VBProject project = (VBProject)projectService.CurrentSelectedProject;
			VBCompilerParameters compilerParameters = (VBCompilerParameters)project.ActiveConfiguration;
			
			Options options = new Options();
			string extension = compilerParameters.CompileTarget == CompileTarget.Exe ? ".dll" : ".exe";
			options.AssemblyFile = Path.Combine(compilerParameters.OutputDirectory, compilerParameters.OutputAssembly) + extension;
			
			ArrayList files = new ArrayList();
			foreach(ProjectFile file in project.ProjectFiles) {
				if(VBDOCConfigurationPanel.IsFileIncluded(file.Name, project)) {
					files.Add(file.Name);
				}
			}
			
			options.Files = (string[])files.ToArray(typeof(string));
			options.GlobalImports = compilerParameters.Imports.Split(',');
			options.OutputXML = compilerParameters.VBDOCOutputFile;
			options.Prefix = compilerParameters.VBDOCCommentPrefix;
			options.RootNamespace = compilerParameters.RootNamespace;
			
			ArrayList referenceDirs = new ArrayList();
			string mainDirectory = Path.GetDirectoryName(options.AssemblyFile);
			
			foreach(ProjectReference projectFile in project.ProjectReferences) {
				if(projectFile.ReferenceType == ReferenceType.Assembly) {
					string referenceDir = Path.GetDirectoryName(projectFile.Reference);
					if(referenceDir.ToLower() != mainDirectory.ToLower() && referenceDirs.Contains(referenceDir) == false) {
						referenceDirs.Add(referenceDir);
					}
				}
			}
			
			StringCollection errors = options.Validate();
		
			if(errors.Count > 0) {
				string message = "";
				foreach(string description in errors) {
					message += description + "\n";
				}
				MessageBox.Show(message, "Invalid VB.DOC options", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			
			VBDOCRunner runner = new VBDOCRunner();
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(resolve);
			GuiMessageRecipient messageRecipient = new GuiMessageRecipient();
			
			try {
				runner.RunVBDOC(options, messageRecipient);
			} catch(Exception ex) {
				MessageBox.Show("Documentation generation failed:\n" + ex.Message);
			} finally {
				messageRecipient.Finished();
				AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(resolve);
			}
			
		}
		
		Assembly resolve(object sender, ResolveEventArgs e)
		{
			if(e.Name.StartsWith("CommentExtractor")) {
				return Assembly.GetAssembly(typeof(VBDOCRunner));
			}
			return null;
		}
	}
}
