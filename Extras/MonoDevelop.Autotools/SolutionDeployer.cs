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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects.Serialization;

using Mono.Unix.Native;

namespace MonoDevelop.Autotools
{
	public class SolutionDeployer
	{
		string solution_dir;
		string solution_name;
		string solution_version;

		bool pkgconfig = false;
		
		AutotoolsContext context;

		public bool HasGeneratedFiles ( Combine combine )
		{
			string dir = Path.GetDirectoryName(combine.FileName) + "/";

			if ( File.Exists ( dir + "configure.ac" ) && File.Exists ( dir + "autogen.sh" ) )
				return true;
			return false;
		}
		
		public bool CanDeploy ( Combine combine )
		{
			IMakefileHandler handler = AutotoolsContext.GetMakefileHandler ( combine );
			if ( !handler.CanDeploy (combine) )	return false;
			return true;
		}
		
		public bool GenerateFiles (Combine combine, IProgressMonitor monitor )
		{
			 return GenerateFiles ( combine, combine.ActiveConfiguration.Name, monitor );
		}
		
		public bool GenerateFiles (Combine combine, string defaultConf, IProgressMonitor monitor )
		{
			monitor.BeginTask ( GettextCatalog.GetString ("Generating Autotools files for Solution {0}", combine.Name), 1 );

			try
			{
				solution_dir = Path.GetDirectoryName(combine.FileName);
				
				context = new AutotoolsContext ( solution_dir );
				IMakefileHandler handler = AutotoolsContext.GetMakefileHandler ( combine );
				if ( !handler.CanDeploy (combine) )
					throw new Exception ( GettextCatalog.GetString ("MonoDevelop does not currently support generating autotools files for one (or more) child projects.") );

				solution_name = combine.Name;
				solution_version = AutotoolsContext.EscapeStringForAutoconf (combine.Version);

				// check if we need to create pkgconfig file
				object en_obj =  ((IExtendedDataItem) combine).ExtendedProperties ["MakePkgConfig"];
				if (en_obj== null) pkgconfig = false;
				else pkgconfig = (bool) en_obj;
				
				Makefile makefile = handler.Deploy ( context, combine, monitor );
				string path = solution_dir + "/Makefile";
				context.AddAutoconfFile ( path );

				CreateAutoGenDotSH ( monitor );
				CreatePkgConfigFile ( combine, monitor, makefile, context );
				CreateConfigureDotAC ( combine, defaultConf, monitor );
				CreateMakefileInclude ( monitor );

				AddTopLevelMakefileVars ( makefile, monitor );

				StreamWriter writer = new StreamWriter ( path + ".am" );
				makefile.Write ( writer );
				writer.Close ();

				monitor.ReportSuccess ( GettextCatalog.GetString ("Autotools files were successfully generated.") );
				monitor.Step (1);
			}
			catch ( Exception e )
			{
				monitor.ReportError ( GettextCatalog.GetString ("Autotools files could not be generated: "), e );
				DeleteGeneratedFiles ( context );
				return false;
			}
			finally
			{
				monitor.EndTask ();
			}
			return true;
		}

		public void Deploy ( Combine combine, string targetDir, IProgressMonitor monitor  )
		{
			Deploy ( combine, combine.ActiveConfiguration.Name, targetDir, monitor  );
		}
		
