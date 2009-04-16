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
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Core.Assemblies;
using Mono.Addins;
using MonoDevelop.Deployment;

namespace MonoDevelop.Autotools
{
	public class SimpleProjectMakefileHandler : IMakefileHandler
	{
		string resourcedir = "resources";
		bool generateAutotools = true;

		// Handle files to be deployed
		Dictionary<string, StringBuilder> deployDirs;
		Dictionary<string, string> deployFileVars;
		Dictionary<string, DeployFileData> allDeployVars;
		List<string> builtFiles;

		StringBuilder deployFileCopyVars;
		StringBuilder deployFileCopyTargets;

		//used only for simple makefile generation
		StringBuilder templateFilesTargets = null;
		StringBuilder installTarget = null;
		StringBuilder installDeps = null;
		List<string> installDirs = null;

		StringBuilder uninstallTarget = null;

		// handle configuration specific variables
		StringBuilder conf_vars;

		// Variables for project files
		StringBuilder files;
		StringBuilder res_files;
		StringBuilder extras;
		StringBuilder datafiles;

		// custom commands
		StringBuilder customCommands;

		// store all refs for easy access
		Set<SystemPackage> pkgs;

		public bool CanDeploy (SolutionItem entry, MakefileType type)
		{
			Project project = entry as Project;
			if ( project == null ) return false;
			if ( FindSetupForProject ( project ) == null ) return false;
			return true;
		}

		ISimpleAutotoolsSetup FindSetupForProject (Project project)
		{
			object[] items = AddinManager.GetExtensionObjects ("/MonoDevelop/Autotools/SimpleSetups", typeof(ISimpleAutotoolsSetup));
			foreach ( ISimpleAutotoolsSetup setup in items)
			{
				if ( setup.CanDeploy ( project ) ) return setup;
			}
			return null;
		}

		public Makefile Deploy (AutotoolsContext ctx, SolutionItem entry, IProgressMonitor monitor)
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

				// Handle files to be deployed
				deployDirs = new Dictionary<string, StringBuilder> ();
				deployFileVars = new Dictionary<string, string> ();
				builtFiles = new List<string> ();
				deployFileCopyVars = new StringBuilder ();
				deployFileCopyTargets = new StringBuilder ();

				//used only for simple makefile generation
				templateFilesTargets = null;
				installTarget = null;
				installDeps = null;
				installDirs = null;
				uninstallTarget = null;

				// handle configuration specific variables
				conf_vars = new StringBuilder ();

				// grab all project files
				files = new StringBuilder ();
				res_files = new StringBuilder ();
				extras = new StringBuilder ();
				datafiles = new StringBuilder ();
				Set<string> extraFiles = new Set<string> ();

				string includes = String.Empty;
				string references, dllReferences;
				DotNetProject netProject = project as DotNetProject;
				ProcessProjectReferences (netProject, out references, out dllReferences, ctx);

				templateEngine.Variables["REFERENCES"] = references;
				templateEngine.Variables["DLL_REFERENCES"] =  dllReferences;
				templateEngine.Variables["WARNING"] = "Warning: This is an automatically generated file, do not edit!";

				DotNetProject dotnetProject = entry as DotNetProject;
				if (dotnetProject != null)
				{
					string resgen = "resgen";
					if (System.Environment.Version.Major >= 2) {
						switch (dotnetProject.TargetFramework.ClrVersion) {
							case ClrVersion.Net_2_0: resgen = "resgen2"; break;
							case ClrVersion.Net_1_1: resgen = "resgen1"; break;
						}
					}
					templateEngine.Variables ["RESGEN"] = resgen;
				}
				
