// 
// TestParser.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;

using MonoDevelop.Xml.Completion;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Xml.Tests.Parser
{
	public class TestXmlParser : XmlParser
	{
		readonly List<Error> errors = new List<Error> ();
		
		public TestXmlParser (XmlRootState rootState) : this (rootState, false)
		{
		}
		
		public TestXmlParser (XmlRootState rootState, bool buildTree) : base (rootState, buildTree)
		{
			ErrorLogged += errors.Add;
		}
		
		public new List<Error> Errors { get { return errors; } }

		public static void AssertState (string doc, params Action<TestXmlParser>[] asserts)
		{
			var p = new TestXmlParser (new XmlRootState ());
			p.Parse (doc, Array.ConvertAll (asserts, a => (Action)(() => a (p))));
		}

		public static void AssertTree (string txt, params Action<XNode>[] asserts)
		{
			var p = new TestXmlParser (new XmlRootState (), true);

			//parse and capture line/col info
			var list = new List<DocumentLocation> ();
			p.Parse (txt, Array.ConvertAll (asserts, a => (Action)(() => list.Add (p.Location))));

			var doc = (XDocument) p.Nodes.Last ();

			for (int i = 0; i < asserts.Length; i++) {
				asserts [i] (doc.AllDescendentNodes.FirstOrDefault (n => n.Region.IsInside (list[i])));
			}
		}

		public void Parse (string doc, params Action[] asserts)
		{
			Parse (doc, '$', asserts);
		}
		
		public void Parse (string doc, char trigger, params Action[] asserts)
		{
			Assert.AreEqual (Position, 0);
			int assertNo = 0;
			for (int i = 0; i < doc.Length; i++) {
				char c = doc[i];
				if (c == trigger) {
					if (i + 1 < doc.Length && doc [i + 1] == trigger) {
						Push (c);
						i++;
						continue;
					}
					asserts[assertNo] ();
					assertNo++;
				} else {
					Push (c);
				}
			}
			Assert.AreEqual (asserts.Length, assertNo);
		}
		
		public string GetPath ()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			foreach (XObject obj in Nodes) {
				if (obj is XDocument) {
					sb.Insert (0, '/');
					break;
				}
				sb.Insert (0, obj.FriendlyPathRepresentation);
				sb.Insert (0, '/');
			}
			return sb.ToString ();
		}
		
		public void AssertPath (string path)
		{
			Assert.AreEqual (path, GetPath ());
		}
		
		public Action PathAssertion (string path)
		{
			return delegate {
				Assert.AreEqual (path, GetPath ());
			};
		}
		
		public void AssertStateIs<T> () where T : XmlParserState
		{
			Assert.IsTrue (CurrentState is T, "Current state is {0} not {1}", CurrentState.GetType ().Name, typeof (T).Name);
		}

		public void AssertStateIsNot<T> () where T : XmlParserState
		{
			Assert.IsFalse (CurrentState is T, "Current state is {0}", typeof (T).Name);
		}
		
		public void AssertNodeDepth (int depth)
		{
			Assert.AreEqual (depth, Nodes.Count, "Node depth is {0} not {1}", Nodes.Count, depth);
		}
		
		public void AssertNodeIs<T> (int down)
		{
			XObject n = Nodes.Peek (down);
			AssertNodeDepth (down);
			Assert.IsTrue (n is T, "Node down {0} is {1}, not {2}", down, n.GetType ().Name, typeof (T).Name);
		}
		
		public void AssertNodeIs<T> ()
		{
			XObject n = Nodes.Peek ();
			Assert.IsTrue (n is T, "Node is {0}, not {1}", n.GetType ().Name, typeof (T).Name);
		}
		
		public void AssertErrorCount (int count)
		{
			AssertErrorCount (count, x => true);
		}
		
		public void AssertErrorCount (int count, Func<Error,bool> filter)
		{
			string msg = null;
			int actualCount = errors.Count (filter);
			if (actualCount != count) {
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				foreach (Error err in errors)
					if (filter (err))
						sb.AppendFormat ("{0}@{1}: {2}\n", err.ErrorType, err.Region, err.Message);
				msg = sb.ToString ();
			}
			Assert.AreEqual (count, actualCount, msg);
		}

		public void AssertAttributes (params string[] nameValuePairs)
		{
			AssertNodeIs<IAttributedXObject> ();
			IAttributedXObject obj = (IAttributedXObject) Nodes.Peek ();
			AssertAttributes (obj, nameValuePairs);
		}

		public void AssertAttributes (IAttributedXObject obj, params string[] nameValuePairs)
		{
			if ((nameValuePairs.Length % 2) != 0)
				throw new ArgumentException ("nameValuePairs");

			int i = 0;
			foreach (XAttribute att in obj.Attributes) {
				Assert.IsTrue (i < nameValuePairs.Length);
				Assert.AreEqual (nameValuePairs[i], att.Name.FullName);
				Assert.AreEqual (nameValuePairs[i + 1], att.Value);
				i += 2;
			}
			Assert.AreEqual (nameValuePairs.Length, i);
		}
		
		public void AssertNoErrors ()
		{
			AssertErrorCount (0);
		}
		
		public void AssertEmpty ()
		{
			AssertNodeDepth (1);
			AssertNodeIs<XDocument> (); 
		}
		
		public void AssertName (string name)
		{
			AssertName (0, name);
		}
		
		public void AssertName (int down, string name)
		{
			XObject node = Nodes.Peek (down);
			Assert.IsTrue (node is INamedXObject);
			Assert.AreEqual (name, ((INamedXObject)node).Name.FullName);
		}

		public static void AssertPath (XNode node, params QualifiedName[] qualifiedNames)
		{
			var path = new List<XNode> ();
			while (node != null) {
				path.Add (node);
				node = node.Parent as XNode;
			}
			path.Reverse ();

			Assert.AreEqual (
				new XmlElementPath (qualifiedNames),
				XmlElementPath.Resolve (path.ToArray ())
			);
		}
	}
}
