//
// PolicyTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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

using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Projects.Policies;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Projects;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class PolicyTests: TestBase
	{
		[Test]
		public async Task CSharpFormattingPolicyIndentSwitchCaseSectionChangedMultipleTimes ()
		{
			string dir = Util.CreateTmpDir ("IndentSwitchCaseSectionChangedMultipleTimes");
			var pset = PolicyService.GetPolicySet ("Mono");
			var monoFormattingPolicy = pset.Get<CSharpFormattingPolicy> ("text/x-csharp");
			var formattingPolicy = monoFormattingPolicy.Clone ();
			var solution = new Solution ();
			solution.Policies.Set (formattingPolicy);

			bool expectedSetting = !formattingPolicy.IndentSwitchCaseSection;
			string fileName = Path.Combine (dir, "IndentSwitchCaseSectionChangedMultipleTimes.sln");

			for (int i = 0; i < 3; ++i) {
				formattingPolicy.IndentSwitchCaseSection = expectedSetting;

				await solution.SaveAsync (fileName, Util.GetMonitor ());

				var savedSolution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), fileName);
				var savedFormattingPolicy = savedSolution.Policies.Get<CSharpFormattingPolicy> ();
				Assert.AreEqual (expectedSetting, savedFormattingPolicy.IndentSwitchCaseSection);

				expectedSetting = !expectedSetting;
			}

			solution.Dispose ();
		}

		/// <summary>
		/// Test to make sure that all the formatting policy items are not written to the 
		/// solution only those which differ from the inherited formatting policy should be
		/// saved in the solution file.
		/// </summary>
		[Test]
		public async Task SaveSolutionAfterChangingCSharpFormattingPolicyForTheFirstTime ()
		{
			string dir = Util.CreateTmpDir ("FormattingPolicyChangedOnce");
			var formattingPolicy = new CSharpFormattingPolicy ();
			var solution = new Solution ();
			solution.Policies.Set (formattingPolicy);

			bool expectedSetting = !formattingPolicy.IndentSwitchCaseSection;
			formattingPolicy.IndentSwitchCaseSection = expectedSetting;
			string fileName = Path.Combine (dir, "FormattingPolicyChangedOnce.sln");

			await solution.SaveAsync (fileName, Util.GetMonitor ());

			var file = new SlnFile ();
			file.Read (fileName);
			var s = file.Sections.GetSection ("MonoDevelopProperties", SlnSectionType.PreProcess);
			var missingItem = default(KeyValuePair<string, string>);
			Assert.AreEqual (expectedSetting.ToString (), s.Properties.SingleOrDefault (p => p.Key.Contains ("IndentSwitchCaseSection")).Value);
			Assert.AreEqual (missingItem, s.Properties.SingleOrDefault (p => p.Key.Contains ("IndentSwitchSection")));
			Assert.AreEqual (missingItem, s.Properties.SingleOrDefault (p => p.Key.Contains ("IndentBlock")));
			Assert.AreEqual (missingItem, s.Properties.SingleOrDefault (p => p.Key.Contains ("SpaceBeforeDot")));
			Assert.AreEqual (missingItem, s.Properties.SingleOrDefault (p => p.Key.Contains ("NewLineForElse")));

			solution.Dispose ();
		}
	}
}