				string pfpath = null;
				foreach (ProjectFile projectFile in project.Files) 
				{
					pfpath = (PlatformID.Unix == Environment.OSVersion.Platform) ? projectFile.RelativePath : projectFile.RelativePath.Replace("\\","/");
					pfpath = FileService.NormalizeRelativePath (pfpath);
					switch ( projectFile.BuildAction )
					{
						case "Compile":
							
							if ( projectFile.Subtype != Subtype.Code ) continue;
							files.AppendFormat ( "\\\n\t{0} ", EscapeSpace (pfpath));
							break;

						case "None":

							extraFiles.Add (EscapeSpace(pfpath));
							break;

						case "EmbeddedResource":

							if ( !projectFile.FilePath.StartsWith ( ctx.BaseDirectory ) )
							{
								// file is not within directory hierarchy, copy it in
								string rdir = Path.Combine (Path.GetDirectoryName (project.FileName), resourcedir);
								if ( !Directory.Exists ( rdir ) ) Directory.CreateDirectory ( rdir );
								string newPath = Path.Combine (rdir, Path.GetFileName ( projectFile.FilePath ));
								FileService.CopyFile ( projectFile.FilePath, newPath ) ;
								pfpath = (PlatformID.Unix == Environment.OSVersion.Platform) ? project.GetRelativeChildPath (newPath) : project.GetRelativeChildPath (newPath).Replace("\\","/");
								pfpath = FileService.NormalizeRelativePath (pfpath);
							}
							if (!String.IsNullOrEmpty (projectFile.ResourceId) && projectFile.ResourceId != Path.GetFileName (pfpath))
								res_files.AppendFormat ( "\\\n\t{0},{1} ", EscapeSpace (pfpath), EscapeSpace (projectFile.ResourceId));
							else
								res_files.AppendFormat ( "\\\n\t{0} ", EscapeSpace (pfpath));
 
							break;

						case "FileCopy":
						
							datafiles.AppendFormat ("\\\n\t{0} ", EscapeSpace (pfpath));
							break;
					}
				}

				if (!generateAutotools) {
					templateFilesTargets = new StringBuilder ();
					installTarget = new StringBuilder ();
					uninstallTarget = new StringBuilder ();
					installDeps = new StringBuilder ();
					installDirs = new List<string> ();

					customCommands = new StringBuilder ();

					string programFilesDir = ctx.DeployContext.GetDirectory (TargetDirectory.ProgramFiles);
					//FIXME:temp
					programFilesDir = TranslateDir (programFilesDir);
					installDirs.Add (programFilesDir);
					installTarget.Append ("\tmake pre-install-local-hook prefix=$(prefix)\n");
					installTarget.Append ("\tmake install-satellite-assemblies prefix=$(prefix)\n");
					installTarget.AppendFormat ("\tmkdir -p '$(DESTDIR){0}'\n", programFilesDir);
					installTarget.AppendFormat ("\t$(call cp,$(ASSEMBLY),$(DESTDIR){0})\n", programFilesDir);
					installTarget.AppendFormat ("\t$(call cp,$(ASSEMBLY_MDB),$(DESTDIR){0})\n", programFilesDir);

					//remove dir?
					uninstallTarget.Append ("\tmake pre-uninstall-local-hook prefix=$(prefix)\n");
					uninstallTarget.Append ("\tmake uninstall-satellite-assemblies prefix=$(prefix)\n");
					uninstallTarget.AppendFormat ("\t$(call rm,$(ASSEMBLY),$(DESTDIR){0})\n", programFilesDir);
					uninstallTarget.AppendFormat ("\t$(call rm,$(ASSEMBLY_MDB),$(DESTDIR){0})\n", programFilesDir);

					installDeps.Append (" $(ASSEMBLY) $(ASSEMBLY_MDB)");

					conf_vars.AppendFormat ("srcdir=.\n");
					conf_vars.AppendFormat ("top_srcdir={0}\n\n",
						FileService.AbsoluteToRelativePath (project.BaseDirectory, ctx.TargetSolution.BaseDirectory));

					conf_vars.AppendFormat ("include $(top_srcdir)/config.make\n\n");

					// Don't emit for top level project makefile(eg. pdn.make), as it would be
					// included by top level solution makefile
					if (ctx.TargetSolution.BaseDirectory != project.BaseDirectory){
						string customhooks = Path.Combine (project.BaseDirectory, "custom-hooks.make");
						bool include = File.Exists (customhooks);
					
						includes = "include $(top_srcdir)/Makefile.include\n";
						includes += String.Format ("{0}include $(srcdir)/custom-hooks.make\n\n", include ? "" : "#");
						if (include)
							makefile.SetVariable ("EXTRA_DIST", "$(srcdir)/custom-hooks.make");
					}
				}

