using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class LocalVariableDeclaration : Statement
	{
		TypeReference type;
		Modifier      modifier = Modifier.None;
		ArrayList     variables = new ArrayList(); // [VariableDeclaration]
		INode block; // the block in witch the variable is declared; needed for the LookupTable
		
		public TypeReference Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public Modifier Modifier {
			get {
				return modifier;
			}
			set {
				modifier = value;
			}
		}
		
		public ArrayList Variables {
			get {
				return variables;
			}
		}
		
		public INode Block {
			get {
				return block;
			}
			set {
				block = value;
			}
		}
		
		public LocalVariableDeclaration(TypeReference type)
		{
			this.type = type;
		}
		
		public LocalVariableDeclaration(TypeReference type, Modifier modifier)
		{
			this.type     = type;
			this.modifier = modifier;
		}
		
		public VariableDeclaration GetVariableDeclaration(string variableName)
		{
			foreach (VariableDeclaration variableDeclaration in variables) {
				if (variableDeclaration.Name == variableName) {
					return variableDeclaration;
				}
			}
			return null;
		}
				
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[LocalVariableDeclaration: Type={0}, Modifier ={1} Variables={2}]", 
			                     type, 
			                     modifier, 
			                     GetCollectionString(variables));
		}
	}
}
