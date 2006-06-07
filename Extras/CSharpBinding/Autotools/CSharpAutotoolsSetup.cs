
using System;
using System.IO;
using MonoDevelop.Autotools;
using MonoDevelop.Projects;
using CSharpBinding;

namespace CSharpBinding.Autotools
{
	public class CSharpAutotoolsSetup : ISimpleAutotoolsSetup
	{
		public string GetCompilerCommand ( Project project )
		{
			if ( !this.CanDeploy ( project ) )
				throw new Exception ( "Not a deployable project." );
			
			return "mcs";
		}

		public string GetCompilerFlags ( Project project )
		{
			if ( !this.CanDeploy ( project ) )
				throw new Exception ( "Not a deployable project." );
			
			DotNetProject dotNetProject = (DotNetProject) project;
				
			DotNetProjectConfiguration config =
			(DotNetProjectConfiguration) dotNetProject.Configurations["Release"];
			
			//Console.WriteLine ( config.CompilationParameters );
			CSharpCompilerParameters parameters = (CSharpCompilerParameters) config.CompilationParameters;
			StringWriter writer = new StringWriter();
			
			writer.Write(" -noconfig");
			writer.Write(" -codepage:utf8");
			
			if (parameters.UnsafeCode) {
				writer.Write(" -unsafe");
			}
			writer.Write(" -warn:" + parameters.WarningLevel);
			if(!parameters.Optimize) {
				writer.Write(" -optimize-");
			}
			if(parameters.NoWarnings != null && parameters.NoWarnings != "") {
				writer.Write(" \"-nowarn:" + parameters.NoWarnings + '"');
			}
			if(config.DebugMode) {
				writer.Write(" -debug -d:DEBUG");	
			}
			
			// TODO check path and add to extradist...
			//if (parameters.Win32Icon != null && parameters.Win32Icon.Length > 0) {
			//	writer.Write(" \"-win32icon:" + compilerparameters.Win32Icon + "\"");
			//}
			
			if (parameters.DefineSymbols.Length > 0) {
				writer.WriteLine(" \"-define:" + parameters.DefineSymbols + '"');
			}
				
			if (parameters.MainClass != null && parameters.MainClass != "") {
				writer.WriteLine(" \"-main:" + parameters.MainClass + '"');
			}
			
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
