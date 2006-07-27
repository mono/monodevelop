//
// GenericParameter.cs: Represents a type parameter for generic types. It stores
//                      constraint information.
//
// Author:
//   Matej Urbas (matej.urbas@gmail.com)
//
// (C) 2006 Matej Urbas
// 

using System;
using System.Collections.Generic;
using MonoDevelop.Projects.Parser;
using Mono.Cecil;
using System.Reflection;

namespace MonoDevelop.Projects.Parser
{
	/// <summary>
	/// Contains information about generic parameters.
	/// <para>For example, if a class would be defined like this:
	/// <code>
	/// public class MyClass<T> where T : SomeType, new()
	/// {
	/// }
	/// </code>
	/// Then <c>T</c> is the <c>name</c> of the only generic parameter and
	/// <c>SomeType<c> and <c>new()</c> are constraints for this parameter.
	/// <c>SomeType</c> is thus an element in the <c>baseTypes</c> list.
	/// </para>
	/// </summary>
	public class GenericParameter
	{
		string                      name;
		ReturnTypeList              baseTypes;
		GenericParameterAttributes  specialConstraints;
		
		public GenericParameter() {
		}
		
		public GenericParameter(string name
		                      , ReturnTypeList baseTypes
		                      , GenericParameterAttributes specialConstraints) {
			this.name               = name;
			this.baseTypes          = baseTypes;
			this.specialConstraints = specialConstraints;
		}
		
		/// <summary>
		/// Gets or sets the collection of base types that constrain this
		/// parameter.
		/// <param>A parameter is not constrained if this property returns
		/// <c>null</c> or an empty <c>List</c>.</param>
		/// </summary>
		public ReturnTypeList BaseTypes {
			get {
				return baseTypes;
			}
			set {
				baseTypes = value;
			}
		}
		
		/// <summary>
		/// Returns the name of this parameter.
		/// </summary>
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'new' constraint applied to it (meaning that it has the default
		/// constructor).
		/// </summary>
		public bool HasNewConstraint {
			get {
				return (specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) > 0;
			}
			set {
				if (value)
					specialConstraints |= GenericParameterAttributes.DefaultConstructorConstraint;
				else
					specialConstraints &= ~GenericParameterAttributes.DefaultConstructorConstraint;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'struct' constraint applied to it (meaning that it can represent
		/// anything that is not a nullable type).
		/// </summary>
		public bool HasStructConstraint {
			get {
				return (specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) > 0;
			}
			set {
				if (value)
					specialConstraints |= GenericParameterAttributes.NotNullableValueTypeConstraint;
				else
					specialConstraints &= ~GenericParameterAttributes.NotNullableValueTypeConstraint;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'class' constraint applied to it (meaning that it can represent
		/// only reference type).
		/// </summary>
		public bool HasClassConstraint {
			get {
				return (specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) > 0;
			}
			set {
				if (value)
					specialConstraints |= GenericParameterAttributes.ReferenceTypeConstraint;
				else
					specialConstraints &= ~GenericParameterAttributes.ReferenceTypeConstraint;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'covariant' constraint applied to it.
		/// </summary>
		public bool HasCovariantConstraint {
			get {
				return (specialConstraints & GenericParameterAttributes.Covariant) > 0;
			}
			set {
				if (value)
					specialConstraints |= GenericParameterAttributes.Covariant;
				else
					specialConstraints &= ~GenericParameterAttributes.Covariant;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'contravariant' constraint applied to it.
		/// </summary>
		public bool HasContravariantConstraint {
			get {
				return (specialConstraints & GenericParameterAttributes.Contravariant) > 0;
			}
			set {
				if (value)
					specialConstraints |= GenericParameterAttributes.Contravariant;
				else
					specialConstraints &= ~GenericParameterAttributes.Contravariant;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates what kind of special constraints
		/// this parameter has applied to it.
		/// </summary>
		public GenericParameterAttributes SpecialConstraints {
			get {
				return specialConstraints;
			}
			set {
				specialConstraints = value;
			}
		}
	}
}
