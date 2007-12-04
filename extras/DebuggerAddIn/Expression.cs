#if NET_2_0
using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Globalization;
using Mono.Debugger;
using Mono.Debugger.Languages;

namespace MonoDevelop.Debugger
{
	public enum LocationType
	{
		Method,
		PropertyGetter,
		PropertySetter,
		EventAdd,
		EventRemove
	}

	public abstract class Expression
	{
		public abstract string Name {
			get;
		}

		protected bool resolved;

		protected virtual ITargetType DoEvaluateType (EvaluationContext context)
		{
			return EvaluateVariable (context).TypeInfo.Type;
		}

		public ITargetType EvaluateType (EvaluationContext context)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			try {
				ITargetType type = DoEvaluateType (context);
				if (type == null)
					throw new EvaluationException (
						"Cannot get type of expression `{0}'", Name);

				return type;
			} catch (LocationInvalidException ex) {
				throw new EvaluationException (
					"Location of variable `{0}' is invalid: {1}",
					Name, ex.Message);
			}
		}

		protected virtual object DoEvaluate (EvaluationContext context)
		{
			return DoEvaluateVariable (context);
		}

		public object Evaluate (EvaluationContext context)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			object result = DoEvaluate (context);
			if (result == null)
				throw new EvaluationException (
					"Cannot evaluate expression `{0}'", Name);

