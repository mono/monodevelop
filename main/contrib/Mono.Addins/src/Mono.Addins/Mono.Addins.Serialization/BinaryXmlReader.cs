//
// BinaryXmlReader.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.IO;

namespace Mono.Addins.Serialization
{
	internal class BinaryXmlReader
	{
		BinaryReader reader;
		
		internal const byte TagBeginElement = 1;
		internal const byte TagEndElement = 2;
		internal const byte TagValue = 4;
		
		internal const byte TagObject = 5;
		internal const byte TagObjectArray = 6;
		internal const byte TagObjectDictionary = 7;
		internal const byte TagObjectNull = 8;
		
		byte currentType;
		string currentName;
		ArrayList stringTable = new ArrayList ();
		BinaryXmlTypeMap typeMap;
		object contextData;
		bool ignoreDesc;
		
		public BinaryXmlReader (Stream stream, BinaryXmlTypeMap typeMap)
		{
			reader = new BinaryReader (stream);
			this.typeMap = typeMap;
			ReadNext ();
		}
		
		public BinaryXmlTypeMap TypeMap {
			get { return typeMap; }
			set { typeMap = value; }
		}
		
		public object ContextData {
			get { return contextData; }
			set { contextData = value; }
		}
		
		// Returns 'true' if description data must be ignored when reading the contents of a file
		public bool IgnoreDescriptionData {
			get { return ignoreDesc; }
			set { ignoreDesc = value; }
		}
		
		void ReadNext ()
		{
			int b = reader.BaseStream.ReadByte ();
			if (b == -1) {
				currentType = TagBeginElement;
				return;
			}
			currentType = (byte) b;
			if (currentType == TagBeginElement || currentType == TagValue)
				currentName = ReadString ();
		}
		
		string ReadString ()
		{
			// The first integer means:
			// >=0: string of the specified length
			// -1: null string
			// <-1: a string from the string table
			
			int len = reader.ReadInt32 ();
			if (len == -1)
				return null;
			if (len < -1)
				return (string) stringTable [-(len + 2)];

			byte[] bytes = new byte [len];
			int n = 0;
			while (n < len)
				n += reader.Read (bytes, n, len - n); 
			string s = Encoding.UTF8.GetString (bytes);
			stringTable.Add (s);
			return s;
		}
		
		public string LocalName {
			get { return currentName; }
		}
		
		public bool IsElement {
			get { return currentType == TagBeginElement; }
		}
		
		public bool IsValue {
			get { return currentType == TagValue; }
		}
		
		TypeCode ReadValueType (TypeCode type)
		{
			if (currentType != TagValue)
				throw new InvalidOperationException ("Reader not positioned on a value.");
			TypeCode t = (TypeCode) reader.ReadByte ();
			if (t != type && type != TypeCode.Empty)
				throw new InvalidOperationException ("Invalid value type. Expected " + type + ", found " + t);
			return t;
		}
		
		public string ReadStringValue (string name)
		{
			if (!SkipToValue (name))
				return null;
			return ReadStringValue ();
		}
		
		public string ReadStringValue ()
		{
			if (currentType != TagValue)
				throw new InvalidOperationException ("Reader not positioned on a value.");
				
			TypeCode t = (TypeCode) reader.ReadByte ();
			if (t == TypeCode.Empty) {
				ReadNext ();
				return null;
			}
			if (t != TypeCode.String)
				throw new InvalidOperationException ("Invalid value type. Expected String, found " + t);
			
			string s = ReadString ();
			ReadNext ();
			return s;
		}
		
		public bool ReadBooleanValue (string name)
		{
			if (!SkipToValue (name))
				return false;
			return ReadBooleanValue ();
		}
		
		public bool ReadBooleanValue ()
		{
			ReadValueType (TypeCode.Boolean);
			bool value = reader.ReadBoolean ();
			ReadNext ();
			return value;
		}
		
		public char ReadCharValue (string name)
		{
			if (!SkipToValue (name))
				return (char)0;
			return ReadCharValue ();
		}
		
		public char ReadCharValue ()
		{
			ReadValueType (TypeCode.Char);
			char value = reader.ReadChar ();
			ReadNext ();
			return value;
		}
		
		public byte ReadByteValue (string name)
		{
			if (!SkipToValue (name))
				return (byte)0;
			return ReadByteValue ();
		}
		
		public byte ReadByteValue ()
		{
			ReadValueType (TypeCode.Byte);
			byte value = reader.ReadByte ();
			ReadNext ();
			return value;
		}
		
		public short ReadInt16Value (string name)
		{
			if (!SkipToValue (name))
				return (short)0;
			return ReadInt16Value ();
		}
		
		public short ReadInt16Value ()
		{
			ReadValueType (TypeCode.Int16);
			short value = reader.ReadInt16 ();
			ReadNext ();
			return value;
		}
		
