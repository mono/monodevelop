
using System;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Collections;

namespace Stetic
{
	internal static class CodeGeneratorInternalClass
	{
		static CodeExpression bindingFlags;
		
		static CodeGeneratorInternalClass ()
		{
			CodeTypeReferenceExpression flagsType = new CodeTypeReferenceExpression ("System.Reflection.BindingFlags");
			bindingFlags = new CodeBinaryOperatorExpression (
				new CodeFieldReferenceExpression (flagsType, "Public"),
				CodeBinaryOperatorType.BitwiseOr,
				new CodeFieldReferenceExpression (flagsType, "NonPublic")
			);
			
			bindingFlags = new CodeBinaryOperatorExpression (
				bindingFlags,
				CodeBinaryOperatorType.BitwiseOr,
				new CodeFieldReferenceExpression (flagsType, "Instance")
			);		
		}
		
		public static void GenerateProjectGuiCode (SteticCompilationUnit globalUnit, CodeNamespace globalNs, CodeTypeDeclaration globalType, GenerationOptions options, List<SteticCompilationUnit> units, ProjectBackend[] projects, ArrayList warnings)
		{
			bool multiProject = projects.Length > 1;
			
			// Build method overload that takes a type as parameter.
			
			CodeMemberMethod met = new CodeMemberMethod ();
			met.Name = "Build";
			globalType.Members.Add (met);
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(object), "cobj"));
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(Type), "type"));
			if (multiProject)
				met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(string), "file"));
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			
			CodeMethodInvokeExpression call = new CodeMethodInvokeExpression (
					new CodeMethodReferenceExpression (
						new CodeTypeReferenceExpression (globalNs.Name + ".Gui"),
						"Build"
					),
					new CodeArgumentReferenceExpression ("cobj"),
					new CodePropertyReferenceExpression (
						new CodeArgumentReferenceExpression ("type"),
						"FullName"
					)
			);
			if (multiProject)
				call.Parameters.Add (new CodeArgumentReferenceExpression ("file"));

			met.Statements.Add (call);
			
			// Generate the build method
			
			met = new CodeMemberMethod ();
			met.Name = "Build";
			globalType.Members.Add (met);
			
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(object), "cobj"));
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(string), "id"));
			if (multiProject)
				met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(string), "file"));
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Attributes = MemberAttributes.Public | MemberAttributes.Static;

			if (options.GenerateEmptyBuildMethod)
				return;
			
			CodeArgumentReferenceExpression cobj = new CodeArgumentReferenceExpression ("cobj");
			CodeArgumentReferenceExpression cfile = new CodeArgumentReferenceExpression ("file");
			CodeArgumentReferenceExpression cid = new CodeArgumentReferenceExpression ("id");
			
			CodeStatementCollection projectCol = met.Statements;
			
			CodeConditionStatement tcond = new CodeConditionStatement ();
			tcond.Condition = new CodeMethodInvokeExpression (new CodeTypeOfExpression (typeof(Gtk.Widget)), "IsAssignableFrom", cobj);
			
			tcond.TrueStatements.Add (
					new CodeMethodInvokeExpression (
						new CodeTypeReferenceExpression (globalNs.Name + ".Gui"),
						"Initialize",
			            cobj
					)
			);
					
			// Generate code for each project
			
			foreach (ProjectBackend gp in projects) {
				
				CodeStatementCollection widgetCol;
				
				if (multiProject) {
					CodeConditionStatement pcond = new CodeConditionStatement ();
					pcond.Condition = new CodeBinaryOperatorExpression (
						cfile, 
						CodeBinaryOperatorType.IdentityEquality,
						new CodePrimitiveExpression (gp.Id)
					);
					projectCol.Add (pcond);
					
					widgetCol = pcond.TrueStatements;
					projectCol = pcond.FalseStatements;
				} else {
					widgetCol = projectCol;
				}
				
				// Generate top levels
				
				CodeIdentifiers ids = new CodeIdentifiers ();
				
				foreach (Gtk.Widget w in gp.Toplevels) {
					CodeConditionStatement cond = new CodeConditionStatement ();
					cond.Condition = new CodeBinaryOperatorExpression (
						cid, 
						CodeBinaryOperatorType.IdentityEquality,
						new CodePrimitiveExpression (w.Name)
					);
					widgetCol.Add (cond);
					
					GenerateComponentCode (w, globalUnit, globalNs, cobj, cond.TrueStatements, globalType, options, units, ids, warnings);
					
					widgetCol = cond.FalseStatements;
				}
				
				// Generate action groups
				
				foreach (Wrapper.ActionGroup agroup in gp.ActionGroups) {
					CodeConditionStatement cond = new CodeConditionStatement ();
					cond.Condition = new CodeBinaryOperatorExpression (
						cid, 
						CodeBinaryOperatorType.IdentityEquality,
						new CodePrimitiveExpression (agroup.Name)
					);
					widgetCol.Add (cond);
					
					GenerateComponentCode (agroup, globalUnit, globalNs, cobj, cond.TrueStatements, globalType, options, units, ids, warnings);
					
					widgetCol = cond.FalseStatements;
				}
			}
		}
		
		static CodeMemberMethod GetBuildMethod (string name, string internalClassName, string typeName, SteticCompilationUnit globalUnit, GenerationOptions options, List<SteticCompilationUnit> units)
		{
			SteticCompilationUnit unit;
			
			if (options.GenerateSingleFile)
				unit = globalUnit;
			else {
				unit = new SteticCompilationUnit (name);
				units.Add (unit);
			}
			
			CodeTypeDeclaration type = new CodeTypeDeclaration (internalClassName);
			type.Attributes = MemberAttributes.Private;
			type.TypeAttributes = TypeAttributes.NestedAssembly;
			
			CodeNamespace cns = new CodeNamespace (options.GlobalNamespace + ".SteticGenerated");
			cns.Types.Add (type);
			unit.Namespaces.Add (cns);
					
			// Create the build method for the component
			
			CodeMemberMethod met = new CodeMemberMethod ();
			met.Name = "Build";
			type.Members.Add (met);
			
			met.Parameters.Add (new CodeParameterDeclarationExpression (new CodeTypeReference (typeName), "cobj"));
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			
			return met;
		}
		
		static void GenerateComponentCode (object component, SteticCompilationUnit globalUnit, CodeNamespace globalNs, CodeExpression cobj, CodeStatementCollection statements, CodeTypeDeclaration globalType, GenerationOptions options, List<SteticCompilationUnit> units, CodeIdentifiers ids, ArrayList warnings)
		{
			Gtk.Widget widget = component as Gtk.Widget;
			Wrapper.Widget wwidget = Stetic.Wrapper.Widget.Lookup (widget);
			Wrapper.ActionGroup agroup = component as Wrapper.ActionGroup;
			
			string name = widget != null ? widget.Name : agroup.Name;
			string internalClassName = ids.MakeUnique (CodeIdentifier.MakeValid (name));
			
			string typeName = widget != null ? wwidget.WrappedTypeName : "Gtk.ActionGroup";
			// Create the build method for the top level
			
			CodeMemberMethod met;
			met = GetBuildMethod (name, internalClassName, typeName, globalUnit, options, units);
			
			// Generate the build code
			
			CodeVariableDeclarationStatement varDecHash = new CodeVariableDeclarationStatement (typeof(System.Collections.Hashtable), "bindings");
			met.Statements.Add (varDecHash);
			varDecHash.InitExpression = new CodeObjectCreateExpression (
				typeof(System.Collections.Hashtable),
				new CodeExpression [0]
			);
			
			CodeVariableReferenceExpression targetObjectVar = new CodeVariableReferenceExpression ("cobj");
			Stetic.WidgetMap map;
			
			if (widget != null) {
				map = Stetic.CodeGenerator.GenerateCreationCode (globalNs, globalType, widget, targetObjectVar, met.Statements, options, warnings);
				CodeGenerator.BindSignalHandlers (targetObjectVar, wwidget, map, met.Statements, options);
			} else {
				map = Stetic.CodeGenerator.GenerateCreationCode (globalNs, globalType, agroup, targetObjectVar, met.Statements, options, warnings);
				foreach (Wrapper.Action ac in agroup.Actions)
					CodeGenerator.BindSignalHandlers (targetObjectVar, ac, map, met.Statements, options);
			}
			
			GenerateBindFieldCode (met.Statements, cobj);
			
			// Add a method call to the build method
			
			statements.Add (
				new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression (options.GlobalNamespace + ".SteticGenerated." + internalClassName),
					"Build",
					new CodeCastExpression (typeName, cobj)
				)
			);
		}
		
		static void GenerateBindFieldCode (CodeStatementCollection statements, CodeExpression cobj)
		{
			// Bind the fields
			
			CodeVariableDeclarationStatement varDecIndex = new CodeVariableDeclarationStatement (typeof(int), "n");
			varDecIndex.InitExpression = new CodePrimitiveExpression (0);
			CodeExpression varIndex = new CodeVariableReferenceExpression ("n");
			
			CodeVariableDeclarationStatement varDecArray = new CodeVariableDeclarationStatement (typeof(FieldInfo[]), "fields");
			varDecArray.InitExpression = new CodeMethodInvokeExpression (
				new CodeMethodInvokeExpression (
					cobj,
					"GetType",
					new CodeExpression [0]
				),
				"GetFields",
				bindingFlags
			);
			statements.Add (varDecArray);
			CodeVariableReferenceExpression varArray = new CodeVariableReferenceExpression ("fields");
			
			CodeIterationStatement iteration = new CodeIterationStatement ();
			statements.Add (iteration);
			
			iteration.InitStatement = varDecIndex;
			
			iteration.TestExpression = new CodeBinaryOperatorExpression (
				varIndex,
				CodeBinaryOperatorType.LessThan,
				new CodePropertyReferenceExpression (varArray, "Length")
			);
			iteration.IncrementStatement = new CodeAssignStatement (
				varIndex,
				new CodeBinaryOperatorExpression (
					varIndex,
					CodeBinaryOperatorType.Add,
					new CodePrimitiveExpression (1)
				)
			);
			
			CodeVariableDeclarationStatement varDecField = new CodeVariableDeclarationStatement (typeof(FieldInfo), "field");
			varDecField.InitExpression = new CodeArrayIndexerExpression (varArray, new CodeExpression [] {varIndex});
			CodeVariableReferenceExpression varField = new CodeVariableReferenceExpression ("field");
			iteration.Statements.Add (varDecField);
			
			CodeVariableDeclarationStatement varDecWidget = new CodeVariableDeclarationStatement (typeof(object), "widget");
			iteration.Statements.Add (varDecWidget);
			varDecWidget.InitExpression = new CodeIndexerExpression (
				new CodeVariableReferenceExpression ("bindings"),
				new CodePropertyReferenceExpression (varField, "Name")
			);
			CodeVariableReferenceExpression varWidget = new CodeVariableReferenceExpression ("widget");
			
			// Make sure the type of the field matches the type of the widget
			
			CodeConditionStatement fcond = new CodeConditionStatement ();
			iteration.Statements.Add (fcond);
			fcond.Condition = new CodeBinaryOperatorExpression (
				new CodeBinaryOperatorExpression (
					varWidget,
					CodeBinaryOperatorType.IdentityInequality,
					new CodePrimitiveExpression (null)
				),
				CodeBinaryOperatorType.BooleanAnd,
				new CodeMethodInvokeExpression (
					new CodePropertyReferenceExpression (varField, "FieldType"),
					"IsInstanceOfType",
					varWidget
				)
			);
			
			// Set the variable value
			
			fcond.TrueStatements.Add (
				new CodeMethodInvokeExpression (
					varField,
					"SetValue",
					cobj,
					varWidget
				)
			);
		}
	}
}
