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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;

using NUnit.Framework;

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public class TestParser : Parser
	{
		List<Error> errors = new List<Error> ();
		
		public TestParser (RootState rootState) : this (rootState, false)
		{
		}
		
		public TestParser (RootState rootState, bool buildTree) : base (rootState, buildTree)
		{
			base.ErrorLogged += delegate (Error err) { errors.Add (err); };
		}
		
		public new List<Error> Errors { get { return errors; } }
		
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
					asserts[assertNo] ();
					assertNo++;
				} else {
					Push (c);
				}
			}
			Assert.AreEqual (Position, doc.Length - assertNo);
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
		
		public void AssertStateIs<T> () where T : State
		{
			Assert.IsTrue (CurrentState is T);
		}
		
		public void AssertNodeDepth (int depth)
		{
			Assert.AreEqual (depth, Nodes.Count);
		}
		
		public void AssertNodeIs<T> (int down)
		{
			Assert.IsTrue (Nodes.Peek (down) is T);
		}
		
		public void AssertNodeIs<T> ()
		{
			Assert.IsTrue (Nodes.Peek () is T);
		}
		
		public void AssertErrorCount (int count)
		{
			Assert.AreEqual (count, errors.Count);
		}
		
		public void AssertNoErrors ()
		{
			Assert.AreEqual (0, errors.Count);
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
	}
}
