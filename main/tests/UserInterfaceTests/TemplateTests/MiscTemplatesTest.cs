//
// MiscTemplatesTest.cs
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
using System;
using NUnit.Framework;

namespace UserInterfaceTests
{
	[TestFixture]
	[Category("Misc")]
	public class MiscTemplatesTest : CreateBuildTemplatesTestBase
	{
		readonly string miscCategory = "Miscellaneous";

		readonly string genericKindRoot = "Generic";
		readonly string cCPlusKindRoot = "C/C++";

		#region Generic

		[Test]
		public void TestMiscGenericProject ()
		{
			RunMiscGenericTests ("Generic Project");
		}

		[Test]
		public void TestMiscPackagingProject ()
		{
			RunMiscGenericTests ("Packaging project");
		}

		void RunMiscGenericTests (string templateName)
		{
			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = miscCategory,
				TemplateKindRoot = genericKindRoot,
				TemplateKind = templateName
			};
			CreateBuildProject (templateOptions, EmptyAction);
		}

		#endregion

		#region C/C++

		[Test]
		public void TestMiscCCPlusSharedLibrary ()
		{
			RunCCPlusTests ("Shared Library");
		}

		[Test]
		public void TestMiscCCPlusStaticLibrary ()
		{
			RunCCPlusTests ("Static Library");
		}

		[Test]
		public void TestMiscCCPlusConsoleProject ()
		{
			RunCCPlusTests ("Console Project");
		}

		void RunCCPlusTests (string templateName)
		{
			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = miscCategory,
				TemplateKindRoot = cCPlusKindRoot,
				TemplateKind = templateName
			};
			CreateBuildProject (templateOptions, EmptyAction);
		}

		#endregion
	}
}
