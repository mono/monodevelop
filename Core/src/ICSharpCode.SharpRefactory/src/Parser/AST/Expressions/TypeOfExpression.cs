using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class TypeOfExpression : Expression
	{
		TypeReference typeReference;
		
		public TypeReference TypeReference {
			get {
				return typeReference;
			}
			set {
				typeReference = value;
			}
		}
		
		public TypeOfExpression(TypeReference typeReference)
		{
			this.typeReference  = typeReference;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[TypeOfExpression: TypeReference={0}]", 
			                     typeReference);
		}
	}
}
