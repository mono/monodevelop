// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.Diagnostics;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;

namespace JavaBinding
{
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class JavaCompilerParameters: ICloneable
	{
		[ItemProperty("deprecation")]
		bool deprecation = true;
		
		[ItemProperty("optimize")]
		bool optimize = true;
		
		[ItemProperty("mainclass")]
		string  mainclass = null;
		
		[ItemProperty("definesymbols")]
		string definesymbols = String.Empty;
		
		[ItemProperty("classpath")]
		string classpath = String.Empty;
		
		[ItemProperty ("compiler")]
		JavaCompiler compiler = JavaCompiler.Gcj;

		[ItemProperty("compilerpath")]
		string compilerpath = "gcj";		
		
		[ItemProperty("genwarnings")]
		bool genwarnings = false;
		
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		public bool GenWarnings {
			get {
				return genwarnings;
			}
			set {
				genwarnings = value;
			}
		}
		
		public string ClassPath {
			get {
				return classpath;
			}
			set {
				classpath = value;
			}
		}

		public JavaCompiler Compiler {
			get {
				return compiler;
			}
			set {
				compiler = value;
			}
		}
		
		public string CompilerPath {
			get {
				return compilerpath;
			}
			set {
				compilerpath = value;
			}
		}
		
		public bool Deprecation {
			get {
				return deprecation;
			}
			set {
				deprecation = value;
			}
		}
		
		public bool Optimize {
			get {
				return optimize;
			}
			set {
				optimize = value;
			}
		}
		
		public string MainClass {
			get {
				return mainclass;
			}
			set {
				mainclass = value;
			}
		}
		
		public string DefineSymbols {
			get {
				return definesymbols;
			}
			set {
				definesymbols = value;
			}
		}
	}
}
