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
		public async Task RemoveFileLink ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-file-link-test.csproj");

				string linkedFile = Path.Combine (projFile.ParentDirectory, "..", "test.txt");
				File.WriteAllText (linkedFile, "test");

				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (4, p.Files.Count);

				var f = p.Files.First (fi => fi.FilePath.FileName == "test.txt");
				Assert.IsTrue (f.IsLink);
				p.Files.Remove (f);

				await p.SaveAsync (Util.GetMonitor ());

				string expectedProjectXml = File.ReadAllText (p.FileName.ChangeName ("glob-file-link-test-saved1"));
				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
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

		[Test]
		public async Task RemoveAllFilesFromProject_OneFileNotDeleted_RemoveItemAddedForFileNotDeleted ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test.csproj");
				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (3, p.Files.Count);

				var f2 = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
				var f3 = p.Files.First (fi => fi.FilePath.FileName == "c3.cs");
				File.Delete (f2.FilePath);
				File.Delete (f3.FilePath);

				p.Files.Clear ();

				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-remove-saved2")), projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task RemoveAllFilesFromProject_NoFilesDeleted_RemoveItemAddedForFiles ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test.csproj");
				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (3, p.Files.Count);

				p.Files.Clear ();

				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-remove-saved3")), projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// Single .cs file found by the imported file glob. The .cs file has an Update item.
		/// On removing the file from the project, but not deleting it, was not adding a Remove item
		/// to the project.
		/// </summary>
		[Test]
		public async Task RemoveAllFilesFromProject_ProjectHasOneFileWithUpdateItem_RemoveItemAddedAndUpdateItemRemoved ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-remove-test2.csproj");
				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (3, p.Files.Count);

				var f2 = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
				var f3 = p.Files.First (fi => fi.FilePath.FileName == "c3.cs");
				File.Delete (f2.FilePath);
				File.Delete (f3.FilePath);

				p.Files.Remove (f2);
				p.Files.Remove (f3);

				await p.SaveAsync (Util.GetMonitor ());

				// Single c1.cs Update item in project. No other .cs files found by the file glob.
				// With two or more files the bug does not happen. Also need to reload the project
				// otherwise the bug does not happen.
				p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (1, p.Files.Count);

				// Remove c1.cs file but do not delete it.
				p.Files.Clear ();

				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-remove-saved2")), projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// As above but the Update item is added whilst the project is loaded. If the Update item
		/// exists when the project is loaded and then the file is removed then the Update item is
		/// removed correctly.
		/// </summary>
		[Test]
		public async Task RemoveAllFilesFromProject_ProjectHasOneFileWithUpdateItem_RemoveItemAddedAndUpdateItemRemoved2 ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test.csproj");

				// Leave only the c1.cs file.
				var c2File = projFile.ParentDirectory.Combine ("c2.cs");
				var c3File = projFile.ParentDirectory.Combine ("c3.cs");
				File.Delete (c2File);
				File.Delete (c3File);

				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (1, p.Files.Count);

				var c1File = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");
				c1File.CopyToOutputDirectory = FileCopyMode.Always;

				await p.SaveAsync (Util.GetMonitor ());

				// Remove c1.cs file but do not delete it.
				p.Files.Clear ();

				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (File.ReadAllText (p.FileName.ChangeName ("glob-remove-saved2")), projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// As above but the file is deleted not just removed from the project. If the Update item
		/// exists when the project is loaded and then the file is deleted then the Update item is
		/// removed correctly.
		/// </summary>
		[Test]
		public async Task DeleteAllFilesFromProject_ProjectHasOneFileWithUpdateItem_UpdateItemRemoved ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test.csproj");
				string expectedProjectXml = File.ReadAllText (projFile);

				// Leave only the c1.cs file.
				var c2File = projFile.ParentDirectory.Combine ("c2.cs");
				var c3File = projFile.ParentDirectory.Combine ("c3.cs");
				File.Delete (c2File);
				File.Delete (c3File);

				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (1, p.Files.Count);

				var c1File = p.Files.First (fi => fi.FilePath.FileName == "c1.cs");
				c1File.CopyToOutputDirectory = FileCopyMode.Always;

				await p.SaveAsync (Util.GetMonitor ());

				File.Delete (c1File.FilePath);
				p.Files.Clear ();

				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task FilesImportedAreMarkedAsImported ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test2.csproj");
				var textFilePath = projFile.ParentDirectory.Combine ("test.txt");
				File.WriteAllText (textFilePath, "test");
				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				Assert.AreEqual (4, p.Files.Count);

				var c2File = p.Files.First (fi => fi.FilePath.FileName == "c2.cs");
				var textFile = p.Files.First (fi => fi.FilePath.FileName == "test.txt");

				Assert.IsFalse (textFile.IsImported);
				Assert.IsTrue (c2File.IsImported);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// Tests that the %(FileName) metadata is correctly applied to a file from a Update glob.
		///
		/// Compile Update="**\*.xaml.cs" DependentUpon="%(Filename)"
		/// </summary>
		[Test]
		public async Task DependentUponUsingFileNameMetadataProperty ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-metadata-prop.csproj");
				string expectedProjectXml = File.ReadAllText (projFile);

				var xamlCSharpFileName = projFile.ParentDirectory.Combine ("test.xaml.cs");
				File.WriteAllText (xamlCSharpFileName, "csharp");
				var xamlFileName = projFile.ParentDirectory.Combine ("test.xaml");
				File.WriteAllText (xamlFileName, "xaml");
				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				var xamlCSharpFile = p.Files.Single (fi => fi.FilePath.FileName == "test.xaml.cs");
				var xamlFile = p.Files.Single (fi => fi.FilePath.FileName == "test.xaml");

				Assert.AreEqual (xamlFileName.ToString (), xamlCSharpFile.DependsOn);
				Assert.AreEqual (xamlCSharpFile.DependsOnFile, xamlFile);

				// Ensure the expanded %(FileName) does not get added to the main project on saving.
				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[Test]
		public async Task RemoveFile_WhenNotUsingAdvancedGlobSupport_ShouldNotAddRemoveItemWhenFileNotDeleted ()
		{
			string projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-test.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			string expectedProjectXml = File.ReadAllText (p.FileName);

			string fileName = p.BaseDirectory.Combine ("test.txt");
			File.WriteAllText (fileName, "Test");
			var projectFile = p.AddFile (fileName);
			await p.SaveAsync (Util.GetMonitor ());

			p.Files.Remove (projectFile);

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (expectedProjectXml, projectXml);

			p.Dispose ();
		}

		[TestCase (true)] // Add metadata to xaml file before adding to project.
		[TestCase (false)] // Do not add any metadata to xaml file before adding to project.
		public async Task AddFile_WildCardHasMetadataProperties (bool addXamlFileMetadataBeforeAddingToProject)
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-metadata-prop.csproj");
				string expectedProjectXml = File.ReadAllText (projFile);

				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;
				p.UseDefaultMetadataForExcludedExpandedItems = true;

				var xamlFileName1 = projFile.ParentDirectory.Combine ("MyView1.xaml");
				File.WriteAllText (xamlFileName1, "xaml1");
				var xamlCSharpFileName = projFile.ParentDirectory.Combine ("MyView1.xaml.cs");
				File.WriteAllText (xamlCSharpFileName, "csharpxaml");

				// Xaml file with Generator and Subtype set to match that defined in the glob.
				var xamlFile1 = new ProjectFile (xamlFileName1, BuildAction.EmbeddedResource);
				if (addXamlFileMetadataBeforeAddingToProject) {
					xamlFile1.Generator = "MSBuild:UpdateDesignTimeXaml";
					xamlFile1.ContentType = "Designer";
				}
				p.Files.Add (xamlFile1);

				var xamlCSharpFile = p.AddFile (xamlCSharpFileName);
				xamlCSharpFile.DependsOn = "MyView1.xaml";

				// Ensure no items are added to the project on saving.
				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				// Save again. A second save was adding an include for the .xaml file whilst
				// the first save was not.
				await p.SaveAsync (Util.GetMonitor ());

				projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// Add a file and then remove it but do not delete it.
		/// </summary>
		[Test]
		public async Task Remove_WildCardHasMetadataProperties ()
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-metadata-prop.csproj");
				string expectedProjectXml = File.ReadAllText (projFile);

				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				var xamlFileName1 = projFile.ParentDirectory.Combine ("MyView1.xaml");
				File.WriteAllText (xamlFileName1, "xaml1");
				var xamlCSharpFileName = projFile.ParentDirectory.Combine ("MyView1.xaml.cs");
				File.WriteAllText (xamlCSharpFileName, "csharpxaml");

				var xamlFile1 = new ProjectFile (xamlFileName1, BuildAction.EmbeddedResource);
				xamlFile1.Generator = "MSBuild:UpdateDesignTimeXaml";
				xamlFile1.ContentType = "Designer";
				p.Files.Add (xamlFile1);

				var xamlCSharpFile = p.AddFile (xamlCSharpFileName);
				xamlCSharpFile.DependsOn = "MyView1.xaml";

				// Ensure no items are added to the project on saving.
				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				// Remove .xaml.cs file but do not delete it.
				p.Files.Remove (xamlCSharpFile);
				await p.SaveAsync (Util.GetMonitor ());

				// Remove item should be added for .xaml.cs file.
				projectXml = File.ReadAllText (p.FileName);
				expectedProjectXml = File.ReadAllText (p.FileName.ChangeName ("glob-import-metadata-prop-saved1"));
				Assert.AreEqual (expectedProjectXml, projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// With no other .cs files in the project directory when removing the .xaml and .xaml.cs
		/// file from the project, but not deleting it, was not adding a Remove item for the .xaml.cs file.
		/// </summary>
		[TestCase (true)] // Adds .xaml files before loading project the first time.
		[TestCase (false)] // Loads the project first then adds the .xaml files.
		public async Task RemoveXamlAndDependentXamlCSharpFile (bool addXamlFilesBeforeLoading)
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-metadata-prop2.csproj");
				string expectedProjectXml = File.ReadAllText (projFile);

				// Ensure no other files are in the project's directory.
				FilePath oldProjFile = projFile;
				var subDir = projFile.ParentDirectory.Combine ("subdir");
				Directory.CreateDirectory (subDir);
				projFile = subDir.Combine (projFile.FileName);

				foreach (FilePath file in Directory.GetFiles (oldProjFile.ParentDirectory, "glob-import-metadata-prop2.*")) {
					var newFile = subDir.Combine (file.FileName);
					File.Move (file, newFile);
				}

				DotNetProject p = null;

				if (!addXamlFilesBeforeLoading) {
					p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
					p.UseAdvancedGlobSupport = true;
				}

				var xamlFileName = projFile.ParentDirectory.Combine ("MyView1.xaml");
				File.WriteAllText (xamlFileName, "xaml1");
				var xamlCSharpFileName = projFile.ParentDirectory.Combine ("MyView1.xaml.cs");
				File.WriteAllText (xamlCSharpFileName, "csharpxaml");

				if (addXamlFilesBeforeLoading) {
					p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
					p.UseAdvancedGlobSupport = true;
				} else {
					p.AddFiles (new [] { xamlFileName, xamlCSharpFileName });
					await p.SaveAsync (Util.GetMonitor ());
					Assert.AreEqual (expectedProjectXml, File.ReadAllText (p.FileName));
				}

				var xamlFile = p.Files.SingleOrDefault (f => f.FilePath.FileName == "MyView1.xaml");
				var xamlCSharpFile = p.Files.SingleOrDefault (f => f.FilePath.FileName == "MyView1.xaml.cs");

				// Remove files but do not delete them.
				// Remove dependency C# file first to mirror what happens in the
				// IDE when removing the .xaml file.
				p.Files.Remove (xamlCSharpFile);
				p.Files.Remove (xamlFile);

				await p.SaveAsync (Util.GetMonitor ());

				expectedProjectXml = File.ReadAllText (oldProjFile.ChangeName ("glob-import-metadata-prop2-saved3"));
				var projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// Imported wildcard: EmbeddedResource Include="**\*.xaml"
		/// Project has a EmbeddedResource Remove="MyPage.xaml"
		/// File exists and is included.
		/// Remove item should be removed.
		/// </summary>
		[TestCase (true)] // Use Project.AddFiles.
		[TestCase (false)] // Use Project.AddFile. Code is different in these methods.
		public async Task IncludeFileWithExistingRemoveItem_ItemHasImportedWildcardInclude (bool addFiles)
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-metadata-prop2.csproj");
				string expectedProjectXml = File.ReadAllText (projFile);

				var xamlFileName = projFile.ParentDirectory.Combine ("test.xaml");
				File.WriteAllText (xamlFileName, "xaml");
				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				var xamlFile = p.Files.Single (fi => fi.FilePath.FileName == "test.xaml");

				// Remove .xaml file but do not delete it.
				p.Files.Remove (xamlFile);
				await p.SaveAsync (Util.GetMonitor ());

				var projSavedFileName = projFile.ChangeName ("glob-import-metadata-prop2-saved");
				string expectedProjectXmlAfterExclude = File.ReadAllText (projSavedFileName);
				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXmlAfterExclude, projectXml);

				if (addFiles) {
					// Include file back in project. Using AddFiles to mirror what happens
					// when a file is included back in the Solution pad.
					p.AddFiles (new [] { xamlFile.FilePath });
				} else {
					// Code in AddFile is not shared with AddFiles so both are tested.
					p.AddFile (xamlFile.FilePath);
				}
				await p.SaveAsync (Util.GetMonitor ());

				// The EmbeddedResource remove item should be removed after the
				// .xaml file is included back in the project.
				projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				// Save again. On adding files in the IDE the project is written twice,
				// once in memory when the type system re-loads the project after the file is
				// added, then again when the project is saved to disk. The second write
				// was causing the EmbeddedResource to be added as a new Include item.
				await p.SaveAsync (Util.GetMonitor ());

				projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		/// <summary>
		/// Imported wildcard: EmbeddedResource Include="**\*.xaml"
		/// Project has a EmbeddedResource Remove="MyPage.xaml"
		/// File exists and is included.
		/// Remove item should be removed.
		/// </summary>
		[TestCase (true)] // Use Project.AddFiles.
		[TestCase (false)] // Use Project.AddFile. Code is different in these methods.
		public async Task AddFileWithDifferentBuildAction_ItemHasImportedWildcardInclude (bool addFiles)
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-metadata-prop2.csproj");
				string expectedProjectXml = File.ReadAllText (projFile);

				var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
				p.UseAdvancedGlobSupport = true;

				var xamlFileName = projFile.ParentDirectory.Combine ("test.xaml");
				File.WriteAllText (xamlFileName, "xaml");

				if (addFiles) {
					// Include file back in project. Using AddFiles to mirror what happens
					// when a file is included back in the Solution pad.
					p.AddFiles (new [] { xamlFileName }, BuildAction.Content);
				} else {
					// Code in AddFile is not shared with AddFiles so both are tested.
					p.AddFile (xamlFileName, BuildAction.Content);
				}
				await p.SaveAsync (Util.GetMonitor ());

				var projSavedFileName = projFile.ChangeName ("glob-import-metadata-prop2-saved2");
				string expectedProjectXmlAfterExclude = File.ReadAllText (projSavedFileName);
				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXmlAfterExclude, projectXml);

				var xamlFile = p.Files.Single (fi => fi.FilePath.FileName == "test.xaml");
				Assert.AreEqual (BuildAction.Content, xamlFile.BuildAction);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}

		[TestCase (true)] // Adds .xaml files before loading project the first time.
		[TestCase (false)] // Loads the project first then adds the .xaml files.
		public async Task RenameXamlAndXamlCSharpFileAtSameTime (bool addXamlFilesBeforeLoading)
		{
			var fn = new CustomItemNode<SupportImportedProjectFilesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				FilePath projFile = Util.GetSampleProject ("msbuild-glob-tests", "glob-import-metadata-prop2.csproj");
				string expectedProjectXml = File.ReadAllText (projFile);

				DotNetProject p = null;

				if (!addXamlFilesBeforeLoading) {
					p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
					p.UseAdvancedGlobSupport = true;
				}

				var xamlFileName = projFile.ParentDirectory.Combine ("MyView1.xaml");
				File.WriteAllText (xamlFileName, "xaml1");
				var xamlCSharpFileName = projFile.ParentDirectory.Combine ("MyView1.xaml.cs");
				File.WriteAllText (xamlCSharpFileName, "csharpxaml");

				if (addXamlFilesBeforeLoading) {
					p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
					p.UseAdvancedGlobSupport = true;
				} else {
					p.AddFiles (new [] { xamlFileName, xamlCSharpFileName });
					await p.SaveAsync (Util.GetMonitor ());
					Assert.AreEqual (expectedProjectXml, File.ReadAllText (p.FileName));
				}

				var xamlFile = p.Files.Single (f => f.FilePath.FileName == "MyView1.xaml");
				var xamlCSharpFile = p.Files.Single (f => f.FilePath.FileName == "MyView1.xaml.cs");

				// Simulate a rename of the xaml file and its dependent xaml.cs file in the Solution pad.
				var renamedXamlFileName = xamlFileName.ParentDirectory.Combine ("MyViewRename.xaml");
				FileService.RenameFile (xamlFileName, renamedXamlFileName.FileName);
				xamlFile.Name = renamedXamlFileName;

				var renamedXamlCSharpFileName = xamlCSharpFileName.ParentDirectory.Combine ("MyViewRename.xaml.cs");
				FileService.RenameFile (xamlCSharpFileName, renamedXamlCSharpFileName.FileName);
				xamlCSharpFile.Name = renamedXamlCSharpFileName;

				await p.SaveAsync (Util.GetMonitor ());

				xamlFile = p.Files.Single (f => f.FilePath.FileName == "MyViewRename.xaml");
				xamlCSharpFile = p.Files.Single (f => f.FilePath.FileName == "MyViewRename.xaml.cs");

				// Project xml should be unchanged.
				var projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);

				p.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
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
