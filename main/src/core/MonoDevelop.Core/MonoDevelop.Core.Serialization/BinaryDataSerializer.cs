//
// BinaryDataSerializer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

namespace MonoDevelop.Core.Serialization
{
	public class BinaryDataSerializer
	{
		DataSerializer serializer;
		
		public BinaryDataSerializer (DataContext ctx) : this (new DataSerializer (ctx))
		{	
		}
		
		public BinaryDataSerializer (DataSerializer serializer)
		{
			this.serializer = serializer;
		}
		
		public void Serialize (string file, object obj)
		{
			Serialize (file, obj, null);
		}
		
		public void Serialize (string file, object obj, Type type)
		{
			using (Stream s = File.OpenWrite (file)) {
				Serialize (s, obj, type);
			}
		}
		
		public void Serialize (Stream stream, object obj)
		{
			Serialize (stream, obj, null);
		}
		
		public void Serialize (Stream stream, object obj, Type type)
		{
			Serialize (new BinaryWriter (stream), obj, type);
		}
		
		public void Serialize (BinaryWriter writer, object obj)
		{
			Serialize (writer, obj, null);
		}
		
		public void Serialize (BinaryWriter writer, object obj, Type type)
		{
			DataNode data = serializer.Serialize (obj, type);
			BinaryConfigurationWriter.DefaultWriter.Write (writer, data);
		}
		
		public object Deserialize (string fileName, Type type)
		{
			using (Stream sr = File.OpenRead (fileName)) {
				return Deserialize (sr, type);
			}
		}
		
		public object Deserialize (Stream stream, Type type)
		{
			return Deserialize (new BinaryReader (stream), type);
		}
		
		public object Deserialize (BinaryReader reader, Type type)
		{
			DataNode data = BinaryConfigurationReader.DefaultReader.Read (reader);
			return serializer.Deserialize (type, data);
		}
		
		public SerializationContext SerializationContext {
			get { return serializer.SerializationContext; }
		}
	}
	
	public class BinaryConfigurationWriter
	{
		public static BinaryConfigurationWriter DefaultWriter = new BinaryConfigurationWriter ();
		
		public void Write (Stream stream, DataNode data)
		{
			BinaryWriter writer = new BinaryWriter (stream);
			Write (writer, data);
		}
		
		public void Write (BinaryWriter writer, DataNode data)
		{
			Write (writer, new Dictionary<string,int> (), data);
		}
		
		void Write (BinaryWriter writer, Dictionary<string, int> nameTable, DataNode data)
		{
			if (data is DataValue) {
				writer.Write ((byte)1);
				WriteString (writer, nameTable, data.Name);
				WriteString (writer, nameTable, ((DataValue)data).Value);
			} else if (data is DataItem) {
				writer.Write ((byte)2);
				WriteString (writer, nameTable, data.Name);
				DataItem item = (DataItem) data;
				writer.Write (item.ItemData.Count);
				foreach (DataNode cn in item.ItemData)
					Write (writer, nameTable, cn);
			}
		}
		
		void WriteString (BinaryWriter writer, Dictionary<string, int> nameTable, string str)
		{
			int id;
			if (!nameTable.TryGetValue (str, out id)) {
				id = nameTable.Count + 1;
				nameTable [str] = id;
				writer.Write (-id);
				writer.Write (str);
			} else {
				writer.Write (id);
			}
		}
	}
	
	public class BinaryConfigurationReader
	{
		public static BinaryConfigurationReader DefaultReader = new BinaryConfigurationReader ();

		public DataNode Read (Stream stream)
		{
			return Read (new BinaryReader (stream), new Dictionary<int,string> ());
		}
		
		public DataNode Read (BinaryReader reader)
		{
			return Read (reader, new Dictionary<int,string> ());
		}
		
		DataNode Read (BinaryReader reader, Dictionary<int,string> nameTable)
		{
			byte type = reader.ReadByte ();
			if (type == 1) {
				string name = ReadString (reader, nameTable);
				string value = ReadString (reader, nameTable);
				return new DataValue (name, value);
			}
			else if (type == 2) {
				DataItem item = new DataItem ();
				item.Name = ReadString (reader, nameTable);
				int count = reader.ReadInt32 ();
				while (count-- > 0)
					item.ItemData.Add (Read (reader, nameTable));
				return item;
			}
			else
				throw new InvalidOperationException ("Unknown node type: " + type);
		}
		
		string ReadString (BinaryReader reader, Dictionary<int,string> nameTable)
		{
			int id = reader.ReadInt32 ();
			// Negative means that it is the first appearance of the string,
			// so the string needs to be read
			if (id < 0) {
				string str = reader.ReadString ();
				nameTable [-id] = str;
				return str;
			} else {
				return nameTable [id];
			}
		}
	}
}
