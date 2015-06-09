//
// MSBuildXmlNode.cs
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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildNode
	{
		public MSBuildNode ()
		{
		}

		MSBuildProject project;
		MSBuildNode parentObject;

		internal object StartWhitespace { get; set; }
		internal object EndWhitespace { get; set; }

		internal virtual void Read (MSBuildXmlReader reader)
		{
		}

		internal virtual void Write (XmlWriter writer, WriteContext context)
		{
		}

		internal MSBuildNode ParentNode {
			get {
				return parentObject;
			}
			set {
				parentObject = value;
				if (parentObject != null && parentObject.ParentProject != null)
					OnProjectSet ();
			}
		}

		public MSBuildObject ParentObject {
			get {
				if (ParentNode == null)
					return null;
				return ParentNode is MSBuildObject ? (MSBuildObject) ParentNode : ParentNode.ParentObject;
			}
		}

		public MSBuildProject ParentProject {
			get {
				return project ?? (ParentObject != null ? ParentObject.ParentProject : null);
			}
			internal set {
				project = value;
				OnProjectSet ();
			}
		}

		internal virtual IEnumerable<MSBuildNode> GetChildren ()
		{
			yield break;
		}

		internal virtual MSBuildNode GetPreviousSibling ()
		{
			var p = ParentObject;
			if (p != null) {
				MSBuildNode last = null;
				foreach (var c in p.GetChildren ()) {
					if (c == this)
						return last;
					last = c;
				}
			}
			return null;
		}

		internal void NotifyChanged ()
		{
			if (ParentProject != null)
				ParentProject.NotifyChanged ();
		}

		internal virtual void OnProjectSet ()
		{
		}
	}

	class MSBuildXmlElement : MSBuildObject
	{
		string name;
		string ns;

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
			if (name == "xmlns") {
			/*	var parentElement = ParentNode as MSBuildXmlElement;
				var parentNs = parentElement != null ? parentElement.ns : MSBuildProject.Schema;
				if (parentNs != ns)
					return ns;
				else*/
					return null;
			}
			return base.WriteAttribute (name);
		}

		internal override string Namespace
		{
			get
			{
				var parentElement = ParentNode as MSBuildXmlElement;
				var parentNs = parentElement != null ? parentElement.ns : MSBuildProject.Schema;
				if (parentNs != ns)
					return ns;
				else
					return MSBuildProject.Schema;
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
			using (var xw = new XmlTextWriter (sb)) {
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

	class MSBuildXmlValueNode: MSBuildNode
	{
		public string Value { get; set; }

		internal override void Read (MSBuildXmlReader reader)
		{
			StartWhitespace = reader.ConsumeWhitespace ();
			Value = reader.Value;
			reader.Read ();

			while (reader.IsWhitespace)
				reader.ReadAndStoreWhitespace ();

			EndWhitespace = reader.ConsumeWhitespaceUntilNewLine ();
		}
	}

	class MSBuildXmlTextNode: MSBuildXmlValueNode
	{
		internal override void Write (XmlWriter writer, WriteContext context)
		{
			MSBuildWhitespace.Write (StartWhitespace, writer);
			writer.WriteString (Value);
			MSBuildWhitespace.Write (EndWhitespace, writer);
		}
	}

	class MSBuildXmlCDataNode : MSBuildXmlValueNode
	{
		internal override void Write (XmlWriter writer, WriteContext context)
		{
			MSBuildWhitespace.Write (StartWhitespace, writer);
			writer.WriteCData (Value);
			MSBuildWhitespace.Write (EndWhitespace, writer);
		}
	}

	class MSBuildXmlCommentNode: MSBuildXmlValueNode
	{
		internal override void Write (XmlWriter writer, WriteContext context)
		{
			MSBuildWhitespace.Write (StartWhitespace, writer);
			writer.WriteComment (Value);
			MSBuildWhitespace.Write (EndWhitespace, writer);
		}
	}
}

