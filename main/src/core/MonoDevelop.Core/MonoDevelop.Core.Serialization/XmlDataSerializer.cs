//
// XmlDataSerializer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Xml;
using System.Collections;

namespace MonoDevelop.Core.Serialization
{
	public class XmlDataSerializer
	{
		DataSerializer serializer;
		
		public bool StoreAllInElements { get; set; }
		
		public string Namespace { get; set; }

		public XmlDataSerializer (DataContext ctx) : this (new DataSerializer (ctx))
		{	
		}
		
		public XmlDataSerializer (DataSerializer serializer)
		{
			this.serializer = serializer;
		}
		
		public void Serialize (string file, object obj)
		{
			Serialize (file, obj, null);
		}
		
		public void Serialize (string file, object obj, Type type)
		{
			using (StreamWriter sw = new StreamWriter (file)) {
				Serialize (sw, obj, type);
			}
		}
		
		public void Serialize (TextWriter writer, object obj)
		{
			Serialize (writer, obj, null);
		}
		
		public void Serialize (TextWriter writer, object obj, Type type)
		{
			XmlTextWriter tw = new XmlTextWriter (writer);
			tw.Formatting = Formatting.Indented;
			Serialize (tw, obj, type);
		}
		
		public void Serialize (XmlWriter writer, object obj)
		{
			Serialize (writer, obj, null);
		}
		
		public void Serialize (XmlWriter writer, object obj, Type type)
		{
			DataNode data = serializer.Serialize (obj, type);
			XmlConfigurationWriter cw = new XmlConfigurationWriter ();
			cw.Namespace = Namespace;
			cw.StoreAllInElements = StoreAllInElements;
			cw.Write (writer, data);
		}

		public T Deserialize<T> (string fileName)
		{
			return (T)Deserialize (fileName, typeof (T));
		}

		public object Deserialize (string fileName, Type type)
		{
			using (StreamReader sr = new StreamReader (fileName)) {
				return Deserialize (sr, type);
			}
		}

		public T Deserialize<T> (TextReader reader)
		{
			return (T)Deserialize (reader, typeof (T));
		}

		public object Deserialize (TextReader reader, Type type)
		{
			return Deserialize (new XmlTextReader (reader), type);
		}

		public T Deserialize<T> (XmlReader reader)
		{
			return (T)Deserialize (reader, typeof (T));
		}

		public object Deserialize (XmlReader reader, Type type)
		{
			DataNode data = XmlConfigurationReader.DefaultReader.Read (reader);
			return serializer.Deserialize (type, data);
		}
		
		public SerializationContext SerializationContext {
			get { return serializer.SerializationContext; }
		}
	}
	
	public class XmlConfigurationWriter
	{
		public static XmlConfigurationWriter DefaultWriter = new XmlConfigurationWriter ();
		
		public bool StoreAllInElements = false;
		
		public string[] StoreInElementExceptions { get; set; }

		public string Namespace { get; set; }
		
		public void Write (XmlWriter writer, DataNode data)
		{
			if (data is DataValue)
				writer.WriteElementString (data.Name, ((DataValue)data).Value);
			else if (data is DataItem) {
				writer.WriteStartElement (data.Name, Namespace);
				WriteAttributes (writer, (DataItem) data);
				WriteChildren (writer, (DataItem) data);
				writer.WriteEndElement ();
			}
		}
		
		public XmlElement Write (XmlDocument doc, DataNode data)
		{
			XmlElement elem = doc.CreateElement (data.Name, Namespace);
			if (data is DataValue) {
				elem.InnerText = ((DataValue)data).Value;
			}
			else if (data is DataItem) {
				WriteAttributes (elem, (DataItem) data);
				WriteChildren (elem, (DataItem) data);
			}
			return elem;
		}
		
		protected virtual void WriteAttributes (XmlElement elem, DataItem item)
		{
			var defaultIsAtt = item.UniqueNames && !StoreAllInElements;
			foreach (DataNode data in item.ItemData) {
				DataValue val = data as DataValue;
				if (val != null && (StoreAsAttribute (val) || (defaultIsAtt && !val.StoreAsElementRequired)))
					WriteAttribute (elem, val.Name, val.Value);
			}
		}
		
		protected virtual void WriteAttributes (XmlWriter writer, DataItem item)
		{
			var defaultIsAtt = item.UniqueNames && !StoreAllInElements;
			foreach (DataNode data in item.ItemData) {
				DataValue val = data as DataValue;
				if (val != null && (StoreAsAttribute (val) || (defaultIsAtt && !val.StoreAsElementRequired)))
					WriteAttribute (writer, val.Name, val.Value);
			}
		}
		
		protected virtual void WriteAttribute (XmlElement elem, string name, string value)
		{
			elem.SetAttribute (name, value);
		}
		
