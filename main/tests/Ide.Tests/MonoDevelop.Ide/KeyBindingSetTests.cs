//
// KeyBindingSetTests.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2017 (c) Microsoft Corporation
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
using System.Text;
using System.Xml;
using MonoDevelop.Components.Commands;
using NUnit.Framework;

namespace MonoDevelop.Ide
{
	[TestFixture]
	public class KeyBindingSetTests
	{
		[Test]
		public void TestKeyBindingSetLoadXml ()
		{
			var xml = "<schemes version=\"1.0\">" +
				"<scheme name=\"Scheme1\">" +
				"    <binding command=\"Command1\" shortcut=\"Ctrl+1\" />" +
				"    <binding command=\"Command2\" shortcut=\"Ctrl+2 Alt+2\" />" +
				"</scheme>" +
				"</schemes>";

			var set1 = new KeyBindingSet ();

			using (var str = new StringReader (xml))
			using (var reader = new XmlTextReader (str))
					Assert.True (set1.LoadScheme (reader, "Scheme1"));

			var cmd1 = set1.GetBindings (new ActionCommand ("Command1", "Test Command 1"));
			Assert.AreEqual (1, cmd1.Length);
			Assert.AreEqual ("Ctrl+1", cmd1 [0]);

			var cmd2 = set1.GetBindings (new ActionCommand ("Command2", "Test Command 2"));
			Assert.AreEqual (2, cmd2.Length);
			Assert.AreEqual ("Ctrl+2", cmd2 [0]);
			Assert.AreEqual ("Alt+2", cmd2 [1]);
		}

		[Test]
		public void TestKeyBindingSetLoadXmlWithParent ()
		{
			var xml = "<schemes version=\"1.0\">" +
				"<scheme name=\"Scheme1\">" +
				"    <binding command=\"Command1\" shortcut=\"Alt+1\" />" +
				"    <binding command=\"Command2\" shortcut=\"\" />" +
				"</scheme>" +
				"</schemes>";

			var cmd1 = new ActionCommand ("Command1", "Test Command 1");
			var cmd2 = new ActionCommand ("Command2", "Test Command 2");
			var cmd3 = new ActionCommand ("Command3", "Test Command 2");
			var parent = new KeyBindingSet ();
			parent.SetBinding (cmd1, "Ctrl+1");
			parent.SetBinding (cmd2, "Ctrl+2");
			parent.SetBinding (cmd3, "Ctrl+3");

			var set1 = new KeyBindingSet (parent);
			Assert.AreEqual (new string [] { "Ctrl+1" }, set1.GetBindings (cmd1));
			Assert.AreEqual (new string [] { "Ctrl+2" }, set1.GetBindings (cmd2));

			using (var str = new StringReader (xml)) 
			using (var reader = new XmlTextReader (str))
				Assert.True (set1.LoadScheme (reader, "Scheme1"));

			// verify that the set overrides its parent
			Assert.AreEqual (new string [] { "Alt+1" }, set1.GetBindings (cmd1));
			Assert.AreEqual (new string [0], set1.GetBindings (cmd2));

			// cmd3 should be still there
			Assert.AreEqual (new string [] { "Ctrl+3" }, set1.GetBindings (cmd3));
		}

