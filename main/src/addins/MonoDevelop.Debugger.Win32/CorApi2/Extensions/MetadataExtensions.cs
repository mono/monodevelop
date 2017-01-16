//
// MetadataExtensions.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CorApi2.Metadata.Microsoft.Samples.Debugging.CorMetadata;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata;
using Microsoft.Samples.Debugging.CorMetadata.NativeApi;

namespace Microsoft.Samples.Debugging.Extensions
{
	// [Xamarin] Expression evaluator.
	public static class MetadataExtensions
	{
		internal static bool TypeFlagsMatch (bool isPublic, bool isStatic, BindingFlags flags)
		{
			if (isPublic && (flags & BindingFlags.Public) == 0)
				return false;
			if (!isPublic && (flags & BindingFlags.NonPublic) == 0)
				return false;
			if (isStatic && (flags & BindingFlags.Static) == 0)
				return false;
			if (!isStatic && (flags & BindingFlags.Instance) == 0)
				return false;
			return true;
		}

		internal static Type MakeDelegate (Type retType, List<Type> argTypes)
		{
			throw new NotImplementedException ();
		}

		public static Type MakeArray (Type t, List<int> sizes, List<int> loBounds)
		{
			var mt = t as MetadataType;
			if (mt != null) {
				if (sizes == null) {
					sizes = new List<int> ();
					sizes.Add (1);
				}
				mt.m_arraySizes = sizes;
				mt.m_arrayLoBounds = loBounds;
				return mt;
			}
			if (sizes == null || sizes.Count == 1)
				return t.MakeArrayType ();
			return t.MakeArrayType (sizes.Capacity);
		}

		static Type MakeByRefTypeIfNeeded (Type t)
		{
			if (t.IsByRef)
				return t;
			var makeByRefType = t.MakeByRefType ();
			return makeByRefType;
		}

		public static Type MakeByRef (Type t)
		{
			var mt = t as MetadataType;
			if (mt != null) {
				mt.m_isByRef = true;
				return mt;
			}

			return MakeByRefTypeIfNeeded (t);
		}

		public static Type MakePointer (Type t)
		{
			var mt = t as MetadataType;
			if (mt != null) {
				mt.m_isPtr = true;
				return mt;
			}
			return MakeByRefTypeIfNeeded (t);
		}

		public static Type MakeGeneric (Type t, List<Type> typeArgs)
		{
			var mt = (MetadataType)t;
			mt.m_typeArgs = typeArgs;
			return mt;
		}
	}

