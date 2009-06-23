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
	public abstract class ObjectValueAdaptor<TValue, TType> where TValue:class where TType: class
	{
		static Dictionary<string, TypeDisplayData> typeDisplayData = new Dictionary<string, TypeDisplayData> ();

		public int DefaultAsyncSwitchTimeout = 60;
		public int DefaultEvaluationTimeout = 1000;
		public int DefaultChildEvaluationTimeout = 5000;

		AsyncEvaluationTracker asyncEvaluationTracker = new AsyncEvaluationTracker ();
		AsyncOperationManager asyncOperationManager = new AsyncOperationManager ();

//		public abstract TValue GetRealObject (EvaluationContext<TValue, TType> ctx, TValue obj);

		public ObjectValue CreateObjectValue (EvaluationContext<TValue, TType> ctx, IObjectValueSource source, ObjectPath path, TValue obj, ObjectValueFlags flags)
		{
			try {
				return CreateObjectValueImpl (ctx, source, path, obj, flags);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
				return ObjectValue.CreateError (path.LastName, ex.Message, flags);
			}
		}

		public abstract ICollectionAdaptor<TValue,TType> CreateArrayAdaptor (EvaluationContext<TValue, TType> ctx, TValue arr);

		public abstract bool IsNull (EvaluationContext<TValue, TType> ctx, TValue val);
		public abstract bool IsPrimitive (EvaluationContext<TValue, TType> ctx, TValue val);
		public abstract bool IsClassInstance (EvaluationContext<TValue, TType> ctx, TValue val);
		public abstract bool IsArray (EvaluationContext<TValue, TType> ctx, TValue val);
		public abstract bool IsClass (TType type);
		public abstract TValue TryCast (EvaluationContext<TValue, TType> ctx, TValue val, TType type);

		public abstract TType GetValueType (EvaluationContext<TValue, TType> ctx, TValue val);
		public abstract string GetTypeName (EvaluationContext<TValue, TType> ctx, TType val);
		public abstract TType[] GetTypeArgs (EvaluationContext<TValue, TType> ctx, TType type);

		public TType GetType (EvaluationContext<TValue, TType> ctx, string name)
		{
			return GetType (ctx, name, null);
		}

		public abstract TType GetType (EvaluationContext<TValue, TType> ctx, string name, TType[] typeArgs);

		public virtual string GetValueTypeName (EvaluationContext<TValue, TType> ctx, TValue val)
		{
			return GetTypeName (ctx, GetValueType (ctx, val));
		}

		public virtual TValue CreateTypeObject (EvaluationContext<TValue, TType> ctx, TType type)
		{
			return default (TValue);
		}

		public abstract TValue CreateValue (EvaluationContext<TValue, TType> ctx, object value);

		public abstract TValue CreateValue (EvaluationContext<TValue, TType> ctx, TType type, params TValue[] args);

		public abstract TValue CreateNullValue (EvaluationContext<TValue, TType> ctx, TType type);

		public virtual TValue GetBaseValue (EvaluationContext<TValue, TType> ctx, TValue val)
		{
			return val;
		}

		public virtual string[] GetImportedNamespaces (EvaluationContext<TValue, TType> ctx)
		{
			return new string[0];
		}

		public virtual void GetNamespaceContents (EvaluationContext<TValue, TType> ctx, string namspace, out string[] childNamespaces, out string[] childTypes)
		{
			childTypes = childNamespaces = new string[0];
		}

		protected abstract ObjectValue CreateObjectValueImpl (EvaluationContext<TValue, TType> ctx, IObjectValueSource source, ObjectPath path, TValue obj, ObjectValueFlags flags);

		public ObjectValue[] GetObjectValueChildren (EvaluationContext<TValue, TType> ctx, TValue obj, int firstItemIndex, int count)
		{
			return GetObjectValueChildren (ctx, obj, firstItemIndex, count, true);
		}

		public virtual ObjectValue[] GetObjectValueChildren (EvaluationContext<TValue, TType> ctx, TValue obj, int firstItemIndex, int count, bool dereferenceProxy)
		{
			if (IsArray (ctx, obj)) {
				ArrayElementGroup<TValue, TType> agroup = new ArrayElementGroup<TValue, TType> (ctx, CreateArrayAdaptor (ctx, obj));
				return agroup.GetChildren ();
			}

			if (IsPrimitive (ctx, obj))
				return new ObjectValue[0];

			// If there is a proxy, it has to show the members of the proxy
			TValue proxy = dereferenceProxy ? GetProxyObject (ctx, obj) : obj;

			TypeDisplayData tdata = GetTypeDisplayData (ctx, GetValueType (ctx, proxy));
			bool showRawView = tdata.IsProxyType && dereferenceProxy;

			List<ObjectValue> values = new List<ObjectValue> ();
			BindingFlags access = BindingFlags.Public | BindingFlags.Instance;

			// Load all members to a list before creating the object values,
			// to avoid problems with objects being invalidated due to evaluations in the target,
			List<ValueReference<TValue, TType>> list = new List<ValueReference<TValue, TType>> ();
			list.AddRange (GetMembers (ctx, GetValueType (ctx, proxy), proxy, access));

			foreach (ValueReference<TValue, TType> val in list) {
				try {
					DebuggerBrowsableState state = tdata.GetMemberBrowsableState (val.Name);
					if (state == DebuggerBrowsableState.Never)
						continue;

					if (state == DebuggerBrowsableState.RootHidden && dereferenceProxy) {
						TValue ob = val.Value;
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
				values.Add (RawViewSource<TValue, TType>.CreateRawView (ctx, obj));
			}
			else {
				ICollectionAdaptor<TValue, TType> col = CreateArrayAdaptor (ctx, proxy);
				if (col != null) {
					ArrayElementGroup<TValue, TType> agroup = new ArrayElementGroup<TValue, TType> (ctx, col);
					ObjectValue val = ObjectValue.CreateObject (null, new ObjectPath ("Raw View"), "", "", ObjectValueFlags.ReadOnly, values.ToArray ());
					values = new List<ObjectValue> ();
					values.Add (val);
					values.AddRange (agroup.GetChildren ());
				}
				else {
					values.Add (FilteredMembersSource<TValue, TType>.CreateNode (ctx, GetValueType (ctx, proxy), proxy, BindingFlags.Static | BindingFlags.Public));
					values.Add (FilteredMembersSource<TValue, TType>.CreateNode (ctx, GetValueType (ctx, proxy), proxy, BindingFlags.Instance | BindingFlags.NonPublic));
				}
			}
			return values.ToArray ();
		}

		public ObjectValue[] GetExpressionValuesAsync (EvaluationContext<TValue, TType> ctx, string[] expressions, bool evaluateMethods, int timeout)
		{
			ObjectValue[] values = new ObjectValue[expressions.Length];
			for (int n = 0; n < values.Length; n++) {
				string exp = expressions[n];
				// This is a workaround to a bug in mono 2.0. That mono version fails to compile
				// an anonymous method here
				ExpData edata = new ExpData (ctx, exp, evaluateMethods, this);
				values[n] = asyncEvaluationTracker.Run (exp, ObjectValueFlags.Literal, edata.Run);
			}
			return values;
		}
		
		class ExpData
		{
			public EvaluationContext<TValue, TType> ctx;
			public string exp;
			public bool evaluateMethods;
			public ObjectValueAdaptor<TValue, TType> adaptor;
			
			public ExpData (EvaluationContext<TValue, TType> ctx, string exp, bool evaluateMethods, ObjectValueAdaptor<TValue, TType> adaptor)
			{
				this.ctx = ctx;
				this.exp = exp;
				this.evaluateMethods = evaluateMethods;
				this.adaptor = adaptor;
			}
			
			public ObjectValue Run ()
			{
				return adaptor.GetExpressionValue (ctx, exp, evaluateMethods);
			}
		}

		public virtual ValueReference<TValue, TType> GetIndexerReference (EvaluationContext<TValue, TType> ctx, TValue target, TValue index)
		{
			return null;
		}

		public virtual ValueReference<TValue, TType> GetLocalVariable (EvaluationContext<TValue, TType> ctx, string name)
		{
			foreach (ValueReference<TValue, TType> var in GetLocalVariables (ctx)) {
				if (var.Name == name)
					return var;
			}
			return null;
		}

		public virtual ValueReference<TValue, TType> GetParameter (EvaluationContext<TValue, TType> ctx, string name)
		{
			foreach (ValueReference<TValue, TType> var in GetParameters (ctx)) {
				if (var.Name == name)
					return var;
			}
			return null;
		}

		public virtual IEnumerable<ValueReference<TValue, TType>> GetLocalVariables (EvaluationContext<TValue, TType> ctx)
		{
			yield break;
		}

		public virtual IEnumerable<ValueReference<TValue, TType>> GetParameters (EvaluationContext<TValue, TType> ctx)
		{
			yield break;
		}

		public virtual ValueReference<TValue, TType> GetThisReference (EvaluationContext<TValue, TType> ctx)
		{
			return null;
		}

		public virtual TType GetEnclosingType (EvaluationContext<TValue, TType> ctx)
		{
			return null;
		}

		public virtual CompletionData GetExpressionCompletionData (EvaluationContext<TValue, TType> ctx, string exp)
		{
			int i;

			if (exp [exp.Length - 1] == '.') {
				exp = exp.Substring (0, exp.Length - 1);
				i = 0;
				while (i < exp.Length) {
					ValueReference<TValue,TType> vr = null;
					try {
						vr = ctx.Evaluator.Evaluate (ctx, exp.Substring (i), null);
						if (vr != null) {
							CompletionData data = new CompletionData ();
							foreach (ValueReference<TValue,TType> cv in vr.GetChildReferences ())
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
				
				foreach (ValueReference<TValue,TType> vc in ctx.Adapter.GetLocalVariables (ctx))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Parameters
				
				foreach (ValueReference<TValue,TType> vc in ctx.Adapter.GetParameters (ctx))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				// Members
				
				ValueReference<TValue,TType> thisobj = ctx.Adapter.GetThisReference (ctx);
				
				if (thisobj != null)
					data.Items.Add (new CompletionItem ("this", ObjectValueFlags.Field | ObjectValueFlags.ReadOnly));

				TType type = ctx.Adapter.GetEnclosingType (ctx);
				
				foreach (ValueReference<TValue,TType> vc in ctx.Adapter.GetMembers (ctx, type, thisobj != null ? thisobj.Value : null))
					if (vc.Name.StartsWith (partialWord))
						data.Items.Add (new CompletionItem (vc.Name, vc.Flags));
				
				if (data.Items.Count > 0)
					return data;
			}
			return null;
		}
		
		public IEnumerable<ValueReference<TValue, TType>> GetMembers (EvaluationContext<TValue, TType> ctx, TType t, TValue co)
		{
			return GetMembers (ctx, t, co, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		}

		public ValueReference<TValue, TType> GetMember (EvaluationContext<TValue, TType> ctx, TValue co, string name)
		{
			return GetMember (ctx, GetValueType (ctx, co), co, name);
		}

		public virtual ValueReference<TValue, TType> GetMember (EvaluationContext<TValue, TType> ctx, TType t, TValue co, string name)
		{
			foreach (ValueReference<TValue, TType> var in GetMembers (ctx, t, co))
				if (var.Name == name)
					return var;
			return null;
		}

		public abstract IEnumerable<ValueReference<TValue, TType>> GetMembers (EvaluationContext<TValue, TType> ctx, TType t, TValue co, BindingFlags bindingFlags);

		public abstract object TargetObjectToObject (EvaluationContext<TValue, TType> ctx, TValue obj);

		public virtual TValue Cast (EvaluationContext<TValue, TType> ctx, TValue obj, TType targetType)
		{
			return obj;
		}

		public virtual string CallToString (EvaluationContext<TValue, TType> ctx, TValue obj)
		{
			return "";
		}

		public TValue GetProxyObject (EvaluationContext<TValue, TType> ctx, TValue obj)
		{
			TypeDisplayData data = GetTypeDisplayData (ctx, GetValueType (ctx, obj));
			if (string.IsNullOrEmpty (data.ProxyType))
				return obj;

			TType[] typeArgs = null;

			int i = data.ProxyType.IndexOf ('`');
			if (i != -1) {
				int np = int.Parse (data.ProxyType.Substring (i + 1));
				typeArgs = GetTypeArgs (ctx, GetValueType (ctx, obj));
				if (typeArgs.Length != np)
					return obj;
			}
			
			TType ttype = GetType (ctx, data.ProxyType, typeArgs);
			if (ttype == null) {
				i = data.ProxyType.IndexOf (',');
				if (i != -1)
					ttype = GetType (ctx, data.ProxyType.Substring (0, i).Trim (), typeArgs);
			}
			if (ttype == null)
				throw new EvaluatorException ("Unknown type '{0}'", data.ProxyType);

			TValue val = CreateValue (ctx, ttype, obj);
			return val ?? obj;
		}

		public TypeDisplayData GetTypeDisplayData (EvaluationContext<TValue, TType> ctx, TType type)
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

		protected virtual TypeDisplayData OnGetTypeDisplayData (EvaluationContext<TValue, TType> ctx, TType type)
		{
			return null;
		}

		public string EvaluateDisplayString (EvaluationContext<TValue, TType> ctx, TValue obj, string exp)
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

				ValueReference<TValue, TType> member = GetMember (ctx, GetValueType (ctx, obj), obj, mem);
				TValue val = member.Value;
				sb.Append (ctx.Evaluator.TargetObjectToString (ctx, val));
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

		public void CancelAsyncOperations ( )
		{
			asyncEvaluationTracker.Stop ();
			asyncOperationManager.AbortAll ();
			asyncEvaluationTracker.WaitForStopped ();
		}

		ObjectValue GetExpressionValue (EvaluationContext<TValue, TType> ctx, string exp, bool evaluateMethods)
		{
			try {
				EvaluationOptions<TType> ops = new EvaluationOptions<TType> ();
				ops.CanEvaluateMethods = evaluateMethods;
				ValueReference<TValue, TType> var = ctx.Evaluator.Evaluate (ctx, exp, ops);
				if (var != null) {
					return var.CreateObjectValue ();
				}
				else
					return ObjectValue.CreateUnknown (exp);
			}
			catch (NotSupportedExpressionException ex) {
				return ObjectValue.CreateNotSupported (exp, ex.Message, ObjectValueFlags.None);
			}
			catch (EvaluatorException ex) {
				return ObjectValue.CreateError (exp, ex.Message, ObjectValueFlags.None);
			}
			catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				return ObjectValue.CreateUnknown (exp);
			}
		}

		public virtual TValue RuntimeInvoke (EvaluationContext<TValue, TType> ctx, TType targetType, TValue target, string methodName, TType[] argTypes, TValue[] argValues)
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
		public bool IsProxyType;

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