				bool buildEnabled;
				List<ConfigSection> configSections = new List<ConfigSection> ();
				allDeployVars = new Dictionary<string, DeployFileData> ();

				foreach (SolutionConfiguration combineConfig in ctx.TargetSolution.Configurations)
				{
					DotNetProjectConfiguration config = GetProjectConfig (combineConfig.Id, project, out buildEnabled) as DotNetProjectConfiguration;
					if (config == null)
						continue;

					ConfigSection configSection = new ConfigSection (combineConfig.Id);

					string assembly = (PlatformID.Unix == Environment.OSVersion.Platform) ?
						project.GetRelativeChildPath ( config.CompiledOutputName ) :
						project.GetRelativeChildPath ( config.CompiledOutputName ).Replace("\\","/");

					configSection.BuildVariablesBuilder.AppendFormat ("ASSEMBLY_COMPILER_COMMAND = {0}\n",
							setup.GetCompilerCommand ( project, config.Id ) );
					configSection.BuildVariablesBuilder.AppendFormat ("ASSEMBLY_COMPILER_FLAGS = {0}\n",
							setup.GetCompilerFlags ( project, config.Id ) );

					// add check for compiler command in configure.ac
					ctx.AddCommandCheck ( setup.GetCompilerCommand ( project, config.Id ) );

					configSection.BuildVariablesBuilder.AppendFormat ("ASSEMBLY = {0}\n",
						AutotoolsContext.EscapeStringForAutomake (assembly));
					configSection.BuildVariablesBuilder.AppendFormat ("ASSEMBLY_MDB = {0}\n",
						config.DebugMode ? "$(ASSEMBLY).mdb" : String.Empty);

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
					configSection.BuildVariablesBuilder.AppendFormat ( "COMPILE_TARGET = {0}\n", target );

					// for project references, we need a ref to the dll for the current configuration
					StringWriter projectReferences = new StringWriter();
					string pref = null;
					foreach (ProjectReference reference in netProject.References) 
					{
						if (reference.ReferenceType != ReferenceType.Project)
							continue;
						Project refp = GetProjectFromName (reference.Reference, ctx.TargetSolution);

						if (!(refp is DotNetProject))
							continue;
						
						DotNetProjectConfiguration dnpc = GetProjectConfig (combineConfig.Id, refp, out buildEnabled) as DotNetProjectConfiguration;
						if ( dnpc == null )
							throw new Exception ( GettextCatalog.GetString
									("Could not add reference to project '{0}'", refp.Name) );

						projectReferences.WriteLine (" \\");
						projectReferences.Write ("\t");
						pref = (PlatformID.Unix == Environment.OSVersion.Platform) ?
							project.GetRelativeChildPath ( dnpc.CompiledOutputName ) :
							project.GetRelativeChildPath ( dnpc.CompiledOutputName ).Replace("\\","/");

						projectReferences.Write (EscapeSpace (pref));
					}
					configSection.BuildVariablesBuilder.AppendFormat ( "PROJECT_REFERENCES = {0}\n", projectReferences.ToString() );

					string buildDir = (PlatformID.Unix == Environment.OSVersion.Platform) ?
						project.GetRelativeChildPath (config.OutputDirectory) :
						project.GetRelativeChildPath (config.OutputDirectory).Replace ("\\","/");
					configSection.BuildVariablesBuilder.AppendFormat ("BUILD_DIR = {0}\n", buildDir);

					// Register files built by this configuration.
					// Built files won't be distributed.
					foreach (string bfile in builtFiles)
						ctx.AddBuiltFile (Path.Combine (config.OutputDirectory, bfile));

					DeployFileCollection deployFiles = DeployService.GetDeployFiles (
							ctx.DeployContext, new SolutionItem[] { project }, config.Id);

					ProcessDeployFilesForConfig (deployFiles, project, configSection, ctx, config);
					configSections.Add (configSection);

					if (!generateAutotools) {
						EmitCustomCommandTargets (config.CustomCommands, project, customCommands, combineConfig.Id,
								new CustomCommandType [] {
									CustomCommandType.BeforeBuild,
									CustomCommandType.AfterBuild,
									CustomCommandType.BeforeClean,
									CustomCommandType.AfterClean}, monitor);
					} else {
						if (config.CustomCommands.Count > 0)
							monitor.ReportWarning (GettextCatalog.GetString ("Custom commands are not supported for autotools based makefiles. Ignoring."));
					}

					// Register files generated by the compiler
					ctx.AddBuiltFile (project.GetOutputFileName (combineConfig.Id));
					if (config.DebugMode)
						ctx.AddBuiltFile (project.GetOutputFileName (combineConfig.Id) + ".mdb");

					if (config.SignAssembly) {
						string spath = project.GetRelativeChildPath (config.AssemblyKeyFile);
						spath = (PlatformID.Unix == Environment.OSVersion.Platform) ? spath : spath.Replace("\\","/");
						spath = FileService.NormalizeRelativePath (spath);
						extraFiles.Add (EscapeSpace (spath));
					}

					if (buildEnabled && pkgs.Count > 0)
						ctx.AddRequiredPackages (combineConfig.Id, pkgs);
				}


