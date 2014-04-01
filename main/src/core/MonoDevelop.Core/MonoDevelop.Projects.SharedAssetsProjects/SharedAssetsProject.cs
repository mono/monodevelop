//
// SharedProject.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.IO;
using System.Xml;

namespace MonoDevelop.Projects.SharedAssetsProjects
{
	public class SharedAssetsProject: Project
	{
		Solution currentSolution;

		public SharedAssetsProject ()
		{
		}

		public SharedAssetsProject (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions)
		{
		}

		public string DefaultNamespace { get; set; }

		public override string ProjectType {
			get {
				return "SharedAssets";
			}
		}

		public override string[] SupportedLanguages {
			get {
				return new [] {"C#"};
			}
		}

		protected override BuildResult OnBuild (MonoDevelop.Core.IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return new BuildResult ();
		}

		internal protected override bool OnGetCanRunTarget (string target, ConfigurationSelector configuration)
		{
			return false;
		}

		protected override void OnBoundToSolution ()
		{
			if (currentSolution != null) {
				currentSolution.ReferenceAddedToProject -= HandleReferenceAddedToProject;
				currentSolution.ReferenceRemovedFromProject -= HandleReferenceRemovedFromProject;
			}

			base.OnBoundToSolution ();

			ParentSolution.ReferenceAddedToProject += HandleReferenceAddedToProject;
			ParentSolution.ReferenceRemovedFromProject += HandleReferenceRemovedFromProject;
			currentSolution = ParentSolution;
		}

		public override void Dispose ()
		{
			base.Dispose ();
			if (currentSolution != null) {
				currentSolution.ReferenceAddedToProject -= HandleReferenceAddedToProject;
				currentSolution.ReferenceRemovedFromProject -= HandleReferenceRemovedFromProject;
			}
		}

		void HandleReferenceRemovedFromProject (object sender, ProjectReferenceEventArgs e)
		{
			if (e.ProjectReference.ReferenceType == ReferenceType.Project && e.ProjectReference.Reference == Name) {
				foreach (var f in Files) {
					var pf = e.Project.GetProjectFile (f.FilePath);
					if ((pf.Flags & ProjectItemFlags.DontPersist) != 0)
						e.Project.Files.Remove (pf.FilePath);
				}
			}
		}

		void HandleReferenceAddedToProject (object sender, ProjectReferenceEventArgs e)
		{
			if (e.ProjectReference.ReferenceType == ReferenceType.Project && e.ProjectReference.Reference == Name) {
				e.ProjectReference.Flags = ProjectItemFlags.DontPersist;
				foreach (var f in Files) {
					var cf = (ProjectFile) f.Clone ();
					cf.Flags |= ProjectItemFlags.DontPersist | ProjectItemFlags.Hidden;
					e.Project.Files.Add (cf);
					e.ProjectReference.SetItemsProjectPath (Path.ChangeExtension (FileName, ".projitems"));
				}
			}
		}
	}

	internal static class SharedAssetsProjectExtensions
	{
		public static string GetItemsProjectPath (this ProjectReference r)
		{
			return (string) r.ExtendedProperties ["MSBuild.SharedAssetsProject"];
		}

		public static void SetItemsProjectPath (this ProjectReference r, string path)
		{
			r.ExtendedProperties ["MSBuild.SharedAssetsProject"] = path;
		}
	}
}

