//
// ProjectReferenceMaintainer.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	/// <summary>
	/// Keeps a record of references added and removed from a project without applying the changes until the
	/// ApplyChanges is called. This allows the conversion of a reference being removed and then a new reference
	/// being added for the same reference but with a different hint path to be converted into an update of the
	/// original reference. This prevents references from being moved around the project file.
	///
	/// On an error NuGet will rollback the changes by applying the opposite actions (e.g. install => uninstall).
	/// There is no logic in the ProjectReferenceMaintainer class to handle a rollback
	/// since on a rollback an exception will be thrown by NuGet so any changes recorded in the
	/// ProjectReferenceMaintainer will not be applied since ApplyChanges will not be called.
	/// </summary>
	class ProjectReferenceMaintainer : IProjectReferenceMaintainer, IDisposable
	{
		NuGetProject project;
		IDotNetProject dotNetProject;
		IHasProjectReferenceMaintainer hasProjectReferenceMaintainer;
		IProjectReferenceMaintainer originalProjectReferenceMaintainer;
		List<ProjectReference> references;
		List<ProjectReference> removedReferences = new List<ProjectReference> ();
		List<ProjectReference> addedReferences = new List<ProjectReference> ();
		List<UpdatedProjectReference> updatedReferences = new List<UpdatedProjectReference> ();

		public ProjectReferenceMaintainer (NuGetProject project)
		{
			this.project = project;
			dotNetProject = project.GetDotNetProject ();

			hasProjectReferenceMaintainer = project as IHasProjectReferenceMaintainer;
			if (hasProjectReferenceMaintainer != null) {
				originalProjectReferenceMaintainer = hasProjectReferenceMaintainer.ProjectReferenceMaintainer;
				hasProjectReferenceMaintainer.ProjectReferenceMaintainer = this;
				references = new List<ProjectReference> (dotNetProject.References);
			}
		}

		public IEnumerable<ProjectReference> GetReferences ()
		{
			return references;
		}

		public void Dispose ()
		{
			if (hasProjectReferenceMaintainer == null)
				return;

			hasProjectReferenceMaintainer.ProjectReferenceMaintainer = originalProjectReferenceMaintainer;
		}

		public async Task ApplyChanges ()
		{
			if (hasProjectReferenceMaintainer == null)
				return;

			if (!AnyChanges ())
				return;

			await Runtime.RunInMainThread (() => ApplyChangesInternal ());
		}

		async Task ApplyChangesInternal ()
		{
			foreach (ProjectReference removedReference in removedReferences) {
				dotNetProject.References.Remove (removedReference);
			}

			foreach (ProjectReference addedReference in addedReferences) {
				dotNetProject.References.Add (addedReference);
			}

			foreach (UpdatedProjectReference updatedReference in updatedReferences) {
				updatedReference.ApplyChanges ();
			}

			await dotNetProject.SaveAsync ();
		}

		public Task RemoveReference (ProjectReference reference)
		{
			references.Remove (reference);
			removedReferences.Add (reference);

			return Task.CompletedTask;
		}

		public Task AddReference (ProjectReference reference)
		{
			references.Add (reference);

			ProjectReference removedReference = FindMatchingRemovedReference (reference);
			if (removedReference != null) {
				var updatedReference = new UpdatedProjectReference (removedReference, reference);
				updatedReferences.Add (updatedReference);
				removedReferences.Remove (removedReference);
			} else {
				addedReferences.Add (reference);
			}

			return Task.CompletedTask;
		}

		ProjectReference FindMatchingRemovedReference (ProjectReference reference)
		{
			return removedReferences.FirstOrDefault (removedReference => IsMatch (reference, removedReference));
		}

		static bool IsMatch (ProjectReference x, ProjectReference y)
		{
			return StringComparer.OrdinalIgnoreCase.Equals (x.Reference, y.Reference);
		}

		bool AnyChanges ()
		{
			return removedReferences.Any () || addedReferences.Any () || updatedReferences.Any ();
		}

		class UpdatedProjectReference
		{
			public UpdatedProjectReference (ProjectReference oldReference, ProjectReference newReference)
			{
				OldReference = oldReference;
				NewReference = newReference;
			}

			public ProjectReference OldReference { get; }
			public ProjectReference NewReference { get; }

			public void ApplyChanges ()
			{
				OldReference.HintPath = NewReference.HintPath;
			}
		}
	}
}
