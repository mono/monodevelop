// 
// Node.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core;

namespace MonoDevelop.DocFood
{
	abstract class Node
	{
		public List<KeyValuePair<string, string>> Attributes {
			get;
			private set;
		}
		
		public List<Node> Children = new List<Node> ();
		
		public Node ()
		{
			Attributes = new List<KeyValuePair<string, string>> ();
		}
		
		public abstract void Run (DocGenerator generator, object member);
		
		public void SetAttribute (string name, string value)
		{
			Attributes.Add (new KeyValuePair<string, string> (name, value));
		}
		
		public abstract void Write (XmlWriter writer);
		public static void WriteNodeList (XmlWriter writer, IEnumerable<Node> nodes)
		{
			if (nodes == null)
				return;
			foreach (var node in nodes) {
				node.Write (writer);
			}
		}
		
		public static List<Node> ReadNodeList (XmlReader reader, string tag)
		{
			List<Node> result = new List<Node> ();
			XmlReadHelper.ReadList (reader, tag, delegate () {
				switch (reader.LocalName) {
				case Section.XmlTag:
					result.Add (Section.Read (reader));
					return true;
				case IfStatement.XmlTag:
					result.Add (IfStatement.Read (reader));
					return true;
				case IfNotStatement.XmlTag:
					result.Add (IfNotStatement.Read (reader));
					return true;
				case SwitchStatement.XmlTag:
					result.Add (SwitchStatement.Read (reader));
					return true;
				}
				return false;
			});
			return result;
		}
	}
}

