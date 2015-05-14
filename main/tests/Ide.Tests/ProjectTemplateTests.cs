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
using System.Collections.Generic;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using NUnit.Framework;
using System.Text;
using UnitTests;
using System.Linq;

namespace MonoDevelop.Ide
{
	[TestFixture]
	public class ProjectTemplateTests : TestBase
	{
		[Test]
		[Ignore]
		public void CreateGtkSharpProjectTemplate ()
		{
			// This test is a placeholder to remind us that Gtk# project creation is untested.
			//
			// The GTK# template uses stetic which depends on the IdeApp being initialized, but we cannot
			// reliably start/shutdown XS as part of the test suite.
		}

		static IEnumerable<string> Templates {
			get {
				return ProjectTemplate.ProjectTemplates.Select (t => t.Category + t.Name + t.LanguageName);
			}
		}

		[Test]
		[TestCaseSource ("Templates")]
		public void CreateEveryProjectTemplate (string tt)
		{
//			var builder = new StringBuilder ();
//			foreach (var template in ProjectTemplate.ProjectTemplates) {
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Category + t.Name + t.LanguageName == tt);
				if (template.Name.Contains ("Gtk#"))
					return;
//				try {
					var dir = Util.CreateTmpDir (template.Id);
					var cinfo = new ProjectCreateInformation {
						ProjectBasePath = dir,
						ProjectName = "ProjectName",
						SolutionName = "SolutionName",
						SolutionPath = dir
					};
					cinfo.Parameters ["CreateSharedAssetsProject"] = "False";
					cinfo.Parameters ["UseUniversal"] = "True";
					cinfo.Parameters ["UseIPad"] = "False";
					cinfo.Parameters ["UseIPhone"] = "False";
					cinfo.Parameters ["CreateiOSUITest"] = "False";
					cinfo.Parameters ["CreateAndroidUITest"] = "False";

					template.CreateWorkspaceItem (cinfo);
/*				} catch (Exception ex) {
					builder.AppendFormat (
						"Could not create a project from the template '{0} / {1} ({2})': {3}",
						template.Category, template.Name, template.LanguageName, ex
					);
					builder.AppendLine ();
					builder.AppendLine ();
					builder.AppendLine (ex.ToString ());
					builder.AppendLine ();
				}*/
			//}

//			if (builder.Length > 0)
//				Assert.Fail (builder.ToString ());
		}
	}
}

