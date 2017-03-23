//
// FinalProjectConfigurationPage.cs
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

using System;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Ide.Templates;
using ProjectCreateParameters = MonoDevelop.Projects.ProjectCreateParameters;
using SolutionFolder = MonoDevelop.Projects.SolutionFolder;

namespace MonoDevelop.Ide.Projects
{
	class FinalProjectConfigurationPage
	{
		static readonly string WorkplaceTemplateId = "MonoDevelop.Workspace";

		NewProjectConfiguration config;
		SolutionTemplate template;
		bool valid;
		bool projectNameIsReadOnly;

		public FinalProjectConfigurationPage (NewProjectConfiguration config)
		{
			this.config = config;
		}

		public SolutionFolder ParentFolder { get; set; }

		public ProjectCreateParameters Parameters {
			get { return config.Parameters; }
		}

		public string ProjectFileExtension {
			get { return template.ProjectFileExtension; }
		}

		public SolutionTemplate Template {
			get { return template; }
			set {
				template = value;
				HasProjects = template.HasProjects;
				config.Parameters.Clear ();
				config.Parameters ["CreateSolution"] = (ParentFolder == null).ToString ();
			}
		}

		public bool IsWorkspace {
			get { return template.Id == WorkplaceTemplateId; }
		}

		public bool HasProjects {
			get { return !config.IsNewSolutionWithoutProjects; }
			private set {
				config.IsNewSolutionWithoutProjects = !value;
				CheckIsValid ();
			}
		}

		public string Location {
			get { return config.Location; }
			set {
				config.Location = value;
				CheckIsValid ();
			}
		}

		public string ProjectName {
			get { return config.ProjectName; }
			set {
				config.ProjectName = value;
				Parameters ["UserDefinedProjectName"] = value;
				CheckIsValid ();
			}
		}

		public string SolutionName {
			get {
				if (ParentFolder != null) {
					return ParentFolder.Name;
				}
				return config.SolutionName;
			}
			set {
				config.SolutionName = value;
				CheckIsValid ();
			}
		}

		public string ProjectFileName {
			get { return config.ProjectName + ProjectFileExtension; }
		}

		public string SolutionFileName {
			get { return config.SolutionName + GetSolutionFileExtension (); }
		}

		string GetSolutionFileExtension ()
		{
			if (IsWorkspace) {
				return ".mdw";
			}
			return ".sln";
		}

		public string GetValidProjectName ()
		{
			return config.GetValidProjectName ();
		}

		public string GetValidSolutionName ()
		{
			if (ParentFolder != null) {
				return ParentFolder.Name;
			}
			return config.GetValidSolutionName ();
		}

		public bool CreateGitIgnoreFile {
			get { return config.CreateGitIgnoreFile; }
			set { config.CreateGitIgnoreFile = value; }
		}

		public bool UseGit {
			get { return config.UseGit; }
			set { config.UseGit = value; }
		}

		public bool CreateProjectDirectoryInsideSolutionDirectory {
			get { return config.CreateProjectDirectoryInsideSolutionDirectory; }
			set { config.CreateProjectDirectoryInsideSolutionDirectory = value; }
		}

		public bool IsProjectNameEnabled {
			get { return HasProjects && !projectNameIsReadOnly; }
		}

		public bool IsSolutionNameEnabled {
			get { return config.CreateSolution; }
		}

		public bool IsCreateProjectDirectoryInsideSolutionDirectoryEnabled {
			get { return HasProjects && IsNewSolution; }
		}

		public bool IsGitIgnoreEnabled {
			get { return config.UseGit && IsUseGitEnabled; }
		}

		public bool IsUseGitEnabled { get; set; }

		public bool IsNewSolution {
			get { return config.CreateSolution; }
		}

		public string DefaultPreviewSolutionName {
			get {
				if (IsWorkspace) {
					return "Workspace";
				}
				return "Solution";
			}
		}

		public string DefaultPreviewSolutionFileName {
			get {
				if (IsWorkspace) {
					return "Workspace.mdw";
				}
				return "Solution.sln";
			}
		}

		public string DefaultPreviewProjectName {
			get { return "Project"; }
		}

		public string DefaultPreviewProjectFileName {
			get { return "Project" + ProjectFileExtension; }
		}

		void CheckIsValid ()
		{
			IsValid = (!IsProjectNameEnabled || !String.IsNullOrEmpty (config.ProjectName)) &&
				(!IsSolutionNameEnabled || !String.IsNullOrEmpty (config.SolutionName)) &&
				!String.IsNullOrEmpty (config.Location);
		}

		public bool IsValid {
			get { return valid; }
			set {
				if (valid != value) {
					valid = value;
					OnIsValidChanged ();
				}
			}
		}

		public event EventHandler IsValidChanged;

		void OnIsValidChanged ()
		{
			if (IsValidChanged != null) {
				IsValidChanged (this, new EventArgs ());
			}
		}

		public void UpdateFromParameters ()
		{
			ProjectName = Parameters ["ProjectName"];
			projectNameIsReadOnly = Parameters.GetBoolValue ("IsProjectNameReadOnly", false);
		}
	}
}

