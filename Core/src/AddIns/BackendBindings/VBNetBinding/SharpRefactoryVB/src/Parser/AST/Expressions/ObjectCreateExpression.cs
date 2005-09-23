using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class ObjectCreateExpression : Expression
	{
		TypeReference createType;
		ArrayList     parameters;
		
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
		
		public ObjectCreateExpression(TypeReference createType, ArrayList parameters)
		{
			this.createType = createType;
			this.parameters = parameters;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[ObjectCreateExpression: CreateType={0}, Parameters={1}]",
			                     createType,
			                     GetCollectionString(parameters));
		}
	}
}
