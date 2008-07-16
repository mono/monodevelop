//
// IMember.cs
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
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom
{
	public interface IMember : IComparable, IDomVisitable
	{
		string FullName {
			get;
		}
		
		IReturnType ReturnType {
			get;
			set;
		}
		
		IType DeclaringType {
			get;
			set;
		}
		
		IEnumerable<IReturnType> ExplicitInterfaces {
			get;
		}
		
		string Name {
			get;
			set;
		}
		
		string Documentation {
			get;
			set;
		}
		
		DomLocation Location {
			get;
			set;
		}
		
		DomRegion BodyRegion {
			get;
			set;
		}
		
		Modifiers Modifiers {
			get;
			set;
		}
		
		IEnumerable<IAttribute> Attributes {
			get;
		}
		
		string HelpUrl {
			get;
		}
		
		string StockIcon {
			get;
		}
		
		bool IsObsolete {
			get;
			set;
		}
		
		bool IsAccessibleFrom (ProjectDom dom, IMember member);
		
		#region ModifierAccessors
		bool IsPrivate   { get; }
		bool IsInternal  { get; }
		bool IsProtected { get; }
		bool IsPublic    { get; }
		bool IsProtectedAndInternal { get; }
		bool IsProtectedOrInternal { get; }
		
		bool IsAbstract  { get; }
		bool IsVirtual   { get; }
		bool IsSealed    { get; }
		bool IsStatic    { get; }
		bool IsOverride  { get; }
		bool IsReadonly  { get; }
		bool IsConst	 { get; }
		bool IsNew       { get; }
		bool IsPartial   { get; }
		
		bool IsExtern    { get; }
		bool IsVolatile  { get; }
		bool IsUnsafe    { get; }
		bool IsOverloads  { get; }
		bool IsWithEvents { get; }
		bool IsDefault    { get; }
		bool IsFixed      { get; }
		
		bool IsSpecialName { get; }
		bool IsFinal       { get; }
		bool IsLiteral     { get; }
		#endregion		
		
	}
}
