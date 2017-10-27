//
// XmlTextWriter.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc.

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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Formatting
{
	internal class XmlFormatterWriter : XmlWriter
	{
		// Static/constant members.

		const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
		const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

		static readonly Encoding unmarked_utf8encoding =
			new UTF8Encoding (false, false);
		static char [] escaped_text_chars;
		static char [] escaped_attr_chars;

		// Internal classes

		class XmlNodeInfo
		{
			public string Prefix;
			public string LocalName;
			public string NS;
			public bool HasSimple;
			public bool HasElements;
			public string XmlLang;
			public XmlSpace XmlSpace;
		}

		internal class StringUtil
		{
			static CultureInfo cul = CultureInfo.InvariantCulture;
			static CompareInfo cmp =
				CultureInfo.InvariantCulture.CompareInfo;

			public static int IndexOf (string src, string target)
			{
				return cmp.IndexOf (src, target);
			}

			public static int Compare (string s1, string s2)
			{
				return cmp.Compare (s1, s2);
			}

			public static string Format (
				string format, params object [] args)
			{
				return String.Format (cul, format, args);
			}
		}

		enum XmlDeclState {
			Allow,
			Ignore,
			Auto,
			Prohibit,
		}

		// Instance fields

		Stream base_stream;
		TextWriter source; // the input TextWriter to .ctor().
		TextWriterWrapper writer;
		// It is used for storing xml:space, xml:lang and xmlns values.
		StringWriter preserver;
		string preserved_name;
		bool is_preserved_xmlns;

		bool allow_doc_fragment;
		bool close_output_stream = true;
		bool ignore_encoding;
		bool namespaces = true;
		XmlDeclState xmldecl_state = XmlDeclState.Allow;

		bool check_character_validity;
		NewLineHandling newline_handling = NewLineHandling.None;

		bool is_document_entity;
		WriteState state = WriteState.Start;
		XmlNodeType node_state = XmlNodeType.None;
		XmlNamespaceManager nsmanager;
		int open_count;
		XmlNodeInfo [] elements = new XmlNodeInfo [10];
		Stack new_local_namespaces = new Stack ();
		ArrayList explicit_nsdecls = new ArrayList ();

		string newline;
		bool v2;
		int lastEmptyLineCount;
		
		XmlFormattingSettings formatSettings = new XmlFormattingSettings ();
		XmlFormattingSettings defaultFormatSettings = new XmlFormattingSettings ();
		internal TextStylePolicy TextPolicy;
		
		// Constructors

		public XmlFormatterWriter (string filename, Encoding encoding)
			: this (new FileStream (filename, FileMode.Create, FileAccess.Write, FileShare.None), encoding)
		{
		}

		public XmlFormatterWriter (Stream stream, Encoding encoding)
			: this (new StreamWriter (stream,
				encoding == null ? unmarked_utf8encoding : encoding))
		{
			ignore_encoding = (encoding == null);
			Initialize (writer);
			allow_doc_fragment = true;
		}

		public XmlFormatterWriter (TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException ("writer");
			ignore_encoding = (writer.Encoding == null);
			Initialize (writer);
			allow_doc_fragment = true;
			xmldecl_state = formatSettings.OmitXmlDeclaration ? XmlDeclState.Ignore : XmlDeclState.Allow;
			check_character_validity = false;
			v2 = true;
		}

		void Initialize (TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException ("writer");
			XmlNameTable name_table = new NameTable ();
			this.writer = new TextWriterWrapper (writer, this);
			if (writer is StreamWriter)
				base_stream = ((StreamWriter) writer).BaseStream;
			source = writer;
			nsmanager = new XmlNamespaceManager (name_table);

			escaped_text_chars =
				newline_handling != NewLineHandling.None ?
				new char [] {'&', '<', '>', '\r', '\n'} :
				new char [] {'&', '<', '>'};
			escaped_attr_chars =
				new char [] {'"', '&', '<', '>', '\r', '\n'};
		}
		
		Dictionary<XmlNode,XmlFormattingSettings> formatMap = new Dictionary<XmlNode, XmlFormattingSettings> ();
		
		public void WriteNode (XmlNode node, XmlFormattingPolicy formattingPolicy, TextStylePolicy textPolicy)
		{
			this.TextPolicy = textPolicy;
			newline = TextPolicy.GetEolMarker ();
			formatMap.Clear ();
			defaultFormatSettings = formattingPolicy.DefaultFormat;
			foreach (XmlFormattingSettings format in formattingPolicy.Formats) {
				foreach (string xpath in format.ScopeXPath) {
					foreach (XmlNode n in node.SelectNodes (xpath))
						formatMap [n] = format;
				}
			}
			WriteNode (node);
		}
		
		void WriteNode (XmlNode node)
		{
			XmlFormattingSettings oldFormat = formatSettings;
			SetFormat (node);
			
			switch (node.NodeType) {
				case XmlNodeType.Document: {
					if (!defaultFormatSettings.OmitXmlDeclaration)
						WriteDeclarationIfMissing ((XmlDocument)node);
					WriteContent (node);
					break;
				}
				case XmlNodeType.Attribute: {
					XmlAttribute at = (XmlAttribute) node;
					if (at.Specified) {
						WriteStartAttribute (at.NamespaceURI.Length > 0 ? at.Prefix : String.Empty, at.LocalName, at.NamespaceURI);
						WriteContent (node);
						WriteEndAttribute ();
					}
					break;
				}
				case XmlNodeType.CDATA: {
					WriteCData (((XmlCDataSection)node).Data);
					break;
				}
				case XmlNodeType.Comment: {
					WriteComment (((XmlComment)node).Data);
					break;
				}
				case XmlNodeType.DocumentFragment: {
					for (int i = 0; i < node.ChildNodes.Count; i++)
						WriteNode (node.ChildNodes [i]);
					break;
				}
				case XmlNodeType.DocumentType: {
					XmlDocumentType dt = (XmlDocumentType) node;
					WriteDocType (dt.Name, dt.PublicId, dt.SystemId, dt.InternalSubset);
					break;
				}
				case XmlNodeType.Element: {
					XmlElement elem = (XmlElement) node;
					writer.AttributesIndent = -1;
					WriteStartElement (
						elem.NamespaceURI == null || elem.NamespaceURI.Length == 0 ? String.Empty : elem.Prefix,
						elem.LocalName,
						elem.NamespaceURI);
		
					if (elem.HasAttributes) {
						int oldBeforeSp = formatSettings.SpacesBeforeAssignment;
						int maxLen = 0;
						if (formatSettings.AlignAttributeValues) {
							foreach (XmlAttribute at in elem.Attributes) {
								string name = GetAttributeName (at);
								if (name.Length > maxLen)
									maxLen = name.Length;
							}
						}
						foreach (XmlAttribute at in elem.Attributes) {
							if (formatSettings.AlignAttributeValues) {
								string name = GetAttributeName (at);
								formatSettings.SpacesBeforeAssignment = (maxLen - name.Length) + oldBeforeSp;
							}
							WriteNode (at);
						}
						formatSettings.SpacesBeforeAssignment = oldBeforeSp;
					}
					
					if (!elem.IsEmpty) {
						CloseStartElement ();
						WriteContent (elem);
						WriteFullEndElement ();
					}
					else
						WriteEndElement ();
					break;
				}
				case XmlNodeType.EntityReference: {
					XmlEntityReference eref = (XmlEntityReference) node;
					WriteRaw ("&");
					WriteName (eref.Name);
					WriteRaw (";");
					break;
				}
				case XmlNodeType.ProcessingInstruction: {
					XmlProcessingInstruction pi = (XmlProcessingInstruction) node;
					WriteProcessingInstruction (pi.Target, pi.Data);
					break;
				}
				case XmlNodeType.SignificantWhitespace: {
					XmlSignificantWhitespace wn = (XmlSignificantWhitespace) node;
					WriteWhitespace (wn.Data);
					break;
				}
				case XmlNodeType.Text: {
					XmlText t = (XmlText) node;
					WriteString (t.Data);
					break;
				}
				case XmlNodeType.Whitespace: {
					XmlWhitespace wn = (XmlWhitespace) node;
					WriteWhitespace (wn.Data);
					break;
				}
				case XmlNodeType.XmlDeclaration: {
					if (!defaultFormatSettings.OmitXmlDeclaration) {
						XmlDeclaration dec = (XmlDeclaration) node;
						WriteRaw (String.Format ("<?xml {0}?>", dec.Value));
					}
					break;
				}
			}
			formatSettings = oldFormat;
		}
		string GetAttributeName (XmlAttribute at)
		{
			if (at.NamespaceURI.Length > 0)
				return at.Prefix + ":" + at.LocalName;
			else
				return at.LocalName;
		}
		
		void WriteContent (XmlNode node)
		{
			for (XmlNode n = node.FirstChild; n != null; n = n.NextSibling)
				WriteNode (n);
		}

		void WriteDeclarationIfMissing (XmlDocument doc)
		{
			var declaration = doc.FirstChild as XmlDeclaration;
			if (declaration == null) {
				declaration = doc.CreateXmlDeclaration ("1.0", "UTF-8", null);
				WriteNode (declaration);
			}
		}
		
		void SetFormat (XmlNode node)
		{
			XmlFormattingSettings s;
			if (formatMap.TryGetValue (node, out s)) {
				formatSettings = s;
			}
			else {
				if (node is XmlElement)
					formatSettings = defaultFormatSettings;
			}
		}

		// 2.0 XmlWriterSettings support

		// As for ConformanceLevel, MS.NET is inconsistent with
		// MSDN documentation. For example, even if ConformanceLevel
		// is set as .Auto, multiple WriteStartDocument() calls
		// result in an error.
		// ms-help://MS.NETFramework.v20.en/wd_xml/html/7db8802b-53d8-4735-a637-4d2d2158d643.htm

		// Context Retriever

		public override string XmlLang {
			get { return open_count == 0 ? null : elements [open_count - 1].XmlLang; }
		}

		public override XmlSpace XmlSpace {
			get { return open_count == 0 ? XmlSpace.None : elements [open_count - 1].XmlSpace; }
		}

		public override WriteState WriteState {
			get { return state; }
		}

		public override string LookupPrefix (string namespaceUri)
		{
			if (namespaceUri == null || namespaceUri == String.Empty)
				throw ArgumentError ("The Namespace cannot be empty.");

			if (namespaceUri == nsmanager.DefaultNamespace)
				return String.Empty;

			string prefix = nsmanager.LookupPrefixExclusive (namespaceUri, false);

			// XmlNamespaceManager has changed to return null
			// when NSURI not found.
			// (Contradiction to the ECMA documentation.)
			return prefix;
		}

		// Stream Control

		public Stream BaseStream {
			get { return base_stream; }
		}

		public override void Close ()
		{
			if (state != WriteState.Error) {
				if (state == WriteState.Attribute)
					WriteEndAttribute ();
				while (open_count > 0)
					WriteEndElement ();
			}

			if (close_output_stream)
				writer.Close ();
			else
				writer.Flush ();
			state = WriteState.Closed;
		}

		public override void Flush ()
		{
			writer.Flush ();
		}

		// Misc Control
		public bool Namespaces {
			get { return namespaces; }
			set {
				if (state != WriteState.Start)
					throw InvalidOperation ("This property must be set before writing output.");
				namespaces = value;
			}
		}

		// XML Declaration

		public override void WriteStartDocument ()
		{
			WriteStartDocumentCore (false, false);
			is_document_entity = true;
		}

		public override void WriteStartDocument (bool standalone)
		{
			WriteStartDocumentCore (true, standalone);
			is_document_entity = true;
		}

		void WriteStartDocumentCore (bool outputStd, bool standalone)
		{
			if (state != WriteState.Start)
				throw StateError ("XmlDeclaration");

			switch (xmldecl_state) {
			case XmlDeclState.Ignore:
				return;
			case XmlDeclState.Prohibit:
				throw InvalidOperation ("WriteStartDocument cannot be called when ConformanceLevel is Fragment.");
			}

			state = WriteState.Prolog;

			writer.Write ("<?xml version=");
			writer.Write (formatSettings.QuoteChar);
			writer.Write ("1.0");
			writer.Write (formatSettings.QuoteChar);
			if (!ignore_encoding) {
				writer.Write (" encoding=");
				writer.Write (formatSettings.QuoteChar);
				writer.Write (writer.Encoding.WebName);
				writer.Write (formatSettings.QuoteChar);
			}
			if (outputStd) {
				writer.Write (" standalone=");
				writer.Write (formatSettings.QuoteChar);
				writer.Write (standalone ? "yes" : "no");
				writer.Write (formatSettings.QuoteChar);
			}
			writer.Write ("?>");

			xmldecl_state = XmlDeclState.Ignore;
		}

		public override void WriteEndDocument ()
		{
			switch (state) {
			case WriteState.Error:
			case WriteState.Closed:
			case WriteState.Start:
				throw StateError ("EndDocument");
			}

			if (state == WriteState.Attribute)
				WriteEndAttribute ();
			while (open_count > 0)
				WriteEndElement ();

			state = WriteState.Start;
			is_document_entity = false;
		}

		// DocType Declaration

		public override void WriteDocType (string name,
			string pubid, string sysid, string subset)
		{
			if (name == null)
				throw ArgumentError ("name");
			if (!XmlChar.IsName (name))
				throw ArgumentError ("name");

			if (node_state != XmlNodeType.None)
				throw StateError ("DocType");
			node_state = XmlNodeType.DocumentType;

			if (xmldecl_state == XmlDeclState.Auto)
				OutputAutoStartDocument ();

			WriteIndent ();

			writer.Write ("<!DOCTYPE ");
			writer.Write (name);
			if (pubid != null) {
				writer.Write (" PUBLIC ");
				writer.Write (formatSettings.QuoteChar);
				writer.Write (pubid);
				writer.Write (formatSettings.QuoteChar);
				writer.Write (' ');
				writer.Write (formatSettings.QuoteChar);
				if (sysid != null)
					writer.Write (sysid);
				writer.Write (formatSettings.QuoteChar);
			}
			else if (sysid != null) {
				writer.Write (" SYSTEM ");
				writer.Write (formatSettings.QuoteChar);
				writer.Write (sysid);
				writer.Write (formatSettings.QuoteChar);
			}

			if (subset != null) {
				writer.Write ("[");
				// LAMESPEC: see the top of this source.
				writer.Write (subset);
				writer.Write ("]");
			}
			writer.Write ('>');

			state = WriteState.Prolog;
		}

		// StartElement

		public override void WriteStartElement (
			string prefix, string localName, string namespaceUri)
		{
			if (state == WriteState.Error || state == WriteState.Closed)
				throw StateError ("StartTag");
			node_state = XmlNodeType.Element;

			bool anonPrefix = (prefix == null);
			if (prefix == null)
				prefix = String.Empty;

			// Crazy namespace check goes here.
			//
			// 1. if Namespaces is false, then any significant 
			//    namespace indication is not allowed.
			// 2. if Prefix is non-empty and NamespaceURI is
			//    empty, it is an error in 1.x, or it is reset to
			//    an empty string in 2.0.
			// 3. null NamespaceURI indicates that namespace is
			//    not considered.
			// 4. prefix must not be equivalent to "XML" in
			//    case-insensitive comparison.
			if (!namespaces && namespaceUri != null && namespaceUri.Length > 0)
				throw ArgumentError ("Namespace is disabled in this XmlTextWriter.");
			if (!namespaces && prefix.Length > 0)
				throw ArgumentError ("Namespace prefix is disabled in this XmlTextWriter.");

			// If namespace URI is empty, then either prefix
			// must be empty as well, or there is an
			// existing namespace mapping for the prefix.
			if (prefix.Length > 0 && namespaceUri == null) {
				namespaceUri = nsmanager.LookupNamespace (prefix, false);
				if (namespaceUri == null || namespaceUri.Length == 0)
					throw ArgumentError ("Namespace URI must not be null when prefix is not an empty string.");
			}
			// Considering the fact that WriteStartAttribute()
			// automatically changes argument namespaceURI, this
			// is kind of silly implementation. See bug #77094.
			if (namespaces &&
			    prefix != null && prefix.Length == 3 &&
			    namespaceUri != XmlNamespace &&
			    (prefix [0] == 'x' || prefix [0] == 'X') &&
			    (prefix [1] == 'm' || prefix [1] == 'M') &&
			    (prefix [2] == 'l' || prefix [2] == 'L'))
				throw new ArgumentException ("A prefix cannot be equivalent to \"xml\" in case-insensitive match.");


			if (xmldecl_state == XmlDeclState.Auto)
				OutputAutoStartDocument ();
			if (state == WriteState.Element)
				CloseStartElement ();
			if (open_count > 0)
				elements [open_count - 1].HasElements = true;

			nsmanager.PushScope ();

			if (namespaces && namespaceUri != null) {
				// If namespace URI is empty, then prefix must 
				// be empty as well.
				if (anonPrefix && namespaceUri.Length > 0)
					prefix = LookupPrefix (namespaceUri);
				if (prefix == null || namespaceUri.Length == 0)
					prefix = String.Empty;
			}
			
			WriteEmptyLines (formatSettings.EmptyLinesBeforeStart);
			ResetEmptyLineCount ();
			WriteIndent ();

			writer.Write ("<");

			if (prefix.Length > 0) {
				writer.Write (prefix);
				writer.Write (':');
			}
			writer.Write (localName);

			if (elements.Length == open_count) {
				XmlNodeInfo [] tmp = new XmlNodeInfo [open_count << 1];
				Array.Copy (elements, tmp, open_count);
				elements = tmp;
			}
			if (elements [open_count] == null)
				elements [open_count] =
					new XmlNodeInfo ();
			XmlNodeInfo info = elements [open_count];
			info.Prefix = prefix;
			info.LocalName = localName;
			info.NS = namespaceUri;
			info.HasSimple = false;
			info.HasElements = false;
			info.XmlLang = XmlLang;
			info.XmlSpace = XmlSpace;
			open_count++;

			if (namespaces && namespaceUri != null) {
				string oldns = nsmanager.LookupNamespace (prefix, false);
				if (oldns != namespaceUri) {
					nsmanager.AddNamespace (prefix, namespaceUri);
					new_local_namespaces.Push (prefix);
				}
			}

			state = WriteState.Element;
		}
		
		void WriteEmptyLines (int count)
		{
			if (count > lastEmptyLineCount) {
				for (int n=0; n<count - lastEmptyLineCount; n++)
					writer.Write (newline);
				lastEmptyLineCount = count;
			}
		}
		
		void ResetEmptyLineCount ()
		{
			lastEmptyLineCount = 0;
		}
		
		void WriteAssignment ()
		{
			for (int n=0; n < formatSettings.SpacesBeforeAssignment; n++)
				writer.Write (' ');
			writer.Write ('=');
			for (int n=0; n < formatSettings.SpacesAfterAssignment; n++)
				writer.Write (' ');
		}

		void CloseStartElement ()
		{
			CloseStartElementCore ();

			if (state == WriteState.Element) {
				writer.Write ('>');
				WriteEmptyLines (formatSettings.EmptyLinesAfterStart);
			}
			state = WriteState.Content;
		}

		void CloseStartElementCore ()
		{
			ResetEmptyLineCount ();
			if (state == WriteState.Attribute)
				WriteEndAttribute ();

			if (new_local_namespaces.Count == 0) {
				if (explicit_nsdecls.Count > 0)
					explicit_nsdecls.Clear ();
				return;
			}

			// Missing xmlns attributes are added to 
			// explicit_nsdecls (it is cleared but this way
			// I save another array creation).
			int idx = explicit_nsdecls.Count;
			while (new_local_namespaces.Count > 0) {
				string p = (string) new_local_namespaces.Pop ();
				bool match = false;
				for (int i = 0; i < explicit_nsdecls.Count; i++) {
					if ((string) explicit_nsdecls [i] == p) {
						match = true;
						break;
					}
				}
				if (match)
					continue;
				explicit_nsdecls.Add (p);
			}

			for (int i = idx; i < explicit_nsdecls.Count; i++) {
				string prefix = (string) explicit_nsdecls [i];
				string ns = nsmanager.LookupNamespace (prefix, false);
				if (ns == null)
					continue; // superceded
				if (prefix.Length > 0) {
					writer.Write (" xmlns:");
					writer.Write (prefix);
				} else {
					writer.Write (" xmlns");
				}
				WriteAssignment ();
				writer.Write (formatSettings.QuoteChar);
				WriteEscapedString (ns, true);
				writer.Write (formatSettings.QuoteChar);
			}
			explicit_nsdecls.Clear ();
		}

		// EndElement

		public override void WriteEndElement ()
		{
			WriteEndElementCore (false);
		}

		public override void WriteFullEndElement ()
		{
			WriteEndElementCore (true);
		}

		void WriteEndElementCore (bool full)
		{
			if (state == WriteState.Error || state == WriteState.Closed)
				throw StateError ("EndElement");
			if (open_count == 0)
				throw InvalidOperation ("There is no more open element.");

			// bool isEmpty = state != WriteState.Content;

			CloseStartElementCore ();

			nsmanager.PopScope ();

			if (state == WriteState.Element) {
				if (full) {
					writer.Write ('>');
					WriteEmptyLines (formatSettings.EmptyLinesAfterStart);
					WriteEmptyLines (formatSettings.EmptyLinesBeforeEnd);
				}
				else {
					writer.Write (" />");
					WriteEmptyLines (formatSettings.EmptyLinesAfterEnd);
				}
			}

			if (full || state == WriteState.Content) {
				WriteEmptyLines (formatSettings.EmptyLinesBeforeEnd);
				WriteIndentEndElement ();
			}

			XmlNodeInfo info = elements [--open_count];

			if (full || state == WriteState.Content) {
				writer.Write ("</");
				if (info.Prefix.Length > 0) {
					writer.Write (info.Prefix);
					writer.Write (':');
				}
				writer.Write (info.LocalName);
				writer.Write ('>');
				ResetEmptyLineCount ();
				WriteEmptyLines (formatSettings.EmptyLinesAfterEnd);
			}

			state = WriteState.Content;
			if (open_count == 0)
				node_state = XmlNodeType.EndElement;
		}

		// Attribute

		public override void WriteStartAttribute (
			string prefix, string localName, string namespaceUri)
		{
			// LAMESPEC: this violates the expected behavior of
			// this method, as it incorrectly allows unbalanced
			// output of attributes. Microfot changes description
			// on its behavior at their will, regardless of
			// ECMA description.
			if (state == WriteState.Attribute)
				WriteEndAttribute ();

			if (state != WriteState.Element && state != WriteState.Start)
				throw StateError ("Attribute");

			if ((object) prefix == null)
				prefix = String.Empty;

			// For xmlns URI, prefix is forced to be "xmlns"
			bool isNSDecl = false;
			if (namespaceUri == XmlnsNamespace) {
				isNSDecl = true;
				if (prefix.Length == 0 && localName != "xmlns")
					prefix = "xmlns";
			}
			else
				isNSDecl = (prefix == "xmlns" ||
					localName == "xmlns" && prefix.Length == 0);

			if (namespaces) {
				// MS implementation is pretty hacky here. 
				// Regardless of namespace URI it is regarded
				// as NS URI for "xml".
				if (prefix == "xml")
					namespaceUri = XmlNamespace;
				// infer namespace URI.
				else if ((object) namespaceUri == null) {
					if (isNSDecl)
						namespaceUri = XmlnsNamespace;
					else
						namespaceUri = String.Empty;
				}

				// It is silly design - null namespace with
				// "xmlns" are allowed (for namespace-less
				// output; while there is Namespaces property)
				// On the other hand, namespace "" is not 
				// allowed.
				if (isNSDecl && namespaceUri != XmlnsNamespace)
					throw ArgumentError (String.Format ("The 'xmlns' attribute is bound to the reserved namespace '{0}'", XmlnsNamespace));

				// If namespace URI is empty, then either prefix
				// must be empty as well, or there is an
				// existing namespace mapping for the prefix.
				if (prefix.Length > 0 && namespaceUri.Length == 0) {
					namespaceUri = nsmanager.LookupNamespace (prefix, false);
					if (namespaceUri == null || namespaceUri.Length == 0)
						throw ArgumentError ("Namespace URI must not be null when prefix is not an empty string.");
				}

				// Dive into extremely complex procedure.
				if (!isNSDecl && namespaceUri.Length > 0)
					prefix = DetermineAttributePrefix (
						prefix, localName, namespaceUri);
			}

			writer.AttributesPerLine++;
			if (formatSettings.WrapAttributes && writer.AttributesPerLine > 1)
				writer.MarkBlockStart ();
			
			if (formatSettings.AttributesInNewLine || writer.AttributesPerLine > formatSettings.MaxAttributesPerLine) {
				writer.MarkBlockEnd ();
				WriteIndentAttribute ();
				writer.AttributesPerLine = 1;
			}
			else if (state != WriteState.Start)
				writer.Write (' ');

			if (writer.AttributesIndent == -1)
				writer.AttributesIndent = writer.Column;

			if (prefix.Length > 0) {
				writer.Write (prefix);
				writer.Write (':');
			}
			writer.Write (localName);
			WriteAssignment ();
			writer.Write (formatSettings.QuoteChar);

			if (isNSDecl || prefix == "xml") {
				if (preserver == null)
					preserver = new StringWriter ();
				else
					preserver.GetStringBuilder ().Length = 0;
				writer = new TextWriterWrapper (preserver, this, writer);

				if (!isNSDecl) {
					is_preserved_xmlns = false;
					preserved_name = localName;
				} else {
					is_preserved_xmlns = true;
					preserved_name = localName == "xmlns" ? 
						String.Empty : localName;
				}
			}

			state = WriteState.Attribute;
		}

		// See also:
		// "DetermineAttributePrefix(): local mapping overwrite"
		string DetermineAttributePrefix (
			string prefix, string local, string ns)
		{
			bool mockup = false;
			if (prefix.Length == 0) {
				prefix = LookupPrefix (ns);
				if (prefix != null && prefix.Length > 0)
					return prefix;
				mockup = true;
			} else {
				prefix = nsmanager.NameTable.Add (prefix);
				string existing = nsmanager.LookupNamespace (prefix, true);
				if (existing == ns)
					return prefix;
				if (existing != null) {
					// See code comment on the head of
					// this source file.
					nsmanager.RemoveNamespace (prefix, existing);
					if (nsmanager.LookupNamespace (prefix, true) != existing) {
						mockup = true;
						nsmanager.AddNamespace (prefix, existing);
					}
				}
			}

			if (mockup)
				prefix = MockupPrefix (ns, true);
			new_local_namespaces.Push (prefix);
			nsmanager.AddNamespace (prefix, ns);

			return prefix;
		}

		string MockupPrefix (string ns, bool skipLookup)
		{
			string prefix = skipLookup ? null :
				LookupPrefix (ns);
			if (prefix != null && prefix.Length > 0)
				return prefix;
			for (int p = 1; ; p++) {
				prefix = StringUtil.Format ("d{0}p{1}", open_count, p);
				if (new_local_namespaces.Contains (prefix))
					continue;
				if (null != nsmanager.LookupNamespace (
					nsmanager.NameTable.Get (prefix)))
					continue;
				nsmanager.AddNamespace (prefix, ns);
				new_local_namespaces.Push (prefix);
				return prefix;
			}
		}

		public override void WriteEndAttribute ()
		{
			if (state != WriteState.Attribute)
				throw StateError ("End of attribute");

			if (writer.Wrapped == preserver) {
				writer = writer.PreviousWrapper ?? new TextWriterWrapper (source, this);
				string value = preserver.ToString ();
				if (is_preserved_xmlns) {
					if (preserved_name.Length > 0 &&
					    value.Length == 0)
						throw ArgumentError ("Non-empty prefix must be mapped to non-empty namespace URI.");
					string existing = nsmanager.LookupNamespace (preserved_name, false);
					explicit_nsdecls.Add (preserved_name);
					if (open_count > 0) {

						if (v2 &&
						    elements [open_count - 1].Prefix == preserved_name &&
						    elements [open_count - 1].NS != value)
							throw new XmlException (String.Format ("Cannot redefine the namespace for prefix '{0}' used at current element", preserved_name));

						if (elements [open_count - 1].NS != String.Empty ||
						    elements [open_count - 1].Prefix != preserved_name) {
							if (existing != value)
								nsmanager.AddNamespace (preserved_name, value);
						}
					}
				} else {
					switch (preserved_name) {
					case "lang":
						if (open_count > 0)
							elements [open_count - 1].XmlLang = value;
						break;
					case "space":
						switch (value) {
						case "default":
							if (open_count > 0)
								elements [open_count - 1].XmlSpace = XmlSpace.Default;
							break;
						case "preserve":
							if (open_count > 0)
								elements [open_count - 1].XmlSpace = XmlSpace.Preserve;
							break;
						default:
							throw ArgumentError ("Invalid value for xml:space.");
						}
						break;
					}
				}
				writer.Write (value);
			}

			writer.Write (formatSettings.QuoteChar);
			
			if (writer.InBlock) {
				writer.MarkBlockEnd ();
				if (writer.Column > TextPolicy.FileWidth) {
					WriteIndentAttribute ();
					writer.WriteBlock (true);
					writer.AttributesPerLine++;
				} else {
					writer.WriteBlock (false);
				}
			}
			
			state = WriteState.Element;
		}

		// Non-Text Content

		public override void WriteComment (string text)
		{
			if (text == null)
				throw ArgumentError ("text");

			if (text.Length > 0 && text [text.Length - 1] == '-')
				throw ArgumentError ("An input string to WriteComment method must not end with '-'. Escape it with '&#2D;'.");
			if (StringUtil.IndexOf (text, "--") > 0)
				throw ArgumentError ("An XML comment cannot end with \"-\".");

			if (state == WriteState.Attribute || state == WriteState.Element)
				CloseStartElement ();

			WriteIndent ();

			ShiftStateTopLevel ("Comment", false, false, false);

			writer.Write ("<!--");
			writer.Write (text);
			writer.Write ("-->");
			ResetEmptyLineCount ();
		}

		// LAMESPEC: see comments on the top of this source.
		public override void WriteProcessingInstruction (string name, string text)
		{
			if (name == null)
				throw ArgumentError ("name");
			if (text == null)
				throw ArgumentError ("text");

			WriteIndent ();

			if (!XmlChar.IsName (name))
				throw ArgumentError ("A processing instruction name must be a valid XML name.");

			if (StringUtil.IndexOf (text, "?>") > 0)
				throw ArgumentError ("Processing instruction cannot contain \"?>\" as its value.");

			ShiftStateTopLevel ("ProcessingInstruction", false, name == "xml", false);

			writer.Write ("<?");
			writer.Write (name);
			writer.Write (' ');
			writer.Write (text);
			writer.Write ("?>");

			if (state == WriteState.Start)
				state = WriteState.Prolog;
			ResetEmptyLineCount ();
		}

		// Text Content

		public override void WriteWhitespace (string text)
		{
			if (text == null)
				throw ArgumentError ("text");

			// huh? Shouldn't it accept an empty string???
			if (text.Length == 0 ||
			    XmlChar.IndexOfNonWhitespace (text) >= 0)
				throw ArgumentError ("WriteWhitespace method accepts only whitespaces.");

			ShiftStateTopLevel ("Whitespace", true, false, true);

			writer.Write (text);
			ResetEmptyLineCount ();
		}

		public override void WriteCData (string text)
		{
			if (text == null)
				text = String.Empty;
			ShiftStateContent ("CData", false);

			if (StringUtil.IndexOf (text, "]]>") >= 0)
				throw ArgumentError ("CDATA section must not contain ']]>'.");
			writer.Write ("<![CDATA[");
			WriteCheckedString (text);
			writer.Write ("]]>");
			ResetEmptyLineCount ();
		}

		public override void WriteString (string text)
		{
			if (text == null || text.Length == 0)
				return; // do nothing, including state transition.
			ShiftStateContent ("Text", true);

			WriteEscapedString (text, state == WriteState.Attribute);
		}

		public override void WriteRaw (string raw)
		{
			if (raw == null)
				return; // do nothing, including state transition.

			//WriteIndent ();

			// LAMESPEC: It rejects XMLDecl while it allows
			// DocType which could consist of non well-formed XML.
			ShiftStateTopLevel ("Raw string", true, true, true);

			writer.Write (raw);
		}

		public override void WriteCharEntity (char ch)
		{
			WriteCharacterEntity (ch, '\0', false);
		}

		public override void WriteSurrogateCharEntity (char low, char high)
		{
			WriteCharacterEntity (low, high, true);
		}

		void WriteCharacterEntity (char ch, char high, bool surrogate)
		{
			if (surrogate &&
			    ('\uD800' > high || high > '\uDC00' ||
			     '\uDC00' > ch || ch > '\uDFFF'))
				throw ArgumentError (String.Format ("Invalid surrogate pair was found. Low: &#x{0:X}; High: &#x{1:X};", (int) ch, (int) high));
			else if (check_character_validity && XmlChar.IsInvalid (ch))
				throw ArgumentError (String.Format ("Invalid character &#x{0:X};", (int) ch));

			ShiftStateContent ("Character", true);

			int v = surrogate ? (high - 0xD800) * 0x400 + ch - 0xDC00 + 0x10000 : (int) ch;
			writer.Write ("&#x");
			writer.Write (v.ToString ("X", CultureInfo.InvariantCulture));
			writer.Write (';');
		}

		public override void WriteEntityRef (string name)
		{
			if (name == null)
				throw ArgumentError ("name");
			if (!XmlChar.IsName (name))
				throw ArgumentError ("Argument name must be a valid XML name.");

			ShiftStateContent ("Entity reference", true);

			writer.Write ('&');
			writer.Write (name);
			writer.Write (';');
		}

		// Applied methods

		public override void WriteName (string name)
		{
			if (name == null)
				throw ArgumentError ("name");
			if (!XmlChar.IsName (name))
				throw ArgumentError ("Not a valid name string.");
			WriteString (name);
		}

		public override void WriteNmToken (string nmtoken)
		{
			if (nmtoken == null)
				throw ArgumentError ("nmtoken");
			if (!XmlChar.IsNmToken (nmtoken))
				throw ArgumentError ("Not a valid NMTOKEN string.");
			WriteString (nmtoken);
		}

		public override void WriteQualifiedName (
			string localName, string ns)
		{
			if (localName == null)
				throw ArgumentError ("localName");
			if (ns == null)
				ns = String.Empty;

			if (ns == XmlnsNamespace)
				throw ArgumentError ("Prefix 'xmlns' is reserved and cannot be overriden.");
			if (!XmlChar.IsNCName (localName))
				throw ArgumentError ("localName must be a valid NCName.");

			ShiftStateContent ("QName", true);

			string prefix = ns.Length > 0 ? LookupPrefix (ns) : String.Empty;
			if (prefix == null) {
				if (state == WriteState.Attribute)
					prefix = MockupPrefix (ns, false);
				else
					throw ArgumentError (String.Format ("Namespace '{0}' is not declared.", ns));
			}

			if (prefix != String.Empty) {
				writer.Write (prefix);
				writer.Write (":");
			}
			writer.Write (localName);
		}

		// Chunk data

		void CheckChunkRange (Array buffer, int index, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");
			if (index < 0 || buffer.Length < index)
				throw ArgumentOutOfRangeError ("index");
			if (count < 0 || buffer.Length < index + count)
				throw ArgumentOutOfRangeError ("count");
		}

		public override void WriteBase64 (byte [] buffer, int index, int count)
		{
			CheckChunkRange (buffer, index, count);

			WriteString (Convert.ToBase64String (buffer, index, count));
		}

		public override void WriteBinHex (byte [] buffer, int index, int count)
		{
			CheckChunkRange (buffer, index, count);

			ShiftStateContent ("BinHex", true);

			WriteBinHex (buffer, index, count, writer);
		}

		internal static void WriteBinHex (byte [] buffer, int index, int count, TextWriter w)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");
			if (index < 0) {
				throw new ArgumentOutOfRangeException (
					"index", index,
					"index must be non negative integer.");
			}
			if (count < 0) {
				throw new ArgumentOutOfRangeException (
					"count", count,
					"count must be non negative integer.");
			}
			if (buffer.Length < index + count)
				throw new ArgumentOutOfRangeException ("index and count must be smaller than the length of the buffer.");

			// Copied from XmlTextWriter.WriteBinHex ()
			int end = index + count;
			for (int i = index; i < end; i++) {
				int val = buffer [i];
				int high = val >> 4;
				int low = val & 15;
				if (high > 9)
					w.Write ((char) (high + 55));
				else
					w.Write ((char) (high + 0x30));
				if (low > 9)
					w.Write ((char) (low + 55));
				else
					w.Write ((char) (low + 0x30));
			}
		}

		public override void WriteChars (char [] buffer, int index, int count)
		{
			CheckChunkRange (buffer, index, count);

			ShiftStateContent ("Chars", true);

			WriteEscapedBuffer (buffer, index, count,
				state == WriteState.Attribute);
		}

		public override void WriteRaw (char [] buffer, int index, int count)
		{
			CheckChunkRange (buffer, index, count);

			ShiftStateContent ("Raw text", false);

			writer.Write (buffer, index, count);
		}

		// Utilities

		void WriteIndent ()
		{
			WriteIndentCore (0, false);
		}

		void WriteIndentEndElement ()
		{
			WriteIndentCore (-1, false);
		}

		void WriteIndentAttribute ()
		{
			if (formatSettings.AlignAttributes && writer.AttributesIndent != -1) {
				if (state != WriteState.Start)
					writer.Write (newline);
				if (TextPolicy.TabsToSpaces)
					writer.Write (new string (' ', writer.AttributesIndent));
				else
					writer.Write (new string ('\t', writer.AttributesIndent / TextPolicy.TabWidth) + new string (' ', writer.AttributesIndent % TextPolicy.TabWidth));
			} else {
				if (!WriteIndentCore (0, true))
					writer.Write (' '); // space is required instead.
			}
		}

		bool WriteIndentCore (int nestFix, bool attribute)
		{
			if (!formatSettings.IndentContent)
				return false;
			for (int i = open_count - 1; i >= 0; i--)
				if (!attribute && elements [i].HasSimple)
					return false;

			if (state != WriteState.Start)
				writer.Write (newline);
			writer.Write (TextPolicy.TabsToSpaces ? new string (' ', (open_count + nestFix) * TextPolicy.TabWidth) : new string ('\t', open_count + nestFix));
			return true;
		}

		void OutputAutoStartDocument ()
		{
			if (state != WriteState.Start)
				return;
			WriteStartDocumentCore (false, false);
		}

		void ShiftStateTopLevel (string occured, bool allowAttribute, bool dontCheckXmlDecl, bool isCharacter)
		{
			switch (state) {
			case WriteState.Error:
			case WriteState.Closed:
				throw StateError (occured);
			case WriteState.Start:
				if (isCharacter)
					CheckMixedContentState ();
				if (xmldecl_state == XmlDeclState.Auto && !dontCheckXmlDecl)
					OutputAutoStartDocument ();
				state = WriteState.Prolog;
				break;
			case WriteState.Attribute:
				if (allowAttribute)
					break;
				goto case WriteState.Closed;
			case WriteState.Element:
				if (isCharacter)
					CheckMixedContentState ();
				CloseStartElement ();
				break;
			case WriteState.Content:
				if (isCharacter)
					CheckMixedContentState ();
				break;
			}

		}

		void CheckMixedContentState ()
		{
//			if (open_count > 0 &&
//			    state != WriteState.Attribute)
//				elements [open_count - 1].HasSimple = true;
			if (open_count > 0)
				elements [open_count - 1].HasSimple = true;
		}

		void ShiftStateContent (string occured, bool allowAttribute)
		{
			switch (state) {
			case WriteState.Error:
			case WriteState.Closed:
					throw StateError (occured);
			case WriteState.Prolog:
			case WriteState.Start:
				if (!allow_doc_fragment || is_document_entity)
					goto case WriteState.Closed;
				if (xmldecl_state == XmlDeclState.Auto)
					OutputAutoStartDocument ();
				CheckMixedContentState ();
				state = WriteState.Content;
				break;
			case WriteState.Attribute:
				if (allowAttribute)
					break;
				goto case WriteState.Closed;
			case WriteState.Element:
				CloseStartElement ();
				CheckMixedContentState ();
				break;
			case WriteState.Content:
				CheckMixedContentState ();
				break;
			}
		}

		void WriteEscapedString (string text, bool isAttribute)
		{
			escaped_attr_chars [0] = formatSettings.QuoteChar;
			char [] escaped = isAttribute ?
				escaped_attr_chars : escaped_text_chars;

			int idx = text.IndexOfAny (escaped);
			if (idx >= 0) {
				char [] arr = text.ToCharArray ();
				WriteCheckedBuffer (arr, 0, idx);
				WriteEscapedBuffer (
					arr, idx, arr.Length - idx, isAttribute);
			} else {
				WriteCheckedString (text);
			}
		}

		void WriteCheckedString (string s)
		{
			int i = XmlChar.IndexOfInvalid (s, true);
			if (i >= 0) {
				char [] arr = s.ToCharArray ();
				writer.Write (arr, 0, i);
				WriteCheckedBuffer (arr, i, arr.Length - i);
			} else {
				// no invalid character.
				writer.Write (s);
			}
		}

		void WriteCheckedBuffer (char [] text, int idx, int length)
		{
			int start = idx;
			int end = idx + length;
			while ((idx = XmlChar.IndexOfInvalid (text, start, length, true)) >= 0) {
				if (check_character_validity) // actually this is one time pass.
					throw ArgumentError (String.Format ("Input contains invalid character at {0} : &#x{1:X};", idx, (int) text [idx]));
				if (start < idx)
					writer.Write (text, start, idx - start);
				writer.Write ("&#x");
				writer.Write (((int) text [idx]).ToString (
					"X",
					CultureInfo.InvariantCulture));
				writer.Write (';');
				length -= idx - start + 1;
				start = idx + 1;
			}
			if (start < end)
				writer.Write (text, start, end - start);
		}

		void WriteEscapedBuffer (char [] text, int index, int length,
			bool isAttribute)
		{
			int start = index;
			int end = index + length;
			for (int i = start; i < end; i++) {
				switch (text [i]) {
				default:
					continue;
				case '&':
				case '<':
				case '>':
					if (start < i)
						WriteCheckedBuffer (text, start, i - start);
					writer.Write ('&');
					switch (text [i]) {
					case '&': writer.Write ("amp;"); break;
					case '<': writer.Write ("lt;"); break;
					case '>': writer.Write ("gt;"); break;
					case '\'': writer.Write ("apos;"); break;
					case '"': writer.Write ("quot;"); break;
					}
					break;
				case '"':
				case '\'':
					if (isAttribute && text [i] == formatSettings.QuoteChar)
						goto case '&';
					continue;
				case '\r':
					if (i + 1 < end && text [i] == '\n')
						i++; // CRLF
					goto case '\n';
				case '\n':
					if (start < i)
						WriteCheckedBuffer (text, start, i - start);
					if (isAttribute) {
						writer.Write (text [i] == '\r' ?
							"&#xD;" : "&#xA;");
						break;
					}
					switch (newline_handling) {
					case NewLineHandling.Entitize:
						writer.Write (text [i] == '\r' ?
							"&#xD;" : "&#xA;");
						break;
					case NewLineHandling.Replace:
						writer.Write (newline);
						break;
					default:
						writer.Write (text [i]);
						break;
					}
					break;
				}
				start = i + 1;
			}
			if (start < end)
				WriteCheckedBuffer (text, start, end - start);
		}

		// Exceptions

		Exception ArgumentOutOfRangeError (string name)
		{
			state = WriteState.Error;
			return new ArgumentOutOfRangeException (name);
		}

		Exception ArgumentError (string msg)
		{
			state = WriteState.Error;
			return new ArgumentException (msg);
		}

		Exception InvalidOperation (string msg)
		{
			state = WriteState.Error;
			return new InvalidOperationException (msg);
		}

		Exception StateError (string occured)
		{
			return InvalidOperation (String.Format ("This XmlWriter does not accept {0} at this state {1}.", occured, state));
		}
	}

	internal class XmlNamespaceManager : IXmlNamespaceResolver, IEnumerable
	{
		#region Data
		struct NsDecl {
			public string Prefix, Uri;
		}
		
		struct NsScope {
			public int DeclCount;
			public string DefaultNamespace;
		}
		
		NsDecl [] decls;
		int declPos = -1;
		
		NsScope [] scopes;
		int scopePos = -1;
		
		string defaultNamespace;
		int count;
		
		void InitData ()
		{
			decls = new NsDecl [10];
			scopes = new NsScope [40];
		}
		
		// precondition declPos == nsDecl.Length
		void GrowDecls ()
		{
			NsDecl [] old = decls;
			decls = new NsDecl [declPos * 2 + 1];
			if (declPos > 0)
				Array.Copy (old, 0, decls, 0, declPos);
		}
		
		// precondition scopePos == scopes.Length
		void GrowScopes ()
		{
			NsScope [] old = scopes;
			scopes = new NsScope [scopePos * 2 + 1];
			if (scopePos > 0)
				Array.Copy (old, 0, scopes, 0, scopePos);
		}
		
		#endregion
		
		#region Fields

		private XmlNameTable nameTable;
		internal const string XmlnsXml = "http://www.w3.org/XML/1998/namespace";
		internal const string XmlnsXmlns = "http://www.w3.org/2000/xmlns/";
		internal const string PrefixXml = "xml";
		internal const string PrefixXmlns = "xmlns";

		#endregion

		#region Constructor

		public XmlNamespaceManager (XmlNameTable nameTable)
		{
			if (nameTable == null)
				throw new ArgumentNullException ("nameTable");
			this.nameTable = nameTable;

			nameTable.Add (PrefixXmlns);
			nameTable.Add (PrefixXml);
			nameTable.Add (String.Empty);
			nameTable.Add (XmlnsXmlns);
			nameTable.Add (XmlnsXml);
			
			InitData ();
		}

		#endregion

		#region Properties

		public virtual string DefaultNamespace {
			get { return defaultNamespace == null ? string.Empty : defaultNamespace; }
		}

		public virtual XmlNameTable NameTable {
			get { return nameTable; }
		}

		#endregion

		#region Methods

		public virtual void AddNamespace (string prefix, string uri)
		{
			AddNamespace (prefix, uri, false);
		}

		internal virtual void AddNamespace (string prefix, string uri, bool atomizedNames)
		{
			if (prefix == null)
				throw new ArgumentNullException ("prefix", "Value cannot be null.");

			if (uri == null)
				throw new ArgumentNullException ("uri", "Value cannot be null.");
			if (!atomizedNames) {
				prefix = nameTable.Add (prefix);
				uri = nameTable.Add (uri);
			}

			if (prefix == PrefixXml && uri == XmlnsXml)
				return;

			IsValidDeclaration (prefix, uri, true);

			if (prefix.Length == 0)
				defaultNamespace = uri;
			
			for (int i = declPos; i > declPos - count; i--) {
				if (object.ReferenceEquals (decls [i].Prefix, prefix)) {
					decls [i].Uri = uri;
					return;
				}
			}
			
			declPos ++;
			count ++;
			
			if (declPos == decls.Length)
				GrowDecls ();
			decls [declPos].Prefix = prefix;
			decls [declPos].Uri = uri;
		}

		static string IsValidDeclaration (string prefix, string uri, bool throwException)
		{
			string message = null;
			// It is funky, but it does not check whether prefix
			// is equivalent to "xml" in case-insensitive means.
			if (prefix == PrefixXml && uri != XmlnsXml)
				message = String.Format ("Prefix \"xml\" can only be bound to the fixed namespace URI \"{0}\". \"{1}\" is invalid.", XmlnsXml, uri);
			else if (message == null && prefix == "xmlns")
				message = "Declaring prefix named \"xmlns\" is not allowed to any namespace.";
			else if (message == null && uri == XmlnsXmlns)
				message = String.Format ("Namespace URI \"{0}\" cannot be declared with any namespace.", XmlnsXmlns);
			if (message != null && throwException)
				throw new ArgumentException (message);
			else
				return message;
		}

		public virtual IEnumerator GetEnumerator ()
		{
			// In fact it returns such table's enumerator that contains all the namespaces.
			// while HasNamespace() ignores pushed namespaces.
			
			Hashtable ht = new Hashtable ();
			for (int i = 0; i <= declPos; i++) {
				if (decls [i].Prefix != string.Empty && decls [i].Uri != null) {
					ht [decls [i].Prefix] = decls [i].Uri;
				}
			}
			
			ht [string.Empty] = DefaultNamespace;
			ht [PrefixXml] = XmlnsXml;
			ht [PrefixXmlns] = XmlnsXmlns;
			
			return ht.Keys.GetEnumerator ();
		}

		public virtual IDictionary<string, string> GetNamespacesInScope (XmlNamespaceScope scope)
		{
			IDictionary namespaceTable = GetNamespacesInScopeImpl (scope);
			IDictionary<string, string> namespaces = new Dictionary<string, string>(namespaceTable.Count);

			foreach (DictionaryEntry entry in namespaceTable) {
				namespaces[(string) entry.Key] = (string) entry.Value;
			}
			return namespaces;
		}

		internal virtual IDictionary GetNamespacesInScopeImpl (XmlNamespaceScope scope)
		{
			Hashtable table = new Hashtable ();

			if (scope == XmlNamespaceScope.Local) {
				for (int i = 0; i < count; i++)
					if (decls [declPos - i].Prefix == String.Empty && decls [declPos - i].Uri == String.Empty) {
						if (table.Contains (String.Empty))
							table.Remove (String.Empty);
					}
					else if (decls [declPos - i].Uri != null)
						table.Add (decls [declPos - i].Prefix, decls [declPos - i].Uri);
				return table;
			} else {
				for (int i = 0; i <= declPos; i++) {
					if (decls [i].Prefix == String.Empty && decls [i].Uri == String.Empty) {
						// removal of default namespace
						if (table.Contains (String.Empty))
							table.Remove (String.Empty);
					}
					else if (decls [i].Uri != null)
						table [decls [i].Prefix] = decls [i].Uri;
				}

				if (scope == XmlNamespaceScope.All)
					table.Add ("xml", XmlNamespaceManager.XmlnsXml);
				return table;
			}
		}

		public virtual bool HasNamespace (string prefix)
		{
			return HasNamespace (prefix, false);
		}

		internal virtual bool HasNamespace (string prefix, bool atomizedNames)
		{
			if (prefix == null || count == 0)
				return false;

			for (int i = declPos; i > declPos - count; i--) {
				if (decls [i].Prefix == prefix)
					return true;
			}
			
			return false;
		}

		public virtual string LookupNamespace (string prefix)
		{
			return LookupNamespace (prefix, false);
		}

		internal virtual string LookupNamespace (string prefix, bool atomizedNames)
		{
			switch (prefix) {
			case PrefixXmlns:
				return nameTable.Get (XmlnsXmlns);
			case PrefixXml:
				return nameTable.Get (XmlnsXml);
			case "":
				return DefaultNamespace;
			case null:
				return null;
			}

			for (int i = declPos; i >= 0; i--) {
				if (CompareString (decls [i].Prefix, prefix, atomizedNames) && decls [i].Uri != null /* null == flag for removed */)
					return decls [i].Uri;
			}
			
			return null;
		}

		public virtual string LookupPrefix (string uri)
		{
			return LookupPrefix (uri, false);
		}

		private bool CompareString (string s1, string s2, bool atomizedNames)
		{
			if (atomizedNames)
				return object.ReferenceEquals (s1, s2);
			else
				return s1 == s2;
		}

		internal string LookupPrefix (string uri, bool atomizedName)
		{
			return LookupPrefixCore (uri, atomizedName, false);
		}

		internal string LookupPrefixExclusive (string uri, bool atomizedName)
		{
			return LookupPrefixCore (uri, atomizedName, true);
		}

		string LookupPrefixCore (string uri, bool atomizedName, bool excludeOverriden)
		{
			if (uri == null)
				return null;

			if (CompareString (uri, DefaultNamespace, atomizedName))
				return string.Empty;

			if (CompareString (uri, XmlnsXml, atomizedName))
				return PrefixXml;
			
			if (CompareString (uri, XmlnsXmlns, atomizedName))
				return PrefixXmlns;

			for (int i = declPos; i >= 0; i--) {
				if (CompareString (decls [i].Uri, uri, atomizedName) && decls [i].Prefix.Length > 0) // we already looked for ""
					if (!excludeOverriden || !IsOverriden (i))
						return decls [i].Prefix;
			}

			// ECMA specifies that this method returns String.Empty
			// in case of no match. But actually MS.NET returns null.
			// For more information,see
			//  http://lists.ximian.com/archives/public/mono-list/2003-January/005071.html
			//return String.Empty;
			return null;
		}

		bool IsOverriden (int idx)
		{
			if (idx == declPos)
				return false;
			string prefix = decls [idx + 1].Prefix;
			for (int i = idx + 1; i <= declPos; i++)
				if ((object) decls [idx].Prefix == (object) prefix)
					return true;
			return false;
		}

		public virtual bool PopScope ()
		{
			if (scopePos == -1)
				return false;

			declPos -= count;
			defaultNamespace = scopes [scopePos].DefaultNamespace;
			count = scopes [scopePos].DeclCount;
			scopePos --;
			return true;
		}

		public virtual void PushScope ()
		{
			scopePos ++;
			if (scopePos == scopes.Length)
				GrowScopes ();
			
			scopes [scopePos].DefaultNamespace = defaultNamespace;
			scopes [scopePos].DeclCount = count;
			count = 0;
		}

		// It is rarely used, so we don't need NameTable optimization on it.
		public virtual void RemoveNamespace (string prefix, string uri)
		{
			RemoveNamespace (prefix, uri, false);
		}

		internal virtual void RemoveNamespace (string prefix, string uri, bool atomizedNames)
		{
			if (prefix == null)
				throw new ArgumentNullException ("prefix");

			if (uri == null)
				throw new ArgumentNullException ("uri");
			
			if (count == 0)
				return;

			for (int i = declPos; i > declPos - count; i--) {
				if (CompareString (decls [i].Prefix, prefix, atomizedNames) && CompareString (decls [i].Uri, uri, atomizedNames))
					decls [i].Uri = null;
			}
		}

		#endregion
	}
	
	class TextWriterWrapper: TextWriter
	{
		public TextWriter Wrapped;
		public readonly TextWriterWrapper PreviousWrapper;
		XmlFormatterWriter formatter;
		StringBuilder sb;
		bool inBlock;
		
		public int Column;
		public int AttributesPerLine;
		public int AttributesIndent;

		public TextWriterWrapper (TextWriter wrapped, XmlFormatterWriter formatter)
		{
			this.Wrapped = wrapped;
			this.formatter = formatter;
		}

		public TextWriterWrapper (TextWriter wrapped, XmlFormatterWriter formatter, TextWriterWrapper currentWriter)
			: this (wrapped, formatter)
		{
			PreviousWrapper = currentWriter;
		}

		public void MarkBlockStart ()
		{
			sb = new StringBuilder ();
			inBlock = true;
		}
		
		public void MarkBlockEnd ()
		{
			inBlock = false;
		}
		
		public void WriteBlock (bool wrappedLine)
		{
			if (wrappedLine)
				Write (sb.ToString ());
			else
				Wrapped.Write (sb.ToString ());
			sb = null;
		}
		
		public bool InBlock {
			get { return this.inBlock; }
		}
		
		public override Encoding Encoding {
			get { return Wrapped.Encoding; }
		}
		
		public override void Write (char c)
		{
			if (inBlock)
				sb.Append (c);
			else
				Wrapped.Write (c);
			
			if (c == '\n') {
				AttributesPerLine = 0;
				Column = 0;
			}
			else {
				if (c == '\t')
					Column += formatter.TextPolicy.TabWidth;
				else
					Column++;
			}
		}
	}
}
