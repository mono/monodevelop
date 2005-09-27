using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class LocalVariableDeclaration : Statement
	{
		Modifier      modifier = Modifier.None;
		ArrayList     variables = new ArrayList(); // [VariableDeclaration]
		INode block;
		
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
			} set {
				variables = value;
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
		
		public LocalVariableDeclaration(Modifier modifier)
		{
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
			return String.Format("[LocalVariableDeclaration: Modifier ={0} Variables={1}]", 
			                     modifier, 
			                     GetCollectionString(variables));
		}
	}
}
