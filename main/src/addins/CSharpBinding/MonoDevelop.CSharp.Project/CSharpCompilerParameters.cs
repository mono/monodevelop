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
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using Mono.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MonoDevelop.CSharp.Project
{
	public enum LangVersion {
		Default = 0,
		ISO_1   = 1,
		ISO_2   = 2,
		Version3 = 3,
		Version4 = 4,
		Version5 = 5,
		Version6 = 6
	}
	
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class CSharpCompilerParameters: DotNetCompilerParameters
	{
		// Configuration parameters

		int? warninglevel = 4;
		
		[ItemProperty ("NoWarn", DefaultValue = "")]
		string noWarnings = String.Empty;
		
		bool? optimize = false;
		
		[ItemProperty ("AllowUnsafeBlocks", DefaultValue = false)]
		bool unsafecode = false;
		
		[ItemProperty ("CheckForOverflowUnderflow", DefaultValue = false)]
		bool generateOverflowChecks;
		
		[ItemProperty ("DefineConstants", DefaultValue = "")]
		string definesymbols = String.Empty;

		[ProjectPathItemProperty ("DocumentationFile")]
		FilePath documentationFile;
		
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

		protected override void Write (IPropertySet pset)
		{
			pset.SetPropertyOrder ("DebugSymbols", "DebugType", "Optimize", "OutputPath", "DefineConstants", "ErrorReport", "WarningLevel", "TreatWarningsAsErrors", "DocumentationFile");

			base.Write (pset);

			if (optimize.HasValue)
				pset.SetValue ("Optimize", optimize.Value);
			if (warninglevel.HasValue)
				pset.SetValue ("WarningLevel", warninglevel.Value);
		}

		protected override void Read (IPropertySet pset)
		{
			base.Read (pset);

			var prop = pset.GetProperty ("GenerateDocumentation");
			if (prop != null && documentationFile != null) {
				if (prop.GetValue<bool> ())
					documentationFile = ParentConfiguration.CompiledOutputName.ChangeExtension (".xml");
				else
					documentationFile = null;
			}

			optimize = pset.GetValue ("Optimize", (bool?)null);
			warninglevel = pset.GetValue<int?> ("WarningLevel", null);
		}

		public override CompilationOptions CreateCompilationOptions ()
		{
			var project = (CSharpProject) ParentProject;
			var options = new CSharpCompilationOptions (
				OutputKind.ConsoleApplication,
				false,
				null,
				project.MainClass,
				"Script",
				null,
				OptimizationLevel.Debug,
				GenerateOverflowChecks,
				UnsafeCode,
				null,
				ParentConfiguration.SignAssembly ? ParentConfiguration.AssemblyKeyFile : null,
				ImmutableArray<byte>.Empty,
				null,
				Microsoft.CodeAnalysis.Platform.AnyCpu,
				ReportDiagnostic.Default,
				WarningLevel,
				null,
				false,
				assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
				strongNameProvider: new DesktopStrongNameProvider ()
			);

			return options.WithPlatform (GetPlatform ())
				.WithGeneralDiagnosticOption (TreatWarningsAsErrors ? ReportDiagnostic.Error : ReportDiagnostic.Default)
				.WithOptimizationLevel (Optimize ? OptimizationLevel.Release : OptimizationLevel.Debug)
				.WithSpecificDiagnosticOptions (GetSuppressedWarnings ().ToDictionary (
					suppress => suppress, _ => ReportDiagnostic.Suppress));
		}

		Microsoft.CodeAnalysis.Platform GetPlatform ()
		{
			Microsoft.CodeAnalysis.Platform platform;
			if (Enum.TryParse (PlatformTarget, true, out platform))
				return platform;

			return Microsoft.CodeAnalysis.Platform.AnyCpu;
		}

		IEnumerable<string> GetSuppressedWarnings ()
		{
			string warnings = NoWarnings ?? string.Empty;
			var items = warnings.Split (new [] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct ();

			foreach (string warning in items) {
				if (warning.StartsWith ("CS", StringComparison.OrdinalIgnoreCase)) {
					yield return warning;
				} else {
					yield return "CS" + warning;
				}
			}
		}

		public override Microsoft.CodeAnalysis.ParseOptions CreateParseOptions (DotNetProjectConfiguration configuration)
		{
			var symbols = GetDefineSymbols ();
			if (configuration != null)
				symbols = symbols.Concat (configuration.GetDefineSymbols ()).Distinct ();
			return new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions (
				GetRoslynLanguageVersion (langVersion),
				Microsoft.CodeAnalysis.DocumentationMode.Parse,
				Microsoft.CodeAnalysis.SourceCodeKind.Regular,
				ImmutableArray<string>.Empty.AddRange (symbols)
			);
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

#region Code Generation

		public override void AddDefineSymbol (string symbol)
		{
			var symbols = new List<string> (GetDefineSymbols ());
			symbols.Add (symbol);
			definesymbols = string.Join (";", symbols) + ";";
		}

		public override IEnumerable<string> GetDefineSymbols ()
		{
			return definesymbols.Split (';', ',', ' ', '\t').Where (s => SyntaxFacts.IsValidIdentifier (s) && !string.IsNullOrWhiteSpace (s));
		}

		public override void RemoveDefineSymbol (string symbol)
		{
			var symbols = new List<string> (GetDefineSymbols ());
			symbols.Remove (symbol);

			if (symbols.Count > 0)
				definesymbols = string.Join (";", symbols) + ";";
			else
				definesymbols = string.Empty;
		}
		
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
				return optimize ?? false;
			}
			set {
				if (value != Optimize)
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
		
		public FilePath DocumentationFile {
			get {
				return documentationFile;
			}
			set {
				documentationFile = value;
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
				return warninglevel ?? 4;
			}
			set {
				if (warninglevel.HasValue) {
					warninglevel = value;
				} else {
					if (value != 4)
						warninglevel = value; 
				}
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

		public override bool NoStdLib {
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
			case "6": return LangVersion.Version6;
			default: return null;
			}
		}

		Microsoft.CodeAnalysis.CSharp.LanguageVersion GetRoslynLanguageVersion (string value)
		{
			switch (value) {
			case "ISO-1": return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp1;
			case "ISO-2": return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp2;
			case "3": return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp3;
			case "4": return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp4;
			case "5": return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp5;
			case "6": return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6;
			default: return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6;
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
			case LangVersion.Version6: return "6";
			default: return null;
			}
		}
	}
}
