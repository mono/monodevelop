//
// LocalVariable.cs
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
using System.Collections;
using System.Collections.Specialized;

namespace MonoDevelop.Projects.Dom
{
	public class LocalVariable : IDomVisitable
	{
		string name;
		IReturnType returnType;
		DomRegion region;
		IMember declaringMember;
		
		public virtual string StockIcon {
			get {
				return Stock.Field;
			}
		}
		
		public LocalVariable (IMember declaringMember, string name, IReturnType type, DomRegion region)
		{
			this.declaringMember = declaringMember;
			this.name = name;
			this.returnType = type;
			this.region = region;
		}
		
		public IMember DeclaringMember {
			get {
				return declaringMember;
			}
		}
		
		public ICompilationUnit CompilationUnit {
			get {
				return declaringMember.DeclaringType.CompilationUnit;
			}
		}
		
		public string FileName {
			get {
				return declaringMember.DeclaringType.CompilationUnit.FileName;
			}
		}
		public string Name {
			get { return name; }
		}
		
		public IReturnType ReturnType {
			get { return returnType; }
		}
		
		public DomRegion Region {
			get { return region; }
		}
/*
		public virtual int CompareTo (object value)
		{
			LocalVariable loc = (LocalVariable) value;
			
			int res = name.CompareTo (loc.name);
			if (res != 0) return res;
			
			return returnType.CompareTo (loc.returnType);
		}
		
		public override bool Equals (object ob)
		{
			LocalVariable other = ob as LocalVariable;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			return name.GetHashCode () + returnType.GetHashCode ();
		}*/
		
		public S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
	}
}
