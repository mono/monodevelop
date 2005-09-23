using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST {
	
	public class FieldReferenceExpression : Expression
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
		
		public FieldReferenceExpression(Expression targetObject, string fieldName)
		{
			this.targetObject = targetObject;
			this.fieldName = fieldName;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[FieldReferenceExpression: FieldName={0}, TargetObject={1}]",
			                     fieldName,
			                     targetObject);
		}
	}
}
