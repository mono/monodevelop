// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Base class for <see cref="ITypeParameter"/> implementations.
	/// </summary>
	[Serializable]
	public abstract class AbstractTypeParameter : AbstractFreezable, ITypeParameter
	{
		EntityType ownerType;
		int index;
		string name;
		
		IList<IAttribute> attributes;
		DomRegion region;
		VarianceModifier variance;
		
		protected override void FreezeInternal()
		{
			attributes = FreezeList(attributes);
			base.FreezeInternal();
		}
		
		protected AbstractTypeParameter(EntityType ownerType, int index, string name)
		{
			if (!(ownerType == EntityType.TypeDefinition || ownerType == EntityType.Method))
				throw new ArgumentException("owner must be a type or a method", "ownerType");
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, "Value must not be negative");
			if (name == null)
				throw new ArgumentNullException("name");
			this.ownerType = ownerType;
			this.index = index;
			this.name = name;
		}
		
		public EntityType OwnerType {
			get { return ownerType; }
		}
		
		public int Index {
			get { return index; }
		}
		
		public TypeKind Kind {
			get { return TypeKind.TypeParameter; }
		}
		
		public string Name {
			get { return name; }
		}
		
		string INamedElement.FullName {
			get { return name; }
		}
		
		string INamedElement.Namespace {
			get { return string.Empty; }
		}
		
		public string ReflectionName {
			get {
				if (this.OwnerType == EntityType.Method)
					return "``" + index.ToString();
				else
					return "`" + index.ToString();
			}
		}
		
		public abstract bool? IsReferenceType(ITypeResolveContext context);
		
		protected bool? IsReferenceTypeHelper(IType effectiveBaseClass)
		{
			// A type parameter is known to be a reference type if it has the reference type constraint
			// or its effective base class is not object or System.ValueType.
			if (effectiveBaseClass.Kind == TypeKind.Class || effectiveBaseClass.Kind == TypeKind.Delegate) {
				if (effectiveBaseClass.Namespace == "System" && effectiveBaseClass.TypeParameterCount == 0) {
					switch (effectiveBaseClass.Name) {
						case "Object":
						case "ValueType":
						case "Enum":
							return null;
					}
				}
				return true;
			} else if (effectiveBaseClass.Kind == TypeKind.Struct || effectiveBaseClass.Kind == TypeKind.Enum) {
				return false;
			}
			return null;
		}
		
		int IType.TypeParameterCount {
			get { return 0; }
		}
		
		IType IType.DeclaringType {
			get { return null; }
		}
		
		ITypeDefinition IType.GetDefinition()
		{
			return null;
		}
		
		IType ITypeReference.Resolve(ITypeResolveContext context)
		{
			return this;
		}
		
		public virtual bool Equals(IType other)
		{
			// Use reference equality for type parameters. While we could consider any types with same
			// ownerType + index as equal for the type system, doing so makes it difficult to cache calculation
			// results based on types - e.g. the cache in the Conversions class.
			return this == other;
			// We can still consider type parameters of different methods/classes to be equal to each other,
			// if they have been interned. But then also all constraints are equal, so caching conversions
			// are valid in that case.
		}
		
		public IList<IAttribute> Attributes {
			get {
				if (attributes == null)
					attributes = new List<IAttribute>();
				return attributes;
			}
		}
		
		public VarianceModifier Variance {
			get { return variance; }
			set {
				CheckBeforeMutation();
				variance = value;
			}
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitTypeParameter(this);
		}
		
		public IType VisitChildren(TypeVisitor visitor)
		{
			return this;
		}
		
		static readonly SimpleProjectContent dummyProjectContent = new SimpleProjectContent();
		
		DefaultTypeDefinition GetDummyClassForTypeParameter(ITypeParameterConstraints constraints)
		{
			DefaultTypeDefinition c = new DefaultTypeDefinition(dummyProjectContent, string.Empty, this.Name);
			c.Region = this.Region;
			if (constraints.HasValueTypeConstraint) {
				c.Kind = TypeKind.Struct;
			} else if (constraints.HasDefaultConstructorConstraint) {
				c.Kind = TypeKind.Class;
			} else {
				c.Kind = TypeKind.Interface;
			}
			return c;
		}
		
		public IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				ITypeParameterConstraints constraints = GetConstraints(context);
				if (constraints.HasDefaultConstructorConstraint || constraints.HasValueTypeConstraint) {
					DefaultMethod m = DefaultMethod.CreateDefaultConstructor(GetDummyClassForTypeParameter(constraints));
					if (filter(m))
						return new [] { m };
				}
				return EmptyList<IMethod>.Instance;
			} else {
				return GetMembersHelper.GetConstructors(this, context, filter, options);
			}
		}
		
		public IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetMethods(this, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetMethods(this, typeArguments, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IProperty>.Instance;
			else
				return GetMembersHelper.GetProperties(this, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IField>.Instance;
			else
				return GetMembersHelper.GetFields(this, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IEvent>.Instance;
			else
				return GetMembersHelper.GetEvents(this, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IMember> GetMembers(ITypeResolveContext context, Predicate<IMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMember>.Instance;
			else
				return GetMembersHelper.GetMembers(this, context, FilterNonStatic(filter), options);
		}
		
		static Predicate<T> FilterNonStatic<T>(Predicate<T> filter) where T : class, IMember
		{
			if (filter == null)
				return member => !member.IsStatic;
			else
				return member => !member.IsStatic && filter(member);
		}
		
		IEnumerable<IType> IType.GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return EmptyList<IType>.Instance;
		}
		
		IEnumerable<IType> IType.GetNestedTypes(IList<IType> typeArguments, ITypeResolveContext context, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return EmptyList<IType>.Instance;
		}
		
		public abstract IType GetEffectiveBaseClass(ITypeResolveContext context);
		
		public abstract IEnumerable<IType> GetEffectiveInterfaceSet(ITypeResolveContext context);
		
		public abstract ITypeParameterConstraints GetConstraints(ITypeResolveContext context);
		
		public virtual IEnumerable<IType> GetBaseTypes(ITypeResolveContext context)
		{
			ITypeParameterConstraints constraints = GetConstraints(context);
			bool hasNonInterfaceConstraint = false;
			foreach (IType c in constraints) {
				yield return c;
				if (c.Kind != TypeKind.Interface)
					hasNonInterfaceConstraint = true;
			}
			// Do not add the 'System.Object' constraint if there is another constraint with a base class.
			if (constraints.HasValueTypeConstraint || !hasNonInterfaceConstraint) {
				IType defaultBaseType = context.GetTypeDefinition("System", constraints.HasValueTypeConstraint ? "ValueType" : "Object", 0, StringComparer.Ordinal);
				if (defaultBaseType != null)
					yield return defaultBaseType;
			}
		}
		
		protected virtual void PrepareForInterning(IInterningProvider provider)
		{
			name = provider.Intern(name);
			attributes = provider.InternList(attributes);
		}
		
		protected virtual int GetHashCodeForInterning()
		{
			unchecked {
				int hashCode = index + name.GetHashCode();
				if (ownerType == EntityType.Method)
					hashCode += 7613561;
				if (attributes != null)
					hashCode += attributes.GetHashCode();
				hashCode += 900103 * (int)variance;
				return hashCode;
			}
		}
		
		protected bool EqualsForInterning(AbstractTypeParameter other)
		{
			return other != null
				&& this.attributes == other.attributes
				&& this.name == other.name
				&& this.ownerType == other.ownerType
				&& this.index == other.index
				&& this.variance == other.variance;
		}
		
		public override string ToString()
		{
			return this.name;
		}
	}
}
