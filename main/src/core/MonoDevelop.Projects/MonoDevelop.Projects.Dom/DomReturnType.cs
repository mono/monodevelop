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
		protected bool isNullable;
		protected List<IReturnType> typeParameters = new List<IReturnType> ();
		
		public static readonly DomReturnType Void = new DomReturnType (null, false, null);
		
		public string FullName {
			get {
				return !String.IsNullOrEmpty (nspace) ? nspace + "." + name : name;
			}
		}

		public string Name {
			get {
				return name;
			}
		}

		public string Namespace {
			get {
				return nspace;
			}
		}
		public int PointerNestingLevel {
			get {
				return pointerNestingLevel;
			}
		}
		
		public int ArrayDimensions {
			get {
				return arrayDimensions;
			}
		}
		
		public bool IsNullable {
			get {
				return this.isNullable;
			}
		}
		
		public System.Collections.ObjectModel.ReadOnlyCollection<IReturnType> TypeParameters {
			get {
				return typeParameters.AsReadOnly ();
			}
		}
		
		protected DomReturnType ()
		{
		}
		
		public DomReturnType (string name) : this (name, false, new List<IReturnType> ())
		{
		}
		
		public DomReturnType (string name, bool isNullable, List<IReturnType> typeParameters)
		{
			this.name           = name;
			this.isNullable     = isNullable;
			this.typeParameters = typeParameters;
		}
		
		public object AcceptVisitior (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
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
			StringBuilder sb = new StringBuilder (DomType.GetInstantiatedTypeName (type.FullName, type.TypeParameters));
			
			if (type.PointerNestingLevel > 0)
				sb.Append (new String ('*', type.PointerNestingLevel));
			
			if (type.ArrayDimensions > 0) {
				for (int i = 0; i < type.ArrayDimensions; i++) {
					sb.Append ("[]");
				}
			}
			
			return sb.ToString ();
		}
	}
}
