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

namespace MonoDevelop.Projects.Dom.Serialization
{
	public static class DomPersistence
	{
		public static DomLocation ReadLocation (BinaryReader reader, INameDecoder nameTable)
		{
			if (ReadNull (reader)) 
				return DomLocation.Empty;
			
			int line   = reader.ReadInt32 ();
			int column = reader.ReadInt32 ();
			
			return new DomLocation (line, column);
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, DomLocation location)
		{
			if (WriteNull (writer, location)) 
				return;
			writer.Write (location.Line);
			writer.Write (location.Column);
		}
		
		public static DomRegion ReadRegion (BinaryReader reader, INameDecoder nameTable)
		{
			if (ReadNull (reader)) 
				return DomRegion.Empty;
			
			int startLine   = reader.ReadInt32 ();
			int startColumn = reader.ReadInt32 ();
			int endLine     = reader.ReadInt32 ();
			int endColumn   = reader.ReadInt32 ();
			
			return new DomRegion (startLine, startColumn, endLine, endColumn);
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, DomRegion region)
		{
			if (WriteNull (writer, region)) 
				return;
			writer.Write (region.Start.Line);
			writer.Write (region.Start.Column);
			writer.Write (region.End.Line);
			writer.Write (region.End.Column);
		}
		
		public static DomField ReadField (BinaryReader reader, INameDecoder nameTable)
		{
			DomField result = new DomField ();
			ReadMemberInformation (reader, nameTable, result);
			result.ReturnType = ReadReturnType (reader, nameTable);
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IField field)
		{
			Debug.Assert (field != null);
			WriteMemberInformation (writer, nameTable, field);
			Write (writer, nameTable, field.ReturnType);
		}
		
		public static DomReturnType ReadReturnType (BinaryReader reader, INameDecoder nameTable)
		{
			if (ReadNull (reader))
				return null;
			
			string name       = ReadString (reader, nameTable);
			int  pointerNesting = reader.ReadInt32 ();
			bool isNullable = reader.ReadBoolean ();
			bool isByRef = reader.ReadBoolean ();
			int  arrayDimensions = reader.ReadInt32 ();
			uint arguments  = reader.ReadUInt32 ();
			List<IReturnType> parameters = new List<IReturnType> ();
			
			while (arguments-- > 0) {
				parameters.Add (ReadReturnType (reader, nameTable));
			}
			
			DomReturnType result = new DomReturnType (name, isNullable, parameters);
			result.PointerNestingLevel = pointerNesting;
			result.IsByRef = isByRef;
			result.ArrayDimensions = arrayDimensions;
			return result;
		}
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IReturnType returnType)
		{
			if (WriteNull (writer, returnType))
				return;
			WriteString (returnType.FullName, writer, nameTable);
			writer.Write (returnType.PointerNestingLevel);
			writer.Write (returnType.IsNullable);
			writer.Write (returnType.IsByRef);
			writer.Write (returnType.ArrayDimensions);
			if (returnType.GenericArguments == null) {
				writer.Write ((uint)0);
				return;
			}
			writer.Write ((uint)returnType.GenericArguments.Count);
			foreach (DomReturnType param in returnType.GenericArguments) {
				Write (writer, nameTable, param);
			}
		}
		
		public static DomMethod ReadMethod (BinaryReader reader, INameDecoder nameTable)
		{
			DomMethod result = new DomMethod ();
			ReadMemberInformation (reader, nameTable, result);
			result.BodyRegion = ReadRegion (reader, nameTable);
			result.ReturnType = ReadReturnType (reader, nameTable);
			result.MethodModifier = (MethodModifier)reader.ReadInt32 ();
			uint arguments = reader.ReadUInt32 ();
			
			while (arguments-- > 0) {
				result.Add (ReadParameter (reader, nameTable));
			}
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IMethod method)
		{
			Debug.Assert (method != null);
			WriteMemberInformation (writer, nameTable, method);
			Write (writer, nameTable, method.BodyRegion);
			Write (writer, nameTable, method.ReturnType);
			writer.Write ((int)method.MethodModifier);
			if (method.Parameters == null) {
				writer.Write (0);
			} else {
				writer.Write (method.Parameters.Count);
				foreach (IParameter param in method.Parameters) {
					Write (writer, nameTable, param);
				}
			}
		}
		
