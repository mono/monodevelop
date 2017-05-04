//
// RemoteProcessConnectionTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core.Execution;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class RemoteProcessConnectionTests
	{
		[Test]
		public void BinaryMessageBasicSerialization ()
		{
			var m = new BinaryMessage ("Test");
			m.AddArgument ("string", "foo");
			m.AddArgument ("bool", true);
			m.AddArgument ("float", 44f);
			m.AddArgument ("double", 45d);
			m.AddArgument ("short", (short)46);
			m.AddArgument ("int", 47);
			m.AddArgument ("long", 48L);
			m.AddArgument ("byte", (byte)49);
			m.AddArgument ("DateTime", new DateTime (2016, 2, 17));
			m.AddArgument ("TimeSpan", TimeSpan.FromSeconds (44));

			MemoryStream ms = new MemoryStream ();
			m.Write (ms);
			ms.Position = 0;

			m = BinaryMessage.Read (ms);
			Assert.AreEqual (10, m.Args.Count);
			Assert.AreEqual ("foo", m.GetArgument ("string"));
			Assert.AreEqual (true, m.GetArgument ("bool"));
			Assert.AreEqual (44f, m.GetArgument ("float"));
			Assert.AreEqual (45d, m.GetArgument ("double"));
			Assert.AreEqual ((short)46, m.GetArgument ("short"));
			Assert.AreEqual (47, m.GetArgument ("int"));
			Assert.AreEqual (48L, m.GetArgument ("long"));
			Assert.AreEqual ((byte)49, m.GetArgument ("byte"));
			Assert.AreEqual (new DateTime (2016, 2, 17), m.GetArgument ("DateTime"));
			Assert.AreEqual (TimeSpan.FromSeconds (44), m.GetArgument ("TimeSpan"));
		}

		[Test]
		public void BinaryMessageArraySerialization ()
		{
			var m = new BinaryMessage ("Test");
			m.AddArgument ("string", new [] { "foo", "bar" });
			m.AddArgument ("bool", new [] { true, false });
			m.AddArgument ("float", new [] { 44f, 45f });
			m.AddArgument ("double", new [] { 45d, 46d });
			m.AddArgument ("short", new short [] { 46, 47 });
			m.AddArgument ("int", new int [] { 47, 48 });
			m.AddArgument ("long", new [] { 48L, 49L });
			m.AddArgument ("byte", new byte [] { 49, 50 });
			m.AddArgument ("DateTime", new [] { new DateTime (2016, 2, 17), new DateTime (2016, 2, 18) });
			m.AddArgument ("TimeSpan", new [] { TimeSpan.FromSeconds (44), TimeSpan.FromSeconds (45) });

			MemoryStream ms = new MemoryStream ();
			m.Write (ms);
			ms.Position = 0;

			m = BinaryMessage.Read (ms);
			Assert.AreEqual (10, m.Args.Count);
			Assert.AreEqual (new [] { "foo", "bar" }, m.GetArgument ("string"));
			Assert.AreEqual (new [] { true, false }, m.GetArgument ("bool"));
			Assert.AreEqual (new [] { 44f, 45f }, m.GetArgument ("float"));
			Assert.AreEqual (new [] { 45d, 46d }, m.GetArgument ("double"));
			Assert.AreEqual (new short [] { 46, 47 }, m.GetArgument ("short"));
			Assert.AreEqual (new int [] { 47, 48 }, m.GetArgument ("int"));
			Assert.AreEqual (new [] { 48L, 49L }, m.GetArgument ("long"));
			Assert.AreEqual (new byte [] { 49, 50 }, m.GetArgument ("byte"));
			Assert.AreEqual (new [] { new DateTime (2016, 2, 17), new DateTime (2016, 2, 18) }, m.GetArgument ("DateTime"));
			Assert.AreEqual (new [] { TimeSpan.FromSeconds (44), TimeSpan.FromSeconds (45) }, m.GetArgument ("TimeSpan"));
		}

		[Test]
		public void BinaryMessageMapSerialization ()
		{
			Dictionary<string, object> data1 = new Dictionary<string, object> ();
			data1 ["one"] = 1;
			data1 ["two"] = true;

			Dictionary<string, string> data2 = new Dictionary<string, string> ();
			data2 ["one"] = "uno";
			data2 ["two"] = "dos";

			Dictionary<int, double[]> data3 = new Dictionary<int, double[]> ();
			data3 [1] = new double[] { 11, 111 };
			data3 [2] = new double[] { 22, 222 };

			var m = new BinaryMessage ("Test");
			m.AddArgument ("map1", data1);
			m.AddArgument ("map2", data2);
			m.AddArgument ("map3", data3);

			MemoryStream ms = new MemoryStream ();
			m.Write (ms);
			ms.Position = 0;

			m = BinaryMessage.Read (ms);
			Assert.AreEqual (3, m.Args.Count);
			Assert.AreEqual (data1, m.GetArgument ("map1"));
			Assert.AreEqual (data2, m.GetArgument ("map2"));
			Assert.AreEqual (data3, m.GetArgument ("map3"));
		}

		[Test]
		public void BinaryMessageSerializeDictionaryWithNullStringValues ()
		{
			var data1 = new Dictionary<string, string> ();
			data1 ["one"] = "a";
			data1 ["two"] = null;

			var m = new BinaryMessage ("Test");
			m.AddArgument ("map1", data1);

			MemoryStream ms = new MemoryStream ();
			m.Write (ms);
			ms.Position = 0;

			m = BinaryMessage.Read (ms);
			var readData1 = m.GetArgument ("map1") as Dictionary<string, string>;
			Assert.AreEqual (1, m.Args.Count);
			Assert.AreEqual (data1, readData1);
		}

		[Test]
		public void BinaryMessageCustomMapSerialization ()
		{
			var data = new Dictionary<string, CustomData[]> ();
			data ["one"] = new CustomData [] { new CustomData { Data = "1" }, new CustomData { Data = "2" } };
			data ["two"] = new CustomData [] { new CustomData { Data = "3" }, new CustomData { Data = "4" } };

			var m = new BinaryMessage ("Test");
			m.AddArgument ("map", data);

			MemoryStream ms = new MemoryStream ();
			m.Write (ms);
			ms.Position = 0;

			m = BinaryMessage.Read (ms);
			Assert.AreEqual (1, m.Args.Count);

			var dict = m.GetArgument<Dictionary<string, CustomData[]>> ("map");

			Assert.AreEqual (2, dict.Count);
			Assert.IsTrue (dict.ContainsKey ("one"));
			Assert.IsTrue (dict.ContainsKey ("two"));

			var ar = dict ["one"];
			Assert.AreEqual (2, ar.Length);
			Assert.AreEqual ("1", ar[0].Data);
			Assert.AreEqual ("2", ar[1].Data);

			ar = dict ["two"];
			Assert.AreEqual (2, ar.Length);
			Assert.AreEqual ("3", ar[0].Data);
			Assert.AreEqual ("4", ar[1].Data);
		}

		[Test]
		public void EmptyMessage ()
		{
			var m = new BinaryMessage ("MonoDevelop.Projects.MSBuild.PingRequest");
			Assert.AreEqual (0, m.Args.Count);
			Assert.AreEqual ("MonoDevelop.Projects.MSBuild.PingRequest", m.Name);
			Assert.AreEqual ("", m.Target);

			MemoryStream ms = new MemoryStream ();
			m.Write (ms);
			ms.Position = 0;

			m = BinaryMessage.Read (ms);
			Assert.AreEqual (0, m.Args.Count);
			Assert.AreEqual ("MonoDevelop.Projects.MSBuild.PingRequest", m.Name);
			Assert.AreEqual ("", m.Target);
		}

		[Test]
		public void EmptyCustomMessage ()
		{
			var m = new PingRequest ();
			m.ReadCustomData ();

			Assert.AreEqual (0, m.Args.Count);
			Assert.AreEqual ("MonoDevelop.Core.PingRequest", m.Name);
			Assert.AreEqual (null, m.Target);

			MemoryStream ms = new MemoryStream ();
			m.Write (ms);
			ms.Position = 0;

			var rm = BinaryMessage.Read (ms);
			m = new PingRequest ();
			m.CopyFrom (rm);

			Assert.AreEqual (0, m.Args.Count);
			Assert.AreEqual ("MonoDevelop.Core.PingRequest", m.Name);
			Assert.AreEqual ("", m.Target);
		}
	}

	[MessageDataTypeAttribute]
	class PingRequest: BinaryMessage
	{
	}

	[MessageDataTypeAttribute]
	class CustomData
	{
		[MessageDataProperty]
		public string Data;
	}
}

