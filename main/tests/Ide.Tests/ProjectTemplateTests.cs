//
// MyClass.cs
//
// Author:
//       alan <>
//
// Copyright (c) 2013 alan
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
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace MonoDevelop.Ide
{
	[TestFixture]
	public class ProjectTemplateTests : TestBase
	{
		string TempDir {
			get; set;
		}

		public override void Setup ()
		{
			base.Setup ();
			var currentDir = Path.GetDirectoryName (typeof(ProjectTemplateTests).Assembly.Location);
			TempDir = Path.GetFullPath (Path.Combine (currentDir, "TempDirForTests"));
		}

		public override void Teardown ()
		{
			base.Teardown ();
			try { Directory.Delete (TempDir, true); } catch { }
		}

		[Test]
		[Ignore]
		public void CreateGtkSharpProjectTemplate ()
		{
			// This test is a placeholder to remind us that Gtk# project creationg is untested because
			// we cannot reliably start/shutdown XS as part of the test suite. We hit may differnt kinds
			// of race condition once we initialize the ide services.
		}

		[Test]
		public void CreateEveryProjectTemplate ()
		{
			var builder = new StringBuilder ();
			foreach (var template in ProjectTemplate.ProjectTemplates) {
				if (template.Name.Contains ("Gtk#"))
					continue;

				try {
					try { Directory.Delete (TempDir, true); } catch { }
					var cinfo = new ProjectCreateInformation {
						ProjectBasePath = TempDir,
						ProjectName =  "ProjectName",
						SolutionName =  "SolutionName",
						SolutionPath = TempDir
					};
					template.CreateWorkspaceItem (cinfo);
				} catch {
					builder.AppendFormat ("Could not create a project from the template '{0} / {1}'", template.Category, template.Name);
					builder.AppendLine ();
				}
			}

			if (builder.Length > 0)
				Assert.Fail (builder.ToString ());
		}
	}
}

