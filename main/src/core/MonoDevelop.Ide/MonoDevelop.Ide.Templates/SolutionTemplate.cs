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
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	public class SolutionTemplate : IEquatable<SolutionTemplate>
	{
		public static readonly string DefaultImageId = "md-project";

		string imageId;
		string language;
		string projectFileExtension;
		List<string> availableLanguages = new List<string> ();
		List<SolutionTemplate> groupedTemplates = new List<SolutionTemplate> ();
		TemplateCondition condition = TemplateCondition.Null;

		public SolutionTemplate (string id, string name, string iconId)
		{
			Id = id;
			Name = name;
			IconId = UseDefaultIconIdIfNullOrEmpty (iconId);
			HasProjects = true;
			Visibility = SolutionTemplateVisibility.All;
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
		public SolutionTemplateVisibility Visibility { get; set; }

		/// <summary>
		/// Allows templates to be grouped together in the New Project dialog.
		/// </summary>
		public string GroupId { get; set; }

		public bool HasGroupId {
			get { return !String.IsNullOrEmpty (GroupId); }
		}

		/// <summary>
		/// Allows a template to be selected conditionally.
		/// </summary>
		public string Condition {
			get { return condition.ToString (); }
			set {
				condition = new TemplateCondition (value);
			}
		}

		public bool HasCondition {
			get { return !String.IsNullOrEmpty (Condition); }
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
			template.Parent = this;

			if (!availableLanguages.Contains (template.Language)) {
				availableLanguages.Add (template.Language);
			}
		}

		internal SolutionTemplate Parent { get; set; }

		internal void ClearGroupedTemplates ()
		{
			Parent = null;
			groupedTemplates.Clear ();
		}

		public SolutionTemplate GetTemplate (string language)
		{
			return GetTemplate (template => template.Language == language);
		}

		public SolutionTemplate GetTemplate (string language, ProjectCreateParameters parameters)
		{
			return GetTemplate (template => template.IsMatch (language, parameters));
		}

		public SolutionTemplate GetTemplate (Predicate<SolutionTemplate> predicate)
		{
			if (predicate (this)) {
				return this;
			}

			if (Parent != null)
				return Parent.GetTemplate (predicate);

			return groupedTemplates.FirstOrDefault (template => predicate (template));
		}

		public string ImageId {
			get {
				if (String.IsNullOrEmpty (imageId)) {
					return DefaultImageId;
				}
				return imageId;
			}
			set { imageId = value; }
		}

		public string ImageFile { get; set; }

		public bool HasImageFile {
			get { return !String.IsNullOrEmpty (ImageFile); }
		}

		public string Wizard { get; set; }

		public bool HasWizard {
			get { return !String.IsNullOrEmpty (Wizard); }
		}

		public string SupportedParameters { get; set; }
		public string DefaultParameters { get; set; }

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

		bool IsMatch (string language, ProjectCreateParameters parameters)
		{
			return (this.language == language) && !condition.IsExcluded (parameters);
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
			case "F#":
				return ".fsproj";
			case "C":
			case "Objective C":
			case "CPP":
				return ".cproj";
			default:
				return ".proj";
			}
		}

		internal bool IsMatch (SolutionTemplateVisibility visibility)
		{
			return (Visibility == visibility) || (Visibility == SolutionTemplateVisibility.All);
		}

		public static bool operator == (SolutionTemplate template1, SolutionTemplate template2)
		{
			if (ReferenceEquals (template1, template2))
				return true;
			
			if (((object)template1 == null) || ((object)template2 == null))
				return false;
			
			return template1.Equals (template2);
		}

		public static bool operator != (SolutionTemplate template1, SolutionTemplate template2)
		{
			return !(template1 == template2);
		}

		public bool Equals (SolutionTemplate other)
		{
			return other != null && Id == other.Id && Name == other.Name && Category == other.Category;
		}

		public override bool Equals (object obj)
		{
			return Equals (obj as SolutionTemplate);
		}

		public override int GetHashCode ()
		{
			return (Id != null ? Id.GetHashCode () : 0)
				^ (Name != null ? Name.GetHashCode () : 0)
				^ (Category != null ? Category.GetHashCode () : 0);
		}

		/// <summary>
		/// Returns all other templates in the group. Does not include this template.
		/// </summary>
		internal IEnumerable<SolutionTemplate> GetGroupedTemplates ()
		{
			if (Parent != null)
				return Parent.groupedTemplates
					.Where (template => template != this)
					.Concat (Parent);

			return groupedTemplates;
		}
	}
}

