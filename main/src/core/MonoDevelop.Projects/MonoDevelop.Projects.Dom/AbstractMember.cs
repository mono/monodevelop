//
// AbstractMember.cs
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
	public abstract class AbstractMember : IMember
	{
		protected IReturnType returnType;
		protected List<IReturnType> explicitInterfaces = null;
		
		protected IType  declaringType;
		
		public IType DeclaringType {
			get {
				return declaringType;
			}
			set {
				this.declaringType = value;
				CalculateFullName ();
			}
		}
		
		public virtual string FullName {
			get {
				return fullName;
			}
		}
		
		protected virtual void CalculateFullName ()
		{
			fullName = DeclaringType != null ? DeclaringType.FullName + "." + Name : Name;
		}
		
		public virtual IReturnType ReturnType {
			get {
				return returnType;
			}
			set {
				returnType = value;
			}
		}
		
		public IEnumerable<IReturnType> ExplicitInterfaces {
			get {
				return (IEnumerable<IReturnType>)explicitInterfaces ?? new IReturnType [0];
			}
		}
		
		public bool IsExplicitDeclaration {
			get {
				return explicitInterfaces != null && explicitInterfaces.Count > 0;
			}
		}
		
		protected string name;
		protected string documentation;
		protected string fullName;
		
		protected DomRegion bodyRegion;
		protected DomLocation location;
		protected Modifiers modifiers;
		List<IAttribute> attributes = null;
		
		public virtual string Name {
			get {
				return name;
			}
			set {
				name = value;
				CalculateFullName ();
			}
		}
		
		public virtual string Documentation {
			get {
				return documentation;
			}
			set {
				documentation = value;
			}
		}
		
		public virtual DomLocation Location {
			get {
				return location;
			}
			set {
				location = value;
			}
		}
		
		public virtual DomRegion BodyRegion {
			get {
				return bodyRegion;
			}
			set {
				bodyRegion = value;
			}
		}
		
		public virtual Modifiers Modifiers {
			get {
				return modifiers;
			}
			set {
				modifiers = value;
			}
		}
		
		public bool IsObsolete {
			get {
				if (attributes == null)
					return false;
				foreach (IAttribute attr in attributes) {
					if (attr.Name.EndsWith ("Obsolete") || attr.Name.EndsWith ("ObsoleteAttribute"))
						return true;
				}
				return false;
			}
		}
		
		public virtual IEnumerable<IAttribute> Attributes {
			get {
				return (IEnumerable<IAttribute>)attributes ?? new IAttribute[0];
			}
		}
		
		public void AddExplicitInterface (IReturnType iface)
		{
			if (explicitInterfaces == null) 
				explicitInterfaces = new List<IReturnType> ();
			explicitInterfaces.Add (iface);
		}
		
		protected void ClearAttributes ()
		{
			if (attributes != null)
				attributes.Clear ();
		}
		
		public void Add (IAttribute attribute)
		{
			if (attributes == null)
				attributes = new List<IAttribute> ();
			attributes.Add (attribute);
		}
		
		public void AddRange (IEnumerable<IAttribute> attributes)
		{
			if (attributes == null)
				return;
			foreach (IAttribute attribute in attributes) {
				Add (attribute);
			}
		}
		
		/// <summary>
		/// This method is used to look up special methods that are connected to
		/// the member (like set/get method for events).
		/// </summary>
		/// <param name="prefix">
		/// A <see cref="System.String"/> for the prefix. For example the property Name has the method set_Name attacehd
		/// and 'set_' is the prefix.
		/// </param>
		/// <returns>
		/// A <see cref="IMethod"/> when the special method is found, null otherwise.
		/// </returns>
		protected IMethod LookupSpecialMethod (string prefix)
		{
			if (DeclaringType == null)
				return null;
			string specialMethodName = prefix + Name;
			foreach (IMethod method in DeclaringType.Methods) {
				if (method.IsSpecialName && method.Name == specialMethodName)
					return method;
			}
			return null;
		}
		
		public abstract string HelpUrl {
			get;
		}
		
		public abstract string StockIcon {
			get;
		}
		
		/// <summary>
		/// Help method used for getting the right icon for a member.
		/// </summary>
		/// <param name="modifier">
		/// A <see cref="Modifiers"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> 
		/// </returns>
		protected static int ModifierToOffset (Modifiers modifier)
		{
			if ((modifier & Modifiers.Private) == Modifiers.Private)
				return 1;
			if ((modifier & Modifiers.Protected) == Modifiers.Protected)
				return 2;
			if ((modifier & Modifiers.Internal) == Modifiers.Internal)
				return 3;
			return 0;
		}
		
		public bool IsAccessibleFrom (ProjectDom dom, IType calledType, IMember member)
		{
			if (member == null)
				return IsStatic;
	//		if (member.IsStatic && !IsStatic)
	//			return false;
			if (IsPublic || calledType != null && calledType.ClassType == ClassType.Interface)
				return true;
			if (IsInternal) {
				IType type1 = this is IType ? (IType)this : DeclaringType;
				IType type2 = member is IType ? (IType)member : member.DeclaringType;
				return type1.SourceProjectDom == type2.SourceProjectDom;
			}
			if (member.DeclaringType == null || DeclaringType == null)
				return false;
			
			if (IsProtected) {
//				foreach (IType type in dom.GetInheritanceTree (member.DeclaringType)) {
//					System.Console.WriteLine(type);
					if (member.DeclaringType.FullName == calledType.FullName)
						return true;
//				}
				return false;
			}
			// inner class 
			if (member.DeclaringType.DeclaringType == DeclaringType)
				return true;
			
			return DeclaringType.FullName == member.DeclaringType.FullName;
		}
		
		
		public virtual int CompareTo (object obj)
		{
			if (obj is IMember)
				return Name.CompareTo (((IMember)obj).Name);
			return 1;
		}
		
		#region ModifierAccessors
		public bool IsPrivate { 
			get {
				return (this.Modifiers & Modifiers.Private) == Modifiers.Private;
			}
		}
		public bool IsInternal { 
			get {
				return (this.Modifiers & Modifiers.Internal) == Modifiers.Internal;
			}
		}
		public bool IsProtected { 
			get {
				return (this.Modifiers & Modifiers.Protected) == Modifiers.Protected;
			}
		}
		public bool IsPublic { 
			get {
				return (this.Modifiers & Modifiers.Public) == Modifiers.Public;
			}
		}
		public bool IsProtectedAndInternal { 
			get {
				return (this.Modifiers & Modifiers.ProtectedAndInternal) == Modifiers.ProtectedAndInternal;
			}
		}
		public bool IsProtectedOrInternal { 
			get {
				return (this.Modifiers & Modifiers.ProtectedOrInternal) == Modifiers.ProtectedOrInternal;
			}
		}
		
		public bool IsAbstract { 
			get {
				return (this.Modifiers & Modifiers.Abstract) == Modifiers.Abstract;
			}
		}
		public bool IsVirtual { 
			get {
				return (this.Modifiers & Modifiers.Virtual) == Modifiers.Virtual;
			}
		}
		public bool IsSealed { 
			get {
				return (this.Modifiers & Modifiers.Sealed) == Modifiers.Sealed;
			}
		}
		public bool IsStatic { 
			get {
				return (this.Modifiers & Modifiers.Static) == Modifiers.Static;
			}
		}
		public bool IsOverride { 
			get {
				return (this.Modifiers & Modifiers.Override) == Modifiers.Override;
			}
		}
		public bool IsReadonly { 
			get {
				return (this.Modifiers & Modifiers.Readonly) == Modifiers.Readonly;
			}
		}
		public bool IsConst { 
			get {
				return (this.Modifiers & Modifiers.Const) == Modifiers.Const;
			}
		}
		public bool IsNew { 
			get {
				return (this.Modifiers & Modifiers.New) == Modifiers.New;
			}
		}
		public bool IsPartial { 
			get {
				return (this.Modifiers & Modifiers.Partial) == Modifiers.Partial;
			}
		}
		
		public bool IsExtern { 
			get {
				return (this.Modifiers & Modifiers.Extern) == Modifiers.Extern;
			}
		}
		public bool IsVolatile { 
			get {
				return (this.Modifiers & Modifiers.Volatile) == Modifiers.Volatile;
			}
		}
		public bool IsUnsafe { 
			get {
				return (this.Modifiers & Modifiers.Unsafe) == Modifiers.Unsafe;
			}
		}
		public bool IsOverloads { 
			get {
				return (this.Modifiers & Modifiers.Overloads) == Modifiers.Overloads;
			}
		}
		public bool IsWithEvents { 
			get {
				return (this.Modifiers & Modifiers.WithEvents) == Modifiers.WithEvents;
			}
		}
		public bool IsDefault { 
			get {
				return (this.Modifiers & Modifiers.Default) == Modifiers.Default;
			}
		}
		public bool IsFixed { 
			get {
				return (this.Modifiers & Modifiers.Fixed) == Modifiers.Fixed;
			}
		}
		
		public bool IsSpecialName { 
			get {
				return (this.Modifiers & Modifiers.SpecialName) == Modifiers.SpecialName;
			}
		}
		public bool IsFinal { 
			get {
				return (this.Modifiers & Modifiers.Final) == Modifiers.Final;
			}
		}
		public bool IsLiteral { 
			get {
				return (this.Modifiers & Modifiers.Literal) == Modifiers.Literal;
			}
		}
		#endregion
		
		public virtual System.Xml.XmlNode GetMonodocDocumentation ()
		{
			if (DeclaringType == null)
				return null;
			
			if (DeclaringType.HelpXml != null)  {
				System.Xml.XmlNode result = DeclaringType.HelpXml.SelectSingleNode ("/Type/Members/Member[@MemberName='" + Name + "']/Docs");
				return result;
			}
			return null;
		}
		
		public static void Resolve (IMember source, AbstractMember target, ITypeResolver typeResolver)
		{
			target.Name           = source.Name;
			target.Documentation  = source.Documentation;
			target.Modifiers      = source.Modifiers;
			target.ReturnType     = DomReturnType.Resolve (source.ReturnType, typeResolver);
			target.Location       = source.Location;
			target.BodyRegion     = source.BodyRegion;
			target.AddRange (DomAttribute.Resolve (source.Attributes, typeResolver));
		}
		
		public abstract object AcceptVisitior (IDomVisitor visitor, object data);
		
	}
}