		protected virtual void WriteAttribute (XmlWriter writer, string name, string value)
		{
			writer.WriteAttributeString (name, value);
		}
		
		protected virtual void WriteChildren (XmlWriter writer, DataItem item)
		{
			var defaultIsAtt = item.UniqueNames && !StoreAllInElements;
			foreach (DataNode data in item.ItemData) {
				DataValue dval = data as DataValue;
				if (dval == null || !(StoreAsAttribute (dval) || (defaultIsAtt && !dval.StoreAsElementRequired)))
					WriteChild (writer, data);
			}
		}
		
		protected virtual void WriteChildren (XmlElement elem, DataItem item)
		{
			var defaultIsAtt = item.UniqueNames && !StoreAllInElements;
			foreach (DataNode data in item.ItemData) {
				// DataDeletedNode is used for differential serialization. It should be ignored in this context.
				if (data is DataDeletedNode)
					continue;
				DataValue dval = data as DataValue;
				if (dval == null || !(StoreAsAttribute (dval) || (defaultIsAtt && !dval.StoreAsElementRequired)))
					WriteChild (elem, data);
			}
		}
		
		protected virtual void WriteChild (XmlElement elem, DataNode data)
		{
			elem.AppendChild (GetChildWriter (data).Write (elem.OwnerDocument, data));
		}
		
		protected virtual void WriteChild (XmlWriter writer, DataNode data)
		{
			GetChildWriter (data).Write (writer, data);
		}
		
		public virtual bool StoreAsAttribute (DataValue val)
		{
			if (val.StoreAsAttributeRequired)
				return true;
			else if (StoreAllInElements)
				return StoreInElementExceptions != null && ((IList)StoreInElementExceptions).Contains (val.Name);
			else
				return false;
		}
		
		protected virtual XmlConfigurationWriter GetChildWriter (DataNode data)
		{
			return this;
		}
	}
	
	public class XmlConfigurationReader
	{
		public static XmlConfigurationReader DefaultReader = new XmlConfigurationReader ();

		public DataNode Read (XmlReader reader)
		{
			DataItem item = new DataItem (); 
			item.UniqueNames = false;
			reader.MoveToContent ();
			string name = reader.LocalName;
			item.Name = name;
			
			while (reader.MoveToNextAttribute ()) {
				if (reader.LocalName == "xmlns")
					continue;
				DataNode data = ReadAttribute (reader.LocalName, reader.Value);
				if (data != null) {
					DataValue val = data as DataValue;
					if (val != null)
						val.StoreAsAttribute = true;
					item.ItemData.Add (data);
				}
			}
			
			reader.MoveToElement ();
			if (reader.IsEmptyElement) {
				reader.Skip ();
				return item;
			}
			
			reader.ReadStartElement ();
			
			string text = "";
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					DataNode data = ReadChild (reader, item);
					if (data != null) item.ItemData.Add (data);
				} else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.Whitespace) {
					text += reader.Value;
					reader.Skip ();
				} else {
					reader.Skip ();
				}
			}
			
			reader.ReadEndElement ();
			
			if (!item.HasItemData && text != "")
				return new DataValue (name, text); 

			return item;
		}

		public DataNode Read (XmlElement elem)
		{
			DataItem item = new DataItem (); 
			item.UniqueNames = false;

			item.Name = elem.LocalName;
			
			foreach (XmlAttribute att in elem.Attributes) {
				if (att.LocalName == "xmlns")
					continue;
				DataNode data = ReadAttribute (att.LocalName, att.Value);
				if (data != null) {
					DataValue val = data as DataValue;
					if (val != null)
						val.StoreAsAttribute = true;
					item.ItemData.Add (data);
				}
			}

			string text = "";
			
			foreach (XmlNode node in elem.ChildNodes) {
				if (node.NodeType == XmlNodeType.Element) {
					DataNode data = ReadChild ((XmlElement)node, item);
					if (data != null) item.ItemData.Add (data);
				} else if (node.NodeType == XmlNodeType.Text) {
					text += ((XmlText)node).Value;
				}
			}
			
			if (!item.HasItemData && text != "")
				return new DataValue (item.Name, text); 

			return item;
		}
		
		protected bool MoveToNextElement (XmlReader reader)
		{
			reader.MoveToContent ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element)
					return true;
				reader.Skip ();
			}
			return false;
		}
		
		protected virtual DataNode ReadAttribute (string name, string value)
		{
			return new DataValue (name, value);
		}
		
		protected virtual DataNode ReadChild (XmlElement elem, DataItem parent)
		{
			return GetChildReader (parent).Read (elem);
		}
		
		protected virtual DataNode ReadChild (XmlReader reader, DataItem parent)
		{
			return GetChildReader (parent).Read (reader);
		}
		
		protected virtual XmlConfigurationReader GetChildReader (DataItem parent)
		{
			return this;
		}
	}
}
