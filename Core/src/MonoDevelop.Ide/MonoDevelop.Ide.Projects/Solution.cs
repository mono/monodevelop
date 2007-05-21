//
// Solution.cs
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.Projects
{
	public class Solution
	{
		const string versionNumber = "9.00";
		
		string fileName;
		
		List<SolutionItem>    items    = new List<SolutionItem> ();
		List<SolutionSection> sections = new List<SolutionSection> ();
		
		public string Name {
			get {
				return Path.GetFileNameWithoutExtension (this.fileName);
			}
		}
		
		public List<SolutionItem> Items {
			get {
				return items;
			}
		}
		
		public List<SolutionSection> Sections {
			get {
				return sections;
			}
		}
		
		public Solution ()
		{
			sections.Add (new SolutionSection ("SolutionConfigurationPlatforms", "preSolution"));
			sections.Add (new SolutionSection ("ProjectConfigurationPlatforms", "postSolution"));
		}
		public Solution (string fileName)
		{
			this.fileName = fileName;
		}
		
		enum ReadState {
			ReadVersion,
			ReadProjects,
			ReadGlobal
		};
		
		public void AddItem (SolutionItem item)
		{
			Items.Add(item);
			
			//System.Console.WriteLine ("Add item:" + item);
		}
		
		public SolutionItem GetItem (string guid)
		{
			foreach (SolutionItem item in this.Items) {
				if (item.Guid == guid) 
					return item;
			}
			return null;
		}
		
		public void AddSection (SolutionSection section)
		{
			Sections.Add(section);
			//System.Console.WriteLine ("Add section:" + section);
		}
		
		public SolutionSection GetSection (string name)
		{
			foreach (SolutionSection section in this.Sections) {
				if (section.Name == name) 
					return section;
			}
			return null;
		}
		
#region I/O
		static Regex versionPattern       = new Regex ("Microsoft Visual Studio Solution File, Format Version\\s+(?<Version>.*)", RegexOptions.Compiled);
		static Regex projectLinePattern   = new Regex ("Project\\(\"(?<TypeGuid>.*)\"\\)\\s+=\\s+\"(?<Name>.*)\",\\s*\"(?<Location>.*)\",\\s*\"(?<Guid>.*)\"", RegexOptions.Compiled);
		static Regex globalSectionPattern = new Regex ("\\s*GlobalSection\\((?<Name>.*)\\)\\s*=\\s*(?<Type>.*)", RegexOptions.Compiled);
		
		public static Solution Load (string fileName)
		{
			if (!File.Exists (fileName)) {
				throw new FileNotFoundException ("Solution file not found", fileName);
			}
			Solution result = new Solution (fileName);
			string basePath = Path.GetDirectoryName (fileName);
			using (StreamReader reader = File.OpenText (fileName)) {
				ReadState state = ReadState.ReadVersion;
				Match match;
				SolutionReadHelper.ReadSection (reader, delegate(string curLine, SolutionReadHelper.ReadLineData data) {
					switch (state) {
					case ReadState.ReadVersion:
						match = versionPattern.Match (curLine);
						if (match.Success) {
							string version = match.Result ("${Version}");
							if (version != versionNumber) {
								// TODO: Conversion
								data.ContinueRead = false;
								result = null;
								return;
							}
							state = ReadState.ReadProjects;
						}
						break;
					case ReadState.ReadProjects:
						match = projectLinePattern.Match (curLine);
						if (match.Success) {
							string typeGuid     = match.Result ("${TypeGuid}");
							string guid         = match.Result ("${Guid}");
							string name         = match.Result ("${Name}");
							string location     = match.Result ("${Location}");
							result.AddItem (SolutionItemFactory.CreateSolutionItem(reader, basePath, typeGuid, guid, name, location));
						} else if (curLine == "Global") {
							state = ReadState.ReadGlobal;
						}
						break;
					case ReadState.ReadGlobal:
						match = globalSectionPattern.Match (curLine);
						if (match.Success) {
							result.AddSection (SolutionSection.Read (reader, match.Result ("${Name}"), match.Result ("${Type}")));
						}
						break;
					}
				});
				
				SolutionSection nestedProjects = result.GetSection ("NestedProjects");
				if (nestedProjects != null) {
					foreach (KeyValuePair<string, string> pair in nestedProjects.Contents) {
						SolutionFolder parent = result.GetItem (pair.Value) as SolutionFolder;
						SolutionItem   child  = result.GetItem (pair.Key);
						if (parent != null && child != null) {
							parent.AddChild (child);
						}
					}
				}
				return result;
			}
		}
		
		public void Save (string fileName)
		{
			using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8)) {
				writer.WriteLine ();
				writer.WriteLine ("#Microsoft Visual Studio Solution File, Format Version " + versionNumber);
				writer.WriteLine ("# Visual Studio 2005");				writer.WriteLine ("# MonoDevelop ");
				Console.WriteLine ("items:" + this.Items.Count);
				
				foreach (SolutionItem item in this.Items) {
					item.Write (writer);
				}
				writer.WriteLine ("Global");
				foreach (SolutionSection section in this.Sections) {
					section.Write (writer);
				}
				writer.WriteLine ("EndGlobal");
			}
		}
#endregion
	}
}
