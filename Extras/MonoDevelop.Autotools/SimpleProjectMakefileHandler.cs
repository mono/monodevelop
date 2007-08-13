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
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Deployment;

namespace MonoDevelop.Autotools
{
	public class SimpleProjectMakefileHandler : IMakefileHandler
	{
		string resourcedir = "resources";
		bool generateAutotools = true;

		public bool CanDeploy (CombineEntry entry, MakefileType type)
		{
			Project project = entry as Project;
			if ( project == null ) return false;
			if ( FindSetupForProject ( project ) == null ) return false;
			return true;
		}

		ISimpleAutotoolsSetup FindSetupForProject ( Project project )
		{
			object[] items = AddinManager.GetExtensionObjects ("/MonoDevelop/Autotools/SimpleSetups", typeof(ISimpleAutotoolsSetup));
			foreach ( ISimpleAutotoolsSetup setup in items)
			{
				if ( setup.CanDeploy ( project ) ) return setup;
			}
			return null;
		}

		public Makefile Deploy ( AutotoolsContext ctx, CombineEntry entry, IProgressMonitor monitor )
		{
			generateAutotools = ctx.MakefileType == MakefileType.AutotoolsMakefile;
			
			monitor.BeginTask ( GettextCatalog.GetString (
						"Creating {0} for Project {1}",
						generateAutotools ? "Makefile.am" : "Makefile", entry.Name), 1 );
			
			Makefile makefile = new Makefile ();
			try
			{
				if ( !CanDeploy (entry, generateAutotools ? MakefileType.AutotoolsMakefile : MakefileType.SimpleMakefile) ) 
					throw new Exception ( GettextCatalog.GetString ("Not a deployable project.") );

				Project project = entry as Project;
				TemplateEngine templateEngine = new TemplateEngine();			
				ISimpleAutotoolsSetup setup = FindSetupForProject ( project );
		
				// store all refs for easy access
				Set<SystemPackage> pkgs = new Set<SystemPackage>();
				
				// strings for variables
				StringWriter references = new StringWriter();
				StringWriter dllReferences = new StringWriter();
				
				// grab pkg-config references
				foreach (ProjectReference reference in project.ProjectReferences) 
				{
					if (reference.ReferenceType == ReferenceType.Gac) 
					{
						// Get pkg-config keys
						SystemPackage pkg = Runtime.SystemAssemblyService.GetPackageFromFullName (reference.Reference);
						if (pkg != null && !pkg.IsCorePackage) 
						{
							if ( pkgs.Contains(pkg) ) continue;
							pkgs.Add(pkg);

							references.WriteLine (" \\");
							if (generateAutotools) {
								references.Write ("\t$(");
								references.Write (AutotoolsContext.GetPkgConfigVariable(pkg.Name));
								references.Write ("_LIBS)");
							} else {
								references.Write ("\t-pkg:{0}", pkg.Name);
							}
							ctx.AddRequiredPackage (pkg);
						} 
						else 
						{
							references.WriteLine (" \\");
							AssemblyName assembly = Runtime.SystemAssemblyService.ParseAssemblyName (reference.Reference);
							references.Write ("\t" + assembly.Name);
							references.Write ("");
						}
					} 
					else if (reference.ReferenceType == ReferenceType.Assembly) 
					{
						string assemblyPath = Path.GetFullPath (reference.Reference);

						dllReferences.WriteLine (" \\");
						dllReferences.Write ("\t");

						ctx.AddGlobalReferencedFile (Runtime.FileService.AbsoluteToRelativePath (
							Path.GetFullPath (ctx.BaseDirectory), assemblyPath));
						dllReferences.Write (Runtime.FileService.AbsoluteToRelativePath (
							entry.BaseDirectory, assemblyPath));

					} 
					else if (reference.ReferenceType == ReferenceType.Project)
						continue; // handled elsewhere
					else
						throw new Exception ( GettextCatalog.GetString  ("Project Reference Type {0} not supported yet", 
								reference.ReferenceType.ToString() ) );
				}
				templateEngine.Variables["REFERENCES"] = references.ToString();
				templateEngine.Variables["DLL_REFERENCES"] =  dllReferences.ToString () ;
				templateEngine.Variables["WARNING"] = "Warning: This is an automatically generated file, do not edit!";

				DotNetProject dotnetProject = entry as DotNetProject;
				if (dotnetProject != null) {
					if (dotnetProject.ClrVersion == ClrVersion.Net_2_0)
						templateEngine.Variables ["RESGEN"] = "resgen2";
					else
						templateEngine.Variables ["RESGEN"] = "resgen";
				}
				
				// grab all project files
				StringBuilder files = new StringBuilder ();
				StringBuilder res_files = new StringBuilder ();
				StringBuilder extras = new StringBuilder ();
				StringBuilder datafiles = new StringBuilder ();
				string pfpath = null;
				foreach (ProjectFile projectFile in project.ProjectFiles) 
				{
					pfpath = (PlatformID.Unix == Environment.OSVersion.Platform) ? projectFile.RelativePath : projectFile.RelativePath.Replace("\\","/");
					pfpath = Runtime.FileService.NormalizeRelativePath (pfpath);
					switch ( projectFile.BuildAction )
					{
						case BuildAction.Compile:
							
							if ( projectFile.Subtype != Subtype.Code ) continue;
							files.AppendFormat ( "\\\n\t{0} ", pfpath );
							break;

						case BuildAction.Nothing:
							
							extras.AppendFormat ( "\\\n\t{0} ", pfpath );
							break;

						case BuildAction.EmbedAsResource:

							if ( !projectFile.FilePath.StartsWith ( ctx.BaseDirectory ) )
							{
								// file is not within directory hierarchy, copy it in
								string rdir = Path.Combine (Path.GetDirectoryName (project.FileName), resourcedir);
								if ( !Directory.Exists ( rdir ) ) Directory.CreateDirectory ( rdir );
								string newPath = Path.Combine (rdir, Path.GetFileName ( projectFile.FilePath ));
								Runtime.FileService.CopyFile ( projectFile.FilePath, newPath ) ;
								pfpath = (PlatformID.Unix == Environment.OSVersion.Platform) ? project.GetRelativeChildPath (newPath) : project.GetRelativeChildPath (newPath).Replace("\\","/");
								pfpath = Runtime.FileService.NormalizeRelativePath (pfpath);
							}
							if (!String.IsNullOrEmpty (projectFile.ResourceId) && projectFile.ResourceId != Path.GetFileName (pfpath))
								res_files.AppendFormat ( "\\\n\t{0},{1} ", pfpath, projectFile.ResourceId);
							else
								res_files.AppendFormat ( "\\\n\t{0} ", pfpath);
 
							break;

						case BuildAction.FileCopy:
						
							datafiles.AppendFormat ("\\\n\t{0} ", pfpath);
							break;
					}
				}
				
				// Handle files to be deployed
				Hashtable deployDirs = new Hashtable ();
				Hashtable deployFileVars = new Hashtable ();
				Dictionary<string, DeployFile> uniqueDeployFiles = new Dictionary<string, DeployFile> ();
				ArrayList builtFiles = new ArrayList ();
				
				StringBuilder deployFileCopyVars = new StringBuilder ();
				StringBuilder deployFileCopyTargets = new StringBuilder ();

				//used only for simple makefile generation
				StringBuilder templateFilesTargets = null;
				StringBuilder installTarget = null;
				StringBuilder installDeps = null;
				List<string> installDirs = null;

				StringBuilder uninstallTarget = null;

				if (!generateAutotools) {
					templateFilesTargets = new StringBuilder ();
					installTarget = new StringBuilder ();
					uninstallTarget = new StringBuilder ();
					installDeps = new StringBuilder ();
					installDirs = new List<string> ();

					string programFilesDir = ctx.DeployContext.GetDirectory (TargetDirectory.ProgramFiles);
					//FIXME:temp
					programFilesDir = programFilesDir.Replace ("@prefix@", "$(prefix)");
					programFilesDir = programFilesDir.Replace ("@PACKAGE@", "$(PACKAGE)");
					installDirs.Add (programFilesDir);
					installTarget.AppendFormat ("\tmkdir -p {0}\n", programFilesDir);
					installTarget.AppendFormat ("\tcp $(ASSEMBLY) $(ASSEMBLY).mdb {0}\n", programFilesDir);

					//remove dir?
					uninstallTarget.AppendFormat ("\trm -f {0}/$(notdir $(ASSEMBLY))\n", programFilesDir);
					uninstallTarget.AppendFormat ("\trm -f {0}/$(notdir $(ASSEMBLY)).mdb\n", programFilesDir);

					installDeps.Append (" $(ASSEMBLY) $(ASSEMBLY).mdb");
				}

				DeployFileCollection deployFiles = DeployService.GetDeployFiles (ctx.DeployContext, new CombineEntry[] { project });
				foreach (DeployFile dfile in deployFiles) {
					if (dfile.SourcePath == project.GetOutputFileName ())
						continue;

					// GetDeployFiles can return duplicates, ignore them
					string key = dfile.SourcePath + dfile.RelativeTargetPath;
					if (uniqueDeployFiles.ContainsKey (key))
						continue;
					uniqueDeployFiles [key] = dfile;

					string fname = null;
					
					string deployVar = GetDeployVar (deployFileVars, Path.GetFileName (dfile.RelativeTargetPath));
					
					if (dfile.ContainsPathReferences) {
						// If the file is a template, create a .in file for it.
						fname = Path.Combine (project.BaseDirectory, Path.GetFileName (dfile.RelativeTargetPath));
						string infname = fname + ".in";
						if (File.Exists (infname) && project.IsFileInProject (infname)) {
							string datadir = Path.Combine (project.BaseDirectory, "data");
							if (!Directory.Exists (datadir))
								Directory.CreateDirectory (datadir);
							infname = Path.Combine (datadir, Path.GetFileName (dfile.RelativeTargetPath) + ".in");
						}
						File.Copy (dfile.SourcePath, infname, true);
						extras.AppendFormat ( "\\\n\t{0} ", Runtime.FileService.AbsoluteToRelativePath (project.BaseDirectory, infname));
						ctx.AddAutoconfFile (fname);
						ctx.AddGeneratedFile (fname + ".in");
						fname = ctx.GetRelativePath (project, fname, true);

						if (!generateAutotools) {
							infname = fname + ".in";
							//FIXME: this should probably be done in configure script itself
							templateFilesTargets.AppendFormat ("{0}: {1} $(top_srcdir)/config.make\n", fname, infname);
							templateFilesTargets.AppendFormat (
								"\tsed -e \"s,@prefix@,$(prefix),\" -e \"s,@PACKAGE@,$(PACKAGE),\" < {1} > {0}\n",
								fname, infname);
						}
					} else {
						fname = Runtime.FileService.AbsoluteToRelativePath (
								Path.GetFullPath (project.BaseDirectory),
								Path.GetFullPath (dfile.SourcePath));
					}
					
					string targetDeployFile = "$(BUILD_DIR)" + Path.DirectorySeparatorChar + Path.GetFileName (dfile.RelativeTargetPath); 
					builtFiles.Add (Path.GetFileName (dfile.RelativeTargetPath));
					string srcDeployFile = fname;

					deployFileCopyVars.AppendFormat ("{0} = {1}\n", deployVar, targetDeployFile);
					deployFileCopyTargets.AppendFormat ("$({0}): {1}\n", deployVar, srcDeployFile);
					deployFileCopyTargets.AppendFormat ("\tmkdir -p $(BUILD_DIR)\n");
					deployFileCopyTargets.AppendFormat ("\tcp '$<' '$@'\n");
					if (!generateAutotools && (dfile.FileAttributes & DeployFileAttributes.Executable) != 0)
						deployFileCopyTargets.AppendFormat ("\tchmod u+x '$@'\n");
					deployFileCopyTargets.Append ("\n");

					switch (dfile.TargetDirectoryID) {
						case TargetDirectory.Gac:
							// TODO
							break;
						default:
							string var;
							if (dfile.TargetDirectoryID != TargetDirectory.Binaries) {
								string ddir = Runtime.FileService.NormalizeRelativePath (Path.GetDirectoryName (dfile.RelativeTargetPath).Trim ('/',' '));
								if (ddir.Length > 0)
									ddir = "/" + ddir;
								var = ctx.GetDeployDirectoryVar (dfile.TargetDirectoryID + ddir);
							}
							else
								var = "BINARIES";
								
							StringBuilder sb = (StringBuilder) deployDirs [var];
							if (sb == null) {
								sb = new StringBuilder ();
								deployDirs [var] = sb;
							}
							sb.AppendFormat ("\\\n\t$({0}) ", deployVar);
							break;
					}

					if (!generateAutotools) {
						string installDir = ctx.DeployContext.GetDirectory (dfile.TargetDirectoryID);
						//FIXME: temp
						installDir = installDir.Replace ("@prefix@", "$(prefix)");
						installDir = installDir.Replace ("@PACKAGE@", "$(PACKAGE)");

						if (!installDirs.Contains (installDir))
							installTarget.AppendFormat ("\tmkdir -p {0}\n", installDir);

						installTarget.AppendFormat ("\tcp $({0}) {1}\n", deployVar, installDir);
						installDeps.AppendFormat (" $({0})", deployVar);

						uninstallTarget.AppendFormat ("\trm -f {0}/$(notdir $({1}))\n", installDir, deployVar);
					}
				}
				
				if (!generateAutotools) {
					installTarget.Insert (0, String.Format ("install-local:{0}\n", installDeps.ToString ()));
					uninstallTarget.Insert (0, String.Format ("uninstall-local:{0}\n", installDeps.ToString ()));
				}

				string vars = "";
				foreach (string s in deployDirs.Keys)
					vars += "$(" + s + ") ";
				
				templateEngine.Variables["DEPLOY_FILE_VARS"] = vars;
				templateEngine.Variables["COPY_DEPLOY_FILES_VARS"] = deployFileCopyVars.ToString();
				templateEngine.Variables["COPY_DEPLOY_FILES_TARGETS"] = deployFileCopyTargets.ToString();

				templateEngine.Variables["FILES"] = files.ToString();
				templateEngine.Variables["RESOURCES"] = res_files.ToString();
				templateEngine.Variables["EXTRAS"] = extras.ToString();
				templateEngine.Variables["DATA_FILES"] = datafiles.ToString();

				if (!generateAutotools) {
					templateEngine.Variables["CLEANFILES"] = vars;
					templateEngine.Variables["TEMPLATE_FILES_TARGETS"] = templateFilesTargets.ToString();
					templateEngine.Variables["INSTALL_TARGET"] = installTarget.ToString();
					templateEngine.Variables["UNINSTALL_TARGET"] = uninstallTarget.ToString();
				}
				
				// handle configuration specific variables
				StringBuilder conf_vars = new StringBuilder ();

				if (!generateAutotools) {
					conf_vars.AppendFormat ("srcdir=.\n");
					conf_vars.AppendFormat ("top_srcdir={0}\n\n",
						Runtime.FileService.AbsoluteToRelativePath (project.BaseDirectory, ctx.TargetCombine.BaseDirectory));

					conf_vars.AppendFormat ("include $(top_srcdir)/Makefile.include\n");
					conf_vars.AppendFormat ("include $(top_srcdir)/config.make\n\n");
				}

				foreach (CombineConfiguration combineConfig in ctx.TargetCombine.Configurations)
				{
					DotNetProjectConfiguration config = GetProjectConfig (combineConfig.Name, project) as DotNetProjectConfiguration;
					if (config == null)
						continue;

					if (generateAutotools)
						conf_vars.AppendFormat ("if ENABLE_{0}\n", ctx.EscapeAndUpperConfigName (combineConfig.Name));
					else
						conf_vars.AppendFormat ("ifeq ($(CONFIG),{0})\n", ctx.EscapeAndUpperConfigName (combineConfig.Name));

					string assembly = (PlatformID.Unix == Environment.OSVersion.Platform) ? project.GetRelativeChildPath ( config.CompiledOutputName ) : project.GetRelativeChildPath ( config.CompiledOutputName ).Replace("\\","/");

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

					// for project references, we need a ref to the dll for the current configuration
					StringWriter projectReferences = new StringWriter();
					string pref = null;
					foreach (ProjectReference reference in project.ProjectReferences) 
					{
						if (reference.ReferenceType == ReferenceType.Project) 
						{
							Project refp = GetProjectFromName (reference.Reference, ctx.TargetCombine);

							DotNetProjectConfiguration dnpc = GetProjectConfig (combineConfig.Name, refp) as DotNetProjectConfiguration;
							if ( dnpc == null )
								throw new Exception ( GettextCatalog.GetString 
										("Could not add reference to project '{0}'", refp.Name) );
							
							projectReferences.WriteLine (" \\");
							projectReferences.Write ("\t");
							pref = (PlatformID.Unix == Environment.OSVersion.Platform) ? project.GetRelativeChildPath ( dnpc.CompiledOutputName ) : project.GetRelativeChildPath ( dnpc.CompiledOutputName ).Replace("\\","/");
							projectReferences.Write ( pref );
						} 
					}
					conf_vars.AppendFormat ( "PROJECT_REFERENCES = {0}\n", projectReferences.ToString() );
					pref = (PlatformID.Unix == Environment.OSVersion.Platform) ? project.GetRelativeChildPath ( config.OutputDirectory ) : project.GetRelativeChildPath ( config.OutputDirectory ).Replace("\\","/");
					conf_vars.AppendFormat ( "BUILD_DIR = {0}\n", pref);
					conf_vars.Append ( "endif\n\n" );
					
					// Register files built by this configuration.
					// Built files won't be distributed.
					foreach (string bfile in builtFiles)
						ctx.AddBuiltFile (Path.Combine (config.OutputDirectory, bfile));
				}
				
				// Register files generated by the compiler
				ctx.AddBuiltFile (project.GetOutputFileName ());
				ctx.AddBuiltFile (project.GetOutputFileName () + ".mdb");
				
				conf_vars.Append ('\n');
				
				foreach (DictionaryEntry e in deployDirs) {
					conf_vars.AppendFormat ("{0} = {1} \n", e.Key, e.Value);
				}
				
				templateEngine.Variables["CONFIG_VARS"] = conf_vars.ToString ();

				// Create project specific makefile
				Stream stream = ctx.GetTemplateStream (
						generateAutotools ? "Makefile.am.project.template" : "Makefile.noauto.project.template");

				StreamReader reader = new StreamReader (stream);			                                          
				string txt = templateEngine.Process ( reader );
				reader.Close();

				makefile.Append ( txt );
				monitor.Step (1);
			}
			finally	{ monitor.EndTask (); }
			return makefile;
		}

