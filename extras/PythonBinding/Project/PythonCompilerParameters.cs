using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Projects;

namespace PythonBinding
{
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class PythonCompilerParameters : ProjectConfiguration
	{
		CompilerOptions compilerOptions = new CompilerOptions ();
		
		public CompilerOptions CurrentCompilerOptions {
			get {
				return compilerOptions;
			}
		}
		
		public string OutputPath {
			get {
				return OutputDirectory;
			}
			set {
				OutputDirectory = value;
			}
		}
		
		public string AssemblyName {
			get {
				return OutputAssembly;
			}
			set {
				OutputAssembly = value;
			}
		}
		
		[DefaultValue(CompilationTarget.Exe)]
		public CompilationTarget CompilationTarget {
			get {
				return compilerOptions.compilationTarget;
			}
			set {
				compilerOptions.compilationTarget = value;
			}
		}
		
		[DefaultValue(false)]
		public bool IncludeDebugInformation {
			get {
				return compilerOptions.includeDebugInformation;
			}
			set {
				compilerOptions.includeDebugInformation = value;
			}
		}
		
		public PythonCompilerParameters ()
		{
		}
		
		public PythonCompilerParameters (string name)
		{
			this.name = name;
		}
		
		[XmlNodeName("CompilerOptions")]
		public class CompilerOptions
		{
			[XmlAttribute("compilationTarget")]
			public CompilationTarget compilationTarget = CompilationTarget.Exe;
			
			[XmlAttribute("includeDebugInformation")]
			internal bool includeDebugInformation = false;
			
			public string GenerateOptions ()
			{
				StringBuilder options = new StringBuilder ();
				switch (compilationTarget) {
					case PythonBinding.CompilationTarget.Dll:
						options.Append ("/dll ");
						break;
					case PythonBinding.CompilationTarget.Exe:
						options.Append ("/exe ");
						break;
					default:
						throw new System.NotSupportedException ("Unsupported compilation target : " + compilationTarget);
				}
				
				if (includeDebugInformation) {
					options.Append ("/DEBUG ");
				}
				
				return options.ToString ();
			}
		}
	}
}
