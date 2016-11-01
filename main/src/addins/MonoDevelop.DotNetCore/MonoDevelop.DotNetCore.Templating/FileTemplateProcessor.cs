//
// FileTemplateProcessor.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.Templating
{
	static class FileTemplateProcessor
	{
		static readonly FilePath templateSourceRootDirectory;

		static FileTemplateProcessor ()
		{
			templateSourceRootDirectory = GetTemplateSourceRootDirectory ();
		}

		public static void CreateFilesFromTemplate (Project project, SolutionFolderItem policyItem, string projectTemplateName, params string[] files)
		{
			foreach (string templateFileName in files) {
				CreateFileFromTemplate (project, policyItem, projectTemplateName, templateFileName);
			}
		}

		public static FilePath GetTemplateDirectory (string projectTemplateName)
		{
			FilePath templateSourceDirectory = templateSourceRootDirectory;

			if (!String.IsNullOrEmpty (projectTemplateName)) {
				templateSourceDirectory = templateSourceRootDirectory.Combine (projectTemplateName.Replace ("Project", ""));
			}

			return templateSourceDirectory;
		}

		public static void CreateFileFromTemplate (Project project, SolutionFolderItem policyItem, string projectTemplateName, string fileTemplateName)
		{
			FilePath templateSourceDirectory = GetTemplateDirectory (projectTemplateName);

			if (!String.IsNullOrEmpty (projectTemplateName)) {
				fileTemplateName = projectTemplateName + "." + fileTemplateName;
			}

			CreateFileFromTemplate (project, policyItem, templateSourceDirectory, fileTemplateName);
		}

		public static void CreateFileFromTemplate (Project project, SolutionFolderItem policyItem, FilePath templateSourceDirectory, string fileTemplateName)
		{
			string templateFileName = templateSourceDirectory.Combine (fileTemplateName + ".xft.xml");
			using (Stream stream = File.OpenRead (templateFileName)) {
				var document = new XmlDocument ();
				document.Load (stream);

				foreach (XmlElement templateElement in document.DocumentElement["TemplateFiles"].ChildNodes.OfType<XmlElement> ()) {
					var template = FileDescriptionTemplate.CreateTemplate (templateElement, templateSourceDirectory);
					template.AddToProject (policyItem, project, "C#", project.BaseDirectory, null);
				}
			}
		}

		public static void CreateFileFromTemplate (Solution solution, FilePath templateSourceDirectory, string templateName)
		{
			var project = new DummyProject (string.Empty);
			project.BaseDirectory = solution.BaseDirectory;
			CreateFileFromTemplate (project, solution.RootFolder, templateSourceDirectory, templateName);
		}

		public static FilePath GetTemplateSourceRootDirectory ()
		{
			var assemblyPath = new FilePath (typeof(FileTemplateProcessor).Assembly.Location);
			return assemblyPath.ParentDirectory.Combine ("Templates", "Projects");
		}
	}

	class DummyProject : Project, IDotNetFileContainer
	{
		string name;
		string defaultNamespace;

		public DummyProject (string name)
		{
			this.name = name;
			defaultNamespace = SanitisePotentialNamespace (name);

			Initialize (this);
		}

		protected override string OnGetName ()
		{
			return name;
		}

		static string SanitisePotentialNamespace (string potential)
		{
			var sb = new StringBuilder ();
			foreach (char c in potential) {
				if (char.IsLetter (c) || c == '_' || (sb.Length > 0 && (char.IsLetterOrDigit (sb[sb.Length - 1]) || sb[sb.Length - 1] == '_') && (c == '.' || char.IsNumber (c)))) {
					sb.Append (c);
				}
			}
			if (sb.Length > 0) {
				if (sb[sb.Length - 1] == '.')
					sb.Remove (sb.Length - 1, 1);

				return sb.ToString ();
			} else
				return null;
		}

		public string GetDefaultNamespace (string fileName, bool useVisualStudioNamingPolicy = false)
		{
			return defaultNamespace;
		}
	}
}
