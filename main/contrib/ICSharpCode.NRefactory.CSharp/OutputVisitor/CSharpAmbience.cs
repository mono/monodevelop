// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// C# ambience.
	/// </summary>
	public class CSharpAmbience : IAmbience
	{
		public ConversionFlags ConversionFlags { get; set; }
		
		#region ConvertEntity
		public string ConvertEntity(IEntity e)
		{
			StringWriter writer = new StringWriter();
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			AttributedNode node = astBuilder.ConvertEntity(e);
			PrintModifiers(node.Modifiers, writer);
			
			if ((ConversionFlags & ConversionFlags.ShowDefinitionKeyWord) == ConversionFlags.ShowDefinitionKeyWord) {
				if (node is TypeDeclaration) {
					switch (((TypeDeclaration)node).ClassType) {
						case ClassType.Class:
							writer.Write("class");
							break;
						case ClassType.Struct:
							writer.Write("struct");
							break;
						case ClassType.Interface:
							writer.Write("interface");
							break;
						case ClassType.Enum:
							writer.Write("enum");
							break;
						default:
							throw new Exception("Invalid value for ClassType");
					}
					writer.Write(' ');
				} else if (node is DelegateDeclaration) {
					writer.Write("delegate ");
				}
			}
			
			if ((ConversionFlags & ConversionFlags.ShowReturnType) == ConversionFlags.ShowReturnType) {
				var rt = node.GetChildByRole(AstNode.Roles.Type);
				if (!rt.IsNull) {
					rt.AcceptVisitor(CreatePrinter(writer));
					writer.Write(' ');
				}
			}
			
			if (e is ITypeDefinition)
				WriteTypeDeclarationName((ITypeDefinition)e, writer);
			else
				WriteMemberDeclarationName((IMember)e, writer);
			
			if ((ConversionFlags & ConversionFlags.ShowParameterList) == ConversionFlags.ShowParameterList && HasParameters(e)) {
				writer.Write(e.EntityType == EntityType.Indexer ? '[' : '(');
				bool first = true;
				foreach (var param in node.GetChildrenByRole(AstNode.Roles.Parameter)) {
					if (first)
						first = false;
					else
						writer.Write(", ");
					param.AcceptVisitor(CreatePrinter(writer));
				}
				writer.Write(e.EntityType == EntityType.Indexer ? ']' : ')');
			}
			
			if ((ConversionFlags & ConversionFlags.ShowBody) == ConversionFlags.ShowBody && !(node is TypeDeclaration)) {
				IProperty property = e as IProperty;
				if (property != null) {
					writer.Write(" { ");
					if (property.CanGet)
						writer.Write("get; ");
					if (property.CanSet)
						writer.Write("set; ");
					writer.Write('}');
				} else {
					writer.Write(';');
				}
			}
			
			return writer.ToString().TrimEnd();
		}
		
		bool HasParameters(IEntity e)
		{
			switch (e.EntityType) {
				case EntityType.TypeDefinition:
					return ((ITypeDefinition)e).Kind == TypeKind.Delegate;
				case EntityType.Indexer:
				case EntityType.Method:
				case EntityType.Operator:
				case EntityType.Constructor:
				case EntityType.Destructor:
					return true;
				default:
					return false;
			}
		}
		
		TypeSystemAstBuilder CreateAstBuilder()
		{
			TypeSystemAstBuilder astBuilder = new TypeSystemAstBuilder();
			astBuilder.ShowModifiers = (ConversionFlags & ConversionFlags.ShowModifiers) == ConversionFlags.ShowModifiers;
			astBuilder.ShowAccessibility = (ConversionFlags & ConversionFlags.ShowAccessibility) == ConversionFlags.ShowAccessibility;
			astBuilder.AlwaysUseShortTypeNames = (ConversionFlags & ConversionFlags.UseFullyQualifiedTypeNames) != ConversionFlags.UseFullyQualifiedTypeNames;
			astBuilder.ShowParameterNames = (ConversionFlags & ConversionFlags.ShowParameterNames) == ConversionFlags.ShowParameterNames;
			return astBuilder;
		}
		
		void ConvertTypeDeclaration(ITypeDefinition typeDef, StringWriter writer)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			var declaration = astBuilder.ConvertEntity(typeDef);
			TypeDeclaration typeDeclaration = (TypeDeclaration)declaration;
			PrintModifiers(typeDeclaration.Modifiers, writer);
			if ((ConversionFlags & ConversionFlags.ShowDefinitionKeyWord) == ConversionFlags.ShowDefinitionKeyWord) {
				switch (typeDeclaration.ClassType) {
					case ClassType.Class:
						writer.Write("class");
						break;
					case ClassType.Struct:
						writer.Write("struct");
						break;
					case ClassType.Interface:
						writer.Write("interface");
						break;
					case ClassType.Enum:
						writer.Write("enum");
						break;
					default:
						throw new Exception("Invalid value for ClassType");
				}
				writer.Write(' ');
			}
			WriteTypeDeclarationName(typeDef, writer);
		}

		void WriteTypeDeclarationName(ITypeDefinition typeDef, StringWriter writer)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			if (typeDef.DeclaringTypeDefinition != null) {
				WriteTypeDeclarationName(typeDef.DeclaringTypeDefinition, writer);
				writer.Write('.');
			} else if ((ConversionFlags & ConversionFlags.UseFullyQualifiedMemberNames) == ConversionFlags.UseFullyQualifiedMemberNames) {
				writer.Write(typeDef.Namespace);
				writer.Write('.');
			}
			writer.Write(typeDef.Name);
			if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList) {
				CreatePrinter(writer).WriteTypeParameters(astBuilder.ConvertEntity(typeDef).GetChildrenByRole(AstNode.Roles.TypeParameter));
			}
		}
		
		void WriteMemberDeclarationName(IMember member, StringWriter writer)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			if ((ConversionFlags & ConversionFlags.UseFullyQualifiedMemberNames) == ConversionFlags.UseFullyQualifiedMemberNames) {
				writer.Write(ConvertType(member.DeclaringType));
				writer.Write('.');
			}
			switch (member.EntityType) {
				case EntityType.Indexer:
					writer.Write("this");
					break;
				case EntityType.Constructor:
					writer.Write(member.DeclaringType.Name);
					break;
				case EntityType.Destructor:
					writer.Write('~');
					writer.Write(member.DeclaringType.Name);
					break;
				case EntityType.Operator:
					switch (member.Name) {
						case "op_Implicit":
							writer.Write("implicit operator ");
							writer.Write(ConvertType(member.ReturnType));
							break;
						case "op_Explicit":
							writer.Write("explicit operator ");
							writer.Write(ConvertType(member.ReturnType));
							break;
						default:
							writer.Write("operator ");
							var operatorType = OperatorDeclaration.GetOperatorType(member.Name);
							if (operatorType.HasValue)
								writer.Write(OperatorDeclaration.GetToken(operatorType.Value));
							else
								writer.Write(member.Name);
							break;
					}
					break;
				default:
					writer.Write(member.Name);
					break;
			}
			if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList && member.EntityType == EntityType.Method) {
				CreatePrinter(writer).WriteTypeParameters(astBuilder.ConvertEntity(member).GetChildrenByRole(AstNode.Roles.TypeParameter));
			}
		}
		
		CSharpOutputVisitor CreatePrinter(StringWriter writer)
		{
			return new CSharpOutputVisitor(writer, new CSharpFormattingOptions());
		}
		
		void PrintModifiers(Modifiers modifiers, StringWriter writer)
		{
			foreach (var m in CSharpModifierToken.AllModifiers) {
				if ((modifiers & m) == m) {
					writer.Write(CSharpModifierToken.GetModifierName(m));
					writer.Write(' ');
				}
			}
		}
		#endregion
		
		public string ConvertVariable(IVariable v)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			AstNode astNode = astBuilder.ConvertVariable(v);
			return astNode.GetText().TrimEnd(';', '\r', '\n');
		}
		
		public string ConvertType(IType type)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			AstType astType = astBuilder.ConvertType(type);
			return astType.GetText();
		}
		
		public string WrapAttribute(string attribute)
		{
			return "[" + attribute + "]";
		}
		
		public string WrapComment(string comment)
		{
			return "// " + comment;
		}
	}
}
