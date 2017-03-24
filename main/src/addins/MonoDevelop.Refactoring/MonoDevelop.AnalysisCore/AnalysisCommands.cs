// 
// AnalysisCommands.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Linq;
using Microsoft.CodeAnalysis;
using MonoDevelop.CodeActions;
using MonoDevelop.CodeIssues;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Dialogs;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MonoDevelop.AnalysisCore
{
	public enum AnalysisCommands
	{
		ExportRules
	}

	class ExportRulesHandler : CommandHandler
	{
		protected override void Run ()
		{
			var lang = "text/x-csharp";

			OpenFileDialog dlg = new OpenFileDialog ("Export Rules", MonoDevelop.Components.FileChooserAction.Save);
			dlg.InitialFileName = "rules.html";
			if (!dlg.Run ())
				return;

			Dictionary<CodeDiagnosticDescriptor, DiagnosticSeverity?> severities = new Dictionary<CodeDiagnosticDescriptor, DiagnosticSeverity?> ();

			foreach (var node in BuiltInCodeDiagnosticProvider.GetBuiltInCodeDiagnosticDescriptorsAsync (CodeRefactoringService.MimeTypeToLanguage(lang), true).Result) {
				severities [node] = node.DiagnosticSeverity;
//				if (node.GetProvider ().SupportedDiagnostics.Length > 1) {
//					foreach (var subIssue in node.GetProvider ().SupportedDiagnostics) {
//						severities [subIssue] = node.GetSeverity (subIssue);
//					}
//				}
			}

			var grouped = severities.Keys.OfType<CodeDiagnosticDescriptor> ()
				.GroupBy (node => node.GetProvider ().SupportedDiagnostics.First ().Category)
				.OrderBy (g => g.Key, StringComparer.Ordinal);

			using (var sw = new StreamWriter (dlg.SelectedFile)) {
				sw.WriteLine ("<h1>Code Rules</h1>");
				foreach (var g in grouped) {
					sw.WriteLine ("<h2>" + g.Key + "</h2>");
					sw.WriteLine ("<table border='1'>");

					foreach (var node in g.Select (n => new { Descriptor = n, Provider = n.GetProvider ()}).OrderBy (n => n.Provider.GetAnalyzerId(), StringComparer.Ordinal)) {
						var title = node.Provider.GetAnalyzerId ();
						var desc = node.Provider.SupportedDiagnostics.First ().Description.ToString () != title ? node.Provider.SupportedDiagnostics.First ().Description : "";
						sw.WriteLine ("<tr><td>" + title + "</td><td>" + desc + "</td><td>" + node.Descriptor.DiagnosticSeverity + "</td></tr>");
						if (node.Provider.SupportedDiagnostics.Length > 1) {
							foreach (var subIssue in node.Provider.SupportedDiagnostics) {
								title = subIssue.Description.ToString ();
								desc = subIssue.Description.ToString () != title ? subIssue.Description : "";
								sw.WriteLine ("<tr><td> - " + title + "</td><td>" + desc + "</td><td>" + node.Descriptor.GetSeverity (subIssue) + "</td></tr>");
							}
						}
					}
					sw.WriteLine ("</table>");
				}

				var providerStates = new Dictionary<CodeRefactoringDescriptor, bool> ();
				foreach (var node in BuiltInCodeDiagnosticProvider.GetBuiltInCodeRefactoringDescriptorsAsync (CodeRefactoringService.MimeTypeToLanguage(lang), true).Result) {
					providerStates [node] = node.IsEnabled;
				}

				sw.WriteLine ("<h1>Code Actions</h1>");
				sw.WriteLine ("<table border='1'>");
				var sortedAndFiltered = providerStates.Keys.OrderBy (n => n.Name, StringComparer.Ordinal);
				foreach (var node in sortedAndFiltered) {
					sw.WriteLine ("<tr><td>" + node.IdString + "</td><td>" + node.Name + "</td></tr>");
				}
				sw.WriteLine ("</table>");
			}
		}
	}
}

