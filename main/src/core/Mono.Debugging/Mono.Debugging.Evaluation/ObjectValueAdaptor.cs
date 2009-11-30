using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using System.Diagnostics;

namespace Mono.Debugging.Evaluation
{
	public abstract class ObjectValueAdaptor: IDisposable
	{
		static Dictionary<string, TypeDisplayData> typeDisplayData = new Dictionary<string, TypeDisplayData> ();

		// Time to wait while evaluating before switching to async mode
		public int DefaultEvaluationWaitTime = 100;

		public event EventHandler<BusyStateEventArgs> BusyStateChanged;
		
		AsyncEvaluationTracker asyncEvaluationTracker = new AsyncEvaluationTracker ();
		AsyncOperationManager asyncOperationManager = new AsyncOperationManager ();

		public ObjectValueAdaptor ()
		{
			asyncOperationManager.BusyStateChanged += delegate(object sender, BusyStateEventArgs e) {
				OnBusyStateChanged (e);
			};
			asyncEvaluationTracker.WaitTime = DefaultEvaluationWaitTime;
		}
		
		public void Dispose ()
		{
			asyncEvaluationTracker.Dispose ();
			asyncOperationManager.Dispose ();
		}

		public ObjectValue CreateObjectValue (EvaluationContext ctx, IObjectValueSource source, ObjectPath path, object obj, ObjectValueFlags flags)
		{
			try {
				return CreateObjectValueImpl (ctx, source, path, obj, flags);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
				return ObjectValue.CreateFatalError (path.LastName, ex.Message, flags);
			}
		}
		
		public virtual void OnBusyStateChanged (BusyStateEventArgs e)
		{
			if (BusyStateChanged != null)
				BusyStateChanged (this, e);
		}

		public abstract ICollectionAdaptor CreateArrayAdaptor (EvaluationContext ctx, object arr);

		public abstract bool IsNull (EvaluationContext ctx, object val);
		public abstract bool IsPrimitive (EvaluationContext ctx, object val);
		public abstract bool IsArray (EvaluationContext ctx, object val);
		public abstract bool IsEnum (EvaluationContext ctx, object val);
		public abstract bool IsClass (object type);
		public abstract object TryCast (EvaluationContext ctx, object val, object type);

		public abstract object GetValueType (EvaluationContext ctx, object val);
		public abstract string GetTypeName (EvaluationContext ctx, object val);
		public abstract object[] GetTypeArgs (EvaluationContext ctx, object type);

		public virtual bool IsClassInstance (EvaluationContext ctx, object val)
		{
			return IsClass (GetValueType (ctx, val));
		}
		
		public object GetType (EvaluationContext ctx, string name)
		{
			return GetType (ctx, name, null);
		}

		public abstract object GetType (EvaluationContext ctx, string name, object[] typeArgs);

		public virtual string GetValueTypeName (EvaluationContext ctx, object val)
		{
			return GetTypeName (ctx, GetValueType (ctx, val));
		}

		public virtual object CreateTypeObject (EvaluationContext ctx, object type)
		{
			return default (object);
		}

		public abstract object CreateValue (EvaluationContext ctx, object value);

		public abstract object CreateValue (EvaluationContext ctx, object type, params object[] args);

		public abstract object CreateNullValue (EvaluationContext ctx, object type);

		public virtual object GetBaseValue (EvaluationContext ctx, object val)
		{
			return val;
		}

		public virtual string[] GetImportedNamespaces (EvaluationContext ctx)
		{
			return new string[0];
		}

		public virtual void GetNamespaceContents (EvaluationContext ctx, string namspace, out string[] childNamespaces, out string[] childTypes)
		{
			childTypes = childNamespaces = new string[0];
		}