				foreach (string ef in extraFiles)
					extras.AppendFormat ( "\\\n\t{0} ", EscapeSpace (ef));

				Dictionary<string, DeployFileData> commonDeployVars = new Dictionary<string, DeployFileData> (allDeployVars);
				foreach (ConfigSection configSection in configSections) {
					List<string> toRemove = new List<string> ();
					foreach (KeyValuePair<string, DeployFileData> pair in commonDeployVars) {
						if (!configSection.DeployFileVars.ContainsKey (pair.Key))
							toRemove.Add (pair.Key);
					}
					foreach (string s in toRemove)
						commonDeployVars.Remove (s);
				}

				//emit the config sections here.. to conf_vars
				foreach (ConfigSection configSection in configSections) {
					conf_vars.AppendFormat (generateAutotools ? "if ENABLE_{0}\n" : "ifeq ($(CONFIG),{0})\n",
							ctx.EscapeAndUpperConfigName (configSection.Name));

					conf_vars.Append (configSection.BuildVariablesBuilder.ToString ());
					conf_vars.Append ("\n");

					foreach (KeyValuePair<string, DeployFileData> pair in allDeployVars) {
						string targetDeployVar = pair.Key;
						if (pair.Value.File.ContainsPathReferences)
							//Template files are not handled per-config
							continue;

						if (configSection.DeployFileVars.ContainsKey (targetDeployVar)) {
							//use the dfile from the config section
							DeployFile dfile = configSection.DeployFileVars [targetDeployVar];
							string fname = EscapeSpace (
									FileService.AbsoluteToRelativePath (
										Path.GetFullPath (project.BaseDirectory),
										Path.GetFullPath (dfile.SourcePath)));

							conf_vars.AppendFormat ("{0}_SOURCE={1}\n", targetDeployVar, fname);

							if (!commonDeployVars.ContainsKey (targetDeployVar)) {
								//FOO_DLL=$(BUILD_DIR)/foo.dll
								conf_vars.AppendFormat ("{0}=$(BUILD_DIR){1}{2}\n",
										targetDeployVar,
										Path.DirectorySeparatorChar,
										EscapeSpace (dfile.RelativeTargetPath));
							}
						} else {
							// not common and not part of @configSection
							conf_vars.AppendFormat ("{0}=\n", pair.Key);
						}
					}

					conf_vars.Append ( "\nendif\n\n" );
				}