		public int ReadInt32Value (string name)
		{
			if (!SkipToValue (name))
				return 0;
			return ReadInt32Value ();
		}
		
		public int ReadInt32Value ()
		{
			ReadValueType (TypeCode.Int32);
			int value = reader.ReadInt32 ();
			ReadNext ();
			return value;
		}
		
		public long ReadInt64Value (string name)
		{
			if (!SkipToValue (name))
				return (long)0;
			return ReadInt64Value ();
		}
		
		public long ReadInt64Value ()
		{
			ReadValueType (TypeCode.Int64);
			long value = reader.ReadInt64 ();
			ReadNext ();
			return value;
		}
		
		public DateTime ReadDateTimeValue (string name)
		{
			if (!SkipToValue (name))
				return DateTime.MinValue;
			return ReadDateTimeValue ();
		}
		
		public DateTime ReadDateTimeValue ()
		{
			ReadValueType (TypeCode.DateTime);
			DateTime value = new DateTime (reader.ReadInt64 ());
			ReadNext ();
			return value;
		}
		
		public object ReadValue (string name)
		{
			if (!SkipToValue (name))
				return null;
			return ReadValue ();
		}
		
		public object ReadValue ()
		{
			object res = ReadValueInternal ();
			ReadNext ();
			return res;
		}
		
		public object ReadValue (string name, object targetInstance)
		{
			if (!SkipToValue (name))
				return null;
			return ReadValue (targetInstance);
		}
		
		public object ReadValue (object targetInstance)
		{
			TypeCode t = (TypeCode) reader.ReadByte ();
			if (t == TypeCode.Empty) {
				ReadNext ();
				return null;
			}
			if (t != TypeCode.Object)
				throw new InvalidOperationException ("Invalid value type. Expected Object, found " + t);
			
			object res = ReadObject (targetInstance);
			ReadNext ();
			return res;
		}
		
		object ReadValueInternal ()
		{
			TypeCode t = (TypeCode) reader.ReadByte ();
			if (t == TypeCode.Empty)
				return null;
			return ReadValueInternal (t);
		}
		
		object ReadValueInternal (TypeCode t)
		{
			object res;
			switch (t) {
				case TypeCode.Boolean: res = reader.ReadBoolean (); break;
				case TypeCode.Char: res = reader.ReadChar (); break;
				case TypeCode.SByte: res = reader.ReadSByte (); break;
				case TypeCode.Byte: res = reader.ReadByte (); break;
				case TypeCode.Int16: res = reader.ReadInt16 (); break;
				case TypeCode.UInt16: res = reader.ReadUInt16 (); break;
				case TypeCode.Int32: res = reader.ReadInt32 (); break;
				case TypeCode.UInt32: res = reader.ReadUInt32 (); break;
				case TypeCode.Int64: res = reader.ReadInt64 (); break;
				case TypeCode.UInt64: res = reader.ReadUInt64 (); break;
				case TypeCode.Single: res = reader.ReadSingle (); break;
				case TypeCode.Double: res = reader.ReadDouble (); break;
				case TypeCode.DateTime: res = new DateTime (reader.ReadInt64 ()); break;
				case TypeCode.String: res = ReadString (); break;
				case TypeCode.Object: res = ReadObject (null); break;
				case TypeCode.Empty: res = null; break;
				default:
					throw new InvalidOperationException ("Unexpected value type: " + t);
			}
			return res;
		}
		
		bool SkipToValue (string name)
		{
			do {
				if ((currentType == TagBeginElement || currentType == TagValue) && currentName == name)
					return true;
				if (EndOfElement)
					return false;
				Skip ();
			} while (true);
		}

		public void ReadBeginElement ()
		{
			if (currentType != TagBeginElement)
				throw new InvalidOperationException ("Reader not positioned on an element.");
			ReadNext ();
		}
		
		public void ReadEndElement ()
		{
			if (currentType != TagEndElement)
				throw new InvalidOperationException ("Reader not positioned on an element.");
			ReadNext ();
		}
		
		public bool EndOfElement {
			get { return currentType == TagEndElement; }
		}
		
		public void Skip ()
		{
			if (currentType == TagValue)
				ReadValue ();
			else if (currentType == TagEndElement)
				ReadNext ();
			else if (currentType == TagBeginElement) {
				ReadNext ();
				while (!EndOfElement)
					Skip ();
				ReadNext ();
			}
		}
		
