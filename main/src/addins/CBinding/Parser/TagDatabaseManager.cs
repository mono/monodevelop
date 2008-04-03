//
// TagDatabaseManager.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
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
using System.Collections.Generic;
using System.Threading;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui;

using CBinding.Navigation;

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
					if (IdeApp.Services.PlatformService.Name.Equals ("OSX")) {
						return false;
					}
					try {
						Runtime.ProcessService.StartProcess ("ctags", "--version", null, null).WaitForOutput ();
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
				return ctagsInstalled;
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
			string option = (with_system ? "-M" : "-MM");
			ProcessWrapper p;
			try {
				p = Runtime.ProcessService.StartProcess ("gcc", option + " -MG " + filename, null, null);
				p.WaitForExit ();
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return new string [0];
			}
			
			StringBuilder output = new StringBuilder ();
			string line;
			
			while ((line = p.StandardOutput.ReadLine ()) != null)
				output.Append (line);
			
			p.Close ();
			
			string[] lines = output.ToString ().Split ('\\');
			List<string> headers = new List<string> ();
			
			for (int i = 0; i < lines.Length; i++) {
				string[] files = lines[i].Split (' ');
				// first line contains the rule (eg. file.o: dep1.c dep2.h ...) and we must skip it
				// and we skip the *.cpp or *.c etc. too
				for (int j = 0; j < files.Length; j++) {
					if (j == 0 || j == 1) continue;
					
					string depfile = files[j].Trim ();
					
					if (!string.IsNullOrEmpty (depfile))
						headers.Add (findFileInIncludes (project, depfile));
				}
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
			CProjectConfiguration conf = project.ActiveConfiguration as CProjectConfiguration;
			string fullpath = string.Empty;
			
			if (!Path.IsPathRooted (filename)) {
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
		
		private void UpdateSystemTags (Project project, string filename, string[] includedFiles)
		{
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			List<FileInformation> files;
			
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
					FillFileInformation (newFileInfo);
				}
				
				contains = false;
			}
		}
		
		private void FillFileInformation (FileInformation fileInfo)
		{
			if (!DepsInstalled)
				return;
			
			string confdir = PropertyService.ConfigPath;
			string tagFileName = Path.GetFileName (fileInfo.FileName) + ".tag";
			string tagdir = Path.Combine (confdir, "system-tags");
			string tagFullFileName = Path.Combine (tagdir, tagFileName);
			string ctags_kinds = "--C++-kinds=+u";
			
			if (PropertyService.Get<bool> ("CBinding.ParseLocalVariables", false))
				ctags_kinds += "+l";
			
			string ctags_options = ctags_kinds + " --fields=+a-f+S --language-force=C++ --excmd=pattern -f - '" + tagFullFileName + "' " + fileInfo.FileName;
			
			if (!Directory.Exists (tagdir))
				Directory.CreateDirectory (tagdir);
			
			if (!File.Exists (tagFullFileName) || File.GetLastWriteTimeUtc (tagFullFileName) < File.GetLastWriteTimeUtc (fileInfo.FileName)) {
				ProcessWrapper p = null;
				System.IO.StringWriter output = null;
				try {
					output = new System.IO.StringWriter ();
					
					p = Runtime.ProcessService.StartProcess ("ctags", ctags_options, null, output, output, null);
					p.WaitForOutput (10000);
					if (p.ExitCode != 0 || !File.Exists (tagFullFileName)) {
						LoggingService.LogError ("Ctags did not successfully populate the tags database '{0}' from '{1}' within ten seconds.\nOutput: {2}", tagFullFileName, output.ToString ());
						return;
					}
				} catch (Exception ex) {
					throw new IOException ("Could not create tags database (You must have exuberant ctags installed).", ex);
				} finally {
					if (output != null)
						output.Dispose ();
					if (p != null)
						p.Dispose ();
				}
			}
			
			string ctags_output;
			string tagEntry;
			
			using (StreamReader reader = new StreamReader (tagFullFileName)) {
				ctags_output = reader.ReadToEnd ();
			}
			
			using (StringReader reader = new StringReader (ctags_output)) {
				while ((tagEntry = reader.ReadLine ()) != null) {
					if (tagEntry.StartsWith ("!_")) continue;
					
					Tag tag = ParseTag (tagEntry);
					
					if (tag != null)
						AddInfo (fileInfo, tag, ctags_output);
				}
			}
			
			fileInfo.IsFilled = true;
		}
		
		private void ParsingThread ()
		{
			try {
				while (parsingJobs.Count > 0) {
					ProjectFilePair p;
					
					lock (parsingJobs) {
						p = parsingJobs.Dequeue ();
					}
					
					DoUpdateFileTags (p.Project, p.File);
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
				parsingThread.IsBackground = true;
				parsingThread.Priority = ThreadPriority.Lowest;
				parsingThread.Start();
			}
		}
		
		private void DoUpdateFileTags (Project project, string filename)
		{
			if (!DepsInstalled)
				return;

			string[] headers = Headers (project, filename, false);
			string ctags_kinds = "--C++-kinds=+u";
			
			if (PropertyService.Get<bool> ("CBinding.ParseLocalVariables", true))
				ctags_kinds += "+l";
			
			string ctags_options = ctags_kinds + " --fields=+a-f+S --language-force=C++ --excmd=pattern -f - '" + filename + "' " + string.Join (" ", headers);
			
			string[] system_headers = diff (Headers (project, filename, true), headers);
			
			ProcessWrapper p = null;
			System.IO.StringWriter output = null, error = null;
			string ctags_output = string.Empty;
			try {
				output = new System.IO.StringWriter ();
				error = new System.IO.StringWriter ();
				p = Runtime.ProcessService.StartProcess ("ctags", ctags_options, null, output, error, null);
				p.WaitForOutput (10000);
				if (p.ExitCode != 0) {
					LoggingService.LogError ("Ctags did not successfully populate the tags database from '{0}' within ten seconds.\nError output: {1}", filename, error.ToString ());
					return;
				}
				ctags_output = output.ToString ();
			} catch (Exception ex) {
				throw new IOException ("Could not create tags database (You must have exuberant ctags installed).", ex);
			} finally {
				if (output != null)
					output.Dispose ();
				if (error != null)
					error.Dispose ();
				if (p != null)
					p.Dispose ();
			}
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			string tagEntry;

			using (StringReader reader = new StringReader (ctags_output)) {
				while ((tagEntry = reader.ReadLine ()) != null) {
					if (tagEntry.StartsWith ("!_")) continue;
					
					Tag tag = ParseTag (tagEntry);
					
					if (tag != null)
						AddInfo (info, tag, ctags_output);
				}
			}			
			
			if (FileUpdated != null)
				FileUpdated (new ClassPadEventArgs (project));
			
			if (PropertyService.Get<bool> ("CBinding.ParseSystemTags", true))
				UpdateSystemTags (project, filename, system_headers);
			
			if (cache.Count > cache_size)
				cache.Clear ();
		}
		
		private void AddInfo (FileInformation info, Tag tag, string ctags_output)
		{
			switch (tag.Kind)
			{
			case TagKind.Class:
				Class c = new Class (tag, info.Project, ctags_output);
				if (!info.Classes.Contains (c))
					info.Classes.Add (c);
				break;
			case TagKind.Enumeration:
				Enumeration e = new Enumeration (tag, info.Project, ctags_output);
				if (!info.Enumerations.Contains (e))
					info.Enumerations.Add (e);
				break;
			case TagKind.Enumerator:
				Enumerator en= new Enumerator (tag, info.Project, ctags_output);
				if (!info.Enumerators.Contains (en))
					info.Enumerators.Add (en);
				break;
			case TagKind.ExternalVariable:
				break;
			case TagKind.Function:
				Function f = new Function (tag, info.Project, ctags_output);
				if (!info.Functions.Contains (f))
					info.Functions.Add (f);
				break;
			case TagKind.Local:
				Local lo = new Local (tag, info.Project, ctags_output);
				if(!info.Locals.Contains (lo))
					info.Locals.Add (lo);
				break;
			case TagKind.Macro:
				Macro m = new Macro (tag, info.Project);
				if (!info.Macros.Contains (m))
					info.Macros.Add (m);
				break;
			case TagKind.Member:
				Member me = new Member (tag, info.Project, ctags_output);
				if (!info.Members.Contains (me))
					info.Members.Add (me);
				break;
			case TagKind.Namespace:
				Namespace n = new Namespace (tag, info.Project, ctags_output);
				if (!info.Namespaces.Contains (n))
					info.Namespaces.Add (n);
				break;
			case TagKind.Prototype:
				Function fu = new Function (tag, info.Project, ctags_output);
				if (!info.Functions.Contains (fu))
					info.Functions.Add (fu);
				break;
			case TagKind.Structure:
				Structure s = new Structure (tag, info.Project, ctags_output);
				if (!info.Structures.Contains (s))
					info.Structures.Add (s);
				break;
			case TagKind.Typedef:
				Typedef t = new Typedef (tag, info.Project, ctags_output);
				if (!info.Typedefs.Contains (t))
					info.Typedefs.Add (t);
				break;
			case TagKind.Union:
				Union u = new Union (tag, info.Project, ctags_output);
				if (!info.Unions.Contains (u))
					info.Unions.Add (u);
				break;
			case TagKind.Variable:
				Variable v = new Variable (tag, info.Project);
				if (!info.Variables.Contains (v))
					info.Variables.Add (v);
				break;
			default:
				break;
			}
		}
		
		private Tag ParseTag (string tagEntry)
		{
			int i1, i2;
			string file;
			string pattern;
			string name;
			string tagField;
			TagKind kind;
			AccessModifier access = AccessModifier.Public;
			string _class = null;
			string _namespace = null;
			string _struct = null;
			string _union = null;
			string _enum = null;
			string signature = null;
			char delimiter;
			
			name = tagEntry.Substring (0, tagEntry.IndexOf ('\t'));
			
			i1 = tagEntry.IndexOf ('\t') + 1;
			i2 = tagEntry.IndexOf ('\t', i1);
			
			file = tagEntry.Substring (i1, i2 - i1);
			
			delimiter = tagEntry[i2 + 1];
			
			i1 = i2 + 2;
			i2 = tagEntry.IndexOf (delimiter, i1) - 1;
			
			// apparentlty sometimes ctags will create faulty tags, make sure this is not one of them
			if (i2 < 0 || i1 < 0)
				return null;
			
			pattern = tagEntry.Substring (i1 + 1, i2 - i1 - 1);
			
			tagField = tagEntry.Substring (i2 + 5);
			
			// parse tag field
			kind = (TagKind)tagField[0];
			
			string[] fields = tagField.Split ('\t');
			int index;
			
			foreach (string field in fields) {
				index = field.IndexOf (':');
				
				// TODO: Support friend modifier
				if (index > 0) {
					string key = field.Substring (0, index);
					string val = field.Substring (index + 1);
					
					switch (key) {
					case "access":
						try {
							access = (AccessModifier)System.Enum.Parse (typeof(AccessModifier), val, true);
						} catch (ArgumentException) {
						}
						break;
					case "class":
						_class = val;
						break;
					case "namespace":
						_namespace = val;
						break;
					case "struct":
						_struct = val;
						break;
					case "union":
						_union = val;
						break;
					case "enum":
						_enum = val;
						break;
					case "signature":
						signature = val;
						break;
					}
				}
			}
			
			return new Tag (name, file, pattern, kind, access, _class, _namespace, _struct, _union, _enum, signature);
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
						Tag tag = ParseTag (entry);
						
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
		
		private static string[] diff (string[] a1, string[] a2)
		{
			List<string> res = new List<string> ();
			List<string> right = new List<string> (a2);
			
			foreach (string s in a1) {
				if (!right.Contains (s))
					res.Add (s);
			}
			
			return res.ToArray ();
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