				conf_vars.AppendFormat ("AL={0}\n", (dotnetProject.TargetFramework.ClrVersion == ClrVersion.Net_2_0) ? "al2" : "al");
				conf_vars.AppendFormat ("SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll\n");

				foreach (KeyValuePair<string, DeployFileData> pair in allDeployVars) {
					HandleDeployFile (pair.Value, pair.Key, project, ctx);

					if (commonDeployVars.ContainsKey (pair.Key)) {
						//FOO_DLL=$(BUILD_DIR)/foo.dll
						deployFileCopyVars.AppendFormat ("{0} = $(BUILD_DIR){1}{2}\n",
									pair.Key,
									Path.DirectorySeparatorChar,
									EscapeSpace (pair.Value.File.RelativeTargetPath));
					}
				}
				
				conf_vars.Append ('\n');
				
				StringBuilder vars = new StringBuilder ();
				foreach (KeyValuePair<string, StringBuilder> pair in deployDirs) {
					//PROGRAM_FILES= .. etc
					conf_vars.AppendFormat ("{0} = {1} \n\n", pair.Key, pair.Value.ToString ());
					//Build list of deploy dir variables
					vars.AppendFormat ("$({0}) ", pair.Key);
				}

				if (!generateAutotools) {
					installTarget.Insert (0, String.Format ("install-local:{0}\n", installDeps.ToString ()));
					installTarget.Append ("\tmake post-install-local-hook prefix=$(prefix)\n");

					uninstallTarget.Insert (0, String.Format ("uninstall-local:{0}\n", installDeps.ToString ()));
					uninstallTarget.Append ("\tmake post-uninstall-local-hook prefix=$(prefix)\n");
				}

				if (!generateAutotools && customCommands.Length > 0)
					customCommands.Insert (0, "# Targets for Custom commands\n");

				templateEngine.Variables["CONFIG_VARS"] = conf_vars.ToString ();
				templateEngine.Variables["DEPLOY_FILE_VARS"] = vars.ToString ();
				templateEngine.Variables["COPY_DEPLOY_FILES_VARS"] = deployFileCopyVars.ToString();
				templateEngine.Variables["COPY_DEPLOY_FILES_TARGETS"] = deployFileCopyTargets.ToString();
				templateEngine.Variables["ALL_TARGET"] = (ctx.TargetSolution.BaseDirectory == project.BaseDirectory) ? "all-local" : "all";
				templateEngine.Variables["INCLUDES"] = includes;

				templateEngine.Variables["FILES"] = files.ToString();
				templateEngine.Variables["RESOURCES"] = res_files.ToString();
				templateEngine.Variables["EXTRAS"] = extras.ToString();
				templateEngine.Variables["DATA_FILES"] = datafiles.ToString();
				templateEngine.Variables["CLEANFILES"] = vars.ToString ();

				if (!generateAutotools) {
					templateEngine.Variables["TEMPLATE_FILES_TARGETS"] = templateFilesTargets.ToString();
					templateEngine.Variables["INSTALL_TARGET"] = installTarget.ToString();
					templateEngine.Variables["UNINSTALL_TARGET"] = uninstallTarget.ToString();
					templateEngine.Variables["CUSTOM_COMMAND_TARGETS"] = customCommands.ToString();
				}

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

