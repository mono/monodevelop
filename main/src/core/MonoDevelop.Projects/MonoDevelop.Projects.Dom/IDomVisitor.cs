//
// IDomVisitor.cs
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

namespace MonoDevelop.Projects.Dom
{
	public interface IDomVisitable
	{
		S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data);
	}
	
	public interface IDomVisitor<T, S>
	{
		S Visit (ICompilationUnit unit, T data);
		
		S Visit (IAttribute attribute, T data);
		
		S Visit (IType type, T data);
		
		S Visit (IField field, T data);
		S Visit (IMethod method, T data);
		S Visit (IProperty property, T data);
		S Visit (IEvent evt, T data);
		
		S Visit (IReturnType returnType, T data);
		S Visit (IParameter parameter, T data);
		S Visit (IUsing u, T data);
		S Visit (Namespace namesp, T data);
		
		S Visit (LocalVariable var, T data);
	}

	public abstract class AbstractDomVistitor<T, S> : IDomVisitor<T, S>
	{
		public virtual S Visit (ICompilationUnit unit, T data)
		{
			if (unit != null) {
				foreach (IUsing u in unit.Usings) 
					u.AcceptVisitor (this, data);
				foreach (IType type in unit.Types) 
					type.AcceptVisitor (this, data);
			}
			return default(S);
		}
		
		public virtual S Visit (IAttribute attribute, T data)
		{
			return default(S);
		}
		
		public virtual S Visit (IType type, T data)
		{
	//		foreach (IMember member in type.Members)
	//			member.AcceptVisitor (this, data);
			return default(S);
		}
		
		public virtual S Visit (IField field, T data)
		{
			return default(S);
		}
		
		public virtual S Visit (IMethod method, T data)
		{
			foreach (IParameter param in method.Parameters) {
				param.AcceptVisitor (this, data);
			}
			return default(S);
		}
		
		public virtual S Visit (IProperty property, T data)
		{
			foreach (IParameter param in property.Parameters) {
				param.AcceptVisitor (this, data);
			}
			return default(S);
		}
		
		public virtual S Visit (IEvent evt, T data)
		{
			return default(S);
		}
		
		public virtual S Visit (IReturnType returnType, T data)
		{
			return default(S);
		}
		
		public virtual S Visit (IParameter parameter, T data)
		{
			return default(S);
		}
		
		public virtual S Visit (IUsing u, T data)
		{
			return default(S);
		}
		
		public virtual S Visit (Namespace namesp, T data)
		{
			return default(S);
		}
		
		public virtual S Visit (LocalVariable var, T data)
		{
			return default(S);
		}
	}
}
