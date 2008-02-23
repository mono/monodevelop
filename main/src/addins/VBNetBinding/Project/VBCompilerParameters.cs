//  VBCompilerParameters.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

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
		
		[ProjectPathItemProperty("win32Resource")]
		string win32Resource = String.Empty;

		[ItemProperty("imports")]
		string imports = String.Empty;
		
		[ItemProperty("additionalParameters")]
		string additionalParameters = String.Empty;
		
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
		public bool UnsafeCode {
			get {
				return unsafecode;
			}
			set {
				unsafecode = value;
			}
		}
		
		[DefaultValue(false)]
		public bool GenerateXmlDocumentation {
			get {
				return generateXmlDocumentation;
			}
			set {
				generateXmlDocumentation = value;
			}
		}
		
		
		[DefaultValue(4)]
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
		
		public string Win32Resource
		{
			get {
				return win32Resource;
			}
			set {
				win32Resource = value;
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

		public string AdditionalParameters {
			get {
				return additionalParameters;
			}
			set {
				additionalParameters = value;
			}
		}
	}
}
