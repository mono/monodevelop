//
// CSharpLocalizationConcatenationDiagnosticAnalyzerTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring.Tests;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding.Refactoring
{
	[TestFixture]
	public class CSharpLocalizationConcatenationDiagnosticAnalyzerTests : ResultsEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

		const string OneFromEach = @"using System;

namespace MonoDevelop.Core
{
	static class GettextCatalog
	{
		public static string GetString(string phrase) => phrase;
		public static string GetString(string phrase, object arg1) => string.Format(phrase, arg1);
	}
}

namespace Mono.Addins.Localization
{
	public interface IAddinLocalizer { string GetString(string phrase); }
}

namespace testfsw
{
	using MonoDevelop.Core;
	class AddinLocalizer : Mono.Addins.Localization.IAddinLocalizer
	{
		public string GetString(string phrase) => phrase;
	}

	class MainClass
	{
		static void Main()
		{
			var localizer = new AddinLocalizer();
			string a = ""test"";

			// Fine
			GettextCatalog.GetString(""asdf"");
			GettextCatalog.GetString(@""bsdf"");
			GettextCatalog.GetString(@""csdf"" + ""csdf"");
			GettextCatalog.GetString(""dsdf"" + ""dsdf"");
			GettextCatalog.GetString(""{0} stuff"", ""thing"");
			localizer.GetString(""a"");
			GettextCatalog.GetString(""{0} stuff"" + ""a"", ""thing"");

			// Shows errors.
			GettextCatalog.GetString(a);
			GettextCatalog.GetString($""dsdf"");
			GettextCatalog.GetString($""{a}"");
			localizer.GetString(a);
			GettextCatalog.GetString(""asdf"" + ""asdf"" + a);
		}
	}
}
";
		const string message = "Only literal strings can be passed to GetString for the crawler to work";
		static readonly ExpectedDiagnostic [] LocalizationDiagnostics = {
			new ExpectedDiagnostic (988, DiagnosticSeverity.Error,  message),
			new ExpectedDiagnostic (1020, DiagnosticSeverity.Error, message),
			new ExpectedDiagnostic (1058, DiagnosticSeverity.Error, message),
			new ExpectedDiagnostic (1122, DiagnosticSeverity.Error, message),
			new ExpectedDiagnostic (1090, DiagnosticSeverity.Error, message),
		};

		[Test]
		public async Task DiagnosticsAreReported ()
		{
			await RunTest (5, OneFromEach, (remainingUpdates, doc) => {
				if (remainingUpdates == 0) {
					AssertExpectedDiagnostics (LocalizationDiagnostics, doc, x => x.Severity == DiagnosticSeverity.Error);
				}
				return Task.CompletedTask;
			});
		}
	}
}
