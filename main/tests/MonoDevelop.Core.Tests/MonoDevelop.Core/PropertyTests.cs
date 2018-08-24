//
// PropertyTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using System.IO;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class PropertyTests
	{
		/// <summary>
		/// Ensures that a Properties instance used as a property value can be saved
		/// and re-loaded with the updated values.
		/// </summary>
		[Test]
		public void PropertyIsPropertiesInstance_SaveAndReload ()
		{
			var mainProperties = new Properties ();
			var properties = mainProperties.Get ("PropertyInstance", new Properties ());
			var propertyDefaultValue = properties.Get<string> ("First");
			properties.Set ("First", "Test");

			FilePath directory = Util.CreateTmpDir ("PropertyInstanceTest");
			var fileName = directory.Combine ("Properties.xml");
			mainProperties.Save (fileName);

			mainProperties = Properties.Load (fileName);
			var savedProperties = mainProperties.Get ("PropertyInstance", new Properties ());
			var savedFirstValue = savedProperties.Get<string> ("First");

			Assert.IsNull (propertyDefaultValue);
			Assert.AreEqual ("Test", savedFirstValue);
		}

		[Test]
		public void BoolAndStringProperties_SaveAndReload ()
		{
			var properties = new Properties ();
			var stringPropertyDefaultValue = properties.Get<string> ("FirstString");
			properties.Set ("FirstString", "Test");
			var defaultBoolValue = properties.Get ("FirstBool", true);
			properties.Set ("FirstBool", false);

			FilePath directory = Util.CreateTmpDir ("StringBoolPropertiesTests");
			var fileName = directory.Combine ("Properties.xml");
			properties.Save (fileName);

			properties = Properties.Load (fileName);
			var savedFirstStringValue = properties.Get<string> ("FirstString");
			var savedFirstBoolValue = properties.Get ("FirstBool", true);

			Assert.IsNull (stringPropertyDefaultValue);
			Assert.AreEqual ("Test", savedFirstStringValue);
			Assert.IsTrue (defaultBoolValue);
			Assert.IsFalse (savedFirstBoolValue);
		}

		[Test]
		public void PropertiesWithDefaultValues_SavedPropertiesFileDoesNotContainProperty ()
		{
			var mainProperties = new Properties ();
			var properties = mainProperties.Get ("PropertyInstance", new Properties ());
			var propertyDefaultValue = properties.Get<string> ("FirstProperties", "defaultValue");
			var stringPropertyDefaultValue = properties.Get<string> ("FirstString", "Test");
			var defaultBoolValue = properties.Get ("FirstBool", true);

			FilePath directory = Util.CreateTmpDir ("PropertyWithDefaultValues");
			var fileName = directory.Combine ("Properties.xml");
			mainProperties.Save (fileName);

			string text = File.ReadAllText (fileName);

			Assert.IsFalse (text.Contains ("First"), "Default properties should not be saved.");
		}
	}
}
