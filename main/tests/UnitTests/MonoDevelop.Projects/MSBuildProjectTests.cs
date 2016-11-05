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
using MonoDevelop.Projects.MSBuild;
using System.Linq;
using ValueSet = MonoDevelop.Projects.ConditionedPropertyCollection.ValueSet;

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

		static MSBuildProject LoadAndEvaluate (string dir, string testFile)
		{
			string projectFile = Util.GetSampleProject (dir, testFile);
			var p = new MSBuildProject ();
			p.Load (projectFile);
			p.Evaluate ();
			return p;
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
			Assert.AreEqual ("value2", pg.GetValue ("Case2"));
			Assert.AreEqual ("value2", pg.GetValue ("Case3"));
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
			Assert.AreEqual (3, ig.Items.Count());

			it = ar [0];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.Include);

			it = ar [1];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.Include);

			it = ar [2];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.Include);
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

			it = items [6];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.UnevaluatedInclude);
			Assert.AreEqual ("file1.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta3"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [2]);

			it = items [7];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.UnevaluatedInclude);
			Assert.AreEqual ("file2.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta3"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [2]);

			it = items [8];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.UnevaluatedInclude);
			Assert.AreEqual ("file1.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta3"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [2]);

			it = items [9];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.UnevaluatedInclude);
			Assert.AreEqual ("file2.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta3"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [2]);
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
			Assert.IsFalse (tn.Contains ("Conditioned"));
		}

		[Test]
		public void TargetsIgnoringCondition ()
		{
			var p = LoadProject ();
			p.Evaluate ();
			var tn = p.EvaluatedTargetsIgnoringCondition.Select (t => t.Name).ToArray ();

			// Verify that some of the imported targets are returned
			Assert.IsTrue (tn.Contains ("Build"));
			Assert.IsTrue (tn.Contains ("Clean"));
			Assert.IsTrue (tn.Contains ("ResolveReferences"));
			Assert.IsTrue (tn.Contains ("GetReferenceAssemblyPaths"));
			Assert.IsTrue (tn.Contains ("Conditioned"));
		}

		[Test]
		public void EvalExists ()
		{
			var p = LoadProject ();
			p.Evaluate ();
			var res = p.EvaluatedProperties.GetValue ("ExistsTest");
			Assert.AreEqual ("OK", res);
		}

		[Test]
		public void ImportGroups ()
		{
			var p = LoadAndEvaluate ("project-with-import-groups", "import-group-test.csproj");

			Assert.AreEqual ("v2", p.EvaluatedProperties.GetValue ("TestProp"));
			Assert.AreEqual ("one", p.EvaluatedProperties.GetValue ("PropFromTest1"));
			Assert.AreEqual ("two", p.EvaluatedProperties.GetValue ("PropFromTest2"));
			Assert.AreEqual ("three", p.EvaluatedProperties.GetValue ("PropFromFoo"));
		}

		[Test]
		public void ChooseElement ()
		{
			var p = LoadAndEvaluate ("project-with-choose-element", "project.csproj");

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
			var p = LoadAndEvaluate ("msbuild-tests", "condition-parse.csproj");
			Assert.AreEqual (new [] {"aa","vv","test"}, p.EvaluatedItems.Select (i => i.Include).ToArray ());
		}

		[Test]
		public void EvalItemsAfterProperties ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "property-eval-order.csproj");
			Assert.AreEqual (new [] {"Two"}, p.EvaluatedItems.Select (i => i.Include).ToArray ());
		}

		[Test]
		public void FunctionProperties ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "functions.csproj");

			Assert.AreEqual ("bcd", p.EvaluatedProperties.GetValue ("Substring"));
			Assert.AreEqual ("ab", p.EvaluatedProperties.GetValue ("MethodWithParams1"));
			Assert.AreEqual ("abc", p.EvaluatedProperties.GetValue ("MethodWithParams2"));
			Assert.AreEqual ("abcd", p.EvaluatedProperties.GetValue ("MethodWithParams3"));
			Assert.AreEqual ("abcdefghij", p.EvaluatedProperties.GetValue ("MethodWithParams4"));
			Assert.AreEqual ("ab", p.EvaluatedProperties.GetValue ("MethodWithParams5"));
			Assert.AreEqual ("255", p.EvaluatedProperties.GetValue ("MaxByte"));
			Assert.AreEqual ("A", p.EvaluatedProperties.GetValue ("Upper1"));
			Assert.AreEqual ("a'b'c5", p.EvaluatedProperties.GetValue ("Upper2"));
			Assert.AreEqual ("a\"b\"c5", p.EvaluatedProperties.GetValue ("Upper3"));
			Assert.AreEqual ("abc5", p.EvaluatedProperties.GetValue ("Upper4"));
			Assert.AreEqual ("abcdefgh5", p.EvaluatedProperties.GetValue ("Upper5"));
			Assert.AreEqual ("1234567890", p.EvaluatedProperties.GetValue ("FileContent"));
			Assert.AreEqual ("00007fff", p.EvaluatedProperties.GetValue ("HexConv"));
			Assert.AreEqual ("[1234567890]", p.EvaluatedProperties.GetValue ("ConcatFileContent"));

			Assert.AreEqual ("5", p.EvaluatedProperties.GetValue ("MSBuildAdd"));
			Assert.AreEqual ("5.5", p.EvaluatedProperties.GetValue ("MSBuildAddDouble"));
			Assert.AreEqual ("abcdefgh", p.EvaluatedProperties.GetValue ("MSBuildValueOrDefault1"));
			Assert.AreEqual ("empty", p.EvaluatedProperties.GetValue ("MSBuildValueOrDefault2"));
			Assert.AreEqual ("a", p.EvaluatedProperties.GetValue ("CharTrim"));
			Assert.AreEqual ("2", p.EvaluatedProperties.GetValue ("SplitLength"));
			Assert.AreEqual ("abcdefg", p.EvaluatedProperties.GetValue ("NewString"));
			Assert.AreEqual ("100", p.EvaluatedProperties.GetValue ("CharConvert"));

			var dir = System.IO.Path.GetFullPath (System.IO.Path.Combine (System.IO.Path.GetDirectoryName (p.FileName), "foo"));
			Assert.AreEqual (dir, p.EvaluatedProperties.GetValue ("FullPath"));
		}

		[Test]
		public void ConditionedProperties ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "conditioned-properties.csproj");

			Assert.That (new string [] { "cond1", "cond2", "cond9", "cond10", "cond13" }, Is.EquivalentTo (p.ConditionedProperties.GetAllProperties ().ToArray ()));

			Assert.That (new string [] { "val1", "val14_1", "val14_4", "val14_5" }, Is.EquivalentTo (p.ConditionedProperties.GetAllPropertyValues ("cond1").ToArray ()));

			Assert.That (new string [] { "val2_0", "val2_7", "val14_2", "val14_3", "val14_6" }, Is.EquivalentTo (p.ConditionedProperties.GetAllPropertyValues ("cond2").ToArray ()));

			Assert.That (new string [] { "val9" }, Is.EquivalentTo (p.ConditionedProperties.GetAllPropertyValues ("cond9").ToArray ()));

			Assert.That (new string [] { "val10_1", "val10_2" }, Is.EquivalentTo (p.ConditionedProperties.GetAllPropertyValues ("cond10").ToArray ()));

			Assert.That (new string [] { "val13_4" }, Is.EquivalentTo (p.ConditionedProperties.GetAllPropertyValues ("cond13").ToArray ()));

			// Combined values

			Assert.That (new [] {
				new ValueSet (new [] { "cond1" }, new [] { "val1" })
			}, Is.EquivalentTo (p.ConditionedProperties.GetCombinedPropertyValues ("cond1").ToArray ()));

			Assert.That (new [] {
				new ValueSet (new [] { "cond2" }, new [] { "val2_0" }),
				new ValueSet (new [] { "cond2" }, new [] { "val2_7" }),
			}, Is.EquivalentTo (p.ConditionedProperties.GetCombinedPropertyValues ("cond2").ToArray ()));

			Assert.That (new [] {
				new ValueSet (new [] { "cond9" }, new [] { "val9" }),
			}, Is.EquivalentTo (p.ConditionedProperties.GetCombinedPropertyValues ("cond9").ToArray ()));

			Assert.That (new [] {
				new ValueSet (new [] { "cond10" }, new [] { "val10_1" }),
				new ValueSet (new [] { "cond10" }, new [] { "val10_2" }),
			}, Is.EquivalentTo (p.ConditionedProperties.GetCombinedPropertyValues ("cond10").ToArray ()));

			Assert.That (new [] {
				new ValueSet (new [] { "cond13" }, new [] { "val13_4" }),
			}, Is.EquivalentTo (p.ConditionedProperties.GetCombinedPropertyValues ("cond13").ToArray ()));

			Assert.That (new [] {
				new ValueSet (new [] { "cond1", "cond2" }, new [] { "val14_1", "val14_2" }),
				new ValueSet (new [] { "cond1", "cond2" }, new [] { "val14_4", "val14_3" }),
				new ValueSet (new [] { "cond1", "cond2" }, new [] { "val14_5", "val14_6" }),
			}, Is.EquivalentTo (p.ConditionedProperties.GetCombinedPropertyValues ("cond1", "cond2").ToArray ()));
		}

		[Test]
		public void StartWhitespaceForImportInsertedAsLastImport ()
		{
			var p = LoadAndEvaluate ("ConsoleApp-VS2013", "ConsoleApplication.csproj");

			MSBuildImport import = p.AddNewImport ("MyImport.targets", beforeObject: null);

			Assert.AreEqual (p.TextFormat.NewLine, p.StartWhitespace);
			Assert.AreEqual ("  ", import.StartWhitespace);
		}

		/// <summary>
		/// Inserting an import at the start as the first child was inserting an extra
		/// new line between the Project start element and the Import element.
		/// </summary>
		[Test]
		public void StartWhitespaceForImportInsertedAsFirstChild ()
		{
			var p = LoadAndEvaluate ("ConsoleApp-VS2013", "ConsoleApplication.csproj");
			var firstChild = p.GetAllObjects ().First ();

			MSBuildImport import = p.AddNewImport ("MyImport.targets", beforeObject: firstChild);

			Assert.AreEqual (p.TextFormat.NewLine, p.StartWhitespace);
			Assert.AreEqual ("  ", import.StartWhitespace);
		}

		[Test]
		public void ParseConditionWithMethodInvoke ()
		{
			// XBC 40008
			var p = LoadAndEvaluate ("msbuild-tests", "condition-parse.csproj");
			Assert.AreEqual ("Foo", p.EvaluatedProperties.GetValue ("Test1"));
			Assert.AreEqual ("Bar", p.EvaluatedProperties.GetValue ("Test2"));
		}

		[Test]
		public void ImplicitImportOfUserProject ()
		{
			var p = LoadAndEvaluate ("msbuild-project-test", "test-user.csproj");
			Assert.AreEqual ("Bar", p.EvaluatedProperties.GetValue ("TestProp"));
		}

		[Test]
		public void Transforms ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "transforms.csproj");

			Assert.AreEqual ("a-m1;b-m2;t1-", p.EvaluatedProperties.GetValue ("MetadataList"));

			// Metedata is kept when transforming
			Assert.AreEqual ("a-m1-b1_m1;b-m2-b1_m2;t1--b1_", p.EvaluatedProperties.GetValue ("MetadataList2"));

			// Exlude item with empty include
			Assert.AreEqual ("AA;BB;CC", p.EvaluatedProperties.GetValue ("EmptyItem"));

			// Loader should not crash if metadata evaluation fails
			Assert.AreEqual (";;", p.EvaluatedProperties.GetValue ("MetadataCatch"));

			// Includes can contain several transforms
			Assert.AreEqual ("a.txt;b.txt;t1.txt;TT;AA;BB;CC", p.EvaluatedProperties.GetValue ("MultiValue"));
			Assert.AreEqual ("a;b;t1;TT;AA;BB;CC", p.EvaluatedProperties.GetValue ("MultiValue2"));
		}

		[Test]
		public void TransformsWithReferences ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "transforms.csproj");

			// Count
			Assert.AreEqual ("3", p.EvaluatedProperties.GetValue ("TestCount"));

			// DirectoryName
			var dir = p.FileName.ParentDirectory.ToString ();
			Assert.AreEqual (dir + ";" + dir + ";" + dir, p.EvaluatedProperties.GetValue ("TestDirectoryName"));

			Assert.AreEqual ("m1;m2", p.EvaluatedProperties.GetValue ("TestVarInTransform"));

			Assert.AreEqual ("t1 - [t0_a.txt;b.txt;t1.txt]", p.EvaluatedProperties.GetValue ("FadaRes"));

			Assert.AreEqual ("abc@(File -> Count())", p.EvaluatedProperties.GetValue ("Func"));

			Assert.AreEqual ("@", p.EvaluatedProperties.GetValue ("Func2"));

			Assert.AreEqual ("t0 - []", p.EvaluatedProperties.GetValue ("FadaResPrev"));
		}

		[Test]
		public void ItemFunctions ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "transforms.csproj");

			// Count
			Assert.AreEqual ("3", p.EvaluatedProperties.GetValue ("TestCount"));

			// DirectoryName
			var dir = p.FileName.ParentDirectory.ToString ();
			Assert.AreEqual (dir + ";" + dir + ";" + dir, p.EvaluatedProperties.GetValue ("TestDirectoryName"));

			// Distinct
			Assert.AreEqual ("aa;bb", p.EvaluatedProperties.GetValue ("Distinct"));

			// DistinctWithCase
			Assert.AreEqual ("aa;bb;BB", p.EvaluatedProperties.GetValue ("DistinctWithCase"));

			// Reverse
			Assert.AreEqual ("BB;aa;bb;aa", p.EvaluatedProperties.GetValue ("Reverse"));

			// AnyHaveMetadataValue
			Assert.AreEqual ("true", p.EvaluatedProperties.GetValue ("AnyHaveMetadataValue"));
			Assert.AreEqual ("false", p.EvaluatedProperties.GetValue ("AnyHaveMetadataValue2"));

			// ClearMetadata
			Assert.AreEqual ("false", p.EvaluatedProperties.GetValue ("ClearMetadata"));

			// HasMetadata
			Assert.AreEqual ("a.txt;b.txt", p.EvaluatedProperties.GetValue ("HasMetadata"));

			// Metadata
			Assert.AreEqual ("m1;m2", p.EvaluatedProperties.GetValue ("Metadata"));

			// WithMetadataValue
			Assert.AreEqual ("a.txt", p.EvaluatedProperties.GetValue ("WithMetadataValue"));

			// IndexOf
			Assert.AreEqual ("1;1;2", p.EvaluatedProperties.GetValue ("IndexOf"));

			// Replace
			Assert.AreEqual ("a_._txt;b_._txt;t1_._txt", p.EvaluatedProperties.GetValue ("Replace"));

			// get_Length
			Assert.AreEqual ("5;5;6", p.EvaluatedProperties.GetValue ("get_Length"));

			// get_Chars
			Assert.AreEqual ("t;t;.", p.EvaluatedProperties.GetValue ("get_Chars"));
		}
	}
}

