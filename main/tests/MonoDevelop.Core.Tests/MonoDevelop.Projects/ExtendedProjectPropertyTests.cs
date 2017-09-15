//
// ExtendedPropertyTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ExtendedProjectPropertyTests: TestBase
	{
		[Test]
		public async Task WriteExtendedProperties ()
		{
			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = Services.ProjectService.CreateProject (tn.Guid);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;
				mp.ItemId = "{74FADC4E-C9A8-456E-9A2C-DB933220E073}";
				string dir = Util.CreateTmpDir ("WriteExtendedProperties");
				mp.FileName = Path.Combine (dir, "test.sln");
				mp.Data = new MyProjectData { Foo = "bar" };
				mp.DataProperty = new MyProjectData { Foo = "rep" };
				mp.SimpleData = "Test";
				await p.SaveAsync (Util.GetMonitor ());

				string referenceFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

				string projectXml1 = File.ReadAllText (referenceFile);
				string projectXml2 = File.ReadAllText (mp.FileName);
				Assert.AreEqual (Util.ToWindowsEndings (projectXml1), projectXml2);

				p.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task LoadExtendedProperties ()
		{
			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;

				Assert.NotNull (mp.Data);
				Assert.AreEqual (mp.Data.Foo, "bar");
				Assert.AreEqual (mp.SimpleData, "Test");

				p.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task LoadSaveExtendedPropertiesWithUnknownProperty ()
		{
			// Unknown data should be kept in the file

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-unknown-data.myproj");
			string projectXml1 = File.ReadAllText (projFile);

			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;

				Assert.NotNull (mp.Data);
				Assert.AreEqual (mp.Data.Foo, "bar");
				Assert.AreEqual (mp.SimpleData, "Test");

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task RemoveExtendedProperties ()
		{
			// Whole ProjectExtensions section should be removed

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;

				Assert.NotNull (mp.Data);
				Assert.AreEqual (mp.Data.Foo, "bar");
				Assert.AreEqual (mp.SimpleData, "Test");

				mp.Data = null;

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml1 = File.ReadAllText (Util.GetSampleProject ("extended-project-properties", "test-empty.myproj"));

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task RemoveExtendedPropertiesWithUnknownProperty ()
		{
			// Unknown data should be kept in the file

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-unknown-data.myproj");

			var tn = new MyProjectTypeNode ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyProject> (p);
				var mp = (MyProject)p;

				Assert.NotNull (mp.Data);
				Assert.AreEqual (mp.Data.Foo, "bar");
				Assert.AreEqual (mp.SimpleData, "Test");

				mp.Data = null;

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml1 = File.ReadAllText (Util.GetSampleProject ("extended-project-properties", "test-extra-data.myproj"));

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);
				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
			}
		}

		[Test]
		public async Task FlavorLoadExtendedProperties ()
		{
			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");
				p.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FlavorLoadExtendedProperties_InitialEmptyGroup ()
		{
			// Check that data load works when it is not defined in the main group
			// Test for BXC 41774.
			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data-empty-group.myproj");

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");
				p.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FlavorLoadSaveExtendedPropertiesWithUnknownProperty ()
		{
			// Unknown data should be kept in the file

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-unknown-data.myproj");
			string projectXml1 = File.ReadAllText (projFile);

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FlavorRemoveExtendedProperties ()
		{
			// Whole ProjectExtensions section should be removed

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-data.myproj");

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");

				f.Data = null;

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml1 = File.ReadAllText (Util.GetSampleProject ("extended-project-properties", "test-empty.myproj"));

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FlavorRemoveExtendedPropertiesWithUnknownProperty ()
		{
			// Unknown data should be kept in the file

			string projFile = Util.GetSampleProject ("extended-project-properties", "test-unknown-data.myproj");

			var tn = new MyEmptyProjectTypeNode ();
			var fn = new CustomItemNode<FlavorWithData> ();
			MSBuildProjectService.RegisterCustomItemType (tn);
			WorkspaceObject.RegisterCustomExtension (fn);
			try {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				Assert.IsInstanceOf<MyEmptyProject> (p);
				var mp = (MyEmptyProject)p;

				var f = mp.GetFlavor<FlavorWithData> ();
				Assert.NotNull (f.Data);
				Assert.AreEqual (f.Data.Foo, "bar");
				Assert.AreEqual (f.SimpleData, "Test");

				f.Data = null;

				await mp.SaveAsync (Util.GetMonitor ());

				string projectXml1 = File.ReadAllText (Util.GetSampleProject ("extended-project-properties", "test-extra-data.myproj"));

				string projectXml2 = File.ReadAllText (projFile);
				Assert.AreEqual (projectXml1, projectXml2);

				p.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterCustomItemType (tn);
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}
	}


	class MyProjectTypeNode: ProjectTypeNode
	{
		public MyProjectTypeNode ()
		{
			Guid = "{52136883-B1F9-4238-BAAA-0FB243663676}";
			Extension = "myproj";
		}

		public override Type ItemType {
			get {
				return typeof(MyProject);
			}
		}
	}

	class MyEmptyProjectTypeNode: ProjectTypeNode
	{
		public MyEmptyProjectTypeNode ()
		{
			Guid = "{52136883-B1F9-4238-BAAA-0FB243663676}";
			Extension = "myproj";
		}

		public override Type ItemType {
			get {
				return typeof(MyEmptyProject);
			}
		}
	}

	class MyProject: Project
	{
		[ItemProperty]
		public string SimpleData { get; set; }

		[ItemProperty (IsExternal = true)]
		public MyProjectData Data;

		[ItemProperty (WrapObject = false)]
		public MyProjectData DataProperty;
	}

	class MyProjectData
	{
		[ItemProperty]
		public string Foo { get; set; }
	}

	class MyEmptyProject: Project
	{
	}

	class FlavorWithData: ProjectExtension
	{
		[ItemProperty]
		public string SimpleData { get; set; }

		[ItemProperty (IsExternal = true)]
		public MyProjectData Data;
	}
}
