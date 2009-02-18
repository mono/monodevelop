//
// ProjectPackage.cs: A pkg-config package
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

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace CBinding
{
	public class Package
	{
		[ItemProperty ("file")]
		private string file;
		
		[ItemProperty ("name")]
		private string name;
		
		[ItemProperty ("IsProject")]
		private bool is_project;

		private string description;
		private string version;
		private List<string> requires = new List<string>();
		private List<string> libPaths = new List<string>();
		private List<string> libs = new List<string>();
		private List<string> cflags = new List<string>();
		private Dictionary<string, string> vars = new Dictionary<string,string> ();
		
		public Package (string file)
		{
			this.file = file;
			this.is_project = false;
			
			ParsePackageEssentials ();
		}
		
		public Package (CProject project)
		{
			name = project.Name;
			file = Path.Combine (project.BaseDirectory, name + ".md.pc");
			is_project = true;
			
			ParsePackageEssentials ();
		}
		
		public Package ()
		{
		}
		
		void ParsePackageEssentials ()
		{
			string line;
			
			using (StreamReader reader = new StreamReader (file)) {
				while ((line = reader.ReadLine ()) != null) {
					if (line.StartsWith ("Name")) {
						name = line.Split (':')[1].TrimStart ();
						continue;
					}
					
					if (line.StartsWith ("Version")) {
						version = line.Split (':')[1].TrimStart ();
					}
				}
			}
		}
		
		bool parsed = false;
		public void ParsePackage ()
		{
			if (parsed)
				return;
			
			string line;
			
			using (StreamReader reader = new StreamReader (file)) {
				while ((line = reader.ReadLine ()) != null) {
					if (line.StartsWith ("#"))
					    continue;
					    
					if (line.IndexOf ('=') >= 0)
						ParseVar (line);
					
					if (line.IndexOf (':') >= 0)
						ParseProperty (line);
				}
			}
			
			parsed = true;
		}
		
		void ParseVar (string line)
		{
			int i = line.IndexOf ('=');
			string key = line.Substring (0, i);
			string value = line.Substring (i+1, line.Length - i-1).Trim ();
			string parsedValue = StringParserService.Parse (value, CustomTags ());
			
			vars.Add (key, parsedValue);
		}
		
		void ParseProperty (string line)
		{
			int i = line.IndexOf (':');
			string key = line.Substring (0, i);
			string value = StringParserService.Parse (line.Substring (i+1, line.Length - i-1).Trim (), CustomTags ());
			
			if (value.Length <= 0)
				return;
			
			switch (key) {
			case "Name":
				name = value;
				break;
			case "Description":;
				description = ProcessDescription (value);
				break;
			case "Version":
				version = value;
				break;
			case "Requires":
				ParseRequires (value);
				break;
			case "Libs":
				ParseLibs (value);
				break;
			case "Cflags":
				ParseCFlags (value);
				break;
			}
		}
		
		void ParseRequires (string reqsline)
		{
			string[] splitted = reqsline.Split (' ');
			
			foreach (string str in splitted) {
				if (str.Trim () == string.Empty)
					continue;
				Requires.Add (str);
			}
		}
		
		void ParseLibs (string libline)
		{
			int i = 0;
			string lib;
			
			while (true) {
				i = libline.IndexOf ('-', i);
				
				if (i < 0)
					break;
				
				int count = 0;
				
				while (libline.Length > (count+i+2) && libline[count+i+2] != ' ')
					count++;
				
				lib = libline.Substring (i+2, count);
				
				if (libline[i+1] == 'L') {
					libPaths.Add (lib);
				} else if (libline[i+1] == 'l') {
					libs.Add (lib);
				}
				
				i++;
			}
		}
		
		void ParseCFlags (string cflagsline)
		{
			string[] splitted = cflagsline.Split (' ');
			
			foreach (string str in splitted) {
				if (str.Trim () == string.Empty)
					continue;
				CFlags.Add (str);
			}
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
		string ProcessDescription (string desc)
		{
			int length = 80;
			
			if (desc.Length <= length)
				return desc;			
			
			StringBuilder builder = new StringBuilder (desc);
			int i = 0;
			int lines = 1;
			
			while (i < desc.Length) {
				i++;
				
				if (i > lines * length) {
					lines++;
					
					do {
						i--;
					} while (desc [i] != ' ');
					
					builder.Replace (' ', '\n', i, 1);
				}
			}
			
			return builder.ToString ();
		}
		
		Dictionary<string, string> CustomTags ()
		{
			Dictionary<string, string> customTags = new Dictionary<string, string> (StringComparer.InvariantCultureIgnoreCase);
			int i = 0;
			
			foreach (KeyValuePair<string, string> kvp in vars) {
				customTags.Add (kvp.Key, kvp.Value);
				i++;
			}
			
			return customTags;
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
		
		public string Version {
			get { return version; }
			set { version = value; }
		}

		public string Description {
			get { return description; }
		}

		public List<string> Requires {
			get { return requires; }
		}

		public List<string> LibPaths {
			get { return libPaths; }
		}

		public List<string> Libs {
			get { return libs; }
		}

		public List<string> CFlags {
			get { return cflags; }
		}
		
		public override bool Equals (object o)
		{
			Package other = o as Package;
			
			if (other == null) return false;
			
			return other.File.Equals (file);
		}
		
		public override int GetHashCode ()
		{
			return (name + version).GetHashCode ();
		}
	}
}
