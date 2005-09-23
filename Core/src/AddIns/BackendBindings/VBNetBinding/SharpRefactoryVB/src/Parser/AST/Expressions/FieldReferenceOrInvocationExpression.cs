using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	
	public class FieldReferenceOrInvocationExpression : Expression
	{
		Expression targetObject;
		string fieldName;
		
		public Expression TargetObject {
			get {
				return targetObject;
			}
			set {
				targetObject = value;
			}
		}
		
		public string FieldName {
			get {
				return fieldName;
			}
			set {
				fieldName = value;
			}
		}
		
		public FieldReferenceOrInvocationExpression(Expression targetObject, string fieldName)
		{
			this.targetObject = targetObject;
			this.fieldName = fieldName;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			if(visitor==null) return null;
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[FieldReferenceOrInvocationExpression: FieldName={0}, TargetObject={1}]",
			                     fieldName,
			                     targetObject);
		}
	}
}