		[Test]
		public void TestKeyBindingSetSaveXml ()
		{
			var cmd1 = new ActionCommand ("Command1", "Test Command 1");
			var cmd2 = new ActionCommand ("Command2", "Test Command 2");
			var cmd3 = new ActionCommand ("Command3", "Test Command 3");
			var set1 = new KeyBindingSet ();
			set1.SetBinding (cmd1, "Ctrl+1");
			set1.SetBinding (cmd2, "Ctrl+2");
			set1.SetBinding (cmd3, "Ctrl+3");

			var sb = new StringBuilder ();
			using (var str = new StringWriter(sb))
			using (var writer = new XmlTextWriter (str) { Indentation = 0, Formatting = Formatting.None})
				set1.Save (writer, "Scheme1");

			var xml = sb.ToString ();

			var expectedXml = 
				"<scheme name=\"Scheme1\">" +
				"<binding command=\"Command1\" shortcut=\"Ctrl+1\" />" +
				"<binding command=\"Command2\" shortcut=\"Ctrl+2\" />" +
				"<binding command=\"Command3\" shortcut=\"Ctrl+3\" />" +
				"</scheme>";

			Assert.AreEqual (expectedXml, xml);

			// verify that empty bindings are not stored
			set1.SetBinding (cmd3, string.Empty);

			sb.Clear ();
			using (var str = new StringWriter (sb))
			using (var writer = new XmlTextWriter (str) { Indentation = 0, Formatting = Formatting.None })
				set1.Save (writer, "Scheme1");

			xml = sb.ToString ();
			expectedXml =
				 "<scheme name=\"Scheme1\">" +
				 "<binding command=\"Command1\" shortcut=\"Ctrl+1\" />" +
				 "<binding command=\"Command2\" shortcut=\"Ctrl+2\" />" +
				 "</scheme>";

			Assert.AreEqual (expectedXml, xml);
		}

		[Test]
		public void TestKeyBindingSetSaveXmlWithParent ()
		{
			var cmd1 = new ActionCommand ("Command1", "Test Command 1");
			var cmd2 = new ActionCommand ("Command2", "Test Command 2");
			var cmd3 = new ActionCommand ("Command3", "Test Command 3");
			var parent = new KeyBindingSet ();
			parent.SetBinding (cmd1, "Ctrl+1");
			parent.SetBinding (cmd2, "Ctrl+2");
			var set1 = new KeyBindingSet (parent);
			set1.SetBinding (cmd3, "Ctrl+3");

			var sb = new StringBuilder ();
			using (var str = new StringWriter (sb))
			using (var writer = new XmlTextWriter (str) { Indentation = 0, Formatting = Formatting.None })
				set1.Save (writer, "Scheme1");

			var xml = sb.ToString ();

			// parent bindings should be ignored
			var expectedXml =
				"<scheme name=\"Scheme1\">" +
				"<binding command=\"Command3\" shortcut=\"Ctrl+3\" />" +
				"</scheme>";

			Assert.AreEqual (expectedXml, xml);

			// verify that parent overrides are stored, especially empty ones to disable bindings
			set1.SetBinding (cmd1, "Alt+1");
			set1.SetBinding (cmd2, string.Empty);

			sb.Clear ();
			using (var str = new StringWriter (sb))
			using (var writer = new XmlTextWriter (str) { Indentation = 0, Formatting = Formatting.None })
				set1.Save (writer, "Scheme1");

			xml = sb.ToString ();
			expectedXml =
				"<scheme name=\"Scheme1\">" +
				"<binding command=\"Command3\" shortcut=\"Ctrl+3\" />" +
				"<binding command=\"Command1\" shortcut=\"Alt+1\" />" +
				"<binding command=\"Command2\" shortcut=\"\" />" +
				"</scheme>";

			Assert.AreEqual (expectedXml, xml);
		}

