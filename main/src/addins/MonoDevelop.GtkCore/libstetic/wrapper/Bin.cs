using System;
using System.CodeDom;
using System.Reflection;

namespace Stetic.Wrapper
{
	public class Bin: Container
	{
		public static new Gtk.Bin CreateInstance (ClassDescriptor klass)
		{
			if (klass.Name == "Gtk.Bin")
				return new CustomWidget ();
			else
				return null;
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			if (ClassDescriptor.WrappedTypeName == "Gtk.Bin") {
			
				// Gtk.Bin needs a helper class which handles child allocation.
				// This class needs to be generated since Stetic won't be linked with
				// the app.
				
				bool found = false;
				foreach (CodeTypeDeclaration dec in ctx.GlobalCodeNamespace.Types) {
					if (dec.Name == "BinContainer") {
						found = true;
						break;
					}
				}
				
				if (!found)
					GenerateHelperClass (ctx);
				
				CodeMethodInvokeExpression attachExp = new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression (ctx.GlobalCodeNamespace.Name + ".BinContainer"),
					"Attach",
					var
				);
				
				// If the Bin has its own action groups, we need to register
				// the resulting UIManager in the BinContainer, but it needs to be done
				// after generating it. Right now, we only keep a reference to
				// the BinContainer.
				
				string binContainerVar = null;
				
				if (IsTopLevel && LocalActionGroups.Count > 0) {
					binContainerVar = ctx.NewId ();
					ctx.Statements.Add (
						new CodeVariableDeclarationStatement (
							ctx.GlobalCodeNamespace.Name + ".BinContainer", 
							binContainerVar,
							attachExp
						)
					);
				} else {
					ctx.Statements.Add (attachExp);
				}
				
				base.GenerateBuildCode (ctx, var);
				
				// Register the UIManager, if the Bin has one
				
				if (binContainerVar != null && UIManagerName != null) {
					ctx.Statements.Add (
						new CodeMethodInvokeExpression (
							new CodeVariableReferenceExpression (binContainerVar),
							"SetUiManager",
							new CodeVariableReferenceExpression (UIManagerName)
						)
					);
				}
				
			} else
				base.GenerateBuildCode (ctx, var);
		}
		