		public void Deploy ( Combine combine, string defaultConf, string targetDir, IProgressMonitor monitor  )
		{
			if ( !GenerateFiles ( combine, defaultConf, monitor ) )  return;
			
			monitor.BeginTask ( GettextCatalog.GetString( "Deploying Solution to Tarball" ) , 3 );
			try
			{
				string baseDir = Path.GetDirectoryName ( combine.FileName);
	
				ProcessWrapper ag_process = Runtime.ProcessService.StartProcess ( "sh", 
						"autogen.sh", 
						baseDir, 
						monitor.Log, 
						monitor.Log, 
						null );
				ag_process.WaitForOutput ();
				
				if ( ag_process.ExitCode > 0 )
					throw new Exception ( GettextCatalog.GetString ("An unspecified error occurred while running '{0}'","autogen.sh") );
				
				monitor.Step ( 1 );

				StringWriter sw = new StringWriter ();
				LogTextWriter chainedOutput = new LogTextWriter ();
				chainedOutput.ChainWriter (monitor.Log);
				chainedOutput.ChainWriter (sw);

				ProcessWrapper process = Runtime.ProcessService.StartProcess ( "make", 
						"dist", 
						baseDir, 
						chainedOutput, 
						monitor.Log, 
						null );
				process.WaitForOutput ();

				if ( process.ExitCode > 0 )
					throw new Exception ( GettextCatalog.GetString ("An unspecified error occurred while running '{0}'", "make dist") );

				monitor.Step ( 1 );

				// FIXME: hackish way to get the created tarball's filename
				string output = sw.ToString();
				int targz = output.LastIndexOf  ( "tar.gz" );
				int begin = output.LastIndexOf ( '>', targz );

				string filename = output.Substring ( begin + 1, (targz - begin) + 5 ); 
				
				File.Copy ( baseDir + "/" + filename, targetDir + "/" + filename, true );
				monitor.Step ( 1 );
			}
			catch ( Exception e )
			{
				monitor.ReportError ( GettextCatalog.GetString ("Solution could not be deployed: "), e );
				return;
			}
			finally 
			{
				monitor.EndTask ();
			}
			monitor.ReportSuccess ( GettextCatalog.GetString ( "Solution was succesfully deployed" ) );
		}

		void DeleteGeneratedFiles ( AutotoolsContext context )
		{
			foreach ( string file in context.GetAutoConfFiles () )
				if ( File.Exists ( file ) ) File.Delete ( file );

			string[] other_files = new string [] { "autogen.sh", "configure.ac", "Makefile.include" };

			foreach ( string file in other_files )
			{
				string path = solution_dir + "/" + file;
				if ( File.Exists ( path ) ) File.Delete ( path );
			}
		}

		void AddTopLevelMakefileVars ( Makefile makefile, IProgressMonitor monitor)
		{
			monitor.Log.WriteLine ( GettextCatalog.GetString ("Adding variables to top-level Makefile.am") );

			StringBuilder sb = new StringBuilder ();
			foreach ( string dll in context.GetReferencedDlls() )
			{
				string dll_name = Path.GetFileName  ( dll );

				string libdir = solution_dir + "/lib/";
				if ( !Directory.Exists ( libdir ) ) Directory.CreateDirectory ( libdir );

				string newPath = libdir + dll_name;
				File.Copy ( dll, newPath , true );

				newPath = Runtime.FileUtilityService.AbsoluteToRelativePath ( solution_dir, newPath );
				sb.Append (' ');
				sb.Append ( newPath );
			}
			string vals = sb.ToString ();

			makefile.AppendToVariable ( "DLL_REFERENCES", vals );
			makefile.AppendToVariable ( "EXTRA_DIST", "$(DLL_REFERENCES)" );
			makefile.AppendToVariable ( "pkglib_DATA", "$(DLL_REFERENCES)" );
		}

		void CreateAutoGenDotSH (IProgressMonitor monitor)
		{
			monitor.Log.WriteLine ( GettextCatalog.GetString ("Creating autogen.sh") );

			TemplateEngine templateEngine = new TemplateEngine();			

			templateEngine.Variables["NAME"] = solution_name;

			string fileName = solution_dir + "/autogen.sh";

			StreamWriter writer = new StreamWriter( fileName );

			Stream stream = context.GetTemplateStream ("autogen.sh.template");
			StreamReader reader = new StreamReader(stream);

			templateEngine.Process(reader, writer);

			reader.Close();
			writer.Close();

			// make autogen.sh executable
			Syscall.chmod ( fileName , FilePermissions.S_IXOTH | FilePermissions.S_IROTH | FilePermissions.S_IRWXU | FilePermissions.S_IRWXG );
		}

