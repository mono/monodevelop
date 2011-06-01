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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace MonoDevelop.Projects.Dom
{
	public class ReturnTypePart : IReturnTypePart
	{
		public string Name {
			get;
			set;
		}
		
		protected List<IReturnType> genericArguments = null;
		static readonly ReadOnlyCollection<IReturnType> emptyGenericParameters = new ReadOnlyCollection<IReturnType> (new IReturnType [0]);
		public System.Collections.ObjectModel.ReadOnlyCollection<IReturnType> GenericArguments {
			get {
				if (genericArguments == null)
					return emptyGenericParameters;
				return genericArguments.AsReadOnly ();
			}
		}
		public virtual string HelpUrl {
			get {
				if (GenericArguments.Count == 0)
					return Name;
				return Name + "`" + GenericArguments.Count;
			}
		}

		public bool IsGenerated {
			get;
			set;
		}
		
		public object Tag {
			get;
			set;
		}
		
		public ReturnTypePart ()
		{
		}
		
		public ReturnTypePart (IReturnTypePart part)
		{
			Name = part.Name;
			IsGenerated = part.IsGenerated;
			Tag = part.Tag;
			foreach (var a in part.GenericArguments)
				AddTypeParameter (a);
		}
		
		public ReturnTypePart (string name)
		{
			this.Name = name;
		}
		
		public ReturnTypePart (string name, IEnumerable<IReturnType> typeParameters)
		{
			for (int i = 0; i < name.Length; i++) {
				char ch = name[i];
				if (!(Char.IsLetterOrDigit (ch) || ch =='_')) {
					name = name.Substring (0, i);
					break;
				}
			}
			this.Name = name;
			if (typeParameters != null && typeParameters.Any ())
				this.genericArguments = new List<IReturnType> (typeParameters);
		}
		public ReturnTypePart (string baseName, string name, IEnumerable<ITypeParameter> typeParameters)
		{
			this.Name = name;
			if (typeParameters != null && typeParameters.Any ()) {
				this.genericArguments = new List<IReturnType> ();
				foreach (ITypeParameter para in typeParameters) {
					this.genericArguments.Add (new DomReturnType (baseName + "." + para.Name));
				}
			}
		}
		
		public string ToInvariantString ()
		{
			if (genericArguments != null && genericArguments.Count > 0) {
				StringBuilder result = new StringBuilder ();
				result.Append (Name);
				result.Append ('<');
				for (int i = 0; i < genericArguments.Count; i++) {
					if (i > 0)
						result.Append (',');
					result.Append (genericArguments[i].ToInvariantString ());
				}
				result.Append ('>');
				return result.ToString ();
			}
			return Name;
		}
		
		public void AddTypeParameter (IReturnType type)
		{
			if (genericArguments == null)
				genericArguments = new List<IReturnType> ();
			this.genericArguments.Add (type);
		}
		public override string ToString ()
		{
			return string.Format ("[ReturnTypePart: Name={0}, #GenericArguments={1}]", Name, GenericArguments.Count);
		}
		
	}
	
	public class DomReturnType : AbstractNode, IReturnType
	{
		static readonly int[] zeroDimensions = new int[0];
		static readonly int[] oneDimensions = new int[] { 0 };
		
		List<ReturnTypePart> parts = new List<ReturnTypePart> ();
		
		public static readonly IReturnType Void;
		public static readonly IReturnType Object;
		public static readonly IReturnType String;
		public static readonly IReturnType Char;
		public static readonly IReturnType Byte;
		public static readonly IReturnType SByte;
		public static readonly IReturnType Bool;
		public static readonly IReturnType Delegate;
		
		public static readonly IReturnType Int16;
		public static readonly IReturnType Int32;
		public static readonly IReturnType Int64;
		
		public static readonly IReturnType UInt16;
		public static readonly IReturnType UInt32;
		public static readonly IReturnType UInt64;
		
		public static readonly IReturnType Float;
		public static readonly IReturnType Double;
		public static readonly IReturnType Decimal;
		
		public static readonly IReturnType IntPtr;
		public static readonly IReturnType UIntPtr;
		
		public static readonly IReturnType Exception;
		public static readonly IReturnType DateTime;
		public static readonly IReturnType EventArgs;
		public static readonly IReturnType StringBuilder;
		public static readonly IReturnType TypeReturnType;
		
		public static readonly IReturnType Enum;
		public static readonly IReturnType ValueType;
	
		public bool IsGenerated {
			get;
			set;
		}
		
		public string HelpUrl {
			get {
				
				StringBuilder result = new StringBuilder ();
				result.Append ("T:");
				if (!string.IsNullOrEmpty (Namespace))
					result.Append (Namespace);
				for (int i = 0; i < parts.Count; i++) {
					if (result.Length > "T:".Length)
						result.Append (".");
					result.Append (parts[i].HelpUrl);
				}
				return result.ToString ();
			}
		}
		
		public object Tag {
			get {
				return parts[parts.Count - 1].Tag;
			}
			set {
				parts[parts.Count - 1].Tag = value;
			}
		}
		
		static DomReturnType ()
		{
			// Initialization is done here instead of using field initializers to
			// ensure that the returnTypeCache dictionary us properly initialized
			// when calling GetSharedReturnType.
			
			Void = CreateTableEntry ("System.Void");
			Object = CreateTableEntry ("System.Object");
			String = CreateTableEntry("System.String");
			Bool = CreateTableEntry ("System.Boolean");
			Char = CreateTableEntry ("System.Char");
			Byte = CreateTableEntry ("System.Byte");
			SByte = CreateTableEntry ("System.SByte");
			Exception = CreateTableEntry ("System.Exception");
			Int16 = CreateTableEntry ("System.Int16"); 
			Int32 = CreateTableEntry ("System.Int32");
			Int64 = CreateTableEntry ("System.Int64");
			UInt16 = CreateTableEntry ("System.UInt16"); 
			UInt32 = CreateTableEntry ("System.UInt32");
			UInt64 = CreateTableEntry ("System.UInt64");
			UInt64 = CreateTableEntry ("System.UInt64");
			Float   = CreateTableEntry ("System.Single");
			Double  = CreateTableEntry ("System.Double");
			Decimal = CreateTableEntry ("System.Decimal");
			IntPtr = CreateTableEntry ("System.IntPtr");
			UIntPtr = CreateTableEntry ("System.UIntPtr");
			
			TypeReturnType = CreateTableEntry ("System.Type");
			EventArgs = CreateTableEntry ("System.EventArgs");
			StringBuilder = CreateTableEntry ("System.Text.StringBuilder");
			DateTime = CreateTableEntry ("System.DateTime");
			
			// table entries for frequently used types
			CreateTableEntry ("System.Runtime.InteropServices.DllImport");
			CreateTableEntry ("System.EventHandler");
			CreateTableEntry ("System.Runtime.InteropServices.ComVisibleAttribute");
			CreateTableEntry ("System.CLSCompliantAttribute");
			CreateTableEntry ("System.ObsoleteAttribute");
			CreateTableEntry ("System.MonoTODOAttribute");
			CreateTableEntry ("System.AttributeUsageAttribute");
			CreateTableEntry ("System.ComponentModel.DefaultValueAttribute");
			CreateTableEntry ("System.ComponentModel.BrowsableAttribute");
			CreateTableEntry ("System.ComponentModel.DesignerSerializationVisibilityAttribute");
			CreateTableEntry ("System.Web.WebCategoryAttribute");
			CreateTableEntry ("System.Web.WebSysDescriptionAttribute");
			CreateTableEntry ("System.Configuration.ConfigurationPropertyAttribute");
			CreateTableEntry ("System.ComponentModel.EditorBrowsableAttribute");
			CreateTableEntry ("System.Reflection.DefaultMemberAttribute");
			CreateTableEntry ("System.ComponentModel.LocalizableAttribute");
			CreateTableEntry ("System.FlagsAttribute");
			CreateTableEntry ("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
			CreateTableEntry ("System.ComponentModel.LocalizableAttribute");
			CreateTableEntry ("GLib.PropertyAttribute");
			CreateTableEntry ("GLib.SignalAttribute");
			CreateTableEntry ("GLib.DefaultSignalHandlerAttribute");
			CreateTableEntry ("Gtk.Widget");
			CreateTableEntry ("Gtk.Label");
			
			Delegate = CreateTableEntry ("System.Delegate");
			Enum = CreateTableEntry ("System.Enum");
			ValueType = CreateTableEntry ("System.ValueType");
		}

		public List<ReturnTypePart> Parts {
			get {
				return parts;
			}
		}
		
		ReadOnlyCollection<IReturnTypePart> IReturnType.Parts {
			get {
				return new ReadOnlyCollection<IReturnTypePart> (parts.ToArray ());
			}
		}
		
		public string Name {
			get {
				Debug.Assert (parts.Count > 0);
				return parts[parts.Count - 1].Name;
			}
			set {
				Debug.Assert (parts.Count > 0);
				parts[parts.Count - 1].Name = value;
			}
		}
		
		public ReadOnlyCollection<IReturnType> GenericArguments {
			get {
				if (parts.Count ==0)
					return new ReadOnlyCollection<IReturnType> (new IReturnType[0]);
				return parts[parts.Count - 1].GenericArguments;
			}
		}
		
		public void AddTypeParameter (IReturnType type)
		{
			Debug.Assert (parts.Count > 0);
			parts[parts.Count - 1].AddTypeParameter (type);
		}
		
		protected string nspace;
		protected int pointerNestingLevel, arrayPointerNestingLevel;
		protected int[] dimensions = null;
		ReturnTypeModifiers modifiers;
		
		public string FullName {
			get {
				if (Parts.Count == 1)
					return !string.IsNullOrEmpty (nspace) ? nspace + "." + Name : Name;
				StringBuilder result = new StringBuilder (nspace);
				foreach (IReturnTypePart part in Parts) {
					if (result.Length > 0)
						result.Append (".");
					result.Append (part.Name);
				}
				return result.ToString ();
			}
		}
		
		public string DecoratedFullName {
			get {
				StringBuilder result = new StringBuilder (Namespace);
				foreach (ReturnTypePart rpart in Parts) {
					if (result.Length > 0)
						result.Append (".");
					result.Append (rpart.Name);
					if (rpart.GenericArguments.Count > 0) {
						result.Append ("`");
						result.Append (rpart.GenericArguments.Count);
					}
				}
				return result.ToString ();
			}
		}
		
		public static KeyValuePair<string, string> SplitFullName (string fullName)
		{
			if (string.IsNullOrEmpty (fullName)) 
				return new KeyValuePair<string, string> ("", "");
			int idx = fullName.LastIndexOf ('.');
			if (idx >= 0) 
				return new KeyValuePair<string, string> (fullName.Substring (0, idx), fullName.Substring (idx + 1));
			return new KeyValuePair<string, string> ("", fullName);
		}

		public ReturnTypeModifiers Modifiers {
			get {
				return this.modifiers;
			}
			set {
				this.modifiers = value;
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
		
		public int ArrayPointerNestingLevel {
			get {
				return arrayPointerNestingLevel;
			}
			set {
				arrayPointerNestingLevel = value;
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
				return dimensions != null ? dimensions.Length : 0;
			}
			set {
				List<int> curDimensions = new List<int> (dimensions ?? zeroDimensions);
				if (curDimensions.Count > value) 
					curDimensions.RemoveRange (value, curDimensions.Count - value);
				while (curDimensions.Count < value)
					curDimensions.Add (0);
				SetDimensions (curDimensions.ToArray ());
			}
		}

		public bool IsNullable {
			get {
				return (Modifiers & ReturnTypeModifiers.Nullable) == ReturnTypeModifiers.Nullable;
			}
			set {
				if (value) {
					Modifiers |= ReturnTypeModifiers.Nullable;
				} else {
					Modifiers &= ~ReturnTypeModifiers.Nullable;
				}
			}
		}

		public bool IsByRef {
			get {
				return (Modifiers & ReturnTypeModifiers.ByRef) == ReturnTypeModifiers.ByRef;
			}
			set {
				if (value) {
					Modifiers |= ReturnTypeModifiers.ByRef;
				} else {
					Modifiers &= ~ReturnTypeModifiers.ByRef;
				}
			}
		}
		
		protected IType type;
		public virtual IType Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public DomReturnType ()
		{
			this.parts.Add (new ReturnTypePart ());
		}
		
		internal DomReturnType (string ns, List<ReturnTypePart> parts)
		{
			this.nspace = ns;
			this.parts = parts;
		}
		
		public DomReturnType (IReturnType type)
		{
			DomReturnType rt = (DomReturnType) type;
			if (rt.dimensions != null) {
				dimensions = new int [rt.dimensions.Length];
				rt.dimensions.CopyTo (dimensions, 0);
			}
			IsGenerated = rt.IsGenerated;
			nspace = rt.nspace;
			pointerNestingLevel = rt.pointerNestingLevel;
			arrayPointerNestingLevel = rt.arrayPointerNestingLevel;
			modifiers = rt.modifiers;
			
			foreach (var p in rt.Parts)
				parts.Add (new ReturnTypePart (p));
		}
		
		public DomReturnType (IType type)
		{
			SetType (type);
		}
		
		public void SetType (IType type)
		{
			if (type == null)
				throw new ArgumentNullException ("type was null");
			this.parts.Clear ();
			this.type = type;
			this.nspace = type is InstantiatedType ? ((InstantiatedType)type).UninstantiatedType.Namespace : type.Namespace;
			IType curType = type;
			do {
				if (curType is InstantiatedType) {
					InstantiatedType instType = (InstantiatedType)curType;
					this.parts.Insert (0, new ReturnTypePart (instType.UninstantiatedType.Name, instType.GenericParameters));
				} else
					this.parts.Insert (0, new ReturnTypePart (curType.FullName, curType.Name, curType.TypeParameters));
				curType = curType.DeclaringType;
			} while (curType != null);
		}
		
		public override bool Equals (object obj)
		{
			DomReturnType type = obj as DomReturnType;
			if (type == null)
				return false;
			if (ArrayDimensions != type.ArrayDimensions)
				return false;
			for (int n=0; n<ArrayDimensions; n++) {
				if (GetDimension (n) != type.GetDimension (n))
					return false;
			}
			if (GenericArguments.Count != type.GenericArguments.Count)
				return false;
			for (int i = 0; i < GenericArguments.Count; i++) {
				if (!GenericArguments[i].Equals (type.GenericArguments [i]))
					return false;
			}

			return Name == type.Name &&
				nspace == type.nspace &&
				pointerNestingLevel == type.pointerNestingLevel &&
				Modifiers == type.Modifiers;
		}

		public override int GetHashCode ()
		{
			return ToInvariantString ().GetHashCode ();
		}

		
		public int GetDimension (int arrayDimension)
		{
			if (dimensions == null || arrayDimension < 0 || arrayDimension >= dimensions.Length)
				return -1;
			return this.dimensions [arrayDimension];
		}

		public void SetDimension (int arrayDimension, int dimension)
		{
			if (arrayDimension < 0 || arrayDimension >= ArrayDimensions)
				return;
			
			// Avoid changing the shared dimension
			if (dimensions == oneDimensions)
				dimensions = new int [ArrayDimensions];
			
			dimensions [arrayDimension] = dimension;
			SetDimensions (dimensions);
		}
		
		public void SetDimensions (int[] arrayDimensions)
		{
			// Reuse common dimension constants to save memory
			if (arrayDimensions == null)
				dimensions = null;
			else if (arrayDimensions != null && arrayDimensions.Length == 1 && arrayDimensions[0] == 0)
				dimensions = oneDimensions;
			else
				dimensions = arrayDimensions;
		}
		
		public int[] GetDimensions ()
		{
			return dimensions ?? zeroDimensions;
		}
		
		public DomReturnType (string name) : this (name, false, new List<IReturnType> ())
		{
		}
		
		public DomReturnType (string nameSpace, string name) : this (nameSpace, name, false, new List<IReturnType> ())
		{
		}
		
		public DomReturnType (string name, bool isNullable, IEnumerable<IReturnType> typeParameters)
		{
			KeyValuePair<string, string> splitted = SplitFullName (name);
			this.nspace = splitted.Key;
			this.parts.Add (new ReturnTypePart (splitted.Value, typeParameters));
			this.IsNullable     = isNullable;
		}
		
		public DomReturnType (string nameSpace, string name, bool isNullable, IEnumerable<IReturnType> typeParameters)
		{
			this.nspace = nameSpace;
			var parts = name.Split ('.');
			for (int i = 0; i < parts.Length; i++) {
				string part = parts[i];
				this.parts.Add (i + 1 < parts.Length ? new ReturnTypePart (part) : new ReturnTypePart (part, typeParameters));
			}
			this.IsNullable = isNullable;
		}
		
		public static int num = 0;
		string invariantString = null;
		public string ToInvariantString ()
		{
			if (invariantString != null)
				return invariantString;
			StringBuilder result = new StringBuilder ();
			result.Append (Namespace);
			foreach (ReturnTypePart part in Parts) {
				if (result.Length > 0)
					result.Append ('.');
				result.Append (part.ToInvariantString ());
			}
			if (this.IsNullable)
				result.Append ('?');

			result.Append ('*', this.PointerNestingLevel);

			for (int i = 0; i < ArrayDimensions; i++) {
				result.Append ('[');
				int dimension = this.GetDimension (i);
				 // setting upper limit to prevent out of memory exceptions on false data.
				if (dimension > 0 && dimension < 4096)
					result.Append (',', dimension);
				result.Append (']');
			}
			
			result.Append ('*', this.ArrayPointerNestingLevel);

			if (this.IsByRef)
				result.Append ('&');

			return invariantString = result.ToString ();
		}
		
		public override S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
		
		public override string ToString ()
		{
			string result = string.Format ("[DomReturnType:FullName={0}, PointerNestingLevel={1}, ArrayDimensions={2}, #GenericArguments={3}, UnderlyingType={4}]", 
			                      FullName, 
			                      PointerNestingLevel, 
			                      ArrayDimensions, 
			                      GenericArguments.Count, 
			                      Type == null ? "null" : Type.ToString ());
			return result;
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
		
#region shared return types
		static List<IReturnType> returnTypeTable = new List<IReturnType> ();
		static Dictionary<string, int> tableIndex = new Dictionary<string, int> ();
		
		static IReturnType CreateTableEntry (string fullName)
		{
			DomReturnType result = new DomReturnType (fullName);
			tableIndex[fullName] = returnTypeTable.Count;
			returnTypeTable.Add (result);
			return result;
		}
		
		internal static int GetIndex (IReturnType returnType)
		{
			if (returnType.PointerNestingLevel != 0 || returnType.ArrayDimensions != 0 || returnType.GenericArguments.Count != 0)
				return -1;
			
			string invariantString = returnType.ToInvariantString ();
			int index;
			if (tableIndex.TryGetValue (invariantString, out index))
				return index;
			return -1;
		}
		
		internal static IReturnType GetSharedReturnType (int index)
		{
			return returnTypeTable[index];
		}
		
		public static IReturnType GetSharedReturnType (string invariantString)
		{
			int index;
			if (tableIndex.TryGetValue (invariantString, out index))
				return returnTypeTable[index];
			
/*			if (!table.ContainsKey (invariantString))
				table[invariantString] = 0;
			table[invariantString]++;*/
			
			return new DomReturnType (invariantString);
		}
		
/*		static Dictionary<string, int> table = new Dictionary<string, int> ();
		public static void PrintIndex ()
		{
			List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>> (table);
			list.Sort (delegate (KeyValuePair<string, int> left, KeyValuePair<string, int> right) {
				return left.Value.CompareTo (right.Value);
			});
			Console.WriteLine ("--------");
			foreach (KeyValuePair<string, int> p in list) {
				if (p.Value < 400)
					continue;
				Console.WriteLine (p.Key  + "-" + p.Value);
			}
		}*/
		
		public static IReturnType GetSharedReturnType (IReturnType returnType, bool nullIfNotShared = false)
		{
			if (returnType == null)
				return null;
			if (returnType.PointerNestingLevel != 0 || returnType.ArrayDimensions != 0 || returnType.GenericArguments.Count != 0)
				return nullIfNotShared ? null : returnType;
			
			string invariantString = returnType.ToInvariantString ();
			int index;
			if (tableIndex.TryGetValue (invariantString, out index))
				return returnTypeTable[index];
			/*
			if (!table.ContainsKey (invariantString))
				table[invariantString] = 0;
			table[invariantString]++;*/
			
			return nullIfNotShared ? null : returnType;
		}
		
#endregion
	}
}
