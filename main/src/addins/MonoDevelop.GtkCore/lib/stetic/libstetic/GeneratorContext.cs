using System;
using System.CodeDom;
using System.Collections;

namespace Stetic
{
	public class GeneratorContext
	{
		CodeNamespace cns;
		int n;
		string idPrefix;
		Hashtable vars = new Hashtable ();
		ArrayList generatedWrappers = new ArrayList ();
		WidgetMap map;
		CodeStatementCollection statements;
		GenerationOptions options;
		ArrayList warnings = new ArrayList ();
		CodeExpression rootObject;
		
		public GeneratorContext (CodeNamespace cns, string idPrefix, CodeStatementCollection statements, GenerationOptions options)
		{
			this.cns = cns;
			this.idPrefix = idPrefix;
			this.statements = statements;
			this.options = options;
			map = new WidgetMap (vars);
		}
		
		public CodeNamespace GlobalCodeNamespace {
			get { return cns; }
		}
		
		public CodeStatementCollection Statements {
			get { return statements; }
		}
		
		public GenerationOptions Options {
			get { return options; }
		}
		
		public string[] Warnings {
			get { return (string[]) warnings.ToArray (typeof(string)); }
		}
		
		public void ReportWarning (string s)
		{
			warnings.Add (s);
		}
		
		public string NewId ()
		{
			return idPrefix + (++n);
		}
		
		public CodeExpression GenerateNewInstanceCode (Wrapper.Widget widget)
		{
			CodeExpression exp = widget.GenerateObjectCreation (this);
			CodeExpression var = GenerateInstanceExpression (widget, exp);
			GenerateBuildCode (widget, var);
			return var;
		}
		
		public virtual CodeExpression GenerateInstanceExpression (ObjectWrapper wrapper, CodeExpression newObject)
		{
			string varName = NewId ();
			CodeVariableDeclarationStatement varDec = new CodeVariableDeclarationStatement (wrapper.WrappedTypeName, varName);
			varDec.InitExpression = newObject;
			statements.Add (varDec);
			return new CodeVariableReferenceExpression (varName);
		}
		
		public virtual void GenerateCreationCode (ObjectWrapper wrapper, CodeExpression varExp)
		{
			rootObject = varExp;
			wrapper.GenerateInitCode (this, varExp);
			GenerateBuildCode (wrapper, varExp);
		}
		
		public virtual void GenerateBuildCode (ObjectWrapper wrapper, CodeExpression var)
		{
			vars [wrapper] = var;
			wrapper.GenerateBuildCode (this, var);
			generatedWrappers.Add (wrapper);
		}
		
		public virtual void GenerateCreationCode (Wrapper.ActionGroup agroup, CodeExpression var)
		{
			rootObject = var;
			vars [agroup] = var;
			agroup.GenerateBuildCode (this, var);
		}
		
		public CodeExpression GenerateValue (object value, Type type)
		{
			return GenerateValue (value, type, false);
		}
		
		public CodeExpression GenerateValue (object value, Type type, bool translatable)
		{
			if (value == null)
				return new CodePrimitiveExpression (value);
				
			if (value.GetType ().IsEnum) {
				if (!type.IsEnum) {
					object ival = Convert.ChangeType (value, type);
					return new CodePrimitiveExpression (ival);
				} else {
					long ival = (long) Convert.ChangeType (value, typeof(long));
					return new CodeCastExpression (
						new CodeTypeReference (value.GetType ()), 
						new CodePrimitiveExpression (ival)
					);
				}
			}
			
			if (value is Gtk.Adjustment) {
				Gtk.Adjustment adj = value as Gtk.Adjustment;
				return new CodeObjectCreateExpression (
					typeof(Gtk.Adjustment),
					new CodePrimitiveExpression (adj.Value),
					new CodePrimitiveExpression (adj.Lower),
					new CodePrimitiveExpression (adj.Upper),
					new CodePrimitiveExpression (adj.StepIncrement),
					new CodePrimitiveExpression (adj.PageIncrement),
					new CodePrimitiveExpression (adj.PageSize));
			}
			if (value is ushort || value is uint) {
				return new CodeCastExpression (
					new CodeTypeReference (value.GetType ()),
					new CodePrimitiveExpression (Convert.ChangeType (value, typeof(long))));
			}
			if (value is ulong) {
				return new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression (value.GetType ()),
					"Parse",
					new CodePrimitiveExpression (value.ToString ()));
			}
			
