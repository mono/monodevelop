//
// BaseContractsTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using NUnit.Framework;
using System.Reflection;
using System.IO;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public class BaseContractsTests
	{
		readonly string AssemblyPath = Path.Combine ("..", "AddIns", "VersionControl", "MonoDevelop.VersionControl.dll");
		string[] AllowedTypes = {
			"MonoDevelop.VersionControl.UrlBasedRepositoryEditor",
			"MonoDevelop.VersionControl.Views.BlameWidget",
			"MonoDevelop.VersionControl.Views.MergeWidget",
		};

		void NoVisibleUI (Assembly asm)
		{
			var types = asm.ExportedTypes;
			var gtkType = typeof(Gtk.Widget);

			var uiTypes = types
				.Where (gtkType.IsAssignableFrom)
				.Where (t => t.CustomAttributes.All (attr => attr.AttributeType != typeof(System.ComponentModel.ToolboxItemAttribute)))
				.Where (t => !t.IsAbstract)
				.Where (t => !AllowedTypes.Contains (t.FullName))
				;
			Assert.True (!uiTypes.Any (), "Should not be public:\n" + String.Join ("\n", uiTypes.Select (t => t.FullName)));
		}

		[Test]
		public void TestNoVisibleUI ()
		{
			NoVisibleUI (Assembly.LoadFrom (AssemblyPath));
		}
	}
}

