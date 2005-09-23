using System;

namespace ICSharpCode.SharpRefactory.Parser
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public enum Types
	{
		Class,
		Interface,
		Struct,
		Enum
	}
	
	public enum ParentType
	{
		ClassOrStruct,
		InterfaceOrEnum,
		Namespace,
		Unknown
	}
	
	public enum FieldDirection {
		None,
		In,
		Out,
		Ref
	}
	
	public enum Members
	{
		Constant,
		Field,
		Method,
		Property,
		Event,
		Indexer,
		Operator,
		Constructor,
		StaticConstructor,
		Destructor,
		NestedType
	}
	
	public enum ParamModifiers
	{
		In,
		Out,
		Ref,
		Params
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
		
		ShiftLeft,
		ShiftRight,
		
		BitwiseAnd,
		BitwiseOr,
		ExclusiveOr,
	}
	
	public enum BinaryOperatorType
	{
		None,
		Add,
		BitwiseAnd,
		BitwiseOr,
		LogicalAnd,
		LogicalOr,
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
		ValueEquality,
		
		// additional
		ShiftLeft,
		ShiftRight,
		IS,
		AS,
		ExclusiveOr,
	}
	
	
}