			return result;
		}

		protected virtual ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			return null;
		}

		public ITargetObject EvaluateVariable (EvaluationContext context)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}' ({1})", Name,
						GetType ()));

			try {
				ITargetObject retval = DoEvaluateVariable (context);
				if (retval == null)
					throw new EvaluationException (
						"Expression `{0}' is not a variable", Name);

				return retval;
			} catch (LocationInvalidException ex) {
				throw new EvaluationException (
					"Location of variable `{0}' is invalid: {1}",
					Name, ex.Message);
			}
		}

		protected virtual SourceLocation DoEvaluateLocation (EvaluationContext context,
								     LocationType type, Expression[] types)
		{
			return null;
		}

		public SourceLocation EvaluateLocation (EvaluationContext context, LocationType type,
							Expression [] types)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			try {
				SourceLocation location = DoEvaluateLocation (context, type, types);
				if (location == null)
					throw new EvaluationException (
						"Expression `{0}' is not a method", Name);

				return location;
			} catch (LocationInvalidException ex) {
				throw new EvaluationException (
					"Location of variable `{0}' is invalid: {1}",
					Name, ex.Message);
			}
		}

		protected virtual bool DoAssign (EvaluationContext context, ITargetObject obj)
		{
			return false;
		}

		public void Assign (EvaluationContext context, ITargetObject obj)
		{
			if (!resolved)
				throw new InvalidOperationException (
					String.Format (
						"Some clown tried to evaluate the " +
						"unresolved expression `{0}'", Name));

			bool ok = DoAssign (context, obj);
			if (!ok)
				throw new EvaluationException (
					"Expression `{0}' is not an lvalue", Name);
		}

		protected virtual Expression DoResolveType (EvaluationContext context)
		{
			return null;
		}

		public Expression ResolveType (EvaluationContext context)
		{
			Expression expr = DoResolveType (context);
			if (expr == null)
				throw new EvaluationException (
					"Expression `{0}' is not a type.", Name);

			return expr;
		}

		public Expression TryResolveType (EvaluationContext context)
		{
			try {
				return DoResolveType (context);
			} catch (EvaluationException) {
				return null;
			} catch (Mono.Debugger.TargetException) {
				return null;
			}
		}

		protected abstract Expression DoResolve (EvaluationContext context);

		public Expression Resolve (EvaluationContext context)
		{
			Expression expr = DoResolve (context);
			if (expr == null)
				throw new EvaluationException (
					"Expression `{0}' is not a variable or value.", Name);

			return expr;
		}

		public Expression TryResolve (EvaluationContext context)
		{
			try {
				return DoResolve (context);
			} catch (EvaluationException) {
				return null;
			} catch (Mono.Debugger.TargetException) {
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

		public NumberExpression (long val)
		{
			this.val = val;
		}

		public long Value {
			get {
				if (val is int)
					return (long) (int) val;
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

		protected override Expression DoResolve (EvaluationContext context)
		{
			resolved = true;
			return this;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			StackFrame frame = context.CurrentFrame.Frame;
			if ((frame.Language == null) ||
			    !frame.Language.CanCreateInstance (val.GetType ()))
				return null;

			return frame.Language.CreateInstance (frame, val);
		}

		public override TargetLocation EvaluateAddress (EvaluationContext context)
		{
			TargetAddress addr = new TargetAddress (context.AddressDomain, Value);
			return new AbsoluteTargetLocation (context.CurrentFrame.Frame, addr);
		}

		protected override object DoEvaluate (EvaluationContext context)
		{
			return val;
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
			get { return val; }
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			resolved = true;
			return this;
		}

		protected override object DoEvaluate (EvaluationContext context)
		{
			return val;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			StackFrame frame = context.CurrentFrame.Frame;
			if ((frame.Language == null) ||
			    !frame.Language.CanCreateInstance (typeof (string)))
				return null;

			return frame.Language.CreateInstance (frame, val);
		}
	}

	public class ConditionalExpression : Expression
	{
		Expression test;
		Expression true_expr;
		Expression false_expr;

		public override string Name {
			get {
				return "conditional";
			}
		}
		public ConditionalExpression (Expression test, Expression true_expr, Expression false_expr)
		{
			this.test = test;
			this.true_expr = true_expr;
			this.false_expr = false_expr;
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			test = test.Resolve (context);
			true_expr = true_expr.Resolve (context);
			false_expr = false_expr.Resolve (context);

			resolved = true;
			return this;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
		  bool cond = false;

		  try {
		    cond = (bool) this.test.Evaluate (context);
		  }
		  catch (Exception e) {
		    throw new EvaluationException (
			   "Cannot convert {0} to a boolean for conditional: {1}",
			   this.test, e);
		  }

		  return cond ? true_expr.EvaluateVariable (context) : false_expr.EvaluateVariable (context);
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

		protected override Expression DoResolve (EvaluationContext context)
		{
			resolved = true;
			return this;
		}

		protected override object DoEvaluate (EvaluationContext context)
		{
			return val;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			StackFrame frame = context.CurrentFrame.Frame;
			if ((frame.Language == null) ||
			    !frame.Language.CanCreateInstance (typeof (bool)))
				return null;

			return frame.Language.CreateInstance (frame, val);
		}
	}

	public class ThisExpression : Expression
	{
		public override string Name {
			get { return "this"; }
		}

		protected FrameHandle frame;
		protected IVariable var;

		protected override Expression DoResolve (EvaluationContext context)
		{
			frame = context.CurrentFrame;
			IMethod method = frame.Frame.Method;
			if (method == null)
				throw new EvaluationException (
					"Keyword `this' not allowed: no current method.");

			if (!method.HasThis)
				throw new EvaluationException (
					"Keyword `this' not allowed: current method is " +
					"either static or unmanaged.");

			var = method.This;
			resolved = true;
			return this;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			return context.This;
		}
	}

	public class BaseExpression : ThisExpression
	{
		public override string Name {
			get { return "base"; }
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			Expression expr = base.DoResolve (context);
			if (expr == null)
				return null;

			if (var.Type.Kind != TargetObjectKind.Class)
				throw new EvaluationException (
					"`base' is only allowed in a class.");
			if (!((ITargetClassType) var.Type).HasParent)
				throw new EvaluationException (
					"Current class has no base class.");

			return expr;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			return ((ITargetClassObject) base.DoEvaluateVariable (context)).Parent;
		}
	}

	public class TypeExpression : Expression
	{
		ITargetType type;

		public TypeExpression (ITargetType type)
		{
			this.type = type;
			resolved = true;
		}

		public override string Name {
			get { return type.Name; }
		}

		protected override Expression DoResolveType (EvaluationContext context)
		{
			return this;
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			return this;
		}

		protected override ITargetType DoEvaluateType (EvaluationContext context)
		{
			return type;
		}

		protected override object DoEvaluate (EvaluationContext context)
		{
			return type;
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

		protected override Expression DoResolve (EvaluationContext context)
		{
			resolved = true;
			return this;
		}

		protected override SourceLocation DoEvaluateLocation (EvaluationContext context,
								      LocationType type, Expression[] types)
		{
			if (types != null)
				return null;

			return location;
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

		Expression LookupMember (EvaluationContext context, FrameHandle frame,
					 string full_name)
		{
			return StructAccessExpression.FindMember (
				context.This.TypeInfo.Type as ITargetStructType, frame.Frame,
				(ITargetStructObject) context.This, false, full_name);
		}

		Expression Lookup (EvaluationContext context, FrameHandle frame)
		{
			string[] namespaces = context.GetNamespaces (frame);
			if (namespaces == null)
				return null;

			foreach (string ns in namespaces) {
				string full_name = MakeFQN (ns, name);
				Expression expr = LookupMember (context, frame, full_name);
				if (expr != null)
					return expr;
			}

			return null;
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			FrameHandle frame = context.CurrentFrame;

			Expression expr = LookupMember (context, frame, name);
			if (expr != null)
				return expr;

			expr = Lookup (context, frame);
			if (expr != null)
				return expr;

// 			SourceLocation location = context.Interpreter.FindMethod (name);
// 			if (location != null)
// 				return new SourceExpression (location);

			expr = DoResolveType (context);
			if (expr != null)
				return expr;

			throw new EvaluationException ("No such type of method: `{0}'", Name);
		}

		protected override Expression DoResolveType (EvaluationContext context)
		{
			FrameHandle frame = context.CurrentFrame;
			ITargetType type = frame.Language.LookupType (frame.Frame, name);
			if (type != null)
				return new TypeExpression (type);

			string[] namespaces = context.GetNamespaces (frame);
			if (namespaces == null)
				return null;

			foreach (string ns in namespaces) {
				string full_name = MakeFQN (ns, name);
				type = frame.Language.LookupType (frame.Frame, full_name);
				if (type != null)
					return new TypeExpression (type);
			}

			return null;
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

		public Expression ResolveMemberAccess (EvaluationContext context, bool allow_instance)
		{
			StackFrame frame = context.CurrentFrame.Frame;

			Expression expr;
			Expression ltype = left.TryResolveType (context);
			if (ltype != null) {
				ITargetStructType stype = ltype.EvaluateType (context)
					as ITargetStructType;
				if (stype == null)
					throw new EvaluationException (
						"`{0}' is not a struct or class", ltype.Name);

				expr = StructAccessExpression.FindMember (
					stype, frame, null, allow_instance, name);
				if (expr == null)
					throw new EvaluationException (
						"Type `{0}' has no member `{1}'",
						stype.Name, name);

				return expr;
			}

			Expression lexpr = left.TryResolve (context);
			if (lexpr == null)
				throw new EvaluationException (
					"No such variable or type: `{0}'", left.Name);

			ITargetStructObject sobj = lexpr.EvaluateVariable (context)
				as ITargetStructObject;
			if (sobj == null)
				throw new EvaluationException (
					"`{0}' is not a struct or class", left.Name);

			expr = StructAccessExpression.FindMember (
				sobj.Type, frame, sobj, true, name);
			if (expr == null)
				throw new EvaluationException (
					"Type `{0}' has no member `{1}'",
					sobj.Type.Name, name);

			return expr;
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			return ResolveMemberAccess (context, false);
		}

		protected override Expression DoResolveType (EvaluationContext context)
		{
			StackFrame frame = context.CurrentFrame.Frame;

			ITargetType the_type;

			Expression ltype = left.TryResolveType (context);
			if (ltype == null)
				the_type = frame.Language.LookupType (frame, Name);
			else {
				string nested = ltype.Name + "+" + name;
				the_type = frame.Language.LookupType (frame, nested);
			}

			if (the_type == null)
				return null;

			return new TypeExpression (the_type);
		}
	}

	public class MethodGroupExpression : Expression
	{
		ITargetStructType stype;
		ITargetStructObject instance;
		ILanguage language;
		string name;
		ArrayList methods;

		public MethodGroupExpression (ITargetStructType stype, string name,
					      ITargetStructObject instance,
					      ILanguage language, ArrayList methods)
		{
			this.stype = stype;
			this.instance = instance;
			this.language = language;
			this.name = name;
			this.methods = methods;
			resolved = true;
		}

		public override string Name {
			get { return stype.Name + "." + name; }
		}

		public bool IsStatic {
			get { return instance == null; }
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			return this;
		}

		protected override SourceLocation DoEvaluateLocation (EvaluationContext context,
								      LocationType type, Expression[] types)
		{
			try {
				ITargetMethodInfo method = OverloadResolve (context, types);
				return new SourceLocation (method.Type.Source);
			} catch {
// 				ArrayList list = new ArrayList ();
// 				foreach (ITargetMethodInfo method in methods) {
// 					if (method.Type.Source == null)
// 						continue;
// 					list.Add (method.Type.Source);
// 				}
// 				SourceMethod[] sources = new SourceMethod [list.Count];
// 				list.CopyTo (sources, 0);
// 				throw new MultipleLocationsMatchException (sources);
			  return null;
			}
		}

		public ITargetFunctionObject EvaluateMethod (EvaluationContext context,
							     StackFrame frame,
							     Expression[] arguments)
		{
			ITargetMethodInfo method = OverloadResolve (context, arguments);

			if (method.IsStatic)
				return stype.GetStaticMethod (frame, method.Index);
			else if (!IsStatic)
				return instance.GetMethod (method.Index);
			else
				throw new EvaluationException (
					"Instance method {0} cannot be used in " +
					"static context.", Name);
		}

		protected ITargetMethodInfo OverloadResolve (EvaluationContext context,
							     Expression[] types)
		{
			ArrayList candidates = new ArrayList ();

			foreach (ITargetMethodInfo method in methods) {
				if ((types != null) &&
				    (method.Type.ParameterTypes.Length != types.Length))
					continue;

				candidates.Add (method);
			}

			if (candidates.Count == 1)
				return (ITargetMethodInfo) candidates [0];

			if (candidates.Count == 0)
				throw new EvaluationException (
					"No overload of method `{0}' has {1} arguments.",
					Name, types.Length);

			if (types == null)
				throw new EvaluationException (
					"Ambiguous method `{0}'; need to use " +
					"full name", Name);

			ITargetMethodInfo match = OverloadResolve (
				context, language, stype, types, candidates);

			if (match == null)
				throw new EvaluationException (
					"Ambiguous method `{0}'; need to use " +
					"full name", Name);

			return match;
		}

		public static ITargetMethodInfo OverloadResolve (EvaluationContext context,
								 ILanguage language,
								 ITargetStructType stype,
								 Expression[] types,
								 ArrayList candidates)
		{
			// We do a very simple overload resolution here
			ITargetType[] argtypes = new ITargetType [types.Length];
			for (int i = 0; i < types.Length; i++)
				argtypes [i] = types [i].EvaluateType (context);

			// Ok, no we need to find an exact match.
			ITargetMethodInfo match = null;
			foreach (ITargetMethodInfo method in candidates) {
				bool ok = true;
				for (int i = 0; i < types.Length; i++) {
					if (method.Type.ParameterTypes [i].TypeHandle != argtypes [i].TypeHandle) {
						ok = false;
						break;
					}
				}

				if (!ok)
					continue;

				// We need to find exactly one match
				if (match != null)
					return null;

				match = method;
			}

			return match;
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

		protected override Expression DoResolveType (EvaluationContext context)
		{
			return expr.ResolveType (context);
		}
		
		protected override Expression DoResolve (EvaluationContext context)
		{
			return expr.Resolve (context);
		}
	}

	public abstract class PointerExpression : Expression
	{
		public abstract TargetLocation EvaluateAddress (EvaluationContext context);
	}

	public class StructAccessExpression : Expression
	{
		string name;

		public readonly string Identifier;
		public readonly bool IsStatic;

		ITargetStructType Type;
		ITargetStructObject Instance;
		StackFrame Frame;

		protected StructAccessExpression (StackFrame frame, ITargetStructType type,
						  string identifier)
		{
			this.Frame = frame;
			this.Type = type;
			this.Identifier = identifier;
			this.IsStatic = true;
			resolved = true;
		}

		protected StructAccessExpression (StackFrame frame,
						  ITargetStructObject instance,
						  string identifier)
		{
			this.Frame = frame;
			this.Type = instance.Type;
			this.Instance = instance;
			this.Identifier = identifier;
			this.IsStatic = false;
			resolved = true;
		}

		public override string Name {
			get {
				return Identifier;
			}
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			return this;
		}

		protected ITargetObject GetField (ITargetStructObject sobj, ITargetFieldInfo field)
		{
			try {
				return sobj.GetField (field.Index);
			} catch (Mono.Debugger.TargetInvocationException ex) {
				throw new EvaluationException ("Can't get field {0}: {1}", Name, ex.Message);
			}
		}

		protected ITargetObject GetStaticField (ITargetStructType stype, StackFrame frame, ITargetFieldInfo field)
		{
			try {
				return stype.GetStaticField (frame, field.Index);
			} catch (Mono.Debugger.TargetInvocationException ex) {
				throw new EvaluationException ("Can't get field {0}: {1}", Name, ex.Message);
			}
		}

		protected ITargetObject GetProperty (ITargetStructObject sobj, ITargetPropertyInfo property)
		{
			try {
				return sobj.GetProperty (property.Index);
			} catch (Mono.Debugger.TargetInvocationException ex) {
				throw new EvaluationException ("Can't get property {0}: {1}", Name, ex.Message);
			}
		}

		protected ITargetObject GetStaticProperty (ITargetStructType stype, StackFrame frame, ITargetPropertyInfo property)
		{
			try {
				return stype.GetStaticProperty (frame, property.Index);
			} catch (Mono.Debugger.TargetInvocationException ex) {
				throw new EvaluationException ("Can't get property {0}: {1}", Name, ex.Message);
			}
		}

		protected ITargetObject GetEvent (ITargetStructObject sobj, ITargetEventInfo ev)
		{
			try {
				return sobj.GetEvent (ev.Index);
			} catch (Mono.Debugger.TargetInvocationException ex) {
				throw new EvaluationException ("Can't get event {0}: {1}", Name, ex.Message);
			}
		}

		protected ITargetObject GetStaticEvent (ITargetStructType stype, StackFrame frame, ITargetEventInfo ev)
		{
			try {
				return stype.GetStaticEvent (frame, ev.Index);
			} catch (Mono.Debugger.TargetInvocationException ex) {
				throw new EvaluationException ("Can't get event {0}: {1}", Name, ex.Message);
			}
		}

		protected ITargetObject GetMember (ITargetStructObject sobj, ITargetMemberInfo member)
		{
			if (member is ITargetPropertyInfo)
				return GetProperty (sobj, (ITargetPropertyInfo) member);
			else if (member is ITargetEventInfo)
				return GetEvent (sobj, (ITargetEventInfo) member);
			else
				return GetField (sobj, (ITargetFieldInfo) member);
		}

		protected ITargetObject GetStaticMember (ITargetStructType stype, StackFrame frame, ITargetMemberInfo member)
		{
			if (member is ITargetPropertyInfo)
				return GetStaticProperty (stype, frame, (ITargetPropertyInfo) member);
			else if (member is ITargetEventInfo)
				return GetStaticEvent (stype, frame, (ITargetEventInfo) member);
			else
				return GetStaticField (stype, frame, (ITargetFieldInfo) member);
		}

		public static ITargetMemberInfo FindMember (ITargetStructType stype, bool is_static, string name)
		{
			if (!is_static) {
				foreach (ITargetFieldInfo field in stype.Fields)
					if (field.Name == name)
						return field;

				foreach (ITargetPropertyInfo property in stype.Properties)
					if (property.Name == name)
						return property;

				foreach (ITargetEventInfo ev in stype.Events)
					if (ev.Name == name)
						return ev;
			}

			foreach (ITargetFieldInfo field in stype.StaticFields)
				if (field.Name == name)
					return field;

			foreach (ITargetPropertyInfo property in stype.StaticProperties)
				if (property.Name == name)
					return property;

			foreach (ITargetEventInfo ev in stype.StaticEvents)
				if (ev.Name == name)
					return ev;

			return null;
		}

		public static Expression FindMember (ITargetStructType stype, StackFrame frame,
						     ITargetStructObject instance, bool allow_instance,
						     string name)
		{
			ITargetMemberInfo member = FindMember (stype, (instance == null) && !allow_instance, name);
			if (member != null) {
				if (instance != null)
					return new StructAccessExpression (frame, instance, name);
				else
					return new StructAccessExpression (frame, stype, name);
			}

			ArrayList methods = new ArrayList ();

		again:
			if (name == ".ctor") {
				foreach (ITargetMethodInfo method in stype.Constructors) {
					methods.Add (method);
				}
			}
			else if (name == ".cctor") {
				foreach (ITargetMethodInfo method in stype.StaticConstructors) {
					methods.Add (method);
				}
			}
			else {
				if ((instance != null) || allow_instance) {
					foreach (ITargetMethodInfo method in stype.Methods) {
						if (method.Name != name)
							continue;

						methods.Add (method);
					}
				}

				foreach (ITargetMethodInfo method in stype.StaticMethods) {
					if (method.Name != name)
						continue;

					methods.Add (method);
				}
			}


			if (methods.Count > 0)
				return new MethodGroupExpression (
					stype, name, instance, frame.Language, methods);

			ITargetClassType ctype = stype as ITargetClassType;
			if ((ctype != null) && ctype.HasParent) {
				stype = ctype.ParentType;
				goto again;
			}

			return null;
		}

		protected ITargetMemberInfo FindMember (EvaluationContext context, bool report_error)
		{
			ITargetMemberInfo member = FindMember (Type, IsStatic, Identifier);
			if ((member != null) || !report_error)
				return member;

			if (IsStatic)
				throw new EvaluationException ("Type {0} has no static member {1}.", Type.Name, Identifier);
			else
				throw new EvaluationException ("Type {0} has no member {1}.", Type.Name, Identifier);
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			ITargetMemberInfo member = FindMember (context, true);

			if (member.IsStatic)
				return GetStaticMember (Type, Frame, member);
			else if (!IsStatic)
				return GetMember (Instance, member);
			else
				throw new EvaluationException ("Instance member {0} cannot be used in static context.", Name);
		}

		protected override SourceLocation DoEvaluateLocation (EvaluationContext context,
								      LocationType type, Expression[] types)
		{
			ITargetMemberInfo member = FindMember (context, true);
			if (member == null)
				return null;

			ITargetFunctionType func;

			switch (type) {
			case LocationType.PropertyGetter:
			case LocationType.PropertySetter:
				ITargetPropertyInfo property = member as ITargetPropertyInfo;
				if (property == null)
					return null;

				if (type == LocationType.PropertyGetter) {
					if (!property.CanRead)
						throw new EvaluationException (
							"Property {0} doesn't have a getter.", Name);
					func = property.Getter;
				} else {
					if (!property.CanWrite)
						throw new EvaluationException (
							"Property {0} doesn't have a setter.", Name);
					func = property.Setter;
				}

				return new SourceLocation (func.Source);
			case LocationType.EventAdd:
			case LocationType.EventRemove:
				ITargetEventInfo ev = member as ITargetEventInfo;
				if (ev == null)
					return null;

				if (type == LocationType.EventAdd) {
					func = ev.Add;
				} else {
					func = ev.Remove;
				}

				return new SourceLocation (func.Source);
			default:
				return null;
			}
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

		protected override Expression DoResolve (EvaluationContext context)
		{
			expr = expr.Resolve (context);
			if (expr == null)
				return null;

			resolved = true;
			return this;
		}

		protected override ITargetType DoEvaluateType (EvaluationContext context)
		{
			ITargetType type = expr.EvaluateType (context);

			ITargetPointerType ptype = type as ITargetPointerType;
			if (ptype != null)
				return ptype.StaticType;

			throw new EvaluationException (
				"Expression `{0}' is not a pointer.", expr.Name);
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			ITargetObject obj = expr.EvaluateVariable (context);

			ITargetPointerObject pobj = obj as ITargetPointerObject;
			if (pobj != null) {
				if (!pobj.HasDereferencedObject)
					throw new EvaluationException (
						"Cannot dereference `{0}'.", expr.Name);

				return pobj.DereferencedObject;
			}

			ITargetClassObject cobj = obj as ITargetClassObject;
			if (current_ok && (cobj != null))
				return cobj.CurrentObject;

			throw new EvaluationException (
				"Expression `{0}' is not a pointer type.", expr.Name);
		}

		public override TargetLocation EvaluateAddress (EvaluationContext context)
		{
			FrameHandle frame = context.CurrentFrame;

			object obj = expr.Resolve (context);
			if (obj is int)
				obj = (long) (int) obj;
			if (obj is long) {
				TargetAddress taddress = new TargetAddress (
					frame.Frame.AddressDomain, (long) obj);

				return new AbsoluteTargetLocation (frame.Frame, taddress);
			}

			ITargetPointerObject pobj = obj as ITargetPointerObject;
			if (pobj == null)
				throw new EvaluationException (
					"Expression `{0}' is not a pointer type.", expr.Name);

			return pobj.Location;
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

		protected override Expression DoResolve (EvaluationContext context)
		{
			expr = expr.Resolve (context);
			if (expr == null)
				return null;

			resolved = true;
			return this;
		}

		protected override ITargetType DoEvaluateType (EvaluationContext context)
		{
			FrameHandle frame = context.CurrentFrame;

			ITargetPointerType ptype = expr.EvaluateType (context)
				as ITargetPointerType;
			if (ptype != null)
				return ptype;

			return frame.Language.PointerType;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			FrameHandle frame = context.CurrentFrame;

			TargetLocation location = EvaluateAddress (context);
			return frame.Language.CreatePointer (frame.Frame, location.Address);
		}

		public override TargetLocation EvaluateAddress (EvaluationContext context)
		{
			ITargetObject obj = expr.EvaluateVariable (context);
			if (!obj.Location.HasAddress)
				throw new EvaluationException (
					"Cannot take address of expression `{0}'", expr.Name);
			return obj.Location;
		}
	}

	public class ArrayAccessExpression : Expression
	{
		Expression expr, index;
		string name;

		public ArrayAccessExpression (Expression expr, Expression index)
		{
			this.expr = expr;
			this.index = index;

			name = String.Format ("{0}[{1}]", expr.Name, index);
		}

		public override string Name {
			get {
				return name;
			}
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			expr = expr.Resolve (context);
			if (expr == null)
				return null;

			index = index.Resolve (context);
			if (index == null)
				return null;

			resolved = true;
			return this;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			int i;

			ITargetObject obj = expr.EvaluateVariable (context);

			try {
				i = (int) this.index.Evaluate (context);
			} catch (Exception e) {
				throw new EvaluationException (
					"Cannot convert {0} to an integer for indexing: {1}",
					this.index, e);
			}

			ITargetArrayObject aobj = obj as ITargetArrayObject;
			if (aobj == null) {
				ITargetPointerObject pobj = obj as ITargetPointerObject;
				if ((pobj != null) && pobj.Type.IsArray)
					return pobj.GetArrayElement (i);

				throw new EvaluationException (
							      "Variable {0} is not an array type.", expr.Name);
			}

			if ((i < aobj.LowerBound) || (i >= aobj.UpperBound)) {
				if (aobj.UpperBound == 0)
					throw new EvaluationException (
								      "Index {0} of array expression {1} out of bounds " +
								      "(array is of zero length)", i, expr.Name);
				else
					throw new EvaluationException (
								      "Index {0} of array expression {1} out of bounds " +
								      "(must be between {2} and {3}).", i, expr.Name,
								      aobj.LowerBound, aobj.UpperBound - 1);
			}

			return aobj [i];
		}

		protected override ITargetType DoEvaluateType (EvaluationContext context)
		{
			ITargetArrayType type = expr.EvaluateType (context)
				as ITargetArrayType;
			if (type == null)
				throw new EvaluationException (
					"Variable {0} is not an array type.", expr.Name);

			return type.ElementType;
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

		protected override Expression DoResolve (EvaluationContext context)
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

		static ITargetClassObject TryParentCast (EvaluationContext context,
							 ITargetClassObject source,
							 ITargetClassType source_type,
							 ITargetClassType target_type)
		{
			if (source_type == target_type)
				return source;

			if (!source_type.HasParent)
				return null;

			source = TryParentCast (
				context, source, source_type.ParentType, target_type);
			if (source == null)
				return null;

			return source.Parent;
		}

		static ITargetClassObject TryCurrentCast (EvaluationContext context,
							  ITargetClassObject source,
							  ITargetClassType source_type,
							  ITargetClassType target_type)
		{
			ITargetClassObject current = source.CurrentObject;
			if (current.Type == source_type)
				return null;

			return TryParentCast (context, current, current.Type, target_type);
		}

		static ITargetObject TryCast (EvaluationContext context, ITargetObject source,
					      ITargetClassType target_type)
		{
			if (source.TypeInfo.Type == target_type)
				return source;

			ITargetClassObject sobj = source as ITargetClassObject;
			if (sobj == null)
				return null;

			ITargetClassObject result;
			result = TryParentCast (context, sobj, sobj.Type, target_type);
			if (result != null)
				return result;

			return TryCurrentCast (context, sobj, sobj.Type, target_type);
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			ITargetClassType type = target.EvaluateType (context)
				as ITargetClassType;
			if (type == null)
				throw new EvaluationException (
					"Variable {0} is not a class type.", target.Name);

			ITargetClassObject source = expr.EvaluateVariable (context)
				as ITargetClassObject;
			if (source == null)
				throw new EvaluationException (
					"Variable {0} is not a class type.", expr.Name);

			ITargetObject obj = TryCast (context, source, type);
			if (obj == null)
				throw new EvaluationException (
					"Cannot cast from {0} to {1}.", source.Type.Name,
					type.Name);

			return obj;
		}

		protected override ITargetType DoEvaluateType (EvaluationContext context)
		{
			ITargetObject obj = EvaluateVariable (context);
			if (obj == null)
				return null;

			return obj.TypeInfo.Type;
		}
	}

	public class InvocationExpression : Expression
	{
		Expression method_expr;
		Expression[] arguments;
		MethodGroupExpression mg;
		string name;

		public InvocationExpression (Expression method_expr, Expression[] arguments)
		{
			this.method_expr = method_expr;
			this.arguments = arguments;

			name = String.Format ("{0} ()", method_expr.Name);
		}

		public override string Name {
			get { return name; }
		}

		protected override Expression DoResolve (EvaluationContext context)
		{
			method_expr = method_expr.Resolve (context);
			if (method_expr == null)
				return null;

			mg = method_expr as MethodGroupExpression;
			if (mg == null)
				throw new EvaluationException (
					"Expression `{0}' is not a method.", method_expr.Name);

			resolved = true;
			return this;
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			return Invoke (context, false);
		}

		protected override SourceLocation DoEvaluateLocation (EvaluationContext context,
								      LocationType type, Expression[] types)
		{
			Expression[] argtypes = new Expression [arguments.Length];
			for (int i = 0; i < arguments.Length; i++) {
				argtypes [i] = arguments [i].ResolveType (context);
				if (argtypes [i] == null)
					return null;
			}

			return method_expr.EvaluateLocation (context, type, argtypes);
		}

		public ITargetObject Invoke (EvaluationContext context, bool debug)
		{
			Expression[] args = new Expression [arguments.Length];
			for (int i = 0; i < arguments.Length; i++) {
				args [i] = arguments [i].Resolve (context);
				if (args [i] == null)
					return null;
			}

			ITargetFunctionObject func = mg.EvaluateMethod (
				context, context.CurrentFrame.Frame, args);

			ITargetObject[] objs = new ITargetObject [args.Length];
			for (int i = 0; i < args.Length; i++)
				objs [i] = args [i].EvaluateVariable (context);

			try {
				ITargetObject retval = func.Invoke (objs, debug);
				if (!debug && !func.Type.HasReturnValue)
					throw new EvaluationException ("Method `{0}' doesn't return a value.", Name);

				return retval;
			} catch (Mono.Debugger.TargetInvocationException ex) {
				throw new EvaluationException ("Invocation of `{0}' raised an exception: {1}", Name, ex.Message);
			}
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

		protected override Expression DoResolve (EvaluationContext context)
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

		protected override ITargetType DoEvaluateType (EvaluationContext context)
		{
			return type_expr.EvaluateType (context);
		}

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			return Invoke (context, false);
		}

		public ITargetObject Invoke (EvaluationContext context, bool debug)
		{
			FrameHandle frame = context.CurrentFrame;

			ITargetStructType stype = type_expr.EvaluateType (context)
				as ITargetStructType;
			if (stype == null)
				throw new EvaluationException (
					"Type `{0}' is not a struct or class.",
					type_expr.Name);

			ArrayList candidates = new ArrayList ();
			candidates.AddRange (stype.Constructors);

			ITargetMethodInfo method;
			if (candidates.Count == 0)
				throw new EvaluationException (
					"Type `{0}' has no public constructor.",
					type_expr.Name);
			else if (candidates.Count == 1)
				method = (ITargetMethodInfo) candidates [0];
			else
				method = MethodGroupExpression.OverloadResolve (
					context, frame.Frame.Language, stype, arguments,
					candidates);

			if (method == null)
				throw new EvaluationException (
					"Type `{0}' has no constructor which is applicable " +
					"for your list of arguments.", type_expr.Name);

			ITargetFunctionObject ctor = stype.GetConstructor (
				frame.Frame, method.Index);

			ITargetObject[] args = new ITargetObject [arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
				args [i] = arguments [i].EvaluateVariable (context);

			try {
				return ctor.Type.InvokeStatic (frame.Frame, args, debug);
			} catch (Mono.Debugger.TargetInvocationException ex) {
				throw new EvaluationException (
					"Invocation of type `{0}'s constructor raised an " +
					"exception: {1}", type_expr.Name, ex.Message);
			}
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

		protected override Expression DoResolve (EvaluationContext context)
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

		protected override ITargetObject DoEvaluateVariable (EvaluationContext context)
		{
			ITargetObject obj = right.EvaluateVariable (context);
			left.Assign (context, obj);
			return obj;
		}
	}
}
#endif
