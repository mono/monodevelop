//
// TagDatabaseManager.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//   Mitchell Wheeler <mitchell.wheeler@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
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
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui;

using CBinding.Navigation;

//  TODO
//  Generic, language independant 'TagDatabaseManager'
//  Parsing of ctags data into a Sqlite database, for easy/efficient access & updates.
//  
namespace CBinding.Parser
{
	/// <summary>
	/// Singleton class to manage tag databases
	/// </summary>
	class TagDatabaseManager
	{
		private static TagDatabaseManager instance;
		private Queue<ProjectFilePair> parsingJobs = new Queue<ProjectFilePair> ();
		private Thread parsingThread;
		private CTagsManager ctags;
		
		public event ClassPadEventHandler FileUpdated;
		
		bool ctagsInstalled = false;
		bool checkedCtagsInstalled = false;
		
		private TagDatabaseManager()
		{
		}
		
		public static TagDatabaseManager Instance
		{
			get {
				if (instance == null)
					instance = new TagDatabaseManager ();
				
				return instance;
			}
		}
		
		bool DepsInstalled {
			get {
				if (!checkedCtagsInstalled) {
					checkedCtagsInstalled = true;
					/*
					if (PropertyService.IsMac) {
						return false;
					}
					*/
					try {
						var output = new StringWriter ();
						Runtime.ProcessService.StartProcess ("ctags", "--version", null, output, null, null).WaitForExit ();
						if (PropertyService.IsMac && !output.ToString ().StartsWith ("Exuberant", StringComparison.Ordinal)) {
							System.Console.WriteLine ("Fallback to OSX ctags");
						}
						ctags = new ExuberantCTagsManager ();
					} catch {
						LoggingService.LogWarning ("Cannot update C/C++ tags database because exuberant ctags is not installed.");
						return false;
					}
					try {
						Runtime.ProcessService.StartProcess ("gcc", "--version", null, null).WaitForOutput ();
					} catch {
						LoggingService.LogWarning ("Cannot update C/C++ tags database because gcc is not installed.");
						return false;
					}
					lock (parsingJobs) {
						ctagsInstalled = true;
					}
				}
				return ctagsInstalled && ctags != null;
			}
			set {
				//don't assume that the caller is correct :-)
				if (value)
					checkedCtagsInstalled = false; //wil re-determine ctagsInstalled on next getting
				else
					ctagsInstalled = false;
			}
		}
		
