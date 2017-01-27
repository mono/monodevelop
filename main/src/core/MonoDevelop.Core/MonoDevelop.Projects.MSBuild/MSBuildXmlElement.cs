//
// MSBuildXmlElement.cs
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace MonoDevelop.Projects.MSBuild
{

	class MSBuildXmlElement : MSBuildObject
	{
		string name;
		string ns;
		string prefix;

		static string [] knownAtts = { "xmlns" };

		public string Name {
			get {
				return name;
			}
		}

		internal void SetNamespace (string ns)
		{
			this.ns = ns;
		}

		internal override void Read (MSBuildXmlReader reader)
		{
			name = reader.LocalName;
			ns = reader.NamespaceURI;
			prefix = reader.Prefix;
			base.Read (reader);
		}

		internal override void ReadAttribute (string name, string value)
		{
			// Ignore xmlns
			if (name != "xmlns")
				base.ReadAttribute (name, value);
		}

		internal override string WriteAttribute (string name)
		{
			// Ignore xmlns since the xml writer automatically generates it
			if (name == "xmlns")
				return null;
			
			return base.WriteAttribute (name);
		}

		internal override bool SupportsNamespacePrefixes {
			get {
				return true;
			}
		}

		internal override bool SupportsTextContent {
			get {
				return true;
			}
		}

		public override string Namespace {
			get {
				if (ns != null)
					return ns;
				var parentElement = ParentNode as MSBuildXmlElement;
				return parentElement != null ? parentElement.ns : MSBuildProject.Schema;
			}
		}

		internal override string NamespacePrefix {
			get {
				return prefix;
			}
		}

		internal override string GetElementName ()
		{
			return name;
		}

		internal override string [] GetKnownAttributes ()
		{
			return knownAtts;
		}

		internal string GetInnerXml ()
		{
			if (StartInnerWhitespace == null && EndInnerWhitespace == null && ChildNodes.Count == 0)
				return string.Empty;

			var c = new WriteContext ();
			StringWriter sb = new StringWriter ();

			var xw = XmlWriter.Create (sb, new XmlWriterSettings {
				OmitXmlDeclaration = true,
				NewLineChars = ParentProject.TextFormat.NewLine,
				NewLineHandling = NewLineHandling.None
			});

			using (xw) {
				xw.WriteStartElement ("a");
				WriteContent (xw, c);
				xw.WriteEndElement ();
			}
			var s = sb.ToString ();
			int si = s.IndexOf ('>') + 1;
			int ei = s.LastIndexOf ('<');
			if (ei < si)
				return string.Empty;
			return s.Substring (si, ei - si);
		}

		internal string GetText ()
		{
			StringBuilder sb = new StringBuilder ();
			foreach (var c in ChildNodes) {
				if (c is MSBuildXmlTextNode || c is MSBuildXmlCDataNode)
					sb.Append (((MSBuildXmlValueNode)c).Value);
			}
			return sb.ToString ();
		}
	}
	
}
