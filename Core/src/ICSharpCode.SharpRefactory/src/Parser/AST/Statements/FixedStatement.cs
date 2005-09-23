using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class FixedStatement : Statement
	{
		TypeReference type;
		ArrayList     pointerDeclarators = new ArrayList();
		Statement     embeddedStatement;
		
		public TypeReference TypeReference {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public ArrayList PointerDeclarators {
			get {
				return pointerDeclarators;
			}
			set {
				pointerDeclarators = value;
			}
		}
		
		public Statement EmbeddedStatement {
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		
		public FixedStatement(TypeReference type)
		{
			this.type = type;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[FixedStatement: Type={0}, PointerDeclarators={1}, EmbeddedStatement={2}]", 
			                     type,
			                     GetCollectionString(pointerDeclarators),
			                     embeddedStatement);
		}
	}
}
