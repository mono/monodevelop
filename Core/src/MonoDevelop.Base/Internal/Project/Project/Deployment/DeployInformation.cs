// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Xml;

using MonoDevelop.Internal.Serialization;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Internal.Project
{
	public enum DeploymentStrategy {
		Script,
		Assembly,
		File
	}
	
	[DataItem ("DeploymentInformation")]
	public class DeployInformation
	{
		[DataItem ("Exclude")]
		class ExcludeFile 
		{
			[ProjectPathItemProperty ("file")]
			protected string fileName;
			
			public string FileName {
				get {
					return fileName;
				}
				set {
					fileName = value;
				}
			}
			
			public ExcludeFile()
			{
			}
			
			public ExcludeFile(string fileName)
			{
				this.fileName = fileName;
			}
		}
		
		[ItemProperty]
		[ItemProperty ("ExcludeFile", ValueType = typeof(ExcludeFile), Scope = 1)]
		ArrayList excludeFiles = new ArrayList();
		
		[ItemProperty ("target", DefaultValue = "")]
		string deployTarget = "";
		
		[ItemProperty ("script", DefaultValue = "")]
		string deployScript = "";
		
		[ItemProperty ("strategy")]
		DeploymentStrategy deploymentStrategy = DeploymentStrategy.File;
		
		public DeploymentStrategy DeploymentStrategy {
			get {
				return deploymentStrategy;
			}
			set {
				deploymentStrategy = value;
			}
		}
		
		ArrayList ExcludeFiles {
			get {
				return excludeFiles;
			}
		}
		
		public string DeployTarget {
			get {
				return deployTarget;
			}
			set {
				deployTarget = value;
				if (deployTarget.EndsWith(Path.DirectorySeparatorChar.ToString())) {
					deployTarget = deployTarget.Substring(0, deployTarget.Length - 1);
				}
			}
		}
		
		public string DeployScript {
			get {
				return deployScript;
			}
			set {
				deployScript = value;
			}
		}
		
		public void ClearExcludedFiles()
		{
			excludeFiles.Clear();
		}
		
		public void AddExcludedFile(string fileName)
		{
			excludeFiles.Add(new ExcludeFile(fileName));
		}
		
		public void RemoveExcludedFile(string fileName)
		{
			foreach (ExcludeFile excludedFile in ExcludeFiles) {
				if (excludedFile.FileName == fileName) {
					ExcludeFiles.Remove(excludedFile);
					RemoveExcludedFile(fileName);
					break;
				}
			}
		}
		
		public bool IsFileExcluded(string name)
		{
			foreach (ExcludeFile file in excludeFiles) {
				if (file.FileName == name) {
					return true;
				}
			}
			return false;
		}
		public DeployInformation()
		{
		}
		
		public static void Deploy(Project project)
		{
			switch (project.DeployInformation.DeploymentStrategy) {
				case DeploymentStrategy.File:
					new FileDeploy().DeployProject(project);
					break;
				case DeploymentStrategy.Script:
					new ScriptDeploy().DeployProject(project);
					break;
				case DeploymentStrategy.Assembly:
					new AssemblyDeploy().DeployProject(project);
					break;
			}
		}
	}
}
