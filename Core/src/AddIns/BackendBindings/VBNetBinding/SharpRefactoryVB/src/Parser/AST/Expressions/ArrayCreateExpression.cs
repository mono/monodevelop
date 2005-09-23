using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class ArrayCreateExpression : Expression
	{
		TypeReference              createType       = null;
		ArrayList                  parameters       = null; // Expressions
		ArrayInitializerExpression arrayInitializer = null; // Array Initializer OR NULL
		
		public TypeReference CreateType {
			get {
				return createType;
			}
			set {
				createType = value;
			}
		}
		
		public ArrayList Parameters {
			get {
				return parameters;
			}
			set {
				parameters = value;
			}
		}
		
		public ArrayInitializerExpression ArrayInitializer
		{
			get {
				return arrayInitializer;
			}
			set {
				arrayInitializer = value;
			}
		}
		
		public ArrayCreateExpression(TypeReference createType, ArrayList parameters)
		{
			this.createType = createType;
			this.parameters = parameters;
		}
		
		public ArrayCreateExpression(TypeReference createType, ArrayInitializerExpression arrayInitializer)
		{
			this.createType = createType;
			this.arrayInitializer = arrayInitializer;
		}
		
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
