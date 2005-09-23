// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Services;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;

using MonoDevelop.Services;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Commands
{
	internal class SharpDevelopStringTagProvider :  IStringTagProvider 
	{
		IProjectService projectService = Runtime.ProjectService;
		
		public string[] Tags {
			get {
				return new string[] { "ITEMPATH", "ITEMDIR", "ITEMFILENAME", "ITEMEXT",
				                      "CURLINE", "CURCOL", "CURTEXT",
				                      "TARGETPATH", "TARGETDIR", "TARGETNAME", "TARGETEXT",
				                      "PROJECTDIR", "PROJECTFILENAME",
				                      "COMBINEDIR", "COMBINEFILENAME",
				                      "STARTUPPATH"};
			}
		}
		
		string GetCurrentItemPath()
		{
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null && !WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.IsViewOnly && !WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.IsUntitled) {
				return WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.ContentName;
			}
			return String.Empty;
		}
		
		string GetCurrentTargetPath()
		{
			if (projectService.CurrentSelectedProject != null) {
				return projectService.CurrentSelectedProject.GetOutputFileName ();
			}
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null) {
				string fileName = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.ContentName;
				Project project = projectService.GetProject (fileName);
				if (project != null) return project.GetOutputFileName();
			}
			return String.Empty;
		}
		
		public string Convert(string tag)
		{
			switch (tag) {
				case "ITEMPATH":
					try {
						return GetCurrentItemPath();
					} catch (Exception) {}
					break;
				case "ITEMDIR":
					try {
						return Path.GetDirectoryName(GetCurrentItemPath());
					} catch (Exception) {}
					break;
				case "ITEMFILENAME":
					try {
						return Path.GetFileName(GetCurrentItemPath());
					} catch (Exception) {}
					break;
				case "ITEMEXT":
					try {
						return Path.GetExtension(GetCurrentItemPath());
					} catch (Exception) {}
					break;
				
				// TODO:
				case "CURLINE":
					return String.Empty;
				case "CURCOL":
					return String.Empty;
				case "CURTEXT":
					return String.Empty;
				
				case "TARGETPATH":
					try {
						return GetCurrentTargetPath();
					} catch (Exception) {}
					break;
				case "TARGETDIR":
					try {
						return Path.GetDirectoryName(GetCurrentTargetPath());
					} catch (Exception) {}
					break;
				case "TARGETNAME":
					try {
						return Path.GetFileName(GetCurrentTargetPath());
					} catch (Exception) {}
					break;
				case "TARGETEXT":
					try {
						return Path.GetExtension(GetCurrentTargetPath());
					} catch (Exception) {}
					break;
				
				case "PROJECTDIR":
					if (projectService.CurrentSelectedProject != null) {
						return projectService.CurrentSelectedProject.BaseDirectory;
					}
					break;
				case "PROJECTFILENAME":
					if (projectService.CurrentSelectedProject != null) {
						try {
							return Path.GetFileName(projectService.CurrentSelectedProject.FileName);
						} catch (Exception) {}
					}
					break;
				
				case "COMBINEDIR":
					return Path.GetDirectoryName (projectService.CurrentOpenCombine.FileName);

				case "COMBINEFILENAME":
					try {
						return Path.GetFileName(projectService.CurrentOpenCombine.FileName);
					} catch (Exception) {}
					break;
				case "STARTUPPATH":
					//return Application.StartupPath;
					return "";
			}
			return String.Empty;
		}
	}

}
