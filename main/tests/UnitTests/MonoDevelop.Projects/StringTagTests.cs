//
// StringTagTests.cs
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
using System;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core.StringParsing;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class StringTagTests: TestBase
	{
		[Test]
		public async Task ProjectTags ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			sol.LocalAuthorInformation = new AuthorInformation ("test name", "test email", "test copy", "test company", "test trademark");

			var model = p.GetStringTagModel (ConfigurationSelector.Default);

			Assert.AreEqual ("ConsoleProject", model.GetValue ("ProjectName"));
			Assert.AreEqual ("test copy", model.GetValue ("AUTHORCOPYRIGHT"));
			Assert.AreEqual ("test company", model.GetValue ("AUTHORCOMPANY"));
			Assert.AreEqual ("test trademark", model.GetValue ("AUTHORTRADEMARK"));
			Assert.AreEqual ("test email", model.GetValue ("AUTHOREMAIL"));
			Assert.AreEqual ("test name", model.GetValue ("AUTHORNAME"));
			Assert.AreEqual (p.BaseDirectory, model.GetValue ("PROJECTDIR"));
			Assert.AreEqual (p.FileName, model.GetValue ("PROJECTFILE"));

			Assert.AreEqual (sol.FileName, model.GetValue ("SolutionFile"));
			Assert.AreEqual ("ConsoleProject", model.GetValue ("SolutionName"));
			Assert.AreEqual (sol.ItemDirectory, model.GetValue ("SolutionDir"));
			Assert.AreEqual ("Debug", model.GetValue ("ProjectConfig"));
			Assert.AreEqual ("Debug", model.GetValue ("ProjectConfigName"));
			Assert.AreEqual ("", model.GetValue ("ProjectConfigPlat"));
			Assert.AreEqual (p.GetOutputFileName (ConfigurationSelector.Default), model.GetValue ("TargetFile"));
			Assert.AreEqual (p.GetOutputFileName (ConfigurationSelector.Default), model.GetValue ("TargetPath"));
			Assert.AreEqual (p.GetOutputFileName (ConfigurationSelector.Default).FileName, model.GetValue ("TargetName"));
			Assert.AreEqual (p.GetOutputFileName (ConfigurationSelector.Default).ParentDirectory, model.GetValue ("TargetDir"));
			Assert.AreEqual (".exe", model.GetValue ("TargetExt"));

			var mdesc = p.GetStringTagModelDescription (ConfigurationSelector.Default);
			var tt = mdesc.GetTags ().Select (t => t.Name).ToArray ();

			Assert.That (tt.Contains ("ProjectName"));
			Assert.That (tt.Contains ("AuthorCopyright"));
			Assert.That (tt.Contains ("AuthorCompany"));
			Assert.That (tt.Contains ("AuthorTrademark"));
			Assert.That (tt.Contains ("AuthorEmail"));
			Assert.That (tt.Contains ("AuthorName"));
			Assert.That (tt.Contains ("ProjectDir"));
			Assert.That (tt.Contains ("SolutionFile"));
			Assert.That (tt.Contains ("SolutionName"));
			Assert.That (tt.Contains ("SolutionDir"));
			Assert.That (tt.Contains ("ProjectConfig"));
			Assert.That (tt.Contains ("ProjectConfigName"));
			Assert.That (tt.Contains ("ProjectConfigPlat"));
			Assert.That (tt.Contains ("TargetFile"));
			Assert.That (tt.Contains ("TargetPath"));
			Assert.That (tt.Contains ("TargetName"));
			Assert.That (tt.Contains ("TargetDir"));

			sol.Dispose ();
		}

		[Test]
		public async Task SolutionTags ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			sol.LocalAuthorInformation = new AuthorInformation ("test name", "test email", "test copy", "test company", "test trademark");

			var model = sol.GetStringTagModel (ConfigurationSelector.Default);

			Assert.AreEqual (sol.FileName, model.GetValue ("SolutionFile"));
			Assert.AreEqual ("ConsoleProject", model.GetValue ("SolutionName"));
			Assert.AreEqual (sol.ItemDirectory, model.GetValue ("SolutionDir"));

			var mdesc = sol.GetStringTagModelDescription (ConfigurationSelector.Default);
			var tt = mdesc.GetTags ().Select (t => t.Name).ToArray ();

			Assert.That (tt.Contains ("SolutionFile"));
			Assert.That (tt.Contains ("SolutionName"));
			Assert.That (tt.Contains ("SolutionDir"));

			sol.Dispose ();
		}

		[Test]
		public void TagsInItemExtension ()
		{
			var p = new TestTagProvider ();
			StringParserService.RegisterStringTagProvider (p);

			var node = new CustomItemNode<StringTagTestExtension> ();
			WorkspaceObject.RegisterCustomExtension (node);

			try {
				var project = Services.ProjectService.CreateDotNetProject ("C#");

				var modeld = project.GetStringTagModelDescription (ConfigurationSelector.Default);
				Assert.IsTrue (modeld.GetTags ().Any (t => t.Name == "foo"));

				var model = project.GetStringTagModel (ConfigurationSelector.Default);
				Assert.AreEqual ("bar", model.GetValue ("foo"));

				project.Dispose ();
			}
			finally {
				StringParserService.UnregisterStringTagProvider (p);
				WorkspaceObject.UnregisterCustomExtension (node);
			}
		}
	}

	class StringTagTestExtension: SolutionItemExtension
	{
	}

	class TestTagProvider: StringTagProvider<StringTagTestExtension>
	{
		#region implemented abstract members of StringTagProvider

		public override object GetTagValue (StringTagTestExtension instance, string tag)
		{
			if (tag == "FOO")
				return "bar";
			return null;
		}

		public override IEnumerable<StringTagDescription> GetTags ()
		{
			yield return new StringTagDescription ("foo", "desc");
		}

		#endregion
	}
}

