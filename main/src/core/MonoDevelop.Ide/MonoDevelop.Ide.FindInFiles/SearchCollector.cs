// 
// SearchCollector.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang
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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.FindInFiles
{
	public class SearchCollector
	{
		
		public class FileList
		{
			public Project Project
			{
				get;
				private set;
			}

			public IProjectContent Content
			{
				get;
				private set;
			}

			public IEnumerable<FilePath> Files
			{
				get;
				private set;
			}

			public FileList (Project project, IProjectContent content, IEnumerable<FilePath> files)
			{
				Project = project;
				Content = content;
				Files = files;
			}
		}

		public static IEnumerable<Project> CollectProjects (Solution solution, IEnumerable<object> entities)
		{
			return new SearchCollector (solution, null, entities).CollectProjects ();
		}

		public static IEnumerable<FileList> CollectFiles (Project project, IEnumerable<object> entities)
		{
			return new SearchCollector (project.ParentSolution, project, entities).CollectFiles ();
		}

		public static IEnumerable<FileList> CollectFiles (Solution solution, IEnumerable<object> entities)
		{
			return new SearchCollector (solution, null, entities).CollectFiles ();
		}
		
		static IEnumerable<Project> GetAllReferencingProjects (Solution solution, string assemblyName)
		{
			return solution.GetAllProjects ().Where (
				project => TypeSystemService.GetCompilation (project).Assemblies.Any (a => a.AssemblyName == assemblyName));
		}

		static FileList CollectDeclaringFiles (IEntity entity, IEnumerable<string> fileNames)
		{
			var project = TypeSystemService.GetProject (entity);
			var paths = fileNames.Distinct().Select (p => (FilePath)p);
 			return new SearchCollector.FileList (project, TypeSystemService.GetProjectContext (project), paths);
		}
		
		public static FileList CollectDeclaringFiles (IEntity entity)
		{
			if (entity is ITypeDefinition)
				return CollectDeclaringFiles (entity, (entity as ITypeDefinition).Parts.Select (p => p.Region.FileName));
			if (entity is IMethod)
				return CollectDeclaringFiles (entity, (entity as IMethod).Parts.Select (p => p.Region.FileName));
			return CollectDeclaringFiles (entity, new [] { entity.Region.FileName });
		}
		
		Project searchProject;
		bool searchProjectAdded; // if the searchProject is added, we can stop collecting
		Solution solution;
		IEnumerable<object> entities;
		bool projectOnly; // only collect projects
		
		IDictionary<Project, ISet<string>> collectedFiles = new Dictionary<Project, ISet<string>> ();
		ISet<Project> collectedProjects = new HashSet<Project> ();
		
		ISet<string> searchedAssemblies = new HashSet<string> ();
		ISet<Project> searchedProjects = new HashSet<Project> ();

		/// <param name="searchProject">the project to search. use to null to search the whole solution</param>
		SearchCollector (Solution solution, Project searchProject, IEnumerable<object> entities)
		{
			this.solution = solution;
			this.searchProject = searchProject;
			this.entities = entities;
		}

		IEnumerable<Project> CollectProjects ()
		{
			projectOnly = true;
			foreach (var o in entities) {
				var entity = o as IEntity;
				if (entity == null)
					continue;
				Collect (TypeSystemService.GetProject (entity), entity);
			}
			return collectedProjects;
		}

		IEnumerable<FileList> CollectFiles ()
		{
			projectOnly = false;
			foreach (var o in entities) {
				if (o is INamespace) {
					Collect (null, null);
					continue;
				}


				var entity = o as IEntity;
				if (entity == null)
					continue;
				Collect (TypeSystemService.GetProject(entity), entity);

				if (searchProjectAdded) break;
			}
			foreach (var project in collectedProjects)
				yield return new FileList (project, TypeSystemService.GetProjectContext (project), project.Files.Select (f => f.FilePath));
			
			foreach (var files in collectedFiles)
				yield return new FileList (files.Key, TypeSystemService.GetProjectContext (files.Key), files.Value.Select (f => (FilePath)f));
		}

		void AddProject (Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");

			searchProjectAdded = (project == searchProject);

			// remove duplicate files
			if (collectedProjects.Add (project)) 
				collectedFiles.Remove (project);
		}

		void AddFiles (Project project, IEnumerable<string> files)
		{
			if (project == null)
				throw new ArgumentNullException ("project");

			if (collectedProjects.Contains (project))
				return;

			ISet<string> fileSet;
			if (!collectedFiles.TryGetValue (project, out fileSet)) {
				fileSet = new HashSet<string> ();
				collectedFiles[project] = fileSet;
			}

			foreach (var file in files)
				fileSet.Add (file);
		}

		void Collect (Project sourceProject, IEntity entity, bool searchInProject = false)
		{
			if (searchedProjects.Contains(sourceProject))
				return;
			
			if (searchProject != null && sourceProject != searchProject) {
				// searching for a entity not defined in the project
				AddProject (searchProject);
				return;
			}
			
			if (sourceProject == null) {
				if (entity == null) {
					foreach (var project in solution.GetAllProjects ())
						AddProject (project);
					return;
				}
				// entity is defined in a referenced assembly
				var assemblyName = entity.ParentAssembly.AssemblyName;
				if (!searchedAssemblies.Add (assemblyName)) 
					return;
				foreach (var project in GetAllReferencingProjects (solution, assemblyName))
					AddProject (project);

				return;
			}

			var declaringType = entity.DeclaringTypeDefinition;
			// TODO: possible optimization for protected
			switch (entity.Accessibility) {
			case Accessibility.Public:
			case Accessibility.Protected:
			case Accessibility.ProtectedOrInternal:
			case Accessibility.Internal:
			case Accessibility.ProtectedAndInternal:

				if (declaringType != null)
					Collect (sourceProject, entity.DeclaringTypeDefinition, searchInProject);
				else if (searchProject != null || searchInProject)
					AddProject (sourceProject);
				else {
					foreach (var project in ReferenceFinder.GetAllReferencingProjects (solution, sourceProject)) {
						if (entity.Accessibility == Accessibility.Internal || entity.Accessibility == Accessibility.ProtectedAndInternal) {
							if (!entity.ParentAssembly.InternalsVisibleTo (TypeSystemService.GetProjectContentWrapper (project).Compilation.MainAssembly))
								continue;
						}
						AddProject (project);
					}
				}
				break;
			default: // private
				if (projectOnly)
					AddProject (sourceProject);
				else if (declaringType != null)
					AddFiles (sourceProject, declaringType.Parts.Select (p => p.Region.FileName));
				break;
			}
		}
	}
}

