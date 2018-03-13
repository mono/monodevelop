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
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

namespace MonoDevelop.CSharp.Project
{
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class CSharpCompilerParameters : DotNetCompilerParameters
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

		[ItemProperty ("PlatformTarget", DefaultValue = "anycpu")]
		string platformTarget = "anycpu";

		[ItemProperty ("WarningsNotAsErrors", DefaultValue = "")]
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
			var project = (CSharpProject)ParentProject;
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

		public override ParseOptions CreateParseOptions (DotNetProjectConfiguration configuration)
		{
			var symbols = GetDefineSymbols ();
			if (configuration != null)
				symbols = symbols.Concat (configuration.GetDefineSymbols ()).Distinct ();

			langVersion.TryParse (out LanguageVersion lv);

			return new CSharpParseOptions (
				lv,
				DocumentationMode.Parse,
				SourceCodeKind.Regular,
				ImmutableArray<string>.Empty.AddRange (symbols)
			);
		}


		public LanguageVersion LangVersion {
			get {
				if (!langVersion.TryParse (out LanguageVersion val)) {
					throw new Exception ("Unknown LangVersion string '" + langVersion + "'");
				}
				return val;
			}
			set {
				if (LangVersion == value) {
					return;
				}

				langVersion = LanguageVersionToString (value);
				NotifyChange ();
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
				if (definesymbols == (value ?? string.Empty))
					return;
				definesymbols = value ?? string.Empty;
				NotifyChange ();
			}
		}

		public bool Optimize {
			get {
				return optimize ?? false;
			}
			set {
				if (value == Optimize)
					return;
				optimize = value;
				NotifyChange ();
			}
		}

		public bool UnsafeCode {
			get {
				return unsafecode;
			}
			set {
				if (unsafecode == value)
					return;
				unsafecode = value;
				NotifyChange ();
			}
		}

		public bool GenerateOverflowChecks {
			get {
				return generateOverflowChecks;
			}
			set {
				if (generateOverflowChecks == value)
					return;
				generateOverflowChecks = value;
				NotifyChange ();
			}
		}

		public FilePath DocumentationFile {
			get {
				return documentationFile;
			}
			set {
				if (documentationFile == value)
					return;
				documentationFile = value;
				NotifyChange ();
			}
		}

		public string PlatformTarget {
			get {
				return platformTarget;
			}
			set {
				if (platformTarget == (value ?? string.Empty))
					return;
				platformTarget = value ?? string.Empty;
				NotifyChange ();
			}
		}

		#endregion

		#region Errors and Warnings
		public int WarningLevel {
			get {
				return warninglevel ?? 4;
			}
			set {
				int? newLevel = warninglevel ;
				if (warninglevel.HasValue) {
					newLevel = value;
				} else {
					if (value != 4)
						newLevel = value;
				}
				if (warninglevel == newLevel)
					return;
				warninglevel = newLevel;
				NotifyChange ();
			}
		}

		public string NoWarnings {
			get {
				return noWarnings;
			}
			set {
				if (noWarnings == value)
					return;
				noWarnings = value;
				NotifyChange ();
			}
		}

		public override bool NoStdLib {
			get {
				return noStdLib;
			}
			set {
				if (noStdLib == value)
					return;
				noStdLib = value;
				NotifyChange ();
			}
		}

		public bool TreatWarningsAsErrors {
			get {
				return treatWarningsAsErrors;
			}
			set {
				if (treatWarningsAsErrors == value)
					return;
				treatWarningsAsErrors = value;
				NotifyChange ();
			}
		}

		public string WarningsNotAsErrors {
			get {
				return warningsNotAsErrors;
			}
			set {
				if (warningsNotAsErrors == value)
					return;
				warningsNotAsErrors = value;
				NotifyChange ();
			}
		}
		#endregion

		internal static string LanguageVersionToString (LanguageVersion value)
		{
			switch (value) {
			case LanguageVersion.Default: return "Default";
			case LanguageVersion.Latest: return "Latest";
			case LanguageVersion.CSharp1: return "ISO-1";
			case LanguageVersion.CSharp2: return "ISO-2";
			case LanguageVersion.CSharp7_1: return "7.1";
			case LanguageVersion.CSharp7_2: return "7.2";
			case LanguageVersion.CSharp7_3: return "7.3";
			default: return ((int)value).ToString ();
			}
		}

		void NotifyChange ()
		{
			ParentProject?.NotifyModified ("CompilerParameters");
		}
	}
}
