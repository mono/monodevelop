//
// MSBuildObject.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using System.Xml;


namespace MonoDevelop.Projects.Formats.MSBuild
{
	public abstract class MSBuildObject: MSBuildNode
	{
		UnknownAttribute[] unknownAttributes;
		string [] attributeOrder;
		List<MSBuildNode> children;

		class UnknownAttribute
		{
			public string LocalName;
			public string Prefix;
			public string Namespace;
			public string Value;
			public string AfterAttribute;
		}

		internal object StartInnerWhitespace { get; set; }
		internal object EndInnerWhitespace { get; set; }

		internal override void Read (MSBuildXmlReader reader)
		{
			if (reader.ForEvaluation) {
				if (reader.MoveToFirstAttribute ()) {
					do {
						ReadAttribute (reader.LocalName, reader.Value);
					} while (reader.MoveToNextAttribute ());
				}
			} else {
				StartWhitespace = reader.ConsumeWhitespace ();

				if (reader.MoveToFirstAttribute ()) {
					var knownAtts = GetKnownAttributes ();
					int attOrderIndex = 0;
					int expectedKnownAttIndex = 0;
					bool attOrderIsUnexpected = false;
					List<UnknownAttribute> unknownAttsList = null;
					attributeOrder = new string [knownAtts.Length];
					string lastAttr = null;
					do {
						var attName = reader.LocalName;
						int i = Array.IndexOf (knownAtts, attName);
						if (i == -1) {
							var ua = new UnknownAttribute {
								LocalName = attName,
								Prefix = reader.Prefix,
								Namespace = reader.NamespaceURI,
								Value = reader.Value,
								AfterAttribute = lastAttr
							};
							if (unknownAttsList == null)
								unknownAttsList = new List<UnknownAttribute> ();
							unknownAttsList.Add (ua);
							lastAttr = null;
						} else {
							if (attOrderIndex >= attributeOrder.Length)
								throw new InvalidOperationException ("Attribute specified twice");
							attributeOrder [attOrderIndex++] = attName;
							ReadAttribute (attName, reader.Value);
							if (i < expectedKnownAttIndex) {
								// Attributes have an unexpected order
								attOrderIsUnexpected = true;
							}
							expectedKnownAttIndex = i + 1;
							lastAttr = attName;
						}
					} while (reader.MoveToNextAttribute ());

					if (unknownAttsList != null)
						unknownAttributes = unknownAttsList.ToArray ();
					if (!attOrderIsUnexpected)
						attributeOrder = null;
					else {
						// Fill the remaning slots in the attributeOrder array (known attributes that were not read)
						foreach (var a in knownAtts) {
							if (!attributeOrder.Contains (a)) {
								if (attOrderIndex >= attributeOrder.Length)
									throw new InvalidOperationException ("Attribute specified twice");
								attributeOrder [attOrderIndex++] = a;
							}
						}
					}
				}
			} 
			reader.MoveToElement ();

			ReadContent (reader);

			while (reader.IsWhitespace)
				reader.ReadAndStoreWhitespace ();

			EndWhitespace = reader.ConsumeWhitespaceUntilNewLine ();
		}

		internal virtual void ReadContent (MSBuildXmlReader reader)
		{
			if (reader.IsEmptyElement) {
				reader.Skip ();
				return;
			}
			reader.Read ();
			bool childFound = false;

			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					if (!childFound) {
						childFound = true;
						StartInnerWhitespace = reader.ConsumeWhitespaceUntilNewLine ();
					}
					ReadChildElement (reader);
				} else if (reader.NodeType == XmlNodeType.Text) {
					var tn = new MSBuildXmlTextNode ();
					tn.Read (reader);
					ChildNodes.Add (tn);
				} else if (reader.NodeType == XmlNodeType.CDATA) {
					var tn = new MSBuildXmlCDataNode ();
					tn.Read (reader);
					ChildNodes.Add (tn);
				} else if (reader.NodeType == XmlNodeType.Comment) {
					var tn = new MSBuildXmlCommentNode ();
					tn.Read (reader);
					ChildNodes.Add (tn);
				} else if (reader.IsWhitespace) {
					reader.ReadAndStoreWhitespace ();
				} else if (reader.EOF)
					throw new InvalidOperationException ("Invalid XML");
				else
					reader.Read ();
			}
			reader.Read ();

