
using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Autotools;
using MonoDevelop.Projects;
using CSharpBinding;

namespace CSharpBinding.Autotools
{
	public class CSharpAutotoolsSetup : ISimpleAutotoolsSetup
	{
		public string GetCompilerCommand ( Project project, string configuration )
		{
			DotNetProject dp = project as DotNetProject;
			if ( !this.CanDeploy ( project ) || dp == null)
				throw new Exception ( "Not a deployable project." );
			
			switch (dp.ClrVersion) {
			case ClrVersion.Net_1_1:
				return "mcs";
			case ClrVersion.Net_2_0:
				return "gmcs";
			case ClrVersion.Clr_2_1:
				return "smcs";
			default:
				throw new Exception ("Cannot handle unknown runtime version ClrVersion.'" + dp.ClrVersion.ToString () + "'.");
			}
		}

		public string GetCompilerFlags ( Project project, string configuration )
		{
			if ( !this.CanDeploy ( project ) )
				throw new Exception ( "Not a deployable project." );
			
			DotNetProjectConfiguration config = 
				project.Configurations [configuration] as DotNetProjectConfiguration;

			if ( config == null ) return "";
			
			CSharpCompilerParameters parameters = (CSharpCompilerParameters) config.CompilationParameters;
			StringWriter writer = new StringWriter();
			
			writer.Write(" -noconfig");
			writer.Write(" -codepage:utf8");
			
			if (parameters.UnsafeCode) {
				writer.Write(" -unsafe");
			}
			writer.Write(" -warn:" + parameters.WarningLevel);
			if(parameters.Optimize)
				writer.Write(" -optimize+");
			else
				writer.Write(" -optimize-");

			if(parameters.NoWarnings != null && parameters.NoWarnings != "") {
				writer.Write(" \"-nowarn:" + parameters.NoWarnings + '"');
			}

			if(config.DebugMode) {
				writer.Write(" -debug");
				//Check whether we have a DEBUG define
				bool hasDebugDefine = false;
				foreach (string define in parameters.DefineSymbols.Split (';')) {
					if (String.Compare (define, "DEBUG") == 0) {
						hasDebugDefine = true;
						break;
					}
				}
				if (!hasDebugDefine)
					writer.Write (" -define:DEBUG");
			}
			
			if (!string.IsNullOrEmpty (parameters.AdditionalArguments)) {
				writer.Write (" " + parameters.AdditionalArguments + " ");
			}
			
			switch (parameters.LangVersion) {
			case LangVersion.Default:
				break;
			case LangVersion.ISO_1:
				writer.Write (" -langversion:ISO-1 ");
				break;
			case LangVersion.ISO_2:
				writer.Write (" -langversion:ISO-2 ");
				break;
			default:
				throw new Exception ("Invalid LangVersion enum value '" + parameters.LangVersion.ToString () + "'");
			}
			
			
			// TODO check path and add to extradist...
			//if (parameters.Win32Icon != null && parameters.Win32Icon.Length > 0) {
			//	writer.Write(" \"-win32icon:" + compilerparameters.Win32Icon + "\"");
			//}
			
			if (parameters.DefineSymbols.Length > 0) {
				writer.Write (" \"-define:" + parameters.DefineSymbols + '"');
			}
				
			if (parameters.MainClass != null && parameters.MainClass != "") {
				writer.Write (" \"-main:" + parameters.MainClass + '"');
			}

			if (config.SignAssembly)
				writer.Write (" \"-keyfile:" + project.GetRelativeChildPath (config.AssemblyKeyFile) + '"');
			
			// TODO check paths and add to extradist?
			//if (parameters.GenerateXmlDocumentation) {
			//	writer.WriteLine(" \"-doc:" + Path.ChangeExtension(exe, ".xml") + '"');
			//}
		
			return writer.ToString();
		}
		
		public bool CanDeploy ( Project project )
		{
			DotNetProject csproj = project as DotNetProject;
			if ( csproj != null )
				if ( csproj.LanguageName == "C#" ) return true;
			return false;
		}
	}
}
