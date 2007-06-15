//
// CombineToSolutionConverter.cs
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using MonoDevelop.Ide.Projects;

namespace MonoDevelop.Projects.Converter
{
	public class CombineToSolutionConverter : IConverter 
	{
		public bool CanLoad (string sourceFile)
		{
			return Path.GetExtension (sourceFile) == ".mds";
		}
	
		public Solution Read (string sourceFile)
		{
			Solution result = new Solution (Path.ChangeExtension (sourceFile, ".sln"));
			List<ProjectReference> references = new List<ProjectReference> ();
			Folder rootFolder = new Folder ("root");
			ReadSolution (result, rootFolder, sourceFile, references);
			
			foreach (Folder folder in rootFolder.Folders[0].Folders) {
				folder.SolutionFolder.Parent = null;
			}
			foreach (Folder folder in rootFolder.Folders[0].AllFolders)
				result.AddItem (folder.SolutionFolder);
			
			foreach (ProjectReference reference in references) {
				SolutionProject project = result.FindProject (reference.ProjectName);
				if (project != null) {
					string relativePath = MonoDevelop.Core.Runtime.FileService.AbsoluteToRelativePath (Path.GetDirectoryName (reference.Project.FileName), project.Project.FileName);
					reference.Project.Add (new ProjectReferenceProjectItem (ConvertPath (relativePath), 
					                                                        project.Guid, 
					                                                        project.Name));
				}
			}
			SolutionSection configSection = new SolutionSection ("SolutionConfigurationPlatforms", SolutionSection.PreSolution);
			configSection.Add ("Debug|Any CPU", "Debug|Any CPU");
			configSection.Add ("Release|Any CPU", "Release|Any CPU");
			result.AddSection (configSection);
			
			SolutionSection projectConfigSection = new SolutionSection ("ProjectConfigurationPlatforms", SolutionSection.PostSolution);
			foreach (IProject project in result.AllProjects) {
				foreach (string config in new string[] {"Debug|Any CPU", "Release|Any CPU" }) {
					projectConfigSection.Add (project.Guid + "." + config + ".Build.0", config);
					projectConfigSection.Add (project.Guid + "." + config + ".ActiveCfg", config);
				}
			}
			result.AddSection (projectConfigSection);
			
			SolutionSection nestedProjectsSection = new SolutionSection ("NestedProjects", SolutionSection.PreSolution);
			foreach (Folder folder in rootFolder.AllFolders) {
				foreach (SolutionProject project in folder.Projects) {
					folder.SolutionFolder.AddChild (project);
					nestedProjectsSection.Add (project.Guid, folder.SolutionFolder.Guid);
				}
			}
			
			result.AddSection (nestedProjectsSection);
			
			// save all stuff (test wise)
			result.Save ();
			foreach (IProject project in result.AllProjects) {
				project.Save ();
			}
			
			return result;
		}
		static Regex namePattern      = new Regex("<Combine\\s*name=\\s*\"(?<Name>^[\"]*)\"\\s", RegexOptions.Compiled);
		static Regex entryLinePattern = new Regex("<Entry\\s*filename=\\s*\"(?<File>.*)\"\\s*/>", RegexOptions.Compiled);
		
		class Folder 
		{
			string name;
			SolutionFolder folder;
			List<Folder>  folders = new List<Folder> (); 
			List<SolutionProject> projects = new List<SolutionProject> ();
			
			public string Name {
				get { return name; }
				set { name = value; }
			}
			
			public SolutionFolder SolutionFolder {
				get { return folder; }
			}
			
			public List<Folder> Folders {
				get { return folders; }
			}
			
			public List<SolutionProject> Projects {
				get { return projects; }
			}
			
			public IEnumerable<Folder> AllFolders {
				get {
					Stack<Folder> folders = new Stack<Folder> ();
					foreach (Folder folder in Folders)
						folders.Push (folder);
					while (folders.Count > 0) {
						Folder folder = folders.Pop ();
						yield return folder;
						foreach (Folder childFolder in folder.Folders)
							folders.Push (childFolder);
					}
				}
			}
		
			public Folder (string name)
			{
				this.name   = name;
				this.folder = new SolutionFolder (GenerateGuid (), name, name);
			}
			
			public Folder GetFolder (string name)
			{
				foreach (Folder folder in folders)
					if (folder.Name == name)
						return folder;
				Console.WriteLine ("Create Folder:" + name);
				Folder result = new Folder (name);
				folders.Add (result);
				this.SolutionFolder.AddChild (result.SolutionFolder);
				return result;
			}
		}
		
		void ReadSolution (Solution solution, Folder parentFolder, string sourceFile, List<ProjectReference> references)
		{
			string sourcePath = Path.GetDirectoryName (sourceFile);
			string content = File.ReadAllText (sourceFile);
			string name = Path.GetFileNameWithoutExtension (sourceFile);
			Match nameMatch = namePattern.Match (content);
			if (nameMatch.Success) 
				name = nameMatch.Result ("${Name}");
			Folder curFolder = parentFolder.GetFolder (name);
			
			foreach (Match match in entryLinePattern.Matches (content)) {
				string fileName = Path.GetFullPath (Path.Combine (sourcePath, match.Result("${File}")));
				if (Path.GetExtension (fileName) == ".mds") {
					ReadSolution (solution, curFolder, fileName, references);
				} else if (Path.GetExtension (fileName) == ".mdp") {
					SolutionProject project = ReadProject (solution, fileName, references);
					if (curFolder != null)
						curFolder.Projects.Add (project);
					solution.AddItem (project);
				}
			}
		}
		
