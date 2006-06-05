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

using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Autotools
{
	public class SimpleProjectMakefileHandler : IMakefileHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
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

		public Makefile Deploy ( AutotoolsContext ctx, CombineEntry entry )
		{
			if ( !CanDeploy ( entry ) ) 
				throw new Exception ("Not a deployable project.");
			
			Project project = entry as Project;
			ISimpleAutotoolsSetup setup = FindSetupForProject ( project );

			Console.WriteLine("Exporting project: " + project.FileName);
			ctx.AddCommandCheck ( setup.GetCompilerCommand ( project ) );
			
			/* Collect file groups: Such as resources, code files, etc...
			 * files are currently split into groups depending on their BuildAction setting
			 * Each group will be outputted like this in the Makefile:
			 *   GroupName = \
			 *     FILE1 \
			 *     FILE2 \
			 *     ...
			 */
			Hashtable groups = new Hashtable();
			foreach(ProjectFile projectFile in project.ProjectFiles) {
				string fileGroup = projectFile.BuildAction.ToString();

				ArrayList list = (ArrayList) groups[fileGroup];
				if(list == null) {
					list = new ArrayList();
					groups[fileGroup] = list;
				}

				list.Add(projectFile.RelativePath);
			}

			TemplateEngine templateEngine = new TemplateEngine();			

			// Collect references
			StringWriter references = new StringWriter();
			StringWriter dllReferences = new StringWriter();
			StringWriter projectReferences = new StringWriter();
			Set pkgs = new Set();
			
			foreach(ProjectReference reference in project.ProjectReferences) 
			{
				if(reference.ReferenceType == ReferenceType.Gac) 
				{
					String pkg = Runtime.SystemAssemblyService.GetPackageFromFullName (reference.Reference);
					if(pkg != "MONO-SYSTEM") 
					{
						if(pkgs.Contains(pkg))
							continue;
						pkgs.Add(pkg);

						references.WriteLine (" \\");
						references.Write ("\t$(");
						references.Write (AutotoolsContext.GetPkgVar(pkg));
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
				else if(reference.ReferenceType == ReferenceType.Assembly) 
				{
					dllReferences.WriteLine (" \\");
					dllReferences.Write ("\t");
					string assemblyPath = Path.GetFullPath (reference.Reference);
					assemblyPath = project.GetRelativeChildPath (assemblyPath);
					dllReferences.Write (assemblyPath);
					ctx.AddReferencedDll (assemblyPath);
				} 
				else if (reference.ReferenceType == ReferenceType.Project) 
				{
					Project refp = null;
					Combine c = project.RootCombine;
					
					if (c != null) 
						refp = c.FindProject (reference.Reference);
					if(refp == null)
						throw new Exception ("Couldn't find referenced project '" + reference.Reference + "'");
					
					projectReferences.WriteLine (" \\");
					projectReferences.Write ("\t");
					projectReferences.Write ( ctx.GetProjectAssembly (refp) );
				} 
				else log.Info("ProjectReferenceType " + reference.ReferenceType.ToString() + " not handled yet");
			}
			templateEngine.Variables["REFERENCES"] = references.ToString();
			templateEngine.Variables["DLL_REFERENCES"] = dllReferences.ToString();
			templateEngine.Variables["PROJECT_REFERENCES"] = projectReferences.ToString();
			templateEngine.Variables["WARNING"] = "Warning: This is an automatically generated file, do not edit!";				templateEngine.Variables["PROJECT_TYPE"] = AutotoolsContext.EscapeStringForAutomake(project.ProjectType); 
			templateEngine.Variables["ASSEMBLY_COMPILER_COMMAND"] = setup.GetCompilerCommand (project);
			templateEngine.Variables["ASSEMBLY_COMPILER_FLAGS"] = setup.GetCompilerFlags (project);

			// .Net-specific stuff
			if(project is DotNetProject) 
			{
				DotNetProject dotNetProject = (DotNetProject) project;

				templateEngine.Variables["LANGUAGE"] = AutotoolsContext.EscapeStringForAutomake(dotNetProject.LanguageName);

				//FIXME: does not need to be DotNetProject?
				DotNetProjectConfiguration config =
					(DotNetProjectConfiguration) dotNetProject.Configurations["Release"];
				if(config == null)
					throw new Exception("No 'Release' configuration in project '"
							+ project.Name + "'");

				string assembly = ctx.GetProjectAssembly(project);
				templateEngine.Variables["ASSEMBLY"] = AutotoolsContext.EscapeStringForAutomake (assembly);
				switch(config.CompileTarget) 
				{
					case CompileTarget.Exe:
						templateEngine.Variables["COMPILE_TARGET"] = "exe";
						break;
					case CompileTarget.Library:
						templateEngine.Variables["COMPILE_TARGET"] = "library";
						break;
					case CompileTarget.WinExe:
						templateEngine.Variables["COMPILE_TARGET"] = "winexe";
						break;
					case CompileTarget.Module:
						templateEngine.Variables["COMPILE_TARGET"] = "module";
						break;
					default:
						throw new Exception("Unknown target " + config.CompileTarget);
				}

				if(config.CompileTarget == CompileTarget.Exe || 
						config.CompileTarget == CompileTarget.WinExe) 
				{
					string wrapper = CreateExeWrapper ( ctx, assembly,  Path.GetDirectoryName(project.FileName) );
					templateEngine.Variables["ASSEMBLY_WRAPPER"] = wrapper;
					templateEngine.Variables["ASSEMBLY_WRAPPER_IN"] = wrapper + ".in";  	
				}
			}

			// write out various variables ( RESOURCES, EXTRAS, ... )
			foreach(string group in groups.Keys) 
			{
				ArrayList files = (ArrayList) groups[group];
				StringWriter gwriter = new StringWriter();

				if(files.Count > 2) 
				{
					gwriter.WriteLine("\\");

					for(int i = 0; i < files.Count; ++i) 
					{
						string file = (string) files[i];
						file = project.GetRelativeChildPath (file);
						gwriter.Write("\t" + AutotoolsContext.EscapeStringForAutomake(file));
						if(i+1 < files.Count)
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

			Stream stream = ctx.GetTemplateStream ("Makefile.am.project.template");
			StreamReader reader = new StreamReader (stream);			                                          
			string txt = templateEngine.Process ( reader );
			
			reader.Close();

			Makefile makefile = new Makefile ();
			makefile.Append ( txt );
			return makefile;
		}

		string CreateExeWrapper ( AutotoolsContext context , string assembly, string baseDir )
		{
			string basename = assembly;
			
			if(basename.EndsWith(".exe"))
				basename = basename.Substring(0, basename.Length-4);
			string wrapperName = Path.GetFileName(basename).ToLower();			


			TemplateEngine templateEngine = new TemplateEngine();
			templateEngine.Variables["ASSEMBLY"] = Path.GetFileName(assembly);

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
