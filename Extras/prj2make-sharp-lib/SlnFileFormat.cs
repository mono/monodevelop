//
// SlnFileFormat.cs
//
// Author:
//   Ankit Jain
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Prj2Make
{
	internal class SlnFileFormat: IFileFormat
	{
		static Guid folderGuid = new Guid ("2150E333-8FDC-42A3-9474-1A3956D46DE8");

		public string Name {
			get { return "Visual Studio .NET 2005 Solution"; }
		}
		
		public string GetValidFormatName (string fileName)
		{
			return Path.ChangeExtension (fileName, ".sln");
		}
		
		public bool CanReadFile (string file)
		{
			return String.Compare (Path.GetExtension (file), ".sln", true) == 0;
		}
		
		public bool CanWriteFile (object obj)
		{
			return obj is Combine;
		}
		
		public void WriteFile (string file, object obj, IProgressMonitor monitor)
		{
			//FIXME:
		}
		
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			Combine combine = null;
			if (fileName == null || monitor == null)
				return null;
			
			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading solution: {0}"), fileName), 1);
				combine = ParseSln (fileName, monitor);
			} catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not load solution: {0}"), fileName), ex);
				throw;
			} finally {
				monitor.EndTask ();
			}
			
			return combine;
		}

		//FIXME: Move to seperate class
		Combine ParseSln (string fileName, IProgressMonitor monitor)
		{
			//string version = GetSlnFileVersion (fileName);
			//if (version != "9.00")
			//	throw new UnknownProjectVersionException (fileName, version);

			StreamReader reader = new StreamReader(fileName);
			Regex regex = new Regex(@"Project\(""(\{.*\})""\) = ""(.*)"", ""(.*)"", ""(\{.*\})""");
    
			Combine combine = new Combine ();
			combine.FileName = fileName;
			combine.FileFormat = new SlnFileFormat ();

			Dictionary<string, DotNetProject> projects = new Dictionary<string, DotNetProject> ();
			Dictionary<string, Combine> folders = null;
			string s;
			while (true)
			{
				s = reader.ReadLine();
    				if (s.StartsWith("Global"))
					break;

				Match match = regex.Match(s);
				if (!match.Success)
					continue;

				Guid projectTypeGuid = new Guid (match.Groups [1].Value);
				string projectName = match.Groups[2].Value;
				string projPath = match.Groups[3].Value;
				string projectGuid = match.Groups[4].Value;

				if (projectTypeGuid == folderGuid) {
					//Solution folder
					Combine folder = new Combine ();
					folder.Name = projectName;
					folder.FileName = projPath;
					folder.FileFormat = new SlnFileFormat ();

					folder.ExtendedProperties ["guid"] = projectGuid;
					folder.ExtendedProperties ["folder"] = true;

					if (folders == null)
						folders = new Dictionary<string, Combine> ();
					folders [projectGuid] = folder;

					continue;
				}

				if (!projPath.StartsWith("http://") &&
					(projPath.EndsWith (".csproj") || projPath.EndsWith (".vbproj")))
				{
					projPath = MapPath (Path.GetDirectoryName (fileName), projPath);
					try {
						DotNetProject project = Services.ProjectService.ReadFile (projPath, monitor) as DotNetProject;
						project.Name = projectName;
						projects [projectGuid] = project;
					} catch {
						//FIXME: monitor.ReportWarning 
						Console.WriteLine ("Failed to load project file '{0}'", projPath);
						continue;
					}
				}
    
			}

			//FIXME: Project can have ProjectDependencies

			if (String.Compare (s.Trim (), "Global", true) == 0) {
				ListDictionary dict = ParseGlobal (reader);

				if (dict.Contains ("NestedProjects") && folders != null)
					//"NestedProjects" section was found and 
					//folders are defined
					LoadNestedProjects (dict ["NestedProjects"] as List<string>,
						folders, projects);

				//Add the folders to the main combine
				if (folders != null)
					foreach (Combine f in folders.Values)
						combine.Entries.Add (f);

				//Add the projects to the main combine
				foreach (DotNetProject p in projects.Values)
					combine.Entries.Add (p);

				//FIXME: This can be just SolutionConfiguration also!
				if (dict.Contains ("SolutionConfigurationPlatforms"))
					LoadConfigurations (dict ["SolutionConfigurationPlatforms"] as List<string>,
						combine);
			}

			return combine;
		}

		ListDictionary ParseGlobal (StreamReader reader)
		{
			Regex regex = new Regex (@"GlobalSection\s*\(([^)]*)\)\s*=\s*(\w*)");
			ListDictionary dict = new ListDictionary ();

			//Process GlobalSection-s
			while (!reader.EndOfStream) {
				List<string> list = new List<string> ();

				string s = reader.ReadLine ().Trim ();

				Match m = regex.Match (s);
				if (!m.Success)
					continue;

				string key = m.Groups [1].Value;
				string val = m.Groups [2].Value;

				dict [key] = list;

				//First entry in the list is the val 'preSolution' like below
				//GlobalSection(SolutionConfigurationPlatforms) = preSolution
				list.Add (val);

				while (!reader.EndOfStream) {
					s = reader.ReadLine ().Trim ();
					if (String.Compare (s, "EndGlobalSection", true) == 0)
						break;
					if (s.Length == 0)
						continue;

					//Not splitting the key/value here
					list.Add (s);
				}
			}

			return dict;
		}

		void LoadConfigurations (List<string> list, Combine combine)
		{
			if (list == null || list.Count <= 1 ||
				String.Compare (list [0], "preSolution", true) != 0) 
				return;

			for (int i = 1; i < list.Count; i ++) {
				//FIXME: expects both key and val to be on the same line
				KeyValuePair<string, string> pair = SplitKeyValue (list [i]);

				//key : Debug|Any CPU
				string [] key_parts = pair.Key.Split (new char [] {'|'}, 2);
				if (key_parts.Length == 0)
					continue;

				CombineConfiguration config = (CombineConfiguration) 
					combine.CreateConfiguration (key_parts [0]);
				
				//Add all the projects to the configurations
				foreach (CombineEntry entry in combine.Entries)
					config.AddEntry (entry);
			}
		}

		void LoadNestedProjects (List<string> list, 
			Dictionary<string, Combine> folders, Dictionary<string, DotNetProject> projects)
		{
			if (list == null || list.Count <= 1 ||
				String.Compare (list [0], "preSolution", true) != 0) 
				return;

			for (int i = 1; i < list.Count; i ++) {
				KeyValuePair<string, string> pair = SplitKeyValue (list [i]);
				if (!folders.ContainsKey (pair.Value))
					//Folder not found
					//FIXME: Warning? 
					continue;
				if (!projects.ContainsKey (pair.Key))
					//Project not found
					//FIXME: Warning?
					continue;

				Combine folder = folders [pair.Value];
				DotNetProject proj = projects [pair.Key];

				folder.Entries.Add (proj);
				
				//Remove the project from the dictionary
				projects.Remove (pair.Key);
			}
		}

		KeyValuePair<string, string> SplitKeyValue (string s)
		{
			string [] pair = s.Split (new char [] {'='}, 2);
			string key = pair [0].Trim ();
			string val = String.Empty;
			if (pair.Length == 2)
				val = pair [1].Trim ();

			return new KeyValuePair<string, string> (key, val);
		}

		// Utility function to determine the sln file version
		string GetSlnFileVersion(string strInSlnFile)
		{
			string strVersion = null;
			string strInput = null;
			Match match;
			StreamReader reader = new StreamReader(strInSlnFile);
			Regex regex = new Regex(@"Microsoft Visual Studio Solution File, Format Version (\d.\d\d)");
			
			strInput = reader.ReadLine();

			match = regex.Match(strInput);
			if (match.Success)
			{
				strVersion = match.Groups[1].Value;
			}
			
			// Close the stream
			reader.Close();

			return strVersion;
		}

		//FIXME: Move this somewhere else?
		//is there an existing method for this?
		internal static string MapPath (string basePath, string relPath)
		{
			if (relPath == null || relPath.Length == 0)
				return null;
			
			string path = relPath.Replace ("\\", "/");
			if (char.IsLetter (path [0]) && path.Length > 1 && path[1] == ':')
				return null;
			
			if (basePath != null)
				path = Path.Combine (basePath, path);
				
			if (Path.IsPathRooted (path)) {
					
				// Windows paths are case-insensitive. When mapping an absolute path
				// we can try to find the correct case for the path.
				
				string[] names = path.Substring (1).Split ('/');
				string part = "/";
				
				for (int n=0; n<names.Length; n++) {
					string[] entries;
					if (n < names.Length - 1)
						entries = Directory.GetDirectories (part);
					else
						entries = Directory.GetFiles (part);
					
					string fpath = null;
					foreach (string e in entries) {
						if (string.Compare (Path.GetFileName (e), names[n], true) == 0) {
							fpath = e;
							break;
						}
					}
					if (fpath == null) {
						// Part of the path does not exist. Can't do any more checking.
						for (; n < names.Length; n++)
							part += "/" + names[n];
						return part;
					}
					
					part = fpath;
				}
				return part;
			} else
				return path;
		}


	}
}
