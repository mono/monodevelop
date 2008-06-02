//
// DomPersistence.cs 
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MonoDevelop.Projects.Dom
{
	internal static class DomPersistence
	{
		internal static DomLocation ReadLocation (BinaryReader reader, INameDecoder nameTable)
		{
			if (ReadNull (reader)) 
				return DomLocation.Empty;
			
			int line   = reader.ReadInt32 ();
			int column = reader.ReadInt32 ();
			
			return new DomLocation (line, column);
		}
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, DomLocation location)
		{
			if (WriteNull (writer, location)) 
				return;
			writer.Write (location.Line);
			writer.Write (location.Column);
		}
		
		internal static DomRegion ReadRegion (BinaryReader reader, INameDecoder nameTable)
		{
			if (ReadNull (reader)) 
				return DomRegion.Empty;
			
			int startLine   = reader.ReadInt32 ();
			int startColumn = reader.ReadInt32 ();
			int endLine     = reader.ReadInt32 ();
			int endColumn   = reader.ReadInt32 ();
			
			return new DomRegion (startLine, startColumn, endLine, endColumn);
		}
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, DomRegion region)
		{
			if (WriteNull (writer, region)) 
				return;
			writer.Write (region.Start.Line);
			writer.Write (region.Start.Column);
			writer.Write (region.End.Line);
			writer.Write (region.End.Column);
		}
		
		internal static DomField ReadField (BinaryReader reader, INameDecoder nameTable)
		{
			DomField result = new DomField ();
			ReadMemberInformation (reader, nameTable, result);
			result.ReturnType = ReadReturnType (reader, nameTable);
			return result;
		}
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, IField field)
		{
			Debug.Assert (field != null);
			WriteMemberInformation (writer, nameTable, field);
			Write (writer, nameTable, field.ReturnType);
		}
		
		internal static DomReturnType ReadReturnType (BinaryReader reader, INameDecoder nameTable)
		{
			// TODO: Attributes
			string name       = ReadString (reader, nameTable);
			bool   isNullable = reader.ReadBoolean ();
			uint    arguments  = reader.ReadUInt32 ();
			List<IReturnType> parameters = new List<IReturnType> ();
			while (arguments-- > 0) {
				parameters.Add (ReadReturnType (reader, nameTable));
			}
			
			return new DomReturnType (name, isNullable, parameters);
		}
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, IReturnType returnType)
		{
			Debug.Assert (returnType != null);
			
			WriteString (returnType.Name, writer, nameTable);
			writer.Write (returnType.IsNullable);
			if (returnType.TypeParameters == null) {
				writer.Write ((uint)0);
				return;
			}
			writer.Write ((uint)returnType.TypeParameters.Count);
			foreach (DomReturnType param in returnType.TypeParameters) {
				Write (writer, nameTable, param);
			}
		}
		
		internal static DomMethod ReadMethod (BinaryReader reader, INameDecoder nameTable)
		{
			DomMethod result = new DomMethod ();
			ReadMemberInformation (reader, nameTable, result);
			result.ReturnType    = ReadReturnType (reader, nameTable);
			result.IsConstructor = reader.ReadBoolean ();
			uint    arguments  = reader.ReadUInt32 ();
			List<IParameter> parameters = new List<IParameter> ();
			while (arguments-- > 0) {
				result.Add (ReadParameter (reader, nameTable));
			}
			return result;
		}
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, IMethod method)
		{
			Debug.Assert (method != null);
			WriteMemberInformation (writer, nameTable, method);
			Write (writer, nameTable, method.ReturnType);
			writer.Write (method.IsConstructor);
			writer.Write (method.Parameters.Count);
			foreach (IParameter param in method.Parameters) {
				Write (writer, nameTable, param);
			}
		}
		
		internal static DomParameter ReadParameter (BinaryReader reader, INameDecoder nameTable)
		{
			DomParameter result = new DomParameter ();
			
			result.Name               = ReadString (reader, nameTable);
			result.ParameterModifiers = (ParameterModifiers)reader.ReadUInt32();
			result.ReturnType         = ReadReturnType (reader, nameTable);
			result.Location           = ReadLocation (reader, nameTable);
			
			return result;
		}
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, IParameter parameter)
		{
			Debug.Assert (parameter != null);
			WriteString (parameter.Name, writer, nameTable);
			writer.Write ((uint)parameter.ParameterModifiers);
			Write (writer, nameTable, parameter.ReturnType);
			Write (writer, nameTable, parameter.Location);
		}
		
		internal static DomProperty ReadProperty (BinaryReader reader, INameDecoder nameTable)
		{
			DomProperty result = new DomProperty ();
			ReadMemberInformation (reader, nameTable, result);
			result.ReturnType = ReadReturnType (reader, nameTable);
			result.IsIndexer  = reader.ReadBoolean ();
			bool hasGet = ReadNull (reader);
			if (hasGet)
				result.GetMethod = ReadMethod (reader, nameTable);
			bool hasSet = ReadNull (reader);
			if (hasSet)
				result.SetMethod = ReadMethod (reader, nameTable);
			return result;
		}
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, IProperty property)
		{
			Debug.Assert (property != null);
			WriteMemberInformation (writer, nameTable, property);
			Write (writer, nameTable, property.ReturnType);
			WriteNull (writer, property.GetMethod);
			if (property.GetMethod != null) 
				Write (writer, nameTable, property.GetMethod);
			WriteNull (writer, property.SetMethod);
			if (property.SetMethod != null) 
				Write (writer, nameTable, property.SetMethod);
		}
		
		internal static DomEvent ReadEvent (BinaryReader reader, INameDecoder nameTable)
		{
			DomEvent result = new DomEvent ();
			ReadMemberInformation (reader, nameTable, result);
			result.ReturnType = ReadReturnType (reader, nameTable);
			bool hasAdd = ReadNull (reader);
			if (hasAdd)
				result.AddMethod = ReadMethod (reader, nameTable);
			bool hasRemove = ReadNull (reader);
			if (hasRemove)
				result.RemoveMethod = ReadMethod (reader, nameTable);
			bool hasRaise = ReadNull (reader);
			if (hasRaise)
				result.RaiseMethod = ReadMethod (reader, nameTable);
			return result;
		}
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, IEvent evt)
		{
			Debug.Assert (evt != null);
			WriteMemberInformation (writer, nameTable, evt);
			Write (writer, nameTable, evt.ReturnType);
			WriteNull (writer, evt.AddMethod);
			if (evt.AddMethod != null) 
				Write (writer, nameTable, evt.AddMethod);
			WriteNull (writer, evt.RemoveMethod);
			if (evt.RemoveMethod != null) 
				Write (writer, nameTable, evt.RemoveMethod);
			WriteNull (writer, evt.RaiseMethod);
			if (evt.RaiseMethod != null) 
				Write (writer, nameTable, evt.RaiseMethod);
		}
		
		
		internal static DomType ReadType (BinaryReader reader, INameDecoder nameTable)
		{
			DomType result = new DomType ();
			ReadMemberInformation (reader, nameTable, result);
			result.Namespace = ReadString (reader, nameTable);
			result.ClassType = (ClassType)reader.ReadUInt32();
			result.BaseType  = ReadReturnType (reader, nameTable);
			// implemented interfaces
			uint count = reader.ReadUInt32 ();
			while (count-- > 0) {
				result.Add (ReadReturnType (reader, nameTable));
			}
			
			// innerTypes
			count = reader.ReadUInt32 ();
			while (count-- > 0) {
				result.Add (ReadType (reader, nameTable));
			}
			
			// fields
			count = reader.ReadUInt32 ();
			while (count-- > 0) {
				result.Add (ReadField (reader, nameTable));
			}
			
			// properties
			count = reader.ReadUInt32 ();
			while (count-- > 0) {
				result.Add (ReadProperty (reader, nameTable));
			}
			
			// methods
			count = reader.ReadUInt32 ();
			while (count-- > 0) {
				result.Add (ReadMethod (reader, nameTable));
			}
			
			// events
			count = reader.ReadUInt32 ();
			while (count-- > 0) {
				result.Add (ReadEvent (reader, nameTable));
			}
			return result;
		}
		
		internal static void Write (BinaryWriter writer, INameEncoder nameTable, IType type)
		{
			Debug.Assert (type != null);
			WriteMemberInformation (writer, nameTable, type);
			WriteString (type.Namespace, writer, nameTable);
			writer.Write ((uint)type.ClassType);
			Write (writer, nameTable, type.BaseType);
			writer.Write (GetCount (type.ImplementedInterfaces));
			foreach (IReturnType iface in type.ImplementedInterfaces) {
				Write (writer, nameTable, iface);
			}
			writer.Write (GetCount (type.Fields));
			foreach (IField field in type.Fields) {
				Write (writer, nameTable, field);
			}
			writer.Write (GetCount (type.Properties));
			foreach (IProperty property in type.Properties) {
				Write (writer, nameTable, property);
			}
			writer.Write (GetCount (type.Methods));
			foreach (IMethod method in type.Methods) {
				Write (writer, nameTable, method);
			}
			writer.Write (GetCount (type.Events));
			foreach (IEvent evt in type.Events) {
				Write (writer, nameTable, evt);
			}
		}
		
		static uint GetCount<T> (IEnumerable<T> list)
		{
			uint result = 0;
			foreach (T o in list) {
				result++;
			}
			return result;
		}
		
		