		private string[] Headers (Project project, string filename, bool with_system)
		{
			List<string> headers = new List<string> ();
			CProject cproject = project as CProject;
			if (cproject == null){ return headers.ToArray(); }
			
			StringBuilder output = new StringBuilder ();
			StringBuilder option = new StringBuilder ("-M");
			if (!with_system) {
				option.Append("M");
			}
			
			option.Append (" -MG ");
			foreach (Package package in cproject.Packages) {
				package.ParsePackage ();
				option.AppendFormat ("{0} ", string.Join(" ", package.CFlags.ToArray ()));
			}
			
			ProcessWrapper p = null;
			try {
				p = Runtime.ProcessService.StartProcess ("gcc", option.ToString () + filename.Replace(@"\ ", " ").Replace(" ", @"\ "), null, null);
				p.WaitForOutput ();

				// Doing the below completely breaks header parsing
				// // Skip first two lines (.o & .c* files) - WARNING, sometimes this is compacted to 1 line... we need a better way of handling this.
				// if(p.StandardOutput.ReadLine () == null) return new string[0]; // object file
				// if(p.StandardOutput.ReadLine () == null) return new string[0]; // compile file

				string line;
				while ((line = p.StandardOutput.ReadLine ()) != null)
					output.Append (line);
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return new string [0];
			}
			finally {
				if(p != null)
					p.Dispose();
			}
			
			MatchCollection files = Regex.Matches(output.ToString().Replace(@" \", String.Empty), @" (?<file>([^ \\]|(\\ ))+)", RegexOptions.IgnoreCase);

			foreach (Match match in files) {
				string depfile = findFileInIncludes(project, match.Groups["file"].Value.Trim());
				
				headers.Add (depfile.Replace(@"\ ", " ").Replace(" ", @"\ "));
			}
			
			return headers.ToArray ();
		}
		
		/// <summary>
		/// Finds a file in a project's include path(s)
		/// </summary>
		/// <param name="project">
		/// The project whose include path is to be searched
		/// <see cref="Project"/>
		/// </param>
		/// <param name="filename">
		/// A portion of a full file path
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// The full found path, or filename if not found
		/// <see cref="System.String"/>
		/// </returns>
		private static string findFileInIncludes (Project project, string filename) {
			CProjectConfiguration conf = project.DefaultConfiguration as CProjectConfiguration;
			string fullpath = string.Empty;
			
			if (!Path.IsPathRooted (filename)) {
				// Check against base directory
				fullpath = findFileInPath (filename, project.BaseDirectory);
				if (string.Empty != fullpath) return fullpath;

				// Check project's additional configuration includes
				foreach (string p in conf.Includes) {
					fullpath = findFileInPath (filename, p);
					if (string.Empty != fullpath) return fullpath;
				}
			}
			
			return filename;
		}
		
		/// <summary>
		/// Finds a file in a subdirectory of a given path
		/// </summary>
		/// <param name="relativeFilename">
		/// A portion of a full file path
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="path">
		/// The path beneath which to look for relativeFilename
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// The full path, or string.Empty if not found
		/// <see cref="System.String"/>
		/// </returns>
		private static string findFileInPath (string relativeFilename, string path) {
			string tmp = Path.Combine (path, relativeFilename);
			
			if (Path.IsPathRooted (relativeFilename))
				return relativeFilename;
			else if (File.Exists (tmp))
				return tmp;
			
			if (Directory.Exists (path)) {
				foreach (string subdir in Directory.GetDirectories (path)) {
					tmp = findFileInPath (relativeFilename, subdir);
					if (string.Empty != tmp) return tmp;
				}
			}
			
			return string.Empty;
		}
		
		private void UpdateSystemTags (Project project, string filename, IEnumerable<string> includedFiles)
		{
			if (!DepsInstalled)
				return;
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			List<FileInformation> files;
			
			lock (info) {
				if (!info.IncludedFiles.ContainsKey (filename)) {
					files = new List<FileInformation> ();
					info.IncludedFiles.Add (filename, files);
				} else {
					files = info.IncludedFiles[filename];
				}
				
				foreach (string includedFile in includedFiles) {
					bool contains = false;
					
					foreach (FileInformation fi in files) {
						if (fi.FileName == includedFile) {
							contains = true;
						}
					}
					
					if (!contains) {
						FileInformation newFileInfo = new FileInformation (project, includedFile);
						files.Add (newFileInfo);
						ctags.FillFileInformation (newFileInfo);
					}
					
					contains = false;
				}
			}
		}
		
		private void ParsingThread ()
		{
			try {
				while (parsingJobs.Count > 0) {
					ProjectFilePair p;
					
					lock (parsingJobs) {
						p = parsingJobs.Dequeue ();
					}
					
					string[] headers = Headers (p.Project, p.File, false);
					ctags.DoUpdateFileTags (p.Project, p.File, headers);
					OnFileUpdated (new ClassPadEventArgs (p.Project));
					
					if (PropertyService.Get<bool> ("CBinding.ParseSystemTags", true))
						UpdateSystemTags (p.Project, p.File, Headers (p.Project, p.File, true).Except (headers));
					
					if (cache.Count > cache_size)
						cache.Clear ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error updating parser database. Disabling C/C++ parsing.", ex);
				DepsInstalled = false;
				return;
			}
		}
		
		public void UpdateFileTags (Project project, string filename)
		{
			if (!DepsInstalled)
				return;
			
			ProjectFilePair p = new ProjectFilePair (project, filename);
			
			lock (parsingJobs) {
				if (!parsingJobs.Contains (p))
					parsingJobs.Enqueue (p);
			}
			
			if (parsingThread == null || !parsingThread.IsAlive) {
				parsingThread = new Thread (ParsingThread);
				parsingThread.Name = "Tag database parser";
				parsingThread.IsBackground = true;
				parsingThread.Priority = ThreadPriority.Lowest;
				parsingThread.Start();
			}
		}
		
		Tag BinarySearch (string[] ctags_lines, TagKind kind, string name)
		{
			int low;
			int high = ctags_lines.Length - 2; // last element is an empty string (because of the Split)
			int mid;
			int start_index = 0;
			
			// Skip initial comment lines
			while (ctags_lines[start_index].StartsWith ("!_"))
				start_index++;

			low = start_index;
			
			while (low <= high) {
				mid = (low + high) / 2;
				string entry = ctags_lines[mid];
				string tag_name = entry.Substring (0, entry.IndexOf ('\t'));
				int res = string.CompareOrdinal (tag_name, name);
				
				if (res < 0) {
					low = mid + 1;
				} else if (res > 0) {
					high = mid - 1;
				} else {
					// The tag we are at has the same name than the one we are looking for
					// but not necessarily the same type, the actual tag we are looking
					// for might be higher up or down, so we try both, starting with going down.
					int save = mid;
					bool going_down = true;
					bool eof = false;
					
					while (true) {
						Tag tag = ctags.ParseTag (entry);
						
						if (tag == null)
							return null;
						
						if (tag.Kind == kind && tag_name == name)
							return tag;
						
						if (going_down) {
							mid++;
							
							if (mid >= ctags_lines.Length - 1)
								eof = true;
							
							if (!eof) {
								entry = ctags_lines[mid];
								tag_name = entry.Substring (0, entry.IndexOf ('\t'));
								
								if (tag_name != name) {
									going_down = false;
									mid = save - 1;
								}
							} else {
								going_down = false;
								mid = save - 1;
							}
						} else { // going up
							mid--;

							if (mid < start_index)
								return null;
							
							entry = ctags_lines[mid];
							tag_name = entry.Substring (0, entry.IndexOf ('\t'));
							
							if (tag_name != name)
								return null;
						}
					}
				}
			}
			
			return null;
		}
		
		private struct SemiTag
		{
			readonly internal string name;
			readonly internal TagKind kind;
			
			internal SemiTag (string name, TagKind kind)
			{
				this.name = name;
				this.kind = kind;
			}
			
			public override int GetHashCode ()
			{
				return (name + kind.ToString ()).GetHashCode ();
			}
		}
		
		private const int cache_size = 10000;
		private Dictionary<SemiTag, Tag> cache = new Dictionary<SemiTag, Tag> ();
		
		public Tag FindTag (string name, TagKind kind, string ctags_output)
		{
			SemiTag semiTag = new SemiTag (name, kind);
			
			if (cache.ContainsKey (semiTag))
				return cache[semiTag];
			else {
				string[] ctags_lines = ctags_output.Split ('\n');
				Tag tag = BinarySearch (ctags_lines, kind, name);
				cache.Add (semiTag, tag);
				
				return tag;
			}
		}
		
		/// <summary>
		/// Remove a file's parse information from the database.
		/// </summary>
		/// <param name="project">
		/// A <see cref="Project"/>: The project to which the file belongs.
		/// </param>
		/// <param name="filename">
		/// A <see cref="System.String"/>: The file.
		/// </param>
		public void RemoveFileInfo(Project project, string filename)
		{
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			lock (info){ info.RemoveFileInfo(filename); }
			OnFileUpdated(new ClassPadEventArgs (project));
		}
		
		/// <summary>
		/// Wrapper method for the FileUpdated event.
		/// </summary>
		void OnFileUpdated (ClassPadEventArgs args)
		{
			if (null != FileUpdated){ FileUpdated(args); }
		}
		
		private class ProjectFilePair
		{
			string file;
			Project project;
			
			public ProjectFilePair (Project project, string file)
			{
				this.project = project;
				this.file = file;
			}
			
			public string File {
				get { return file; }
			}
			
			public Project Project {
				get { return project; }
			}
			
			public override bool Equals (object other)
			{
				ProjectFilePair o = other as ProjectFilePair;
				
				if (o == null)
					return false;
				
				if (file == o.File && project == o.Project)
					return true;
				else
					return false;
			}
			
			public override int GetHashCode ()
			{
				return (project.ToString() + file).GetHashCode ();
			}
		}
	}
}
