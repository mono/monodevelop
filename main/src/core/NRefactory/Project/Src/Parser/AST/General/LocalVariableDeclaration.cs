// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 975 $</version>
// </file>

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Parser.AST
{
	public class LocalVariableDeclaration : Statement
	{
		TypeReference             typeReference;
		Modifier                  modifier = Modifier.None;
		List<VariableDeclaration> variables = new List<VariableDeclaration>(1);
		
		public TypeReference TypeReference {
			get {
				return typeReference;
			}
			set {
				typeReference = TypeReference.CheckNull(value);
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
		
		public List<VariableDeclaration> Variables {
			get {
				return variables;
			}
		}
		
		public TypeReference GetTypeForVariable(int variableIndex)
		{
			if (!typeReference.IsNull) {
				return typeReference;
			}
			
			for (int i = variableIndex; i < Variables.Count;++i) {
				if (!((VariableDeclaration)Variables[i]).TypeReference.IsNull) {
					return ((VariableDeclaration)Variables[i]).TypeReference;
				}
			}
			return null;
		}
		
		public LocalVariableDeclaration(VariableDeclaration declaration) : this(TypeReference.Null)
		{
			Variables.Add(declaration);
		}
		
		public LocalVariableDeclaration(TypeReference typeReference)
		{
			this.TypeReference = typeReference;
		}
		
		public LocalVariableDeclaration(TypeReference typeReference, Modifier modifier)
		{
			this.TypeReference = typeReference;
			this.modifier      = modifier;
		}
		
		public LocalVariableDeclaration(Modifier modifier)
		{
			this.typeReference = TypeReference.Null;
			this.modifier      = modifier;
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
				
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[LocalVariableDeclaration: Type={0}, Modifier ={1} Variables={2}]", 
			                     typeReference, 
			                     modifier, 
			                     GetCollectionString(variables));
		}
	}
}
