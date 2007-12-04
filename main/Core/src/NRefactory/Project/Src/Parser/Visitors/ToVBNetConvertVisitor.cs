// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1018 $</version>
// </file>

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;
using Attribute = ICSharpCode.NRefactory.Parser.AST.Attribute;

namespace ICSharpCode.NRefactory.Parser
{
	/// <summary>
	/// Converts elements not supported by VB to their VB representation.
	/// Not all elements are converted here, most simple elements (e.g. ConditionalExpression)
	/// are converted in the output visitor.
	/// </summary>
	public class ToVBNetConvertVisitor : AbstractAstTransformer
	{
		// The following conversions are implemented:
		//   Conflicting field/property names -> m_field
		//   Anonymous methods are put into new methods
		//   Simple event handler creation is replaced with AddressOfExpression
		
		TypeDeclaration currentType;
		
		public override object Visit(TypeDeclaration td, object data)
		{
			TypeDeclaration outerType = currentType;
			currentType = td;
			
			//   Conflicting field/property names -> m_field
			List<string> properties = new List<string>();
			foreach (object o in td.Children) {
				PropertyDeclaration pd = o as PropertyDeclaration;
				if (pd != null) {
					properties.Add(pd.Name);
				}
			}
			List<VariableDeclaration> conflicts = new List<VariableDeclaration>();
			foreach (object o in td.Children) {
				FieldDeclaration fd = o as FieldDeclaration;
				if (fd != null) {
					foreach (VariableDeclaration var in fd.Fields) {
						string name = var.Name;
						foreach (string propertyName in properties) {
							if (name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)) {
								conflicts.Add(var);
							}
						}
					}
				}
			}
			new PrefixFieldsVisitor(conflicts, "m_").Run(td);
			base.Visit(td, data);
			currentType = outerType;
			
			return null;
		}
		
		string GetAnonymousMethodName()
		{
			for (int i = 1;; i++) {
				string name = "ConvertedAnonymousMethod" + i;
				bool ok = true;
				foreach (object c in currentType.Children) {
					MethodDeclaration method = c as MethodDeclaration;
					if (method != null && method.Name == name) {
						ok = false;
						break;
					}
				}
				if (ok)
					return name;
			}
		}
		
		public override object Visit(StatementExpression statementExpression, object data)
		{
			base.Visit(statementExpression, data);
			AssignmentExpression ass = statementExpression.Expression as AssignmentExpression;
			if (ass != null && ass.Right is AddressOfExpression) {
				if (ass.Op == AssignmentOperatorType.Add) {
					ReplaceCurrentNode(new AddHandlerStatement(ass.Left, ass.Right));
				} else if (ass.Op == AssignmentOperatorType.Subtract) {
					ReplaceCurrentNode(new RemoveHandlerStatement(ass.Left, ass.Right));
				}
			}
			return null;
		}
		
		string GetMemberNameOnThisReference(Expression expr)
		{
			IdentifierExpression ident = expr as IdentifierExpression;
			if (ident != null)
				return ident.Identifier;
			FieldReferenceExpression fre = expr as FieldReferenceExpression;
			if (fre != null && fre.TargetObject is ThisReferenceExpression)
				return fre.FieldName;
			return null;
		}
		
		string GetMethodNameOfDelegateCreation(Expression expr)
		{
			string name = GetMemberNameOnThisReference(expr);
			if (name != null)
				return name;
			ObjectCreateExpression oce = expr as ObjectCreateExpression;
			if (oce != null && oce.Parameters.Count == 1) {
				return GetMemberNameOnThisReference(oce.Parameters[0]);
			}
			return null;
		}
		
		public override object Visit(AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			MethodDeclaration method = new MethodDeclaration(GetAnonymousMethodName(), Modifier.Private, new TypeReference("System.Void"), anonymousMethodExpression.Parameters, null);
			method.Body = anonymousMethodExpression.Body;
			currentType.Children.Add(method);
			ReplaceCurrentNode(new AddressOfExpression(new IdentifierExpression(method.Name)));
			return null;
		}
		
