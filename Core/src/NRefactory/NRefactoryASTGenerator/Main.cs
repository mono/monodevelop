// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1080 $</version>
// </file>

using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using NRefactoryASTGenerator.AST;

namespace NRefactoryASTGenerator
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			string directory = "../../../Project/Src/Parser/AST/";
			string visitorsDir = "../../../Project/Src/Parser/Visitors/";
			Debug.WriteLine("AST Generator running...");
			if (!File.Exists(directory + "INode.cs")) {
				Debug.WriteLine("did not find output directory");
				return;
			}
			if (!File.Exists(visitorsDir + "IASTVisitor.cs")) {
				Debug.WriteLine("did not find visitor output directory");
				return;
			}
			
			List<Type> nodeTypes = new List<Type>();
			foreach (Type type in typeof(MainClass).Assembly.GetTypes()) {
				if (type.IsClass && typeof(INode).IsAssignableFrom(type)) {
					nodeTypes.Add(type);
				}
			}
			nodeTypes.Sort(delegate(Type a, Type b) { return a.Name.CompareTo(b.Name); });
			
			CodeCompileUnit ccu = new CodeCompileUnit();
			CodeNamespace cns = new CodeNamespace("ICSharpCode.NRefactory.Parser.AST");
			ccu.Namespaces.Add(cns);
			cns.Imports.Add(new CodeNamespaceImport("System"));
			cns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			cns.Imports.Add(new CodeNamespaceImport("System.Diagnostics"));
			cns.Imports.Add(new CodeNamespaceImport("System.Drawing"));
			foreach (Type type in nodeTypes) {
				if (type.GetCustomAttributes(typeof(CustomImplementationAttribute), false).Length == 0) {
					CodeTypeDeclaration ctd = new CodeTypeDeclaration(type.Name);
					if (type.IsAbstract) {
						ctd.TypeAttributes |= TypeAttributes.Abstract;
					}
					ctd.BaseTypes.Add(new CodeTypeReference(type.BaseType.Name));
					cns.Types.Add(ctd);
					
					ProcessType(type, ctd);
					
					foreach (object o in type.GetCustomAttributes(false)) {
						if (o is TypeImplementationModifierAttribute) {
							(o as TypeImplementationModifierAttribute).ModifyImplementation(cns, ctd, type);
						}
					}
					
					if (!type.IsAbstract) {
						CodeMemberMethod method = new CodeMemberMethod();
						method.Name = "AcceptVisitor";
						method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
						method.Parameters.Add(new CodeParameterDeclarationExpression("IAstVisitor", "visitor"));
						method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "data"));
						method.ReturnType = new CodeTypeReference(typeof(object));
						CodeExpression ex = new CodeVariableReferenceExpression("visitor");
						ex = new CodeMethodInvokeExpression(ex, "Visit",
						                                    new CodeThisReferenceExpression(),
						                                    new CodeVariableReferenceExpression("data"));
						method.Statements.Add(new CodeMethodReturnStatement(ex));
						ctd.Members.Add(method);
						
						method = new CodeMemberMethod();
						method.Name = "ToString";
						method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
						method.ReturnType = new CodeTypeReference(typeof(string));
						method.Statements.Add(new CodeMethodReturnStatement(CreateToString(type)));
						ctd.Members.Add(method);
					}
				}
			}
			
			System.CodeDom.Compiler.CodeGeneratorOptions settings = new System.CodeDom.Compiler.CodeGeneratorOptions();
			settings.IndentString = "\t";
			settings.VerbatimOrder = true;
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(directory + "Generated.cs", writer.ToString());
			}
			
			ccu = new CodeCompileUnit();
			cns = new CodeNamespace("ICSharpCode.NRefactory.Parser");
			ccu.Namespaces.Add(cns);
			cns.Imports.Add(new CodeNamespaceImport("System"));
			cns.Imports.Add(new CodeNamespaceImport("ICSharpCode.NRefactory.Parser.AST"));
			cns.Types.Add(CreateAstVisitorInterface(nodeTypes));
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(visitorsDir + "IAstVisitor.cs", writer.ToString());
			}
			
			ccu = new CodeCompileUnit();
			cns = new CodeNamespace("ICSharpCode.NRefactory.Parser");
			ccu.Namespaces.Add(cns);
			cns.Imports.Add(new CodeNamespaceImport("System"));
			cns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			cns.Imports.Add(new CodeNamespaceImport("System.Diagnostics"));
			cns.Imports.Add(new CodeNamespaceImport("ICSharpCode.NRefactory.Parser.AST"));
			cns.Types.Add(CreateAstVisitorClass(nodeTypes, false));
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(visitorsDir + "AbstractAstVisitor.cs", writer.ToString());
			}
			
			ccu = new CodeCompileUnit();
			cns = new CodeNamespace("ICSharpCode.NRefactory.Parser");
			ccu.Namespaces.Add(cns);
			cns.Imports.Add(new CodeNamespaceImport("System"));
			cns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			cns.Imports.Add(new CodeNamespaceImport("System.Diagnostics"));
			cns.Imports.Add(new CodeNamespaceImport("ICSharpCode.NRefactory.Parser.AST"));
			cns.Types.Add(CreateAstVisitorClass(nodeTypes, true));
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(visitorsDir + "AbstractAstTransformer.cs", writer.ToString());
			}
		}
		
		static CodeTypeDeclaration CreateAstVisitorInterface(List<Type> nodeTypes)
		{
			CodeTypeDeclaration td = new CodeTypeDeclaration("IAstVisitor");
			td.IsInterface = true;
			
			foreach (Type t in nodeTypes) {
				if (!t.IsAbstract) {
					CodeMemberMethod m = new CodeMemberMethod();
					m.Name = "Visit";
					m.ReturnType = new CodeTypeReference(typeof(object));
					m.Parameters.Add(new CodeParameterDeclarationExpression(ConvertType(t), GetFieldName(t.Name)));
					m.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "data"));
					td.Members.Add(m);
				}
			}
			return td;
		}
		
		static CodeTypeDeclaration CreateAstVisitorClass(List<Type> nodeTypes, bool transformer)
		{
			CodeTypeDeclaration td = new CodeTypeDeclaration(transformer ? "AbstractAstTransformer" : "AbstractAstVisitor");
			td.TypeAttributes = TypeAttributes.Public | TypeAttributes.Abstract;
			td.BaseTypes.Add(new CodeTypeReference("IAstVisitor"));
			
			if (transformer) {
				string comment = "<summary>\n " +
					"The AbstractAstTransformer will iterate through the whole AST,\n " +
					"just like the AbstractAstVisitor. However, the AbstractAstTransformer allows\n " +
					"you to modify the AST at the same time: It does not use 'foreach' internally,\n " +
					"so you can add members to collections of parents of the current node (but\n " +
					"you cannot insert or delete items as that will make the index used invalid).\n " +
					"You can use the methods ReplaceCurrentNode and RemoveCurrentNode to replace\n " +
					"or remove the current node, totally independent from the type of the parent node.\n " +
					"</summary>";
				td.Comments.Add(new CodeCommentStatement(comment, true));
				
				CodeMemberField field = new CodeMemberField("Stack", "nodeStack");
				field.Type.TypeArguments.Add("INode");
				field.InitExpression = new CodeObjectCreateExpression(field.Type);
				td.Members.Add(field);
				
				CodeExpression nodeStack = new CodeVariableReferenceExpression("nodeStack");
				
				/*
				CodeMemberProperty p = new CodeMemberProperty();
				p.Name = "CurrentNode";
				p.Type = new CodeTypeReference("INode");
				p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				p.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("currentNode")));
				p.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("currentNode"),
				                                            new CodePropertySetValueReferenceExpression()));
				td.Members.Add(p);
				 */
				
				CodeMemberMethod m = new CodeMemberMethod();
				m.Name = "ReplaceCurrentNode";
				m.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				m.Parameters.Add(new CodeParameterDeclarationExpression("INode", "newNode"));
				m.Statements.Add(new CodeMethodInvokeExpression(nodeStack, "Pop"));
				m.Statements.Add(new CodeMethodInvokeExpression(nodeStack, "Push",
				                                                new CodeVariableReferenceExpression("newNode")));
				td.Members.Add(m);
				
				m = new CodeMemberMethod();
				m.Name = "RemoveCurrentNode";
				m.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				m.Statements.Add(new CodeMethodInvokeExpression(nodeStack, "Pop"));
				m.Statements.Add(new CodeMethodInvokeExpression(nodeStack, "Push",
				                                                new CodePrimitiveExpression(null)));
				td.Members.Add(m);
			}
			
			foreach (Type type in nodeTypes) {
				if (!type.IsAbstract) {
					CodeMemberMethod m = new CodeMemberMethod();
					m.Name = "Visit";
					m.Attributes = MemberAttributes.Public;
					m.ReturnType = new CodeTypeReference(typeof(object));
					m.Parameters.Add(new CodeParameterDeclarationExpression(ConvertType(type), GetFieldName(type.Name)));
					m.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "data"));
					td.Members.Add(m);
					
					List<CodeStatement> assertions = new List<CodeStatement>();
					CodeVariableReferenceExpression var = new CodeVariableReferenceExpression(GetFieldName(type.Name));
					assertions.Add(AssertIsNotNull(var));
					
					AddFieldVisitCode(m, type, var, assertions, transformer);
					
					if (type.GetCustomAttributes(typeof(HasChildrenAttribute), true).Length > 0) {
						if (transformer) {
							m.Statements.Add(new CodeSnippetStatement(CreateTransformerLoop(var.VariableName + ".Children", "INode")));
							m.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
						} else {
							CodeExpression ex = new CodeMethodInvokeExpression(var, "AcceptChildren",
							                                                   new CodeThisReferenceExpression(),
							                                                   new CodeVariableReferenceExpression("data"));
							m.Statements.Add(new CodeMethodReturnStatement(ex));
						}
					} else {
						CodeExpressionStatement lastStatement = null;
						if (m.Statements.Count > 0) {
							lastStatement = m.Statements[m.Statements.Count - 1] as CodeExpressionStatement;
						}
						if (lastStatement != null) {
							m.Statements.RemoveAt(m.Statements.Count - 1);
							m.Statements.Add(new CodeMethodReturnStatement(lastStatement.Expression));
						} else {
							m.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
						}
					}
					
					for (int i = 0; i < assertions.Count; i++) {
						m.Statements.Insert(i, assertions[i]);
					}
				}
			}
			return td;
		}
		
		static void AddFieldVisitCode(CodeMemberMethod m, Type type, CodeVariableReferenceExpression var, List<CodeStatement> assertions, bool transformer)
		{
			if (type != null) {
				if (type.BaseType != typeof(StatementWithEmbeddedStatement)) {
					AddFieldVisitCode(m, type.BaseType, var, assertions, transformer);
				}
				foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)) {
					AddVisitCode(m, field, var, assertions, transformer);
				}
				if (type.BaseType == typeof(StatementWithEmbeddedStatement)) {
					AddFieldVisitCode(m, type.BaseType, var, assertions, transformer);
				}
			}
		}
		
		static CodeStatement AssertIsNotNull(CodeExpression expr)
		{
			CodeExpression bop = new CodeBinaryOperatorExpression(expr,
			                                                      CodeBinaryOperatorType.IdentityInequality,
			                                                      new CodePrimitiveExpression(null)
			                                                     );
			return new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Debug"),
			                                                                  "Assert",
			                                                                  bop));
		}
		
		static string GetCode(CodeExpression ex)
		{
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromExpression(ex, writer, null);
				return writer.ToString();
			}
		}
		
		static string CreateTransformerLoop(string collection, string typeName)
		{
			return
				"\t\t\tfor (int i = 0; i < " + collection + ".Count; i++) {\n" +
				"\t\t\t\t" + typeName + " o = " + collection + "[i];\n" +
				"\t\t\t\tDebug.Assert(o != null);\n" +
				"\t\t\t\tnodeStack.Push(o);\n" +
				"\t\t\t\to.AcceptVisitor(this, data);\n" +
				(typeName == "INode"
				 ? "\t\t\t\to = nodeStack.Pop();\n"
				 : "\t\t\t\to = (" + typeName + ")nodeStack.Pop();\n") +
				"\t\t\t\tif (o == null)\n" +
				"\t\t\t\t\t" + collection + ".RemoveAt(i--);\n" +
				"\t\t\t\telse\n" +
				"\t\t\t\t\t" + collection + "[i] = o;\n" +
				"\t\t\t}";
		}
		
		static bool AddVisitCode(CodeMemberMethod m, FieldInfo field, CodeVariableReferenceExpression var, List<CodeStatement> assertions, bool transformer)
		{
			CodeExpression prop = new CodePropertyReferenceExpression(var, GetPropertyName(field.Name));
			CodeExpression nodeStack = new CodeVariableReferenceExpression("nodeStack");
			if (field.FieldType.FullName.StartsWith("System.Collections.Generic.List")) {
				Type elType = field.FieldType.GetGenericArguments()[0];
				if (!typeof(INode).IsAssignableFrom(elType))
					return false;
				assertions.Add(AssertIsNotNull(prop));
				string code;
				if (transformer) {
					code = CreateTransformerLoop(GetCode(prop), ConvertType(elType).BaseType);
				} else {
					code =
						"\t\t\tforeach (" + ConvertType(elType).BaseType + " o in " + GetCode(prop) + ") {\n" +
						"\t\t\t\tDebug.Assert(o != null);\n" +
						"\t\t\t\to.AcceptVisitor(this, data);\n" +
						"\t\t\t}";
				}
				m.Statements.Add(new CodeSnippetStatement(code));
				return true;
			}
			if (!typeof(INode).IsAssignableFrom(field.FieldType))
				return false;
			assertions.Add(AssertIsNotNull(prop));
			if (transformer) {
				m.Statements.Add(new CodeMethodInvokeExpression(nodeStack, "Push",
				                                                prop));
			}
			m.Statements.Add(new CodeMethodInvokeExpression(prop,
			                                                "AcceptVisitor",
			                                                new CodeThisReferenceExpression(),
			                                                new CodeVariableReferenceExpression("data")));
			if (transformer) {
				CodeExpression ex = new CodeMethodInvokeExpression(nodeStack, "Pop");
				ex = new CodeCastExpression(ConvertType(field.FieldType), ex);
				m.Statements.Add(new CodeAssignStatement(prop, ex));
			}
			return true;
		}
		
		static CodeExpression CreateToString(Type type)
		{
			CodeMethodInvokeExpression ie = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)),
			                                                               "Format");
			CodePrimitiveExpression prim = new CodePrimitiveExpression();
			ie.Parameters.Add(prim);
			string text = "[" + type.Name;
			int index = 0;
			do {
				foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)) {
					text += " " + GetPropertyName(field.Name) + "={" + index.ToString() + "}";
					index++;
					if (typeof(System.Collections.ICollection).IsAssignableFrom(field.FieldType)) {
						ie.Parameters.Add(new CodeSnippetExpression("GetCollectionString(" + GetPropertyName(field.Name) + ")"));
					} else {
						ie.Parameters.Add(new CodeVariableReferenceExpression(GetPropertyName(field.Name)));
					}
				}
				type = type.BaseType;
			} while (type != null);
			prim.Value = text + "]";
			if (ie.Parameters.Count == 1)
				return prim;
			else
				return ie;
			//	return String.Format("[AnonymousMethodExpression: Parameters={0} Body={1}]",
			//	                     GetCollectionString(Parameters),
			//	                     Body);
		}
		
		static void ProcessType(Type type, CodeTypeDeclaration ctd)
		{
			foreach (FieldInfo field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)) {
				CodeMemberField f = new CodeMemberField(ConvertType(field.FieldType), field.Name);
				f.Attributes = 0;
				ctd.Members.Add(f);
			}
			foreach (FieldInfo field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)) {
				CodeMemberProperty p = new CodeMemberProperty();
				p.Name = GetPropertyName(field.Name);
				p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				p.Type = ConvertType(field.FieldType);
				p.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(field.Name)));
				CodeExpression ex;
				if (field.FieldType.IsValueType)
					ex = new CodePropertySetValueReferenceExpression();
				else
					ex = GetDefaultValue("value", field);
				p.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(field.Name), ex));
				ctd.Members.Add(p);
			}
			foreach (ConstructorInfo ctor in type.GetConstructors()) {
				CodeConstructor c = new CodeConstructor();
				c.Attributes = MemberAttributes.Public;
				ctd.Members.Add(c);
				ConstructorInfo baseCtor = GetBaseCtor(type);
				foreach(ParameterInfo param in ctor.GetParameters()) {
					c.Parameters.Add(new CodeParameterDeclarationExpression(ConvertType(param.ParameterType),
					                                                        param.Name));
					if (baseCtor != null && Array.Exists(baseCtor.GetParameters(), delegate(ParameterInfo p) { return param.Name == p.Name; }))
						continue;
					c.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(GetPropertyName(param.Name)),
					                                         new CodeVariableReferenceExpression(param.Name)));
				}
				if (baseCtor != null) {
					foreach(ParameterInfo param in baseCtor.GetParameters()) {
						c.BaseConstructorArgs.Add(new CodeVariableReferenceExpression(param.Name));
					}
				}
				// initialize fields that were not initialized by parameter
				foreach (FieldInfo field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)) {
					if (field.FieldType.IsValueType && field.FieldType != typeof(Point))
						continue;
					if (Array.Exists(ctor.GetParameters(), delegate(ParameterInfo p) { return field.Name == p.Name; }))
						continue;
					c.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(field.Name),
					                                         GetDefaultValue(null, field)));
				}
			}
		}
		
		internal static ConstructorInfo GetBaseCtor(Type type)
		{
			ConstructorInfo[] list = type.BaseType.GetConstructors();
			if (list.Length == 0)
				return null;
			else
				return list[0];
		}
		
		internal static CodeExpression GetDefaultValue(string inputVariable, FieldInfo field)
		{
			string code;
			// get default value:
			if (field.FieldType == typeof(string)) {
				code = "\"\"";
				if (field.GetCustomAttributes(typeof(QuestionMarkDefaultAttribute), false).Length > 0) {
					if (inputVariable == null)
						return new CodePrimitiveExpression("?");
					else
						return new CodeSnippetExpression("string.IsNullOrEmpty(" + inputVariable + ") ? \"?\" : " + inputVariable);
				}
			} else if (field.FieldType.FullName.StartsWith("System.Collections.Generic.List")) {
				code = "new List<" + field.FieldType.GetGenericArguments()[0].Name + ">()";
			} else if (field.FieldType == typeof(Point)) {
				code = "new Point(-1, -1)";
			} else {
				code = field.FieldType.Name + ".Null";
			}
			if (inputVariable != null) {
				code = inputVariable + " ?? " + code;
			}
			return new CodeSnippetExpression(code);
		}
		
		internal static string GetFieldName(string typeName)
		{
			return char.ToLower(typeName[0]) + typeName.Substring(1);
		}
		
		internal static string GetPropertyName(string fieldName)
		{
			return char.ToUpper(fieldName[0]) + fieldName.Substring(1);
		}
		
		internal static CodeTypeReference ConvertType(Type type)
		{
			if (type.IsGenericType && !type.IsGenericTypeDefinition) {
				CodeTypeReference tr = ConvertType(type.GetGenericTypeDefinition());
				foreach (Type subType in type.GetGenericArguments()) {
					tr.TypeArguments.Add(ConvertType(subType));
				}
				return tr;
			} else if (type.FullName.StartsWith("NRefactory") || type.FullName.StartsWith("System.Collections")) {
				if (type.Name == "Attribute")
					return new CodeTypeReference("ICSharpCode.NRefactory.Parser.AST.Attribute");
				return new CodeTypeReference(type.Name);
			} else {
				return new CodeTypeReference(type);
			}
		}
	}
}