		string GetGUID (string language)
		{
			switch (language) {
			case "C#":
				return "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
			}
			return "{00000000-0000-0000-0000-000000000000}";
		}
		
		string GetExtension (string language)
		{
			switch (language) {
			case "C#":
				return ".csproj";
			}
			return ".unknown";
		}
		FileType GetFileType (string buildaction)
		{
			switch (buildaction) {
			case "Nothing":
				return FileType.None;
			case "Compile":
				return FileType.Compile;
			case "EmbedAsResource":
				return FileType.EmbeddedResource;
			case "FileCopy":
				return FileType.None;
				
			}
			return FileType.None;
		}
		
		static string ConvertPath (string path)
		{
			if (path == null)
				return null;
			string result = path.Replace ('/', '\\');
			if (result.StartsWith (".\\"))
				result = result.Substring (2);
			return result;
		}
		
		class ProjectReference {
			IProject project;
			string   projectName;
			
			public IProject Project {
				get { return project; }
			}
			public string ProjectName {
				get { return projectName; }
			}
			
			public ProjectReference (IProject project, string projectName)
			{
				this.project     = project;
				this.projectName = projectName;
			}
		}
		
		static string GenerateGuid ()
		{
			return "{" + Guid.NewGuid ().ToString ().ToUpper () + "}";
		}
		
		SolutionProject ReadProject (Solution solution, string sourceFile, List<ProjectReference> references)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (sourceFile);
			string version  = doc.DocumentElement.GetAttribute ("fileversion");
			string language = doc.DocumentElement.GetAttribute ("language");
			string absoluteLocation = Path.GetFullPath (Path.ChangeExtension (sourceFile, GetExtension (language)));
			string relativeLocation = absoluteLocation.Substring (Path.GetDirectoryName (solution.FileName).Length + 1);
			string projectGuid = GenerateGuid ();
			SolutionProject result = new SolutionProject (GetGUID (language), 
			                                              projectGuid, 
			                                              Path.GetFileNameWithoutExtension (relativeLocation),
			                                              relativeLocation);
			MSBuildProject project = new MSBuildProject (language, absoluteLocation);
			project.Guid          = projectGuid;
			project.Configuration = "Debug";
			project.Platform      = "AnyCpu";
			XmlElement debug = doc.DocumentElement.SelectSingleNode (@"/Project/Configurations/Configuration") as XmlElement;
			if (debug != null) {
				project.OutputPath   = ConvertPath (debug["Output"].GetAttribute ("directory"));
				project.AssemblyName = debug["Output"].GetAttribute ("assembly");
				project.OutputType   = debug["Build"].GetAttribute ("target");
				
				if (debug["CodeGeneration"].HasAttribute ("definesymbols"))
					project.DefineConstants = debug["CodeGeneration"].GetAttribute ("definesymbols");
				if (debug["CodeGeneration"].HasAttribute ("optimize"))
					project.SetProperty ("Optimize", debug["CodeGeneration"].GetAttribute ("optimize"));
				if (debug["CodeGeneration"].HasAttribute ("unsafecodeallowed"))
					project.SetProperty ("AllowUnsafeBlocks", debug["CodeGeneration"].GetAttribute ("unsafecodeallowed"));
				if (debug["CodeGeneration"].HasAttribute ("generateoverflowchecks"))
					project.SetProperty ("CheckForOverflowUnderflow", debug["CodeGeneration"].GetAttribute ("generateoverflowchecks"));
			} else {
				project.OutputPath   = "bin\\debug";
				project.AssemblyName = "a";
				project.OutputType   = "Exe";
			}
			
			XmlNode contents = doc.DocumentElement.SelectSingleNode ("/Project/Contents");
			if (contents != null) {
				foreach (XmlNode node in contents.ChildNodes) {
					XmlElement el = node as XmlElement;
					if (el == null)
						continue;
					string name        = ConvertPath (el.GetAttribute ("name"));
					string buildaction = el.GetAttribute ("buildaction");
					if (buildaction == "Exclude")
						continue;
					ProjectFile newFile = new ProjectFile (name, GetFileType (buildaction));
					if (buildaction == "FileCopy")
						newFile.CopyToOutputDirectory = bool.TrueString;
					project.Add (newFile);
				}
			}
			
			XmlNode reference = doc.DocumentElement.SelectSingleNode ("/Project/References");
			if (reference != null) {
				foreach (XmlNode node in reference.ChildNodes) {
					XmlElement el = node as XmlElement;
					if (el == null)
						continue;
					string type  = el.GetAttribute ("type");
					string refto = el.GetAttribute ("refto");
					switch (type) {
					case "Assembly":
						project.Add (new ReferenceProjectItem (Path.GetFileNameWithoutExtension (refto), ConvertPath (refto)));
						break;
					case "Project":
						references.Add (new ProjectReference (project, refto));
						break;
					case "Gac":
						string gacReference = refto;
						ReferenceProjectItem newReference = new ReferenceProjectItem (refto);
						if (gacReference.IndexOf (",") > 0) {
							newReference.Name            = gacReference.Substring (0, gacReference.IndexOf (","));
							newReference.SpecificVersion = true;
						}
						project.Add (newReference);
						break;
					}
				}
			}
			
			result.Project = project;
			return result;
		}
	}
}
