using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Stetic
{
	internal static class CodeGenerator
	{
		public static void GenerateProjectCode (string file, CodeDomProvider provider, GenerationOptions options, ProjectBackend[] projects)
		{
			CodeGenerationResult res = GenerateProjectCode (options, projects);
			
			string basePath = Path.GetDirectoryName (file);
			
			foreach (SteticCompilationUnit unit in res.Units) {
				string fname;
				if (unit.Name.Length == 0)
					fname = file;
				else
					fname = Path.Combine (basePath, unit.Name);
				StreamWriter fileStream = new StreamWriter (fname);
				try {
					provider.GenerateCodeFromCompileUnit (unit, fileStream, new CodeGeneratorOptions ());
				} finally {
					fileStream.Close ();
				}
			}
		}

		public static CodeGenerationResult GenerateWidgetCode (GenerationOptions options, ObjectWrapper wrapper)
		{
			ArrayList warningList = new ArrayList ();

			List<SteticCompilationUnit> units = new List<SteticCompilationUnit> ();
			SteticCompilationUnit globalUnit = new SteticCompilationUnit ("");
			//units.Add (globalUnit);

			if (options == null) {
				options = new GenerationOptions ();
			}
			CodeNamespace globalNS = new CodeNamespace (options.GlobalNamespace);
			globalUnit.Namespaces.Add (globalNS);

			var widget = wrapper.Wrapped as Gtk.Widget;
			if (widget == null) {
				throw new Exception ("Not a gtk widget");
			}
			CodeGeneratorPartialClass.GenerateWidgetCode (globalUnit, globalNS, options, units, widget, warningList, true);
			return new CodeGenerationResult (units.ToArray (), (string[]) warningList.ToArray (typeof (string)));
		}

		public static CodeGenerationResult GenerateProjectCode (GenerationOptions options, ProjectBackend[] projects)
		{
			ArrayList warningList = new ArrayList ();
			
			List<SteticCompilationUnit> units = new List<SteticCompilationUnit> ();
			SteticCompilationUnit globalUnit = new SteticCompilationUnit ("");
			units.Add (globalUnit);
			
			if (options == null)
				options = new GenerationOptions ();
			CodeNamespace globalNs = new CodeNamespace (options.GlobalNamespace);
			globalUnit.Namespaces.Add (globalNs);
			
			// Global class
			
			CodeTypeDeclaration globalType = new CodeTypeDeclaration ("Gui");
			globalType.Attributes = MemberAttributes.Private;
			globalType.TypeAttributes = TypeAttributes.NestedAssembly;
			globalNs.Types.Add (globalType);
			
			// Create the project initialization method
			// This method will only be added at the end if there
			// is actually something to initialize
			
			CodeMemberMethod initMethod = new CodeMemberMethod ();
			initMethod.Name = "Initialize";
			initMethod.ReturnType = new CodeTypeReference (typeof(void));
			initMethod.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
			initMethod.Parameters.Add (new CodeParameterDeclarationExpression (typeof(Gtk.Widget), "iconRenderer"));
			
			GeneratorContext initContext = new ProjectGeneratorContext (globalNs, globalType, initMethod.Statements, options);
			initContext.RootObject = new CodeArgumentReferenceExpression ("iconRenderer");
			
			// Generate icon factory creation

			foreach (ProjectBackend gp in projects) {
				if (gp.IconFactory.Icons.Count > 0)
					gp.IconFactory.GenerateBuildCode (initContext);
			}
			warningList.AddRange (initContext.Warnings);
					
			// Generate the code
			
			if (options.UsePartialClasses)
				CodeGeneratorPartialClass.GenerateProjectGuiCode (globalUnit, globalNs, globalType, options, units, projects, warningList);
			else
				CodeGeneratorInternalClass.GenerateProjectGuiCode (globalUnit, globalNs, globalType, options, units, projects, warningList);

			GenerateProjectActionsCode (globalNs, options, projects);
			
			// Final step. If there is some initialization code, add all needed infrastructure
			
			globalType.Members.Add (initMethod);
			
			CodeMemberField initField = new CodeMemberField (typeof(bool), "initialized");
			initField.Attributes = MemberAttributes.Private | MemberAttributes.Static;
			globalType.Members.Add (initField);
			
			CodeFieldReferenceExpression initVar = new CodeFieldReferenceExpression (
				new CodeTypeReferenceExpression (globalNs.Name + ".Gui"),
				"initialized"
			);
			
			CodeConditionStatement initCondition = new CodeConditionStatement ();
			initCondition.Condition = new CodeBinaryOperatorExpression (
				initVar, 
				CodeBinaryOperatorType.IdentityEquality,
				new CodePrimitiveExpression (false)
			);
			initCondition.TrueStatements.Add (new CodeAssignStatement (
				initVar,
				new CodePrimitiveExpression (true)
			));
			initCondition.TrueStatements.AddRange (initMethod.Statements);
			initMethod.Statements.Clear ();
			initMethod.Statements.Add (initCondition);
			
			return new CodeGenerationResult (units.ToArray (), (string[]) warningList.ToArray (typeof(string)));
		}
		
		internal static void BindSignalHandlers (CodeExpression targetObjectVar, ObjectWrapper wrapper, Stetic.WidgetMap map, CodeStatementCollection statements, GenerationOptions options)
		{
			foreach (Signal signal in wrapper.Signals) {
				SignalDescriptor descriptor = signal.SignalDescriptor;
				
				CodeExpression createDelegate;
				
				if (options.UsePartialClasses) {
					var rgx = new Regex (@"`\d+");
					createDelegate =
						new CodeDelegateCreateExpression (
							new CodeTypeReference (rgx.Replace (descriptor.HandlerTypeName, ""), CodeTypeReferenceOptions.GlobalReference),
							new CodeThisReferenceExpression (),
							signal.Handler);
				} else {
					createDelegate =
						new CodeMethodInvokeExpression (
							new CodeTypeReferenceExpression (new CodeTypeReference (typeof(Delegate), CodeTypeReferenceOptions.GlobalReference)),
							"CreateDelegate",
							new CodeTypeOfExpression (descriptor.HandlerTypeName),
							targetObjectVar,
							new CodePrimitiveExpression (signal.Handler));
					
					createDelegate = new CodeCastExpression (descriptor.HandlerTypeName.ToGlobalTypeRef (), createDelegate);
				}
				
				CodeAttachEventStatement cevent = new CodeAttachEventStatement (
					new CodeEventReferenceExpression (
						map.GetWidgetExp (wrapper),
						descriptor.Name),
					createDelegate);
				
				statements.Add (cevent);
			}
			
			Wrapper.Widget widget = wrapper as Wrapper.Widget;
			if (widget != null && widget.IsTopLevel) {
				// Bind local action signals
				foreach (Wrapper.ActionGroup grp in widget.LocalActionGroups) {
					foreach (Wrapper.Action ac in grp.Actions)
						BindSignalHandlers (targetObjectVar, ac, map, statements, options);
				}
			}
			
			Gtk.Container cont = wrapper.Wrapped as Gtk.Container;
			if (cont != null) {
				foreach (Gtk.Widget child in cont.AllChildren) {
					Stetic.Wrapper.Widget ww = Stetic.Wrapper.Widget.Lookup (child);
					if (ww != null)
						BindSignalHandlers (targetObjectVar, ww, map, statements, options);
				}
			}
			
		}
		
		static void GenerateProjectActionsCode (CodeNamespace cns, GenerationOptions options, params ProjectBackend[] projects)
		{
			bool multiProject = projects.Length > 1;
			
			CodeTypeDeclaration type = new CodeTypeDeclaration ("ActionGroups");
			type.Attributes = MemberAttributes.Private;
			type.TypeAttributes = TypeAttributes.NestedAssembly;
			cns.Types.Add (type);

			// Generate the global action group getter
			
			CodeMemberMethod met = new CodeMemberMethod ();
			met.Name = "GetActionGroup";
			type.Members.Add (met);
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(Type), "type"));
			if (multiProject)
				met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(string), "file"));
			met.ReturnType = new CodeTypeReference (typeof(Gtk.ActionGroup));
			met.Attributes = MemberAttributes.Public | MemberAttributes.Static;

			CodeMethodInvokeExpression call = new CodeMethodInvokeExpression (
					new CodeMethodReferenceExpression (
						new CodeTypeReferenceExpression (new CodeTypeReference (cns.Name + ".ActionGroups")),
						"GetActionGroup"
					),
					new CodePropertyReferenceExpression (
						new CodeArgumentReferenceExpression ("type"),
						"FullName"
					)
			);
			if (multiProject)
				call.Parameters.Add (new CodeArgumentReferenceExpression ("file"));
				
			met.Statements.Add (new CodeMethodReturnStatement (call));

			// Generate the global action group getter (overload)
			
			met = new CodeMemberMethod ();
			met.Name = "GetActionGroup";
			type.Members.Add (met);
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(string), "name"));
			if (multiProject)
				met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(string), "file"));
			met.ReturnType = new CodeTypeReference (typeof(Gtk.ActionGroup));
			met.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			
			CodeArgumentReferenceExpression cfile = new CodeArgumentReferenceExpression ("file");
			CodeArgumentReferenceExpression cid = new CodeArgumentReferenceExpression ("name");
			
			CodeStatementCollection projectCol = met.Statements;
			int n=1;
			
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
				
				foreach (Wrapper.ActionGroup grp in gp.ActionGroups) {
					string fname = "group" + (n++);
					CodeMemberField grpField = new CodeMemberField (new CodeTypeReference (typeof(Gtk.ActionGroup), CodeTypeReferenceOptions.GlobalReference), fname);
					grpField.Attributes |= MemberAttributes.Static;
					type.Members.Add (grpField);
					CodeFieldReferenceExpression grpVar = new CodeFieldReferenceExpression (
						new CodeTypeReferenceExpression (new CodeTypeReference (cns.Name + ".ActionGroups", CodeTypeReferenceOptions.GlobalReference)),
						fname
					);
					
					CodeConditionStatement pcond = new CodeConditionStatement ();
					pcond.Condition = new CodeBinaryOperatorExpression (
						cid, 
						CodeBinaryOperatorType.IdentityEquality,
						new CodePrimitiveExpression (grp.Name)
					);
					widgetCol.Add (pcond);
					
					// If the group has not yet been created, create it
					CodeConditionStatement pcondGrp = new CodeConditionStatement ();
					pcondGrp.Condition = new CodeBinaryOperatorExpression (
						grpVar, 
						CodeBinaryOperatorType.IdentityEquality,
						new CodePrimitiveExpression (null)
					);
					
					pcondGrp.TrueStatements.Add (
						new CodeAssignStatement (
							grpVar,
							new CodeObjectCreateExpression (grp.Name)
						)
					);
					
					pcond.TrueStatements.Add (pcondGrp);
					pcond.TrueStatements.Add (new CodeMethodReturnStatement (grpVar));
					
					widgetCol = pcond.FalseStatements;
				}
				widgetCol.Add (new CodeMethodReturnStatement (new CodePrimitiveExpression (null)));
			}
			if (met.Statements.Count == 0)
				met.Statements.Add (new CodeMethodReturnStatement (new CodePrimitiveExpression (null)));
		}
		
		internal static List<ObjectBindInfo> GetFieldsToBind (ObjectWrapper wrapper)
		{
			List<ObjectBindInfo> tobind = new List<ObjectBindInfo> ();
			GetFieldsToBind (tobind, wrapper);
			return tobind;
		}
		
		static void GetFieldsToBind (List<ObjectBindInfo> tobind, ObjectWrapper wrapper)
		{
			string memberName = null;
			
			if (wrapper is Wrapper.Widget) {
				Wrapper.Widget ww = wrapper as Wrapper.Widget;
				if (!String.IsNullOrEmpty (ww.UIManagerName)) 
					tobind.Add (new ObjectBindInfo ("Gtk.UIManager", ww.UIManagerName));

				if (!ww.IsTopLevel && ww.InternalChildProperty == null && !ww.Unselectable)
					memberName = ((Wrapper.Widget) wrapper).Wrapped.Name;
			}
			else if (wrapper is Wrapper.Action)
				memberName = ((Wrapper.Action) wrapper).Name;
			
			if (memberName != null) {
				ObjectBindInfo binfo = new ObjectBindInfo (wrapper.WrappedTypeName, memberName);
				tobind.Add (binfo);
			}
			
			Wrapper.ActionGroup agroup = wrapper as Wrapper.ActionGroup;
			if (agroup != null) {
				foreach (Wrapper.Action ac in agroup.Actions)
					GetFieldsToBind (tobind, ac);
			}
			
			Wrapper.Widget widget = wrapper as Wrapper.Widget;
			if (widget != null && widget.IsTopLevel) {
				// Generate fields for local actions
				foreach (Wrapper.ActionGroup grp in widget.LocalActionGroups) {
					GetFieldsToBind (tobind, grp);
				}
			}
			
			Gtk.Container cont = wrapper.Wrapped as Gtk.Container;
			if (cont != null) {
				foreach (Gtk.Widget child in cont.AllChildren) {
					Stetic.Wrapper.Widget ww = Stetic.Wrapper.Widget.Lookup (child);
					if (ww != null)
						GetFieldsToBind (tobind, ww);
				}
			}
		}
		
		public static WidgetMap GenerateCreationCode (CodeNamespace cns, CodeTypeDeclaration type, Gtk.Widget w, CodeExpression widgetVarExp, CodeStatementCollection statements, GenerationOptions options, ArrayList warnings)
		{
			statements.Add (new CodeCommentStatement ("Widget " + w.Name));
			GeneratorContext ctx = new ProjectGeneratorContext (cns, type, statements, options);
			Stetic.Wrapper.Widget ww = Stetic.Wrapper.Widget.Lookup (w);
			ctx.GenerateCreationCode (ww, widgetVarExp);
			ctx.EndGeneration ();
			warnings.AddRange (ctx.Warnings);
			return ctx.WidgetMap;
		}
		
		public static WidgetMap GenerateCreationCode (CodeNamespace cns, CodeTypeDeclaration type, Wrapper.ActionGroup grp, CodeExpression groupVarExp, CodeStatementCollection statements, GenerationOptions options, ArrayList warnings)
		{
			statements.Add (new CodeCommentStatement ("Action group " + grp.Name));
			GeneratorContext ctx = new ProjectGeneratorContext (cns, type, statements, options);
			ctx.GenerateCreationCode (grp, groupVarExp);
			ctx.EndGeneration ();
			warnings.AddRange (ctx.Warnings);
			return ctx.WidgetMap;
		}
	}
	
	class ProjectGeneratorContext: GeneratorContext
	{
		CodeTypeDeclaration type;
		
		public ProjectGeneratorContext (CodeNamespace cns, CodeTypeDeclaration type, CodeStatementCollection statements, GenerationOptions options): base (cns, "w", statements, options)
		{
			this.type = type;
		}
		
		public override CodeExpression GenerateInstanceExpression (ObjectWrapper wrapper, CodeExpression newObject)
		{
			string typeName = wrapper.WrappedTypeName;
			string memberName = null;
			if (wrapper is Wrapper.Widget)
				memberName = ((Wrapper.Widget) wrapper).Wrapped.Name;
			else if (wrapper is Wrapper.Action)
				memberName = ((Wrapper.Action) wrapper).Name;
			
			if (memberName == null)
				return base.GenerateInstanceExpression (wrapper, newObject);
			
			if (Options.UsePartialClasses) {
				// Don't generate fields for top level widgets and for widgets accessible
				// through other widget's properties
				Wrapper.Widget ww = wrapper as Wrapper.Widget;
				if (ww == null || (!ww.IsTopLevel && ww.InternalChildProperty == null && !ww.Unselectable)) {
					type.Members.Add (
						new CodeMemberField (
							new CodeTypeReference (typeName, CodeTypeReferenceOptions.GlobalReference),
							memberName
						)
					);
					CodeExpression var = new CodeFieldReferenceExpression (
					                          new CodeThisReferenceExpression (),
					                          memberName
					);

					Statements.Add (
						new CodeAssignStatement (
							var,
							newObject
						)
					);
					return var;
				} else 
					return base.GenerateInstanceExpression (wrapper, newObject);
			} else {
				CodeExpression var = base.GenerateInstanceExpression (wrapper, newObject);
				Statements.Add (
					new CodeAssignStatement (
						new CodeIndexerExpression (
							new CodeVariableReferenceExpression ("bindings"),
							new CodePrimitiveExpression (memberName)
						),
						var
					)
				);
				return var;
			}
		}
	}
	
	[Serializable]
	public class SteticCompilationUnit: CodeCompileUnit
	{
		string name;
		
		public SteticCompilationUnit (string name)
		{
			this.name = name;
		}
		
		public string Name {
			get { return name; }
			internal set { name = value; }
		}
	}
}
