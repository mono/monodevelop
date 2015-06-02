//
// MSBuildXmlReader.cs
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
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects.Utility;
using System.Linq;
using MonoDevelop.Projects.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.MSBuild
{

	class MSBuildXmlReader
	{
		public bool ForEvaluation;

		object currentWhitespace;

		public MSBuildXmlReader ()
		{
		}

		public object ConsumeWhitespace ()
		{
			var ws = MSBuildWhitespace.GenerateStrings (currentWhitespace);
			currentWhitespace = null;
			return ws;
		}

		public object ConsumeWhitespaceUntilNewLine ()
		{
			return MSBuildWhitespace.ConsumeUntilNewLine (ref currentWhitespace);
		}

		public void ReadAndStoreWhitespace ()
		{
			currentWhitespace = MSBuildWhitespace.Append (currentWhitespace, XmlReader);
			Read ();
		}

		public bool EOF {
			get {
				return XmlReader.EOF;
			}
		}

		public bool IsEmptyElement {
			get {
				return XmlReader.IsEmptyElement;
			}
		}

		public bool IsWhitespace {
			get {
				return MSBuildWhitespace.IsWhitespace (XmlReader);
			}
		}

		public string LocalName {
			get {
				return XmlReader.LocalName;
			}
		}

		public string NamespaceURI {
			get {
				return XmlReader.NamespaceURI;
			}
		}

		public XmlNodeType NodeType {
			get {
				return XmlReader.NodeType;
			}
		}

		public string Prefix {
			get {
				return XmlReader.Prefix;
			}
		}

		public string Value {
			get {
				return XmlReader.Value;
			}
		}

		public XmlReader XmlReader { get; internal set; }

		internal string GetAttribute (string v)
		{
			return XmlReader.GetAttribute (v);
		}

		internal void MoveToElement ()
		{
			XmlReader.MoveToElement ();
		}

		internal bool MoveToFirstAttribute ()
		{
			return XmlReader.MoveToFirstAttribute ();
		}

		internal bool MoveToNextAttribute ()
		{
			return XmlReader.MoveToNextAttribute ();
		}

		internal bool Read ()
		{
			return XmlReader.Read ();
		}

		internal void Skip ()
		{
			XmlReader.Skip ();
		}
	}
	
}
