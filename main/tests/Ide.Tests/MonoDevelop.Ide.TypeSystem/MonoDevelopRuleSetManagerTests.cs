//
// MonoDevelopRuleSetManager.cs
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
using System;
using System.IO;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class MonoDevelopRuleSetManagerTests
	{
		[Test]
		public void TestLoadingDefault ()
		{
			var path = Util.CreateTmpDir ("ruleset");
			var rulesetPath = Path.Combine (path, "a.ruleset");

			// None exists, so it will be re-created
			var rulesetManager = new MonoDevelopRuleSetManager (rulesetPath);

			var ruleset = rulesetManager.GetGlobalRuleSet ();

			Assert.IsNotNull (ruleset);
		}

		[Test]
		public void TestLoadingTimestamp ()
		{
			var path = Util.CreateTmpDir ("ruleset");
			var rulesetPath = Path.Combine (path, "a.ruleset");

			// None exists, so it will be re-created
			var rulesetManager = new MonoDevelopRuleSetManager (rulesetPath);

			var ruleset = rulesetManager.GetGlobalRuleSet ();

			Assert.IsNotNull (ruleset);

			var ruleset2 = rulesetManager.GetGlobalRuleSet ();
			Assert.AreSame (ruleset, ruleset2);
			
			var lastWrite = File.GetLastWriteTimeUtc (rulesetPath);
			File.SetLastWriteTimeUtc (rulesetPath, lastWrite.AddHours (-1));
			Assert.AreNotSame (ruleset, rulesetManager.GetGlobalRuleSet ());
		}

		[Test]
		public void TestLoadingExisting ()
		{
			var path = Util.CreateTmpDir ("ruleset");
			var rulesetPath = Path.Combine (path, "a.ruleset");

			// None exists, so it will be re-created
			var rulesetManager = new MonoDevelopRuleSetManager (rulesetPath);

			var ruleset = rulesetManager.GetGlobalRuleSet ();
			Assert.IsNotNull (ruleset);

			rulesetManager = new MonoDevelopRuleSetManager (rulesetPath);
			var ruleset2 = rulesetManager.GetGlobalRuleSet ();

			// A new manager will query again, so we can assume no cache is being used.
			Assert.IsNotNull (ruleset2);
			Assert.AreNotSame (ruleset, ruleset2);
		}

		[Test]
		public void TestLoadingBrokenRuleset ()
		{
			var path = Util.CreateTmpDir ("ruleset");
			var rulesetPath = Path.Combine (path, "a.ruleset");

			// force ruleset file creation
			var rulesetManager = new MonoDevelopRuleSetManager (rulesetPath);
			var brokenText = @"<RuleSet Name=""Global Rules"" ToolsVersion=""12.0"">
    <Rules AnalyzerId=""Roslyn"" RuleNamespace=""Roslyn"">>
	    <Rule Id=""RECS0076"" Action=""Suppress""/>
    </Rules>
</RuleSet>
";
			File.WriteAllText (rulesetPath, brokenText);

			var ruleset = rulesetManager.GetGlobalRuleSet ();

			Assert.IsNotNull (ruleset);

			var backupPath = rulesetPath + ".backup";
			Assert.IsTrue (File.Exists (backupPath));
		}
	}
}
