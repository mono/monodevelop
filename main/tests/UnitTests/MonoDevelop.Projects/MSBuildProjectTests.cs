//
// MSBuildProject.cs
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
using MonoDevelop.Projects.Formats.MSBuild;
using System.Linq;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildProjectTests: TestBase
	{
		MSBuildProject LoadProject ()
		{
			var prj = new MSBuildProject ();
			prj.Load (Util.GetSampleProject ("msbuild-project-test", "test.csproj"));
			return prj;
		}

		[Test]
		public void Properties ()
		{
			var p = LoadProject ();
			p.Evaluate ();

			Assert.AreEqual ("4.0", p.ToolsVersion);
			Assert.IsFalse (p.IsNewProject);

			var pg = p.GetGlobalPropertyGroup ();
			Assert.AreEqual ("8.0.50727", pg.GetValue ("ProductVersion"));
			Assert.AreEqual ("$(TestProp)", pg.GetValue ("EvalProp"));
		}

		[Test]
		public void EvaluatedProperties ()
		{
			var p = LoadProject ();
			p.Evaluate ();

			var pg = p.EvaluatedProperties;
			Assert.AreEqual ("8.0.50727", pg.GetValue ("ProductVersion"));
			Assert.AreEqual ("TestVal", pg.GetValue ("EvalProp"));
			Assert.AreEqual ("full", pg.GetValue ("DebugType"));
			Assert.AreEqual ("DEBUG;TRACE", pg.GetValue ("DefineConstants"));
			Assert.AreEqual ("Debug", pg.GetValue ("Configuration"));
			Assert.AreEqual ("AnyCPU", pg.GetValue ("Platform"));
			Assert.AreEqual ("ExtraVal", pg.GetValue ("ExtraProp"));
			Assert.AreEqual ("ExtraVal", pg.GetValue ("EvalExtraProp"));
		}

		[Test]
		public void Items ()
		{
			var p = LoadProject ();
			p.Evaluate ();

			var igs = p.ItemGroups.ToArray ();
			Assert.AreEqual (2, igs.Length);

			var ig = igs [0];
			Assert.AreEqual (2, ig.Items.Count());
			var ar = ig.Items.ToArray ();

			var it = ar [0];
			Assert.AreEqual ("Reference", it.Name);
			Assert.AreEqual ("System", it.Include);

			it = ar [1];
			Assert.AreEqual ("Foo", it.Name);
			Assert.AreEqual ("Foo.$(EvalProp)", it.Include);
			Assert.AreEqual ("$(Configuration)", it.Metadata.GetValue ("Meta1"));

			ig = igs [1];
			ar = ig.Items.ToArray ();
			Assert.AreEqual (2, ig.Items.Count());

			it = ar [0];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.Include);
		}

		[Test]
		public void EvaluatedItems ()
		{
			var p = LoadProject ();
			p.Evaluate ();

			var items2 = p.EvaluatedItems.ToArray ();
			var items = p.EvaluatedItems.Where (i => !i.IsImported).ToArray ();
			var it = items [0];
			Assert.AreEqual ("Reference", it.Name);
			Assert.AreEqual ("System", it.Include);

			it = items [1];
			Assert.AreEqual ("Foo", it.Name);
			Assert.AreEqual ("Foo.$(EvalProp)", it.UnevaluatedInclude);
			Assert.AreEqual ("Foo.TestVal", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta1"));

			it = items [2];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.UnevaluatedInclude);
			Assert.AreEqual ("file1.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray ()[1].Items.ToArray()[0]);

			it = items [3];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.UnevaluatedInclude);
			Assert.AreEqual ("file2.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray ()[1].Items.ToArray()[0]);

			it = items [4];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.UnevaluatedInclude);
			Assert.AreEqual ("file1.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray ()[1].Items.ToArray()[1]);

			it = items [5];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.UnevaluatedInclude);
			Assert.AreEqual ("file2.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray ()[1].Items.ToArray()[1]);
		}

		[Test]
		public void Targets ()
		{
			var p = LoadProject ();
			p.Evaluate ();
			var tn = p.EvaluatedTargets.Select (t => t.Name).ToArray ();

			// Verify that some of the imported targets are returned
			Assert.IsTrue (tn.Contains ("Build"));
			Assert.IsTrue (tn.Contains ("Clean"));
			Assert.IsTrue (tn.Contains ("ResolveReferences"));
			Assert.IsTrue (tn.Contains ("GetReferenceAssemblyPaths"));
		}

		[Test]
		public void ImportGroups ()
		{
			string projectFile = Util.GetSampleProject ("project-with-import-groups", "import-group-test.csproj");
			var p = new MSBuildProject ();
			p.Load (projectFile);
			p.Evaluate ();

			Assert.AreEqual ("v2", p.EvaluatedProperties.GetValue ("TestProp"));
			Assert.AreEqual ("one", p.EvaluatedProperties.GetValue ("PropFromTest1"));
			Assert.AreEqual ("two", p.EvaluatedProperties.GetValue ("PropFromTest2"));
			Assert.AreEqual ("three", p.EvaluatedProperties.GetValue ("PropFromFoo"));
		}

		[Test]
		public void ChooseElement ()
		{
			string projectFile = Util.GetSampleProject ("project-with-choose-element", "project.csproj");
			var p = new MSBuildProject ();
			p.Load (projectFile);
			p.Evaluate ();
			Assert.AreEqual ("One", p.EvaluatedProperties.GetValue ("Foo"));

			var pi = p.CreateInstance ();
			pi.SetGlobalProperty ("Configuration", "Release");
			pi.Evaluate ();
			Assert.AreEqual ("Two", pi.EvaluatedProperties.GetValue ("Foo"));

			pi.SetGlobalProperty ("Configuration", "Alt");
			pi.Evaluate ();
			Assert.AreEqual ("Three", pi.EvaluatedProperties.GetValue ("Foo"));
		}

		[Test]
		public void ParseConditionWithoutQuotes ()
		{
			string projectFile = Util.GetSampleProject ("msbuild-tests", "condition-parse.csproj");
			var p = new MSBuildProject ();
			p.Load (projectFile);
			p.Evaluate ();
			Assert.AreEqual (new [] {"aa","vv","test"}, p.EvaluatedItems.Select (i => i.Include).ToArray ());
		}

		[Test]
		public void EvalItemsAfterProperties ()
		{
			string projectFile = Util.GetSampleProject ("msbuild-tests", "property-eval-order.csproj");
			var p = new MSBuildProject ();
			p.Load (projectFile);
			p.Evaluate ();
			Assert.AreEqual (new [] {"Two"}, p.EvaluatedItems.Select (i => i.Include).ToArray ());
		}
	}
}

