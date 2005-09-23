using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class SizeOfExpression : Expression
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
		
		public SizeOfExpression(TypeReference typeReference)
		{
			this.typeReference  = typeReference;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[SizeOfExpression: TypeReference={0}]",
			                     typeReference);
		}
	}
}