#region Helper methods
		static void WriteMemberInformation (BinaryWriter writer, INameEncoder nameTable, IMember member)
		{
			// TODO: Attributes
			WriteString (member.Name, writer, nameTable);
			WriteString (member.Documentation, writer, nameTable);
			writer.Write ((uint)member.Modifiers);
			Write (writer, nameTable, member.Location);
		}
		static void ReadMemberInformation (BinaryReader reader, INameDecoder nameTable, IMember member)
		{
			// TODO: Attributes
			member.Name          = ReadString (reader, nameTable);
			member.Documentation = ReadString (reader, nameTable);
			member.Modifiers     = (Modifiers)reader.ReadUInt32();
			member.Location      = ReadLocation (reader, nameTable);
		}
		
		static void WriteString (string s, BinaryWriter writer, INameEncoder nameTable)
		{
			if (s == null)
				writer.Write (-2);
			else {
				int id = nameTable.GetStringId (s);
				writer.Write (id);
				if (id == -1)
					writer.Write (s);
			}
		}
		
		static string ReadString (BinaryReader reader, INameDecoder nameTable)
		{
			int id = reader.ReadInt32 ();
			if (id == -1)
				return reader.ReadString ();
			else if (id == -2)
				return null;
			else
				return nameTable.GetStringValue (id);
		}
		
		static bool ReadNull (BinaryReader reader)
		{
			return reader.ReadBoolean ();
		}
		
		static bool WriteNull (BinaryWriter writer, DomLocation location)
		{
			writer.Write (location == DomLocation.Empty);
			return location == DomLocation.Empty;
		}
		static bool WriteNull (BinaryWriter writer, DomRegion region)
		{
			writer.Write (region == DomRegion.Empty);
			return region == DomRegion.Empty;
		}
		static bool WriteNull (BinaryWriter writer, object ob)
		{
			writer.Write (ob == null);
			return ob == null;
		}
		
#endregion
	}
}
