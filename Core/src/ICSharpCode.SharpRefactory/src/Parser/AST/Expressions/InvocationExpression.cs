using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST {
	
	public class InvocationExpression : Expression
	{
		Expression  targetObject;
		ArrayList   parameters; // Expression list
		
		public Expression TargetObject {
			get {
				return targetObject;
			}
			set {
				targetObject = value;
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
		
		public InvocationExpression(Expression targetObject, ArrayList parameters)
		{
			this.targetObject = targetObject;
			this.parameters = parameters;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[InvocationExpression: TargetObject={0}, parameters={1}]",
			                     targetObject,
			                     GetCollectionString(parameters));
		}
	}
}

