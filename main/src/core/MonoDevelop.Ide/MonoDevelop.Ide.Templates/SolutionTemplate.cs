//
// SolutionTemplate.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Ide.Templates
{
	public class SolutionTemplate
	{
		public static readonly string DefaultLargeImageId = "template-default-background-light.png";

		string largeImageId;
		string language;
		string projectFileExtension;
		List<string> availableLanguages = new List<string> ();
		List<SolutionTemplate> groupedTemplates = new List<SolutionTemplate> ();

		public SolutionTemplate (string id, string name, string iconId)
		{
			Id = id;
			Name = name;
			IconId = UseDefaultIconIdIfNullOrEmpty (iconId);
			HasProjects = true;
		}

		static string UseDefaultIconIdIfNullOrEmpty (string iconId)
		{
			if (String.IsNullOrEmpty (iconId)) {
				return "md-project";
			}
			return iconId;
		}

		public string Id { get; private set; }
		public string Name { get; private set; }
		public string IconId { get; private set; }
		public string Description { get; set; }
		public string Category { get; set; }
		public bool HasProjects { get; set; }

		/// <summary>
		/// Allows templates to be grouped together in the New Project dialog.
		/// </summary>
		public string GroupId { get; set; }

		public bool HasGroupId {
			get { return !String.IsNullOrEmpty (GroupId); }
		}

		public string Language {
			get { return language; }
			set {
				language = value;
				if (!String.IsNullOrEmpty (value)) {
					availableLanguages.Add (language);
				}
			}
		}

		public string ProjectFileExtension {
			get {
				if (!String.IsNullOrEmpty (projectFileExtension)) {
					return projectFileExtension;
				}
				return GetProjectFileExtensionForLanguage (language);
			}
			set { projectFileExtension = value; }
		}

		public void AddGroupTemplate (SolutionTemplate template)
		{
			groupedTemplates.Add (template);
			availableLanguages.Add (template.Language);
		}

		public SolutionTemplate GetTemplate (string language)
		{
			if (this.language == language) {
				return this;
			}

			return groupedTemplates.FirstOrDefault (template => template.Language == language);
		}

		public string LargeImageId {
			get {
				if (String.IsNullOrEmpty (largeImageId)) {
					return DefaultLargeImageId;
				}
				return largeImageId;
			}
			set { largeImageId = value; }
		}

		public string Wizard { get; set; }

		public bool HasWizard {
			get { return !String.IsNullOrEmpty (Wizard); }
		}

		public IList<string> AvailableLanguages {
			get { return availableLanguages; }
		}

		public override string ToString ()
		{
			return String.Format ("[Template: Id={0}, Name={1}, Category={2}]", Id, Name, Category);
		}

		public bool IsGroupMatch (SolutionTemplate template)
		{
			if (template.HasGroupId && HasGroupId) {
				return String.Equals (GroupId, template.GroupId, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		static string GetProjectFileExtensionForLanguage (string language)
		{
			switch (language) {
			case "C#":
				return ".csproj";
			case "VBNet":
				return ".vbproj";
			case "IL":
				return ".ilproj";
			default:
				return ".proj";
			}
		}
	}
}