		void ProcessProjectReferences (DotNetProject project, out string references, out string dllReferences, AutotoolsContext ctx)
		{
			StringWriter refWriter = new StringWriter();
			StringWriter dllRefWriter = new StringWriter();
			pkgs = new Set<SystemPackage> ();

			// grab pkg-config references
			foreach (ProjectReference reference in project.References) 
			{
				if (reference.ReferenceType == ReferenceType.Gac) 
				{
					// Get pkg-config keys
					SystemPackage pkg = reference.Package;
					if (pkg != null && !pkg.IsCorePackage) 
					{
						if ( pkgs.Contains(pkg) ) continue;
						pkgs.Add(pkg);

						refWriter.WriteLine (" \\");
						if (generateAutotools) {
							refWriter.Write ("\t$(");
							refWriter.Write (AutotoolsContext.GetPkgConfigVariable(pkg.Name));
							refWriter.Write ("_LIBS)");
						} else {
							refWriter.Write ("\t-pkg:{0}", pkg.Name);
						}
						pkgs.Add (pkg);
					} 
					else 
					{
						refWriter.WriteLine (" \\");			// store all refs for easy access
						AssemblyName assembly = SystemAssemblyService.ParseAssemblyName (reference.Reference);
						refWriter.Write ("\t" + assembly.Name);
						refWriter.Write ("");
					}
				} 
				else if (reference.ReferenceType == ReferenceType.Assembly) 
				{
					string assemblyPath = Path.GetFullPath (reference.Reference);

					dllRefWriter.WriteLine (" \\");
					dllRefWriter.Write ("\t");

					ctx.AddGlobalReferencedFile (EscapeSpace (FileService.AbsoluteToRelativePath (
						Path.GetFullPath (ctx.BaseDirectory), assemblyPath)));
					dllRefWriter.Write (EscapeSpace (FileService.AbsoluteToRelativePath (
						project.BaseDirectory, assemblyPath)));

				} 
				else if (reference.ReferenceType == ReferenceType.Project)
					continue; // handled per-config
				else
					throw new Exception ( GettextCatalog.GetString  ("Project reference type '{0}' not supported yet", 
							reference.ReferenceType.ToString() ) );
			}

			references = refWriter.ToString ();
			dllReferences = dllRefWriter.ToString ();
		}

		// Populates configSection.DeployFileVars with unique DeployFiles for a particular config
		void ProcessDeployFilesForConfig (DeployFileCollection deployFiles, Project project, ConfigSection configSection, AutotoolsContext ctx, DotNetProjectConfiguration config)
		{
			//@deployFiles can have duplicates
			Dictionary<string, DeployFile> uniqueDeployFiles = new Dictionary<string, DeployFile> ();
			foreach (DeployFile dfile in deployFiles) {
				if (dfile.SourcePath == project.GetOutputFileName (configSection.Name))
					continue;

				// DeployFileCollection can have duplicates, ignore them
				string key = dfile.RelativeTargetPath;
				if (!dfile.ContainsPathReferences)
					key += dfile.SourcePath;
				if (uniqueDeployFiles.ContainsKey (key))
					continue;
				uniqueDeployFiles [key] = dfile;

				string targetDeployVar = GetDeployVar (deployFileVars, Path.GetFileName (dfile.RelativeTargetPath));
				configSection.DeployFileVars [targetDeployVar] = dfile;
				DeployFileData data = new DeployFileData ();
				data.File = dfile;
				data.Configuration = config;
				allDeployVars [targetDeployVar] = data;
			}
		}

