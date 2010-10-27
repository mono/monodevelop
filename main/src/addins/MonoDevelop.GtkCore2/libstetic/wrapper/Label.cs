using System;
using System.CodeDom;
using System.Collections;

namespace Stetic.Wrapper {

	public class Label : Misc {

		public Label () {}

		string mnem;
		public string MnemonicWidget {
			get {
				return mnem;
			}
			set {
				mnem = value;
			}
		}
		
		protected override void GeneratePropertySet (GeneratorContext ctx, CodeExpression var, PropertyDescriptor prop)
		{
			if (prop.Name != "MnemonicWidget")
				base.GeneratePropertySet (ctx, var, prop);
		}
		
		internal protected override void GeneratePostBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			if (mnem != null) {
				Widget targetWidget = GetTopLevel ().FindChild (mnem);
				if (targetWidget != null) {
					CodeExpression memVar = ctx.WidgetMap.GetWidgetExp (targetWidget);
					if (memVar != null) {
						ctx.Statements.Add (
							new CodeAssignStatement (
								new CodePropertyReferenceExpression (
									var, 
									"MnemonicWidget"
								),
								memVar
							)
						);
					}
				}
			}
			base.GeneratePostBuildCode (ctx, var);
		}
	}
}
