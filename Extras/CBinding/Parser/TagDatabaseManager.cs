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
	public class TagDatabaseManager
	{
		private static TagDatabaseManager instance;
		public event ClassPadEventHandler FileUpdated;
		
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
		
		public void WriteTags (Project project)
		{
			string tagsDir = Path.Combine (project.BaseDirectory, ".tags");
			
			if (!Directory.Exists (tagsDir))
				Directory.CreateDirectory (tagsDir);
			
			string ctags_options = "--C++-kinds=+p+u --fields=+a-f+S --language-force=C++ --excmd=pattern";
			
			StringBuilder args = new StringBuilder (ctags_options);
			
			foreach (ProjectFile file in project.ProjectFiles) 
				args.AppendFormat (" {0}", file.Name);
			
			try {
				ProcessWrapper p = Runtime.ProcessService.StartProcess ("ctags", args.ToString (), tagsDir, null);
				p.WaitForExit ();
			} catch (Exception ex) {
				throw new IOException ("Could not create tags database (You must have exuberant ctags installed).", ex);
			}
		}
		
		internal string[] Headers (string filename, bool with_system)
		{
			string option = (with_system ? "-M" : "-MM");
			ProcessWrapper p = Runtime.ProcessService.StartProcess ("gcc", option + " -MG " + filename, null, null);
			p.WaitForExit ();
			
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
						headers.Add (depfile);
				}
			}
			
			return headers.ToArray ();
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
			string confdir = PropertyService.ConfigPath;
			string tagFileName = Path.GetFileName (fileInfo.FileName) + ".tag";
			string tagdir = Path.Combine (confdir, "system-tags");
			string tagFullFileName = Path.Combine (tagdir, tagFileName);
			string ctags_options = "--C++-kinds=+p+u --fields=+a-f+S --language-force=C++ --excmd=pattern -f " + tagFullFileName + " " + fileInfo.FileName;
			
			if (!Directory.Exists (tagdir))
				Directory.CreateDirectory (tagdir);
			
			if (!File.Exists (tagFullFileName)) {
				ProcessWrapper p;
				
				try {
					p = Runtime.ProcessService.StartProcess ("ctags", ctags_options, null, null);
					p.WaitForExit ();
				} catch (Exception ex) {
					throw new IOException ("Could not create tags database (You must have exuberant ctags installed).", ex);
				}
				
				p.Close ();
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
					
					AddInfo (fileInfo, tag, ctags_output);
				}
			}
			
			fileInfo.IsFilled = true;
		}
		
		public void UpdateFileTags (Project project, string filename)
		{
			string[] headers = Headers (filename, false);
			string ctags_options = "--C++-kinds=+p+u --fields=+a-f+S --language-force=C++ --excmd=pattern -f - " + filename + " " + string.Join (" ", headers);
			
			string[] system_headers = diff (Headers (filename, true), headers);
			
			
			ProcessWrapper p;
			
			try {
				p = Runtime.ProcessService.StartProcess ("ctags", ctags_options, null, null);
				p.WaitForExit ();
			} catch (Exception ex) {
				throw new IOException ("Could not create tags database (You must have exuberant ctags installed).", ex);
			}
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			string tagEntry;
			
			string ctags_output = p.StandardOutput.ReadToEnd ();
			p.Close ();
			
			using (StringReader reader = new StringReader (ctags_output)) {
				while ((tagEntry = reader.ReadLine ()) != null) {
					if (tagEntry.StartsWith ("!_")) continue;
					
					Tag tag = ParseTag (tagEntry);
					
					AddInfo (info, tag, ctags_output);
				}
			}			
			
			FileUpdated (new ClassPadEventArgs (project));
			
			UpdateSystemTags (project, filename, system_headers);
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
			
			pattern = tagEntry.Substring (i1 + 1, i2 - i1 - 1);
			
			tagField = tagEntry.Substring (i2 + 5);
			
			// parse tag field
			kind = (TagKind)tagField[0];
			
			string[] fields = tagField.Split ('\t');
			int index;
			
			foreach (string field in fields) {
				index = field.IndexOf (':');
				
				if (index > 0) {
					string key = field.Substring (0, index);
					string val = field.Substring (index + 1);
					switch (key) {
					case "access":
						access = (AccessModifier)System.Enum.Parse (typeof(AccessModifier), val, true);
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
		
//		public void FillProjectInformation (Project project)
//		{
//			string tagsDir = Path.Combine (project.BaseDirectory, ".tags");
//			string tagsFile = Path.Combine (tagsDir, "tags");
//			
//			if (!File.Exists (tagsFile))
//				throw new IOException ("Tags file does not exist for project: " + project.Name);
//			
//			string tagEntry;
//			
//			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
//			
//			info.Clear ();
//			
//			using (StreamReader reader = new StreamReader (tagsFile)) {
//				while ((tagEntry = reader.ReadLine ()) != null) {
//					if (tagEntry.StartsWith ("!_")) continue;
//					
//					Tag tag = ParseTag (tagEntry);
//					
//					switch (tag.Kind)
//					{
//					case TagKind.Class:
//						Class c = new Class (tag, project);
//						if (!info.Classes.Contains (c))
//							info.Classes.Add (c);
//						break;
//					case TagKind.Enumeration:
//						Enumeration e = new Enumeration (tag, project);
//						if (!info.Enumerations.Contains (e))
//							info.Enumerations.Add (e);
//						break;
//					case TagKind.Enumerator:
//						Enumerator en= new Enumerator (tag, project);
//						if (!info.Enumerators.Contains (en))
//							info.Enumerators.Add (en);
//						break;
//					case TagKind.ExternalVariable:
//						break;
//					case TagKind.Function:
//						Function f = new Function (tag, project);
//						if (!info.Functions.Contains (f))
//							info.Functions.Add (f);
//						break;
//					case TagKind.Local:
//						break;
//					case TagKind.Macro:
//						Macro m = new Macro (tag, project);
//						if (!info.Macros.Contains (m))
//							info.Macros.Add (m);
//						break;
//					case TagKind.Member:
//						Member me = new Member (tag, project);
//						if (!info.Members.Contains (me))
//							info.Members.Add (me);
//						break;
//					case TagKind.Namespace:
//						Namespace n = new Namespace (tag, project);
//						if (!info.Namespaces.Contains (n))
//							info.Namespaces.Add (n);
//						break;
//					case TagKind.Prototype:
//						break;
//					case TagKind.Structure:
//						Structure s = new Structure (tag, project);
//						if (!info.Structures.Contains (s))
//							info.Structures.Add (s);
//						break;
//					case TagKind.Typedef:
//						Typedef t = new Typedef (tag, project);
//						if (!info.Typedefs.Contains (t))
//							info.Typedefs.Add (t);
//						break;
//					case TagKind.Union:
//						Union u = new Union (tag, project);
//						if (!info.Unions.Contains (u))
//							info.Unions.Add (u);
//						break;
//					case TagKind.Variable:
//						Variable v = new Variable (tag, project);
//						if (!info.Variables.Contains (v))
//							info.Variables.Add (v);
//						break;
//					default:
//						break;
//					}
//				}
//			}
//		}
		
		public LanguageItem GetParent (Tag tag, Project project, string ctags_output)
		{
			if (!string.IsNullOrEmpty (tag.Namespace))
				return new LanguageItem (FindTag (tag.Namespace, TagKind.Namespace, ctags_output), project);
			else if (!string.IsNullOrEmpty (tag.Class))
				return new LanguageItem (FindTag (tag.Class, TagKind.Class, ctags_output), project);
			else if (!string.IsNullOrEmpty (tag.Enum))
				return new LanguageItem (FindTag (tag.Enum, TagKind.Enumeration, ctags_output), project);
			else if (!string.IsNullOrEmpty (tag.Structure))
				return new LanguageItem (FindTag (tag.Structure, TagKind.Structure, ctags_output), project);
			else if (!string.IsNullOrEmpty (tag.Union))
				return new LanguageItem (FindTag (tag.Union, TagKind.Union, ctags_output), project);
			
			return null;
		}
		
		public Tag FindTag (string name, TagKind kind, string ctags_output)
		{		
			Tag tag;
			string tagEntry;
			
			using (StringReader reader = new StringReader (ctags_output)) {
				while ((tagEntry = reader.ReadLine ()) != null) {
					if (tagEntry.StartsWith ("!_")) continue;
					
					if (tagEntry.Substring (0, tagEntry.IndexOf ('\t')).Equals (name)) {
						tag = ParseTag (tagEntry);
						
						if (tag.Kind == kind)
							return tag;
					}
				}
				
				return null;
			}
		}
		
		public Tag FindTag (string name, TagKind kind, Project project)
		{
			string tagsDir = Path.Combine (project.BaseDirectory, ".tags");
			string tagsFile = Path.Combine (tagsDir, "tags");
			
			if (!File.Exists (tagsFile))
				throw new IOException ("Tags file does not exist for project: " + project.Name);
			
			Tag tag;
			string tagEntry;
			
			using (StreamReader reader = new StreamReader (tagsFile)) {
				while ((tagEntry = reader.ReadLine ()) != null) {
					if (tagEntry.StartsWith ("!_")) continue;
					
					if (tagEntry.Substring (0, tagEntry.IndexOf ('\t')).Equals (name)) {
						tag = ParseTag (tagEntry);
						
						if (tag.Kind == kind)
							return tag;
					}
				}
				
				return null;
			}
		}
		
		// TODO: This could probably be optimized...
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
	}
}