		void CreateConfigureDotAC ( Combine combine, string defaultConf, IProgressMonitor monitor )
		{
			monitor.Log.WriteLine ( GettextCatalog.GetString ("Creating configure.ac") );
			TemplateEngine templateEngine = new TemplateEngine();			
			templateEngine.Variables["WARNING"] = "Warning: This is an automatically generated file, do not edit!";		
			
			// add solution configuration options
			StringBuilder config_options = new StringBuilder ();
			foreach ( IConfiguration config in combine.Configurations )
			{
				string name = config.Name.ToLower();
				string def = config == combine.ActiveConfiguration ? "YES" : "NO";
				string ac_var = "enable_" + name;

				// test to see if a configuration was enabled
				config_options.AppendFormat ( "AC_ARG_ENABLE({0},\n", name );
				config_options.AppendFormat ("	AC_HELP_STRING([--enable-{0}],\n", name );
				config_options.AppendFormat ("		[Use '{0}' Configuration [default={1}]]),\n", config.Name, def );
				config_options.AppendFormat ( "		{0}=yes, {0}=no)\n", ac_var );
				config_options.AppendFormat ( "AM_CONDITIONAL({0}, test x${1} = xyes)\n", ac_var.ToUpper(), ac_var );

				// if yes, populate some vars
				config_options.AppendFormat ( "if test \"x${0}\" = \"xyes\" ; then\n", ac_var );
				if ( pkgconfig ) AppendPkgConfigVariables ( combine, config.Name, config_options );
				config_options.Append ( "	CONFIG_REQUESTED=\"yes\"\nfi\n" );
			}

			// if no configuration was specified, set to default
			config_options.Append ( "if test -z \"$CONFIG_REQUESTED\" ; then\n" );
			if ( pkgconfig ) AppendPkgConfigVariables ( combine, defaultConf, config_options );
			config_options.AppendFormat ( "	AM_CONDITIONAL({0}, true)\nfi\n", "ENABLE_"
					+ defaultConf.ToUpper()  );

			templateEngine.Variables ["CONFIG_OPTIONS"] = config_options.ToString();

			// build compiler checks
			StringBuilder compiler_checks = new StringBuilder();
			foreach (string compiler in context.GetCommandChecks () ) 
			{
				compiler_checks.AppendFormat ("AC_PATH_PROG({0}, {1}, no)\n", compiler.ToUpper(), compiler);
				compiler_checks.AppendFormat ("if test \"x${0}\" = \"xno\"; then\n", compiler.ToUpper() );
				compiler_checks.AppendFormat ("        AC_MSG_ERROR([{0} Not found])\n", compiler );
				compiler_checks.Append("fi\n");
			}
			templateEngine.Variables["COMPILER_CHECKS"] = compiler_checks.ToString();

			// build list of *.in files
			StringBuilder configFiles = new StringBuilder();
			foreach (string makefile in context.GetAutoConfFiles () ) 
			{
				configFiles.Append( Runtime.FileUtilityService.AbsoluteToRelativePath ( solution_dir, makefile ) );
				configFiles.Append("\n");
			}
			templateEngine.Variables["CONFIG_FILES"] = configFiles.ToString();

			// build list of pkgconfig checks we must make
			StringWriter packageChecks = new StringWriter();
			foreach ( string pkg in context.GetRequiredPackages () ) 
			{
				string pkgvar = AutotoolsContext.GetPkgConfigVariable (pkg); 
				packageChecks.Write("PKG_CHECK_MODULES([");
				packageChecks.Write(pkgvar);
				packageChecks.Write("], [");
				packageChecks.Write(pkg);
				packageChecks.WriteLine("])");
			}
			templateEngine.Variables["PACKAGE_CHECKS"] = packageChecks.ToString();
			templateEngine.Variables["SOLUTION_NAME"] = solution_name;
			templateEngine.Variables["VERSION"] = solution_version;

			string configureFileName = solution_dir + "/configure.ac";

			StreamWriter writer = new StreamWriter(configureFileName);
			Stream stream = context.GetTemplateStream ("configure.ac.template");
			StreamReader reader = new StreamReader(stream);

			templateEngine.Process(reader, writer);

			reader.Close();
			writer.Close();
		}
	
