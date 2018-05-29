//
// RoslynPreferencesTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Core;
using NUnit.Framework;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	[TestFixture]
	public class RoslynPreferencesTests : OptionsTestBase
	{
		[Test]
		public void TestRoslynPropertyMigration ()
		{
			var preferences = new RoslynPreferences ();

			const string mdKey = "TEST_ROSLYN_MIGRATION_KEY";

			foreach (var option in GetOptionKeys<bool> ()) {
				PropertyService.Set (mdKey, true);
				var prop = preferences.Wrap (option, default(bool), mdKey);

				Assert.AreEqual (true, PropertyService.Get<bool> (option.GetPropertyName ()));
				Assert.AreEqual (true, prop.Value);

				Assert.AreEqual (false, PropertyService.HasValue (mdKey));

				prop.Value = false;
				Assert.AreEqual (false, PropertyService.Get<bool> (option.GetPropertyName ()));
				Assert.AreEqual (false, prop.Value);
			}
		}
	}
}
