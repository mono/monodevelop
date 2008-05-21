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
		public bool CanDeploy (SolutionItem entry, MakefileType type)
		{
			return entry is SolutionFolder;
		}

		public Makefile Deploy (AutotoolsContext ctx, SolutionItem entry, IProgressMonitor monitor)
		{
			generateAutotools = ctx.MakefileType == MakefileType.AutotoolsMakefile;
			
			monitor.BeginTask ( GettextCatalog.GetString (
						"Creating {0} for Solution {1}",
						generateAutotools ? "Makefile.am" : "Makefile", entry.Name), 1 );

			Makefile solutionMakefile = new Makefile ();
			StringBuilder solutionTop = new StringBuilder ();

			try
			{
				SolutionFolder solutionFolder = (SolutionFolder) entry;
				string targetDirectory = solutionFolder.BaseDirectory;

				StringBuilder subdirs = new StringBuilder();
				subdirs.Append ("#Warning: This is an automatically generated file, do not edit!\n");

				if (!generateAutotools) {
					solutionTop.AppendFormat ("top_srcdir={0}\n", FileService.AbsoluteToRelativePath (
							entry.BaseDirectory, ctx.TargetSolution.BaseDirectory));
					solutionTop.Append ("include $(top_srcdir)/config.make\n");
					solutionTop.Append ("include $(top_srcdir)/Makefile.include\n");
					solutionTop.Append ("include $(top_srcdir)/rules.make\n\n");
					solutionTop.Append ("#include $(top_srcdir)/custom-hooks.make\n\n");
				}

				ArrayList children = new ArrayList ();
				foreach ( SolutionConfiguration config in solutionFolder.ParentSolution.Configurations )
				{
					if ( !ctx.IsSupportedConfiguration ( config.Id ) ) continue;
					
					if (generateAutotools)
						subdirs.AppendFormat ( "if {0}\n", "ENABLE_" + ctx.EscapeAndUpperConfigName (config.Id));
					else
						subdirs.AppendFormat ( "ifeq ($(CONFIG),{0})\n", ctx.EscapeAndUpperConfigName (config.Id));

					subdirs.Append (" SUBDIRS = ");
					
					foreach (SolutionItem ce in CalculateSubDirOrder (ctx, solutionFolder, config))
					{
						string baseDirectory;
						if (!(ce is SolutionEntityItem) && !(ce is SolutionFolder))
							continue;
						
						// Ignore projects which can't be deployed
						IMakefileHandler handler = AutotoolsContext.GetMakefileHandler (ce, ctx.MakefileType);
						if (handler == null)
							continue;
							
						baseDirectory = ce.BaseDirectory;
						
						if (solutionFolder.BaseDirectory == baseDirectory) {
							subdirs.Append (" . ");
						} else {
							if (!baseDirectory.StartsWith (solutionFolder.BaseDirectory) )
								throw new Exception ( GettextCatalog.GetString (
									"Child projects must be in sub-directories of their parent") );
							
							// add the subdirectory to the list
							string path = FileService.AbsoluteToRelativePath (targetDirectory, baseDirectory);
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
				foreach (SolutionItem ce in children)
				{
					IMakefileHandler handler = AutotoolsContext.GetMakefileHandler ( ce, ctx.MakefileType );
					Makefile makefile;
					string outpath;
					if ( handler != null && handler.CanDeploy ( ce, ctx.MakefileType ) )
					{
						ctx.RegisterBuiltProject (ce);
						makefile = handler.Deploy ( ctx, ce, monitor );

						if (targetDirectory == ce.BaseDirectory)
						{
							if (includedProject != null)
								throw new Exception ( GettextCatalog.GetString (
									"More than 1 project in the same directory as the top-level solution is not supported."));

							// project is in the solution directory
							string projectMakefileName = ce.Name + ".make";
							includedProject = String.Format ("include {0}", projectMakefileName);
							outpath = Path.Combine (targetDirectory, projectMakefileName);
							ctx.AddGeneratedFile (outpath);

							if (!generateAutotools)
								solutionMakefile.SetVariable ("EXTRA_DIST", projectMakefileName);
						} else {
							makefile.AppendToVariable ("EXTRA_DIST", generateAutotools ? String.Empty : "Makefile");
							outpath = Path.Combine (ce.BaseDirectory, "Makefile");
							if (generateAutotools) {
								ctx.AddAutoconfFile (outpath);
								outpath = outpath + ".am";
							} else {
								makefile.Append ("install: install-local\nuninstall: uninstall-local\nclean: clean-local\n");
								if (ce is SolutionFolder)
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

					if (solutionFolder.IsRoot) {
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
		List<SolutionItem> CalculateSubDirOrder (AutotoolsContext ctx, SolutionFolder folder, SolutionConfiguration config)
		{
			List<SolutionItem> resultOrder = new List<SolutionItem>();
			Set<SolutionItem> dependenciesMet = new Set<SolutionItem>();
			Set<SolutionItem> inResult = new Set<SolutionItem>();
			
			// We don't have to worry about projects built in parent combines
			dependenciesMet.Union (ctx.GetBuiltProjects ());

			bool added;
			string notMet;
			do 
			{
				added = false;
				notMet = null;
				
				List<SolutionItem> items = new List<SolutionItem> ();
				GetSubItems (items, folder);

				foreach (SolutionItem item in items) 
				{
					Set<SolutionItem> references, provides;
					
					if (inResult.Contains (item))
						continue;
					
					if (item is SolutionEntityItem)
					{
						SolutionEntityItem entry = (SolutionEntityItem) item;
						if (!config.BuildEnabledForItem (entry))
							continue;
						
						string mappedConf = config.GetMappedConfiguration (entry);
						if (mappedConf == null)
							continue;
						references = new Set<SolutionItem> ();
						provides = new Set<SolutionItem>();
						references.Union (entry.GetReferencedItems (mappedConf));
						provides.Add (entry);
					}
					else if (item is SolutionFolder) {
						GetAllProjects ((SolutionFolder) item, config, out provides, out references);
					}
					else
						continue;
	
					if (dependenciesMet.ContainsSet (references) ) 
					{
						resultOrder.Add (item);
						dependenciesMet.Union(provides);
						inResult.Add(item);
						added = true;
					} 
					else notMet = item.Name;
				}
			} while (added);

			if (notMet != null) 
				throw new Exception("Impossible to find a solution order that satisfies project references for '" + notMet + "'");

			return resultOrder;
		}
		
		void GetSubItems (List<SolutionItem> list, SolutionFolder folder)
		{
			// This method returns the subitems of a folder.
			// If a folder does not match a phisical folder, it will be ignored.
			
			foreach (SolutionItem item in folder.Items) {
				if (item is SolutionFolder) {
					if (item.BaseDirectory != folder.BaseDirectory)
						list.Add (item);
					else
						GetSubItems (list, (SolutionFolder) item);
				}
				else
					list.Add (item);
			}
		}

		// cache references
		Hashtable combineProjects = new Hashtable();
		Hashtable combineReferences = new Hashtable();
		/**
		 * returns a set of projects that a combine contains and a set of projects
		 * that are referenced from combine projects but not part of the combine
		 */
		void GetAllProjects (SolutionFolder folder, SolutionConfiguration config, out Set<SolutionItem> projects, out Set<SolutionItem> references)
		{
			List<SolutionItem> subitems = new List<SolutionItem> ();
			GetSubItems (subitems, folder);

			projects = (Set<SolutionItem>) combineProjects [folder];
			if (projects != null) {
				references = (Set<SolutionItem>) combineReferences [folder];
				return;
			}

			projects = new Set<SolutionItem>();
			references = new Set<SolutionItem>();
			
			foreach (SolutionItem item in subitems) 
			{
				if (item is SolutionEntityItem)
				{
					SolutionEntityItem entry = (SolutionEntityItem) item;
					if (!config.BuildEnabledForItem (entry))
						continue;
					string mappedConf = config.GetMappedConfiguration (entry);
					if (mappedConf == null)
						continue;
					projects.Add (entry);
					references.Union (entry.GetReferencedItems (mappedConf));
				}
				else if (item is SolutionFolder) 
				{
					Set<SolutionItem> subProjects;
					Set<SolutionItem> subReferences;
					GetAllProjects ((SolutionFolder)item, config, out subProjects, out subReferences);
					projects.Union (subProjects);
					references.Union (subReferences);
				}
			}
			
			references.Without (projects);
			combineProjects [folder] = projects;
			combineReferences [folder] = references;
		}
	}
}