			if (value is ImageInfo && typeof(Gdk.Pixbuf).IsAssignableFrom (type))
				return ((ImageInfo)value).ToCodeExpression (this);
			
			if (value is Wrapper.ActionGroup) {
				return new CodeMethodInvokeExpression (
					new CodeMethodReferenceExpression (
						new CodeTypeReferenceExpression (GlobalCodeNamespace.Name + ".ActionGroups"),
						"GetActionGroup"
					),
					new CodePrimitiveExpression (((Wrapper.ActionGroup)value).Name)
				);
			}
			
			if (value is Array) {
				ArrayList list = new ArrayList ();
				foreach (object val in (Array)value)
					list.Add (GenerateValue (val, val != null ? val.GetType() : null, translatable));
				return new CodeArrayCreateExpression (value.GetType().GetElementType(), (CodeExpression[]) list.ToArray(typeof(CodeExpression)));
			}
			
			if (value is DateTime) {
				return new CodeObjectCreateExpression (
					typeof(DateTime),
					new CodePrimitiveExpression (((DateTime)value).Ticks)
				);
			}
			
			if (value is TimeSpan) {
				return new CodeObjectCreateExpression (
					typeof(TimeSpan),
					new CodePrimitiveExpression (((TimeSpan)value).Ticks)
				);
			}
			
			string str = value as string;
			if (translatable && str != null && str.Length > 0 && options.UseGettext) {
				return new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression (options.GettextClass),
					"GetString",
					new CodePrimitiveExpression (str)
				);
			}
			
