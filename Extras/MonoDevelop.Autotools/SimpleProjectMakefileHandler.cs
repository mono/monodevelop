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
using System.Collections;
using System.Reflection;
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Autotools
{
	public class SimpleProjectMakefileHandler : IMakefileHandler
	{
		public bool CanDeploy ( CombineEntry entry )
		{
			Project project = entry as Project;
			if ( project == null ) return false;
			if ( FindSetupForProject ( project ) == null ) return false;
			return true;
		}

		ISimpleAutotoolsSetup FindSetupForProject ( Project project )
		{
			object[] items = Runtime.AddInService.GetTreeItems ("/Autotools/SimpleSetups");
			foreach ( ISimpleAutotoolsSetup setup in items)
			{
				if ( setup.CanDeploy ( project ) ) return setup;
			}
			return null;
		}

		public Makefile Deploy ( AutotoolsContext ctx, CombineEntry entry, IProgressMonitor monitor )
		{
			monitor.BeginTask ( GettextCatalog.GetString ("Creating Makefile.am for Project {0}", entry.Name), 1 );
			
			Makefile makefile = new Makefile ();
			try
			{
				if ( !CanDeploy ( entry ) ) 
					throw new Exception ( GettextCatalog.GetString ("Not a deployable project.") );

				Project project = entry as Project;
				TemplateEngine templateEngine = new TemplateEngine();			
				ISimpleAutotoolsSetup setup = FindSetupForProject ( project );
				
				Set wrapped_exes = new Set ();
				StringBuilder conf_vars = new StringBuilder ();
				foreach ( DotNetProjectConfiguration config in project.Configurations )
				{
					conf_vars.AppendFormat ("if ENABLE_{0}\n", config.Name.ToUpper () );
					string assembly = project.GetRelativeChildPath ( config.CompiledOutputName );

					conf_vars.AppendFormat ("ASSEMBLY_COMPILER_COMMAND = {0}\n",
							setup.GetCompilerCommand ( project, config.Name ) );
					conf_vars.AppendFormat ("ASSEMBLY_COMPILER_FLAGS = {0}\n",
							setup.GetCompilerFlags ( project, config.Name ) );

					// add check for compiler command in configure.ac
					ctx.AddCommandCheck ( setup.GetCompilerCommand ( project, config.Name ) );
					
					conf_vars.AppendFormat ( "ASSEMBLY = {0}\n", 
							AutotoolsContext.EscapeStringForAutomake (assembly) );

					string target;
					switch (config.CompileTarget) 
					{
						case CompileTarget.Exe:
							target = "exe";
							break;
						case CompileTarget.Library:
							target = "library";
							break;
						case CompileTarget.WinExe:
							target = "winexe";
							break;
						case CompileTarget.Module:
							target = "module";
							break;
						default:
							throw new Exception( GettextCatalog.GetString ("Unknown target {0}", config.CompileTarget ) );
					}
					conf_vars.AppendFormat ( "COMPILE_TARGET = {0}\n", target );

					if ( config.CompileTarget == CompileTarget.Exe || 
								config.CompileTarget == CompileTarget.WinExe  )
					{
						string assembly_name = Path.GetFileName ( assembly );
						string wrapper;
						if ( !wrapped_exes.Contains ( assembly_name ) )
						{
							wrapper = CreateExeWrapper ( ctx, 
									assembly,  
									Path.GetDirectoryName (project.FileName), 
									config.CommandLineParameters,
									monitor );
							wrapped_exes.Add ( assembly_name );
						}
						else wrapper = GetExeWrapperFromAssembly ( assembly );

						conf_vars.AppendFormat ( "ASSEMBLY_WRAPPER = {0}\n", wrapper );
						conf_vars.AppendFormat ( "ASSEMBLY_WRAPPER_IN = {0}.in\n", wrapper );
					}

					// for project references, we need a ref to the dll for the current configuration
					StringWriter projectReferences = new StringWriter();
					foreach (ProjectReference reference in project.ProjectReferences) 
					{
						if (reference.ReferenceType == ReferenceType.Project) 
						{
							Project refp = null;
							Combine c = project.RootCombine;

							if (c != null) refp = c.FindProject (reference.Reference);

							if (refp == null)
								throw new Exception ( GettextCatalog.GetString ("Couldn't find referenced project '{0}'", 
											reference.Reference ) );

							DotNetProjectConfiguration dnpc = refp.Configurations[config.Name] as DotNetProjectConfiguration;
							if ( dnpc == null )
								throw new Exception ( GettextCatalog.GetString ("Could not add reference to project '{0}'", refp.Name) );
							
							projectReferences.WriteLine (" \\");
							projectReferences.Write ("\t");
							projectReferences.Write ( project.GetRelativeChildPath ( dnpc.CompiledOutputName ) );
						} 
					}
					conf_vars.AppendFormat ( "PROJECT_REFERENCES = {0}\n", projectReferences.ToString() );
					conf_vars.AppendFormat ( "BUILD_DIR = {0}\n", project.GetRelativeChildPath ( config.OutputDirectory ) );
					conf_vars.Append ( "endif\n" );
				}
				templateEngine.Variables["CONFIG_VARS"] = conf_vars.ToString ();

				// grab pkg-config references
				StringWriter references = new StringWriter();
				Set pkgs = new Set();
				StringBuilder copy_dlls = new StringBuilder ();
				StringWriter dllReferences = new StringWriter();
				foreach(ProjectReference reference in project.ProjectReferences) 
				{
					if(reference.ReferenceType == ReferenceType.Gac) 
					{
						// Get pkg-config keys
						String pkg = Runtime.SystemAssemblyService.GetPackageFromFullName (reference.Reference);
						if (pkg != "MONO-SYSTEM") 
						{
							if ( pkgs.Contains(pkg) ) continue;
							pkgs.Add(pkg);

							references.WriteLine (" \\");
							references.Write ("\t$(");
							references.Write (AutotoolsContext.GetPkgConfigVariable(pkg));
							references.Write ("_LIBS)");
							ctx.AddRequiredPackage (pkg);
						} 
						else 
						{
							references.WriteLine (" \\");
							references.Write ("\t-r:");
							AssemblyName assembly = Runtime.SystemAssemblyService.ParseAssemblyName (reference.Reference);
							references.Write (assembly.Name);
							references.Write ("");
						}
					} 
					else if (reference.ReferenceType == ReferenceType.Assembly) 
					{
						string assemblyPath = Path.GetFullPath (reference.Reference);
						string libdll_path = ctx.AddReferencedDll ( assemblyPath );

						// use reference in local directory (make sure it is there)
						//string newPath = config.OutputDirectory + "/" + Path.GetFileName ( assemblyPath );
						//if ( !File.Exists ( newPath ) ) File.Copy ( assemblyPath , newPath );				
						//newPath = project.GetRelativeChildPath ( newPath );

						string newPath = "$(BUILD_DIR)/" + Path.GetFileName ( assemblyPath );
						copy_dlls.AppendFormat ( "	cp -f {0} {1}\n", project.GetRelativeChildPath (libdll_path), newPath );

						dllReferences.WriteLine (" \\");
						dllReferences.Write ("\t");
						dllReferences.Write ( newPath );
					} 
					else if (reference.ReferenceType == ReferenceType.Project) continue; // handled above
					else throw new Exception ( GettextCatalog.GetString  ("Project Reference Type {0} not support yet", 
								reference.ReferenceType.ToString() ) );
				}
				templateEngine.Variables["REFERENCES"] = references.ToString();
				templateEngine.Variables["COPY_DLLS"] = copy_dlls.ToString();
				templateEngine.Variables["DLL_REFERENCES"] =  dllReferences.ToString () ;
				templateEngine.Variables["WARNING"] = "Warning: This is an automatically generated file, do not edit!";

				/* Collect file groups: Such as resources, code files, etc...
				 * files are currently split into groups depending on their BuildAction setting
				 * Each group will be outputted like this in the Makefile:
				 *   GroupName = \
				 *     FILE1 \
				 *     FILE2 \
				 *     ...
				 */
				//TODO: this should probably be reorganized
				Hashtable groups = new Hashtable();
				foreach(ProjectFile projectFile in project.ProjectFiles) {
					string fileGroup = projectFile.BuildAction.ToString();

					ArrayList list = (ArrayList) groups[fileGroup];
					if(list == null) {
						list = new ArrayList();
						groups[fileGroup] = list;
					}

					// exclude directories from compilation
					if ( projectFile.BuildAction == BuildAction.Compile && 
							projectFile.Subtype != Subtype.Code ) 
						continue;

					list.Add(projectFile.RelativePath);
				}

				// write out various variables ( RESOURCES, EXTRAS, ... )
				foreach (string group in groups.Keys) 
				{
					ArrayList files = (ArrayList) groups [group];
					StringWriter gwriter = new StringWriter ();

					if (files.Count > 2) 
					{
						gwriter.WriteLine("\\");

						for(int i = 0; i < files.Count; ++i) 
						{
							string file = (string) files[i];
							file = project.GetRelativeChildPath (file);
							gwriter.Write ("\t" + AutotoolsContext.EscapeStringForAutomake (file));
							if (i+1 < files.Count)
								gwriter.Write(" \\");

							gwriter.WriteLine("");
						}
						gwriter.WriteLine("");
					} 
					else 
					{
						foreach(string file in files) 
						{
							string nfile = project.GetRelativeChildPath (file);
							gwriter.Write(" " + AutotoolsContext.EscapeStringForAutomake(nfile));
						}
					}

					templateEngine.Variables["GROUP_" + group] = gwriter.ToString();
				}

				// Create makefile
				Stream stream = ctx.GetTemplateStream ("Makefile.am.project.template");
				StreamReader reader = new StreamReader (stream);			                                          
				string txt = templateEngine.Process ( reader );
				reader.Close();
				makefile.Append ( txt );

				monitor.Step (1);
			}
			finally	{ monitor.EndTask (); }
			return makefile;
		}

		static string GetExeWrapperFromAssembly ( string assembly )
		{
			string basename = assembly;
			if (basename.EndsWith(".exe"))
				basename = basename.Substring(0, basename.Length-4);

			return Path.GetFileName(basename).ToLower();			
		}

		string CreateExeWrapper ( AutotoolsContext context , 
				string assembly, 
				string baseDir, 
				string parameters,
				IProgressMonitor monitor )
		{
			monitor.Log.WriteLine ( GettextCatalog.GetString ("Creating wrapper script for executable.") );

			string wrapperName = GetExeWrapperFromAssembly ( assembly );

			TemplateEngine templateEngine = new TemplateEngine();
			templateEngine.Variables["ASSEMBLY"] = Path.GetFileName(assembly);
			templateEngine.Variables["ARGS"] = parameters;

			Stream stream = context.GetTemplateStream ("exe.wrapper.in.template");

			string path = baseDir + "/" + wrapperName;

			StreamReader reader = new StreamReader ( stream );
			StreamWriter writer = new StreamWriter ( path + ".in");

			templateEngine.Process (reader, writer);

			writer.Close();
			reader.Close();

			context.AddAutoconfFile ( path );
			return wrapperName;
		}
	}
}

