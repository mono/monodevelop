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
		string                    name;
		ReturnTypeList            baseTypes;
		SpecialConstraintType     specialConstraints;
		
		public GenericParameter() {
		}
		
		public GenericParameter(string name
		                      , ReturnTypeList baseTypes
		                      , SpecialConstraintType specialConstraints) {
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
				return (specialConstraints & SpecialConstraintType.New) > 0;
			}
			set {
				if (value)
					specialConstraints |= SpecialConstraintType.New;
				else
					specialConstraints &= ~SpecialConstraintType.New;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'struct' constraint applied to it (meaning that it can represent
		/// anything that is not a nullable type).
		/// </summary>
		public bool HasStructConstraint {
			get {
				return (specialConstraints & SpecialConstraintType.Struct) > 0;
			}
			set {
				if (value)
					specialConstraints |= SpecialConstraintType.Struct;
				else
					specialConstraints &= ~SpecialConstraintType.Struct;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'class' constraint applied to it (meaning that it can represent
		/// only reference type).
		/// </summary>
		public bool HasClassConstraint {
			get {
				return (specialConstraints & SpecialConstraintType.Class) > 0;
			}
			set {
				if (value)
					specialConstraints |= SpecialConstraintType.Class;
				else
					specialConstraints &= ~SpecialConstraintType.Class;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'covariant' constraint applied to it.
		/// </summary>
		public bool HasCovariantConstraint {
			get {
				return (specialConstraints & SpecialConstraintType.Covariant) > 0;
			}
			set {
				if (value)
					specialConstraints |= SpecialConstraintType.Covariant;
				else
					specialConstraints &= ~SpecialConstraintType.Covariant;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether this parameter has the
		/// 'contravariant' constraint applied to it.
		/// </summary>
		public bool HasContravariantConstraint {
			get {
				return (specialConstraints & SpecialConstraintType.Contravariant) > 0;
			}
			set {
				if (value)
					specialConstraints |= SpecialConstraintType.Contravariant;
				else
					specialConstraints &= ~SpecialConstraintType.Contravariant;
			}
		}
		
		/// <summary>
		/// Gets or sets a value that indicates what kind of special constraints
		/// this parameter has applied to it.
		/// </summary>
		public SpecialConstraintType SpecialConstraints {
			get {
				return specialConstraints;
			}
			set {
				specialConstraints = value;
			}
		}
	}
	
	public enum SpecialConstraintType : byte
	{
		Class = 0x01,
		Struct = 0x02,
		New = 0x04,
		Covariant = 0x08,
		Contravariant = 0x10
	}
}
