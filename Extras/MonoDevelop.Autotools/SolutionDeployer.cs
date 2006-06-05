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

using Mono.Unix.Native;

namespace MonoDevelop.Autotools
{
	public class SolutionDeployer
	{
		string solution_dir;
		string solution_name;
		string solution_version;

		AutotoolsContext context;

		public SolutionDeployer ()
		{
		}

		public bool CanDeploy ( Combine combine )
		{
			IMakefileHandler handler = AutotoolsContext.GetMakefileHandler ( combine );
			if ( !handler.CanDeploy (combine) )	return false;
			return true;
		}
		
		public void Deploy (Combine combine)
		{
			context = new AutotoolsContext ( );
			IMakefileHandler handler = AutotoolsContext.GetMakefileHandler ( combine );
			if ( !handler.CanDeploy (combine) )
			{
				// TODO: throw exception?
				return;
			}

			solution_dir = Path.GetDirectoryName(combine.FileName);
			solution_name = combine.Name;
			// FIXME: pull version out of AssemblyInfo.cs?
			// or wait for http://bugzilla.ximian.com/show_bug.cgi?id=77889
			solution_version = "0.1";
				
			Makefile makefile = handler.Deploy ( context, combine );
			string path = solution_dir + "/Makefile";
			context.AddAutoconfFile ( path );

			CreateAutoGenDotSH ();
			CreateConfigureDotAC ();
			CreateMakefileInclude ();

			AddTopLevelMakefileVars ( makefile );
			
			StreamWriter writer = new StreamWriter ( path + ".am" );
			makefile.Write ( writer );
			writer.Close ();
		}

		void AddTopLevelMakefileVars ( Makefile makefile )
		{
			StringBuilder sb = new StringBuilder ();
			foreach ( string dll in context.GetReferencedDlls() )
			{
				sb.Append (' ');
				sb.Append ( dll );
			}
			string vals = sb.ToString ();

			makefile.SetVariable ( "DLL_REFERENCES", vals );
			makefile.SetVariable ( "EXTRA_DIST", vals );
			makefile.SetVariable ( "pkglib_DATA", vals );
		}
		
		void CreateAutoGenDotSH ()
		{
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
		
		void CreateConfigureDotAC ()
		{
			TemplateEngine templateEngine = new TemplateEngine();			
			templateEngine.Variables["WARNING"] = "Warning: This is an automatically generated file, do not edit!";			
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
				string pkgvar = AutotoolsContext.GetPkgVar (pkg); 
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
		
		void CreateMakefileInclude ()
		{
			string fileName = solution_dir + "/Makefile.include";

			Stream stream = context.GetTemplateStream ("Makefile.include");

			StreamReader reader = new StreamReader(stream);
			StreamWriter writer = new StreamWriter(fileName);

			writer.Write(reader.ReadToEnd());

			reader.Close();
			writer.Close();
		}
	}
}
