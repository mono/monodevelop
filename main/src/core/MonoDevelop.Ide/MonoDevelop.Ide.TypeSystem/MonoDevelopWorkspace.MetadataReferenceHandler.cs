//
// MonoDevelopWorkspace.MetadataReferenceHandler.cs
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class MetadataReferenceHandler
		{
			static readonly string [] DefaultAssemblies = {
				typeof(string).Assembly.Location,                                // mscorlib
				typeof(System.Text.RegularExpressions.Regex).Assembly.Location,  // System
				typeof(System.Linq.Enumerable).Assembly.Location,                // System.Core
				typeof(System.Data.VersionNotFoundException).Assembly.Location,  // System.Data
				typeof(System.Xml.XmlDocument).Assembly.Location,                // System.Xml
			};

			readonly MonoDevelopMetadataReferenceManager manager;
			readonly ProjectDataMap projectMap;
			public MetadataReferenceHandler (MonoDevelopMetadataReferenceManager manager, ProjectDataMap projectMap)
			{
				this.manager = manager;
				this.projectMap = projectMap;
			}

			ImmutableArray<MonoDevelopMetadataReference> CreateDefaultMetadataReferences ()
			{
				var builder = ImmutableArray.CreateBuilder<MonoDevelopMetadataReference> (DefaultAssemblies.Length);

				foreach (var asm in DefaultAssemblies) {
					var metadataReference = manager.GetOrCreateMetadataReference (asm, MetadataReferenceProperties.Assembly);
					builder.Add (metadataReference);
				}
				return builder.MoveToImmutable ();
			}

			public async Task<(ImmutableArray<MonoDevelopMetadataReference>, ImmutableArray<ProjectReference>)> CreateReferences (MonoDevelop.Projects.Project proj, CancellationToken token)
			{
				var metadataReferences = await CreateMetadataReferences (proj, token).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return (ImmutableArray<MonoDevelopMetadataReference>.Empty, ImmutableArray<ProjectReference>.Empty);

				var projectReferences = await CreateProjectReferences (proj, token).ConfigureAwait (false);
				return (metadataReferences, projectReferences);
			}

			async Task<ImmutableArray<MonoDevelopMetadataReference>> CreateMetadataReferences (MonoDevelop.Projects.Project proj, CancellationToken token)
			{
				// create some default references for unsupported project types.
				if (!(proj is MonoDevelop.Projects.DotNetProject netProject)) {
					return CreateDefaultMetadataReferences ();
				}

				var data = new AddMetadataReferencesData {
					Result = new List<MonoDevelopMetadataReference> (),
					Project = netProject,
					Visited = new HashSet<string> (FilePath.PathComparer),
					ConfigurationSelector = IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default,
					Token = token,
				};

				if (!await AddMetadataAssemblyReferences (data))
					return ImmutableArray<MonoDevelopMetadataReference>.Empty;

				if (!AddMetadataProjectReferences (data))
					return ImmutableArray<MonoDevelopMetadataReference>.Empty;
				return data.Result.ToImmutableArray ();
			}

			class AddMetadataReferencesData
			{
				public List<MonoDevelopMetadataReference> Result;
				public MonoDevelop.Projects.DotNetProject Project;
				public MonoDevelop.Projects.ConfigurationSelector ConfigurationSelector;
				public HashSet<string> Visited;
				public CancellationToken Token;
			}

			async Task<bool> AddMetadataAssemblyReferences (AddMetadataReferencesData data)
			{
				try {
					var referencedAssemblies = await data.Project.GetReferencedAssemblies (data.ConfigurationSelector, false).ConfigureAwait (false);
					foreach (var file in referencedAssemblies) {
						if (data.Token.IsCancellationRequested)
							return false;

						if (!data.Visited.Add (file.FilePath))
							continue;

						var aliases = file.EnumerateAliases ().ToImmutableArray ();
						var metadataReference = manager.GetOrCreateMetadataReference (file.FilePath, new MetadataReferenceProperties (aliases: aliases));
						if (metadataReference != null)
							data.Result.Add (metadataReference);
					}

					return true;
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting referenced assemblies", e);
					// TODO: Check whether this should return false, I retained compat for now.
					return true;
				}
			}

			bool AddMetadataProjectReferences (AddMetadataReferencesData data)
			{
				try {
					var referencedProjects = data.Project.GetReferencedItems (data.ConfigurationSelector);
					foreach (var pr in referencedProjects) {
						if (data.Token.IsCancellationRequested)
							return false;

						if (!(pr is MonoDevelop.Projects.DotNetProject referencedProject) || !TypeSystemService.IsOutputTrackedProject (referencedProject))
							continue;

						var fileName = referencedProject.GetOutputFileName (data.ConfigurationSelector);
						if (!data.Visited.Add (fileName))
							continue;

						var metadataReference = manager.GetOrCreateMetadataReference (fileName, MetadataReferenceProperties.Assembly);
						if (metadataReference != null)
							data.Result.Add (metadataReference);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting referenced assemblies", e);
					// TODO: Check whether this should return false, I retained compat for now.
					return true;
				}

				return true;
			}

			async Task<ImmutableArray<ProjectReference>> CreateProjectReferences (MonoDevelop.Projects.Project p, CancellationToken token)
			{
				if (!(p is MonoDevelop.Projects.DotNetProject netProj))
					return ImmutableArray<ProjectReference>.Empty;

				List<MonoDevelop.Projects.AssemblyReference> references;
				try {
					var config = IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default;
					references = await netProj.GetReferences (config, token).ConfigureAwait (false);
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting referenced projects.", e);
					return ImmutableArray<ProjectReference>.Empty;
				};
				return CreateProjectReferencesFromAssemblyReferences (netProj, references).ToImmutableArray ();
			}

			IEnumerable<ProjectReference> CreateProjectReferencesFromAssemblyReferences (MonoDevelop.Projects.DotNetProject p, List<MonoDevelop.Projects.AssemblyReference> references)
			{
				var addedProjects = new HashSet<MonoDevelop.Projects.DotNetProject> ();

				foreach (var pr in references) {
					if (!pr.IsProjectReference || !pr.ReferenceOutputAssembly)
						continue;

					var referencedItem = pr.GetReferencedItem (p.ParentSolution);
					if (!(referencedItem is MonoDevelop.Projects.DotNetProject referencedProject))
						continue;

					if (!addedProjects.Add (referencedProject))
						continue;

					if (TypeSystemService.IsOutputTrackedProject (referencedProject))
						continue;

					var aliases = pr.EnumerateAliases ();
					yield return new ProjectReference (projectMap.GetOrCreateId (referencedProject, null), aliases.ToImmutableArray ());
				}
			}
		}
	}
}