			return new CodePrimitiveExpression (value);
		}
		
		public WidgetMap WidgetMap {
			get { return map; }
		}

		public System.CodeDom.CodeExpression RootObject {
			get {
				return rootObject;
			}
			set {
				rootObject = value;
			}
		}
		
		public void EndGeneration ()
		{
			foreach (ObjectWrapper w in generatedWrappers) {
				CodeExpression var = (CodeExpression) vars [w];
				w.GeneratePostBuildCode (this, var);
			}
		}
		
		public void Reset ()
		{
			vars.Clear ();
			generatedWrappers.Clear ();
			map = new WidgetMap (vars);
			n = 0;
		}
		
		public CodeExpression GenerateLoadPixbuf (string name, Gtk.IconSize size)
		{
			bool found = false;
			foreach (CodeTypeDeclaration t in cns.Types) {
				if (t.Name == "IconLoader") {
					found = true;
					break;
				}
			}
			
			if (!found)
			{
				CodeTypeDeclaration cls = new CodeTypeDeclaration ("IconLoader");
				cls.Attributes = MemberAttributes.Private;
				cls.TypeAttributes = System.Reflection.TypeAttributes.NestedAssembly;
				cns.Types.Add (cls);
				
				CodeMemberMethod met = new CodeMemberMethod ();
				cls.Members.Add (met);
				met.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				met.Name = "LoadIcon";
				met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(Gtk.Widget), "widget"));
				met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(string), "name"));
				met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(Gtk.IconSize), "size"));
				met.Parameters.Add (new CodeParameterDeclarationExpression (typeof(int), "sz"));
				met.ReturnType = new CodeTypeReference (typeof(Gdk.Pixbuf));
				
				CodeExpression widgetExp = new CodeVariableReferenceExpression ("widget");
				CodeExpression nameExp = new CodeVariableReferenceExpression ("name");
				CodeExpression sizeExp = new CodeVariableReferenceExpression ("size");
				CodeExpression szExp = new CodeVariableReferenceExpression ("sz");
				CodeExpression mgExp = new CodeBinaryOperatorExpression (szExp,  CodeBinaryOperatorType.Divide, new CodePrimitiveExpression (4));
				CodeExpression pmapExp = new CodeVariableReferenceExpression ("pmap");
				CodeExpression gcExp = new CodeVariableReferenceExpression ("gc");
				CodeExpression szM1Exp = new CodeBinaryOperatorExpression (szExp, CodeBinaryOperatorType.Subtract, new CodePrimitiveExpression (1));
				CodeExpression zeroExp = new CodePrimitiveExpression (0);
				CodeExpression resExp = new CodeVariableReferenceExpression ("res");
				
				met.Statements.Add (
					new CodeVariableDeclarationStatement (typeof(Gdk.Pixbuf), "res",
						new CodeMethodInvokeExpression (
							widgetExp,
							"RenderIcon",
							nameExp,
							sizeExp,
							new CodePrimitiveExpression (null)
						)
					)
				);
				
				CodeConditionStatement nullcheck = new CodeConditionStatement ();
				met.Statements.Add (nullcheck);
				nullcheck.Condition = new CodeBinaryOperatorExpression (
					resExp,
					CodeBinaryOperatorType.IdentityInequality,
					new CodePrimitiveExpression (null)
				);
				nullcheck.TrueStatements.Add (new CodeMethodReturnStatement (resExp));
				
				CodeTryCatchFinallyStatement trycatch = new CodeTryCatchFinallyStatement ();
				nullcheck.FalseStatements.Add (trycatch);
				trycatch.TryStatements.Add (
					new CodeMethodReturnStatement (
						new CodeMethodInvokeExpression (
							new CodePropertyReferenceExpression (
								new CodeTypeReferenceExpression (typeof(Gtk.IconTheme)),
								"Default"
							),
							"LoadIcon",
							nameExp,
							szExp,
							zeroExp
						)
					)
				);
				
				CodeCatchClause ccatch = new CodeCatchClause ();
				trycatch.CatchClauses.Add (ccatch);				
				
				CodeConditionStatement cond = new CodeConditionStatement ();
				ccatch.Statements.Add (cond);
				
				cond.Condition = new CodeBinaryOperatorExpression (
					nameExp,
					CodeBinaryOperatorType.IdentityInequality,
					new CodePrimitiveExpression ("gtk-missing-image")
				);
				
				cond.TrueStatements.Add (
					new CodeMethodReturnStatement (
						new CodeMethodInvokeExpression (
							new CodeTypeReferenceExpression (cns.Name + "." + cls.Name),
							"LoadIcon",
							widgetExp,
							new CodePrimitiveExpression ("gtk-missing-image"),
							sizeExp,
							szExp
						)
					)
				);
				
				CodeStatementCollection stms = cond.FalseStatements;
				
				stms.Add (
					new CodeVariableDeclarationStatement (typeof(Gdk.Pixmap), "pmap", 
						new CodeObjectCreateExpression (
							typeof(Gdk.Pixmap),
							new CodePropertyReferenceExpression (
								new CodePropertyReferenceExpression (
									new CodeTypeReferenceExpression (typeof(Gdk.Screen)),
									"Default"
								),
								"RootWindow"
							),
							szExp,
							szExp
						)
					)
				);
				stms.Add (
					new CodeVariableDeclarationStatement (typeof(Gdk.GC), "gc", 
						new CodeObjectCreateExpression (typeof(Gdk.GC), pmapExp)
					)
				);
				stms.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (
							gcExp,
							"RgbFgColor"
						),
						new CodeObjectCreateExpression (
							typeof(Gdk.Color),
							new CodePrimitiveExpression (255),
							new CodePrimitiveExpression (255),
							new CodePrimitiveExpression (255)
						)
					)
				);
				stms.Add (
					new CodeMethodInvokeExpression (
						pmapExp,
						"DrawRectangle",
						gcExp,
						new CodePrimitiveExpression (true),
						zeroExp,
						zeroExp,
						szExp,
						szExp
					)
				);
				stms.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (
							gcExp,
							"RgbFgColor"
						),
						new CodeObjectCreateExpression (
							typeof(Gdk.Color),
							zeroExp, zeroExp, zeroExp
						)
					)
				);
				stms.Add (
					new CodeMethodInvokeExpression (
						pmapExp,
						"DrawRectangle",
						gcExp,
						new CodePrimitiveExpression (false),
						zeroExp,
						zeroExp,
						szM1Exp,
						szM1Exp
					)
				);
				stms.Add (
					new CodeMethodInvokeExpression (
						gcExp,
						"SetLineAttributes",
						new CodePrimitiveExpression (3),
						new CodeFieldReferenceExpression (new CodeTypeReferenceExpression (typeof(Gdk.LineStyle)), "Solid"),
						new CodeFieldReferenceExpression (new CodeTypeReferenceExpression (typeof(Gdk.CapStyle)), "Round"),
						new CodeFieldReferenceExpression (new CodeTypeReferenceExpression (typeof(Gdk.JoinStyle)), "Round")
					)
				);
				stms.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (
							gcExp,
							"RgbFgColor"
						),
						new CodeObjectCreateExpression (
							typeof(Gdk.Color),
							new CodePrimitiveExpression (255),
							zeroExp,
							zeroExp
						)
					)
				);
				stms.Add (
					new CodeMethodInvokeExpression (
						pmapExp,
						"DrawLine",
						gcExp,
						mgExp,
						mgExp,
						new CodeBinaryOperatorExpression (szM1Exp, CodeBinaryOperatorType.Subtract, mgExp),
						new CodeBinaryOperatorExpression (szM1Exp, CodeBinaryOperatorType.Subtract, mgExp)
					)
				);
				stms.Add (
					new CodeMethodInvokeExpression (
						pmapExp,
						"DrawLine",
						gcExp,
						new CodeBinaryOperatorExpression (szM1Exp, CodeBinaryOperatorType.Subtract, mgExp),
						mgExp,
						mgExp,
						new CodeBinaryOperatorExpression (szM1Exp, CodeBinaryOperatorType.Subtract, mgExp)
					)
				);
				stms.Add (
					new CodeMethodReturnStatement (
						new CodeMethodInvokeExpression (
							new CodeTypeReferenceExpression (typeof(Gdk.Pixbuf)),
							"FromDrawable",
							pmapExp,
							new CodePropertyReferenceExpression (pmapExp, "Colormap"),
							zeroExp, zeroExp, zeroExp, zeroExp, szExp, szExp
						)
					)
				);
			}
			
			int sz, h;
			Gtk.Icon.SizeLookup (size, out sz, out h);
			
			return new CodeMethodInvokeExpression (
				new CodeTypeReferenceExpression (cns.Name + ".IconLoader"),
				"LoadIcon",
				rootObject,
				new CodePrimitiveExpression (name),
			    new CodeFieldReferenceExpression (
					new CodeTypeReferenceExpression (typeof(Gtk.IconSize)),
					size.ToString ()
				),
				new CodePrimitiveExpression (sz)
			);
		}
	}

	public class WidgetMap
	{
		Hashtable vars;
		
		internal WidgetMap (Hashtable vars)
		{
			this.vars = vars;
		}
		
		public CodeExpression GetWidgetExp (ObjectWrapper wrapper)
		{
			return (CodeExpression) vars [wrapper];
		}
		
		public CodeExpression GetWidgetExp (object wrapped)
		{
			ObjectWrapper w = ObjectWrapper.Lookup (wrapped);
			if (w != null)
				return GetWidgetExp (w);
			else
				return null;
		}
	}
	
	[Serializable]
	public class GenerationOptions
	{
		bool useGettext;
		bool partialClasses;
		bool generateEmptyBuildMethod;
		bool generateSingleFile = true;
		bool failForUnknownWidgets = false;
		string path;
		string globalNamespace = "Stetic";
		string gettextClass;
		
		public bool UseGettext {
			get { return useGettext; }
			set { useGettext = value; }
		}
		
		public string GettextClass {
			get {
				if (gettextClass == null || gettextClass.Length == 0)
					return "Mono.Unix.Catalog";
				else
					return gettextClass;
			}
			set { gettextClass = value; }
		}
		
		public bool UsePartialClasses {
			get { return partialClasses; }
			set { partialClasses = value; }
		}
		
		public string Path {
			get { return path; }
			set { path = value; }
		}
		
		public bool GenerateEmptyBuildMethod {
			get { return generateEmptyBuildMethod; }
			set { generateEmptyBuildMethod = value; }
		}
		
		public bool GenerateSingleFile {
			get { return generateSingleFile; }
			set { generateSingleFile = value; }
		}
		
		public string GlobalNamespace {
			get { return globalNamespace; }
			set { globalNamespace = value; }
		}
		
		public bool FailForUnknownWidgets {
			get { return failForUnknownWidgets; }
			set { failForUnknownWidgets = value; }
		}
	}
}