		// Get the Project config corresponding to its @parentConfig
		IConfiguration GetProjectConfig (string parentConfig, CombineEntry entry)
		{
			CombineConfiguration combineConfig = entry.ParentCombine.Configurations [parentConfig] as CombineConfiguration;
			if (combineConfig == null)
				return null;

			foreach (CombineConfigurationEntry cce in combineConfig.Entries) {
				if (cce.Entry == entry)
					return entry.Configurations [cce.ConfigurationName];
			}

			return null;
		}

		string GetDeployVar (Hashtable hash, string name)
		{
			name = name.Replace (Path.DirectorySeparatorChar, '_');
			name = name.Replace ('.', '_');
			name = name.ToUpper ();
			name = AutotoolsContext.EscapeStringForAutoconf (name).Trim ('_');
			
			string bname = name;
			int n = 1;
			while (hash.Contains (name)) {
				name = bname + "_" + n;
				n++;
			}
			hash [name] = name;
			return name;
		}

/*
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

		void CreatePkgConfigFile ( Project project, 
				Set packages, 
				Set dlls,
				IProgressMonitor monitor,  
				AutotoolsContext context )
		{
			string projname = AutotoolsContext.EscapeStringForAutoconf (project.Name.ToUpper());
			string uniquenm = GetUniqueName ( project );
			
			monitor.Log.WriteLine ( GettextCatalog.GetString ("Creating pkg-config file") );

			TemplateEngine templateEngine = new TemplateEngine();			
			templateEngine.Variables["NAME"] = uniquenm;
			templateEngine.Variables["DESCRIPTION"] = project.Description;
			templateEngine.Variables["VERSION"] = "@VERSION@"; // inherit from package
			
			// get the external pkg-config dependencies
			StringBuilder pkgs = new StringBuilder ();
			foreach ( SystemPackage pkg in packages )
				pkgs.AppendFormat ( " {0}", pkg.Name );

			// add internal pkg-config dependencies
			foreach (ProjectReference reference in project.ProjectReferences) 
			{
				if (reference.ReferenceType == ReferenceType.Project) 
				{
					Project refp = GetProjectFromName ( reference.Reference, project );
					pkgs.AppendFormat ( " {0}", GetUniqueName ( refp ) );
				}
			}
			
			templateEngine.Variables ["REQUIRES_PKGS"] = pkgs.ToString ();
				
			// build library variable so can be set at configure
			StringBuilder vars = new StringBuilder ();
			foreach ( DotNetProjectConfiguration config in project.Configurations )
			{
				if ( !context.IsSupportedConfiguration ( config.Name ) ) continue;
				vars.AppendFormat ( "@{0}_{1}_LIB@", projname, context.EscapeAndUpperConfigName (config.Name));
			}

			// add additional assemblies to references
			StringBuilder libs = new StringBuilder ();
			StringBuilder libraries = new StringBuilder ();
			foreach ( string dll in dlls )
			{
				libraries.Append ( " ${pkglibdir}/" + dll );
				libs.Append ( " -r:${pkglibdir}/" + dll );
			}
			
			// set the variables
			templateEngine.Variables ["LIBS"] = " -r:${pkglibdir}/" + vars.ToString () + libs.ToString ();
			templateEngine.Variables ["LIBRARIES"] = " ${pkglibdir}/" + vars.ToString () + libraries.ToString ();
			
			// write to file
			string fileName = uniquenm + ".pc";
			string path = string.Format ( "{0}/{1}.in", Path.GetDirectoryName (project.FileName), fileName );
			StreamWriter writer = new StreamWriter( path );
			Stream stream = context.GetTemplateStream ("package.pc.template");
			StreamReader reader = new StreamReader(stream);
			templateEngine.Process(reader, writer);
			reader.Close();
			writer.Close();
			
			// add for autoconf processing
			context.AddAutoconfFile ( Path.GetDirectoryName (project.FileName) + "/" + fileName );
		}

		// GetUniqueName: A way of getting a (hopefully) unique name for the pkg-config item
		// Solution.[Solution].Project
		// FIXME: makes assumption that the root combine is the top of the autotools setup
		string GetUniqueName ( CombineEntry entry )
		{
			string name = entry.Name;
			CombineEntry current = entry.ParentCombine;
			while ( current != null )
			{
				name = string.Format ("{0}.{1}", current.Name, name);
				current = current.ParentCombine;
			}

			return name;
		}

		// FIXME: makes assumption that the root combine is the top of the autotools setup
		bool NeedsPCFile ( Project project ) 
		{
			//go up the chain and find the first non-null of the parm
			CombineEntry current = project.ParentCombine;
			while ( current != null )
			{
				IExtendedDataItem item = current as IExtendedDataItem;
				if ( item != null )
				{
					object en_obj =  item.ExtendedProperties ["MakeLibPC"];
					if (en_obj != null ) return (bool) en_obj;
				}
				
				current = current.ParentCombine;
			}
			return true;
		}
*/

		// FIXME: makes assumption that the root combine is the top of the autotools setup
		Project GetProjectFromName ( string name, Combine targetCombine)
		{
			Project refp = null;
			if (targetCombine != null) refp = targetCombine.FindProject (name);

			if (refp == null)
				throw new Exception ( GettextCatalog.GetString ("Couldn't find referenced project '{0}'", 
							name ) );
			
			return refp;
		}
	}
}

