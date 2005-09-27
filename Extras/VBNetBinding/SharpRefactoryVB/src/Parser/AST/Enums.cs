using System;

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	/// <summary>
	/// The type of a type declaration.
	/// </summary>
	public enum Types
	{
		Class,
		Interface,
		Structure,
		Module,
		Enum
	}
	
	///<summary>
	/// Compare type, used in the <c>Option Compare</c>
	/// pragma.
	///</summary>
	public enum CompareType
	{
		Binary,
		Text
	}
	
	///<summary>
	/// Charset types, used in external mehtods
	/// declarations.
	///</summary>
	public enum CharsetModifier
	{
		None,
		Auto,
		Unicode,
		ANSI
	}
	
	public enum ParentType
	{
		ClassOrStruct,
		InterfaceOrEnum,
		Namespace,
		Unknown
	}
	
	public enum Members
	{
		Constant,
		Field,
		Method,
		Property,
		Event,
		Constructor,
		StaticConstructor,
		NestedType
	}
	
	///<summary>
	/// Used at the exit statement.
	///</summary>
	public enum ExitType
	{
		None,
		Sub,
		Function,
		Property,
		Do,
		For,
		While,
		Select,
		Try
	}
	
	public enum ConditionType
	{
		None,
		Until,
		While
	}
	
	public enum ConditionPosition
	{
		None,
		Start,
		End
	}
	
	public enum UnaryOperatorType
	{
		None,
		Not,
		BitNot,
		
		Minus,
		Plus,
		
		Increment,
		Decrement,
		
		PostIncrement,
		PostDecrement,
		
		Star,
		BitWiseAnd
	}
	
	public enum AssignmentOperatorType
	{
		None,
		Assign,
		
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulus,
		
		ConcatString,
		
		ShiftLeft,
		ShiftRight,
		
		BitwiseAnd,
		BitwiseOr,
		ExclusiveOr,
		Power,
		DivideInteger
	}
	
	public enum BinaryOperatorType
	{
		None,
		Add,
		Concat,
		BitwiseAnd,
		BitwiseOr,
		BooleanAnd,
		BooleanOr,
		Divide,
		GreaterThan,
		GreaterThanOrEqual,
		Equality,
		InEquality,
		LessThan,
		LessThanOrEqual,
		Modulus,
		Multiply,
		Subtract,
		DivideInteger,
		Power,
		
		// additional
		ShiftLeft,
		ShiftRight,
		IS,
		ExclusiveOr,
		Like,
	}
}
