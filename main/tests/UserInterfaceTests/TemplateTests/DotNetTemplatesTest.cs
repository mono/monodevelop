//
// MonoDevelopTemplatesTest.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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

using NUnit.Framework;

namespace UserInterfaceTests
{
	[TestFixture]
	[Category("DotNet")]
	public class MonoDevelopTemplatesTest : CreateBuildTemplatesTestBase
	{
		readonly string dotNetCategory = ".NET";

		[Test]
		[TestCase ("Console Project", BeforeBuildAction.None, TestName = "TestCreateBuildConsoleProject", Description = "Create and build C# Console Project")]
		[TestCase ("Gtk# 2.0 Project", BeforeBuildAction.None, TestName = "TestCreateBuildGtkSharp20Project", Description = "Create and build a GTK#2 Project")]
		[TestCase ("Library", BeforeBuildAction.None, TestName = "TestCreateBuildLibrary", Description = "Create and build a Library Project")]
		[TestCase ("NUnit Library Project", BeforeBuildAction.WaitForPackageUpdate, TestName = "TestCreateBuildNUnitLibraryProject",
			Description = "Create and build NUnit Library Project")]
		public void RunDotNetTests (string templateName, BeforeBuildAction beforeBuild)
		{
			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = dotNetCategory,
				TemplateKindRoot = GeneralKindRoot,
				TemplateKind = templateName
			};
			CreateBuildProject (templateOptions, beforeBuild.GetAction ());
			IsTemplateSelected (templateOptions);
		}
	}
}