		// Handle unique deploy files, emits non-perconfig stuff, like targets for deploy files,
		// un/install commands
		void HandleDeployFile (DeployFileData data, string targetDeployVar, Project project, AutotoolsContext ctx)
		{
			DeployFile dfile = data.File;
			string dependencyDeployFile = null; //Dependency for the deployfile target
			if (dfile.ContainsPathReferences) {
				// Template file, copy to .in file
				string full_fname = Path.Combine (project.BaseDirectory, Path.GetFileName (dfile.RelativeTargetPath));
				string fname = full_fname;
				string infname = fname + ".in";
				if (File.Exists (infname) && project.IsFileInProject (infname)) {
					string datadir = Path.Combine (project.BaseDirectory, "data");
					if (!Directory.Exists (datadir))
						Directory.CreateDirectory (datadir);
					infname = Path.Combine (datadir, Path.GetFileName (dfile.RelativeTargetPath) + ".in");
				}

				//Absolute path required
				File.Copy (dfile.SourcePath, infname, true);

				//Path relative to TargetCombine
				fname = FileService.NormalizeRelativePath (
						FileService.AbsoluteToRelativePath (ctx.TargetSolution.BaseDirectory, full_fname));
				infname = fname + ".in";
				ctx.AddAutoconfFile (EscapeSpace (fname));
				ctx.AddGeneratedFile (full_fname + ".in");

				//Path relative to project
				fname = FileService.NormalizeRelativePath (
						FileService.AbsoluteToRelativePath (project.BaseDirectory, full_fname));
				infname = fname + ".in";
				extras.AppendFormat ( "\\\n\t{0} ", EscapeSpace (infname));

				//dependencyDeployFile here should be filename relative to the project
				dependencyDeployFile = fname;
			} else {
				dependencyDeployFile = String.Format ("$({0}_SOURCE)", targetDeployVar);
			}

			builtFiles.Add (Path.GetFileName (dfile.RelativeTargetPath));

			if (dfile.ContainsPathReferences)
				deployFileCopyTargets.AppendFormat ("$(eval $(call emit-deploy-wrapper,{0},{1}{2}))\n",
					targetDeployVar,
					EscapeSpace (dependencyDeployFile),
					(dfile.FileAttributes & DeployFileAttributes.Executable) != 0 ? ",x" : String.Empty);
			else {
				// The emit-deploy-target macro copies the deployable file to the output directory.
				// This is not needed if the file is already there (e.g. for an .mdb file)
				if (Path.GetFullPath (dfile.SourcePath) != Path.GetFullPath (Path.Combine (data.Configuration.OutputDirectory, dfile.RelativeTargetPath)))
				    deployFileCopyTargets.AppendFormat ("$(eval $(call emit-deploy-target,{0}))\n", targetDeployVar);
			}

			switch (dfile.TargetDirectoryID) {
				case TargetDirectory.Gac:
					// TODO
					break;
				default:
					string var;
					if (dfile.TargetDirectoryID != TargetDirectory.Binaries) {
						string ddir = FileService.NormalizeRelativePath (Path.GetDirectoryName (dfile.RelativeTargetPath).Trim ('/',' '));
						if (ddir.Length > 0)
							ddir = "/" + ddir;
						var = ctx.GetDeployDirectoryVar (dfile.TargetDirectoryID + ddir);
					}
					else
						var = "BINARIES";

					StringBuilder sb;
					if (!deployDirs.TryGetValue (var, out sb)) {
						sb = new StringBuilder ();
						deployDirs [var] = sb;
					}
					sb.AppendFormat ("\\\n\t$({0}) ", targetDeployVar);
					break;
			}

			if (!generateAutotools) {
				string installDir = Path.GetDirectoryName (ctx.DeployContext.GetResolvedPath (dfile.TargetDirectoryID, dfile.RelativeTargetPath));
				//FIXME: temp
				installDir = TranslateDir (installDir);

				if (!installDirs.Contains (installDir)) {
					installTarget.AppendFormat ("\tmkdir -p '$(DESTDIR){0}'\n", installDir);
					installDirs.Add (installDir);
				}

				installTarget.AppendFormat ("\t$(call cp,$({0}),$(DESTDIR){1})\n", targetDeployVar, installDir);
				uninstallTarget.AppendFormat ("\t$(call rm,$({1}),$(DESTDIR){0})\n", installDir, targetDeployVar);
			}
		}
		
		string TranslateDir (string dir)
		{
			dir = dir.Replace ("@prefix@", "$(prefix)");
			dir = dir.Replace ("@PACKAGE@", "$(PACKAGE)");
			dir = dir.Replace ("@expanded_libdir@", "$(libdir)");
			dir = dir.Replace ("@expanded_bindir@", "$(bindir)");
			dir = dir.Replace ("@expanded_datadir@", "$(datadir)");
			return dir;
		}

