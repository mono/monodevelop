//
// FakeDotNetProject.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using System.Linq;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeDotNetProject : FakeProject, IDotNetProject
	{
		public FakeDotNetProject ()
		{
			References = new ProjectReferenceCollection ();
			Files = new ProjectFileCollection ();
			TargetFrameworkMoniker = new TargetFrameworkMoniker ("v4.5");
			CreateEqualsAction ();
		}

		public FakeDotNetProject (string fileName)
			: base (fileName)
		{
			References = new ProjectReferenceCollection ();
			Files = new ProjectFileCollection ();
			CreateEqualsAction ();
		}

		public DotNetProject DotNetProject { get; set; }
		public TargetFrameworkMoniker TargetFrameworkMoniker { get; set; }
		public string DefaultNamespace { get; set; }
		public ProjectReferenceCollection References { get; set; }
		public ProjectFileCollection Files { get; set; }

		public List<ProjectFile> FilesAdded = new List<ProjectFile> ();

		public void AddFile (ProjectFile projectFile)
		{
			FilesAdded.Add (projectFile);
			Files.Add (projectFile);
		}

		public bool IsFileInProject (string fileName)
		{
			return Files.Any (file => file.FilePath == new FilePath (fileName));
		}

		public void AddProjectType (Guid guid)
		{
			ExtendedProperties.Add ("ProjectTypeGuids", guid.ToString ());
		}

		public int ReferencesWhenSavedCount;
		public int FilesAddedWhenSavedCount;
		public int FilesInProjectWhenSavedCount { get; set; }

		public Action SaveAction = () => { };

		public override void Save ()
		{
			SaveAction ();
			base.Save ();
			ReferencesWhenSavedCount = References.Count;
			FilesAddedWhenSavedCount = FilesAdded.Count;
			FilesInProjectWhenSavedCount = Files.Count;
		}

		public void AddDefaultBuildAction (string buildAction, string fileName)
		{
			DefaultBuildActions.Add (fileName.ToNativePath (), buildAction);
		}

		public Dictionary<string, string> DefaultBuildActions = new Dictionary<string, string> ();

		public string GetDefaultBuildAction (string fileName)
		{
			string buildAction = null;
			DefaultBuildActions.TryGetValue (fileName, out buildAction);
			return buildAction;
		}

		public List<ImportAndCondition> ImportsAdded = new List<ImportAndCondition> ();

		public void AddImportIfMissing (string name, string condition)
		{
			ImportsAdded.Add (new ImportAndCondition (name, condition));
		}

		public List<string> ImportsRemoved = new List <string> ();

		public void RemoveImport (string name)
		{
			ImportsRemoved.Add (name);
		}

		public event EventHandler<ProjectModifiedEventArgs> Modified;

		public void RaiseModifiedEvent (IDotNetProject project, string propertyName)
		{
			if (Modified != null) {
				Modified (this, new ProjectModifiedEventArgs (project, propertyName));
			}
		}

		public Func<IDotNetProject, bool> EqualsAction;

		void CreateEqualsAction ()
		{
			EqualsAction = project => {
				return this == project;
			};
		}

		public bool Equals (IDotNetProject project)
		{
			return EqualsAction (project);
		}

		public void RefreshProjectBuilder ()
		{
		}

		public bool IsProjectBuilderDisposed;

		public void DisposeProjectBuilder ()
		{
			IsProjectBuilderDisposed = true;
		}
	}
}

