//  CSharpCompilerParameters.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace CSharpBinding
{
	public enum LangVersion {
		Default = 0,
		ISO_1   = 1,
		ISO_2   = 2
	}
	
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class CSharpCompilerParameters: ICloneable
	{
		// Configuration parameters
		
		[ItemProperty ("WarningLevel")]
		int  warninglevel = 4;
		
		[ItemProperty ("NoWarn", DefaultValue = "")]
		string noWarnings = String.Empty;
		
		[ItemProperty ("Optimize")]
		bool optimize;
		
		[ItemProperty ("AllowUnsafeBlocks", DefaultValue = false)]
		bool unsafecode = false;
		
		[ItemProperty ("CheckForOverflowUnderflow", DefaultValue = false)]
		bool generateOverflowChecks;
		
		[ItemProperty ("StartupObject", DefaultValue = null)]
		string mainclass = null;
		
		[ItemProperty ("DefineConstants", DefaultValue = "")]
		string definesymbols = String.Empty;
		
		[ItemProperty ("GenerateDocumentation", DefaultValue = false)]
		bool generateXmlDocumentation = false;
		
		[ProjectPathItemProperty ("ApplicationIcon", DefaultValue = "")]
		string win32Icon = String.Empty;

		[ProjectPathItemProperty ("Win32Resource", DefaultValue = "")]
		string win32Resource = String.Empty;
	
		[ItemProperty ("CodePage", DefaultValue = 0)]
		int codePage;
		
		[ItemProperty ("additionalargs", DefaultValue = "")]
		string additionalArgs = string.Empty;
		
		[ItemProperty ("LangVersion", DefaultValue = LangVersion.Default)]
		LangVersion langVersion = LangVersion.Default;
		
		[ItemProperty ("NoStdLib", DefaultValue = false)]
		bool noStdLib;
		
		[ItemProperty ("TreatWarningsAsErrors", DefaultValue = false)]
		bool treatWarningsAsErrors;
	
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		public int CodePage {
			get {
				return codePage;
			}
			set {
				codePage = value;
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
		
		[Browsable(false)]
		public string Win32Resource {
			get {
				return win32Resource;
			}
			set {
				win32Resource = value;
			}
		}
		
		public string AdditionalArguments {
			get { return additionalArgs; }
			set { additionalArgs = value; }
		}
		
		public LangVersion LangVersion {
			get { return langVersion; }
			set { langVersion = value; }
		}

#region Code Generation
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
		
		public bool Optimize {
			get {
				return optimize;
			}
			set {
				optimize = value;
			}
		}
		
		public bool UnsafeCode {
			get {
				return unsafecode;
			}
			set {
				unsafecode = value;
			}
		}
		
		public bool GenerateOverflowChecks {
			get {
				return generateOverflowChecks;
			}
			set {
				generateOverflowChecks = value;
			}
		}
		
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
		public int WarningLevel {
			get {
				return warninglevel;
			}
			set {
				warninglevel = value;
			}
		}
		
		public string NoWarnings {
			get {
				return noWarnings;
			}
			set {
				noWarnings = value;
			}
		}

		public bool NoStdLib {
			get {
				return noStdLib;
			}
			set {
				noStdLib = value;
			}
		}

		public bool TreatWarningsAsErrors {
			get {
				return treatWarningsAsErrors;
			}
			set {
				treatWarningsAsErrors = value;
			}
		}
#endregion
	}
}
