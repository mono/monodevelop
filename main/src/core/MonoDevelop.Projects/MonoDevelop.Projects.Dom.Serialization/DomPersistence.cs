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
using System.Globalization;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
			
			string ns = ReadString (reader, nameTable);
			List<IReturnTypePart> parts = new List<IReturnTypePart> ();
			
			uint partCount = ReadUInt (reader, 500);
			while (partCount-- > 0) {
				ReturnTypePart part = new ReturnTypePart ();
				parts.Add (part);
				part.Name = ReadString (reader, nameTable);
				uint arguments  = ReadUInt (reader, 1000);
				while (arguments-- > 0)
					part.AddTypeParameter (ReadReturnType (reader, nameTable));
			}
			
			DomReturnType result = new DomReturnType (ns, parts);
			result.PointerNestingLevel = reader.ReadInt32 ();
			result.IsNullable = reader.ReadBoolean ();
			result.IsByRef = reader.ReadBoolean ();
			
			int  arrayDimensions = reader.ReadInt32 ();
			int[] dims = new int [arrayDimensions];
			for (int n=0; n<arrayDimensions; n++)
				dims [n] = reader.ReadInt32 ();
			
			result.SetDimensions (dims);
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IReturnType returnType)
		{
			if (WriteNull (writer, returnType))
				return;
			WriteString (returnType.Namespace, writer, nameTable);
			writer.Write ((uint) returnType.Parts.Count);
			foreach (ReturnTypePart part in returnType.Parts) {
				WriteString (part.Name, writer, nameTable);
				writer.Write ((uint) part.GenericArguments.Count);
				foreach (IReturnType rtp in part.GenericArguments)
					Write (writer, nameTable, rtp);
			}
			writer.Write (returnType.PointerNestingLevel);
			writer.Write (returnType.IsNullable);
			writer.Write (returnType.IsByRef);
			writer.Write (returnType.ArrayDimensions);
			for (int n=0; n<returnType.ArrayDimensions; n++)
				writer.Write (returnType.GetDimension (n));
		}
		
		public static DomMethod ReadMethod (BinaryReader reader, INameDecoder nameTable)
		{
			DomMethod result = new DomMethod ();
			ReadMemberInformation (reader, nameTable, result);
			uint explicitInterfaces = ReadUInt (reader, 500);
			while (explicitInterfaces-- > 0) {
				result.AddExplicitInterface (ReadReturnType (reader, nameTable));
			}
			
			result.BodyRegion = ReadRegion (reader, nameTable);
			result.ReturnType = ReadReturnType (reader, nameTable);
			result.MethodModifier = (MethodModifier)reader.ReadInt32 ();
			
				
			uint arguments = ReadUInt (reader, 5000);
			while (arguments-- > 0) {
				result.Add (ReadParameter (reader, nameTable));
			}
			arguments = ReadUInt (reader, 500);
			while (arguments-- > 0) {
				result.AddTypeParameter (ReadTypeParameter (reader, nameTable));
			}
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IMethod method)
		{
			Debug.Assert (method != null);
			WriteMemberInformation (writer, nameTable, method);
			writer.Write (method.ExplicitInterfaces.Count());
			foreach (IReturnType returnType in method.ExplicitInterfaces) {
				Write (writer, nameTable, returnType);
			}
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

			writer.Write (method.TypeParameters.Count);
			foreach (ITypeParameter genArg in method.TypeParameters) {
				Write (writer, nameTable, genArg);
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
			uint explicitInterfaces = ReadUInt (reader, 500);
			while (explicitInterfaces-- > 0) {
				result.AddExplicitInterface (ReadReturnType (reader, nameTable));
			}
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
			writer.Write (property.ExplicitInterfaces.Count ());
			foreach (IReturnType returnType in property.ExplicitInterfaces) {
				Write (writer, nameTable, returnType);
			}
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
			nameTable.Reset ();
			return ReadTypeInternal (reader, nameTable);
		}
		
		static DomType ReadTypeInternal (BinaryReader reader, INameDecoder nameTable)
		{
			uint typeCount = ReadUInt (reader, 1000);
			if (typeCount > 1) {
				CompoundType compoundResult = new CompoundType ();
				while (typeCount-- > 0) {
					compoundResult.AddPart (ReadTypeInternal (reader, nameTable));
				}
				return compoundResult;
			}
			
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
			long count = ReadUInt (reader, 5000);
//			if (verbose) System.Console.WriteLine("impl. interfaces:" + count);
			while (count-- > 0) {
				result.AddInterfaceImplementation (ReadReturnType (reader, nameTable));
			}
			
			// innerTypes
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = ReadUInt (reader, 10000);
//			if (verbose) System.Console.WriteLine("inner types:" + count);
			while (count-- > 0) {
				DomType innerType = ReadTypeInternal (reader, nameTable);
				innerType.DeclaringType = result;
				result.Add (innerType);
			}
			
			// fields
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = ReadUInt (reader, 10000);
//			if (verbose) System.Console.WriteLine("fields:" + count);
			while (count-- > 0) {
				DomField field = ReadField (reader, nameTable);
				field.DeclaringType = result;
				result.Add (field);
			}
			
			// methods
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = ReadUInt (reader, 10000);
//			if (verbose) System.Console.WriteLine("methods:" + count);
			while (count-- > 0) {
				DomMethod method = ReadMethod (reader, nameTable);
				method.DeclaringType = result;
				result.Add (method);
			}
			
			// properties
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = ReadUInt (reader, 10000);
//			if (verbose) System.Console.WriteLine("properties:" + count);
			while (count-- > 0) {
				DomProperty property = ReadProperty (reader, nameTable);
				property.DeclaringType = result;
				result.Add (property);
			}
			
			// events
//			if (verbose) System.Console.WriteLine("pos:" + reader.BaseStream.Position);
			count = ReadUInt (reader, 10000);
//			if (verbose) System.Console.WriteLine("events:" + count);
			while (count-- > 0) {
				DomEvent evt = ReadEvent (reader, nameTable);
				evt.DeclaringType = result;
				result.Add (evt);
			}
			
			// type parameters
			count = ReadUInt (reader, 500);
			while (count-- > 0) {
				TypeParameter tp = ReadTypeParameter (reader, nameTable);
				result.AddTypeParameter (tp);
			}
			return result;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IType type)
		{
			nameTable.Reset ();
			WriteInternal (writer, nameTable, type);
		}
		
		static void WriteInternal (BinaryWriter writer, INameEncoder nameTable, IType type)
		{
			Debug.Assert (type != null);
			if (type is CompoundType && ((CompoundType)type).PartsCount > 1) {
				CompoundType compoundType = type as CompoundType;
				writer.Write ((uint)compoundType.PartsCount);
				foreach (IType part in compoundType.Parts)
					WriteInternal (writer, nameTable, part);
				return;
			}
			
			writer.Write ((uint)1);
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
				WriteInternal (writer, nameTable, innerType);
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
			writer.Write (type.TypeParameters.Count);
			foreach (TypeParameter tp in type.TypeParameters)
				Write (writer, nameTable, tp);
		}

		public static TypeParameter ReadTypeParameter (BinaryReader reader, INameDecoder nameTable)
		{
			string name = ReadString (reader, nameTable);
			TypeParameter tp = new TypeParameter (name);
			
			// Flags
			
			byte f = reader.ReadByte ();
			if ((f & 1) != 0)
				tp.ClassRequired = true;
			if ((f & 2) != 0)
				tp.ValueTypeRequired = true;
			if ((f & 4) != 0)
				tp.ConstructorRequired = true;

			// Constraints
			
			uint count = ReadUInt (reader, 1000);
			while (count-- > 0)
				tp.AddConstraint (ReadReturnType (reader, nameTable));

			// Attributes
			
			count = ReadUInt (reader, 1000);
			while (count-- > 0)
				tp.AddAttribute (ReadAttribute (reader, nameTable));

			return tp;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, ITypeParameter typeParameter)
		{
			WriteString (typeParameter.Name, writer, nameTable);

			// Flags
			
			byte f = 0;
			if (typeParameter.ClassRequired)
				f |= 1;
			if (typeParameter.ValueTypeRequired)
				f |= 2;
			if (typeParameter.ConstructorRequired)
				f |= 4;
			writer.Write (f);
			
			// Constraints
			
			writer.Write (typeParameter.Constraints.Count ());
			foreach (IReturnType rt in typeParameter.Constraints)
				Write (writer, nameTable, rt);

			// Attributes
			
			writer.Write (typeParameter.Attributes.Count ());
			foreach (IAttribute attr in typeParameter.Attributes)
				Write (writer, nameTable, attr);
		}

		public static DomAttribute ReadAttribute (BinaryReader reader, INameDecoder nameTable)
		{
			DomAttribute attr = new DomAttribute ();
			attr.Name = ReadString (reader, nameTable);
			attr.Region = ReadRegion (reader, nameTable);
			attr.AttributeTarget = (AttributeTarget) reader.ReadInt32 ();
			attr.AttributeType = ReadReturnType (reader, nameTable);
			
			// Named argument count
			uint num = ReadUInt (reader, 500);
			string[] names = new string[num];
			for (int n=0; n<num; n++)
				names [n] = ReadString (reader, nameTable);
			
			CodeExpression[] exps = ReadExpressionArray (reader, nameTable);
			
			int i;
			for (i=0; i<num; i++)
				attr.AddNamedArgument (names[i], exps [i]);
			
			for (;i<exps.Length; i++)
				attr.AddPositionalArgument (exps [i]);

			return attr;
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, IAttribute attr)
		{
			WriteString (attr.Name, writer, nameTable);
			Write (writer, nameTable, attr.Region);
			writer.Write ((int)attr.AttributeTarget);
			Write (writer, nameTable, attr.AttributeType);
			
			CodeExpression[] exps = new CodeExpression [attr.PositionalArguments.Count + attr.NamedArguments.Count];
			
			// Save the named argument count. The remaining expressions will be considered positionl arguments.
			writer.Write ((uint)attr.NamedArguments.Count);
			int n=0;
			foreach (KeyValuePair<string, CodeExpression> na in attr.NamedArguments) {
				WriteString (na.Key, writer, nameTable);
				exps [n++] = na.Value;
			}
			
			attr.PositionalArguments.CopyTo (exps, n);
			Write (writer, nameTable, exps);
		}
		
		public static void Write (BinaryWriter writer, INameEncoder nameTable, CodeExpression[] exps)
		{
			if (exps.Length == 0) {
				writer.Write (0);
				return;
			}
			
			bool mustSerialize = false;
			foreach (CodeExpression exp in exps) {
				if (!(exp is CodePrimitiveExpression)) {
					mustSerialize = true;
					break;
				}
			}
			if (mustSerialize) {
				writer.Write (-1);
				SerializeObject (writer, exps);
			} else {
				writer.Write (exps.Length);
				foreach (CodePrimitiveExpression exp in exps) {
					if (exp.Value == null) {
						writer.Write ((int) TypeCode.DBNull);
					} else {
						writer.Write ((int) Type.GetTypeCode (exp.Value.GetType ()));
						WriteString (Convert.ToString (exp.Value, CultureInfo.InvariantCulture), writer, nameTable);
					}
				}
			}
		}
		
		public static CodeExpression[] ReadExpressionArray (BinaryReader reader, INameDecoder nameTable)
		{
			int count = reader.ReadInt32 ();
			if (count == 0) {
				return new CodeExpression[0];
			} else if (count == -1) {
				return (CodeExpression[]) DeserializeObject (reader);
			} else {
				CodeExpression[] exps = new CodeExpression[count];
				for (int n=0; n<count; n++) {
					object value;
					TypeCode code = (TypeCode) reader.ReadInt32 ();
					if (code == TypeCode.DBNull)
						value = null;
					else
						value = Convert.ChangeType (ReadString (reader, nameTable), code, CultureInfo.InvariantCulture);
					exps [n] = new CodePrimitiveExpression (value);
				}
				return exps;
			}
		}
		
#region Helper methods
		static void WriteMemberInformation (BinaryWriter writer, INameEncoder nameTable, IMember member)
		{
			WriteString (member.Name, writer, nameTable);
			WriteString (member.Documentation, writer, nameTable);
			writer.Write ((uint)member.Modifiers);
			Write (writer, nameTable, member.Location);
			
			writer.Write (member.Attributes.Count ());
			foreach (IAttribute attr in member.Attributes)
				Write (writer, nameTable, attr);
		}
		static void ReadMemberInformation (BinaryReader reader, INameDecoder nameTable, AbstractMember member)
		{
			member.Name          = ReadString (reader, nameTable);
			member.Documentation = ReadString (reader, nameTable);
			member.Modifiers     = (Modifiers)reader.ReadUInt32();
			member.Location      = ReadLocation (reader, nameTable);
			
			uint count = ReadUInt (reader, 1000);
			while (count-- > 0)
				member.Add (ReadAttribute (reader, nameTable));
		}
		
		static void WriteString (string s, BinaryWriter writer, INameEncoder nameTable)
		{
			if (s == null)
				writer.Write (-2);
			else {
				bool isNew;
				int id = nameTable.GetStringId (s, out isNew);
				writer.Write (id);
				if (isNew)
					writer.Write (s);
			}
		}
		
		static string ReadString (BinaryReader reader, INameDecoder nameTable)
		{
			int id = reader.ReadInt32 ();
			if (id == -2)
				return null;
			string res = nameTable.GetStringValue (id);
			if (res == null) {
				res = reader.ReadString ();
				nameTable.RegisterString (id, res);
			}
			return res;
		}
		
		static uint ReadUInt (BinaryReader reader, int maxValue)
		{
			uint res = reader.ReadUInt32 ();
			if (res > maxValue)
				throw new InvalidOperationException ("Invalid integer value");
			return res;
		}
		
		
		static BinaryFormatter formatter = new BinaryFormatter ();
		
		public static void SerializeObject (BinaryWriter writer, object obj)
		{
			if (obj == null)
				writer.Write (0);
			else {
				MemoryStream ms = new MemoryStream ();
				formatter.Serialize (ms, obj);
				byte[] data = ms.ToArray ();
				writer.Write (data.Length);
				writer.Write (data);
			}
		}
		
		public static object DeserializeObject (BinaryReader reader)
		{
			int len = reader.ReadInt32 ();
			if (len == 0) return null;
			byte[] data = reader.ReadBytes (len);
			MemoryStream ms = new MemoryStream (data);
			object ob = formatter.Deserialize (ms);
			return ob;
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
