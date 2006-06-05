/*
Copyright (C) 2006  Matthias Braun <matze@braunis.de>
					Scott Ellington <scott.ellington@gmail.com>
 
This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the
Free Software Foundation, Inc., 59 Temple Place - Suite 330,
Boston, MA 02111-1307, USA.
*/

using System;
using System.IO;
using System.Text;
using System.Collections;

using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	public class AutotoolsContext
	{
		string template_dir = Path.GetDirectoryName ( typeof ( AutotoolsContext ).Assembly.Location ) + "/";
		
		ArrayList autoconfConfigFiles = new ArrayList();
		Set referencedPackages = new Set();
		Set globalDllReferences = new Set();
		Set compilers = new Set ();

		public void AddRequiredPackage ( string pkg_name )
		{
			referencedPackages.Add (pkg_name);
		}

		public void AddAutoconfFile ( string file_name )
		{
			autoconfConfigFiles.Add( file_name );
		}

		public void AddCommandCheck ( string command_name )
		{
			//if ( !compilers.Contains ( setup ) ) 
			compilers.Add ( command_name );
		}

		public void AddReferencedDll ( string dll_name )
		{
			globalDllReferences.Add ( dll_name );
		}

		public IEnumerable GetAutoConfFiles ()
		{
			return autoconfConfigFiles;
		}

		public IEnumerable GetRequiredPackages ()
		{
			return referencedPackages;
		}

		public IEnumerable GetCommandChecks ()
		{
			return compilers;
		}

		public IEnumerable GetReferencedDlls ()
		{
			return globalDllReferences;
		}
		
		// TODO: add an extension point with which addins can implement 
		// autotools functionality.
		public static IMakefileHandler GetMakefileHandler ( CombineEntry entry )
		{
			if ( entry is Combine )
				return new SolutionMakefileHandler ();
			else if ( entry is Project )
				return new SimpleProjectMakefileHandler ();
			else
				throw new Exception ( "No known IMakefileHandler for type.");
		}
	
		// utility function for finding the correct order to process directories
		public ArrayList CalculateSubDirOrder ( Combine combine )
		{
			ArrayList resultOrder = new ArrayList();
			Set dependenciesMet = new Set();
			Set inResult = new Set();

			bool added;
			string notMet;
			do 
			{
				added = false;
				notMet = null;
				
				foreach (CombineEntry entry in combine.Entries) 
				{
					if (inResult.Contains(entry))
						continue;

					Set references;
					Set provides;
					if(entry is Project)
					{
						Project project = (Project) entry;

						references = GetReferencedProjects (project);
						provides = new Set();
						provides.Add(project.Name);
					} 
					else if (entry is Combine) 
						GetAllProjects ( (Combine) entry, out provides,	out references);
					else continue;

					if (dependenciesMet.ContainsSet (references) ) 
					{
						resultOrder.Add (entry);
						dependenciesMet.Union(provides);
						inResult.Add(entry);
						added = true;
					} 
					else notMet = entry.Name;
				}
			} while (added == true);

			if (notMet != null) 
				throw new Exception("Impossible to find a solution order that satisfies project references for '" + notMet + "'");

			return resultOrder;
		}

		public static string EscapeStringForAutomake (string Str) 
		{
			StringBuilder result = new StringBuilder();
			for(int i = 0; i < Str.Length; ++i) {
				char c = Str[i];
				if(!Char.IsLetterOrDigit(c) && c != '.' && c != '/' && c != '_' && c != '-')
					result.Append('\\');

				result.Append(c);
			}
			return result.ToString();
		}

		public Stream GetTemplateStream ( string id )
		{
			//return GetType().Assembly.GetManifestResourceStream(id); 
			return new FileStream (template_dir + id, FileMode.Open );
		}

		public static string GetPkgVar (string package)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (char c in package)
			{
				if ( char.IsLetterOrDigit (c) ) sb.Append ( char.ToUpper(c) );
				else if (c == '-' || c == '_' ) sb.Append ( '_' );
			}
			return sb.ToString ();	
		}

		// cache references
		Hashtable projectReferences = new Hashtable();		
		/**
		 * returns a set of all (monodevelop) projects that a give
		 * projects references
		 */
		Set GetReferencedProjects (Project project)
		{
			Set set = (Set) projectReferences[project];
			if(set != null)
				return set;

			set = new Set();

			foreach(ProjectReference reference in project.ProjectReferences) {
				if(reference.ReferenceType == ReferenceType.Project)
					set.Add(reference.Reference);
			}

			projectReferences[project] = set;
			return set;
		}

		// cache references
		Hashtable combineProjects = new Hashtable();
		Hashtable combineReferences = new Hashtable();
		/**
		 * returns a set of projects that a combine contains and a set of projects
		 * that are referenced from combine projects but not part of the combine
		 */
		void GetAllProjects (Combine combine, out Set projects, out Set references)
		{
			projects = (Set) combineProjects[combine];
			if(projects != null) {
				references = (Set) combineReferences[combine];
				return;
			}

			projects = new Set();
			references = new Set();
			foreach(CombineEntry entry in combine.Entries) 
			{
				if(entry is Project) 
				{
					Project project = (Project) entry;
					projects.Add(project.Name);
					references.Add(GetReferencedProjects(project));
				} 
				else if(entry is Combine) 
				{
					Set subProjects;
					Set subReferences;
					GetAllProjects((Combine) entry,
							out subProjects,
							out subReferences);
					projects.Union(subProjects);
					references.Union(subReferences);
				}
			}
			references.Without(projects);
			combineProjects[combine] = projects;
			combineReferences[combine] = references;
		}

		DotNetProjectConfiguration GetProjectConfig (Project project)
		{
			if (! (project is DotNetProject))
				return null;

			DotNetProject dotNetProject = (DotNetProject) project;

			DotNetProjectConfiguration config =
				(DotNetProjectConfiguration) dotNetProject.Configurations["Release"];

			return config;
		}

		public string GetProjectAssembly (Project project)
		{
			DotNetProjectConfiguration config = GetProjectConfig (project);		
			string assembly = project.GetRelativeChildPath (config.CompiledOutputName);
			return assembly;
		}
	}
}