		void GenerateHelperClass (GeneratorContext ctx)
		{
			CodeTypeDeclaration type = new CodeTypeDeclaration ("BinContainer");
			type.Attributes = MemberAttributes.Private;
			type.TypeAttributes = TypeAttributes.NestedAssembly;
			ctx.GlobalCodeNamespace.Types.Add (type);
			
			CodeMemberField field = new CodeMemberField ("Gtk.Widget", "child");
			field.Attributes = MemberAttributes.Private;
			type.Members.Add (field);
			
			field = new CodeMemberField ("Gtk.UIManager", "uimanager");
			field.Attributes = MemberAttributes.Private;
			type.Members.Add (field);
			
			CodeExpression child = new CodeFieldReferenceExpression (
				new CodeThisReferenceExpression (),
				"child"
			);
			
			CodeExpression uimanager = new CodeFieldReferenceExpression (
				new CodeThisReferenceExpression (),
				"uimanager"
			);
			
			// Attach method
			
			CodeMemberMethod met = new CodeMemberMethod ();
			type.Members.Add (met);
			met.Name = "Attach";
			met.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			met.ReturnType = new CodeTypeReference ("BinContainer");
			met.Parameters.Add (new CodeParameterDeclarationExpression ("Gtk.Bin", "bin"));
			
			CodeVariableDeclarationStatement bcDec = new CodeVariableDeclarationStatement ("BinContainer", "bc");
			bcDec.InitExpression = new CodeObjectCreateExpression ("BinContainer");
			met.Statements.Add (bcDec);
			CodeVariableReferenceExpression bc = new CodeVariableReferenceExpression ("bc");
			CodeArgumentReferenceExpression bin = new CodeArgumentReferenceExpression ("bin");
			
			met.Statements.Add (
				new CodeAttachEventStatement (
					bin, 
					"SizeRequested",
					new CodeDelegateCreateExpression (
						new CodeTypeReference ("Gtk.SizeRequestedHandler"), bc, "OnSizeRequested"
					)
				)
			);
			
			met.Statements.Add (
				new CodeAttachEventStatement (
					bin, 
					"SizeAllocated",
					new CodeDelegateCreateExpression (
						new CodeTypeReference ("Gtk.SizeAllocatedHandler"), bc, "OnSizeAllocated"
					)
				)
			);
			
			met.Statements.Add (
				new CodeAttachEventStatement (
					bin, 
					"Added",
					new CodeDelegateCreateExpression (
						new CodeTypeReference ("Gtk.AddedHandler"), bc, "OnAdded"
					)
				)
			);
			met.Statements.Add (new CodeMethodReturnStatement (bc));
			
			// OnSizeRequested override
			
			met = new CodeMemberMethod ();
			type.Members.Add (met);
			met.Name = "OnSizeRequested";
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(object), "sender"));
			met.Parameters.Add (new CodeParameterDeclarationExpression ("Gtk.SizeRequestedArgs", "args"));
			
			CodeConditionStatement cond = new CodeConditionStatement ();
			cond.Condition = new CodeBinaryOperatorExpression (
						child,
						CodeBinaryOperatorType.IdentityInequality,
						new CodePrimitiveExpression (null)
			);
			cond.TrueStatements.Add (
				new CodeAssignStatement (
					new CodePropertyReferenceExpression (
						new CodeArgumentReferenceExpression ("args"),
						"Requisition"
					),
					new CodeMethodInvokeExpression (
						child,
						"SizeRequest"
					)
				)
			);
			met.Statements.Add (cond);
			
			// OnSizeAllocated method
			
			met = new CodeMemberMethod ();
			type.Members.Add (met);
			met.Name = "OnSizeAllocated";
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(object), "sender"));
			met.Parameters.Add (new CodeParameterDeclarationExpression ("Gtk.SizeAllocatedArgs", "args"));
			
			cond = new CodeConditionStatement ();
			cond.Condition = new CodeBinaryOperatorExpression (
						child,
						CodeBinaryOperatorType.IdentityInequality,
						new CodePrimitiveExpression (null)
			);
			cond.TrueStatements.Add (
				new CodeAssignStatement (
					new CodePropertyReferenceExpression (
						child,
						"Allocation"
					),
					new CodePropertyReferenceExpression (
						new CodeArgumentReferenceExpression ("args"),
						"Allocation"
					)
				)
			);
			met.Statements.Add (cond);
			
			// OnAdded method
			
			met = new CodeMemberMethod ();
			type.Members.Add (met);
			met.Name = "OnAdded";
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(object), "sender"));
			met.Parameters.Add (new CodeParameterDeclarationExpression ("Gtk.AddedArgs", "args"));
			
			met.Statements.Add (
				new CodeAssignStatement (
					child,
					new CodePropertyReferenceExpression (
						new CodeArgumentReferenceExpression ("args"),
						"Widget"
					)
				)
			);
			
			// SetUiManager method
			
			met = new CodeMemberMethod ();
			type.Members.Add (met);
			met.Name = "SetUiManager";
			met.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(Gtk.UIManager), "uim"));
			
			met.Statements.Add (
				new CodeAssignStatement (
					uimanager,
					new CodeArgumentReferenceExpression ("uim")
				)
			);
			met.Statements.Add (
				new CodeAttachEventStatement (
					child, 
					"Realized",
					new CodeDelegateCreateExpression (
						new CodeTypeReference ("System.EventHandler"), new CodeThisReferenceExpression(), "OnRealized"
					)
				)
			);
			
			// OnRealized method
			
			met = new CodeMemberMethod ();
			type.Members.Add (met);
			met.Name = "OnRealized";
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(object), "sender"));
			met.Parameters.Add (new CodeParameterDeclarationExpression ("System.EventArgs", "args"));
			
			cond = new CodeConditionStatement ();
			cond.Condition = new CodeBinaryOperatorExpression (
						uimanager,
						CodeBinaryOperatorType.IdentityInequality,
						new CodePrimitiveExpression (null)
			);
			
			cond.TrueStatements.Add (
				new CodeVariableDeclarationStatement (
					typeof(Gtk.Widget),
					"w"
				)
			);
			
			CodeExpression wexp = new CodeVariableReferenceExpression ("w");
			
			cond.TrueStatements.Add (
				new CodeAssignStatement (
					wexp,
					new CodePropertyReferenceExpression (
						child,
						"Toplevel"
					)
				)
			);
								
			CodeConditionStatement cond2 = new CodeConditionStatement ();
			cond2.Condition = new CodeBinaryOperatorExpression (
				new CodeBinaryOperatorExpression (
					wexp,
					CodeBinaryOperatorType.IdentityInequality,
					new CodePrimitiveExpression (null)
				),
				CodeBinaryOperatorType.BooleanAnd,
				new CodeMethodInvokeExpression (
					new CodeTypeOfExpression ("Gtk.Window"),
					"IsInstanceOfType",
					wexp
				)
			);
			
			cond2.TrueStatements.Add (
				new CodeMethodInvokeExpression (
					new CodeCastExpression ("Gtk.Window", wexp),
					"AddAccelGroup",
					new CodePropertyReferenceExpression (
						uimanager,
						"AccelGroup"
					)
				)
			);
			cond2.TrueStatements.Add (
				new CodeAssignStatement (
					uimanager,
					new CodePrimitiveExpression (null)
				)
			);
			cond.TrueStatements.Add (cond2);
			
			met.Statements.Add (cond);
		}
	}
	
/*
	 This is a model of what GenerateHelperClass generates:
	
	class BinContainer
	{
		Gtk.Widget child;
		UIManager uimanager;
		
		public static BinContainer Attach (Gtk.Bin bin)
		{
			BinContainer bc = new BinContainer ();
			bin.SizeRequested += new Gtk.SizeRequestedHandler (bc.OnSizeRequested);
			bin.SizeAllocated += new Gtk.SizeAllocatedHandler (bc.OnSizeAllocated);
			bin.Added += new Gtk.AddedHandler (bc.OnAdded);
			return bin;
		}
		
		void OnSizeRequested (object s, Gtk.SizeRequestedArgs args)
		{
			if (child != null)
				args.Requisition = child.SizeRequest ();
		}
		
		void OnSizeAllocated (object s, Gtk.SizeAllocatedArgs args)
		{
			if (child != null)
				child.Allocation = args.Allocation;
		}
		
		void OnAdded (object s, Gtk.AddedArgs args)
		{
			child = args.Widget;
		}
		
		public void SetUiManager (UIManager manager)
		{
			uimanager = manager;
			child.Realized += new System.EventHandler (OnRealized);
		}
		
		void OnRealized ()
		{
			if (uimanager != null) {
				Gtk.Widget w = child.Toplevel;
				if (w != null && w is Gtk.Window) {
					((Gtk.Window)w).AddAccelGroup (uimanager.AccelGroup);
					uimanager = null;
				}
			}
		}
	}
*/

}