		protected virtual ObjectValue CreateObjectValueImpl (EvaluationContext ctx, Mono.Debugging.Backend.IObjectValueSource source, ObjectPath path, object obj, ObjectValueFlags flags)
		{
			string typeName = obj != null ? GetValueTypeName (ctx, obj) : "";
			
			if (obj == null || IsNull (ctx, obj)) {
				return ObjectValue.CreateObject (source, path, typeName, "(null)", flags, null);
			}
			else if (IsPrimitive (ctx, obj) || IsEnum (ctx,obj)) {
				return ObjectValue.CreatePrimitive (source, path, typeName, ctx.Evaluator.TargetObjectToExpression (ctx, obj), flags);
			}
			else if (IsArray (ctx, obj)) {
				return ObjectValue.CreateObject (source, path, typeName, ctx.Evaluator.TargetObjectToExpression (ctx, obj), flags, null);
			}
			else {
				TypeDisplayData tdata = GetTypeDisplayData (ctx, GetValueType (ctx, obj));
				
				string tvalue;
				if (!string.IsNullOrEmpty (tdata.ValueDisplayString) && ctx.Options.AllowDisplayStringEvaluation)
					tvalue = EvaluateDisplayString (ctx, obj, tdata.ValueDisplayString);
				else
					tvalue = ctx.Evaluator.TargetObjectToExpression (ctx, obj);
				
				string tname;
				if (!string.IsNullOrEmpty (tdata.TypeDisplayString) && ctx.Options.AllowDisplayStringEvaluation)
					tname = EvaluateDisplayString (ctx, obj, tdata.TypeDisplayString);
				else
					tname = typeName;
				
				ObjectValue oval = ObjectValue.CreateObject (source, path, tname, tvalue, flags, null);
				if (!string.IsNullOrEmpty (tdata.NameDisplayString) && ctx.Options.AllowDisplayStringEvaluation)
					oval.Name = EvaluateDisplayString (ctx, obj, tdata.NameDisplayString);
				return oval;
			}
		}

		public ObjectValue[] GetObjectValueChildren (EvaluationContext ctx, object obj, int firstItemIndex, int count)
		{
			return GetObjectValueChildren (ctx, obj, firstItemIndex, count, true);
		}