			EndInnerWhitespace = reader.ConsumeWhitespace ();
		}

		internal override void Write (XmlWriter writer, WriteContext context)
		{
			MSBuildWhitespace.Write (StartWhitespace, writer);
			
			writer.WriteStartElement (GetElementName (), Namespace);

			if (unknownAttributes != null) {
				int unknownIndex = 0;
				int knownIndex = 0;
				var knownAtts = attributeOrder ?? GetKnownAttributes ();
				string lastAttr = null;
				do {
					if (unknownIndex < unknownAttributes.Length && (lastAttr == unknownAttributes [unknownIndex].AfterAttribute || unknownAttributes [unknownIndex].AfterAttribute == null)) {
						var att = unknownAttributes [unknownIndex++];
						writer.WriteAttributeString (att.Prefix, att.LocalName, att.Namespace, att.Value);
						lastAttr = att.LocalName;
					} else if (knownIndex < knownAtts.Length) {
						var aname = knownAtts [knownIndex++];
						lastAttr = aname;
						var val = WriteAttribute (aname);
						if (val != null)
							writer.WriteAttributeString (aname, val);
					} else
						lastAttr = null;
				} while (unknownIndex < unknownAttributes.Length || knownIndex < knownAtts.Length);
			} else {
				var knownAtts = attributeOrder ?? GetKnownAttributes ();
				for (int i = 0; i < knownAtts.Length; i++) {
					var aname = knownAtts [i];
					var val = WriteAttribute (aname);
					if (val != null)
						writer.WriteAttributeString (aname, val);
				}
			}

			WriteContent (writer, context);

			writer.WriteEndElement ();

			MSBuildWhitespace.Write (EndWhitespace, writer);
		}

		internal virtual string Namespace
		{
			get
			{
				return MSBuildProject.Schema;
			}
		}

		internal virtual void WriteContent (XmlWriter writer, WriteContext context)
		{
			MSBuildWhitespace.Write (StartInnerWhitespace, writer);

			foreach (var c in GetChildren ()) {
				c.Write (writer, context);
			}

			MSBuildWhitespace.Write (EndInnerWhitespace, writer);
		}

		internal virtual void ReadAttribute (string name, string value)
		{
		}

		internal virtual string WriteAttribute (string name)
		{
			return null;
		}

		internal virtual void ReadChildElement (MSBuildXmlReader reader)
		{
			if (reader.ForEvaluation)
				reader.Skip ();
			else {
				var n = new MSBuildXmlElement ();
				n.Read (reader);
				n.ParentNode = this;
				ChildNodes.Add (n);
			}
		}

		internal void RemoveUnknownAttribute (string name)
		{
			if (unknownAttributes == null)
				return;
			var list = new List<UnknownAttribute> (unknownAttributes);
			int i = list.FindIndex (a => a.LocalName == name);
			if (i != -1) {
				list.RemoveAt (i);
				unknownAttributes = list.ToArray ();
			}
		}

		internal string GetUnknownAttribute (string name)
		{
			if (unknownAttributes == null)
				return null;
			var at = unknownAttributes.FirstOrDefault (a => a.LocalName == name);
			if (at != null)
				return at.Value;
			else
				return null;
		}

		internal abstract string [] GetKnownAttributes ();

		internal abstract string GetElementName ();

		internal List<MSBuildNode> ChildNodes {
			get {
				if (children == null)
					children = new List<MSBuildNode> ();
				return children;
			}
		}

		internal override IEnumerable<MSBuildNode> GetChildren ()
		{
			return children != null ? children : Enumerable.Empty<MSBuildNode> ();
		}

		internal void ResetIndent (bool closeInNewLine)
		{
			if (ParentProject == null)
				return;
			
			ResetIndent (closeInNewLine, ParentProject, ParentObject, GetPreviousSibling ());
		}

		internal void ResetIndent (bool closeInNewLine, MSBuildProject project, MSBuildObject parent, MSBuildNode previousSibling)
		{
			StartInnerWhitespace = StartWhitespace = EndWhitespace = EndInnerWhitespace = null;

			if (previousSibling != null) {
				StartWhitespace = previousSibling.StartWhitespace;
				if (closeInNewLine)
					EndInnerWhitespace = StartWhitespace;
			} else if (parent != null) {
				if (parent.StartInnerWhitespace == null) {
					parent.StartInnerWhitespace = project.TextFormat.NewLine;
					parent.EndInnerWhitespace = parent.StartWhitespace;
				}
				StartWhitespace = parent.StartWhitespace + "  ";
				if (closeInNewLine)
					EndInnerWhitespace = StartWhitespace;
			}
			EndWhitespace = project.TextFormat.NewLine;
			if (closeInNewLine)
				StartInnerWhitespace = project.TextFormat.NewLine;

			ResetChildrenIndent ();
		}

		internal void ResetChildrenIndent ()
		{
			foreach (var c in GetChildren ().OfType<MSBuildObject> ())
				c.ResetIndent (false);
		}

		internal void RemoveIndent ()
		{
		}
	}
}