		[Test]
		public void TestKeyBindingSetWithParent ()
		{
			var cmd1 = new ActionCommand ("Command1", "Test Command 1");
			var cmd2 = new ActionCommand ("Command2", "Test Command 2");
			var cmd3 = new ActionCommand ("Command3", "Test Command 3");

			var set1 = new KeyBindingSet ();
			set1.SetBinding (cmd1, "Ctrl+1");
			set1.SetBinding (cmd2, "Ctrl+2");

			var set2 = new KeyBindingSet (set1);
			set2.SetBinding (cmd2, "Alt+2");
			set2.SetBinding (cmd3, "Ctrl+3");

			// verify that set1 has only cmd1 and cmd2 bindings
			Assert.AreEqual (new string [] { "Ctrl+1" }, set1.GetBindings (cmd1));
			Assert.AreEqual (new string [] { "Ctrl+2" }, set1.GetBindings (cmd2));
			Assert.AreEqual (new string [0], set1.GetBindings (cmd3));

			// verify that set2 has cmd1 binding from set1
			Assert.AreEqual (set1.GetBindings (cmd1), set2.GetBindings (cmd1));

			// verify that set 2 overrides cmd2
			Assert.AreNotEqual (set1.GetBindings (cmd2), set2.GetBindings (cmd2));
			Assert.AreEqual (new string [] { "Alt+2" }, set2.GetBindings (cmd2));

			// verify that set2 has cmd3 binding
			Assert.AreEqual (new string [] { "Ctrl+3" }, set2.GetBindings (cmd3));
		}

		[Test]
		public void TestKeyBindingSetEquals1 ()
		{
			var cmd1 = new ActionCommand ("Command1", "Test Command 1");
			var cmd2 = new ActionCommand ("Command2", "Test Command 2");
			var cmd3 = new ActionCommand ("Command3", "Test Command 3");

			var set1 = new KeyBindingSet ();
			set1.SetBinding (cmd1, "Ctrl+1");
			set1.SetBinding (cmd2, "Ctrl+2");

			var set2 = new KeyBindingSet ();
			set2.SetBinding (cmd1, "Ctrl+1");
			set2.SetBinding (cmd2, "Ctrl+2");

			Assert.IsTrue (set1.Equals (set2));
			Assert.IsTrue (set2.Equals (set1));

			set2.SetBinding (cmd3, "Ctrl+3");

			Assert.IsFalse (set2.Equals (set1));
			//UNDONE: a set equals an other set if its a subset, this may/shold change in the future
			Assert.IsTrue (set1.Equals (set2));
		}

		[Test]
		public void TestKeyBindingSetEquals2 ()
		{
			var cmd1 = new ActionCommand ("Command1", "Test Command 1");
			var cmd2 = new ActionCommand ("Command2", "Test Command 2");

			var set1 = new KeyBindingSet ();
			set1.SetBinding (cmd1, "Ctrl+1");

			var set2 = new KeyBindingSet ();
			set2.SetBinding (cmd1, "Ctrl+1");

			// add an empty binding to set2 which shouldn't change equality (bug #57111)
			set2.SetBinding (cmd2, string.Empty);

			Assert.IsFalse (set2.Equals (set1));
			//UNDONE: a set equals an other set if its a subset, this may/shold change in the future
			Assert.IsTrue (set1.Equals (set2));
		}

		[Test]
		public void TestKeyBindingSetEqualsParent ()
		{
			var cmd1 = new ActionCommand ("Command1", "Test Command 1");
			var cmd2 = new ActionCommand ("Command2", "Test Command 2");
			var cmd3 = new ActionCommand ("Command3", "Test Command 3");
			var parent = new KeyBindingSet ();
			parent.SetBinding (cmd1, "Ctrl+1");

			var set2 = new KeyBindingSet (parent);
			var set3 = new KeyBindingSet (parent);

			//UNDONE: this is not a full IEquatable<KeyBindingSet> implementation
			// an empty set must be equal to its parent
			Assert.IsTrue (set2.Equals (parent));

			set2.SetBinding (cmd2, "Ctrl+2");
			set3.SetBinding (cmd2, "Ctrl+2");

			// sets sharing the same parent and declaring same bindings are equal
			Assert.IsTrue (set3.Equals (set2));
			Assert.IsTrue (set2.Equals (set3));

			// set defines an additional binding, hence not equal to its parent
			Assert.IsFalse (set3.Equals (parent));

			// sets with different binding and same parent are not equal
			set3.SetBinding (cmd2, "Alt+2");
			Assert.IsFalse (set3.Equals (set2));
		}
	}
}
