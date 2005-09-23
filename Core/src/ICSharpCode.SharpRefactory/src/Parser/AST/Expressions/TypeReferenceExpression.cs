using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST {
	
	public class TypeReferenceExpression : Expression
	{
		TypeReference  typeReference;
		
		public TypeReference TypeReference {
			get {
				return typeReference;
			}
			set {
				typeReference = value;
			}
		}
		
		public TypeReferenceExpression(string type)
		{
			this.typeReference = new TypeReference(type);
		}
		public TypeReferenceExpression(TypeReference  typeReference)
		{
			this.typeReference = typeReference;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[TypeReferenceExpression: TypeReference={0}]", 
			                     typeReference);
		}
	}
}
