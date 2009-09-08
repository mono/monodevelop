//
// Taken from PodSleuth (http://git.gnome.org/cgit/podsleuth)
//  
// Author:
//       Aaron Bockover <abockover@novell.com>
// 
// Copyright (c) 2007-2009 Novell, Inc. (http://www.novell.com)
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
using System.Xml;
using System.IO;
using System.Diagnostics;



namespace PropertyList
{
	public class PlistDocument : PlistObjectBase
	{
		private const string version = "1.0";

		private PlistObjectBase root;

		public PlistDocument ()
		{
		}

		public PlistDocument (PlistObjectBase root)
		{
			this.root = root;
		}

		public void LoadFromXmlFile (string path)
		{
			var settings = new XmlReaderSettings () {
				ProhibitDtd = false
			};
			using (var reader = XmlReader.Create (path, settings))
				LoadFromXml (reader);
		}

		public void LoadFromXml (string data)
		{
			var settings = new XmlReaderSettings () {
				CloseInput = true,
				ProhibitDtd = false
			};
			using (var reader = XmlReader.Create (new StringReader (data), settings)) {
				LoadFromXml (reader);
			}
		}

		public void LoadFromXml (XmlReader reader)
		{
			reader.ReadToDescendant ("plist");
			while (reader.Read () && reader.NodeType != XmlNodeType.Element);
			if (!reader.EOF)
				root = LoadFromNode (reader);
		}

		private PlistObjectBase LoadFromNode (XmlReader reader)
		{
			Debug.Assert (reader.NodeType == XmlNodeType.Element);
			bool isEmpty = reader.IsEmptyElement;
			switch (reader.LocalName) {
			case "dict":
				var dict = new PlistDictionary (true);
				if (!isEmpty) {
					if (reader.ReadToDescendant ("key"))
						dict = LoadDictionaryContents (reader, dict);
					reader.ReadEndElement ();
				}
				return dict;
				
			case "array":
				if (isEmpty)
					return new PlistArray ();
				
				//advance to first node
				reader.ReadStartElement ();
				while (reader.Read () && reader.NodeType != XmlNodeType.Element);
				
				// HACK: plist data in iPods is not even valid in some cases! Way to go Apple!
				// This hack checks to see if they really meant for this array to be a dict.
				if (reader.LocalName == "key") {
					var ret = LoadDictionaryContents (reader, new PlistDictionary (true));
					reader.ReadEndElement ();
					return ret;
				}
				
				var arr = new PlistArray ();
				do {
					if (reader.NodeType == XmlNodeType.Element) {
						var val = LoadFromNode (reader);
						if (val != null)
							arr.Add (val);
					}
				} while (reader.Read () && reader.NodeType != XmlNodeType.EndElement);
				reader.ReadEndElement ();
				return arr;
				
			case "string":
				return new PlistString (reader.ReadElementContentAsString ());
			case "integer":
				return new PlistInteger (reader.ReadElementContentAsInt ());
			case "real":
				return new PlistReal (reader.ReadElementContentAsDouble ());
			case "false":
				reader.ReadStartElement ();
				if (!isEmpty)
					reader.ReadEndElement ();
				return new PlistBoolean (false);
			case "true":
				reader.ReadStartElement ();
				if (!isEmpty)
					reader.ReadEndElement ();
				return new PlistBoolean (true);
			case "data":
				return new PlistData (reader.ReadElementContentAsString ());
			case "date":
				return new PlistDate (reader.ReadElementContentAsDateTime ());
			default:
				throw new XmlException (String.Format ("Plist Node `{0}' is not supported", reader.LocalName));
			}
		}

		private PlistDictionary LoadDictionaryContents (XmlReader reader, PlistDictionary dict)
		{
			Debug.Assert (reader.NodeType == XmlNodeType.Element && reader.LocalName == "key");
			while (!reader.EOF && reader.NodeType == XmlNodeType.Element) {
				string key = reader.ReadElementString ();
				while (reader.Read () && reader.NodeType != XmlNodeType.Element)
					if (reader.NodeType == XmlNodeType.EndElement)
						throw new Exception (String.Format ("No value found for key {0}", key));
				PlistObjectBase result = LoadFromNode (reader);
				if (result != null)
					dict.Add (key, result);
				reader.ReadToNextSibling ("key");
			}
			return dict;
		}
		
		public PlistObjectBase Root {
			get { return root; }
			set { root = value; }
		}

		public override void Write (System.Xml.XmlWriter writer)
		{
			writer.WriteStartDocument ();
			writer.WriteDocType ("plist", "-//Apple Computer//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);
			writer.WriteStartElement ("plist");
			writer.WriteAttributeString ("version", version);
			root.Write (writer);
			writer.WriteEndDocument ();
		}
	}
}