		public virtual ObjectValue[] GetObjectValueChildren (EvaluationContext ctx, object obj, int firstItemIndex, int count, bool dereferenceProxy)
		{
			if (IsArray (ctx, obj)) {
				ArrayElementGroup agroup = new ArrayElementGroup (ctx, CreateArrayAdaptor (ctx, obj));
				return agroup.GetChildren ();
			}

			if (IsPrimitive (ctx, obj))
				return new ObjectValue[0];

			// If there is a proxy, it has to show the members of the proxy
			object proxy = dereferenceProxy ? GetProxyObject (ctx, obj) : obj;

			TypeDisplayData tdata = GetTypeDisplayData (ctx, GetValueType (ctx, proxy));
			bool showRawView = tdata.IsProxyType && dereferenceProxy && ctx.Options.AllowDebuggerProxy;

			List<ObjectValue> values = new List<ObjectValue> ();
			BindingFlags access = BindingFlags.Public | BindingFlags.Instance;
			
			// Load all members to a list before creating the object values,
			// to avoid problems with objects being invalidated due to evaluations in the target,
			List<ValueReference> list = new List<ValueReference> ();
			list.AddRange (GetMembersSorted (ctx, GetValueType (ctx, proxy), proxy, access));

			foreach (ValueReference val in list) {
				try {
					DebuggerBrowsableState state = tdata.GetMemberBrowsableState (val.Name);
					if (state == DebuggerBrowsableState.Never)
						continue;

					if (state == DebuggerBrowsableState.RootHidden && dereferenceProxy) {
						object ob = val.Value;
						if (ob != null) {
							values.AddRange (GetObjectValueChildren (ctx, ob, -1, -1));
							showRawView = true;
						}
					}
					else {
						values.Add (val.CreateObjectValue (true));
					}

				}
				catch (Exception ex) {
					ctx.WriteDebuggerError (ex);
					values.Add (ObjectValue.CreateError (null, new ObjectPath (val.Name), GetTypeName (ctx, val.Type), ex.Message, val.Flags));
				}
			}

			if (showRawView) {
				values.Add (RawViewSource.CreateRawView (ctx, obj));
			}
			else {
				if (IsArray (ctx, proxy)) {
					ICollectionAdaptor col = CreateArrayAdaptor (ctx, proxy);
					ArrayElementGroup agroup = new ArrayElementGroup (ctx, col);
					ObjectValue val = ObjectValue.CreateObject (null, new ObjectPath ("Raw View"), "", "", ObjectValueFlags.ReadOnly, values.ToArray ());
					values = new List<ObjectValue> ();
					values.Add (val);
					values.AddRange (agroup.GetChildren ());
				}
				else {
					if (HasMembers (ctx, GetValueType (ctx, proxy), proxy, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
						access = BindingFlags.Static | BindingFlags.Public;
						values.Add (FilteredMembersSource.CreateNode (ctx, GetValueType (ctx, proxy), proxy, access));
					}
					if (HasMembers (ctx, GetValueType (ctx, proxy), proxy, BindingFlags.Instance | BindingFlags.NonPublic))
						values.Add (FilteredMembersSource.CreateNode (ctx, GetValueType (ctx, proxy), proxy, BindingFlags.Instance | BindingFlags.NonPublic));
				}
			}
			return values.ToArray ();
		}

		public ObjectValue[] GetExpressionValuesAsync (EvaluationContext ctx, string[] expressions)
		{
			ObjectValue[] values = new ObjectValue[expressions.Length];
			for (int n = 0; n < values.Length; n++) {
				string exp = expressions[n];
				// This is a workaround to a bug in mono 2.0. That mono version fails to compile
				// an anonymous method here
				ExpData edata = new ExpData (ctx, exp, this);
				values[n] = asyncEvaluationTracker.Run (exp, ObjectValueFlags.Literal, edata.Run);
			}
			return values;
		}
		
		class ExpData
		{
			public EvaluationContext ctx;
			public string exp;
			public ObjectValueAdaptor adaptor;
			
			public ExpData (EvaluationContext ctx, string exp, ObjectValueAdaptor adaptor)
			{
				this.ctx = ctx;
				this.exp = exp;
				this.adaptor = adaptor;
			}
			
			public ObjectValue Run ()
			{
				return adaptor.GetExpressionValue (ctx, exp);
			}
		}

		public virtual ValueReference GetIndexerReference (EvaluationContext ctx, object target, object index)
		{
			return null;
		}

		public virtual ValueReference GetLocalVariable (EvaluationContext ctx, string name)
		{
			foreach (ValueReference var in GetLocalVariables (ctx)) {
				if (var.Name == name)
					return var;
			}
			return null;
		}

		public virtual ValueReference GetParameter (EvaluationContext ctx, string name)
		{
			foreach (ValueReference var in GetParameters (ctx)) {
				if (var.Name == name)
					return var;
			}
			return null;
		}

		public virtual IEnumerable<ValueReference> GetLocalVariables (EvaluationContext ctx)
		{
			yield break;
		}

		public virtual IEnumerable<ValueReference> GetParameters (EvaluationContext ctx)
		{
			yield break;
		}

		public virtual ValueReference GetThisReference (EvaluationContext ctx)
		{
			return null;
		}

		public virtual object GetEnclosingType (EvaluationContext ctx)
		{
			return null;
		}

		public virtual CompletionData GetExpressionCompletionData (EvaluationContext ctx, string exp)
		{
			int i;
			if (exp.Length == 0)
				return null;

			if (exp [exp.Length - 1] == '.') {
				exp = exp.Substring (0, exp.Length - 1);
				i = 0;
				while (i < exp.Length) {
					ValueReference vr = null;
					try {
						vr = ctx.Evaluator.Evaluate (ctx, exp.Substring (i), null);
						if (vr != null) {
							CompletionData data = new CompletionData ();
							foreach (ValueReference cv in vr.GetChildReferences ())
								data.Items.Add (new CompletionItem (cv.Name, cv.Flags));
							data.ExpressionLenght = 0;
							return data;
						}
					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
					i++;
				}
				return null;
			}
			
			i = exp.Length - 1;
			bool lastWastLetter = false;
			while (i >= 0) {
				char c = exp [i--];
				if (!char.IsLetterOrDigit (c) && c != '_')
					break;
				lastWastLetter = !char.IsDigit (c);
			}
			if (lastWastLetter) {
				string partialWord = exp.Substring (i+1);
				
				CompletionData data = new CompletionData ();
				data.ExpressionLenght = partialWord.Length;
				
				// Local variables
				
				foreach (ValueReference vc in ctx.Adapter.GetLocalVariables (ctx))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Parameters
				
				foreach (ValueReference vc in ctx.Adapter.GetParameters (ctx))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Members
				
				ValueReference thisobj = ctx.Adapter.GetThisReference (ctx);
				
				if (thisobj != null)
					data.Items.Add (new CompletionItem ("this", ObjectValueFlags.Field | ObjectValueFlags.ReadOnly));

				object type = ctx.Adapter.GetEnclosingType (ctx);
				
				foreach (ValueReference vc in ctx.Adapter.GetMembers (ctx, type, thisobj != null ? thisobj.Value : null))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				if (data.Items.Count > 0)
					return data;
			}
			return null;
		}
		
		public IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, object t, object co)
		{
			return GetMembers (ctx, t, co, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		}

		public ValueReference GetMember (EvaluationContext ctx, object co, string name)
		{
			return GetMember (ctx, GetValueType (ctx, co), co, name);
		}

		public virtual ValueReference GetMember (EvaluationContext ctx, object t, object co, string name)
		{
			foreach (ValueReference var in GetMembers (ctx, t, co))
				if (var.Name == name)
					return var;
			return null;
		}

		internal IEnumerable<ValueReference> GetMembersSorted (EvaluationContext ctx, object t, object co)
		{
			return GetMembersSorted (ctx, t, co, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		}
		
		internal IEnumerable<ValueReference> GetMembersSorted (EvaluationContext ctx, object t, object co, BindingFlags bindingFlags)
		{
			List<ValueReference> list = new List<ValueReference> ();
			list.AddRange (GetMembers (ctx, t, co, bindingFlags));
			list.Sort (delegate (ValueReference v1, ValueReference v2) {
				return v1.Name.CompareTo (v2.Name);
			});
			return list;
		}
		
		public bool HasMembers (EvaluationContext ctx, object t, object co, BindingFlags bindingFlags)
		{
			return GetMembers (ctx, t, co, bindingFlags).Any ();
		}
		
		public abstract IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, object t, object co, BindingFlags bindingFlags);

		public virtual IEnumerable<object> GetNestedTypes (EvaluationContext ctx, object type)
		{
			yield break;
		}
		
		public virtual object TargetObjectToObject (EvaluationContext ctx, object obj)
		{
			if (IsNull (ctx, obj)) {
				return null;
			} else if (IsArray (ctx, obj)) {
				ICollectionAdaptor adaptor = CreateArrayAdaptor (ctx, obj);
				StringBuilder tn = new StringBuilder (GetTypeName (ctx, adaptor.ElementType));
				int[] dims = adaptor.GetDimensions ();
				tn.Append ("[");
				for (int n=0; n<dims.Length; n++) {
					if (n>0)
						tn.Append (',');
					tn.Append (dims[n]);
				}
				tn.Append ("]");
				return new LiteralExp (tn.ToString ());
			}
			else if (IsClassInstance (ctx, obj)) {
				TypeDisplayData tdata = GetTypeDisplayData (ctx, GetValueType (ctx, obj));
				if (!string.IsNullOrEmpty (tdata.ValueDisplayString) && ctx.Options.AllowDisplayStringEvaluation)
					return new LiteralExp (EvaluateDisplayString (ctx, obj, tdata.ValueDisplayString));
				// Return the type name
				if (ctx.Options.AllowToStringCalls)
					return new LiteralExp ("{" + CallToString (ctx, obj) + "}");
				if (!string.IsNullOrEmpty (tdata.TypeDisplayString) && ctx.Options.AllowDisplayStringEvaluation)
					return new LiteralExp ("{" + EvaluateDisplayString (ctx, obj, tdata.TypeDisplayString) + "}");
				return new LiteralExp ("{" + GetValueTypeName (ctx, obj) + "}");
			}
			return new LiteralExp ("{" + CallToString (ctx, obj) + "}");
		}

		public virtual object Cast (EvaluationContext ctx, object obj, object targetType)
		{
			return obj;
		}

		public virtual string CallToString (EvaluationContext ctx, object obj)
		{
			return GetValueTypeName (ctx, obj);
		}

		public object GetProxyObject (EvaluationContext ctx, object obj)
		{
			TypeDisplayData data = GetTypeDisplayData (ctx, GetValueType (ctx, obj));
			if (string.IsNullOrEmpty (data.ProxyType) || !ctx.Options.AllowDebuggerProxy)
				return obj;

			object[] typeArgs = null;

			int i = data.ProxyType.IndexOf ('`');
			if (i != -1) {
				int np = int.Parse (data.ProxyType.Substring (i + 1));
				typeArgs = GetTypeArgs (ctx, GetValueType (ctx, obj));
				if (typeArgs.Length != np)
					return obj;
			}
			
			object ttype = GetType (ctx, data.ProxyType, typeArgs);
			if (ttype == null) {
				i = data.ProxyType.IndexOf (',');
				if (i != -1)
					ttype = GetType (ctx, data.ProxyType.Substring (0, i).Trim (), typeArgs);
			}
			if (ttype == null)
				throw new EvaluatorException ("Unknown type '{0}'", data.ProxyType);

			try {
				object val = CreateValue (ctx, ttype, obj);
				return val ?? obj;
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				return obj;
			}
		}

		public TypeDisplayData GetTypeDisplayData (EvaluationContext ctx, object type)
		{
			if (!IsClass (type))
				return TypeDisplayData.Default;

			TypeDisplayData td;
			string tname = GetTypeName (ctx, type);
			if (typeDisplayData.TryGetValue (tname, out td))
				return td;

			try {
				td = OnGetTypeDisplayData (ctx, type);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
			}
			if (td == null)
				typeDisplayData[tname] = td = TypeDisplayData.Default;
			else
				typeDisplayData[tname] = td;
			return td;
		}

		protected virtual TypeDisplayData OnGetTypeDisplayData (EvaluationContext ctx, object type)
		{
			return null;
		}

		public string EvaluateDisplayString (EvaluationContext ctx, object obj, string exp)
		{
			StringBuilder sb = new StringBuilder ();
			int last = 0;
			int i = exp.IndexOf ("{");
			while (i != -1 && i < exp.Length) {
				sb.Append (exp.Substring (last, i - last));
				i++;
				int j = exp.IndexOf ("}", i);
				if (j == -1)
					return exp;
				string mem = exp.Substring (i, j - i).Trim ();
				if (mem.Length == 0)
					return exp;

				ValueReference member = GetMember (ctx, GetValueType (ctx, obj), obj, mem);
				if (member != null) {
					object val = member.Value;
					sb.Append (ctx.Evaluator.TargetObjectToString (ctx, val));
				} else {
					sb.Append ("{Unknown member '" + mem + "'}");
				}
				last = j + 1;
				i = exp.IndexOf ("{", last);
			}
			sb.Append (exp.Substring (last));
			return sb.ToString ();
		}

		public void AsyncExecute (AsyncOperation operation, int timeout)
		{
			asyncOperationManager.Invoke (operation, timeout);
		}

		public ObjectValue CreateObjectValueAsync (string name, ObjectValueFlags flags, ObjectEvaluatorDelegate evaluator)
		{
			return asyncEvaluationTracker.Run (name, flags, evaluator);
		}
		
		public bool IsEvaluating {
			get { return asyncEvaluationTracker.IsEvaluating; }
		}

		public void CancelAsyncOperations ( )
		{
			asyncEvaluationTracker.Stop ();
			asyncOperationManager.AbortAll ();
			asyncEvaluationTracker.WaitForStopped ();
		}

		public ObjectValue GetExpressionValue (EvaluationContext ctx, string exp)
		{
			try {
				ValueReference var = ctx.Evaluator.Evaluate (ctx, exp);
				if (var != null) {
					return var.CreateObjectValue ();
				}
				else
					return ObjectValue.CreateUnknown (exp);
			}
			catch (NotSupportedExpressionException) {
				return ObjectValue.CreateImplicitNotSupported (ctx.ExpressionValueSource, new ObjectPath (exp), "", ObjectValueFlags.None);
			}
			catch (EvaluatorException ex) {
				return ObjectValue.CreateError (ctx.ExpressionValueSource, new ObjectPath (exp), "", ex.Message, ObjectValueFlags.None);
			}
			catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				return ObjectValue.CreateUnknown (exp);
			}
		}

		public bool HasMethod (EvaluationContext ctx, object targetType, string methodName)
		{
			return HasMethod (ctx, targetType, methodName, null, BindingFlags.Instance | BindingFlags.Static);
		}
		
		public bool HasMethod (EvaluationContext ctx, object targetType, string methodName, BindingFlags flags)
		{
			return HasMethod (ctx, targetType, methodName, null, flags);
		}
		
		// argTypes can be null, meaning that it has to return true if there is any method with that name
		// flags will only contain Static or Instance flags
		public virtual bool HasMethod (EvaluationContext ctx, object targetType, string methodName, object[] argTypes, BindingFlags flags)
		{
			return false;
		}
		
		public virtual object RuntimeInvoke (EvaluationContext ctx, object targetType, object target, string methodName, object[] argTypes, object[] argValues)
		{
			return null;
		}
	}

	public class TypeDisplayData
	{
		public string ProxyType;
		public string ValueDisplayString;
		public string TypeDisplayString;
		public string NameDisplayString;
		
		public bool IsProxyType {
			get { return ProxyType != null; }
		}

		public static readonly TypeDisplayData Default = new TypeDisplayData ();

		public Dictionary<string, DebuggerBrowsableState> MemberData;

		public DebuggerBrowsableState GetMemberBrowsableState (string name)
		{
			if (MemberData == null)
				return DebuggerBrowsableState.Collapsed;

			DebuggerBrowsableState state;
			if (MemberData.TryGetValue (name, out state))
				return state;
			else
				return DebuggerBrowsableState.Collapsed;
		}
	}
}
