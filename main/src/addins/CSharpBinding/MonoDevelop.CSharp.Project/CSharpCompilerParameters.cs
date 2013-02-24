//
// CSharpCompilerParameters.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.CSharp.Project
{
	public enum LangVersion {
		Default = 0,
		ISO_1   = 1,
		ISO_2   = 2,
		Version3 = 3,
		Version4 = 4,
		Version5 = 5
	}
	
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class CSharpCompilerParameters: ConfigurationParameters
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
		
		[ItemProperty ("DefineConstants", DefaultValue = "")]
		string definesymbols = String.Empty;
		
		[ItemProperty ("GenerateDocumentation", DefaultValue = false)]
		bool generateXmlDocumentation = false;
		
		[ItemProperty ("additionalargs", DefaultValue = "")]
		string additionalArgs = string.Empty;
		
		[ItemProperty ("LangVersion", DefaultValue = "Default")]
		string langVersion = "Default";
		
		[ItemProperty ("NoStdLib", DefaultValue = false)]
		bool noStdLib;
		
		[ItemProperty ("TreatWarningsAsErrors", DefaultValue = false)]
		bool treatWarningsAsErrors;

		[ItemProperty("PlatformTarget", DefaultValue="anycpu")]
		string platformTarget = "anycpu";
		
		[ItemProperty("WarningsNotAsErrors", DefaultValue="")]
		string warningsNotAsErrors = "";
		
		
		#region Members required for backwards compatibility. Not used for anything else.
		
		[ItemProperty ("StartupObject", DefaultValue = null)]
		internal string mainclass;
		
		[ProjectPathItemProperty ("ApplicationIcon", DefaultValue = null)]
		internal string win32Icon;

		[ProjectPathItemProperty ("Win32Resource", DefaultValue = null)]
		internal string win32Resource;
	
		[ItemProperty ("CodePage", DefaultValue = null)]
		internal string codePage;
		
		#endregion
		
		
		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();
			
			// Backwards compatibility. Move parameters to the project parameters object
			if (ParentConfiguration != null && ParentConfiguration.ProjectParameters != null) {
				CSharpProjectParameters cparams = (CSharpProjectParameters) ParentConfiguration.ProjectParameters;
				if (win32Icon != null) {
					cparams.Win32Icon = win32Icon;
					win32Icon = null;
				}
				if (win32Resource != null) {
					cparams.Win32Resource = win32Resource;
					win32Resource = null;
				}
				if (mainclass != null) {
					cparams.MainClass = mainclass;
					mainclass = null;
				}
				if (!string.IsNullOrEmpty (codePage)) {
					cparams.CodePage = int.Parse (codePage);
					codePage = null;
				}
			}
		}
	
		public string AdditionalArguments {
			get { return additionalArgs; }
			set { additionalArgs = value ?? string.Empty; }
		}
		
		public LangVersion LangVersion {
			get {
				var val = TryLangVersionFromString (langVersion);
				if (val == null) {
					throw new Exception ("Unknown LangVersion string '" + val + "'");
				}
				return val.Value;
			}
			set {
				var v = TryLangVersionToString (value);
				if (v == null) {
					throw new ArgumentOutOfRangeException ("Unknown LangVersion enum value '" + value + "'");
				}
				langVersion = v;
			}
		}
		
		public override void AddDefineSymbol (string symbol)
		{
			definesymbols += symbol + ";";
		}

		public override bool HasDefineSymbol (string symbol)
		{
			return definesymbols.Contains (symbol);
		}

		public override void RemoveDefineSymbol (string symbol)
		{
			definesymbols = definesymbols.Replace (symbol + ";", "");
		}
		
#region Code Generation
		
		public string DefineSymbols {
			get {
				return definesymbols;
			}
			set {
				definesymbols = value ?? string.Empty;
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
		
		public string PlatformTarget {
			get {
				return platformTarget;
			}
			set {
				platformTarget = value ?? string.Empty;
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
		
		public string WarningsNotAsErrors {
			get {
				return warningsNotAsErrors;
			}
			set {
				warningsNotAsErrors = value;
			}
		}
#endregion

		static LangVersion? TryLangVersionFromString (string value)
		{
			switch (value) {
			case "Default": return LangVersion.Default;
			case "ISO-1": return LangVersion.ISO_1;
			case "ISO-2": return LangVersion.ISO_2;
			case "3": return LangVersion.Version3;
			case "4": return LangVersion.Version4;
			case "5": return LangVersion.Version5;
			default: return null;
			}
		}

		internal static string TryLangVersionToString (LangVersion value)
		{
			switch (value) {
			case LangVersion.Default: return "Default";
			case LangVersion.ISO_1: return "ISO-1";
			case LangVersion.ISO_2: return "ISO-2";
			case LangVersion.Version3: return "3";
			case LangVersion.Version4: return "4";
			case LangVersion.Version5: return "5";
			default: return null;
			}
		}
	}
}