		void EmitCustomCommandTargets (CustomCommandCollection commands, Project project, StringBuilder builder, string configName, CustomCommandType[] types, IProgressMonitor monitor)
		{
			bool warned = false;
			configName = configName.ToUpper ();
			foreach (CustomCommandType type in types) {
				bool targetEmitted = false;
				for (int i = 0; i < commands.Count; i ++) {
					CustomCommand cmd = commands [i];
					if (cmd.Type != type) {
						if (!warned && Array.IndexOf (types, cmd.Type) < 0) {
							//Warn (only once) if unsupported custom command is found,
							StringBuilder types_list = new StringBuilder ();
							foreach (CustomCommandType t in types)
								types_list.AppendFormat ("{0}, ", t);
							monitor.ReportWarning (GettextCatalog.GetString (
								"Custom commands of only the following types are supported: {0}.", types_list.ToString ()));
							warned = true;
						}
						continue;
					}

					if (!targetEmitted) {
						builder.AppendFormat ("{0}_{1}:\n", configName, type.ToString ());
						targetEmitted = true;
					}

					string dir, exe, args;
					ResolveCustomCommand (project, cmd, out dir, out exe, out args);
					builder.AppendFormat ("\t(cd {0} && {1} {2})\n", dir, exe, args);
				}
				if (targetEmitted)
					builder.Append ("\n");
			}
		}

		void ResolveCustomCommand (Project project, CustomCommand cmd, out string dir, out string exe, out string args)
		{
			dir = exe = args = null;
			string [,] customtags = new string [,] {
				{"ProjectDir", "$(srcdir)"},
				{"TargetName", "$(notdir $(ASSEMBLY))"},
				{"TargetDir", "$(BUILD_DIR)"},
				{"CombineDir", "$(top_srcdir)"}
			};

			int i = cmd.Command.IndexOf (' ');
			if (i == -1) {
				exe = cmd.Command;
				args = string.Empty;
			} else {
				exe = cmd.Command.Substring (0, i);
				args = StringParserService.Parse (cmd.Command.Substring (i + 1), customtags);
			}

			dir = (string.IsNullOrEmpty (cmd.WorkingDir) ? "$(srcdir)" : StringParserService.Parse (cmd.WorkingDir, customtags));
		}

		// Get the Project config corresponding to its @parentConfig
		internal static SolutionItemConfiguration GetProjectConfig (string parentConfig, SolutionEntityItem entry, out bool enabled)
		{
			enabled = false;
			SolutionConfiguration solutionConfig = entry.ParentSolution.Configurations [parentConfig] as SolutionConfiguration;
			if (solutionConfig == null)
				return null;

			foreach (SolutionConfigurationEntry cce in solutionConfig.Configurations) {
				if (cce.Item == entry) {
					enabled = cce.Build;
					return entry.Configurations [cce.ItemConfiguration];
				}
			}

			return null;
		}

		string GetDeployVar (Dictionary<string, string> dict, string name)
		{
			name = name.Replace (Path.DirectorySeparatorChar, '_');
			name = name.Replace ('.', '_');
			name = name.ToUpper ();
			name = AutotoolsContext.EscapeStringForAutoconf (name).Trim ('_');

			dict [name] = name;
			return name;
		}

		Project GetProjectFromName (string name, Solution targetSolution)
		{
			Project refp = null;
			if (targetSolution != null) refp = targetSolution.FindProjectByName (name);

			if (refp == null)
				throw new Exception ( GettextCatalog.GetString ("Couldn't find referenced project '{0}'", 
							name ) );
			
			return refp;
		}

		static string EscapeSpace (string str)
		{
			return str.Replace (" ", "\\ ");
		}

	}
	
	class DeployFileData
	{
		public DeployFile File;
		public DotNetProjectConfiguration Configuration;
	}
}

