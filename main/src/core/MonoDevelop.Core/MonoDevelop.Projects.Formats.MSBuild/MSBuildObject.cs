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
using System.Xml;


namespace MonoDevelop.Projects.Formats.MSBuild
{
	public abstract class MSBuildObject
	{
		string condition;
		MSBuildProject project;
		MSBuildObject parentObject;
		UnknownAttribute[] unknownAttributes;
		string [] attributeOrder;

		static readonly string [] knownAttributes = { "Condition", "Label" };

		class UnknownAttribute
		{
			public string LocalName;
			public string Prefix;
			public string Namespace;
			public string Value;
			public int Position;
		}

		internal string PreviousWhitespace { get; set; }
		internal string TailWhitespace { get; set; }

		internal virtual void Read (MSBuildXmlReader reader)
		{
			if (reader.ForEvaluation) {
				if (reader.MoveToFirstAttribute ()) {
					do {
						ReadAttribute (reader.LocalName, reader.Value);
					} while (reader.MoveToNextAttribute ());
				}
			} else {
				PreviousWhitespace = reader.CurrentWhitespace.ToString ();
				reader.CurrentWhitespace.Clear ();

				if (reader.MoveToFirstAttribute ()) {
					var knownAtts = GetKnownAttributes ();
					int pos = 0;
					int attOrderIndex = 0;
					int expectedKnownAttIndex = 0;
					bool attOrderIsUnexpected = false;
					List<UnknownAttribute> unknownAttsList = null;
					attributeOrder = new string [knownAtts.Length];
					do {
						int i = Array.IndexOf (knownAtts, reader.LocalName);
						if (i == -1) {
							var ua = new UnknownAttribute {
								LocalName = reader.LocalName,
								Prefix = reader.Prefix,
								Namespace = reader.NamespaceURI,
								Value = reader.Value,
								Position = pos
							};
							if (unknownAttsList == null)
								unknownAttsList = new List<UnknownAttribute> ();
							unknownAttsList.Add (ua);
						} else {
							attributeOrder [attOrderIndex++] = reader.LocalName;
							ReadAttribute (reader.LocalName, reader.Value);
							if (i < expectedKnownAttIndex) {
								// Attributes have an unexpected order
								attOrderIsUnexpected = true;
							}
							expectedKnownAttIndex = i + 1;
						}
					} while (reader.MoveToNextAttribute ());

					if (unknownAttsList != null)
						unknownAttributes = unknownAttsList.ToArray ();
					if (!attOrderIsUnexpected)
						attributeOrder = null;
				}
			}
			reader.MoveToElement ();

			ReadContent (reader);

			TailWhitespace = reader.CurrentWhitespace.ToString ();
			reader.CurrentWhitespace.Clear ();
		}

		internal virtual void ReadContent (MSBuildXmlReader reader)
		{
			if (reader.IsEmptyElement) {
				reader.Skip ();
				return;
			}
			reader.Read ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element)
					ReadChildElement (reader);
				else if (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace || reader.NodeType == XmlNodeType.Comment) {
					reader.CurrentWhitespace.Append (reader.Value);
					reader.Read ();
				}
				else
					reader.Read ();
			}
			reader.Read ();
		}

		internal virtual void Write (XmlWriter writer, WriteContext context)
		{
			if (PreviousWhitespace != null && PreviousWhitespace.Length > 0)
				writer.WriteString (PreviousWhitespace);
			
			writer.WriteStartElement (GetElementName (), MSBuildProject.Schema);
			if (unknownAttributes != null) {
				int pos = 0;
				int unknownIndex = 0;
				int knownIndex = 0;
				var knownAtts = attributeOrder ?? GetKnownAttributes ();
				do {
					if (unknownIndex < unknownAttributes.Length && pos == unknownAttributes [unknownIndex].Position) {
						var att = unknownAttributes [unknownIndex++];
						writer.WriteAttributeString (att.Prefix, att.LocalName, att.Namespace, att.Value);
					} else if (knownIndex < knownAtts.Length) {
						var aname = knownAtts [knownIndex++];
						var val = WriteAttribute (aname);
						if (val != null)
							writer.WriteAttributeString (aname, val);
					}
					pos++;
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

			if (TailWhitespace != null && TailWhitespace.Length > 0)
				writer.WriteString (TailWhitespace);
			writer.WriteEndElement ();
		}

		internal virtual void WriteContent (XmlWriter writer, WriteContext context)
		{
			foreach (var c in GetChildren ())
				c.Write (writer, context);
		}

		internal virtual void ReadAttribute (string name, string value)
		{
			switch (name) {
			case "Label": Label = value; break;
			case "Condition": Condition = value; break;
			}
		}

		internal virtual string WriteAttribute (string name)
		{
			switch (name) {
				case "Label": return Label != null && Label.Length > 0 ? Label : null;
				case "Condition": return Condition.Length > 0 ? Condition : null;
			}
			return null;
		}

		internal virtual void ReadChildElement (MSBuildXmlReader reader)
		{
			reader.Skip ();
		}

		internal virtual string [] GetKnownAttributes ()
		{
			return knownAttributes;
		}

		internal abstract string GetElementName ();

		internal virtual IEnumerable<MSBuildObject> GetChildren ()
		{
			yield break;
		}

		internal virtual MSBuildObject GetPreviousSibling ()
		{
			var p = ParentObject;
			if (p != null) {
				MSBuildObject last = null;
				foreach (var c in p.GetChildren ()) {
					if (c == this)
						return last;
					last = c;
				}
			}
			return null;
		}

		public string Label { get; set; }

		public string Condition {
			get {
				return condition ?? "";
			}
			set {
				condition = value;
			}
		}

		public MSBuildObject ParentObject {
			get {
				return parentObject;
			}
			internal set {
				parentObject = value;
				if (parentObject != null && parentObject.ParentProject != null)
					OnProjectSet ();
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

		internal void NotifyChanged ()
		{
			if (ParentProject != null)
				ParentProject.NotifyChanged ();
		}

		internal virtual void OnProjectSet ()
		{
		}

		internal void ResetIndent (bool closeInNewLine)
		{
//			if (Project != null)
//				XmlUtil.Indent (Project.TextFormat, this, closeInNewLine);
		}

		internal void RemoveIndent ()
		{
//			XmlUtil.RemoveElementAndIndenting (prop.Element);
		}

		internal virtual void Evaluate ()
		{
			
		}
	}

}
