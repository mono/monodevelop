using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Globalization;
using Mono.Debugger;
using Mono.Debugger.Languages;

namespace Mono.Debugger.Frontend
{
	public class ExpressionParser : IExpressionParser
	{
		public readonly Interpreter Interpreter;

		private readonly CSharp.ExpressionParser parser;

		internal ExpressionParser (Interpreter interpreter)
		{
			this.Interpreter = interpreter;

			parser = new CSharp.ExpressionParser ("C#");
		}

		public Expression Parse (string expression)
		{
			return parser.Parse (expression);
		}

		protected static SourceLocation FindFile (Thread target, string filename, int line)
		{
			SourceFile file = target.Process.FindFile (filename);
			if (file == null)
				throw new ScriptingException ("Cannot find source file `{0}'.",
							      filename);

			MethodSource source = file.FindMethod (line);
			if (source == null)
				throw new ScriptingException (
					"Cannot find method corresponding to line {0} in `{1}'.",
					line, file.Name);

			return new SourceLocation (source, file, line);
		}

		protected SourceLocation DoParseExpression (Thread target, StackFrame frame,
							    LocationType type, string arg)
		{
			ScriptingContext context = new ScriptingContext (Interpreter);
			context.CurrentThread = frame.Thread;
			context.CurrentFrame = frame;

			Expression expr = Parse (arg);
			if (expr == null)
				throw new ScriptingException ("Cannot resolve expression `{0}'.", arg);

			MethodExpression mexpr = expr.ResolveMethod (context, type);

			if (mexpr != null)
				return mexpr.EvaluateSource (context);
			else
				return context.FindMethod (arg);
		}

		public static bool ParseLocation (Thread target, StackFrame frame,
						  string arg, out SourceLocation location)
		{
			int line;
			int pos = arg.IndexOf (':');
			if (pos >= 0) {
				string filename = arg.Substring (0, pos);
				try {
					line = (int) UInt32.Parse (arg.Substring (pos+1));
				} catch {
					throw new ScriptingException ("Expected filename:line");
				}

				location = FindFile (frame.Thread, filename, line);
				return true;
			}

			try {
				line = (int) UInt32.Parse (arg);
			} catch {
				location = null;
				return false;
			}

			if ((frame == null) || (frame.SourceLocation == null) ||
			    (frame.SourceLocation.FileName == null))
				throw new ScriptingException (
					"Current stack frame doesn't have source code");

			location = FindFile (frame.Thread, frame.SourceLocation.FileName, line);
			return true;
		}

		protected SourceLocation DoParse (Thread target, StackFrame frame,
						  LocationType type, string arg)
		{
			if (type != LocationType.Default)
				return DoParseExpression (target, frame, type, arg);

			SourceLocation location;
			if (ParseLocation (target, frame, arg, out location))
				return location;

			return DoParseExpression (target, frame, type, arg);
		}

		public SourceLocation Parse (Thread target, StackFrame frame,
					     LocationType type, string arg)
		{
			try {
				return DoParse (target, frame, type, arg);
			} catch (ScriptingException ex) {
				throw new TargetException (TargetError.LocationInvalid, ex.Message);
			}
		}
	}

	public abstract class Expression
	{
		public abstract string Name {
			get;
		}

		protected bool resolved;

		protected virtual TargetType DoEvaluateType (ScriptingContext context)
		{
			return EvaluateObject (context).Type;
		}

		public TargetType EvaluateType (ScriptingContext context)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			try {
				TargetType type = DoEvaluateType (context);
				if (type == null)
					throw new ScriptingException (
						"Cannot get type of expression `{0}'", Name);

				return type;
			} catch (LocationInvalidException ex) {
				throw new ScriptingException (
					"Location of variable `{0}' is invalid: {1}",
					Name, ex.Message);
			}
		}

		protected virtual object DoEvaluate (ScriptingContext context)
		{
			return DoEvaluateObject (context);
		}

		public object Evaluate (ScriptingContext context)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			object result = DoEvaluate (context);
			if (result == null)
				throw new ScriptingException (
					"Cannot evaluate expression `{0}'", Name);

