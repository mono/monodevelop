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
				item.Metadata.SetValue ("a1", "v1");
				item.Metadata.SetValue ("a2", "V2");
				item.Metadata.SetValue ("a3", "v3");
				item.Metadata.SetValue ("a4", "V4");
				p.Items.Add (item);
				await p.SaveAsync (new ProgressMonitor ());

				var p2 = (DotNetProject) await Services.ProjectService.ReadSolutionItem (new ProgressMonitor (), p.FileName);
				p.Dispose ();

				item = p2.Files [0];

				item.Metadata.SetValue ("a1", "V1", preserveExistingCase: true);
				item.Metadata.SetValue ("a2", "v2", preserveExistingCase: true);
				item.Metadata.SetValue ("a3", "V3", preserveExistingCase: false);
				item.Metadata.SetValue ("a4", "v4", preserveExistingCase: false);

				await p2.SaveAsync (new ProgressMonitor ());

				var e = LoadElement (p2.FileName, "t1");

				Assert.NotNull (e);
				Assert.AreEqual (4, e.ChildNodes.Count);
				AssertHasMetadata (e, "a1", "v1");
				AssertHasMetadata (e, "a2", "V2");
				AssertHasMetadata (e, "a3", "V3");
				AssertHasMetadata (e, "a4", "v4");
			} finally {
				if (File.Exists (p.FileName))
					File.Delete (p.FileName);
			}
		}
	}
}