		public static DomParameter ReadParameter (BinaryReader reader, INameDecoder nameTable)
		{
			DomParameter result = new DomParameter ();
			
			result.Name               = ReadString (reader, nameTable);
			result.ParameterModifiers = (ParameterModifiers)reader.ReadUInt32();
			result.ReturnType         = ReadReturnType (reader, nameTable);
			result.Location           = ReadLocation (reader, nameTable);
			
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IParameter parameter)
		{
			Debug.Assert (parameter != null);
			WriteString (parameter.Name, writer, nameTable);
			writer.Write ((uint)parameter.ParameterModifiers);
			Write (writer, nameTable, parameter.ReturnType);
			Write (writer, nameTable, parameter.Location);
		}
		
		public static DomProperty ReadProperty (BinaryReader reader, INameDecoder nameTable)
		{
			DomProperty result = new DomProperty ();
			ReadMemberInformation (reader, nameTable, result);
 			result.BodyRegion = ReadRegion (reader, nameTable);
			result.ReturnType = ReadReturnType (reader, nameTable);
			result.PropertyModifier = (PropertyModifier)reader.ReadInt32 ();
			result.GetRegion = ReadRegion (reader, nameTable);
			result.SetRegion = ReadRegion (reader, nameTable);
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IProperty property)
		{
			Debug.Assert (property != null);
			WriteMemberInformation (writer, nameTable, property);
			Write (writer, nameTable, property.BodyRegion);
			Write (writer, nameTable, property.ReturnType);
			writer.Write ((int)property.PropertyModifier);
			Write (writer, nameTable, property.GetRegion);
			Write (writer, nameTable, property.SetRegion);
		}
		
