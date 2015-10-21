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

		[Test, Timeout (90000)]
		[TestCase ("Console Project", 30, TestName = "TestCreateBuildConsoleProject", Description = "Create and build C# Console Project", Category="Smoke")]
		[TestCase ("Gtk# 2.0 Project", 30, TestName = "TestCreateBuildGtkSharp20Project", Description = "Create and build a GTK#2 Project")]
		[TestCase ("Library", 30, TestName = "TestCreateBuildLibrary", Description = "Create and build a Library Project")]
		[TestCase ("NUnit Library Project", 50, TestName = "TestCreateBuildNUnitLibraryProject",
			Description = "Create and build NUnit Library Project", Category="Smoke")]
		public void RunDotNetTests (string templateName, int totalTimeoutInSecs)
		{
			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = dotNetCategory,
				TemplateKindRoot = GeneralKindRoot,
				TemplateKind = templateName
			};
			CreateBuildProject (templateOptions, () => Ide.WaitForIdeIdle ((uint)totalTimeoutInSecs));
		}
	}
}