		object ReadObject (object targetInstance)
		{
			byte ot = reader.ReadByte ();
			if (ot == TagObjectNull) {
				return null;
			}
			else if (ot == TagObject) {
				string tname = ReadString ();
				IBinaryXmlElement ob;
				if (targetInstance != null) {
					ob = targetInstance as IBinaryXmlElement;
					if (ob == null)
						throw new InvalidOperationException ("Target instance has an invalid type. Expected an IBinaryXmlElement implementation.");
				} else {
					ob = typeMap.CreateObject (tname);
				}
				ReadNext ();
				ob.Read (this);
				while (currentType != TagEndElement)
					Skip ();
				return ob;
			}
			else if (ot == TagObjectArray) {
				TypeCode tc = (TypeCode) reader.ReadByte ();
				int len = reader.ReadInt32 ();
				if (targetInstance != null) {
					IList list = targetInstance as IList;
					if (list == null)
						throw new InvalidOperationException ("Target instance has an invalid type. Expected an IList implementation.");
					for (int n=0; n<len; n++)
						list.Add (ReadValueInternal ());
					return list;
				}
				else {
					Array obs = CreateArray (tc, len);
					for (int n=0; n<len; n++)
						obs.SetValue (ReadValueInternal (), n);
					return obs;
				}
			}
			else if (ot == TagObjectDictionary) {
				int len = reader.ReadInt32 ();
				IDictionary table;
				if (targetInstance != null) {
					table = targetInstance as IDictionary;
					if (table == null)
						throw new InvalidOperationException ("Target instance has an invalid type. Expected an IDictionary implementation.");
				} else {
					table = new Hashtable ();
				}
				for (int n=0; n<len; n++) {
					object key = ReadValueInternal ();
					object val = ReadValueInternal ();
					table [key] = val;
				}
				return table;
			}
			else
				throw new InvalidOperationException ("Unknown object type tag: " + ot);
		}
		
		Array CreateArray (TypeCode t, int len)
		{
			switch (t) {
				case TypeCode.Boolean: return new bool [len];
				case TypeCode.Char: return new Char [len];
				case TypeCode.SByte: return new SByte [len];
				case TypeCode.Byte: return new Byte [len];
				case TypeCode.Int16: return new Int16 [len];
				case TypeCode.UInt16: return new UInt16 [len];
				case TypeCode.Int32: return new Int32 [len];
				case TypeCode.UInt32: return new UInt32 [len];
				case TypeCode.Int64: return new Int64 [len];
				case TypeCode.UInt64: return new UInt64 [len];
				case TypeCode.Single: return new Single [len];
				case TypeCode.Double: return new Double [len];
				case TypeCode.DateTime: return new DateTime [len];
				case TypeCode.String: return new String [len];
				case TypeCode.Object: return new Object [len];
				default:
					throw new InvalidOperationException ("Unexpected value type: " + t);
			}
		}
		
		const int IndSize = 2;
		
		public static void DumpFile (string file)
		{
			Console.WriteLine ("FILE: " + file);
			using (Stream s = File.OpenRead (file)) {
				BinaryXmlReader r = new BinaryXmlReader (s, new BinaryXmlTypeMap ());
				r.Dump (0);
			}
		}
		
		public void Dump (int ind)
		{
			if (currentType == TagValue) {
				Console.Write (new string (' ', ind) + LocalName + ": ");
				DumpValue (ind);
				Console.WriteLine ();
			}
			else if (currentType == TagBeginElement) {
				string name = LocalName;
				Console.WriteLine (new string (' ', ind) + "<" + name + ">");
				DumpElement (ind + IndSize);
				Console.WriteLine (new string (' ', ind) + "</" + name + ">");
			}
		}
		
		public void DumpElement (int ind)
		{
			ReadNext ();
			while (currentType != TagEndElement) {
				Dump (ind + IndSize);
				ReadNext ();
			}
		}
		
		void DumpValue (int ind)
		{
			TypeCode t = (TypeCode) reader.ReadByte ();
			if (t != TypeCode.Object) {
				object ob = ReadValueInternal (t);
				if (ob == null) ob = "(null)";
				Console.Write (ob);
			} else {
				byte ot = reader.ReadByte ();
				switch (ot) {
					case TagObjectNull: {
						Console.Write ("(null)");
						break;
					}
					case TagObject: {
						string tname = ReadString ();
						Console.WriteLine ("(" + tname + ")");
						DumpElement (ind + IndSize);
						break;
					}
					case TagObjectArray: {
						TypeCode tc = (TypeCode) reader.ReadByte ();
						int len = reader.ReadInt32 ();
						Console.WriteLine ("(" + tc + "[" + len + "])");
						for (int n=0; n<len; n++) {
							Console.Write (new string (' ', ind + IndSize) + n + ": ");
							DumpValue (ind + IndSize*2);
							Console.WriteLine ();
						}
						break;
					}
					case TagObjectDictionary: {
						int len = reader.ReadInt32 ();
						Console.WriteLine ("(IDictionary)");
						for (int n=0; n<len; n++) {
							Console.Write (new string (' ', ind + IndSize) + "key: ");
							DumpValue (ind + IndSize*2);
							Console.WriteLine ();
							Console.Write (new string (' ', ind + IndSize) + "val: ");
							DumpValue (ind + IndSize*2);
							Console.WriteLine ();
						}
						break;
					}
					default:
						throw new InvalidOperationException ("Invalid object tag: " + ot);
				}
			}
		}
	}
}