		void AppendPkgConfigVariables ( Combine combine, string config, StringBuilder options )
		{
			string name = config.ToLower();

			StringBuilder refb = new StringBuilder ();
			StringBuilder libb = new StringBuilder ();
			foreach ( CombineEntry ce in combine.GetAllBuildableEntries ( config ) )
			{
				Project p = ce as Project;
				if ( p == null ) continue;

				DotNetProjectConfiguration dnpc = p.Configurations [config] as DotNetProjectConfiguration;
				if ( dnpc == null ) continue;

				if ( dnpc.CompileTarget != CompileTarget.Library ) continue;
				
				string filename = Path.GetFileName ( dnpc.CompiledOutputName );
				string projname = AutotoolsContext.EscapeStringForAutoconf (p.Name.ToUpper());
				
				// provide these for per-library pc files
				options.AppendFormat ( "	{0}_{1}_LIB='{2}'\n", projname , config.ToUpper(), filename);
				options.AppendFormat ( "	AC_SUBST({0}_{1}_LIB)\n", projname, config.ToUpper() );
				
				libb.Append ( " ${pkglibdir}/" + filename  );
				refb.Append ( " -r:${pkglibdir}/" +  filename );
			}

			// must also add referenced dlls
			foreach ( string dll in context.GetReferencedDlls () )
			{
				libb.Append ( " ${pkglibdir}/" + Path.GetFileName (dll) );
				refb.Append ( " -r:${pkglibdir}/" + Path.GetFileName (dll) );
			}
			
			options.AppendFormat ( "	{0}_CONFIG_LIBRARIES='{1}'\n", name.ToUpper(), libb.ToString ());
			options.AppendFormat ( "	{0}_CONFIG_LIBS='{1}'\n", name.ToUpper(), refb.ToString ());
			options.AppendFormat ( "	AC_SUBST({0}_CONFIG_LIBRARIES)\n", name.ToUpper() );
			options.AppendFormat ( "	AC_SUBST({0}_CONFIG_LIBS)\n", name.ToUpper() );
		}
		
		void CreateMakefileInclude (IProgressMonitor monitor)
		{
			monitor.Log.WriteLine ( GettextCatalog.GetString ("Creating Makefile.include") );

			string fileName = solution_dir + "/Makefile.include";

			Stream stream = context.GetTemplateStream ("Makefile.include");

			StreamReader reader = new StreamReader(stream);
			StreamWriter writer = new StreamWriter(fileName);

			writer.Write(reader.ReadToEnd());

			reader.Close();
			writer.Close();
		}

		void CreatePkgConfigFile ( Combine combine, IProgressMonitor monitor, Makefile mfile, AutotoolsContext context )
		{
			if ( !pkgconfig ) return;

			monitor.Log.WriteLine ( GettextCatalog.GetString ("Creating pkg-config file") );

			TemplateEngine templateEngine = new TemplateEngine();			

			templateEngine.Variables["NAME"] = solution_name;
			templateEngine.Variables["DESCRIPTION"] = combine.Description;
			templateEngine.Variables["VERSION"] = solution_version;
			
			StringBuilder pkgs = new StringBuilder ();
			foreach ( string pkg in context.GetRequiredPackages () )
				pkgs.AppendFormat ( "{0} ", pkg );
			templateEngine.Variables ["REQUIRES_PKGS"] = pkgs.ToString ();

			StringBuilder libs = new StringBuilder ();
			StringBuilder libraries = new StringBuilder ();
			foreach ( IConfiguration config in combine.Configurations )
			{
				libs.AppendFormat ( "@{0}_CONFIG_LIBS@ ", config.Name.ToUpper() );
				libraries.AppendFormat ( "@{0}_CONFIG_LIBRARIES@ ", config.Name.ToUpper() );
			}
			templateEngine.Variables ["LIBS"] = libs.ToString ();
			templateEngine.Variables ["LIBRARIES"] = libraries.ToString ();
			
			string fileName = solution_name.ToLower() + ".pc";
			string path = String.Format ( "{0}/{1}.in", solution_dir, fileName );

			StreamWriter writer = new StreamWriter( path );

			Stream stream = context.GetTemplateStream ("package.pc.template");
			StreamReader reader = new StreamReader(stream);

			templateEngine.Process(reader, writer);

			reader.Close();
			writer.Close();
			
			context.AddAutoconfFile ( fileName );
			mfile.AppendToVariable ( "EXTRA_DIST", fileName + ".in" );
			mfile.SetVariable ( "pkgconfigdir", "$(libdir)/pkgconfig" );
			mfile.AppendToVariable ( "pkgconfig_DATA", fileName );
			mfile.AppendToVariable ( "DISTCLEANFILES", fileName );
		}
	}
}