			return result;
		}

		protected virtual TargetVariable DoEvaluateVariable (ScriptingContext context)
		{
			return null;
		}

		public TargetVariable EvaluateVariable (ScriptingContext context)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}' ({1})", Name,
						GetType ()));

			try {
				TargetVariable retval = DoEvaluateVariable (context);
				if (retval == null)
					throw new ScriptingException (
						"Expression `{0}' is not a variable", Name);

				return retval;
			} catch (LocationInvalidException ex) {
				throw new ScriptingException (
					"Location of variable `{0}' is invalid: {1}",
					Name, ex.Message);
			}
		}

		protected virtual TargetObject DoEvaluateObject (ScriptingContext context)
		{
			TargetVariable var = DoEvaluateVariable (context);
			if (var == null)
				return null;

			TargetObject obj = var.GetObject (context.CurrentFrame);
			if (obj == null)
				return null;

			TargetStructObject cobj = obj as TargetStructObject;
			if (cobj != null) {
				TargetObject current;
				try {
					current = cobj.GetCurrentObject (context.CurrentThread);
				} catch {
					current = null;
				}
				if (current == null)
					return obj;

				TargetClassObject scurrent = current as TargetClassObject;
				if ((scurrent != null) && scurrent.Type.IsCompilerGenerated)
					return obj;
				else
					return current;
			}

			return obj;
		}

		public TargetObject EvaluateObject (ScriptingContext context)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}' ({1})", Name,
						GetType ()));

			try {
				TargetObject retval = DoEvaluateObject (context);
				if (retval == null)
					throw new ScriptingException (
						"Expression `{0}' is not a variable", Name);

				return retval;
			} catch (LocationInvalidException ex) {
				throw new ScriptingException (
					"Location of variable `{0}' is invalid: {1}",
					Name, ex.Message);
			}
		}

		protected virtual TargetFunctionType DoEvaluateMethod (ScriptingContext context,
								       LocationType type,
								       Expression[] types)
		{
			return null;
		}

		public TargetFunctionType EvaluateMethod (ScriptingContext context,
							  LocationType type, Expression [] types)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			try {
				TargetFunctionType func = DoEvaluateMethod (context, type, types);
				if (func == null)
					throw new ScriptingException (
						"Expression `{0}' is not a method", Name);

				return func;
			} catch (LocationInvalidException ex) {
				throw new ScriptingException (
					"Location of variable `{0}' is invalid: {1}",
					Name, ex.Message);
			}
		}

		protected virtual bool DoAssign (ScriptingContext context, TargetObject obj)
		{
			return false;
		}

		public void Assign (ScriptingContext context, TargetObject obj)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			bool ok = DoAssign (context, obj);
			if (!ok)
				throw new ScriptingException (
					"Expression `{0}' is not an lvalue", Name);
		}

		protected virtual Expression DoResolveType (ScriptingContext context)
		{
			return null;
		}

		public Expression ResolveType (ScriptingContext context)
		{
			Expression expr = DoResolveType (context);
			if (expr == null)
				throw new ScriptingException (
					"Expression `{0}' is not a type.", Name);

			return expr;
		}

		public Expression TryResolveType (ScriptingContext context)
		{
			try {
				return DoResolveType (context);
			} catch (ScriptingException) {
				return null;
			} catch (TargetException) {
				return null;
			}
		}

		protected abstract Expression DoResolve (ScriptingContext context);

		public Expression Resolve (ScriptingContext context)
		{
			Expression expr = DoResolve (context);
			if (expr == null)
				throw new ScriptingException (
					"Expression `{0}' is not a variable or value.", Name);

			return expr;
		}

		protected virtual MethodExpression DoResolveMethod (ScriptingContext context,
								    LocationType type)
		{
			return null;
		}

		public MethodExpression ResolveMethod (ScriptingContext context, LocationType type)
		{
			MethodExpression expr = DoResolveMethod (context, type);
			if (expr == null)
				throw new ScriptingException (
					"Expression `{0}' is not a method.", Name);

			return expr;
		}

		public Expression TryResolve (ScriptingContext context)
		{
			try {
				return DoResolve (context);
			} catch (ScriptingException) {
				return null;
			} catch (TargetException) {
				return null;
			}
		}

		public override string ToString ()
		{
			return String.Format ("{0} ({1})", GetType (), Name);
		}
	}

	public class NumberExpression : PointerExpression
	{
		object val;

		public NumberExpression (int val)
		{
			this.val = val;
		}

		public NumberExpression (uint val)
		{
			this.val = val;
		}

		public NumberExpression (long val)
		{
			this.val = val;
		}

		public NumberExpression (ulong val)
		{
			this.val = val;
		}

		public NumberExpression (float val)
		{
			this.val = val;
		}

		public NumberExpression (double val)
		{
			this.val = val;
		}

		public NumberExpression (decimal val)
		{
			this.val = val;
		}

		public long Value {
			get {
				if (val is int)
					return (long) (int) val;
				else if (val is uint)
					return (long) (uint) val;
				else if (val is ulong)
					return (long) (ulong) val;
				else
					return (long) val;
			}
		}

		public override string Name {
			get {
				if (val is long)
					return String.Format ("0x{0:x}", (long) val);
				else
					return val.ToString ();
			}
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			resolved = true;
			return this;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			StackFrame frame = context.CurrentFrame;
			if ((frame.Language == null) ||
			    !frame.Language.CanCreateInstance (val.GetType ()))
				throw new ScriptingException (
					"Cannot instantiate value '{0}' in the current frame's " +
					"language", Name);

			return frame.Language.CreateInstance (frame.Thread, val);
		}

		public override TargetAddress EvaluateAddress (ScriptingContext context)
		{
			return new TargetAddress (context.AddressDomain, Value);
		}

		protected override object DoEvaluate (ScriptingContext context)
		{
			return val;
		}

		public override string ToString ()
		{
			return Name;
		}
	}

	public class StringExpression : Expression
	{
		string val;

		public StringExpression (string val)
		{
			this.val = val;
		}

		public override string Name {
			get { return '"' + val + '"'; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			resolved = true;
			return this;
		}

		protected override object DoEvaluate (ScriptingContext context)
		{
			return val;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			StackFrame frame = context.CurrentFrame;
			if ((frame.Language == null) ||
			    !frame.Language.CanCreateInstance (typeof (string)))
				throw new ScriptingException ("Cannot instantiate value '{0}' in the current frame's language", Name);

			return frame.Language.CreateInstance (frame.Thread, val);
		}

		public override string ToString ()
		{
			return Name;
		}
	}

	public class BoolExpression : Expression
	{
		bool val;

		public BoolExpression (bool val)
		{
			this.val = val;
		}

		public override string Name {
			get { return val.ToString(); }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			resolved = true;
			return this;
		}

		protected override object DoEvaluate (ScriptingContext context)
		{
			return val;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			StackFrame frame = context.CurrentFrame;
			if ((frame.Language == null) ||
			    !frame.Language.CanCreateInstance (typeof (bool)))
				throw new ScriptingException ("Cannot instantiate value '{0}' in the current frame's language", Name);

			return frame.Language.CreateInstance (frame.Thread, val);
		}

		public override string ToString ()
		{
			return Name;
		}
	}

	public class NullExpression : Expression
	{
		public override string Name {
			get { return "null"; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			resolved = true;
			return this;
		}

		protected override object DoEvaluate (ScriptingContext context)
		{
			return IntPtr.Zero;
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			return context.CurrentLanguage.VoidType;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			throw new InvalidOperationException ();
		}

		public override string ToString ()
		{
			return Name;
		}
	}

	public class ArgumentExpression : Expression
	{
		TargetObject obj;

		public ArgumentExpression (TargetObject obj)
		{
			this.obj = obj;
			resolved = true;
		}

		public override string Name {
			get { return obj.ToString(); }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			return this;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			return obj;
		}

		public override string ToString ()
		{
			return Name;
		}
	}

	public class ThisExpression : Expression
	{
		public override string Name {
			get { return "this"; }
		}

		protected StackFrame frame;
		protected TargetVariable var;

		protected override Expression DoResolve (ScriptingContext context)
		{
			frame = context.CurrentFrame;
			Method method = frame.Method;
			if (method == null)
				throw new ScriptingException (
					"Keyword `this' not allowed: no current method.");

			if (!method.HasThis)
				throw new ScriptingException (
					"Keyword `this' not allowed: current method is " +
					"either static or unmanaged.");

			var = method.GetThis (context.CurrentThread);
			resolved = true;
			return this;
		}

		protected override TargetVariable DoEvaluateVariable (ScriptingContext context)
		{
			return var;
		}
	}

	public class BaseExpression : ThisExpression
	{
		public override string Name {
			get { return "base"; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			Expression expr = base.DoResolve (context);
			if (expr == null)
				return null;

			if (var.Type.Kind != TargetObjectKind.Class)
				throw new ScriptingException (
					"`base' is only allowed in a class.");
			if (!((TargetClassType) var.Type).HasParent)
				throw new ScriptingException (
					"Current class has no base class.");

			return expr;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			TargetVariable var = DoEvaluateVariable (context);
			if (var == null)
				return null;

			TargetObject obj = var.GetObject (context.CurrentFrame);
			if (obj == null)
				return null;

			TargetClassObject cobj = (TargetClassObject) obj;
			return cobj.GetParentObject (context.CurrentThread);
		}
	}

	public class CatchExpression : Expression
	{
		public override string Name {
			get { return "catch"; }
		}

		TargetObject exc;

		protected override Expression DoResolve (ScriptingContext context)
		{
			exc = context.CurrentFrame.ExceptionObject;
			if (exc == null)
				throw new ScriptingException ("No current exception.");

			resolved = true;
			return this;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			return exc;
		}
	}

	public abstract class TypeExpr : Expression
	{
		public abstract TargetType Type {
			get;
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			return Type;
		}

		protected override object DoEvaluate (ScriptingContext context)
		{
			return Type;
		}
	}

	public class TypeExpression : TypeExpr
	{
		TargetType type;

		public TypeExpression (TargetType type)
		{
			this.type = type;
			resolved = true;
		}

		public override string Name {
			get { return type.Name; }
		}

		public override TargetType Type {
			get { return type; }
		}

		protected override Expression DoResolveType (ScriptingContext context)
		{
			return this;
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			return this;
		}
	}

	public class VariableAccessExpression : Expression
	{
		TargetVariable var;

		public VariableAccessExpression (TargetVariable var)
		{
			this.var = var;
			resolved = true;
		}

		public override string Name {
			get { return var.Name; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			resolved = true;
			return this;
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			return var.Type;
		}

		protected override TargetVariable DoEvaluateVariable (ScriptingContext context)
		{
			return var;
		}

		protected override bool DoAssign (ScriptingContext context, TargetObject obj)
		{
			if (!var.CanWrite)
				return false;

			TargetObject new_obj = Convert.ImplicitConversionRequired (
				context, obj, var.Type);

			var.SetObject (context.CurrentFrame, new_obj);
			return true;
		}
	}

	public class SourceExpression : Expression
	{
		SourceLocation location;

		public SourceExpression (SourceLocation location)
		{
			this.location = location;
			resolved = true;
		}

		public override string Name {
			get { return location.Name; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			resolved = true;
			return this;
		}
	}

	public class SimpleNameExpression : Expression
	{
		string name;

		public SimpleNameExpression (string name)
		{
			this.name = name;
		}

		public override string Name {
			get { return name; }
		}

                public static string MakeFQN (string nsn, string name)
                {
                        if (nsn == "")
                                return name;
                        return String.Concat (nsn, ".", name);
                }

		TargetVariable GetVariableByName (StackFrame frame, string name)
		{
			TargetVariable[] locals = frame.Method.GetLocalVariables (frame.Thread);
			foreach (TargetVariable var in locals) {
				if ((var.Name == name) && var.IsInScope (frame.TargetAddress))
					return var;
			}

			TargetVariable[] param_vars = frame.Method.GetParameters (frame.Thread);
			foreach (TargetVariable var in param_vars) {
				if ((var.Name == name) && var.IsInScope (frame.TargetAddress))
					return var;
			}

			return null;
		}

		MemberExpression LookupMember (ScriptingContext context, StackFrame frame,
					       string full_name)
		{
			MemberExpression member;

			TargetFunctionType function = frame.Function;
			if (function != null) {
				member = StructAccessExpression.FindMember (
					frame.Thread, function.DeclaringType, null,
					full_name, true, true);
				if (member != null)
					return member;
			}

			Method method = frame.Method;
			if (method == null)
				return null;

			TargetStructType decl_type = method.GetDeclaringType (context.CurrentThread);
			if (decl_type == null)
				return null;

			TargetStructObject instance = null;
			if (method.HasThis) {
				TargetVariable this_var = method.GetThis (context.CurrentThread);
				instance = (TargetStructObject) this_var.GetObject (frame);
			}

			member = StructAccessExpression.FindMember (
				context.CurrentThread, decl_type, instance, full_name, true, true);
			if (member == null)
				return null;

			return member;
		}

		MemberExpression Lookup (ScriptingContext context, StackFrame frame)
		{
			MemberExpression member = LookupMember (context, frame, name);
			if (member != null)
				return member;

			string[] namespaces = context.GetNamespaces (frame);
			if (namespaces == null)
				return null;

			foreach (string ns in namespaces) {
				string full_name = MakeFQN (ns, name);
				member = LookupMember (context, frame, full_name);
				if (member != null)
					return member;
			}

			return null;
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			if (context.HasFrame) {
				StackFrame frame = context.CurrentFrame;
				if ((frame.Method != null) && frame.Method.IsLoaded) {
					TargetVariable var = GetVariableByName (frame, name);
					if (var != null)
						return new VariableAccessExpression (var);
				}

				Expression expr = Lookup (context, frame);
				if (expr != null)
					return expr;

				TargetAddress address = context.CurrentProcess.LookupSymbol (name);
				if (!address.IsNull)
					return new NumberExpression (address.Address);
			}

			SourceLocation location = context.FindMethod (name);
			if (location != null)
				return new SourceExpression (location);

			Expression texpr = DoResolveType (context);
			if (texpr != null)
				return texpr;

			throw new ScriptingException ("No symbol `{0}' in current context.", Name);
		}

		protected override MethodExpression DoResolveMethod (ScriptingContext context,
								     LocationType type)
		{
			MemberExpression member = null;
			if (type == LocationType.Constructor) {
				Expression texpr = ResolveType (context);
				if (texpr == null)
					return null;

				TargetClassType ctype = texpr.EvaluateType (context) as TargetClassType;
				if (ctype == null)
					return null;

				member = StructAccessExpression.FindMember (
					context.CurrentThread, ctype, null, ".ctor", false, true);
			} else if (context.HasFrame) {
				member = Lookup (context, context.CurrentFrame);
			}

			if (member != null)
				return member.ResolveMethod (context, type);

			return DoResolve (context) as MethodExpression;
		}

		protected override Expression DoResolveType (ScriptingContext context)
		{
			Language language = context.CurrentLanguage;
			TargetType type = language.LookupType (name);
			if (type != null)
				return new TypeExpression (type);

			string[] namespaces = context.GetNamespaces (context.CurrentFrame);
			if (namespaces == null)
				return null;

			foreach (string ns in namespaces) {
				string full_name = MakeFQN (ns, name);
				type = language.LookupType (full_name);
				if (type != null)
					return new TypeExpression (type);
			}

			return null;
		}
	}

	public class PointerTypeExpression : TypeExpr
	{
		TargetType underlying_type;
		TargetType pointer_type;
		Expression expr;

		public PointerTypeExpression (Expression expr)
		{
			this.expr = expr;
		}

		public override string Name {
			get { return expr.Name + "*"; }
		}

		public override TargetType Type {
			get {
				if (!resolved)
					throw new InvalidOperationException ();

				return pointer_type;
			}
		}

		protected bool DoResolveBase (ScriptingContext context)
		{
			if (expr is SimpleNameExpression) {
				Process process = context.CurrentThread.Process;
				Language native = process.NativeLanguage;

				underlying_type = native.LookupType (expr.Name);
			} else {
				TypeExpr te = expr.ResolveType (context) as TypeExpr;
				if (te == null)
					return false;

				underlying_type = te.Type;
			}

			if (underlying_type == null)
				return false;

			pointer_type = underlying_type.Language.CreatePointerType (underlying_type);
			if (pointer_type == null)
				throw new ScriptingException ("Can't create of pointer of type `{0}'",
							      underlying_type.Name);

			resolved = true;
			return true;
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			if (!DoResolveBase (context))
				return null;
			return this;
		}

		protected override Expression DoResolveType (ScriptingContext context)
		{
			if (!DoResolveBase (context))
				return null;
			return this;
		}
	}

	public class MemberAccessExpression : Expression
	{
		Expression left;
		string name;

		public MemberAccessExpression (Expression left, string name)
		{
			this.left = left;
			this.name = name;
		}

		public override string Name {
			get { return left.Name + "." + name; }
		}

		public MemberExpression ResolveMemberAccess (ScriptingContext context,
							     bool allow_instance)
		{
			MemberExpression member;

			Thread target = context.CurrentThread;

			Expression lexpr = left.TryResolve (context);
			if (lexpr is TypeExpr) {
				TargetClassType stype = Convert.ToClassType (((TypeExpr) lexpr).Type);

				member = StructAccessExpression.FindMember (
					target, stype, null, name, true, true);
				if (member == null)
					throw new ScriptingException (
						"Type `{0}' has no member `{1}'",
						stype.Name, name);

				if (!member.IsStatic && !allow_instance)
					throw new ScriptingException (
						"Cannot access instance member `{0}' with a type " +
						"reference.", Name);

				return member;
			}

			if (lexpr != null) {
				TargetStructObject sobj = Convert.ToStructObject (
					target, lexpr.EvaluateObject (context));
				if (sobj == null)
					throw new ScriptingException (
						"`{0}' is not a struct or class", left.Name);

				member = StructAccessExpression.FindMember (
					target, sobj.Type, sobj, name, true, true);
				if (member == null)
					throw new ScriptingException (
						"Type `{0}' has no member `{1}'",
						sobj.Type.Name, name);

				if (!member.IsInstance)
					throw new ScriptingException (
						"Cannot access static member `{0}.{1}' with an " +
						"instance reference; use a type name instead.",
						sobj.Type.Name, name);

				return member;
			}

			Expression ltype = left.TryResolveType (context);
			if (ltype != null) {
				TargetClassType stype = Convert.ToClassType (
					ltype.EvaluateType (context));

				member = StructAccessExpression.FindMember (
					target, stype, null, name, true, true);
				if (member == null)
					throw new ScriptingException (
						"Type `{0}' has no member `{1}'",
						stype.Name, name);

				if (!member.IsStatic && !allow_instance)
					throw new ScriptingException (
						"Cannot access instance member `{0}' with a type " +
						"reference.", Name);

				return member;
			}

			throw new ScriptingException (
				"No such variable or type: `{0}'", left.Name);
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			return ResolveMemberAccess (context, false);
		}

		protected override MethodExpression DoResolveMethod (ScriptingContext context,
								     LocationType type)
		{
			MemberExpression member;
			if (type == LocationType.Constructor) {
				Expression texpr = ResolveType (context);
				if (texpr == null)
					return null;

				TargetClassType ctype = texpr.EvaluateType (context) as TargetClassType;
				if (ctype == null)
					return null;

				member = StructAccessExpression.FindMember (
					context.CurrentFrame.Thread, ctype, null, ".ctor", false, true);
			} else {
				member = ResolveMemberAccess (context, true);
			}

			if (member != null)
				return member.ResolveMethod (context, type);

			return null;
		}

		protected override Expression DoResolveType (ScriptingContext context)
		{
			StackFrame frame = context.CurrentFrame;

			TargetType the_type;

			Expression ltype = left.TryResolveType (context);
			if (ltype == null)
				the_type = frame.Language.LookupType (Name);
			else {
				string nested = ltype.Name + "+" + name;
				the_type = frame.Language.LookupType (nested);
			}

			if (the_type == null)
				return null;

			return new TypeExpression (the_type);
		}
	}

	public abstract class MemberExpression : Expression
	{
		public abstract TargetStructObject InstanceObject {
			get;
		}

		public abstract bool IsInstance {
			get;
		}

		public abstract bool IsStatic {
			get;
		}
	}

	public abstract class MethodExpression : MemberExpression
	{
		protected abstract SourceLocation DoEvaluateSource (ScriptingContext context);

		public SourceLocation EvaluateSource (ScriptingContext context)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			try {
				SourceLocation location = DoEvaluateSource (context);
				if (location == null)
					throw new ScriptingException (
						"Expression `{0}' is not a method", Name);

				return location;
			} catch (LocationInvalidException ex) {
				throw new ScriptingException (
					"Location of variable `{0}' is invalid: {1}",
					Name, ex.Message);
			}
		}
	}

	public class MethodGroupExpression : MethodExpression
	{
		protected readonly TargetStructType stype;
		protected readonly TargetStructObject instance;
		protected readonly string name;
		protected readonly TargetFunctionType[] methods;
		protected readonly bool is_instance, is_static;

		public MethodGroupExpression (TargetStructType stype, TargetStructObject instance,
					      string name, TargetFunctionType[] methods,
					      bool is_instance, bool is_static)
		{
			this.stype = stype;
			this.instance = instance;
			this.name = name;
			this.methods = methods;
			this.is_instance = is_instance;
			this.is_static = is_static;
			resolved = true;
		}

		public override string Name {
			get { return stype.Name + "." + name; }
		}

		public override TargetStructObject InstanceObject {
			get { return instance; }
		}

		public override bool IsInstance {
			get { return is_instance; }
		}

		public override bool IsStatic {
			get { return is_static; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			return this;
		}

		protected override MethodExpression DoResolveMethod (ScriptingContext context,
								     LocationType type)
		{
			return this;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			throw new ScriptingException ("Expression `{0}' is a method, not a " +
						      "field or property.", Name);
		}

		protected override TargetFunctionType DoEvaluateMethod (ScriptingContext context,
									LocationType type,
									Expression[] types)
		{
			if (type != LocationType.Method)
				return null;

			if (types == null) {
				if (methods.Length == 1)
					return (TargetFunctionType) methods [0];

                                throw new ScriptingException (
                                        "Ambiguous method `{0}'; need to use full name", Name);
			}

			TargetType[] argtypes = new TargetType [types.Length];
			for (int i = 0; i < types.Length; i++)
				argtypes [i] = types [i].EvaluateType (context);

			TargetFunctionType func = OverloadResolve (context, argtypes);
			if (func != null)
				return func;

			return context.Interpreter.QueryMethod (methods);
		}

		protected override SourceLocation DoEvaluateSource (ScriptingContext context)
		{
			if (methods.Length == 1)
				return new SourceLocation ((TargetFunctionType) methods [0]);

			throw new ScriptingException (
				"Ambiguous method `{0}'; need to use full name", Name);
		}

		public TargetFunctionType OverloadResolve (ScriptingContext context,
							   TargetType[] argtypes)
		{
			ArrayList candidates = new ArrayList ();

			foreach (TargetFunctionType method in methods) {
				if (method.ParameterTypes.Length != argtypes.Length)
					continue;

				candidates.Add (method);
			}

			TargetFunctionType candidate;
			if (candidates.Count == 1) {
				candidate = (TargetFunctionType) candidates [0];
				string error;
				if (IsApplicable (context, candidate, argtypes, out error))
					return candidate;

				throw new ScriptingException (
					"The best overload of method `{0}' has some invalid " +
					"arguments:\n{1}", Name, error);
			}

			if (candidates.Count == 0)
				throw new ScriptingException (
					"No overload of method `{0}' has {1} arguments.",
					Name, argtypes.Length);

			candidate = OverloadResolve (context, argtypes, candidates);

			if (candidate == null)
				throw new ScriptingException (
					"Ambiguous method `{0}'; need to use " +
					"full name", Name);

			return candidate;
		}

		public static bool IsApplicable (ScriptingContext context, TargetFunctionType method,
						 TargetType[] types, out string error)
		{
			TargetMethodSignature sig = method.GetSignature (context.CurrentThread);

			for (int i = 0; i < types.Length; i++) {
				TargetType param_type = sig.ParameterTypes [i];

				if (param_type == types [i])
					continue;

				if (Convert.ImplicitConversionExists (context, types [i], param_type))
					continue;

				error = String.Format (
					"Argument {0}: Cannot implicitly convert `{1}' to `{2}'",
					i, types [i].Name, param_type.Name);
				return false;
			}

			error = null;
			return true;
		}

		static TargetFunctionType OverloadResolve (ScriptingContext context,
							   TargetType[] argtypes,
							   ArrayList candidates)
		{
			// Ok, no we need to find an exact match.
			TargetFunctionType match = null;
			foreach (TargetFunctionType method in candidates) {
				string error;
				if (!IsApplicable (context, method, argtypes, out error))
					continue;

				// We need to find exactly one match
				if (match != null)
					return null;

				match = method;
			}

			return match;
		}
	}

	// So you can extend this by just creating a subclass
	// of BinaryOperator that implements DoEvaluate and
	// a constructor, but you'll need to add a new rule to
	// the parser of the form
	//
	// expression: my_param_kind MY_OP_TOKEN my_param_kind 
	//             { $$ = new MyBinarySubclass ((MyParam) $1, (MyParam) $3); }
	//
	// If you want to extend on of { +, -, *, /} for non-integers,
	// like supporting "a" + "b" = "ab", then larger changes would
	// be needed.

	public class BinaryOperator : PointerExpression
	{
		public enum Kind { Mult, Plus, Minus, Div };

		protected Kind kind;
		protected Expression left, right;

		public BinaryOperator (Kind kind, Expression left, Expression right)
		{
			this.kind = kind;
			this.left = left;
			this.right = right;
		}

		public override string Name {
			get {
				string op;
				switch (kind) {
				case Kind.Mult:
					op = "*";
					break;
				case Kind.Plus:
					op = "+";
					break;
				case Kind.Minus:
					op = "-";
					break;
				case Kind.Div:
					op = "/";
					break;
				default:
					throw new InternalError ();
				}
				return left.Name + op + right.Name;
			}
		}

		protected long DoEvaluate (ScriptingContext context, long lvalue, long rvalue)
		{
			switch (kind) {
			case Kind.Mult:
				return lvalue * rvalue;
			case Kind.Plus:
				return lvalue + rvalue;
			case Kind.Minus:
				return lvalue - rvalue;
			case Kind.Div:
				return lvalue / rvalue;
			}

			throw new ScriptingException ("Unknown binary operator kind: {0}", kind);
		}

		private long GetValue (ScriptingContext context, Expression expr)
		{
			object val = expr.Evaluate (context);
			if (val is int)
				return (long) (int) val;
			else if (val is uint)
				return (long) (uint) val;
			else if (val is ulong)
				return (long) (ulong) val;
			else if (val is long)
				return (long) val;
			else if (val is TargetPointerObject) {
				TargetPointerObject pobj = (TargetPointerObject) val;
				return pobj.GetAddress (context.CurrentThread).Address;
			} else
				throw new ScriptingException ("Cannot evaluate expression `{0}'", expr);
		}

		protected override object DoEvaluate (ScriptingContext context)
		{
			long lvalue = GetValue (context, left);
			long rvalue = GetValue (context, right);

			try {
				long retval = DoEvaluate (context, lvalue, rvalue);
				return new NumberExpression (retval);
			} catch {
				throw new ScriptingException ("Cannot evaluate expression `{0}'", Name);
			}
		}

		public override TargetAddress EvaluateAddress (ScriptingContext context)
		{
			NumberExpression result = (NumberExpression) Evaluate (context);
			return result.EvaluateAddress (context);
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			left = left.Resolve (context);
			if (left == null)
				return null;

			right = right.Resolve (context);
			if (right == null)
				return null;

			resolved = true;
			return this;
		}
	}

	public class TypeOfExpression : Expression
	{
		Expression expr;

		public TypeOfExpression (Expression expr)
		{
			this.expr = expr;
		}

		public override string Name {
			get { return String.Format ("typeof ({0})", expr.Name); }
		}

		protected override Expression DoResolveType (ScriptingContext context)
		{
			return expr.ResolveType (context);
		}
		
		protected override Expression DoResolve (ScriptingContext context)
		{
			return expr.Resolve (context);
		}
	}

	public abstract class PointerExpression : Expression
	{
		public abstract TargetAddress EvaluateAddress (ScriptingContext context);
	}

	public class RegisterExpression : PointerExpression
	{
		string name;

		public RegisterExpression (string register)
		{
			this.name = register;
			resolved = true;
		}

		public override string Name {
			get { return '%' + name; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			return this;
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			return context.CurrentLanguage.PointerType;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			StackFrame frame = context.CurrentFrame;
			Register register = frame.Registers [name];
			if (register == null)
				throw new ScriptingException ("No such register `{0}'.", name);

			try {
				long value = register.Value;
				TargetAddress address = new TargetAddress (
					context.AddressDomain, value);
				return context.CurrentLanguage.CreatePointer (frame, address);
			} catch {
				throw new ScriptingException (
					"Can't access register `{0}' selected stack frame.", name);
			}
		}

		public override TargetAddress EvaluateAddress (ScriptingContext context)
		{
			TargetPointerObject pobj = (TargetPointerObject) EvaluateObject (context);
			return pobj.GetAddress (context.CurrentThread);
		}

		protected override bool DoAssign (ScriptingContext context, TargetObject tobj)
		{
			long value;

			TargetPointerObject pobj = tobj as TargetPointerObject;
			if (pobj != null) {
				TargetAddress addr = pobj.GetAddress (context.CurrentThread);
				value = addr.Address;
			} else {
				TargetFundamentalObject fobj = tobj as TargetFundamentalObject;
				if (fobj == null)
					throw new ScriptingException (
						"Cannot store non-fundamental object `{0}' in " +
						"a register", tobj.Type.Name);

				object obj = fobj.GetObject (context.CurrentThread);
				value = System.Convert.ToInt64 (obj);
			}

			Register register = context.CurrentFrame.Registers [name];
			if (register == null)
				throw new ScriptingException ("No such register `{0}'.", name);

			register.WriteRegister (context.CurrentThread, value);
			return true;
		}
	}

	public class StructAccessExpression : MemberExpression
	{
		public readonly TargetStructType Type;
		public readonly TargetMemberInfo Member;
		protected readonly TargetStructObject instance;
		TargetClass class_info;

		protected StructAccessExpression (TargetStructType type,
						  TargetStructObject instance,
						  TargetMemberInfo member)
		{
			this.Type = type;
			this.Member = member;
			this.instance = instance;
			resolved = true;
		}

		public override string Name {
			get { return Member.Name; }
		}

		public override TargetStructObject InstanceObject {
			get { return instance; }
		}

		public override bool IsInstance {
			get { return !Member.IsStatic; }
		}

		public override bool IsStatic {
			get { return Member.IsStatic; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			return this;
		}

		protected MethodGroupExpression CreateMethodGroup (TargetFunctionType func)
		{
			return new MethodGroupExpression (
				func.DeclaringType, instance, func.Name,
				new TargetFunctionType[] { func },
				!func.IsStatic, func.IsStatic);
		}

		protected void ResolveClass (Thread target)
		{
			if (class_info != null)
				return;

			class_info = Type.GetClass (target);
			if (class_info == null)
				throw new ScriptingException ("Class `{0}' not initialized yet.",
							      Type.Name);
		}

		protected override MethodExpression DoResolveMethod (ScriptingContext context,
								     LocationType type)
		{
			switch (type) {
			case LocationType.PropertyGetter:
			case LocationType.PropertySetter:
				TargetPropertyInfo property = Member as TargetPropertyInfo;
				if (property == null)
					return null;

				if (type == LocationType.PropertyGetter) {
					if (!property.CanRead)
						throw new ScriptingException (
							"Property {0} doesn't have a getter.", Name);
					return CreateMethodGroup (property.Getter);
				} else {
					if (!property.CanWrite)
						throw new ScriptingException (
							"Property {0} doesn't have a setter.", Name);
					return CreateMethodGroup (property.Setter);
				}

			case LocationType.EventAdd:
			case LocationType.EventRemove:
				TargetEventInfo ev = Member as TargetEventInfo;
				if (ev == null)
					return null;

				if (type == LocationType.EventAdd)
					return CreateMethodGroup (ev.Add);
				else
					return CreateMethodGroup (ev.Remove);

			case LocationType.Method:
			case LocationType.DelegateInvoke:
				TargetMethodInfo method = Member as TargetMethodInfo;
				if (method != null)
					return CreateMethodGroup (method.Type);

				return InvocationExpression.ResolveDelegate (context, this);

			default:
				return null;
			}
		}

		protected TargetObject GetField (Thread target, TargetFieldInfo field)
		{
			ResolveClass (target);
			return class_info.GetField (target, InstanceObject, field);
		}

		protected TargetObject GetProperty (ScriptingContext context,
						    TargetPropertyInfo prop)
		{
			RuntimeInvokeResult result = context.Interpreter.RuntimeInvoke (
				context.CurrentThread, prop.Getter, InstanceObject,
				new TargetObject [0], true, false);

			if (result.ExceptionMessage != null)
				throw new ScriptingException (
					"Invocation of `{0}' raised an exception: {1}",
					Name, result.ExceptionMessage);

			return result.ReturnObject;
		}

		protected TargetObject GetMember (ScriptingContext context, Thread target,
						  TargetMemberInfo member)
		{
			if (member is TargetPropertyInfo)
				return GetProperty (context, (TargetPropertyInfo) member);
			else if (member is TargetFieldInfo)
				return GetField (target, (TargetFieldInfo) member);
			else
				throw new ScriptingException ("Member `{0}' is of unknown type {1}",
							      Name, member.GetType ());
		}

		public static MemberExpression FindMember (Thread target, TargetStructType stype,
							   TargetStructObject instance, string name,
							   bool search_static, bool search_instance)
		{
		again:
			TargetClass klass = stype.GetClass (target);
			if (klass != null) {
				TargetMemberInfo member = klass.FindMember (
					target, name, search_static, search_instance);
				if (member != null)
					return new StructAccessExpression (stype, instance, member);

				ArrayList methods = new ArrayList ();
				bool is_instance = false;
				bool is_static = false;

				TargetMethodInfo[] klass_methods = klass.GetMethods (target);
				if (klass_methods != null) {
					foreach (TargetMethodInfo method in klass_methods) {
						if (method.IsStatic && !search_static)
							continue;
						if (!method.IsStatic && !search_instance)
							continue;
						if (method.Name != name)
							continue;

						methods.Add (method.Type);
						if (method.IsStatic)
							is_static = true;
						else
							is_instance = true;
					}
				}

				if (methods.Count > 0) {
					TargetFunctionType[] funcs = new TargetFunctionType [methods.Count];
					methods.CopyTo (funcs, 0);
					return new MethodGroupExpression (
						stype, instance, name, funcs, is_instance, is_static);
				}
			}

			TargetClassType ctype = stype as TargetClassType;
			if (ctype != null) {
				TargetMemberInfo member = ctype.FindMember (
					name, search_static, search_instance);

				if (member != null)
					return new StructAccessExpression (ctype, instance, member);

				ArrayList methods = new ArrayList ();
				bool is_instance = false;
				bool is_static = false;

				if (name == ".ctor") {
					foreach (TargetMethodInfo method in ctype.Constructors) {
						if (method.IsStatic)
							continue;
						methods.Add (method.Type);
						is_instance = true;
					}
				} else if (name == ".cctor") {
					foreach (TargetMethodInfo method in ctype.Constructors) {
						if (!method.IsStatic)
							continue;
						methods.Add (method.Type);
						is_static = true;
					}
				} else {
					foreach (TargetMethodInfo method in ctype.Methods) {
						if (method.IsStatic && !search_static)
							continue;
						if (!method.IsStatic && !search_instance)
							continue;
						if (method.Name != name)
							continue;

						methods.Add (method.Type);
						if (method.IsStatic)
							is_static = true;
						else
							is_instance = true;
					}
				}

				if (methods.Count > 0) {
					TargetFunctionType[] funcs = new TargetFunctionType [methods.Count];
					methods.CopyTo (funcs, 0);
					return new MethodGroupExpression (
						ctype, instance, name, funcs, is_instance, is_static);
				}
			}

			if (stype.HasParent) {
				stype = stype.GetParentType (target);
				if (instance != null) {
					instance = instance.GetParentObject (target);
					if (instance == null)
						return null;
				}
				goto again;
			}

			return null;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			StackFrame frame = context.CurrentFrame;

			if (!Member.IsStatic && (InstanceObject == null))
				throw new ScriptingException (
					"Instance member `{0}' cannot be used in static context.", Name);

			try {
				return GetMember (context, frame.Thread, Member);
			} catch (TargetException ex) {
				throw new ScriptingException ("Cannot access struct member `{0}': {1}",
							      Name, ex.Message);
			}
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			return Member.Type;
		}

		public TargetFunctionType ResolveDelegate (ScriptingContext context)
		{
			MethodGroupExpression mg = InvocationExpression.ResolveDelegate (
				context, this);
			if (mg == null)
				return null;

			return mg.OverloadResolve (context, null);
		}

		protected override TargetFunctionType DoEvaluateMethod (ScriptingContext context,
									LocationType type,
									Expression[] types)
		{
			switch (type) {
			case LocationType.PropertyGetter:
			case LocationType.PropertySetter:
				TargetPropertyInfo property = Member as TargetPropertyInfo;
				if (property == null)
					return null;

				if (type == LocationType.PropertyGetter) {
					if (!property.CanRead)
						throw new ScriptingException (
							"Property {0} doesn't have a getter.", Name);
					return property.Getter;
				} else {
					if (!property.CanWrite)
						throw new ScriptingException (
							"Property {0} doesn't have a setter.", Name);
					return property.Setter;
				}

			case LocationType.EventAdd:
			case LocationType.EventRemove:
				TargetEventInfo ev = Member as TargetEventInfo;
				if (ev == null)
					return null;

				if (type == LocationType.EventAdd)
					return ev.Add;
				else
					return ev.Remove;

			case LocationType.Method:
			case LocationType.DelegateInvoke:
				MethodGroupExpression mg = InvocationExpression.ResolveDelegate (
					context, this);
				if (mg == null)
					return null;

				return mg.EvaluateMethod (context, LocationType.Method, types);

			default:
				return null;
			}
		}

		protected void SetField (Thread target, TargetFieldInfo field, TargetObject obj)
		{
			ResolveClass (target);
			class_info.SetField (target, InstanceObject, field, obj);
		}

		protected void SetProperty (ScriptingContext context, TargetPropertyInfo prop,
					    TargetObject obj)
		{
			ResolveClass (context.CurrentThread);
			if (prop.Setter == null)
				throw new ScriptingException ("Property `{0}' has no setter.", Name);

			RuntimeInvokeResult result = context.Interpreter.RuntimeInvoke (
				context.CurrentThread, prop.Setter, InstanceObject,
				new TargetObject [] { obj }, true, false);

			if (result.ExceptionMessage != null)
				throw new ScriptingException (
					"Invocation of `{0}' raised an exception: {1}",
					Name, result.ExceptionMessage);
		}

		protected override bool DoAssign (ScriptingContext context, TargetObject obj)
		{
			if (Member is TargetFieldInfo) {
				if (Member.Type != obj.Type)
					throw new ScriptingException (
						"Type mismatch: cannot assign expression of type " +
						"`{0}' to field `{1}', which is of type `{2}'.",
						obj.TypeName, Name, Member.Type.Name);

				SetField (context.CurrentThread, (TargetFieldInfo) Member, obj);
			} else if (Member is TargetPropertyInfo) {
				if (Member.Type != obj.Type)
					throw new ScriptingException (
						"Type mismatch: cannot assign expression of type " +
						"`{0}' to property `{1}', which is of type `{2}'.",
						obj.TypeName, Name, Member.Type.Name);

				SetProperty (context, (TargetPropertyInfo) Member, obj);
			} else if (Member is TargetEventInfo)
				throw new ScriptingException ("Can't set events directly.");
			else if (Member is TargetMethodInfo)
				throw new ScriptingException ("Can't set methods directly.");

			return true;
		}
	}

	public class PointerDereferenceExpression : PointerExpression
	{
		Expression expr;
		string name;
		bool current_ok;

		public PointerDereferenceExpression (Expression expr, bool current_ok)
		{
			this.expr = expr;
			this.current_ok = current_ok;
			name = '*' + expr.Name;
		}

		public override string Name {
			get {
				return name;
			}
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			expr = expr.Resolve (context);
			if (expr == null)
				return null;

			resolved = true;
			return this;
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			TargetType type = expr.EvaluateType (context);

			TargetPointerType ptype = type as TargetPointerType;
			if ((ptype != null) && ptype.CanDereference)
				return ptype.StaticType;

			throw new ScriptingException (
				"Expression `{0}' is not a pointer.", expr.Name);
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			TargetObject obj = expr.EvaluateObject (context);

			TargetPointerObject pobj = obj as TargetPointerObject;
			if ((pobj != null) && pobj.Type.CanDereference) {
				TargetObject result;
				try {
					result = pobj.GetDereferencedObject (context.CurrentThread);
				} catch {
					throw new ScriptingException ("Cannot dereference `{0}'.",
								      expr.Name);
				}

				if (result != null)
					return result;
			}

			PointerExpression pexpr = expr as PointerExpression;
			if (pexpr != null) {
				TargetAddress address = pexpr.EvaluateAddress (context);
				TargetAddress result;
				try {
					result = context.CurrentThread.ReadAddress (address);
					return context.CurrentFrame.Language.CreatePointer (
						context.CurrentFrame, result);
				} catch {
					throw new ScriptingException ("Cannot dereference `{0}'.",
								      expr.Name);
				}
			}

			TargetClassObject cobj = obj as TargetClassObject;
			if (current_ok && (cobj != null))
				return cobj;

			throw new ScriptingException (
				"Expression `{0}' is not a pointer.", expr.Name);
		}

		public override TargetAddress EvaluateAddress (ScriptingContext context)
		{
			object obj = expr.Resolve (context);
			if (obj is int)
				obj = (long) (int) obj;
			if (obj is long)
				return new TargetAddress (context.AddressDomain, (long) obj);
			else if (obj is PointerDereferenceExpression)
				obj = ((Expression) obj).EvaluateObject (context);
			else if (obj is PointerExpression)
				return ((PointerExpression) obj).EvaluateAddress (context);
			else if (obj is Expression)
				obj = ((Expression) obj).EvaluateObject (context);

			TargetPointerObject pobj = obj as TargetPointerObject;
			if (pobj == null)
				throw new ScriptingException (
					"Expression `{0}' is not a pointer type.", expr.Name);

			return pobj.GetAddress (context.CurrentThread);
		}
	}

	public class AddressOfExpression : PointerExpression
	{
		Expression expr;
		string name;

		public AddressOfExpression (Expression expr)
		{
			this.expr = expr;
			name = '&' + expr.Name;
		}

		public override string Name {
			get {
				return name;
			}
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			expr = expr.Resolve (context);
			if (expr == null)
				return null;

			resolved = true;
			return this;
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			TargetPointerType ptype = expr.EvaluateType (context)
				as TargetPointerType;
			if (ptype != null)
				return ptype;

			return context.CurrentLanguage.PointerType;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			StackFrame frame = context.CurrentFrame;

			TargetAddress address = EvaluateAddress (context);

			return context.CurrentLanguage.CreatePointer (frame, address);
		}

		public override TargetAddress EvaluateAddress (ScriptingContext context)
		{
			PointerExpression pexpr = expr as PointerExpression;
			if (pexpr != null)
				return pexpr.EvaluateAddress (context);

			TargetObject obj = expr.EvaluateObject (context);
			if ((obj == null) || !obj.HasAddress)
				throw new ScriptingException (
					"Cannot take address of expression `{0}'", expr.Name);

			return obj.GetAddress (context.CurrentThread);
		}
	}

	public class ArrayAccessExpression : Expression
	{
		Expression expr;
		Expression[] indices;
		string name;

		public ArrayAccessExpression (Expression expr, Expression[] indices)
		{
			this.expr = expr;
			this.indices = indices;

			StringBuilder sb = new StringBuilder("");
			bool comma = false;
			foreach (Expression index in indices) {
				if (comma) sb.Append(",");
				sb.Append (index.ToString());
				comma = true;
			}
			name = String.Format ("{0}[{1}]", expr.Name, sb.ToString());
		}

		public override string Name {
			get {
				return name;
			}
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			int i;
			expr = expr.Resolve (context);
			if (expr == null)
				return null;

			for (i = 0; i < indices.Length; i ++) {
				indices[i] = indices[i].Resolve (context);
				if (indices[i] == null)
					return null;
			}

			resolved = true;
			return this;
		}

		int GetIntIndex (Thread target, Expression index, ScriptingContext context)
		{
			try {
				object idx = index.Evaluate (context);

				if (idx is int)
					return (int) idx;
				else if (idx is long)
					return (int) (long) idx;

				TargetFundamentalObject obj = (TargetFundamentalObject) idx;
				return (int) obj.GetObject (target);
			} catch (Exception e) {
				throw new ScriptingException (
					"Cannot convert {0} to an integer for indexing: {1}",
					index, e);
			}
		}

		int[] GetIntIndices (Thread target, ScriptingContext context)
		{
			int[] int_indices = new int [indices.Length];
			for (int i = 0; i < indices.Length; i++)
				int_indices [i] = GetIntIndex (target, indices [i], context);
			return int_indices;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			Thread target = context.CurrentThread;
			TargetObject obj = expr.EvaluateObject (context);

			// array[int]
			TargetArrayObject aobj = obj as TargetArrayObject;
			if (aobj != null) {
				int[] int_indices = GetIntIndices (target, context);
				try {
					return aobj.GetElement (target, int_indices);
				} catch (ArgumentException) {
					throw new ScriptingException (
						"Index of array expression `{0}' out of bounds.",
						expr.Name);
				}
			}

			// pointer[int]
			TargetPointerObject pobj = obj as TargetPointerObject;
			if (pobj != null) {
				// single dimensional array only at present
				int[] int_indices = GetIntIndices (target, context);
				if (int_indices.Length != 1)
					throw new ScriptingException (
						"Multi-dimensial arrays of type {0} are not yet supported",
						expr.Name);

				if (pobj.Type.IsArray)
					return pobj.GetArrayElement (target, int_indices [0]);

				throw new ScriptingException (
						       "Variable {0} is not an array type.", expr.Name);
			}

			// indexers
			TargetClassObject sobj = Convert.ToClassObject (target, obj);
			if (sobj != null) {
				ArrayList props = new ArrayList ();
				foreach (TargetPropertyInfo prop in sobj.Type.Properties) {
					if (!prop.CanRead)
						continue;

					props.Add (prop.Getter);
				}

				if (props.Count == 0)
					throw new ScriptingException (
						"Indexer `{0}' doesn't have a getter.", expr.Name);

				TargetFunctionType[] funcs = new TargetFunctionType [props.Count];
				props.CopyTo (funcs, 0);

				MethodGroupExpression mg = new MethodGroupExpression (
					sobj.Type, sobj, expr.Name + ".this", funcs, true, false);

				InvocationExpression invocation = new InvocationExpression (
					mg, indices);
				invocation.Resolve (context);

				return invocation.EvaluateObject (context);
			}

			throw new ScriptingException (
				"{0} is neither an array/pointer type, nor is it " +
				"an object with a valid indexer.", expr);
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			TargetArrayType type = expr.EvaluateType (context)
				as TargetArrayType;
			if (type == null)
				throw new ScriptingException (
					"Variable {0} is not an array type.", expr.Name);

			return type.ElementType;
		}

		protected override bool DoAssign (ScriptingContext context, TargetObject right)
		{
			Thread target = context.CurrentThread;
			TargetObject obj = expr.EvaluateObject (context);

			// array[int]
			TargetArrayObject aobj = obj as TargetArrayObject;
			if (aobj != null) {
				int[] int_indices = GetIntIndices (target, context);
				try {
					aobj.SetElement (target, int_indices, right);
				} catch (ArgumentException) {
					throw new ScriptingException (
						"Index of array expression `{0}' out of bounds.",
						expr.Name);
				}

				return true;
			}

			// indexers
			TargetClassObject sobj = Convert.ToClassObject (target, obj);
			if (sobj != null) {
				ArrayList props = new ArrayList ();
				foreach (TargetPropertyInfo prop in sobj.Type.Properties) {
					if (!prop.CanWrite)
						continue;

					props.Add (prop.Setter);
				}

				if (props.Count == 0)
					throw new ScriptingException (
						"Indexer `{0}' doesn't have a setter.", expr.Name);

				TargetFunctionType[] funcs = new TargetFunctionType [props.Count];
				props.CopyTo (funcs, 0);

				MethodGroupExpression mg = new MethodGroupExpression (
					sobj.Type, sobj, expr.Name + "[]", funcs, true, false);

				Expression[] indexargs = new Expression [indices.Length + 1];
				indices.CopyTo (indexargs, 0);
				indexargs [indices.Length] = new ArgumentExpression (right);

				InvocationExpression invocation = new InvocationExpression (
					mg, indexargs);
				invocation.Resolve (context);

				invocation.Invoke (context, false);
				return true;
			}
			
			throw new ScriptingException (
				"{0} is neither an array/pointer type, nor is it " +
				"an object with a valid indexer.", expr);
		}

	}

	public class CastExpression : Expression
	{
		Expression target, expr;
		string name;

		public CastExpression (Expression target, Expression expr)
		{
			this.target = target;
			this.expr = expr;
			this.name = String.Format ("(({0}) {1})", target.Name, expr.Name);
		}

		public override string Name {
			get {
				return name;
			}
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			target = target.ResolveType (context);
			if (target == null)
				return null;

			expr = expr.Resolve (context);
			if (expr == null)
				return null;

			resolved = true;
			return this;
		}

		static TargetStructObject TryParentCast (ScriptingContext context,
							 TargetStructObject source,
							 TargetStructType source_type,
							 TargetStructType target_type)
		{
			if (source_type == target_type)
				return source;

			if (!source_type.HasParent)
				return null;

			TargetStructType parent_type = source_type.GetParentType (context.CurrentThread);
			source = TryParentCast (context, source, parent_type, target_type);
			if (source == null)
				return null;

			return source.GetParentObject (context.CurrentThread) as TargetClassObject;
		}

		static TargetStructObject TryCurrentCast (ScriptingContext context,
							  TargetClassObject source,
							  TargetClassType target_type)
		{
			TargetStructObject current = source.GetCurrentObject (context.CurrentThread);
			if (current == null)
				return null;

			return TryParentCast (context, current, current.Type, target_type);
		}

		public static TargetObject TryCast (ScriptingContext context, TargetObject source,
						    TargetClassType target_type)
		{
			if (source.Type == target_type)
				return source;

			TargetClassObject sobj = Convert.ToClassObject (context.CurrentThread, source);
			if (sobj == null)
				return null;

			TargetStructObject result = TryParentCast (context, sobj, sobj.Type, target_type);
			if (result != null)
				return result;

			return TryCurrentCast (context, sobj, target_type);
		}

		static bool TryParentCast (ScriptingContext context,
					   TargetStructType source_type,
					   TargetStructType target_type)
		{
			if (source_type == target_type)
				return true;

			if (!source_type.HasParent)
				return false;

			TargetStructType parent_type = source_type.GetParentType (context.CurrentThread);
			return TryParentCast (context, parent_type, target_type);
		}

		public static bool TryCast (ScriptingContext context, TargetType source,
					    TargetClassType target_type)
		{
			if (source == target_type)
				return true;

			TargetClassType stype = Convert.ToClassType (source);
			if (stype == null)
				return false;

			return TryParentCast (context, stype, target_type);
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			TargetType target_type = target.EvaluateType (context);

			TargetObject obj = DoCast (context, expr, target_type);
			if (obj == null)
				throw new ScriptingException (
					"Cannot cast from `{0}' to `{1}'.", expr.Name, target.Name);

			return obj;
		}

		TargetObject DoCast (ScriptingContext context, Expression expr,
				     TargetType target_type)
		{
			if (target_type is TargetPointerType) {
				TargetAddress address;

				PointerExpression pexpr = expr as PointerExpression;
				if (pexpr != null)
					address = pexpr.EvaluateAddress (context);
				else {
					TargetPointerType ptype = expr.EvaluateType (context)
						as TargetPointerType;
					if ((ptype == null) || ptype.IsTypesafe)
						return null;

					pexpr = new AddressOfExpression (expr);
					pexpr.Resolve (context);

					address = pexpr.EvaluateAddress (context);
				}

				return ((TargetPointerType) target_type).GetObject (address);
			}

			if (target_type is TargetFundamentalType) {
				TargetFundamentalObject fobj = expr.EvaluateObject (context)
					as TargetFundamentalObject;
				if (fobj == null)
					return null;

				TargetFundamentalType ftype = target_type as TargetFundamentalType;
				return Convert.ExplicitFundamentalConversion (context, fobj, ftype);
			}

			TargetClassType ctype = Convert.ToClassType (target_type);
			TargetClassObject source = Convert.ToClassObject (
				context.CurrentThread, expr.EvaluateObject (context));

			if (source == null)
				throw new ScriptingException (
					"Variable {0} is not a class type.", expr.Name);

			return TryCast (context, source, ctype);
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			TargetObject obj = EvaluateObject (context);
			if (obj == null)
				return null;

			return obj.Type;
		}
	}


	public static class Convert
	{
		static bool ImplicitFundamentalConversionExists (FundamentalKind skind,
								 FundamentalKind tkind)
		{
			//
			// See Convert.ImplicitStandardConversionExists in MCS.
			//
			switch (skind) {
			case FundamentalKind.SByte:
				if ((tkind == FundamentalKind.Int16) ||
				    (tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Byte:
				if ((tkind == FundamentalKind.Int16) ||
				    (tkind == FundamentalKind.UInt16) ||
				    (tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.UInt32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.UInt64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Int16:
				if ((tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.UInt16:
				if ((tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.UInt32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.UInt64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Int32:
				if ((tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.UInt32:
				if ((tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.UInt64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Int64:
			case FundamentalKind.UInt64:
				if ((tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Char:
				if ((tkind == FundamentalKind.UInt16) ||
				    (tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.UInt32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.UInt64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Single:
				if (tkind == FundamentalKind.Double)
					return true;
				break;

			default:
				break;
			}

			return false;
		}

		static bool ImplicitFundamentalConversionExists (ScriptingContext context,
								 TargetFundamentalType source,
								 TargetFundamentalType target)
		{
			return ImplicitFundamentalConversionExists (
				source.FundamentalKind, target.FundamentalKind);
		}

		static object ImplicitFundamentalConversion (object value, FundamentalKind tkind)
		{
			switch (tkind) {
			case FundamentalKind.Char:
				return System.Convert.ToChar (value);
			case FundamentalKind.SByte:
				return System.Convert.ToSByte (value);
			case FundamentalKind.Byte:
				return System.Convert.ToByte (value);
			case FundamentalKind.Int16:
				return System.Convert.ToInt16 (value);
			case FundamentalKind.UInt16:
				return System.Convert.ToUInt16 (value);
			case FundamentalKind.Int32:
				return System.Convert.ToInt32 (value);
			case FundamentalKind.UInt32:
				return System.Convert.ToUInt32 (value);
			case FundamentalKind.Int64:
				return System.Convert.ToInt64 (value);
			case FundamentalKind.UInt64:
				return System.Convert.ToUInt64 (value);
			case FundamentalKind.Single:
				return System.Convert.ToSingle (value);
			case FundamentalKind.Double:
				return System.Convert.ToDouble (value);
			default:
				return null;
			}
		}

		static TargetObject ImplicitFundamentalConversion (ScriptingContext context,
								   TargetFundamentalObject obj,
								   TargetFundamentalType type)
		{
			FundamentalKind skind = obj.Type.FundamentalKind;
			FundamentalKind tkind = type.FundamentalKind;

			if (!ImplicitFundamentalConversionExists (skind, tkind))
				return null;

			object value = obj.GetObject (context.CurrentThread);

			object new_value = ImplicitFundamentalConversion (value, tkind);
			if (new_value == null)
				return null;

			return type.Language.CreateInstance (context.CurrentThread, new_value);
		}

		public static TargetObject ExplicitFundamentalConversion (ScriptingContext context,
									  TargetFundamentalObject obj,
									  TargetFundamentalType type)
		{
			TargetObject retval = ImplicitFundamentalConversion (context, obj, type);
			if (retval != null)
				return retval;

			FundamentalKind tkind = type.FundamentalKind;

			try {
				object value = obj.GetObject (context.CurrentThread);
				object new_value = ImplicitFundamentalConversion (value, tkind);
				if (new_value == null)
					return null;

				return type.Language.CreateInstance (context.CurrentThread, new_value);
			} catch {
				return null;
			}
		}

		static bool ImplicitReferenceConversionExists (ScriptingContext context,
							       TargetStructType source,
							       TargetStructType target)
		{
			if (source == target)
				return true;

			if (!source.HasParent)
				return false;

			TargetStructType parent_type = source.GetParentType (context.CurrentThread);
			return ImplicitReferenceConversionExists (context, parent_type, target);
		}

		static TargetObject ImplicitReferenceConversion (ScriptingContext context,
								 TargetClassObject obj,
								 TargetClassType type)
		{
			if (obj.Type == type)
				return obj;

			if (!obj.Type.HasParent)
				return null;

			return obj.GetParentObject (context.CurrentThread);
		}

		public static bool ImplicitConversionExists (ScriptingContext context,
							     TargetType source, TargetType target)
		{
			if (source.Equals (target))
				return true;

			if ((source is TargetFundamentalType) && (target is TargetFundamentalType))
				return ImplicitFundamentalConversionExists (
					context, (TargetFundamentalType) source,
					(TargetFundamentalType) target);

			if ((source is TargetClassType) && (target is TargetClassType))
				return ImplicitReferenceConversionExists (
					context, (TargetClassType) source,
					(TargetClassType) target);

			return false;
		}

		public static TargetObject ImplicitConversion (ScriptingContext context,
							       TargetObject obj, TargetType type)
		{
			if (obj.Type.Equals (type))
				return obj;

			if ((obj is TargetFundamentalObject) && (type is TargetFundamentalType))
				return ImplicitFundamentalConversion (
					context, (TargetFundamentalObject) obj,
					(TargetFundamentalType) type);

			if ((obj is TargetClassObject) && (type is TargetClassType))
				return ImplicitReferenceConversion (
					context, (TargetClassObject) obj,
					(TargetClassType) type);

			return null;
		}

		public static TargetObject ImplicitConversionRequired (ScriptingContext context,
								       TargetObject obj, TargetType type)
		{
			TargetObject new_obj = ImplicitConversion (context, obj, type);
			if (new_obj != null)
				return new_obj;

			throw new ScriptingException (
				"Cannot implicitly convert `{0}' to `{1}'", obj.Type.Name, type.Name);
		}

		public static TargetClassType ToClassType (TargetType type)
		{
			TargetClassType ctype = type as TargetClassType;
			if (ctype != null)
				return ctype;

			TargetObjectType otype = type as TargetObjectType;
			if (otype != null) {
				ctype = otype.ClassType;
				if (ctype != null)
					return ctype;
			}

			TargetArrayType atype = type as TargetArrayType;
			if (atype != null) {
				if (atype.Language.ArrayType != null)
					return atype.Language.ArrayType;
			}

			throw new ScriptingException (
				"Type `{0}' is not a struct or class.", type.Name);
		}

		public static TargetClassObject ToClassObject (Thread target, TargetObject obj)
		{
			TargetClassObject cobj = obj as TargetClassObject;
			if (cobj != null)
				return cobj;

			TargetObjectObject oobj = obj as TargetObjectObject;
			if (oobj != null)
				return oobj.GetClassObject (target);

			TargetArrayObject aobj = obj as TargetArrayObject;
			if ((aobj != null) && aobj.HasClassObject)
				return aobj.GetClassObject (target);

			return null;
		}

		public static TargetStructObject ToStructObject (Thread target, TargetObject obj)
		{
			TargetStructObject sobj = obj as TargetStructObject;
			if (sobj != null)
				return sobj;

			TargetObjectObject oobj = obj as TargetObjectObject;
			if (oobj != null)
				return oobj.GetClassObject (target);

			TargetArrayObject aobj = obj as TargetArrayObject;
			if ((aobj != null) && aobj.HasClassObject)
				return aobj.GetClassObject (target);

			return null;
		}
	}

	public class ConditionalExpression : Expression
	{
		Expression test;
		Expression true_expr;
		Expression false_expr;

		public override string Name {
			get { return "conditional"; }
		}

		public ConditionalExpression (Expression test, Expression true_expr, Expression false_expr)
		{
		  this.test = test;
		  this.true_expr = true_expr;
		  this.false_expr = false_expr;
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
		  this.test = this.test.Resolve (context);
		  if (this.test == null)
		    return null;

		  this.true_expr = this.true_expr.Resolve (context);
		  if (this.true_expr == null)
		    return null;

		  this.false_expr = this.false_expr.Resolve (context);
		  if (this.false_expr == null)
		    return null;

			resolved = true;
			return this;
		}

		protected override object DoEvaluate (ScriptingContext context)
		{
			bool cond;

			try {
				cond = (bool) this.test.Evaluate (context);
			}
			catch (Exception e) {
				throw new ScriptingException (
					"Cannot convert {0} to a boolean for conditional: {1}",
					this.test, e);
			}

			return cond ? true_expr.Evaluate (context) : false_expr.Evaluate (context);
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			bool cond;

			try {
				cond = (bool) this.test.Evaluate (context);
			}
			catch (Exception e) {
				throw new ScriptingException (
					"Cannot convert {0} to a boolean for conditional: {1}",
					this.test, e);
			}

			return cond ? true_expr.EvaluateObject (context) : false_expr.EvaluateObject (context);
		}
	}

	public class InvocationExpression : MethodExpression
	{
		Expression expr;
		Expression[] arguments;
		MethodGroupExpression method_expr;
		string name;

		TargetType[] argtypes;
		TargetFunctionType method;

		public InvocationExpression (Expression expr, Expression[] arguments)
		{
			this.expr = expr;
			this.arguments = arguments;

			name = String.Format ("{0} ()", expr.Name);
		}

		public override string Name {
			get { return name; }
		}

		public static MethodGroupExpression ResolveDelegate (ScriptingContext context,
								     Expression expr)
		{
			TargetClassType ctype = Convert.ToClassType (expr.EvaluateType (context));
			if (ctype == null)
				return null;

			TargetClassObject cobj;
			try {
				cobj = Convert.ToClassObject (
					context.CurrentThread, expr.EvaluateObject (context));
			} catch {
				cobj = null;
			}

			TargetClassType delegate_type = ctype.Language.DelegateType;
			if (!CastExpression.TryCast (context, ctype, delegate_type))
				return null;

			TargetFunctionType invoke = null;
			foreach (TargetMethodInfo method in ctype.Methods) {
				if (method.Name == "Invoke") {
					invoke = method.Type;
					break;
				}
			}

			if (invoke == null)
				return null;

			TargetFunctionType[] methods = new TargetFunctionType[] { invoke };

			MethodGroupExpression mg = new MethodGroupExpression (
				ctype, cobj, "Invoke", methods, true, false);
			return mg;
		}

		public override TargetStructObject InstanceObject {
			get { return method_expr.InstanceObject; }
		}

		public override bool IsInstance {
			get { return method_expr.IsInstance; }
		}

		public override bool IsStatic {
			get { return method_expr.IsStatic; }
		}

		protected override MethodExpression DoResolveMethod (ScriptingContext context,
								     LocationType type)
		{
			method_expr = (MethodGroupExpression) expr.ResolveMethod (context, type);
			if (method_expr == null)
				return null;

			resolved = true;
			return this;
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			method_expr = (MethodGroupExpression) expr.ResolveMethod (
				context, LocationType.Method);
			if (method_expr == null)
				return null;

			argtypes = new TargetType [arguments.Length];

			for (int i = 0; i < arguments.Length; i++) {
				arguments [i] = arguments [i].Resolve (context);
				if (arguments [i] == null)
					return null;

				argtypes [i] = arguments [i].EvaluateType (context);
			}

			method = method_expr.OverloadResolve (context, argtypes);

			resolved = true;
			return this;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			TargetObject retval = DoInvoke (context, false);

			if (!method.HasReturnValue)
				throw new ScriptingException (
					"Method `{0}' doesn't return a value.", Name);

			return retval;
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			return method.ReturnType;
		}

		protected override TargetFunctionType DoEvaluateMethod (ScriptingContext context,
									LocationType type,
									Expression[] types)
		{
			return method_expr.EvaluateMethod (context, type, types);
		}

		protected override SourceLocation DoEvaluateSource (ScriptingContext context)
		{
			Expression[] types = new Expression [arguments.Length];
			TargetType[] argtypes = new TargetType [types.Length];

			for (int i = 0; i < arguments.Length; i++) {
				types [i] = arguments [i].ResolveType (context);
				argtypes [i] = types [i].EvaluateType (context);
			}

			TargetFunctionType func = method_expr.OverloadResolve (context, argtypes);
			if (func != null)
				return new SourceLocation (func);

			return null;
		}

		protected TargetObject DoInvoke (ScriptingContext context, bool debug)
		{
			TargetObject[] args = new TargetObject [arguments.Length];

			for (int i = 0; i < arguments.Length; i++)
				args [i] = arguments [i].EvaluateObject (context);

			TargetMethodSignature sig = method.GetSignature (context.CurrentThread);

			TargetObject[] objs = new TargetObject [args.Length];
			for (int i = 0; i < args.Length; i++) {
				objs [i] = Convert.ImplicitConversionRequired (
					context, args [i], sig.ParameterTypes [i]);
			}

			TargetStructObject instance = method_expr.InstanceObject;

			if (!method.IsStatic && !method.IsConstructor && (instance == null))
				throw new ScriptingException (
					"Cannot invoke instance method `{0}' with a type reference.",
					method.FullName);

			try {
				Thread thread = context.CurrentThread;
				RuntimeInvokeResult result;

				result = context.Interpreter.RuntimeInvoke (
					thread, method, instance, objs, true, debug);

				if (result == null)
					throw new ScriptingException (
						"Invocation of `{0}' aborted abnormally.", Name);

				if (result.ExceptionMessage != null)
					throw new ScriptingException (
						"Invocation of `{0}' raised an exception: {1}",
						Name, result.ExceptionMessage);

				return result.ReturnObject;
			} catch (TargetException ex) {
				throw new ScriptingException (
					"Invocation of `{0}' raised an exception: {1}", Name, ex.Message);
			}
		}

		public void Invoke (ScriptingContext context, bool debug)
		{
			DoInvoke (context, debug);
		}
	}

	public class NewExpression : Expression
	{
		Expression type_expr;
		Expression[] arguments;
		string name;

		public NewExpression (Expression type_expr, Expression[] arguments)
		{
			this.type_expr = type_expr;
			this.arguments = arguments;

			name = String.Format ("new {0} ()", type_expr.Name);
		}

		public override string Name {
			get { return name; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			type_expr = type_expr.ResolveType (context);
			if (type_expr == null)
				return null;

			for (int i = 0; i < arguments.Length; i++) {
				arguments [i] = arguments [i].Resolve (context);
				if (arguments [i] == null)
					return null;
			}

			resolved = true;
			return this;
		}

		protected override TargetType DoEvaluateType (ScriptingContext context)
		{
			return type_expr.EvaluateType (context);
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			return Invoke (context);
		}

		public TargetObject Invoke (ScriptingContext context)
		{
			TargetClassType stype = Convert.ToClassType (
				type_expr.EvaluateType (context));

			TargetMethodInfo[] ctors = stype.Constructors;
			TargetFunctionType[] funcs = new TargetFunctionType [ctors.Length];
			for (int i = 0; i < ctors.Length; i++)
				funcs [i] = ctors [i].Type;

			MethodGroupExpression mg = new MethodGroupExpression (
				stype, null, ".ctor", funcs, false, true);

			InvocationExpression invocation = new InvocationExpression (mg, arguments);
			invocation.Resolve (context);

			return invocation.EvaluateObject (context);
		}
	}

	public class AssignmentExpression : Expression
	{
		Expression left, right;
		string name;

		public AssignmentExpression (Expression left, Expression right)
		{
			this.left = left;
			this.right = right;

			name = left.Name + "=" + right.Name;
		}

		public override string Name {
			get { return name; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			left = left.Resolve (context);
			if (left == null)
				return null;

			right = right.Resolve (context);
			if (right == null)
				return null;

			resolved = true;
			return this;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			TargetObject obj;
			if (right is NullExpression) {
				StackFrame frame = context.CurrentFrame;
				TargetType ltype = left.EvaluateType (context);
				obj = frame.Language.CreateNullObject (frame.Thread, ltype);
			} else
				obj = right.EvaluateObject (context);

			left.Assign (context, obj);
			return obj;
		}
	}

	public class ParentExpression : Expression
	{
		Expression expr;
		int level;
		string name;

		public ParentExpression (Expression expr, int level)
		{
			this.expr = expr;
			this.level = level;

			if (level > 0)
				name = String.Format ("$parent+{0} ({1})", level, expr.Name);
			else
				name = String.Format ("$parent ({0})", expr.Name);
		}

		public override string Name {
			get { return name; }
		}

		protected override Expression DoResolve (ScriptingContext context)
		{
			expr = expr.Resolve (context);
			if (expr == null)
				return null;

			resolved = true;
			return this;
		}

		protected override TargetObject DoEvaluateObject (ScriptingContext context)
		{
			TargetVariable var = expr.EvaluateVariable (context);
			if (var == null)
				return null;

			TargetObject obj = var.GetObject (context.CurrentFrame);
			if (obj == null)
				return null;

			TargetStructObject sobj = (TargetStructObject) obj;
			for (int i = 0; i <= level; i++) {
				sobj = sobj.GetParentObject (context.CurrentThread);
				if (sobj == null)
					return null;
			}

			return sobj;
		}
	}
}
