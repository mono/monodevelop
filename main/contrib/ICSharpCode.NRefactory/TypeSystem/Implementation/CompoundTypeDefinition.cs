// 
// CompoundTypeDefinition.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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

using ICSharpCode.NRefactory.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public sealed class CompoundTypeDefinition : AbstractFreezable, ITypeDefinition
	{
		List<ITypeDefinition> parts = new List<ITypeDefinition> ();
		
		public CompoundTypeDefinition ()
		{
		}
		
		public void AddPart (ITypeDefinition part)
		{
			parts.Add (part);
		}
		
		#region ITypeDefinition implementation
		public IType Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			return this;
		}

		public bool? IsReferenceType (ITypeResolveContext context)
		{
			return FirstPart.IsReferenceType (context);
		}

		public ITypeDefinition GetDefinition()
		{
			return this;
		}

		public IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitTypeDefinition(this);
		}
		
		public IType VisitChildren(TypeVisitor visitor)
		{
			return this;
		}

		public IEnumerable<IType> GetBaseTypes (ITypeResolveContext context)
		{
			foreach (var part in parts) {
				foreach (var baseType in part.GetBaseTypes (context))
					yield return baseType;
			}
		}

		public IEnumerable<IType> GetNestedTypes (ITypeResolveContext context, Predicate<ITypeDefinition> filter)
		{
			foreach (var part in parts) {
				foreach (var nestedType in part.GetNestedTypes (context))
					yield return nestedType;
			}
		}

		public IEnumerable<IMethod> GetMethods (ITypeResolveContext context, Predicate<IMethod> filter)
		{
			foreach (var part in parts) {
				foreach (var method in part.GetMethods (context))
					yield return method;
			}
		}

		public IEnumerable<IMethod> GetConstructors (ITypeResolveContext context, Predicate<IMethod> filter)
		{
			foreach (var part in parts) {
				foreach (var constructor in part.GetConstructors (context))
					yield return constructor;
			}
		}

		public IEnumerable<IProperty> GetProperties (ITypeResolveContext context, Predicate<IProperty> filter)
		{
			foreach (var part in parts) {
				foreach (var property in part.GetProperties (context))
					yield return property;
			}
		}

		public IEnumerable<IField> GetFields (ITypeResolveContext context, Predicate<IField> filter)
		{
			foreach (var part in parts) {
				foreach (var field in part.GetFields (context))
					yield return field;
			}
		}

		public IEnumerable<IEvent> GetEvents (ITypeResolveContext context, Predicate<IEvent> filter)
		{
			foreach (var part in parts) {
				foreach (var evt in part.GetEvents (context))
					yield return evt;
			}
		}

		public IEnumerable<IMember> GetMembers (ITypeResolveContext context, Predicate<IMember> filter)
		{
			foreach (var part in parts) {
				foreach (var member in part.GetMembers (context))
					yield return member;
			}
		}

		public ITypeDefinition GetCompoundClass ()
		{
			return this;
		}

		public IList<ITypeDefinition> GetParts ()
		{
			return parts;
		}
		
		
		ITypeDefinition FirstPart {
			get {
				if (parts.Count == 0)
					throw new NotSupportedException ("empty compound type");
				return parts [0];
			}
		}
		
		public string FullName {
			get {
				return FirstPart.FullName;
			}
		}

		public string Name {
			get {
				return FirstPart.Name;
			}
		}

		public string Namespace {
			get {
				return FirstPart.Namespace;
			}
		}

		public string ReflectionName {
			get {
				return FirstPart.ReflectionName;
			}
		}

		public IType DeclaringType {
			get {
				return SharedTypes.UnknownType;
			}
		}

		public int TypeParameterCount {
			get {
				return FirstPart.TypeParameterCount;
			}
		}
		public EntityType EntityType {
			get {
				return FirstPart.EntityType;
			}
		}

		public DomRegion Region {
			get {
				return DomRegion.Empty;
			}
		}

		public DomRegion BodyRegion {
			get {
				return DomRegion.Empty;
			}
		}

		public ITypeDefinition DeclaringTypeDefinition {
			get {
				return null;
			}
		}

		public IList<IAttribute> Attributes {
			get {
				var result = new List<IAttribute> ();
				parts.ForEach (p => result.AddRange (p.Attributes));
				return result.AsReadOnly ();
			}
		}

		public string Documentation {
			get {
				return FirstPart.Documentation;
			}
		}

		public Accessibility Accessibility {
			get {
				return FirstPart.Accessibility;
			}
		}

		public bool IsStatic {
			get {
				return FirstPart.IsStatic;
			}
		}

		public bool IsAbstract {
			get {
				return FirstPart.IsAbstract;
			}
		}

		public bool IsSealed {
			get {
				return FirstPart.IsSealed;
			}
		}

		public bool IsShadowing {
			get {
				return FirstPart.IsShadowing;
			}
		}

		public bool IsSynthetic {
			get {
				return FirstPart.IsSynthetic;
			}
		}

		public bool IsPrivate {
			get { return Accessibility == Accessibility.Private; }
		}
		
		public bool IsPublic {
			get { return Accessibility == Accessibility.Public; }
		}
		
		public bool IsProtected {
			get { return Accessibility == Accessibility.Protected; }
		}
		
		public bool IsInternal {
			get { return Accessibility == Accessibility.Internal; }
		}
		
		public bool IsProtectedOrInternal {
			get { return Accessibility == Accessibility.ProtectedOrInternal; }
		}
		
		public bool IsProtectedAndInternal {
			get { return Accessibility == Accessibility.ProtectedAndInternal; }
		}
		

		public IProjectContent ProjectContent {
			get {
				return FirstPart.ProjectContent;
			}
		}

		public ClassType ClassType {
			get {
				return FirstPart.ClassType;
			}
		}

		public IList<ITypeReference> BaseTypes {
			get {
				var result = new List<ITypeReference> ();
				parts.ForEach (p => result.AddRange (p.BaseTypes));
				return result.AsReadOnly ();
			}
		}

		public IList<ITypeParameter> TypeParameters {
			get {
				return FirstPart.TypeParameters;
			}
		}

		public IList<ITypeDefinition> NestedTypes {
			get {
				var result = new List<ITypeDefinition> ();
				parts.ForEach (p => result.AddRange (p.NestedTypes));
				return result.AsReadOnly ();
			}
		}

		public IList<IField> Fields {
			get {
				var result = new List<IField> ();
				parts.ForEach (p => result.AddRange (p.Fields));
				return result.AsReadOnly ();
			}
		}

		public IList<IProperty> Properties {
			get {
				var result = new List<IProperty> ();
				parts.ForEach (p => result.AddRange (p.Properties));
				return result.AsReadOnly ();
			}
		}

		public IList<IMethod> Methods {
			get {
				var result = new List<IMethod> ();
				parts.ForEach (p => result.AddRange (p.Methods));
				return result.AsReadOnly ();
			}
		}

		public IList<IEvent> Events {
			get {
				var result = new List<IEvent> ();
				parts.ForEach (p => result.AddRange (p.Events));
				return result.AsReadOnly ();
			}
		}

		public IEnumerable<IMember> Members {
			get {
				var result = new List<IMember> ();
				parts.ForEach (p => result.AddRange (p.Members));
				return result.AsReadOnly ();
			}
		}

		public bool HasExtensionMethods {
			get {
				return parts.Any (p => p.HasExtensionMethods);
			}
		}
		#endregion
		
		bool IEquatable<IType>.Equals(IType other)
		{
			return this == other;
		}
	}
}
