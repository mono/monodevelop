//
// DomMemberDecorator.cs
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
	/// <summary>
	/// Decorates an IMember object.
	/// </summary>
	class DomMemberDecorator : IMember
	{
		IMember member;
		
		#region IMember implementation 
		
		public virtual MemberType MemberType {
			get {
				return member.MemberType;
			}
		}
		
		public virtual System.Xml.XmlNode GetMonodocDocumentation ()
		{
			return member.GetMonodocDocumentation ();
		}
		
		public virtual bool IsAccessibleFrom (ProjectDom dom, IType calledType, IMember member, bool includeProtected)
		{
			return member.IsAccessibleFrom (dom, calledType, member, includeProtected);
		}
		
		public virtual string FullName {
			get {
				return member.FullName;
			}
		}
		
		public virtual IReturnType ReturnType {
			get {
				return member.ReturnType;
			}
		}
		
		public virtual IType DeclaringType {
			get {
				return member.DeclaringType;
			}
		}
		
		public virtual IEnumerable<IReturnType> ExplicitInterfaces {
			get {
				return member.ExplicitInterfaces;
			}
		}
		
		public virtual string Name {
			get {
				return member.Name;
			}
		}
		
		public virtual string Documentation {
			get {
				return member.Documentation;
			}
		}
		
		public virtual DomLocation Location {
			get {
				return member.Location;
			}
		}
		
		public virtual DomRegion BodyRegion {
			get {
				return member.BodyRegion;
			}
		}
		
		public virtual Modifiers Modifiers {
			get {
				return member.Modifiers;
			}
		}
		
		public virtual IEnumerable<IAttribute> Attributes {
			get {
				return member.Attributes;
			}
		}
		
		public virtual string HelpUrl {
			get {
				return member.HelpUrl;
			}
		}
		
		public virtual string StockIcon {
			get {
				return member.StockIcon;
			}
		}
		
		public virtual bool IsExplicitDeclaration {
			get {
				return member.IsExplicitDeclaration;
			}
		}
		
		public virtual bool IsObsolete {
			get {
				return member.IsObsolete;
			}
		}
		
		public virtual bool IsPrivate {
			get {
				return member.IsPrivate;
			}
		}
		
		public virtual bool IsInternal {
			get {
				return member.IsInternal;
			}
		}
		
		public virtual bool IsProtected {
			get {
				return member.IsProtected;
			}
		}
		
		public virtual bool IsPublic {
			get {
				return member.IsPublic;
			}
		}
		
		public virtual bool IsProtectedAndInternal {
			get {
				return member.IsProtectedAndInternal;
			}
		}
		
		public virtual bool IsProtectedOrInternal {
			get {
				return member.IsProtectedOrInternal;
			}
		}
		
		public virtual bool IsAbstract {
			get {
				return member.IsAbstract;
			}
		}
		
		public virtual bool IsVirtual {
			get {
				return member.IsVirtual;
			}
		}
		
		public virtual bool IsSealed {
			get {
				return member.IsSealed;
			}
		}
		
		public virtual bool IsStatic {
			get {
				return member.IsStatic;
			}
		}
		
		public virtual bool IsOverride {
			get {
				return member.IsOverride;
			}
		}
		
		public virtual bool IsReadonly {
			get {
				return member.IsReadonly;
			}
		}
		
		public virtual bool IsConst {
			get {
				return member.IsConst;
			}
		}
		
		public virtual bool IsNew {
			get {
				return member.IsNew;
			}
		}
		
		public virtual bool IsPartial {
			get {
				return member.IsPartial;
			}
		}
		
		public virtual bool IsExtern {
			get {
				return member.IsExtern;
			}
		}
		
		public virtual bool IsVolatile {
			get {
				return member.IsVolatile;
			}
		}
		
		public virtual bool IsUnsafe {
			get {
				return member.IsUnsafe;
			}
		}
		
		public virtual bool IsOverloads {
			get {
				return member.IsOverloads;
			}
		}
		
		public virtual bool IsWithEvents {
			get {
				return member.IsWithEvents;
			}
		}
		
		public virtual bool IsDefault {
			get {
				return member.IsDefault;
			}
		}
		
		public virtual bool IsFixed {
			get {
				return member.IsFixed;
			}
		}
		
		public virtual bool IsSpecialName {
			get {
				return member.IsSpecialName;
			}
		}
		
		public virtual bool IsFinal {
			get {
				return member.IsFinal;
			}
		}
		
		public virtual bool IsLiteral {
			get {
				return member.IsLiteral;
			}
		}
		
		#endregion 
		
		
		public DomMemberDecorator (IMember member)
		{
			this.member = member;
		}

		#region IDomVisitable implementation 
		
		public virtual S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return member.AcceptVisitor (visitor, data);
		}

		#region IComparable implementation 
		
		public virtual int CompareTo (object other)
		{
			return member.CompareTo (other);
		}
		
		#endregion 
		
		#endregion 
	}
}
