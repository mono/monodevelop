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
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom
{
	public class LocalVariable : AbstractNode, IBaseMember
	{
		public string Name {
			get;
			set;
		}
		
		public DomLocation Location {
			get;
			private set;
		}
		
		public IReturnType ReturnType {
			get;
			set;
		}
		
		public IconId StockIcon {
			get {
				return Stock.Field;
			}
		}
		
		public MemberType MemberType {
			get {
				return MemberType.LocalVariable;
			}
		}
		
		public IMember DeclaringMember {
			get;
			set;
		}
		
		public ICompilationUnit CompilationUnit {
			get {
				return DeclaringMember.DeclaringType.CompilationUnit;
			}
		}
		
		public string FileName {
			get {
				return DeclaringMember.DeclaringType.CompilationUnit.FileName;
			}
		}
		
		public DomRegion Region {
			get;
			private set;
		}
	
		public LocalVariable (IMember declaringMember, string name, IReturnType type, DomRegion region)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (type == null)
				throw new ArgumentNullException ("type");
			this.DeclaringMember = declaringMember;
			this.Name = name;
			this.ReturnType = type;
			this.Region = region;
			this.Location = region.Start;
		}
		
		public override S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
		
	}
}
