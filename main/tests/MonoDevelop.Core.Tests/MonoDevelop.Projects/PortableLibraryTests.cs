//
// PortableLibraryTests.cs
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
using System.Xml;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class PortableLibraryTests: TestBase
	{
		[Test]
		public async Task LoadPortableLibrary ()
		{
			string solFile = Util.GetSampleProject ("portable-library", "portable-library.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.FindProjectByName ("PortableLibrary");

			Assert.IsInstanceOf<DotNetProject> (p);

			var pl = (DotNetProject)p;
			Assert.AreEqual (".NETPortable", pl.GetDefaultTargetFrameworkId ().Identifier);

			sol.Dispose ();
		}

		[Test]
		public async Task BuildPortableLibrary ()
		{
			string solFile = Util.GetSampleProject ("portable-library", "portable-library.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var res = await sol.Build (Util.GetMonitor (), "Debug");
			Assert.IsNull (res.Errors.FirstOrDefault ()?.ToString ());
			sol.Dispose ();
		}

		[Test]
		public async Task PortableLibraryImplicitReferences ()
		{
			string solFile = Util.GetSampleProject ("portable-library", "portable-library.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.FindProjectByName ("PortableLibrary");
			var refs = (await p.GetReferencedAssemblies (p.Configurations [0].Selector)).Select (r => r.FilePath.FileName).ToArray ();
			sol.Dispose ();
		}

		/// <summary>
		/// With a PCL project having no references if you add a reference, then remove, then add it
		/// again then the saved project file will end up no references.
		/// </summary>
		[Test]
		public async Task AddingRemovingAndThenAddingReferenceToPortableLibrarySavesReferenceToFile ()
		{
			string solFile = Util.GetSampleProject ("portable-library", "portable-library.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.FindProjectByName ("PortableLibrary") as DotNetProject;

			Assert.AreEqual (0, p.References.Count);

			// Add System.Xml reference.
			p.References.Add (ProjectReference.CreateAssemblyReference ("System.Xml"));
			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (1, p.References.Count);

			// Remove System.Xml reference so no references remain.
			p.References.RemoveAt (0);
			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (0, p.References.Count);

			// Add System.Xml reference again.
			p.References.Add (ProjectReference.CreateAssemblyReference ("System.Xml"));
			await p.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (1, p.References.Count);

			// Ensure the references are saved to the file.
			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			p = sol.FindProjectByName ("PortableLibrary") as DotNetProject;

			Assert.AreEqual (1, p.References.Count);
			Assert.AreEqual ("System.Xml", p.References [0].Include);

			sol.Dispose ();
		}
	}
}
