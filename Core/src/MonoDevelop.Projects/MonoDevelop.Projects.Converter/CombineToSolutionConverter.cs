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
			ReadSolution (result, sourceFile);
			return result;
		}
		
		static Regex entryLinePattern = new Regex("<Entry\\s*filename=\\s*\"(?<File>.*)\"\\s*/>", RegexOptions.Compiled);
		void ReadSolution (Solution solution, string sourceFile)
		{
			string sourcePath = Path.GetDirectoryName (sourceFile);
			string content = File.ReadAllText (sourceFile);
			foreach (Match match in entryLinePattern.Matches (content)) {
				string fileName = Path.GetFullPath (Path.Combine (sourcePath, match.Result("${File}")));
				if (Path.GetExtension (fileName) == ".mds")
					ReadSolution (solution, fileName);
				else if (Path.GetExtension (fileName) == ".mdp") 
					solution.AddItem (ReadProject (fileName));
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
		
		SolutionProject ReadProject (string sourceFile)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (sourceFile);
			string version  = doc.DocumentElement.GetAttribute ("fileversion");
			string language = doc.DocumentElement.GetAttribute ("language");
			
			SolutionProject result = new SolutionProject (GetGUID (language), 
			                                              "{" + Guid.NewGuid ().ToString ().ToUpper () + "}", 
			                                              Path.GetFileNameWithoutExtension (sourceFile),
			                                              sourceFile);
			MSBuildProject project = new MSBuildProject (language, Path.ChangeExtension (sourceFile, GetExtension (language)));
			project.Configuration = "Debug";
			project.Platform      = "AnyCpu";
			XmlElement debug = doc.DocumentElement.SelectSingleNode (@"/Project/Configurations/Configuration[name=Debug]") as XmlElement;
			if (debug != null) {
				project.OutputPath = ConvertPath (debug["Output"].GetAttribute ("directory"));
				project.OutputType = debug["Build"].GetAttribute ("target");
			} else {
				project.OutputPath = "bin\\debug";
				project.OutputType = "Exe";
			}
			
			XmlNode contents = doc.DocumentElement.SelectSingleNode ("/Project/Contents");
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
			result.Project = project;
			Console.WriteLine ("gen. prj:" + result + " -- " + result.Project + " -- " + result.Project.Items.Count);
			return result;
		}
	}
}
