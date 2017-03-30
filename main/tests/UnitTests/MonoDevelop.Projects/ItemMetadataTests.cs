//
// ItemMetadataTests.cs
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
using System.Xml;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Linq;
using System.IO;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ItemMetadataTests
	{
		XmlNamespaceManager nm;

		public ItemMetadataTests ()
		{
			XmlDocument doc = new XmlDocument ();
			nm = new XmlNamespaceManager (doc.NameTable);
			nm.AddNamespace ("n","http://schemas.microsoft.com/developer/msbuild/2003");
		}

		DotNetProject CreateProject ()
		{
			var p = Services.ProjectService.CreateDotNetProject ("C#");
			p.FileName = Path.GetTempFileName ();
			File.Delete (p.FileName);
			return p;
		}

		XmlElement LoadElement (string file, string itemInclude)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (file);
			return (XmlElement) doc.SelectSingleNode ("n:Project/n:ItemGroup/n:Compile[contains(@Include, '" + itemInclude + "')]", nm);
		}

		async Task<XmlElement> Save (ProjectItem it)
		{
			Project p = CreateProject ();
			p.Items.Add (it);
			try {
				await p.SaveAsync (new MonoDevelop.Core.ProgressMonitor ());
				return LoadElement (p.FileName, it.Include);
			} finally {
				System.IO.File.Delete (p.FileName);
				p.Dispose ();
			}
		}

		void AssertHasMetadata (XmlElement elem, string prop, string val)
		{
			var p = (XmlElement)elem.SelectSingleNode ("n:" + prop, nm);
			Assert.IsNotNull (p, "Property " + prop + " not found");
			Assert.AreEqual (val, p.InnerXml);
		}

		[Test]
		public async Task String ()
		{
			ProjectItem item = new ProjectFile ("t1");
			item.Metadata.SetValue ("a1", "v1");
			var e = await Save (item);
			Assert.NotNull (e);
			Assert.AreEqual (1, e.ChildNodes.Count);
			AssertHasMetadata (e, "a1", "v1");
		}

		[Test]
		public async Task StringWithDefaultValue ()
		{
			ProjectItem item = new ProjectFile ("t1");
			item.Metadata.SetValue ("a1", "v1", "v2");
			item.Metadata.SetValue ("a2", "v2", "v2");
			var e = await Save (item);
			Assert.NotNull (e);
			Assert.AreEqual (1, e.ChildNodes.Count);
			AssertHasMetadata (e, "a1", "v1");
		}

		[Test]
		public async Task StringPreserveCase ()
		{
			var p = CreateProject ();
			try {
				ProjectItem item = new ProjectFile ("t1");
				item.Metadata.SetValue ("a1", "e1");
				item.Metadata.SetValue ("a2", "E2");
				item.Metadata.SetValue ("a3", "e3");
				item.Metadata.SetValue ("a4", "E4");
				p.Items.Add (item);
				await p.SaveAsync (new ProgressMonitor ());

				var e = LoadElement (p.FileName, "t1");
				AssertHasMetadata (e, "a1", "e1");
				AssertHasMetadata (e, "a2", "E2");
				AssertHasMetadata (e, "a3", "e3");
				AssertHasMetadata (e, "a4", "E4");

				var p2 = (DotNetProject) await Services.ProjectService.ReadSolutionItem (new ProgressMonitor (), p.FileName);
				p.Dispose ();

				item = p2.Files [0];

				Assert.IsNotNull (item.Metadata.GetProperty ("a1"));
				Assert.IsNotNull (item.Metadata.GetProperty ("a2"));
				Assert.IsNotNull (item.Metadata.GetProperty ("a3"));
				Assert.IsNotNull (item.Metadata.GetProperty ("a4"));

				Assert.AreEqual ("e1", item.Metadata.GetValue ("a1"));
				Assert.AreEqual ("E2", item.Metadata.GetValue ("a2"));
				Assert.AreEqual ("e3", item.Metadata.GetValue ("a3"));
				Assert.AreEqual ("E4", item.Metadata.GetValue ("a4"));

				item.Metadata.SetValue ("a1", "E1", preserveExistingCase: true);
				item.Metadata.SetValue ("a2", "e2", preserveExistingCase: true);
				item.Metadata.SetValue ("a3", "E3", preserveExistingCase: false);
				item.Metadata.SetValue ("a4", "e4", preserveExistingCase: false);

				await p2.SaveAsync (new ProgressMonitor ());

				e = LoadElement (p2.FileName, "t1");

				Assert.NotNull (e);
				Assert.AreEqual (4, e.ChildNodes.Count);
				AssertHasMetadata (e, "a1", "e1");
				AssertHasMetadata (e, "a2", "E2");
				AssertHasMetadata (e, "a3", "E3");
				AssertHasMetadata (e, "a4", "e4");
			} finally {
				if (File.Exists (p.FileName))
					File.Delete (p.FileName);
			}
		}

		[Test]
		public async Task KeepUnevaluatedMetadata ()
		{
			var p = CreateProject ();
			try {
				var file = p.FileName;

				ProjectItem item = new ProjectFile ("t1");
				item.Metadata.SetValue ("Test", "$(MSBuildProjectDirectory)\\Test.txt");

				p.Items.Add (item);

				await p.SaveAsync (new ProgressMonitor ());
				p.Dispose ();

				p = (DotNetProject) await Services.ProjectService.ReadSolutionItem (new ProgressMonitor (), file);
				item = p.Files [0];

				var testFile = p.BaseDirectory.Combine ("Test.txt");
				var prop = item.Metadata.GetProperty ("Test");
				Assert.AreEqual (testFile.ToString (), prop.GetPathValue ().ToString ());
				Assert.AreEqual ("$(MSBuildProjectDirectory)\\Test.txt", prop.UnevaluatedValue);

				prop.SetValue (testFile);

				await p.SaveAsync (new ProgressMonitor ());
				p.Dispose ();

				// Nothing should change since the value is the same as the evaluated value

				p = (DotNetProject) await Services.ProjectService.ReadSolutionItem (new ProgressMonitor (), file);
				item = p.Files [0];

				prop = item.Metadata.GetProperty ("Test");
				Assert.AreEqual (testFile.ToString (), prop.GetPathValue ().ToString ());
				Assert.AreEqual ("$(MSBuildProjectDirectory)\\Test.txt", prop.UnevaluatedValue);
				p.Dispose ();

			} finally {
				if (File.Exists (p.FileName))
					File.Delete (p.FileName);
			}
		}
	}
}

