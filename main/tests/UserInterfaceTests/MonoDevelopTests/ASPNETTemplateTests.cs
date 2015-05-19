//
// ASPNetTemplatesTest.cs
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
	[Category("ASP")]
	public class ASPNetTemplatesTest : CreateBuildTemplatesTestBase
	{
		readonly string aspCategory = "ASP.NET";

		public ASPNetTemplatesTest () : base (Util.TestRunId) {}

		[Test]
		public void TestEmptyASPMVCProject ()
		{
			RunASPTest ("Empty ASP.NET MVC Project", WaitForPackageUpdate);
		}

		[Test]
		public void TestEmptyASPProject ()
		{
			RunASPTest ("Empty ASP.NET Project", EmptyAction);
		}

		[Test]
		public void TestASPMVCProject ()
		{
			RunASPTest ("ASP.NET MVC Project", WaitForPackageUpdate);
		}

		[Test]
		public void TestASPMVCProjectWithUnitTests ()
		{
			RunASPTest ("ASP.NET MVC Project with Unit Tests", WaitForPackageUpdate);
		}

		[Test]
		public void TestASPMVCMazorProject ()
		{
			RunASPTest ("ASP.NET MVC Razor Project", WaitForPackageUpdate);
		}

		[Test]
		public void TestASPMVCMazorProjectWithUnitTests ()
		{
			RunASPTest ("ASP.NET MVC Razor Project with Unit Tests", WaitForPackageUpdate);
		}

		[Test]
		public void TestASPProject ()
		{
			RunASPTest ("ASP.NET Project", EmptyAction);
		}

		void RunASPTest (string templateName, Action beforeBuild)
		{
			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = aspCategory,
				TemplateKindRoot = GeneralKindRoot,
				TemplateKind = templateName
			};
			CreateBuildProject (templateOptions, beforeBuild);
		}
	}
}
