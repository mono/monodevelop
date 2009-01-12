//
// ProjectPackage.cs: A pkg-config package
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
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
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Mono.Addins;

using MonoDevelop.Core.Serialization;

namespace MonoDevelop.ValaBinding
{
	public class ProjectPackage
	{
		[ItemProperty ("file")]
		private string file;
		
		[ItemProperty ("name")]
		private string name;
		
		[ItemProperty ("IsProject")]
		private bool is_project;
		
		public string Description {
			get{ return description; }
			set{ description = value; }
		}
		private string description;
		
		public string Version {
			get{ return version; }
			set{ version = value; }
		}
		private string version;
		
		public List<string> Requires {
			get { return requires; }
		}
		private List<string> requires;
		
		private static string[] packagePaths;
		
		static ProjectPackage () {
			packagePaths = ScanPackageDirs ();
		}

		protected ProjectPackage() {
			requires = new List<string>();
			description = string.Empty;
			version = string.Empty;
		}
		
		public ProjectPackage (string file): this()
		{
			this.file = file;
			this.name = Path.GetFileNameWithoutExtension(file);
			this.is_project = false;
			ParsePackage();
			ParseRequires();
		}
		
		public ProjectPackage (ValaProject project): this()
		{
			name = project.Name;
			ValaProjectConfiguration vpc = (ValaProjectConfiguration)(project.DefaultConfiguration);
			file = Path.Combine (vpc.OutputDirectory, name + ".vapi");
			is_project = true;
		}
		
		public string File {
			get { return file; }
			set { file = value; }
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public bool IsProject {
			get { return is_project; }
			set { is_project = value; }
		}
		
		public override bool Equals (object o)
		{
			ProjectPackage other = o as ProjectPackage;
			
			if (other == null) return false;
			
			return other.File.Equals (file);
		}
		
		public override int GetHashCode ()
		{
			return (file + name).GetHashCode ();
		}
		
		/// <summary>
		/// Insert '\n's to make sure string isn't too long.
		/// </summary>
		/// <param name="desc">
		/// The unprocessed description.
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// The processed description.
		/// A <see cref="System.String"/>
		/// </returns>
		public static string ProcessDescription (string desc)
		{
			return Regex.Replace(desc, @"(.{1,80} )", "$&" + Environment.NewLine, RegexOptions.Compiled);
		}
		
		/// <summary>
		/// Search for a .pc file for this package, and parse its relevant attributes
		/// </summary>
		protected void ParsePackage ()
		{
			string line, pcfile;
			
			try {
				foreach(string path in packagePaths) {
					pcfile = Path.Combine (path, Path.ChangeExtension (Path.GetFileName (file), ".pc"));
					if (!System.IO.File.Exists (pcfile)){ continue; }
					using (StreamReader reader = new StreamReader (pcfile)) {
						if (null == reader){ continue; }
						
						while ((line = reader.ReadLine ()) != null) {
							if (Regex.IsMatch(line, @"^\s*#", RegexOptions.Compiled))
							    continue;
							    
		//					if (line.IndexOf ('=') >= 0)
		//						ParseVar (line);
							
							if (line.IndexOf (':') >= 0)
								ParseProperty (line);
						}
						return;
					}
				}
			} catch (FileNotFoundException) {
				// We just won't populate some fields
			} catch (IOException) {
				// We just won't populate some fields
			}
		}
		
		protected void ParseProperty (string line)
		{
			string[] tokens = line.Split(new char[]{':'}, 2);
			
			if(2 != tokens.Length){ return; }
			
			string key = tokens[0];
			string value = tokens[1].Trim();
			
			if (value.Length <= 0)
				return;
			
			switch (key) {
			case "Description":;
				description = ProcessDescription (value);
				break;
			case "Version":
				version = value;
				break;
			}
		}
		
		protected void ParseRequires ()
		{
			string line;
			
			try {
				using (StreamReader reader = new StreamReader (Path.ChangeExtension(file, ".deps"))) {
					if(null == reader){ return; }
					for(; null != (line = reader.ReadLine()); requires.Add(line));
				}
			} catch (FileNotFoundException) {
				// We just won't populate requires
			} catch (IOException) {
				// We just won't populate requires
			}
		}
		
		/// <summary>
		/// Scans PKG_CONFIG_PATH and a few static directories 
		/// for potential pkg-config repositories
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> array: The potential directories
		/// </returns>
		private static string[] ScanPackageDirs ()
		{
			List<string> dirs = new List<string> ();
			string pkg_var = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
			string[] staticPaths = { "/usr/lib/pkgconfig",
				"/usr/lib64/pkgconfig",
				"/usr/share/pkgconfig",
				"/usr/local/lib/pkgconfig",
				"/usr/local/share/pkgconfig"
			};
				
			dirs.AddRange(pkg_var.Split(new char[]{System.IO.Path.PathSeparator}, StringSplitOptions.RemoveEmptyEntries));
			
			foreach(string dir in staticPaths) {
				if(!dirs.Contains(dir)){ dirs.Add(dir); }
			}
			
			return dirs.ToArray ();
		}
		
		/// <summary>
		/// Converts an absolute path to a relative one
		/// </summary>
		/// <param name="absolutePath">
		/// A <see cref="System.String"/>: The absolute path
		/// </param>
		/// <param name="relativeTo">
		/// A <see cref="System.String"/>: The path to which the output path shall be relative
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>: The relative path from relativeTo to absolutePath
		/// </returns>
		public static string ToRelativePath (string absolutePath, string relativeTo) {
			List<string>  fileTokens = new List<string> (absolutePath.Split (Path.DirectorySeparatorChar)),
						  anchorTokens = new List<string> (relativeTo.Split (Path.DirectorySeparatorChar));
			StringBuilder builder = new StringBuilder ();
			int length = 0;

			if (!Path.IsPathRooted(absolutePath)){ return absolutePath; }
			if (absolutePath == relativeTo){ return Path.GetFileName (absolutePath); }
	
			if (absolutePath.StartsWith (relativeTo) && Directory.Exists (relativeTo)) {
				builder.AppendFormat (".{0}", Path.DirectorySeparatorChar);
			}// if absolutePath is inside relativeTo
	
			for (;0 != fileTokens.Count && 0 != anchorTokens.Count;) {
				if (fileTokens[0] == anchorTokens[0]) {
					fileTokens.RemoveAt (0);
					anchorTokens.RemoveAt (0);
				} else { break; }
			}// strip identical leading path
	
			for (int i=0; i < anchorTokens.Count-1; ++i) {
				builder.AppendFormat ("..{0}", Path.DirectorySeparatorChar);
			}// navigate out of anchor subdir
	
			foreach (string token in fileTokens) {
				builder.AppendFormat ("{0}{1}", token, Path.DirectorySeparatorChar);
			}// append filepath
	
			length = builder.Length;
			if (0 < builder.Length && Path.DirectorySeparatorChar == builder[builder.Length-1]) {
				--length;
			}// check for trailing separator

			return builder.ToString (0, length);
		}// ToRelativePath
	}
}
