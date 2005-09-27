using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class LoopControlVariableExpression : Expression
	{
		Expression expression = null;
		string name;
		TypeReference type;
		
		public LoopControlVariableExpression(string name, TypeReference type)
		{
			this.name = name;
			this.type = type;
		}
		
		public LoopControlVariableExpression(Expression expression)
		{
			this.expression = expression;
		}
		
		public string Name
		{
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public TypeReference Type
		{
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public Expression Expression
		{
			get {
				return expression;
			}
			set {
				expression = value;
			}
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