		public static DomEvent ReadEvent (BinaryReader reader, INameDecoder nameTable)
		{
			DomEvent result = new DomEvent ();
			ReadMemberInformation (reader, nameTable, result);
			result.ReturnType = ReadReturnType (reader, nameTable);
			if (!ReadNull (reader))
				result.AddMethod = ReadMethod (reader, nameTable);
			if (!ReadNull (reader))
				result.RemoveMethod = ReadMethod (reader, nameTable);
			if (!ReadNull (reader))
				result.RaiseMethod = ReadMethod (reader, nameTable);
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IEvent evt)
		{
			Debug.Assert (evt != null);
			WriteMemberInformation (writer, nameTable, evt);
			Write (writer, nameTable, evt.ReturnType);
			if (!WriteNull (writer, evt.AddMethod)) 
				Write (writer, nameTable, evt.AddMethod);
			if (!WriteNull (writer, evt.RemoveMethod)) 
				Write (writer, nameTable, evt.RemoveMethod);
			if (!WriteNull (writer, evt.RaiseMethod)) 
				Write (writer, nameTable, evt.RaiseMethod);
		}
		
		
		public static DomType ReadType (BinaryReader reader, INameDecoder nameTable)
		{
			DomType result = new DomType ();
			ReadMemberInformation (reader, nameTable, result);
//			bool verbose = result.Name == "CopyDelegate";
//			if (verbose) System.Console.WriteLine("read type:" + result.Name);

			result.BodyRegion = ReadRegion (reader, nameTable);
			string compilationUnitFileName = ReadString (reader, nameTable);
			result.CompilationUnit = new CompilationUnit (compilationUnitFileName);
			
			result.Namespace = ReadString (reader, nameTable);
			result.ClassType = (ClassType)reader.ReadUInt32();
			result.BaseType  = ReadReturnType (reader, nameTable);
			
			// implemented interfaces
			long count = reader.ReadUInt32 ();
//			if (verbose) System.Console.WriteLine("impl. interfaces:" + count);
			while (count-- > 0) {
				result.AddInterfaceImplementation (ReadReturnType (reader, nameTable));
			}
			
			// innerTypes
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = reader.ReadUInt32 ();
//			if (verbose) System.Console.WriteLine("inner types:" + count);
			while (count-- > 0) {
				DomType innerType = ReadType (reader, nameTable);
				innerType.DeclaringType = result;
				result.Add (innerType);
			}
			
			// fields
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = reader.ReadUInt32 ();
//			if (verbose) System.Console.WriteLine("fields:" + count);
			while (count-- > 0) {
				DomField field = ReadField (reader, nameTable);
				field.DeclaringType = result;
				result.Add (field);
			}
			
			// methods
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = reader.ReadUInt32 ();
//			if (verbose) System.Console.WriteLine("methods:" + count);
			while (count-- > 0) {
				DomMethod method = ReadMethod (reader, nameTable);
				method.DeclaringType = result;
				result.Add (method);
			}
			
			// properties
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = reader.ReadUInt32 ();
//			if (verbose) System.Console.WriteLine("properties:" + count);
			while (count-- > 0) {
				DomProperty property = ReadProperty (reader, nameTable);
				property.DeclaringType = result;
				result.Add (property);
			}
			
			// events
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = reader.ReadUInt32 ();
//			if (verbose) System.Console.WriteLine("events:" + count);
			while (count-- > 0) {
				DomEvent evt = ReadEvent (reader, nameTable);
				evt.DeclaringType = result;
				result.Add (evt);
			}
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IType type)
		{
			Debug.Assert (type != null);
//			bool verbose  = type.Name == "CopyDelegate";
//			if (verbose) Console.WriteLine (type.GetType ());
			
			WriteMemberInformation (writer, nameTable, type);
			Write (writer, nameTable, type.BodyRegion);
			
			if (type.CompilationUnit != null) {
				WriteString (type.CompilationUnit.FileName, writer, nameTable);
			} else {
				WriteString (null, writer, nameTable);
			}
			WriteString (type.Namespace, writer, nameTable);
			writer.Write ((uint)type.ClassType);
			Write (writer, nameTable, type.BaseType);
			if (type.ImplementedInterfaces == null) {
				writer.Write (0);
			} else {
				writer.Write (type.ImplementedInterfaces.Count);
				foreach (IReturnType iface in type.ImplementedInterfaces) {
					Write (writer, nameTable, iface);
				}
			}
			writer.Write (type.InnerTypeCount);
//			if (verbose) System.Console.WriteLine("pos:{0}, write {1} inner types.", writer.BaseStream.Position, type.InnerTypeCount);
			foreach (IType innerType in type.InnerTypes) {
				Write (writer, nameTable, innerType);
			}
			writer.Write (type.FieldCount);
//			if (verbose) System.Console.WriteLine("pos:{0}, write {1} fields.", writer.BaseStream.Position, type.FieldCount);
			foreach (IField field in type.Fields) {
				Write (writer, nameTable, field);
			}
			writer.Write (type.MethodCount + type.ConstructorCount);
//			if (verbose) System.Console.WriteLine("pos:{0}, write {1} methods.", writer.BaseStream.Position, type.MethodCount + type.ConstructorCount);
			foreach (IMethod method in type.Methods) {
				Write (writer, nameTable, method);
			}
			writer.Write (type.PropertyCount + type.IndexerCount);
//			if (verbose) System.Console.WriteLine("pos:{0}, write {1} properties.", writer.BaseStream.Position, type.PropertyCount + type.IndexerCount);
			foreach (IProperty property in type.Properties) {
				Write (writer, nameTable, property);
			}
			writer.Write (type.EventCount);
//			if (verbose) System.Console.WriteLine("pos:{0}, write {1} events.", writer.BaseStream.Position, type.EventCount);
			foreach (IEvent evt in type.Events) {
				Write (writer, nameTable, evt);
			}
		}
		
		public static uint GetCount<T> (IEnumerable<T> list)
		{
			if (list == null)
				return 0;
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
