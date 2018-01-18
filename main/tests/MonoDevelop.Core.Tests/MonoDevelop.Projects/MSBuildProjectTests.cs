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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.MSBuild;
using System.IO;
using System.Linq;
using System.Xml;
using ValueSet = MonoDevelop.Projects.ConditionedPropertyCollection.ValueSet;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

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

			p.Dispose ();
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

			p.Dispose ();
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
			Assert.AreEqual (7, ig.Items.Count());

			it = ar [0];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.Include);
			Assert.AreEqual ("Test", it.Metadata.GetValue ("AttributeMetadata"));
			Assert.AreEqual ("$(Platform)", it.Metadata.GetValue ("OverriddenAttributeMetadata"));

			it = ar [1];
			Assert.AreEqual ("Files", it.Name);
			Assert.AreEqual ("file1.txt", it.Include);

			it = ar [2];
			Assert.AreEqual ("Files", it.Name);
			Assert.AreEqual ("*.txt", it.Update);
			Assert.AreEqual ("$(Configuration)", it.Metadata.GetValue ("MetaUpdate"));

			it = ar [3];
			Assert.AreEqual ("Files", it.Name);
			Assert.AreEqual ("file2.txt", it.Include);

			it = ar [4];
			Assert.AreEqual ("Files", it.Name);
			Assert.AreEqual ("file2.txt", it.Update);
			Assert.AreEqual ("$(Platform)", it.Metadata.GetValue ("MetaUpdate2"));

			it = ar [5];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.Include);

			it = ar [6];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.Include);

			p.Dispose ();
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
			Assert.AreEqual ("Files", it.Name);
			Assert.AreEqual ("file1.txt", it.UnevaluatedInclude);
			Assert.AreEqual ("file1.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("MetaUpdate"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [2]);

			// [2] is an Update element, no real elements by itself.

			it = items [5];
			Assert.AreEqual ("Files", it.Name);
			Assert.AreEqual ("file2.txt", it.UnevaluatedInclude);
			Assert.AreEqual ("file2.txt", it.Include);
			Assert.AreEqual (null, it.Metadata.GetValue ("MetaUpdate"));
			Assert.AreEqual ("AnyCPU", it.Metadata.GetValue ("MetaUpdate2"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [4]);

			// [4] is an Update element, no real elements by itself.

			it = items [6];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.UnevaluatedInclude);
			Assert.AreEqual ("file1.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray ()[1].Items.ToArray()[5]);

			it = items [7];
			Assert.AreEqual ("None", it.Name);
			Assert.AreEqual ("*.txt", it.UnevaluatedInclude);
			Assert.AreEqual ("file2.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray ()[1].Items.ToArray()[5]);

			it = items [8];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.UnevaluatedInclude);
			Assert.AreEqual ("file1.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta3"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [6]);

			it = items [9];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.UnevaluatedInclude);
			Assert.AreEqual ("file2.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta3"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [6]);

			it = items [10];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.UnevaluatedInclude);
			Assert.AreEqual ("file1.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta3"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [6]);

			it = items [11];
			Assert.AreEqual ("Transformed", it.Name);
			Assert.AreEqual ("@(None -> WithMetadataValue('Meta2', 'Debug'))", it.UnevaluatedInclude);
			Assert.AreEqual ("file2.txt", it.Include);
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta2"));
			Assert.AreEqual ("Debug", it.Metadata.GetValue ("Meta3"));
			Assert.IsNotNull (it.SourceItem);
			Assert.AreSame (it.SourceItem, p.ItemGroups.ToArray () [1].Items.ToArray () [6]);

			p.Dispose ();
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

			p.Dispose ();
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

			p.Dispose ();
		}

		[Test]
		public void EvalExists ()
		{
			var p = LoadProject ();
			p.Evaluate ();
			var res = p.EvaluatedProperties.GetValue ("ExistsTest");
			Assert.AreEqual ("OK", res);

			p.Dispose ();
		}

		[Test]
		public void EvalExistsWhenNotInsideQuotes ()
		{
			var p = LoadProject ();
			p.Evaluate ();
			var res = p.EvaluatedProperties.GetValue ("ExistsNotInsideQuotesTest");
			Assert.AreEqual ("OK", res);

			p.Dispose ();
		}

		[Test]
		//[SetCulture ("cs-CZ")] Does not work. Culture is not changed.
		public void ConditionUsingEmptyStringsIsEvaluatedCorrectlyForCzechLocale ()
		{
			var currentCulture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = new CultureInfo ("cs-CZ");

			try {
				var p = LoadProject ();
				p.Evaluate ();
				var res = p.EvaluatedProperties.GetValue ("EmptyStringConditionProp");
				Assert.AreEqual ("OK", res);

				p.Dispose ();
			} finally {
				Thread.CurrentThread.CurrentCulture = currentCulture;
			}
		}

		[Test]
		public void ImportGroups ()
		{
			var p = LoadAndEvaluate ("project-with-import-groups", "import-group-test.csproj");

			Assert.AreEqual ("v2", p.EvaluatedProperties.GetValue ("TestProp"));
			Assert.AreEqual ("one", p.EvaluatedProperties.GetValue ("PropFromTest1"));
			Assert.AreEqual ("two", p.EvaluatedProperties.GetValue ("PropFromTest2"));
			Assert.AreEqual ("three", p.EvaluatedProperties.GetValue ("PropFromFoo"));

			p.Dispose ();
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

			p.Dispose ();
		}

		[Test]
		public void ParseConditionWithoutQuotes ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "condition-parse.csproj");
			Assert.AreEqual (new [] {"aa","vv","test"}, p.EvaluatedItems.Select (i => i.Include).ToArray ());
			p.Dispose ();
		}

		[Test]
		public void ConditionRelationalExpressions ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "condition-relational-expressions.targets");
			Assert.AreEqual ("С2С3С4С7С10С12С14С17С18С20С23С24", p.EvaluatedProperties.GetValue("Answer"));
			p.Dispose ();
		}

		[Test]
		public void EvalItemsAfterProperties ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "property-eval-order.csproj");
			Assert.AreEqual (new [] {"Two"}, p.EvaluatedItems.Select (i => i.Include).ToArray ());
			p.Dispose ();
		}

		[Test]
		public void FunctionProperties ()
		{
			var p = LoadAndEvaluate ("msbuild-tests", "functions.csproj");

			Assert.AreEqual ("bcd", p.EvaluatedProperties.GetValue ("Substring"));
			Assert.AreEqual ("bcd", p.EvaluatedProperties.GetValue ("SubstringIgnoreCase"));
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
			Assert.AreEqual ("a", p.EvaluatedProperties.GetValue ("StringAtIndex0"));
			Assert.AreEqual ("b", p.EvaluatedProperties.GetValue ("StringAtIndex1"));

			var dir = System.IO.Path.GetFullPath (System.IO.Path.Combine (System.IO.Path.GetDirectoryName (p.FileName), "foo"));
			Assert.AreEqual (dir, p.EvaluatedProperties.GetValue ("FullPath"));

			Assert.AreEqual ("00065535.0", p.EvaluatedProperties.GetValue ("DoubleNumber"));
			Assert.AreEqual ("56735", p.EvaluatedProperties.GetValue ("DoubleNumberComplex"));

			Assert.AreEqual (Path.Combine ("a", "b", "c", "d", "e", "f"), p.EvaluatedProperties.GetValue ("ParamsPathCombine"));

			var specialFolder = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			Assert.AreEqual (specialFolder, p.EvaluatedProperties.GetValue ("EnumFolderPath"));

			var basePath = Path.GetDirectoryName (p.FileName);
			var targets = Path.Combine (basePath, "false.targets");

			Assert.AreEqual (targets, p.EvaluatedProperties.GetValue ("PathOfFileAbove"));
			Assert.AreEqual (targets, p.EvaluatedProperties.GetValue ("DirectoryNameOfFileAbove"));

			Assert.AreEqual ("a/", p.EvaluatedProperties.GetValue ("EnsureTrailingSlash"));
			Assert.AreEqual (Path.Combine (Environment.CurrentDirectory, "a/"), p.EvaluatedProperties.GetValue ("NormalizeDirectory"));
			Assert.AreEqual (Path.Combine (Environment.CurrentDirectory, "a"), p.EvaluatedProperties.GetValue ("NormalizePath"));

			p.Dispose ();
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

			p.Dispose ();
		}

		[Test]
		public void StartWhitespaceForImportInsertedAsLastImport ()
		{
			var p = LoadAndEvaluate ("ConsoleApp-VS2013", "ConsoleApplication.csproj");

			MSBuildImport import = p.AddNewImport ("MyImport.targets", beforeObject: null);

			Assert.AreEqual (p.TextFormat.NewLine, p.StartWhitespace);
			Assert.AreEqual ("  ", import.StartWhitespace);

			p.Dispose ();
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
			p.Dispose ();
		}

		[Test]
		public void ParseConditionWithMethodInvoke ()
		{
			// XBC 40008
			var p = LoadAndEvaluate ("msbuild-tests", "condition-parse.csproj");
			Assert.AreEqual ("Foo", p.EvaluatedProperties.GetValue ("Test1"));
			Assert.AreEqual ("Bar", p.EvaluatedProperties.GetValue ("Test2"));
			p.Dispose ();
		}

		[Test]
		public void ImplicitImportOfUserProject ()
		{
			var p = LoadAndEvaluate ("msbuild-project-test", "test-user.csproj");
			Assert.AreEqual ("Bar", p.EvaluatedProperties.GetValue ("TestProp"));
			p.Dispose ();
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
			p.Dispose ();
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
			p.Dispose ();
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
			p.Dispose ();
		}

		[Test]
		public void AddKnownAttributeToMSBuildItem ()
		{
			var p = new MSBuildProject ();
			p.LoadXml ("<Project ToolsVersion=\"15.0\" />");
			p.AddKnownItemAttribute ("Test", "Known");

			var item = p.AddNewItem ("Test", "Include");
			item.Metadata.SetValue ("Known", "KnownAttributeValue");

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var itemGroupElement = (XmlElement)doc.DocumentElement.ChildNodes[0];
			var itemElement = (XmlElement)itemGroupElement.ChildNodes[0];

			Assert.AreEqual ("Test", itemElement.Name);
			Assert.AreEqual ("KnownAttributeValue", itemElement.GetAttribute ("Known"));
			Assert.AreEqual (0, itemElement.ChildNodes.Count);
			Assert.IsTrue (itemElement.IsEmpty);
			p.Dispose ();
		}

		[Test]
		public void AddKnownAttributeToMSBuildItemForExistingAttribute ()
		{
			var p = new MSBuildProject ();
			string projectXml =
				"<Project ToolsVersion=\"15.0\">\r\n" +
				"  <ItemGroup>\r\n" +
				"    <Test Include=\"Include\" Known=\"KnownAttributeValue\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			p.LoadXml (projectXml);

			p.AddKnownItemAttribute ("Test", "Known", "Another");
			var item = p.ItemGroups.Single ().Items.Single ();
			item.Metadata.SetValue ("Another", "AnotherValue");

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var itemGroupElement = (XmlElement)doc.DocumentElement.ChildNodes[0];
			var itemElement = (XmlElement)itemGroupElement.ChildNodes[0];

			Assert.AreEqual ("Test", itemElement.Name);
			Assert.AreEqual ("KnownAttributeValue", itemElement.GetAttribute ("Known"));
			Assert.AreEqual ("AnotherValue", itemElement.GetAttribute ("Another"));
			Assert.AreEqual (0, itemElement.ChildNodes.Count);
			Assert.IsTrue (itemElement.IsEmpty);
			p.Dispose ();
		}

		[Test]
		public void Remove ()
		{
			var p = LoadAndEvaluate ("msbuild-project-test", "test-remove.csproj");

			var items = p.EvaluatedItems.Where (it => it.Name == "Test1").Select (it => it.Include).ToArray ();
			Assert.AreEqual (new [] { "file1.txt", "support\\file1.txt" }, items);

			items = p.EvaluatedItems.Where (it => it.Name == "Test2").Select (it => it.Include).ToArray ();
			Assert.AreEqual (new [] { "file2.txt", "support\\file1.txt" }, items);

			items = p.EvaluatedItems.Where (it => it.Name == "Test3").Select (it => it.Include).ToArray ();
			Assert.AreEqual (new [] { "file2.txt" }, items);
			p.Dispose ();
		}

		[Test]
		public void SdkProjectMSBuildXmlNamespaceIsNotSaved ()
		{
			var p = new MSBuildProject ();
			p.LoadXml ("<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\" />");

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			Assert.IsFalse (doc.DocumentElement.HasAttribute ("xmlns"));
			p.Dispose ();
		}

		[Test]
		public void NonSdkProjectMSBuildXmlNamespaceIsAddedOnSaving ()
		{
			var p = new MSBuildProject ();
			p.LoadXml ("<Project ToolsVersion=\"15.0\" />");

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var xmlnsAttributeValue = doc.DocumentElement.GetAttribute ("xmlns");
			Assert.AreEqual ("http://schemas.microsoft.com/developer/msbuild/2003", xmlnsAttributeValue);
			p.Dispose ();
		}

		[Test]
		public void ExistingSdkProjectMSBuildXmlNamespaceRemovedOnSaving ()
		{
			var p = new MSBuildProject ();
			p.LoadXml ("<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" />");

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			Assert.IsFalse (doc.DocumentElement.HasAttribute ("xmlns"));
			p.Dispose ();
		}

		[Test]
		public void NonSdkProjectMSBuildXmlNamespaceIsNotRemovedOnSaving ()
		{
			var p = new MSBuildProject ();
			p.LoadXml ("<Project ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" />");

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var xmlnsAttributeValue = doc.DocumentElement.GetAttribute ("xmlns");
			Assert.AreEqual ("http://schemas.microsoft.com/developer/msbuild/2003", xmlnsAttributeValue);
			p.Dispose ();
		}

		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"")]
		[TestCase ("ToolsVersion=\"15.0\"")]
		public void MSBuildXmlNamespaceNotAddedToChildElementsOnSaving (string projectElementAttributes)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var propertyGroup = (XmlElement)doc.DocumentElement.ChildNodes[0];
			Assert.IsFalse (propertyGroup.HasAttribute ("xmlns"));
			Assert.AreEqual ("PropertyGroup", propertyGroup.Name);
			p.Dispose ();
		}

		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"")]
		[TestCase ("ToolsVersion=\"15.0\"")]
		public void MSBuildXmlNamespaceNotAddedToCustomCommand (string projectElementAttributes)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \">\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);
			var propertyGroup = (MSBuildPropertyGroup)p.ChildNodes[1];
			var customCommand = new CustomCommand {
				Command = "Test"
			};
			var config = new ItemConfiguration ("Debug", "AnyCPU");
			config.CustomCommands.Add (customCommand);
			propertyGroup.WriteObjectProperties (config, config.GetType (), true);

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var propertyGroupElement = (XmlElement)doc.DocumentElement.ChildNodes[1];
			var commandsElement = (XmlElement)propertyGroupElement.ChildNodes[0];
			Assert.IsFalse (commandsElement.HasAttribute ("xmlns"));
			Assert.AreEqual ("CustomCommands", commandsElement.Name);
			Assert.AreEqual (1, propertyGroupElement.ChildNodes.Count);

			commandsElement = (XmlElement)commandsElement.ChildNodes[0];
			Assert.IsFalse (commandsElement.HasAttribute ("xmlns"));
			Assert.AreEqual ("CustomCommands", commandsElement.Name);
			p.Dispose ();
		}

		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"")]
		[TestCase ("ToolsVersion=\"15.0\"")]
		public void MSBuildXmlNamespaceNotAddedToExternalProperties (string projectElementAttributes)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);
			var config = new TestExternalPropertiesConfig ();
			p.WriteExternalProjectProperties (config, config.GetType (), true);

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var projectExtensions = (XmlElement)doc.DocumentElement.ChildNodes[1];
			var monoDevelopElement = (XmlElement)projectExtensions.ChildNodes[0];
			Assert.IsFalse (monoDevelopElement.HasAttribute ("xmlns"));
			Assert.AreEqual ("MonoDevelop", monoDevelopElement.Name);

			var propertiesElement = (XmlElement)monoDevelopElement.ChildNodes[0];
			Assert.IsFalse (propertiesElement.HasAttribute ("xmlns"));
			Assert.AreEqual ("Properties", propertiesElement.Name);

			var externalElement = (XmlElement)propertiesElement.ChildNodes[0];
			Assert.IsFalse (externalElement.HasAttribute ("xmlns"));
			Assert.AreEqual ("External", externalElement.Name);
			p.Dispose ();
		}

		public class TestExternalPropertiesConfig : ItemConfiguration
		{
			public TestExternalPropertiesConfig ()
				: base ("Debug", "AnyCPU")
			{
			}

			[ItemProperty("External", IsExternal=true)]
			TestExternalPropertyObject external = new TestExternalPropertyObject ();
		}

		[DataItem ("TestExternalPropertyObject")]
		public class TestExternalPropertyObject
		{
			[ItemProperty ("Value1")]
			public string value1 = "Test";
		}

		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"")]
		[TestCase ("ToolsVersion=\"15.0\"")]
		public void RemoveMonoDevelopProjectExtension (string projectElementAttributes)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);
			var config = new TestExternalPropertiesConfig ();
			p.WriteExternalProjectProperties (config, config.GetType (), true);

			var externalElement = p.GetMonoDevelopProjectExtension ("External");
			Assert.IsNotNull (externalElement);

			p.RemoveMonoDevelopProjectExtension ("External");

			externalElement = p.GetMonoDevelopProjectExtension ("External");
			Assert.IsNull (externalElement);
			p.Dispose ();
		}

		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"")]
		[TestCase ("ToolsVersion=\"15.0\"")]
		public void UpdatingMonoDevelopProjectExtensionShouldNotAddAnotherXmlElement (string projectElementAttributes)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);
			var config = new TestExternalPropertiesConfig ();
			p.WriteExternalProjectProperties (config, config.GetType (), true);

			// Update existing extension.
			config = new TestExternalPropertiesConfig ();
			p.WriteExternalProjectProperties (config, config.GetType (), true);

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var projectExtensions = (XmlElement)doc.DocumentElement.ChildNodes[1];
			var monoDevelopElement = (XmlElement)projectExtensions.ChildNodes[0];
			var propertiesElement = (XmlElement)monoDevelopElement.ChildNodes[0];
			var externalElement = (XmlElement)propertiesElement.ChildNodes[0];

			Assert.AreEqual ("External", externalElement.Name);
			Assert.AreEqual (1, monoDevelopElement.ChildNodes.Count);
			p.Dispose ();
		}

		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"",
			"<ExtensionData>Value</ExtensionData>",
			false)]
		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"",
			"<ExtensionData xmlns=\"\">Value</ExtensionData>",
			false)]
		[TestCase ("ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"",
			"<ExtensionData>Value</ExtensionData>",
			true)] // xmlns=''
		[TestCase ("ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"",
			"<ExtensionData xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">Value</ExtensionData>",
			false)]
		public void SetMonoDevelopProjectExtension (
			string projectElementAttributes,
			string extensionXml,
			bool expectedHasXmlAttribute)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			var doc = new XmlDocument ();
			doc.LoadXml (extensionXml);
			var element = doc.DocumentElement;
			p.SetMonoDevelopProjectExtension ("Test", element);

			string xml = p.SaveToString ();
			doc = new XmlDocument ();
			doc.LoadXml (xml);

			var projectExtensions = (XmlElement)doc.DocumentElement.ChildNodes[1];
			var monoDevelopElement = (XmlElement)projectExtensions.ChildNodes[0];
			var propertiesElement = (XmlElement)monoDevelopElement.ChildNodes[0];
			var extensionDataElement = (XmlElement)propertiesElement.ChildNodes[0];

			Assert.AreEqual (expectedHasXmlAttribute, extensionDataElement.HasAttribute ("xmlns"));
			Assert.AreEqual ("ExtensionData", extensionDataElement.Name);
			p.Dispose ();
		}

		/// <summary>
		/// This works without any changes to MSBuildProperty using the full
		/// MSBuild xmlns value.
		/// </summary>
		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"")]
		[TestCase ("ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"")]
		public void PropertyWithChildXmlElement (string projectElementAttributes)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"    <Test1>\r\n" +
				"      <Test2>\r\n" +
				"        <Test3></Test3>\r\n" +
				"      </Test2>\r\n" +
				"    </Test1>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var properties = (XmlElement)doc.DocumentElement.ChildNodes[0];
			var test1Element = (XmlElement)properties.ChildNodes[1];
			var test2Element = (XmlElement)test1Element.ChildNodes[0];
			var test3Element = test2Element.ChildNodes.OfType<XmlElement> ().First ();

			Assert.AreEqual ("Test1", test1Element.Name);
			Assert.AreEqual ("Test2", test2Element.Name);
			Assert.AreEqual ("Test3", test3Element.Name);
			Assert.IsFalse (test1Element.HasAttribute ("xmlns"));
			Assert.IsFalse (test2Element.HasAttribute ("xmlns"));
			Assert.IsFalse (test3Element.HasAttribute ("xmlns"));
			p.Dispose ();
		}

		/// <summary>
		/// This works without any changes to MSBuildProperty using the full
		/// MSBuild xmlns value.
		/// </summary>
		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"")]
		[TestCase ("ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"")]
		public void SetPropertyWithChildXmlElement (string projectElementAttributes)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"    <Test1></Test1>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			string propertyValue =
				"<Test2 xmlns='" + p.Namespace + "'>\r\n" +
				"  <Test3>Value</Test3>\r\n" +
				"</Test2>";
			var globalGroup = p.GetGlobalPropertyGroup ();
			var test1Property = globalGroup.GetProperty ("Test1");
			test1Property.SetValue (propertyValue);

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var properties = (XmlElement)doc.DocumentElement.ChildNodes[0];
			var test1Element = (XmlElement)properties.ChildNodes[1];
			var test2Element = (XmlElement)test1Element.ChildNodes[0];
			var test3Element = test2Element.ChildNodes.OfType<XmlElement> ().First ();

			Assert.AreEqual ("Test1", test1Element.Name);
			Assert.AreEqual ("Test2", test2Element.Name);
			Assert.AreEqual ("Test3", test3Element.Name);
			Assert.IsFalse (test1Element.HasAttribute ("xmlns"));
			Assert.IsFalse (test2Element.HasAttribute ("xmlns"));
			Assert.IsFalse (test3Element.HasAttribute ("xmlns"));
			p.Dispose ();
		}

		[TestCase ("Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\"")]
		[TestCase ("ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"")]
		public void PatchedImport (string projectElementAttributes)
		{
			string projectXml =
				"<Project " + projectElementAttributes + ">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"    <Test1></Test1>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <Import Project=\"Original.targets\" />\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			var import = p.Imports.Single ();

			var sw = new StringWriter ();
			var xw = XmlWriter.Create (sw, new XmlWriterSettings {
				OmitXmlDeclaration = true,
				NewLineChars = "\r\n",
				NewLineHandling = NewLineHandling.Replace
			});

			xw.WriteStartElement (string.Empty, "Root", p.Namespace);
			import.WritePatchedImport (xw, "Updated.targets");
			xw.WriteEndElement ();
			xw.Dispose ();

			var doc = new XmlDocument ();
			doc.LoadXml (sw.ToString ());

			var import1 = (XmlElement)doc.DocumentElement.ChildNodes [0];
			var import2 = (XmlElement)doc.DocumentElement.ChildNodes [1];

			Assert.AreEqual ("Original.targets", import1.GetAttribute ("Project"));
			Assert.AreEqual ("Exists('Original.targets')", import1.GetAttribute ("Condition"));
			Assert.AreEqual ("Updated.targets", import2.GetAttribute ("Project"));
			Assert.AreEqual ("!Exists('Original.targets')", import2.GetAttribute ("Condition"));
			Assert.IsFalse (import1.HasAttribute ("xmlns"));
			Assert.IsFalse (import2.HasAttribute ("xmlns"));
			p.Dispose ();
		}

		/// <summary>
		/// Remove items should be grouped together with MSBuildItems with the same type.
		/// </summary>
		[Test]
		public void AddRemoveItem ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			var removeItem = p.CreateItem ("None", "Text2.txt");
			removeItem.Remove = "Text2.txt";
			removeItem.Include = null;
			p.AddItem (removeItem);

			p.AddNewItem ("None", "Text1.txt");

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Remove=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Include=\"Text1.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		/// <summary>
		/// Remove items should be added before Include items in their own ItemGroup.
		/// </summary>
		[Test]
		public void AddRemoveItemBeforeIncludeItem ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			p.AddNewItem ("None", "Text1.txt");

			var removeItem = p.CreateItem ("None", "Text2.txt");
			removeItem.Remove = "Text2.txt";
			removeItem.Include = null;
			p.AddItem (removeItem);

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Remove=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Include=\"Text1.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		[Test]
		public void AddRemoveItemBeforeIncludeItemOfTheSameKind ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <Reference Include=\"System.Xml\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			p.AddNewItem ("None", "Text1.txt");

			var removeItem = p.CreateItem ("None", "Text2.txt");
			removeItem.Remove = "Text2.txt";
			removeItem.Include = null;
			p.AddItem (removeItem);

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <Reference Include=\"System.Xml\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Remove=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Include=\"Text1.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		/// <summary>
		/// Remove items should be added before Update items in their own ItemGroup.
		/// </summary>
		[Test]
		public void AddRemoveItemBeforeUpdateItem ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			var updateItem = p.CreateItem ("None", "Text1.txt");
			updateItem.Update = "Text1.txt";
			updateItem.Include = null;
			p.AddItem (updateItem);

			var removeItem = p.CreateItem ("None", "Text2.txt");
			removeItem.Remove = "Text2.txt";
			removeItem.Include = null;
			p.AddItem (removeItem);

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Remove=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Update=\"Text1.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		[Test]
		public void AddRemoveItemBeforeUpdateItemOfSameKind ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <Compile Update=\"a.cs\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			var updateItem = p.CreateItem ("None", "Text1.txt");
			updateItem.Update = "Text1.txt";
			updateItem.Include = null;
			p.AddItem (updateItem);

			var removeItem = p.CreateItem ("None", "Text2.txt");
			removeItem.Remove = "Text2.txt";
			removeItem.Include = null;
			p.AddItem (removeItem);

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <Compile Update=\"a.cs\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Remove=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Update=\"Text1.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		/// <summary>
		/// Remove items should be added before Include items in their own ItemGroup.
		/// </summary>
		[Test]
		public void AddRemoveItemAfterWildcardIncludeItem ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			MSBuildItem item = p.AddNewItem ("None", @"**\*.txt");
			item.EvaluatedItemCount = 2;

			var removeItem = p.CreateItem ("None", "Text2.txt");
			removeItem.Remove = "Text2.txt";
			removeItem.Include = null;
			p.AddItem (removeItem);

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Include=\"**\\*.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Remove=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		/// <summary>
		/// Update items should be grouped together with MSBuildItems with the same type.
		/// </summary>
		[Test]
		public void AddUpdateItem ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			p.AddNewItem ("None", "Text1.txt");

			var updateItem = p.CreateItem ("None", "Text2.txt");
			updateItem.Update = "Text2.txt";
			updateItem.Include = null;
			p.AddItem (updateItem);

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Include=\"Text1.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Update=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		/// <summary>
		/// Include items should be inserted before existing Update items
		/// in their own ItemGroup.
		/// </summary>
		[Test]
		public void AddIncludeItemBeforeUpdateItem ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			var updateItem = p.CreateItem ("None", "Text2.txt");
			updateItem.Update = "Text2.txt";
			updateItem.Include = null;
			p.AddItem (updateItem);

			p.AddNewItem ("None", "Text1.txt");

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Include=\"Text1.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Update=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		[Test]
		public void AddIncludeItemBeforeUpdateItemOfSameKind ()
		{
			string projectXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <Compile Update=\"a.cs\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";

			var p = new MSBuildProject ();
			p.LoadXml (projectXml);

			var updateItem = p.CreateItem ("None", "Text2.txt");
			updateItem.Update = "Text2.txt";
			updateItem.Include = null;
			p.AddItem (updateItem);

			p.AddNewItem ("None", "Text1.txt");

			string xml = p.SaveToString ();

			string expectedXml =
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <Compile Update=\"a.cs\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Include=\"Text1.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"  <ItemGroup>\r\n" +
				"    <None Update=\"Text2.txt\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";
			Assert.AreEqual (expectedXml, xml);
			p.Dispose ();
		}

		[Test]
		public void GlobalPropertyProvider ()
		{
			var prov = new CustomGlobalPropertyProvider ("Works!");
			MSBuildProjectService.RegisterGlobalPropertyProvider (prov);
			try {
				var p = LoadProject ();
				p.Evaluate ();

				var pg = p.EvaluatedProperties;
				Assert.AreEqual ("Works!", pg.GetValue ("TEST_GLOBAL"));

			} finally {
				MSBuildProjectService.UnregisterGlobalPropertyProvider (prov);
			}
		}

		[Test]
		public void MultipleGlobalPropertyProvider ()
		{
			var prov1 = new CustomGlobalPropertyProvider ("First");
			var prov2 = new CustomGlobalPropertyProvider ("Second");
			MSBuildProjectService.RegisterGlobalPropertyProvider (prov1);
			MSBuildProjectService.RegisterGlobalPropertyProvider (prov2);
			try {
				var p = LoadProject ();
				p.Evaluate ();

				var pg = p.EvaluatedProperties;
				Assert.AreEqual ("Second", pg.GetValue ("TEST_GLOBAL"));

			} finally {
				MSBuildProjectService.UnregisterGlobalPropertyProvider (prov1);
				MSBuildProjectService.UnregisterGlobalPropertyProvider (prov2);
			}
		}
	}

	class CustomGlobalPropertyProvider : IMSBuildGlobalPropertyProvider
	{
		public event EventHandler GlobalPropertiesChanged { add { } remove { } }

		string result;

		public CustomGlobalPropertyProvider (string result) => this.result = result;

		public IDictionary<string, string> GetGlobalProperties ()
		{
			var props = new Dictionary<string, string> ();
			props ["TEST_GLOBAL"] = result;
			return props;
		}
	}
}

