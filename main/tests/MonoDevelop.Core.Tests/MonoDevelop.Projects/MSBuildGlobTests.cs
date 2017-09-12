//
// MSBuildGlobTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildGlobTests: TestBase
	{
		[Test]
		public async Task RemoveFile ()
		{
			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");
			p.Files.Remove (f);

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName.ChangeName ("glob-test-saved1")));

			p.AddFile (f.FilePath);
			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (File.ReadAllText (p.FileName), File.ReadAllText (p.FileName.ChangeName ("glob-test-saved2")));

			p.Dispose ();
		}

		[Test]
		public async Task DeleteFile ()
		{
			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");
			p.Files.Remove (f);
			string text = File.ReadAllText (f.FilePath);
			File.Delete (f.FilePath);

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName.ChangeName ("glob-test-saved2")));

			File.WriteAllText (f.FilePath, text);
			p.AddFile (f.FilePath);
			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (File.ReadAllText (p.FileName), File.ReadAllText (p.FileName.ChangeName ("glob-test-saved2")));

			p.Dispose ();
		}

		[Test]
		public async Task AddFile ()
		{
			// When adding a file, if the new file is included in a glob, there is no need to add a new project item.

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");

			var newFile = f.FilePath.ChangeName ("c1bis");
			File.Copy (f.FilePath, newFile);

			p.AddFile (newFile);

			string projectXml = File.ReadAllText (p.FileName);
			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName.ChangeName ("glob-test-saved2")));

			p.Dispose ();
		}

		[Test]
		public async Task AddFileExcludedFromGlob ()
		{
			// Adding a new file is included in a glob, but the glob item has an exclude attribute that excludes the new file.
			// In this case, a new project item should be added

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");

			var newFile = f.FilePath.ChangeName ("c9");
			File.Copy (f.FilePath, newFile);

			p.AddFile (newFile);

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName.ChangeName ("glob-test-saved4")));

			p.Dispose ();
		}

		[Test]
		public async Task AddFileWithMetadata ()
		{
			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");

			var newFile = f.FilePath.ChangeName ("c1bis");
			File.Copy (f.FilePath, newFile);

			var pf = p.AddFile (newFile);
			pf.Metadata.SetValue ("foo", "bar");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName.ChangeName ("glob-test-saved3")));

			p.Dispose ();
		}

		[Test]
		public async Task ImplicitAddFile ()
		{
			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-auto-include.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (0, p.Files.Count);

			// Add file matching first glob

			var f = p.ItemDirectory.Combine ("a1.cs");
			File.WriteAllText (f, "");

			var res = p.AddItemsForFileIncludedInGlob (f).ToArray ();

			Assert.AreEqual (1, res.Length);
			Assert.IsInstanceOf<ProjectFile> (res[0]);
			Assert.AreEqual ("Compile", res[0].ItemName);
			Assert.AreEqual (1, p.Files.Count);
			Assert.IsTrue (p.Files.Contains (res[0]));

			string projectXml = File.ReadAllText (p.FileName);
		
			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName));

			// Add file matching second glob

			f = p.ItemDirectory.Combine ("1b.cs");
			File.WriteAllText (f, "");

			res = p.AddItemsForFileIncludedInGlob (f).ToArray ();

			Assert.AreEqual (1, res.Length);
			Assert.IsInstanceOf<ProjectFile> (res [0]);
			Assert.AreEqual ("Extra", res [0].ItemName);
			Assert.AreEqual (2, p.Files.Count);
			Assert.IsTrue (p.Files.Contains (res [0]));

			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName));

			// Add file matching both globs

			f = p.ItemDirectory.Combine ("a1b.cs");
			File.WriteAllText (f, "");

			res = p.AddItemsForFileIncludedInGlob (f).ToArray ();

			Assert.AreEqual (2, res.Length);
			Assert.IsInstanceOf<ProjectFile> (res [0]);
			Assert.IsInstanceOf<ProjectFile> (res [1]);
			Assert.AreEqual ("Compile", res [0].ItemName);
			Assert.AreEqual ("Extra", res [1].ItemName);
			Assert.AreEqual (4, p.Files.Count);
			Assert.IsTrue (p.Files.Contains (res [0]));
			Assert.IsTrue (p.Files.Contains (res [1]));

			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task ModifyFileWithMetadata ()
		{
			// When the metadata of a file included by a glob changes (so it doesn't match the glob anymore)
			// then the file has to be excluded from the glob

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			f.Metadata.SetValue ("foo", "bar");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-test-saved5")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task FileUpdateChangeMetadata ()
		{
			// There is an update item with metadata that is modified.
			// The update item has to be modified.

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-update1-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			Assert.AreEqual ("bar", f.Metadata.GetValue ("foo"));
			f.Metadata.SetValue ("foo", "test");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update1-test-saved")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task FileUpdateChangeMetadata2 ()
		{
			// Same as previous, but there are several update items

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-update2-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			Assert.AreEqual ("bar", f.Metadata.GetValue ("foo"));
			Assert.AreEqual ("test1", f.Metadata.GetValue ("one"));
			f.Metadata.SetValue ("foo", "test");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update2-test-saved")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task FileUpdateRemoveMetadata ()
		{
			// There is an update item with metadata that is removed.
			// The update item has to be removed

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-update1-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			f.Metadata.RemoveProperty ("foo");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update1-test-saved2")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task FileUpdateRemoveMetadata2 ()
		{
			// Same as previous, but there are several update items

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-update2-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			f.Metadata.RemoveProperty ("foo");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update2-test-saved2")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task FileUpdateChangeThenRemoveMetadata ()
		{
			// An update item is created by adding a property.
			// Then the metadata is removed.
			// The update item has to be removed

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			string originalProjFile = new FilePath (projFile).ChangeName ("glob-test-original.csproj");
			File.Copy (projFile, originalProjFile);
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			f.Metadata.SetValue ("foo", "bar");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update1-test")), projectXml);

			f.Metadata.RemoveProperty ("foo");

			await p.SaveAsync (Util.GetMonitor ());

			projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (originalProjFile), projectXml);

			p.Dispose ();
		}

		/// <summary>
		/// Same as above but the globs are defined in a .targets file that is imported
		/// into the main project.
		/// </summary>
		[Test]
		public async Task FileUpdateChangeThenRemoveMetadata2 ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test.csproj");
				string originalProjFile = new FilePath (projFile).ChangeName ("glob-import-test-original.csproj");
				File.Copy (projFile, originalProjFile);
				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (3, p.Files.Count);

				var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
				f.Metadata.SetValue ("foo", "bar");

				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-import-update1-test")), projectXml);

				f.Metadata.RemoveProperty ("foo");

				await p.SaveAsync (Util.GetMonitor ());

				projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (File.ReadAllText (originalProjFile), projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FileUpdateChangeThenRemoveMetadataAfterReload ()
		{
			// An update item is created by adding a property.
			// Then the metadata is removed.
			// The update item has to be removed

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			string originalProjFile = new FilePath (projFile).ChangeName ("glob-test-original.csproj");
			File.Copy (projFile, originalProjFile);
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			f.Metadata.SetValue ("foo", "bar");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update1-test")), projectXml);

			// Reload the project.
			p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;
			f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");

			f.Metadata.RemoveProperty ("foo");

			await p.SaveAsync (Util.GetMonitor ());

			projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (originalProjFile), projectXml);

			p.Dispose ();
		}

		/// <summary>
		/// Same as above but the globs are defined in a .targets file that is imported
		/// into the main project.
		/// </summary>
		[Test]
		public async Task FileUpdateChangeThenRemoveMetadataAfterReload2 ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test.csproj");
				string originalProjFile = new FilePath (projFile).ChangeName ("glob-import-test-original.csproj");
				File.Copy (projFile, originalProjFile);
				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (3, p.Files.Count);

				var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
				f.Metadata.SetValue ("foo", "bar");

				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-import-update1-test")), projectXml);
				p.Dispose ();

				// Reload the project.
				p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;
				f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");

				f.Metadata.RemoveProperty ("foo");

				await p.SaveAsync (Util.GetMonitor ());

				projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (File.ReadAllText (originalProjFile), projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FileUpdateChangeMetadataDefinedInGlob ()
		{
			// The glob item defines a metadata. All evaluated items have that value.
			// If the value is modified, an update item is created

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-update3-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");
			Assert.AreEqual ("bar", f.Metadata.GetValue ("foo"));
			f.Metadata.SetValue ("foo", "custom");
		
			f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			Assert.AreEqual ("custom", f.Metadata.GetValue ("foo"));

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update3-test-saved")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task FileUpdateChangeMetadataDefinedInGlob2 ()
		{
			// Same as before, but if the custom metadata is redefined in an update item, and the metadata
			// value is modified so that it has the same value as the metadata in glob item, then
			// the update item removed.

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-update3-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");
			Assert.AreEqual ("bar", f.Metadata.GetValue ("foo"));

			f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			Assert.AreEqual ("custom", f.Metadata.GetValue ("foo"));
			f.Metadata.SetValue ("foo", "bar");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update3-test-saved2")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task RemoveFileWithMetadataUpdates ()
		{
			// Same as before, but if the custom metadata is redefined in an update item, and the metadata
			// value is modified so that it has the same value as the metadata in glob item, then
			// the update item removed.

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-update4-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			p.Items.Remove (f);

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update4-test-saved")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task DeleteFile_RemoveItemAndIncludeItemExistInProject ()
		{
			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-remove-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			var f = p.Files.Single (fi => fi.FilePath.FileName == "test.cs");
			p.Files.Remove (f);
			File.Delete (f.FilePath);

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-remove-saved")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task FileUpdateRemoveMetadataDefinedInGlob ()
		{
			// The glob item defines a metadata. All evaluated items have that value.
			// If the value is modified, an update item is created

			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-update3-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			Assert.AreEqual (3, p.Files.Count);

			var f = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");
			f.Metadata.RemoveProperty ("foo");

			f = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
			f.Metadata.RemoveProperty ("foo");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-update3-test-saved3")), projectXml);

			p.Dispose ();
		}

		[Test]
		public async Task ProjectSerializationRoundtrip ()
		{
			string solFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test.csproj");
			foreach (var f in Directory.GetFiles (Path.GetDirectoryName (solFile), "*.csproj")) {
				var p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), f);

				var refXml = File.ReadAllText (p.FileName);
				await p.SaveAsync (Util.GetMonitor ());
				await p.SaveAsync (Util.GetMonitor ());
				var savedXml = File.ReadAllText (p.FileName);

				p.Dispose ();

				Console.WriteLine ("Serialization roundtrip test: " + Path.GetFileName (f));
				Assert.AreEqual (refXml, savedXml, Path.GetFileName (f) + ": roundtrip serialization failure");
			}
		}

		[Test]
		public async Task AllFilesExcludedFromDirectory ()
		{
			FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-test2.csproj");
			FilePath ignoredDirectory = projFile.ParentDirectory.Combine ("ignored");
			Directory.CreateDirectory (ignoredDirectory);
			File.WriteAllText (ignoredDirectory.Combine ("c4.cs"), "");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			var files = p.Files.Select (f => f.FilePath.FileName).OrderBy (f => f).ToArray ();
			Assert.AreEqual (new string [] {
				"c1.cs",
				"c2.cs",
				"c3.cs",
			}, files);

			p.Dispose ();
		}

		/// <summary>
		/// Checks that a Remove item is added for a new MSBuild item that has a different item type
		/// but matches the Include of an existing file glob.
		/// 
		/// Example: Add a new EmbeddedResource .cs file which matches the existing Compile file glob
		/// should result in an EmbeddedResource Include item and a Compile Remove item being added.
		/// </summary>
		[Test]
		public async Task AddFileWithDifferentMSBuildItemType_IncludeMatchesExistingGlob_AddsRemoveItemForGlob ()
		{
			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.UseAdvancedGlobSupport = true;

			string newFileName = p.BaseDirectory.Combine ("test.cs");
			File.WriteAllText (newFileName, "test");

			var projectFile = new ProjectFile (newFileName, BuildAction.EmbeddedResource);
			p.Files.Add (projectFile);
			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-import-remove-test-saved")), projectXml);

			p.Dispose ();
		}

		class SupportImportedProjectFilesProjectExtension : DotNetProjectExtension
		{
			internal protected override bool OnGetSupportsImportedItem (IMSBuildItemEvaluated buildItem)
			{
				return BuildAction.DotNetCommonActions.Contains (buildItem.Name);
			}
		}
	}
}
