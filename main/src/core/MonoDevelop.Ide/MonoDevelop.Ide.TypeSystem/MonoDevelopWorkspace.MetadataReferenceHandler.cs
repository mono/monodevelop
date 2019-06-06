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

			public async Task<(ImmutableArray<MonoDevelopMetadataReference>, ImmutableArray<ProjectReference>)> CreateReferences (
				MonoDevelop.Projects.Project proj,
				string framework,
				CancellationToken token)
			{
				if (!(proj is MonoDevelop.Projects.DotNetProject netProject))
					return (CreateDefaultMetadataReferences (), ImmutableArray<ProjectReference>.Empty);

				var config = IdeApp.IsInitialized ? IdeApp.Workspace.ActiveConfiguration : MonoDevelop.Projects.ConfigurationSelector.Default;

				if (!string.IsNullOrEmpty (framework))
					config = new MonoDevelop.Projects.ItemFrameworkConfigurationSelector (config, framework);

				var data = new AddReferencesData {
					References = new List<MonoDevelopMetadataReference> (),
					ProjectReferences = new List<ProjectReference> (),
					Project = netProject,
					Visited = new HashSet<string> (FilePath.PathComparer),
					AddedProjects = new HashSet<MonoDevelop.Projects.DotNetProject> (),
					ConfigurationSelector = config,
					Token = token
				};

				if (!await AddReferences (data))
					return (ImmutableArray<MonoDevelopMetadataReference>.Empty, ImmutableArray<ProjectReference>.Empty);
				return (data.References.ToImmutableArray (), data.ProjectReferences.ToImmutableArray ());
			}

			class AddReferencesData
			{
				public List<MonoDevelopMetadataReference> References;
				public MonoDevelop.Projects.DotNetProject Project;
				public List<ProjectReference> ProjectReferences;
				public MonoDevelop.Projects.ConfigurationSelector ConfigurationSelector;
				public HashSet<string> Visited;
				public HashSet<MonoDevelop.Projects.DotNetProject> AddedProjects;
				public CancellationToken Token;
			}

			async Task<bool> AddReferences (AddReferencesData data)
			{
				try {
					var referencedAssemblies = await data.Project.GetReferencedAssemblies (data.ConfigurationSelector, true).ConfigureAwait (false);
					foreach (var file in referencedAssemblies) {
						if (file.IsProjectReference) {
							var referencedItem = file.GetReferencedItem (data.Project.ParentSolution);
							if (!(referencedItem is MonoDevelop.Projects.DotNetProject referencedProject))
								continue;

							if (!IdeApp.TypeSystemService.IsOutputTrackedProject (referencedProject)) {
								if (!file.ReferenceOutputAssembly)
									continue;

								if (!data.AddedProjects.Add (referencedProject))
									continue;

								string framework = file.HasSingleTargetFramework ? null : file.NearestTargetFramework;
								var projectReferenceAliases = file.EnumerateAliases ();
								var projectId = projectMap.GetOrCreateId (referencedProject, null, framework);
								var projectReference = new ProjectReference (projectId, projectReferenceAliases.ToImmutableArray ());
								data.ProjectReferences.Add (projectReference);

								continue;
							}
						}

						if (data.Token.IsCancellationRequested)
							return false;

						if (!data.Visited.Add (file.FilePath))
							continue;

						var aliases = file.EnumerateAliases ().ToImmutableArray ();
						var metadataReference = manager.GetOrCreateMetadataReference (file.FilePath, new MetadataReferenceProperties (aliases: aliases));
						if (metadataReference != null)
							data.References.Add (metadataReference);
					}

					return true;
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting referenced assemblies", e);
					// TODO: Check whether this should return false, I retained compat for now.
					return true;
				}
			}
		}
	}
}