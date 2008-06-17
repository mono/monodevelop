//
// DomReturnType.cs
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
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom
{
	public class DomReturnType : IReturnType
	{
		protected string name;
		protected string nspace;
		protected int pointerNestingLevel;
		protected int arrayDimensions;
		protected int[] dimensions = new int [0];
		protected bool isNullable;
		protected bool byRef;
		protected List<IReturnType> typeParameters = new List<IReturnType> ();
		
		public static readonly DomReturnType Void = new DomReturnType (null, false, null);
		
		public string FullName {
			get {
				return !String.IsNullOrEmpty (nspace) ? nspace + "." + name : name;
			}
			set {
				if (value == null) {
					nspace = name = null; 
					return;
				}
				int idx = value.LastIndexOf ('.');
				if (idx >= 0) {
					nspace = value.Substring (0, idx);
					name   = value.Substring (idx + 1);
				} else {
					nspace = "";
					name  = value;
				}
			}
		}

		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public string Namespace {
			get {
				return nspace;
			}
			set {
				nspace = value;
			}
		}
		public int PointerNestingLevel {
			get {
				return pointerNestingLevel;
			}
			set {
				pointerNestingLevel = value;
			}
		}
		
		public int ArrayDimensions {
			get {
				return arrayDimensions;
			}
			set {
				arrayDimensions = value;
				this.dimensions = new int [arrayDimensions];
			}
		}
		
		public bool IsNullable {
			get {
				return this.isNullable;
			}
			set {
				isNullable = value;
			}
		}
		
		public System.Collections.ObjectModel.ReadOnlyCollection<IReturnType> GenericArguments {
			get {
				if (typeParameters == null)
					return null;
				return typeParameters.AsReadOnly ();
			}
		}

		public bool IsByRef {
			get {
				return byRef;
			}
			set {
				byRef = value;
			}
		}
		
		public DomReturnType ()
		{
		}
		public override bool Equals (object obj)
		{
			DomReturnType type = obj as DomReturnType;
			if (type == null)
				return false;
			if (dimensions != null && type.dimensions != null) {
				if (dimensions.Length != type.dimensions.Length)
					return false;
				for (int i = 0; i < dimensions.Length; i++) {
					if (dimensions [i] != type.dimensions [i])
						return false;
				}
			}
			if (typeParameters != null && type.typeParameters != null) {
				if (typeParameters.Count != type.typeParameters.Count)
					return false;
				for (int i = 0; i < typeParameters.Count; i++) {
					if (!typeParameters[i].Equals (type.typeParameters [i]))
						return false;
				}
			}
			return name == type.name &&
				nspace == type.nspace &&
				pointerNestingLevel == type.pointerNestingLevel &&
				arrayDimensions == type.arrayDimensions &&
				isNullable == type.isNullable &&
				byRef == type.byRef;
		}
		
		public int GetDimension (int arrayDimension)
		{
			if (arrayDimension < 0 || arrayDimension >= this.arrayDimensions)
				return -1;
			return this.dimensions [arrayDimension];
		}
		
		public void SetDimension (int arrayDimension, int dimension)
		{
			if (arrayDimension < 0 || arrayDimension >= this.arrayDimensions)
				return;
			this.dimensions [arrayDimension] = dimension;
		}

		
		public DomReturnType (string name) : this (name, false, new List<IReturnType> ())
		{
		}
		
		public DomReturnType (string name, bool isNullable, List<IReturnType> typeParameters)
		{
			this.FullName           = name;
			this.isNullable     = isNullable;
			this.typeParameters = typeParameters;
		}
		
		
		public void AddTypeParameter (IReturnType type)
		{
			if (typeParameters == null)
				typeParameters = new List<IReturnType> ();
			this.typeParameters.Add (type);
		}
		public object AcceptVisitior (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
		
		public static IReturnType Resolve (IReturnType source, ITypeResolver resolver)
		{
			return source != null ? resolver.Resolve (source) : null;
		}
		
		public override string ToString ()
		{
			return string.Format ("[DomReturnType:Name={0}, PointerNestingLevel={1}, ArrayDimensions={2}]",
			                      Name,
			                      PointerNestingLevel,
			                      ArrayDimensions);
		}
		
		public static string ConvertToString (IReturnType type)
		{
			StringBuilder sb = new StringBuilder (DomType.GetInstantiatedTypeName (type.FullName, type.GenericArguments));
			
			if (type.PointerNestingLevel > 0)
				sb.Append (new String ('*', type.PointerNestingLevel));
			
			if (type.ArrayDimensions > 0) {
				for (int i = 0; i < type.ArrayDimensions; i++) {
					sb.Append ("[]");
				}
			}
			
			return sb.ToString ();
		}
#region Shared return types
		static Dictionary<string, List<IReturnType>> sharedTypes;
		static string[] sharedTypeList = new string [] {
			"System.Void",
			"System.Object",
			"System.Boolean",
			"System.Byte",
			"System.SByte",
			"System.Char",
			"System.Enum",
			"System.Int16",
			"System.Int32",
			"System.Int64",
			"System.UInt16",
			"System.UInt32",
			"System.UInt64",
			"System.Single",
			"System.Double",
			"System.Decimal",
			"System.String",
			"System.DateTime",
			"System.IntPtr",
			"System.Enum",
			"System.Type",
			"System.IO.Stream",
			"System.EventArgs"
		};
		
		internal static IReturnType GetSharedType (IReturnType type)
		{
			if (sharedTypes == null)
				InitSharedTypes ();
			
			if (sharedTypes.ContainsKey (type.FullName)) {
				foreach (IReturnType sharedType in sharedTypes[type.FullName]) {
					if (sharedType.Equals (type)) {
						return sharedType;
					}
				}
			}
			return type;
		}
		
		static void InitSharedTypes ()
		{
			sharedTypes = new Dictionary <string, List<IReturnType>> ();
			foreach (string typeName in sharedTypeList) {
				AddSharedType (typeName);
			}
		}
		
		static void AddSharedType (string typeName)
		{
			AddSharedType (typeName, new DomReturnType (typeName));
		}
		static void AddSharedType (string typeName, DomReturnType type)
		{
			if (!sharedTypes.ContainsKey (typeName)) {
				sharedTypes.Add (typeName, new List <IReturnType> ());
			}
			if (!sharedTypes [typeName].Contains (type))
				sharedTypes [typeName].Add (type);
		}
		
#endregion
	}
}