	// [Xamarin] Expression evaluator.
	[CLSCompliant (false)]
	public static class MetadataHelperFunctionsExtensions
	{
		public static readonly Dictionary<CorElementType, Type> CoreTypes = new Dictionary<CorElementType, Type> ();
		static MetadataHelperFunctionsExtensions ()
		{
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_BOOLEAN, typeof (bool));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_CHAR, typeof (char));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I1, typeof (sbyte));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U1, typeof (byte));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I2, typeof (short));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U2, typeof (ushort));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I4, typeof (int));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U4, typeof (uint));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I8, typeof (long));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U8, typeof (ulong));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_R4, typeof (float));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_R8, typeof (double));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_STRING, typeof (string));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I, typeof (IntPtr));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U, typeof (UIntPtr));
		}

		internal static void ReadMethodSignature (IMetadataImport importer, Instantiation instantiation, ref IntPtr pData, out CorCallingConvention cconv, out Type retType, out List<Type> argTypes, out int sentinelIndex)
		{
			cconv = MetadataHelperFunctions.CorSigUncompressCallingConv (ref pData);
			uint numArgs = 0;
			// FIXME: Use number of <T>s.
			uint types = 0;
			sentinelIndex = -1;
			if ((cconv & CorCallingConvention.Generic) == CorCallingConvention.Generic)
				types = MetadataHelperFunctions.CorSigUncompressData (ref pData);

			if (cconv != CorCallingConvention.Field)
				numArgs = MetadataHelperFunctions.CorSigUncompressData (ref pData);

			retType = ReadType (importer, instantiation, ref pData);
			argTypes = new List<Type> ();
			for (int n = 0; n < numArgs; n++) {
				CorElementType elemType;
				unsafe {
					var pByte = (byte*) pData;
					var b = *pByte;
					elemType = (CorElementType) b;

					if (elemType == CorElementType.ELEMENT_TYPE_SENTINEL) {
						// the case when SENTINEL is presented in a separate position, so we have to increment the pointer
						sentinelIndex = n;
						pData = (IntPtr) (pByte + 1);
					}
					else if ((elemType & CorElementType.ELEMENT_TYPE_SENTINEL) == CorElementType.ELEMENT_TYPE_SENTINEL) {
						// SENTINEL is just a flag on element type, so we haven't to promote the pointer
						sentinelIndex = n;
					}
				}
				argTypes.Add (ReadType (importer, instantiation, ref pData));
			}
		}

		static Type ReadType (IMetadataImport importer, Instantiation instantiation, ref IntPtr pData)
		{
			CorElementType et;
			unsafe {
				var pBytes = (byte*)pData;
				et = (CorElementType) (*pBytes);
				pData = (IntPtr) (pBytes + 1);
			}

			if ((et & CorElementType.ELEMENT_TYPE_SENTINEL) == CorElementType.ELEMENT_TYPE_SENTINEL) {
				et ^= CorElementType.ELEMENT_TYPE_SENTINEL; // substract SENTINEL bits from element type to get clean ET
			}

			switch (et)
			{
			case CorElementType.ELEMENT_TYPE_VOID: return typeof (void);
			case CorElementType.ELEMENT_TYPE_BOOLEAN: return typeof (bool);
			case CorElementType.ELEMENT_TYPE_CHAR: return typeof (char);
			case CorElementType.ELEMENT_TYPE_I1: return typeof (sbyte);
			case CorElementType.ELEMENT_TYPE_U1: return typeof (byte);
			case CorElementType.ELEMENT_TYPE_I2: return typeof (short);
			case CorElementType.ELEMENT_TYPE_U2: return typeof (ushort);
			case CorElementType.ELEMENT_TYPE_I4: return typeof (int);
			case CorElementType.ELEMENT_TYPE_U4: return typeof (uint);
			case CorElementType.ELEMENT_TYPE_I8: return typeof (long);
			case CorElementType.ELEMENT_TYPE_U8: return typeof (ulong);
			case CorElementType.ELEMENT_TYPE_R4: return typeof (float);
			case CorElementType.ELEMENT_TYPE_R8: return typeof (double);
			case CorElementType.ELEMENT_TYPE_STRING: return typeof (string);
			case CorElementType.ELEMENT_TYPE_I: return typeof (IntPtr);
			case CorElementType.ELEMENT_TYPE_U: return typeof (UIntPtr);
			case CorElementType.ELEMENT_TYPE_OBJECT: return typeof (object);
			case CorElementType.ELEMENT_TYPE_TYPEDBYREF: return typeof(TypedReference);

			case CorElementType.ELEMENT_TYPE_VAR: {
					var index = MetadataHelperFunctions.CorSigUncompressData (ref pData);
					if (index < instantiation.TypeArgs.Count) {
						return instantiation.TypeArgs[(int) index];
					}
					return new TypeGenericParameter((int) index);
				}
			case CorElementType.ELEMENT_TYPE_MVAR: {
					// Generic args in methods not supported. Return a dummy type.
					var index = MetadataHelperFunctions.CorSigUncompressData (ref pData);
					return new MethodGenericParameter((int) index);
				}

			case CorElementType.ELEMENT_TYPE_GENERICINST: {
					Type t = ReadType (importer, instantiation, ref pData);
					var typeArgs = new List<Type> ();
					uint num = MetadataHelperFunctions.CorSigUncompressData (ref pData);
					for (int n=0; n<num; n++) {
						typeArgs.Add (ReadType (importer, instantiation, ref pData));
					}
					return MetadataExtensions.MakeGeneric (t, typeArgs);
				}

			case CorElementType.ELEMENT_TYPE_PTR: {
					Type t = ReadType (importer, instantiation, ref pData);
					return MetadataExtensions.MakePointer (t);
				}

			case CorElementType.ELEMENT_TYPE_BYREF: {
					Type t = ReadType (importer, instantiation, ref pData);
					return MetadataExtensions.MakeByRef(t);
				}

			case CorElementType.ELEMENT_TYPE_END:
			case CorElementType.ELEMENT_TYPE_VALUETYPE:
			case CorElementType.ELEMENT_TYPE_CLASS: {
					uint token = MetadataHelperFunctions.CorSigUncompressToken (ref pData);
					return new MetadataType (importer, (int) token);
				}

			case CorElementType.ELEMENT_TYPE_ARRAY: {
					Type t = ReadType (importer, instantiation, ref pData);
					int rank = (int)MetadataHelperFunctions.CorSigUncompressData (ref pData);
					if (rank == 0)
						return MetadataExtensions.MakeArray (t, null, null);

					uint numSizes = MetadataHelperFunctions.CorSigUncompressData (ref pData);
					var sizes = new List<int> (rank);
					for (int n = 0; n < numSizes && n < rank; n++)
						sizes.Add ((int)MetadataHelperFunctions.CorSigUncompressData (ref pData));

					uint numLoBounds = MetadataHelperFunctions.CorSigUncompressData (ref pData);
					var loBounds = new List<int> (rank);
					for (int n = 0; n < numLoBounds && n < rank; n++)
						loBounds.Add ((int)MetadataHelperFunctions.CorSigUncompressData (ref pData));

					return MetadataExtensions.MakeArray (t, sizes, loBounds);
				}

			case CorElementType.ELEMENT_TYPE_SZARRAY: {
					Type t = ReadType (importer, instantiation, ref pData);
					return MetadataExtensions.MakeArray (t, null, null);
				}

			case CorElementType.ELEMENT_TYPE_FNPTR: {
					CorCallingConvention cconv;
					Type retType;
					List<Type> argTypes;
					int sentinelIndex;
					ReadMethodSignature (importer, instantiation, ref pData, out cconv, out retType, out argTypes, out sentinelIndex);
					return MetadataExtensions.MakeDelegate (retType, argTypes);
				}

			case CorElementType.ELEMENT_TYPE_CMOD_REQD:
			case CorElementType.ELEMENT_TYPE_CMOD_OPT: {
					uint token = MetadataHelperFunctions.CorSigUncompressToken (ref pData);
					return new MetadataType (importer, (int) token);
				}

			case CorElementType.ELEMENT_TYPE_INTERNAL:
				return typeof(object); // hack to avoid the exceptions. CLR spec says that this type should never occurs, but it occurs sometimes, mystics

			case CorElementType.ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG:
			case CorElementType.ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
				return ReadType (importer, instantiation, ref pData);

			case CorElementType.ELEMENT_TYPE_CANON_ZAPSIG:
				return typeof(object); // this is representation of __Canon type, but it's inaccessible, using object instead
			}
			throw new NotSupportedException ("Unknown sig element type: " + et);
		}

		static readonly object[] emptyAttributes = new object[0];

		static internal object[] GetDebugAttributes (IMetadataImport importer, int token)
		{
			var attributes = new ArrayList ();
			object attr = GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerTypeProxyAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerDisplayAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerBrowsableAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = GetCustomAttribute (importer, token, typeof (System.Runtime.CompilerServices.CompilerGeneratedAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerHiddenAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerStepThroughAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerNonUserCodeAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerStepperBoundaryAttribute));
			if (attr != null)
				attributes.Add (attr);

			return attributes.Count == 0 ? emptyAttributes : attributes.ToArray ();
		}

		// [Xamarin] Expression evaluator.
		static internal object GetCustomAttribute (IMetadataImport importer, int token, Type type)
		{
			uint sigSize;
			IntPtr ppvSig;
			int hr = importer.GetCustomAttributeByName (token, type.FullName, out ppvSig, out sigSize);
			if (hr != 0)
				return null;

			var data = new byte[sigSize];
			Marshal.Copy (ppvSig, data, 0, (int)sigSize);
			var br = new BinaryReader (new MemoryStream (data));

			// Prolog
			if (br.ReadUInt16 () != 1)
				throw new InvalidOperationException ("Incorrect attribute prolog");

			ConstructorInfo ctor = type.GetConstructors ()[0];
			ParameterInfo[] pars = ctor.GetParameters ();

			var args = new object[pars.Length];

			// Fixed args
			for (int n=0; n<pars.Length; n++)
				args [n] = ReadValue (br, pars[n].ParameterType);

			object ob = Activator.CreateInstance (type, args);

			// Named args
			uint nargs = br.ReadUInt16 ();
			for (; nargs > 0; nargs--) {
				byte fieldOrProp = br.ReadByte ();
				byte atype = br.ReadByte ();

				// Boxed primitive
				if (atype == 0x51)
					atype = br.ReadByte ();
				var et = (CorElementType) atype;
				string pname = br.ReadString ();
				object val = ReadValue (br, CoreTypes [et]);

				if (fieldOrProp == 0x53) {
					FieldInfo fi = type.GetField (pname);
					fi.SetValue (ob, val);
				}
				else {
					PropertyInfo pi = type.GetProperty (pname);
					pi.SetValue (ob, val, null);
				}
			}
			return ob;
		}

		// [Xamarin] Expression evaluator.
		static object ReadValue (BinaryReader br, Type type)
		{
			if (type.IsEnum) {
				object ob = ReadValue (br, Enum.GetUnderlyingType (type));
				return Enum.ToObject (type, Convert.ToInt64 (ob));
			}
			if (type == typeof (string) || type == typeof(Type))
				return br.ReadString ();
			if (type == typeof (int))
				return br.ReadInt32 ();
			throw new InvalidOperationException ("Can't parse value of type: " + type);
		}
	}
}

