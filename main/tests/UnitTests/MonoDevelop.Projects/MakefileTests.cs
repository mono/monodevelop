// MakefileTests.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using Mono.Addins;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture()]
	public class MakefileTests: TestBase
	{
		bool disableWhenDone;

		[TestFixtureSetUp]
		public void SetUp ()
		{
			var ad = AddinManager.Registry.GetAddin ("MonoDevelop.Autotools");
			disableWhenDone = ad != null && ad.Enabled;
			if (ad != null)
				ad.Enabled = true;
		}

		[TestFixtureTearDown]
		public void Teardown ()
		{
			if (disableWhenDone) {
				var ad = AddinManager.Registry.GetAddin ("MonoDevelop.Autotools");
				ad.Enabled = false;
			}
		}

		[Test()]
		[Platform (Exclude = "Win")]
		public async Task MakefileSynchronization ()
		{
			if (Platform.IsWindows)
				Assert.Ignore ();

			string solFile = Util.GetSampleProject ("console-project-with-makefile", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			
			DotNetProject p = (DotNetProject) sol.Items [0];
			
			Assert.AreEqual (2, p.Files.Count);
			string f = Path.Combine (p.BaseDirectory, "Program.cs");
			Assert.IsTrue (p.Files.GetFile (f) != null, "Contains Program.cs");
			f = Path.Combine (p.BaseDirectory, "Properties");
			f = Path.Combine (f, "AssemblyInfo.cs");
			Assert.IsTrue (p.Files.GetFile (f) != null, "Contains Properties/AssemblyInfo.cs");
			
			List<string> refs = new List<string> ();
			refs.Add ("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			refs.Add ("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			refs.Add ("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Assert.AreEqual (3, p.References.Count);
			
			ProjectReference xmlRef = null;
			foreach (ProjectReference pref in p.References) {
				Assert.IsTrue (refs.Contains (pref.Reference), "Contains reference " + pref.Reference);
				refs.Remove (pref.Reference);
				if (pref.Reference.StartsWith ("System.Xml"))
					xmlRef = pref;
			}
			
			// Test saving
			
			p.References.Remove (xmlRef);
			p.References.Add (ProjectReference.CreateAssemblyReference ("System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
			
			p.Files.Remove (f);
			p.Files.Add (new ProjectFile (Path.Combine (p.BaseDirectory, "Class1.cs"), BuildAction.Compile));
			
			await sol.SaveAsync (Util.GetMonitor ());
			
			string makefile = File.ReadAllText (Path.Combine (p.BaseDirectory, "Makefile"));
			string[] values = GetVariable (makefile, "FILES").Split (' ');
			Assert.AreEqual (2, values.Length);
			Assert.AreEqual ("Class1.cs", values [0]);
			Assert.AreEqual ("Program.cs", values [1]);

			values = GetVariable (makefile, "REFERENCES").Split (' ');
			Assert.AreEqual (3, values.Length);
			Assert.AreEqual ("System", values [0]);
			Assert.AreEqual ("System.Data", values [1]);
			Assert.AreEqual ("System.Web", values [2]);

			sol.Dispose ();
		}
		
		string GetVariable (string content, string var)
		{
			string multilineMatch = @"(((?<content>.*)(?<!\\)\n)|((?<content>.*?)\\\n(\t(?<content>.*?)\\\n)*\t(?<content>.*?)(?<!\\)\n))";
			Regex exp = new Regex(@"[.|\n]*^" + var + @"(?<sep>\s*:?=\s*)" + multilineMatch, RegexOptions.Multiline);
			
			Match match = exp.Match (content);
			if (!match.Success) return "";
			string value = "";
			foreach (Capture c in match.Groups["content"].Captures)
				value += c.Value;
			return value.Trim (' ', '\t');
		}
	}
}
