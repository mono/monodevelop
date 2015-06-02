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
	public abstract class MSBuildObject
	{
		MSBuildProject project;
		MSBuildObject parentObject;
		UnknownAttribute[] unknownAttributes;
		string [] attributeOrder;

		class UnknownAttribute
		{
			public string LocalName;
			public string Prefix;
			public string Namespace;
			public string Value;
			public int Position;
		}

		internal object StartWhitespace { get; set; }
		internal object StartInnerWhitespace { get; set; }
		internal object EndInnerWhitespace { get; set; }
		internal object EndWhitespace { get; set; }

		internal virtual void Read (MSBuildXmlReader reader)
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
				}
				else if (reader.IsWhitespace) {
					reader.ReadAndStoreWhitespace ();
				}
				else
					reader.Read ();
			}
			reader.Read ();

			EndInnerWhitespace = reader.ConsumeWhitespace ();
		}

		internal virtual void Write (XmlWriter writer, WriteContext context)
		{
			MSBuildWhitespace.Write (StartWhitespace, writer);
			
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

			writer.WriteEndElement ();

			MSBuildWhitespace.Write (EndWhitespace, writer);
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
			reader.Skip ();
		}

		internal abstract string [] GetKnownAttributes ();

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
			if (ParentProject == null)
				return;
			
			var ps = GetPreviousSibling () as MSBuildObject;
			if (ps != null) {
				StartWhitespace = ps.StartWhitespace;
				if (closeInNewLine)
					EndInnerWhitespace = StartWhitespace;
			} else if (ParentObject != null) {
				if (ParentObject.StartInnerWhitespace == null) {
					ParentObject.StartInnerWhitespace = ParentProject.TextFormat.NewLine;
					ParentObject.EndInnerWhitespace = ParentObject.StartWhitespace;
				}
				StartWhitespace = ParentObject.StartWhitespace + "  ";
				if (closeInNewLine)
					EndInnerWhitespace = StartWhitespace;
			}
			EndWhitespace = ParentProject.TextFormat.NewLine;
			if (closeInNewLine)
				StartInnerWhitespace = ParentProject.TextFormat.NewLine;


			foreach (var c in GetChildren ())
				c.ResetIndent (false);
		}

		internal void RemoveIndent ()
		{
		}
	}
}
