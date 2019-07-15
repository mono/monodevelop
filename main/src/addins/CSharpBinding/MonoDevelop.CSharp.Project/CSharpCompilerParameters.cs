﻿//
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
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

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

		static MetadataReferenceResolver CreateMetadataReferenceResolver (IMetadataService metadataService, string projectDirectory, string outputDirectory)
		{
			ImmutableArray<string> assemblySearchPaths;
			if (projectDirectory != null && outputDirectory != null) {
				assemblySearchPaths = ImmutableArray.Create (projectDirectory, outputDirectory);
			} else if (projectDirectory != null) {
				assemblySearchPaths = ImmutableArray.Create (projectDirectory);
			} else if (outputDirectory != null) {
				assemblySearchPaths = ImmutableArray.Create (outputDirectory);
			} else {
				assemblySearchPaths = ImmutableArray<string>.Empty;
			}

			return new WorkspaceMetadataFileReferenceResolver (metadataService, new RelativePathResolver (assemblySearchPaths, baseDirectory: projectDirectory));
		}

		public override CompilationOptions CreateCompilationOptions ()
		{
			var project = (CSharpProject)ParentProject;
			var workspace = IdeApp.TypeSystemService.GetWorkspace (project.ParentSolution);
			var metadataReferenceResolver = CreateMetadataReferenceResolver (
					workspace.Services.GetService<IMetadataService> (),
					project.BaseDirectory,
					ParentConfiguration.OutputDirectory
			);

			bool isLibrary = ParentProject.IsLibraryBasedProjectType;
			string mainTypeName = project.MainClass;
			if (isLibrary || mainTypeName == string.Empty) {
				// empty string is not accepted by Roslyn
				mainTypeName = null;
			}

			var options = new CSharpCompilationOptions (
				isLibrary ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication,
				mainTypeName: mainTypeName,
				scriptClassName: "Script",
				optimizationLevel: Optimize ? OptimizationLevel.Release : OptimizationLevel.Debug,
				checkOverflow: GenerateOverflowChecks,
				allowUnsafe: UnsafeCode,
				cryptoKeyFile: ParentConfiguration.SignAssembly ? ParentConfiguration.AssemblyKeyFile : null,
				cryptoPublicKey: ImmutableArray<byte>.Empty,
				platform: GetPlatform (),
				publicSign: ParentConfiguration.PublicSign,
				delaySign: ParentConfiguration.DelaySign,
				generalDiagnosticOption: TreatWarningsAsErrors ? ReportDiagnostic.Error : ReportDiagnostic.Default,
				warningLevel: WarningLevel,
				specificDiagnosticOptions: GetSpecificDiagnosticOptions (),
				concurrentBuild: true,
				metadataReferenceResolver: metadataReferenceResolver,
				assemblyIdentityComparer: GetAssemblyIdentityComparer (ParentConfiguration, ParentProject?.MSBuildProject),
				strongNameProvider: new DesktopStrongNameProvider ()
			);

			return options;

			static DesktopAssemblyIdentityComparer GetAssemblyIdentityComparer (DotNetProjectConfiguration configuration, MSBuildProject project)
			{
				var appConfigFile = GetAppConfigPath (configuration, project);

				if (appConfigFile != null) {
					try {
						using var appConfigStream = new FileStream (appConfigFile, FileMode.Open, FileAccess.Read);
						return DesktopAssemblyIdentityComparer.LoadFromXml (appConfigStream);
					} catch (Exception ex) {
						LoggingService.LogError ($"Can't read app config file {appConfigFile}", ex);
					}
				}

				return DesktopAssemblyIdentityComparer.Default;
			}

			static string GetAppConfigPath (DotNetProjectConfiguration configuration, MSBuildProject project)
			{
				var appConfigFile = configuration.Properties.GetValue ("AppConfig")
					?? FindAppConfigFile (project, true)
					?? FindAppConfigFile (project, false);

				return appConfigFile != null
					? MSBuildProjectService.FromMSBuildPath (project.BaseDirectory, appConfigFile)
					: null;
			}

			static string FindAppConfigFile (MSBuildProject project, bool matchWholeItemSpec)
			{
				// Matches behaviour in MSBuild, bar prioritizing None over Content.
				// That complicates the code and makes it run all the matches in case the app config files is set to Content.
				// https://github.com/microsoft/msbuild/blob/6c53fccfab0f1a58e8d04f8c57dac058c798dcf7/src/Tasks/FindAppConfigFile.cs#L72

				if (project == null)
					return null;

				// Probe for item in None and Content
				const string appConfig = "app.config";
				var appConfigSpan = appConfig.AsSpan ();

				foreach (var item in project.EvaluatedItems) {
					if (item.Name != "Content" && item.Name != "None")
						continue;

					var include = item.Include;
					if (matchWholeItemSpec) {
						if (appConfig.Equals (include, StringComparison.OrdinalIgnoreCase))
							return include;
					} else {
						var fileNameStart = include.LastIndexOf (Path.DirectorySeparatorChar) + 1;
						if (include.AsSpan (fileNameStart).Equals (appConfigSpan, StringComparison.OrdinalIgnoreCase))
							return include;
					}
				}

				return null;
			}
		}

		Dictionary<string, ReportDiagnostic> GetSpecificDiagnosticOptions ()
		{
			var result = new Dictionary<string, ReportDiagnostic> ();
			foreach (var warning in GetSuppressedWarnings ())
				result [warning] = ReportDiagnostic.Suppress;

			var globalRuleSet = IdeApp.TypeSystemService.RuleSetManager.GetGlobalRuleSet ();
			if (globalRuleSet != null) {
				foreach (var kv in globalRuleSet.SpecificDiagnosticOptions) {
					result [kv.Key] = kv.Value;
				}
			}
			return result;
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
				if (int.TryParse (warning, out _))
					yield return "CS" + warning;
				else
					yield return warning;
			}
		}

		public override ParseOptions CreateParseOptions (DotNetProjectConfiguration configuration)
		{
			var symbols = GetDefineSymbols ();
			if (configuration != null)
				symbols = symbols.Concat (configuration.GetDefineSymbols ()).Distinct ();
			LanguageVersionFacts.TryParse (langVersion, out LanguageVersion lv);

			return new CSharpParseOptions (
				lv,
				DocumentationMode.Parse,
				SourceCodeKind.Regular,
				ImmutableArray<string>.Empty.AddRange (symbols)
			);
		}


		public LanguageVersion LangVersion {
			get {
				if (!LanguageVersionFacts.TryParse (langVersion, out LanguageVersion val)) {
					throw new Exception ("Unknown LangVersion string '" + langVersion + "'");
				}
				return val;
			}
			set {
				try {
					if (LangVersion == value) {
						return;
					}
				} catch (Exception) { }

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
			=> LanguageVersionFacts.ToDisplayString (value);

		void NotifyChange ()
		{
			ParentProject?.NotifyModified ("CompilerParameters");
		}
	}
}
