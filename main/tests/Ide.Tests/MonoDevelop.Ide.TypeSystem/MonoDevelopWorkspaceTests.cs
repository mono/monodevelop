//
// MonoDevelopWorkspaceTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class MonoDevelopWorkspaceTests : IdeTestBase
	{
		[Test]
		public async Task MetadataReferencesToFrameworkAssembliesAreProperlyFound ()
		{
			if (!IdeApp.IsInitialized)
				IdeApp.Initialize (new ProgressMonitor ());

			FilePath projFile = Util.GetSampleProject ("workspace-metadata-references", "workspace-metadata-references.sln");

			using (var sol = (MonoDevelop.Projects.Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), projFile)) {
				using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {

					DotNetProject mainProject = null, libraryProject = null, libraryProject461 = null;
					foreach (var project in sol.GetAllProjects ()) {
						if (project.Name == "workspace-metadata-references")
							mainProject = (DotNetProject)project;
						else if (project.Name == "library-project")
							libraryProject = (DotNetProject)project;
						else if (project.Name == "library-project-net461")
							libraryProject461 = (DotNetProject)project;
					}

					Assert.IsNotNull (mainProject);
					Assert.IsNotNull (libraryProject);

					// Also, add test for net461 to net47.
					await AddAssemblyReference (ws, libraryProject, mainProject, "System.Messaging");
					await AddAssemblyReference (ws, libraryProject461, mainProject, "System.ServiceModel");
				}
			}
		}

		static async Task AddAssemblyReference (MonoDevelopWorkspace ws, DotNetProject from, DotNetProject to, string name)
		{
			var manager = ws.MetadataReferenceManager;

			var assemblies = await from.GetReferencedAssemblies (IdeApp.Workspace?.ActiveConfiguration ?? ConfigurationSelector.Default);
			var messagingAsm = assemblies.Single (x => x.Metadata.GetValue<string> ("Filename") == name);
			var metadataReference = manager.GetOrCreateMetadataReference (messagingAsm.FilePath, MetadataReferenceProperties.Assembly);

			await FileWatcherService.Update ();

			var roslynProj = ws.GetProjectId (to);

			var oldRefs = ws.CurrentSolution.GetProject (roslynProj).MetadataReferences;
			var newRefs = oldRefs.ToImmutableArray ().Add (metadataReference.CurrentSnapshot);
			var newSolution = ws.CurrentSolution.WithProjectMetadataReferences (roslynProj, newRefs);

			Assert.IsTrue (ws.TryApplyChanges (newSolution));
			await ws.ProjectSaveTask;

			var reference = to.References.Single (x => x.Include == name);
			Assert.IsTrue (reference.HintPath.IsNull);
			Assert.AreEqual (ReferenceType.Package, reference.ReferenceType);
		}
	}
}
