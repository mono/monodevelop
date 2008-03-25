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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Autotools
{
	public class SolutionMakefileHandler : IMakefileHandler
	{
		bool generateAutotools = true;

		// Recurses into children and tests if they are deployable.
		public bool CanDeploy (CombineEntry entry, MakefileType type)
		{
			return entry is Combine;
		}

		public Makefile Deploy ( AutotoolsContext ctx, CombineEntry entry, IProgressMonitor monitor )
		{
			generateAutotools = ctx.MakefileType == MakefileType.AutotoolsMakefile;
			
			monitor.BeginTask ( GettextCatalog.GetString (
						"Creating {0} for Solution {1}",
						generateAutotools ? "Makefile.am" : "Makefile", entry.Name), 1 );

			Makefile solutionMakefile = new Makefile ();
			StringBuilder solutionTop = new StringBuilder ();

			try
			{
				Combine combine = entry as Combine;

				StringBuilder subdirs = new StringBuilder();
				subdirs.Append ("#Warning: This is an automatically generated file, do not edit!\n");

				if (!generateAutotools) {
					solutionTop.AppendFormat ("top_srcdir={0}\n", FileService.AbsoluteToRelativePath (
							entry.BaseDirectory, ctx.TargetCombine.BaseDirectory));
					solutionTop.Append ("include $(top_srcdir)/config.make\n");
					solutionTop.Append ("include $(top_srcdir)/Makefile.include\n");
					solutionTop.Append ("include $(top_srcdir)/rules.make\n\n");
					solutionTop.Append ("#include $(top_srcdir)/custom-hooks.make\n\n");
				}

				ArrayList children = new ArrayList ();
				foreach ( CombineConfiguration config in combine.Configurations )
				{
					if ( !ctx.IsSupportedConfiguration ( config.Name ) ) continue;
					
					if (generateAutotools)
						subdirs.AppendFormat ( "if {0}\n", "ENABLE_" + ctx.EscapeAndUpperConfigName (config.Name));
					else
						subdirs.AppendFormat ( "ifeq ($(CONFIG),{0})\n", ctx.EscapeAndUpperConfigName (config.Name));

					subdirs.Append (" SUBDIRS = ");
					
					foreach (CombineEntry ce in CalculateSubDirOrder (ctx, config))
					{
						if (combine.BaseDirectory == ce.BaseDirectory) {
							subdirs.Append (" . ");
						} else {
							if ( !ce.BaseDirectory.StartsWith (combine.BaseDirectory) )
								throw new Exception ( GettextCatalog.GetString (
									"Child projects / solutions must be in sub-directories of their parent") );
							
							// add the subdirectory to the list
							string path = Path.GetDirectoryName (ce.RelativeFileName);
							if (path.StartsWith ("." + Path.DirectorySeparatorChar) )
								path = path.Substring (2);
							subdirs.Append (" ");
							subdirs.Append ( AutotoolsContext.EscapeStringForAutomake (path) );
						}

						if (!children.Contains (ce))
							children.Add ( ce );
					}
					subdirs.Append ( "\nendif\n" );
				}
				solutionTop.Append ( subdirs.ToString () );

				string includedProject = null;

				// deploy recursively
				foreach ( CombineEntry ce in children )
				{
					IMakefileHandler handler = AutotoolsContext.GetMakefileHandler ( ce, ctx.MakefileType );
					Makefile makefile;
					string outpath;
					if ( handler != null && handler.CanDeploy ( ce, ctx.MakefileType ) )
					{
						if (ce is Project)
							ctx.RegisterBuiltProject (ce.Name);
						makefile = handler.Deploy ( ctx, ce, monitor );
						if (combine.BaseDirectory == ce.BaseDirectory) {
							if (includedProject != null)
								throw new Exception ( GettextCatalog.GetString (
									"More than 1 project in the same directory as the top-level solution is not supported."));

							// project is in the solution directory
							string projectMakefileName = ce.Name + ".make";
							includedProject = String.Format ("include {0}", projectMakefileName);
							outpath = Path.Combine (Path.GetDirectoryName(ce.FileName), projectMakefileName);
							ctx.AddGeneratedFile (outpath);

							if (!generateAutotools)
								solutionMakefile.SetVariable ("EXTRA_DIST", projectMakefileName);
						} else {
							makefile.AppendToVariable ("EXTRA_DIST", generateAutotools ? String.Empty : "Makefile");
							outpath = Path.Combine (Path.GetDirectoryName(ce.FileName), "Makefile");
							if (generateAutotools) {
								ctx.AddAutoconfFile (outpath);
								outpath = outpath + ".am";
							} else {
								makefile.Append ("install: install-local\nuninstall: uninstall-local\nclean: clean-local\n");
								if (ce is Combine)
									//non TargetCombine
									makefile.Append ("dist-local: dist-local-recursive\n");
								else
									makefile.Append ("include $(top_srcdir)/rules.make\n");
							}
							ctx.AddGeneratedFile (outpath);
						}

						StreamWriter writer = new StreamWriter (outpath);
						makefile.Write ( writer );
						writer.Close ();
					}
					else {
						monitor.Log .WriteLine("Project '{0}' skipped.", ce.Name); 
					}
				}

				if (includedProject != null) {
					solutionTop.Append (GettextCatalog.GetString ("\n# Include project specific makefile\n"));
					solutionTop.Append (includedProject);
				}

				if (generateAutotools) {
					solutionMakefile.Append (solutionTop.ToString ());
				} else {
					TemplateEngine templateEngine = new TemplateEngine ();
					templateEngine.Variables ["MAKEFILE_SOLUTION_TOP"] = solutionTop.ToString ();

					Stream stream = ctx.GetTemplateStream ("Makefile.solution.template");
					StreamReader reader = new StreamReader (stream);
					StringWriter sw = new StringWriter ();

					templateEngine.Process (reader, sw);
					reader.Close ();

					solutionMakefile.Append (sw.ToString ());

					if (entry == ctx.TargetCombine) {
						// Emit dist and distcheck targets only for TargetCombine
						reader = new StreamReader (Path.Combine (ctx.TemplateDir, "make-dist.targets"));
						solutionMakefile.Append (reader.ReadToEnd ());
						reader.Close ();
					}
				}

				monitor.Step (1);
			}
			finally
			{
				monitor.EndTask ();
			}
			return solutionMakefile;
		}

		// utility function for finding the correct order to process directories
		List<CombineEntry> CalculateSubDirOrder (AutotoolsContext ctx, CombineConfiguration config)
		{
			List<CombineEntry> resultOrder = new List<CombineEntry>();
			Set<string> dependenciesMet = new Set<string>();
			Set<CombineEntry> inResult = new Set<CombineEntry>();
			
			// We don't have to worry about projects built in parent combines
			dependenciesMet.Union (ctx.GetBuiltProjects ());

			bool added;
			string notMet;
			do 
			{
				added = false;
				notMet = null;

				foreach (CombineConfigurationEntry centry in config.Entries) 
				{
					if ( !centry.Build ) continue;
					
					CombineEntry entry = centry.Entry;
					
					if ( inResult.Contains (entry) ) continue;

					Set<string> references, provides;
					if (entry is Project)
					{
						Project project = entry as Project;

						references = GetReferencedProjects (project);
						provides = new Set<string>();
						provides.Add(project.Name);
					} 
					else if (entry is Combine) 
					{
						CombineConfiguration cc = (entry as Combine).Configurations[config.Name] as CombineConfiguration;
						if ( cc == null ) continue;
						GetAllProjects ( cc, out provides, out references);
					}
					else {
						if (!resultOrder.Contains (entry))
							resultOrder.Add (entry);
						continue;
					}

					if (dependenciesMet.ContainsSet (references) ) 
					{
						resultOrder.Add (entry);
						dependenciesMet.Union(provides);
						inResult.Add(entry);
						added = true;
					} 
					else notMet = entry.Name;
				}
			} while (added);

			if (notMet != null) 
				throw new Exception("Impossible to find a solution order that satisfies project references for '" + notMet + "'");

			return resultOrder;
		}

		// cache references
		Hashtable projectReferences = new Hashtable();		
		/**
		 * returns a set of all monodevelop projects that a give
		 * projects references
		 */
		Set<string> GetReferencedProjects (Project project)
		{
			Set<string> set = (Set<string>) projectReferences [project];
			if (set != null) return set;

			set = new Set<string>();

			foreach (ProjectReference reference in project.ProjectReferences) 
			{
				if (reference.ReferenceType == ReferenceType.Project)
					set.Add (reference.Reference);
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
		void GetAllProjects (CombineConfiguration config, out Set<string> projects, out Set<string> references)
		{
			projects = (Set<string>) combineProjects [config];
			if(projects != null) 
			{
				references = (Set<string>) combineReferences [config];
				return;
			}

			projects = new Set<string>();
			references = new Set<string>();
			
			foreach (CombineConfigurationEntry centry in config.Entries) 
			{
				if ( !centry.Build ) continue;
				
				CombineEntry entry = centry.Entry;
				if (entry is Project) 
				{
					Project project = entry as Project;
					projects.Add (project.Name);
					references.Union ( GetReferencedProjects (project) );
				} 
				else if (entry is Combine) 
				{
					Set<string> subProjects;
					Set<string> subReferences;
					
					CombineConfiguration cc = (entry as Combine).Configurations[config.Name] as CombineConfiguration;
					if ( cc == null ) continue;
					GetAllProjects ( cc, out subProjects, out subReferences);

					projects.Union (subProjects);
					references.Union (subReferences);
				}
			}
			
			references.Without (projects);
			combineProjects [config] = projects;
			combineReferences [config] = references;
		}
	}
}


