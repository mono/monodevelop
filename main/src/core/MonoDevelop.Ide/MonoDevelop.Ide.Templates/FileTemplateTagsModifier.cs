//
// FileTemplateTagsModifier.cs
//
// Author:
//       Vincent Dondain <vincent.dondain@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using System;
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using System.Text;

namespace MonoDevelop.Ide.Templates
{
	static class FileTemplateTagsModifier
	{
		internal static void ModifyTags (SolutionFolderItem policyParent, Project project, string language, string identifier, string fileName, ref Dictionary<string,string> tags)
		{
			//DotNetProject netProject = project as DotNetProject;
			string languageExtension = "";
			LanguageBinding binding = null;
			if (!string.IsNullOrEmpty (language)) {
				binding = SingleFileDescriptionTemplate.GetLanguageBinding (language);
				if (binding != null)
					languageExtension = Path.GetExtension (binding.GetFileName ("Default")).Remove (0, 1);
			}

			//need a default namespace or if there is no project, substitutions can get very messed up
			string ns = "Application";
			string rootNamespace = ns;
			var dotNetFileContainer = project as IDotNetFileContainer;
			if (dotNetFileContainer != null) {
				ns = dotNetFileContainer.GetDefaultNamespace (fileName);
				rootNamespace = dotNetFileContainer.GetDefaultNamespace (null);
			}

			//need an 'identifier' for tag substitution, e.g. class name or page name
			//if not given an identifier, use fileName
			if ((identifier == null) && (fileName != null))
				identifier = Path.GetFileName (fileName);

			if (identifier != null) {
				//remove all extensions
				while (Path.GetExtension (identifier).Length > 0)
					identifier = Path.GetFileNameWithoutExtension (identifier);
				identifier = CreateIdentifierName (identifier);
				tags ["Name"] = identifier;
				tags ["FullName"] = ns.Length > 0 ? ns + "." + identifier : identifier;

				//some .NET languages may be able to use keywords as identifiers if they're escaped
				if (binding != null) {
					System.CodeDom.Compiler.CodeDomProvider provider = binding.GetCodeDomProvider ();
					if (provider != null) {
						tags ["EscapedIdentifier"] = provider.CreateEscapedIdentifier (identifier);
					}
				}
			}

			tags ["Namespace"] = ns;
			tags ["RootNamespace"] = rootNamespace;
			if (policyParent != null)
				tags ["SolutionName"] = policyParent.Name;
			if (project != null) {
				tags ["ProjectName"] = project.Name;
				tags ["SafeProjectName"] = CreateIdentifierName (project.Name);
				var info = project.AuthorInformation ?? AuthorInformation.Default;
				tags ["AuthorCopyright"] = info.Copyright;
				tags ["AuthorCompany"] = info.Company;
				tags ["AuthorTrademark"] = info.Trademark;
				tags ["AuthorEmail"] = info.Email;
				tags ["AuthorName"] = info.Name;
			}
			if ((language != null) && (language.Length > 0))
				tags ["Language"] = language;
			if (languageExtension.Length > 0)
				tags ["LanguageExtension"] = languageExtension;

			if (fileName != FilePath.Null) {
				FilePath fileDirectory = Path.GetDirectoryName (fileName);
				if (project != null && project.BaseDirectory != FilePath.Null && fileDirectory.IsChildPathOf (project.BaseDirectory))
					tags ["ProjectRelativeDirectory"] = fileDirectory.ToRelative (project.BaseDirectory);
				else
					tags ["ProjectRelativeDirectory"] = fileDirectory;

				tags ["FileNameWithoutExtension"] = Path.GetFileNameWithoutExtension (fileName);
				tags ["Directory"] = fileDirectory;
				tags ["FileName"] = fileName;
			}
		}

		static string CreateIdentifierName (string identifier)
		{
			var result = new StringBuilder ();
			for (int i = 0; i < identifier.Length; i++) {
				char ch = identifier[i];
				if (i != 0 && Char.IsLetterOrDigit (ch) || i == 0 && Char.IsLetter (ch) || ch == '_') {
					result.Append (ch);
				} else {
					result.Append ('_');
				}
			}
			return result.ToString ();
		}
	}
}

