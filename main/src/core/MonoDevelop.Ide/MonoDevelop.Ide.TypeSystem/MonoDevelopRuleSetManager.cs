//
// MonoDevelopRuleSetManager.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using System.Text.RegularExpressions;
using System.Collections.Immutable;
using YamlDotNet.Core.Tokens;

namespace MonoDevelop.Ide.TypeSystem
{
	class MonoDevelopRuleSetManager
	{
		public string GlobalRulesetFileName { get; }
		DateTime globalRuleSetWriteTimeUtc = DateTime.MinValue;
		RuleSet globalRuleSet;

		internal MonoDevelopRuleSetManager (string globalRulesetPath = null)
		{
			GlobalRulesetFileName = globalRulesetPath ?? UserProfile.Current.ConfigDir.Combine ("RuleSet.global");
		}

		static readonly Regex severityRegex = new Regex ("CodeIssues\\.System\\.String\\[\\]\\.(.*)\\.(.*)\\.severity");
		static readonly Regex enabledRegex  = new Regex ("CodeIssues\\.System\\.String\\[\\]\\.(.*)\\.(.*)\\.enabled");

		internal void EnsureGlobalRulesetExists()
		{
			if (File.Exists (GlobalRulesetFileName))
				return;

			var reportDiagnostics = new Dictionary<string, ReportDiagnostic> ();
			foreach (var key in PropertyService.GlobalInstance.Keys) {
				var match = severityRegex.Match (key);
				if (match.Success) {
					var severity = PropertyService.Get<DiagnosticSeverity> (key);
					var id = match.Groups [2].Value;
					if (!reportDiagnostics.ContainsKey (id))
						reportDiagnostics [id] = ConvertDiagnostic (severity);
					PropertyService.Set (key, null);
				}
				match = enabledRegex.Match (key);
				if (match.Success) {
					var isEnabled = PropertyService.Get<bool> (key);
					if (!isEnabled) {
						var id = match.Groups [2].Value;
						reportDiagnostics [id] = ReportDiagnostic.Suppress;
					}
					PropertyService.Set (key, null);
				}
			}

			WriteRulesetToFile (reportDiagnostics);
		}

		void WriteRulesetToFile (Dictionary<string, ReportDiagnostic> reportDiagnostics)
		{
			using (var sw = new StreamWriter (GlobalRulesetFileName)) {
				sw.WriteLine ("<RuleSet Name=\"Global Rules\" ToolsVersion=\"12.0\">");
				sw.WriteLine ("    <Rules AnalyzerId=\"Roslyn\" RuleNamespace=\"Roslyn\">>");
				foreach (var kv in reportDiagnostics) {
					if (kv.Key.StartsWith ("RE", StringComparison.Ordinal))
						continue;
					sw.WriteLine ("        <Rule Id=\"{0}\" Action=\"{1}\"/>", kv.Key, ConvertReportDiagnosticToAction (kv.Value));
				}
				sw.WriteLine ("    </Rules>");
				sw.WriteLine ("    <Rules AnalyzerId=\"RefactoringEssentials\" RuleNamespace=\"RefactoringEssentials\">>");
				foreach (var kv in reportDiagnostics) {
					if (!kv.Key.StartsWith ("RE", StringComparison.Ordinal))
						continue;
					sw.WriteLine ("        <Rule Id=\"{0}\" Action=\"{1}\"/>", kv.Key, ConvertReportDiagnosticToAction (kv.Value));
				}
				sw.WriteLine ("    </Rules>");
				sw.WriteLine ("</RuleSet>");
			}
		}


		static string ConvertReportDiagnosticToAction (ReportDiagnostic value)
		{
			switch (value)
			{
			case ReportDiagnostic.Default:
				return "Default";
			case ReportDiagnostic.Error:
				return "Error";
			case ReportDiagnostic.Warn:
				return "Warning";
			case ReportDiagnostic.Info:
				return "Info";
			case ReportDiagnostic.Hidden:
				return "Hidden";
			case ReportDiagnostic.Suppress:
				return "None";
			default:
				throw new InvalidOperationException("This program location is thought to be unreachable.");
			}
		}

		static ReportDiagnostic ConvertDiagnostic (DiagnosticSeverity severity)
		{
			switch (severity) {
			case DiagnosticSeverity.Hidden:
				return ReportDiagnostic.Hidden;
			case DiagnosticSeverity.Info:
				return ReportDiagnostic.Info;
			case DiagnosticSeverity.Warning:
				return ReportDiagnostic.Warn;
			case DiagnosticSeverity.Error:
				return ReportDiagnostic.Error;
			}
			return ReportDiagnostic.Default;
		}

		public RuleSet GetGlobalRuleSet ()
		{
			return QueryGlobalRuleset (retryOnError: true);

			RuleSet QueryGlobalRuleset (bool retryOnError)
			{
				try {
					var fileTime = File.GetLastWriteTimeUtc (GlobalRulesetFileName);
					if (globalRuleSet == null || fileTime != globalRuleSetWriteTimeUtc) {
						globalRuleSetWriteTimeUtc = fileTime;
						globalRuleSet = RuleSet.LoadEffectiveRuleSetFromFile (GlobalRulesetFileName);
					}
					return globalRuleSet;
				} catch (Exception e) {
					LoggingService.LogError ("Error while loading global rule set.", e);
					globalRuleSetWriteTimeUtc = DateTime.MinValue;

					// If the initial query fails, rename the current ruleset to a backup one, create the default value and try again.
					if (retryOnError) {
						FileService.RenameFile (GlobalRulesetFileName, Path.ChangeExtension (GlobalRulesetFileName, ".global.backup"));
						EnsureGlobalRulesetExists ();
						return QueryGlobalRuleset (retryOnError: false);
					}
					return null;
				}
			}
		}
	}
}