		public override object Visit(AssignmentExpression assignmentExpression, object data)
		{
			if (assignmentExpression.Op == AssignmentOperatorType.Add
			    || assignmentExpression.Op == AssignmentOperatorType.Subtract)
			{
				string methodName = GetMethodNameOfDelegateCreation(assignmentExpression.Right);
				if (methodName != null) {
					foreach (object c in currentType.Children) {
						MethodDeclaration method = c as MethodDeclaration;
						if (method != null && method.Name == methodName) {
							// this statement is registering an event
							assignmentExpression.Right = new AddressOfExpression(new IdentifierExpression(methodName));
							break;
						}
					}
				}
			}
			return base.Visit(assignmentExpression, data);
		}
		
		public override object Visit(MethodDeclaration methodDeclaration, object data)
		{
			if ((methodDeclaration.Modifier & Modifier.Visibility) == 0)
				methodDeclaration.Modifier |= Modifier.Private;
			
			base.Visit(methodDeclaration, data);
			
			const Modifier externStatic = Modifier.Static | Modifier.Extern;
			if ((methodDeclaration.Modifier & externStatic) == externStatic
			    && methodDeclaration.Body.IsNull)
			{
				foreach (AttributeSection sec in methodDeclaration.Attributes) {
					foreach (Attribute att in sec.Attributes) {
						if ("DllImport".Equals(att.Name, StringComparison.InvariantCultureIgnoreCase)) {
							if (ConvertPInvoke(methodDeclaration, att)) {
								sec.Attributes.Remove(att);
								break;
							}
						}
					}
					if (sec.Attributes.Count == 0) {
						methodDeclaration.Attributes.Remove(sec);
						break;
					}
				}
			}
			return null;
		}
		
		bool ConvertPInvoke(MethodDeclaration method, ICSharpCode.NRefactory.Parser.AST.Attribute att)
		{
			if (att.PositionalArguments.Count != 1)
				return false;
			PrimitiveExpression pe = att.PositionalArguments[0] as PrimitiveExpression;
			if (pe == null || !(pe.Value is string))
				return false;
			string libraryName = (string)pe.Value;
			string alias = null;
			bool setLastError = false;
			bool exactSpelling = false;
			CharsetModifier charSet = CharsetModifier.Auto;
			foreach (NamedArgumentExpression arg in att.NamedArguments) {
				switch (arg.Name) {
					case "SetLastError":
						pe = arg.Expression as PrimitiveExpression;
						if (pe != null && pe.Value is bool)
							setLastError = (bool)pe.Value;
						else
							return false;
						break;
					case "ExactSpelling":
						pe = arg.Expression as PrimitiveExpression;
						if (pe != null && pe.Value is bool)
							exactSpelling = (bool)pe.Value;
						else
							return false;
						break;
					case "CharSet":
						{
							FieldReferenceExpression fre = arg.Expression as FieldReferenceExpression;
							if (fre == null || !(fre.TargetObject is IdentifierExpression))
								return false;
							if ((fre.TargetObject as IdentifierExpression).Identifier != "CharSet")
								return false;
							switch (fre.FieldName) {
								case "Unicode":
									charSet = CharsetModifier.Unicode;
									break;
								case "Auto":
									charSet = CharsetModifier.Auto;
									break;
								case "Ansi":
									charSet = CharsetModifier.ANSI;
									break;
								default:
									return false;
							}
						}
						break;
					case "EntryPoint":
						pe = arg.Expression as PrimitiveExpression;
						if (pe != null)
							alias = pe.Value as string;
						break;
					default:
						return false;
				}
			}
			if (setLastError && exactSpelling) {
				// Only P/Invokes with SetLastError and ExactSpelling can be converted to a DeclareDeclaration
				const Modifier removeModifiers = Modifier.Static | Modifier.Extern;
				DeclareDeclaration decl = new DeclareDeclaration(method.Name, method.Modifier &~ removeModifiers,
				                                                 method.TypeReference,
				                                                 method.Parameters,
				                                                 method.Attributes,
				                                                 libraryName, alias, charSet);
				ReplaceCurrentNode(decl);
				base.Visit(decl, null);
				return true;
			} else {
				return false;
			}
		}
		
		public override object Visit(PropertyDeclaration propertyDeclaration, object data)
		{
			if ((propertyDeclaration.Modifier & Modifier.Visibility) == 0)
				propertyDeclaration.Modifier |= Modifier.Private;
			return base.Visit(propertyDeclaration, data);
		}
		
		public override object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			if ((constructorDeclaration.Modifier & Modifier.Visibility) == 0)
				constructorDeclaration.Modifier |= Modifier.Private;
			return base.Visit(constructorDeclaration, data);
		}
	}
}
