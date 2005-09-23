// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Xml;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Gui;
using MonoDevelop.Services;
using MonoDevelop.Core.Properties;

namespace JavaBinding
{
	/// <summary>
	/// This class describes the main functionalaty of a language binding
	/// </summary>
	public class JavaLanguageBinding : ILanguageBinding
	{
		internal const string LanguageName = "Java";
		JavaBindingCompilerServices compilerServices = new JavaBindingCompilerServices ();
		
		static GlobalProperties props = new GlobalProperties ();
		
		public JavaLanguageBinding ()
		{
			Runtime.ProjectService.DataContext.IncludeType (typeof(JavaCompilerParameters));
		}
		
		public static GlobalProperties Properties {
			get { return props; }
		}
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public bool CanCompile(string fileName)
		{
			Debug.Assert(compilerServices != null);
			return compilerServices.CanCompile(fileName);
		}
		
		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			Debug.Assert(compilerServices != null);
			return compilerServices.Compile (projectFiles, references, configuration, monitor);
		}
		
		public void GenerateMakefile (Project project, Combine parentCombine)
		{
			// FIXME: dont abort for now
			// throw new NotImplementedException ();
		}
		
		public ICloneable CreateCompilationParameters (XmlElement projectOptions)
		{
			JavaCompilerParameters parameters = new JavaCompilerParameters ();
			parameters.ClassPath = Path.Combine (Path.Combine (Properties.IkvmPath, "classpath"), "mscorlib.jar");
			if (Properties.Classpath.Length > 0)
				parameters.ClassPath += ": " + Properties.Classpath;
				
			parameters.Compiler = Properties.CompilerType;
			parameters.CompilerPath = Properties.CompilerCommand;
			
			if (projectOptions != null) {
				if (projectOptions.Attributes["MainClass"] != null) {
					parameters.MainClass = projectOptions.GetAttribute ("MainClass");
				}
				if (projectOptions.Attributes["ClassPath"] != null) {
					parameters.ClassPath += ":" + projectOptions.GetAttribute ("ClassPath");
				}
			}
			return parameters;
		}
		
		// http://www.nbirn.net/Resources/Developers/Conventions/Commenting/Java_Comments.htm#CommentBlock
		public string CommentTag
		{
			get { return "//"; }
		}
	}
	
	public class GlobalProperties
	{
		IProperties props = (IProperties) Runtime.Properties.GetProperty ("JavaBinding.GlobalProps", new DefaultProperties ());
		
		public string IkvmPath {
			get { return props.GetProperty ("IkvmPath", ""); }
			set { props.SetProperty ("IkvmPath", value != null ? value : ""); }
		}
		
		public string CompilerCommand {
			get { return props.GetProperty ("CompilerCommand", ""); }
			set { props.SetProperty ("CompilerCommand", value != null ? value : "javac"); }
		}
		
		public JavaCompiler CompilerType {
			get { return (JavaCompiler) props.GetProperty ("CompilerType", 0); }
			set { props.SetProperty ("CompilerType", (int)value); }
		}
		
		public string Classpath {
			get { return props.GetProperty ("Classpath", ""); }
			set { props.SetProperty ("Classpath", value != null ? value : ""); }
		}
	}
}
