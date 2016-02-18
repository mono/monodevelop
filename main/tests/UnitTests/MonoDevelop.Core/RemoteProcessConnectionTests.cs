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
	}
}

