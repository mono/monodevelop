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
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace JavaBinding
{
	/// <summary>
	/// This class describes the main functionalaty of a language binding
	/// </summary>
	public class JavaLanguageBinding : IDotNetLanguageBinding
	{
		internal const string LanguageName = "Java";
		JavaBindingCompilerServices compilerServices = new JavaBindingCompilerServices ();
		
		static GlobalProperties props = new GlobalProperties ();
		
		public static GlobalProperties Properties {
			get { return props; }
		}
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public bool IsSourceCodeFile (string fileName)
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
		
		public CodeDomProvider GetCodeDomProvider ()
		{
			return null;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".java";
		}
		
		public IParser Parser {
			get { return null; }
		}
		
		public IRefactorer Refactorer {
			get { return null; }
		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion[] { ClrVersion.Net_1_1 };
		}
	}
	
	public class GlobalProperties
	{
		Properties props = (Properties) PropertyService.Get ("JavaBinding.GlobalProps", new Properties ());
		
		public string IkvmPath {
			get { return props.Get ("IkvmPath", ""); }
			set { props.Set ("IkvmPath", value != null ? value : ""); }
		}
		
		public string CompilerCommand {
			get { return props.Get ("CompilerCommand", ""); }
			set { props.Set ("CompilerCommand", value != null ? value : "javac"); }
		}
		
		public JavaCompiler CompilerType {
			get { return (JavaCompiler) props.Get ("CompilerType", 0); }
			set { props.Set ("CompilerType", (int)value); }
		}
		
		public string Classpath {
			get { return props.Get ("Classpath", ""); }
			set { props.Set ("Classpath", value != null ? value : ""); }
		}
	}
}
