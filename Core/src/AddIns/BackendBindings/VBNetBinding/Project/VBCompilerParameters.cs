// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;

namespace VBBinding {
	
	public enum VBCompiler {
		Vbc,
		Mbas
	};
	
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class VBCompilerParameters: ICloneable
	{
		[ItemProperty("compilerversion")]
		string vbCompilerVersion = String.Empty;
		
		[ItemProperty("compiler")]
		VBCompiler vbCompiler = VBCompiler.Mbas;
		
		[ItemProperty("warninglevel")]
		int  warninglevel       = 4;
		
		[ItemProperty("nowarn")]
		string noWarnings      = String.Empty;
		
		[ItemProperty("optimize")]
		bool optimize = true;
		
		[ItemProperty("unsafecodeallowed")]
		bool unsafecode         = false;
		
		[ItemProperty("generateoverflowchecks")]
		bool generateOverflowChecks = true;
		
		[ItemProperty("rootnamespace")]
		string rootnamespace = String.Empty;
		
		[ItemProperty("mainclass")]
		string mainclass = null;
		
		[ItemProperty("definesymbols")]
		string definesymbols = String.Empty;
		
		[ItemProperty("generatexmldocumentation")]
		bool generateXmlDocumentation = false;
		
		[ItemProperty("optionexplicit")]
		bool optionExplicit = true;
		
		[ItemProperty("optionstrict")]
		bool optionStrict = false;
		
		[ProjectPathItemProperty("win32Icon")]
		string win32Icon = String.Empty;
		
		[ItemProperty("imports")]
		string imports = String.Empty;
		
		[ProjectPathItemProperty("VBDOC-outputfile")]
		string outputfile = String.Empty;
		
		[ItemProperty("VBDOC-filestoparse")]
		string filestoparse = String.Empty;
		
		[ItemProperty("VBDOC-commentprefix")]
		string commentprefix = "'";
		
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		[Browsable(false)]
		public string VBCompilerVersion
		{
			get {
				return vbCompilerVersion;
			}
			set {
				vbCompilerVersion = value;
			}
		} 
		
		[Browsable(false)]
		public VBCompiler VBCompiler {
			get {
				return vbCompiler;
			}
			set {
				vbCompiler = value;
			}
		}
		
		public bool GenerateOverflowChecks
		{
			get {
				return generateOverflowChecks;
			}
			set {
				generateOverflowChecks = value;
			}
		}
		
		[DefaultValue(false)]
//		[LocalizedProperty("${res:BackendBindings.CompilerOptions.CodeGeneration.UnsafeCode}",
//		                   Category    = "${res:BackendBindings.CompilerOptions.CodeGeneration}",
//		                   Description = "${res:BackendBindings.CompilerOptions.CodeGeneration.UnsafeCode.Description}")]
		public bool UnsafeCode {
			get {
				return unsafecode;
			}
			set {
				unsafecode = value;
			}
		}
		
		[DefaultValue(false)]
//		[LocalizedProperty("${res:BackendBindings.CompilerOptions.CodeGeneration.GenerateXmlDocumentation}",
//		                   Category    = "${res:BackendBindings.CompilerOptions.CodeGeneration}",
//		                   Description = "${res:BackendBindings.CompilerOptions.CodeGeneration.GenerateXmlDocumentation.Description}")]
		public bool GenerateXmlDocumentation {
			get {
				return generateXmlDocumentation;
			}
			set {
				generateXmlDocumentation = value;
			}
		}
		
		
		[DefaultValue(4)]
//		[LocalizedProperty("${res:BackendBindings.CompilerOptions.WarningAndErrorCategory.WarningLevel}",
//		                   Category    = "${res:BackendBindings.CompilerOptions.WarningAndErrorCategory}",
//		                   Description = "${res:BackendBindings.CompilerOptions.WarningAndErrorCategory.WarningLevel.Description}")]
		public int WarningLevel {
			get {
				return warninglevel;
			}
			set {
				warninglevel = value;
			}
		}
		
		public string Imports
		{
			get {
				return imports;
			}
			set {
				imports = value;
			}
		}
		
		public string Win32Icon
		{
			get {
				return win32Icon;
			}
			set {
				win32Icon = value;
			}
		}
		
		public string RootNamespace
		{
			get {
				return rootnamespace;
			}
			set {
				rootnamespace = value;
			}
		}
		
		public string DefineSymbols
		{
			get {
				return definesymbols;
			}
			set {
				definesymbols = value;
			}
		}
		
		public bool Optimize
		{
			get {
				return optimize;
			}
			set {
				optimize = value;
			}
		}
		
		public string MainClass
		{
			get {
				return mainclass;
			}
			set {
				mainclass = value;
			}
		}
		
		public bool OptionExplicit
		{
			get {
				return optionExplicit;
			}
			set {
				optionExplicit = value;
			}
		}
		
		public bool OptionStrict
		{
			get {
				return optionStrict;
			}
			set {
				optionStrict = value;
			}
		}
		
		public string VBDOCOutputFile
		{
			get {
				return outputfile;
			}
			set {
				outputfile = value;
			}
		}
		
		public string[] VBDOCFiles
		{
			get {
				return filestoparse.Split(';');
			}
			set {
				filestoparse = System.String.Join(";", value);
			}
		}
		
		public string VBDOCCommentPrefix
		{
			get {
				return commentprefix;
			}
			set {
				commentprefix = value;
			}
		}
	}
}
