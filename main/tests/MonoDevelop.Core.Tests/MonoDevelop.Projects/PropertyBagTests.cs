//
// PropertyBagTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
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
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using NUnit.Framework;
using System.IO;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class PropertyBagTests
	{
		[Test]
		public void SerializationRoundtrip ()
		{
			var bag = new PropertyBag ();
			var t = new SerializableObject {
				SomeValue = "test1"
			};
			bag.SetValue ("foo", t);

			var w = new StringWriter ();
			var ser = new XmlDataSerializer (new DataContext ());
			ser.Serialize (w, bag);

			SerializableObject.CreationCount = 0;

			var data = w.ToString ();
			bag = ser.Deserialize<PropertyBag> (new StringReader (data));

			// SerializableObject is not instantiated if not queried
			Assert.AreEqual (0, SerializableObject.CreationCount);

			t = bag.GetValue<SerializableObject> ("foo");
			Assert.NotNull (t);
			Assert.AreEqual ("test1", t.SomeValue);
		}

		[Test]
		public void SerializationDoubleRoundtrip ()
		{
			var bag = new PropertyBag ();
			var t = new SerializableObject {
				SomeValue = "test1"
			};
			bag.SetValue ("foo", t);

			var w = new StringWriter ();
			var ser = new XmlDataSerializer (new DataContext ());
			ser.Serialize (w, bag);
			var data = w.ToString ();

			SerializableObject.CreationCount = 0;

			bag = ser.Deserialize<PropertyBag> (new StringReader (data));

			// SerializableObject is not instantiated if not queried
			Assert.AreEqual (0, SerializableObject.CreationCount);

			w = new StringWriter ();
			ser.Serialize (w, bag);
			data = w.ToString ();

			bag = ser.Deserialize<PropertyBag> (new StringReader (data));

			// SerializableObject is not instantiated if not queried
			Assert.AreEqual (0, SerializableObject.CreationCount);

			t = bag.GetValue<SerializableObject> ("foo");
			Assert.NotNull (t);
			Assert.AreEqual ("test1", t.SomeValue);
		}
	}


	class SerializableObject
	{
		public static int CreationCount = 0;

		public SerializableObject ()
		{
			CreationCount++;
		}

		[ItemProperty]
		public string SomeValue;
	}
}

