//
// FullSolutionAnalysisTests.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.TypeSystem;
using NUnit.Framework;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	[TestFixture]
	public class FullSolutionAnalysisTests : IdeTestBase
	{
		[TestCase (LanguageNames.CSharp, false)]
		[TestCase (LanguageNames.VisualBasic, true)]
		public void TestFullSolutionAnalysis (string language, bool enabledByDefault)
		{
			var pref = IdeApp.Preferences.Roslyn;
			var perLanguagePref = pref.For (language);

			using (var mdw = new MonoDevelopWorkspace (null)) {
				var key = new OptionKey (ServiceFeatureOnOffOptions.ClosedFileDiagnostic, language);

				var old = mdw.Options.GetOption (key);
				try {
					// Set the closed file diagnostics to default.
					mdw.Options = mdw.Options.WithChangedOption (key, null);

					Assert.IsTrue (pref.FullSolutionAnalysisRuntimeEnabled);
					Assert.IsTrue (mdw.Options.GetOption (RuntimeOptions.FullSolutionAnalysis));
					Assert.AreEqual (enabledByDefault, perLanguagePref.SolutionCrawlerClosedFileDiagnostic.Value);

					// Set closed file diagnostics to false, this should not impact runtime options
					mdw.Options = mdw.Options.WithChangedOption (key, false);

					Assert.IsTrue (pref.FullSolutionAnalysisRuntimeEnabled);
					Assert.IsTrue (mdw.Options.GetOption (RuntimeOptions.FullSolutionAnalysis));
					Assert.IsFalse (perLanguagePref.SolutionCrawlerClosedFileDiagnostic.Value);

					// Set closed file diagnostics to true, this should turn on everything
					// Ensure FSA is off at this point.
					mdw.Options = mdw.Options.WithChangedOption (RuntimeOptions.FullSolutionAnalysis, false);
					pref.FullSolutionAnalysisRuntimeEnabled = false;
					mdw.Options = mdw.Options.WithChangedOption (key, true);

					Assert.IsTrue (pref.FullSolutionAnalysisRuntimeEnabled);
					Assert.IsTrue (mdw.Options.GetOption (RuntimeOptions.FullSolutionAnalysis));
					Assert.IsTrue (perLanguagePref.SolutionCrawlerClosedFileDiagnostic.Value);
				} finally {
					mdw.Options = mdw.Options.WithChangedOption (key, old);
					pref.FullSolutionAnalysisRuntimeEnabled = true;
					mdw.Options = mdw.Options.WithChangedOption (RuntimeOptions.FullSolutionAnalysis, true);
				}
			}
		}
	}
}
