// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;
using MonoDevelop.Gui.Components;
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;

namespace CSharpBinding
{
	public enum CsharpCompiler {
		Csc,
		Mcs
	};
	
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class CSharpCompilerParameters: ICloneable
	{
		// Configuration parameters
		
		[ItemProperty ("compiler")]
		CsharpCompiler csharpCompiler = CsharpCompiler.Csc;
		
		[ItemProperty ("warninglevel")]
		int  warninglevel       = 4;
		
		[ItemProperty ("nowarn", DefaultValue = "")]
		string noWarnings      = String.Empty;
		
		[ItemProperty ("optimize")]
		bool optimize           = true;
		
		[ItemProperty ("unsafecodeallowed")]
		bool unsafecode         = false;
		
		[ItemProperty ("generateoverflowchecks")]
		bool generateOverflowChecks = true;
		
		[ItemProperty ("mainclass")]
		string         mainclass     = null;
		
		[ItemProperty ("definesymbols", DefaultValue = "")]
		string         definesymbols = String.Empty;
		
		[ItemProperty ("generatexmldocumentation")]
		bool generateXmlDocumentation = false;
		
		[ProjectPathItemProperty ("win32Icon", DefaultValue = "")]
		string         win32Icon     = String.Empty;
		
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		[Browsable(false)]
		public CsharpCompiler CsharpCompiler {
			get {
				return csharpCompiler;
			}
			set {
				csharpCompiler = value;
			}
		}
		
		[Browsable(false)]
		public string Win32Icon {
			get {
				return win32Icon;
			}
			set {
				win32Icon = value;
			}
		}
#region Code Generation
		[DefaultValue("")]
		[LocalizedProperty("${res:BackendBindings.CompilerOptions.CodeGeneration.MainClass}",
		                   Category    = "${res:BackendBindings.CompilerOptions.CodeGeneration}",
		                   Description = "${res:BackendBindings.CompilerOptions.CodeGeneration.MainClass.Description}")]
		public string MainClass {
			get {
				return mainclass;
			}
			set {
				mainclass = value;
			}
		}
		
		[DefaultValue("")]
		[LocalizedProperty("${res:BackendBindings.CompilerOptions.CodeGeneration.DefineSymbols}",
		                   Category    = "${res:BackendBindings.CompilerOptions.CodeGeneration}",
		                   Description = "${res:BackendBindings.CompilerOptions.CodeGeneration.DefineSymbols.Description}")]
		public string DefineSymbols {
			get {
				return definesymbols;
			}
			set {
				definesymbols = value;
			}
		}
		
		[DefaultValue(true)]
		[LocalizedProperty("${res:BackendBindings.CompilerOptions.CodeGeneration.Optimize}",
		                   Category    = "${res:BackendBindings.CompilerOptions.CodeGeneration}",
		                   Description = "${res:BackendBindings.CompilerOptions.CodeGeneration.Optimize.Description}")]
		public bool Optimize {
			get {
				return optimize;
			}
			set {
				optimize = value;
			}
		}
		
		[DefaultValue(false)]
		[LocalizedProperty("${res:BackendBindings.CompilerOptions.CodeGeneration.UnsafeCode}",
		                   Category    = "${res:BackendBindings.CompilerOptions.CodeGeneration}",
		                   Description = "${res:BackendBindings.CompilerOptions.CodeGeneration.UnsafeCode.Description}")]
		public bool UnsafeCode {
			get {
				return unsafecode;
			}
			set {
				unsafecode = value;
			}
		}
		
		[DefaultValue(true)]
		[LocalizedProperty("${res:BackendBindings.CompilerOptions.CodeGeneration.GenerateOverflowChecks}",
		                   Category    = "${res:BackendBindings.CompilerOptions.CodeGeneration}",
		                   Description = "${res:BackendBindings.CompilerOptions.CodeGeneration.GenerateOverflowChecks.Description}")]
		public bool GenerateOverflowChecks {
			get {
				return generateOverflowChecks;
			}
			set {
				generateOverflowChecks = value;
			}
		}
		
		[DefaultValue(false)]
		[LocalizedProperty("${res:BackendBindings.CompilerOptions.CodeGeneration.GenerateXmlDocumentation}",
		                   Category    = "${res:BackendBindings.CompilerOptions.CodeGeneration}",
		                   Description = "${res:BackendBindings.CompilerOptions.CodeGeneration.GenerateXmlDocumentation.Description}")]
		public bool GenerateXmlDocumentation {
			get {
				return generateXmlDocumentation;
			}
			set {
				generateXmlDocumentation = value;
			}
		}
		
#endregion

#region Errors and Warnings 
		[DefaultValue(4)]
		[LocalizedProperty("${res:BackendBindings.CompilerOptions.WarningAndErrorCategory.WarningLevel}",
		                   Category    = "${res:BackendBindings.CompilerOptions.WarningAndErrorCategory}",
		                   Description = "${res:BackendBindings.CompilerOptions.WarningAndErrorCategory.WarningLevel.Description}")]
		public int WarningLevel {
			get {
				return warninglevel;
			}
			set {
				warninglevel = value;
			}
		}
		
		[DefaultValue("")]
		[LocalizedProperty("${res:BackendBindings.CompilerOptions.WarningAndErrorCategory.NoWarnings}",
		                   Category    = "${res:BackendBindings.CompilerOptions.WarningAndErrorCategory}",
		                   Description = "${res:BackendBindings.CompilerOptions.WarningAndErrorCategory.NoWarnings.Description}")]
		public string NoWarnings {
			get {
				return noWarnings;
			}
			set {
				noWarnings = value;
			}
		}
#endregion
	}
}
