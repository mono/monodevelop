using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using System.Diagnostics;

namespace MonoDevelop.Debugger.Evaluation
{
	public abstract class ObjectValueAdaptor<TValue,TType>
	{
		static Dictionary<string, TypeDisplayData> typeDisplayData = new Dictionary<string, TypeDisplayData> ();
		static Dictionary<TType, TType> proxyTypes = new Dictionary<TType, TType> ();

		public int DefaultAsyncSwitchTimeout = 60;
		public int DefaultEvaluationTimeout = 1000;
		public int DefaultChildEvaluationTimeout = 5000;
	
		public abstract TValue GetRealObject (EvaluationContext<TValue, TType> ctx, TValue obj);

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

		public abstract bool IsPrimitive (TValue val);
		public abstract bool IsClassInstance (TValue val);
		public abstract bool IsClass (TType val);
		public abstract bool IsArray (TValue val);

		public abstract TType GetValueType (TValue val);
		public abstract string GetTypeName (TType val);

		public virtual string GetValueTypeName (TValue val)
		{
			return GetTypeName (GetValueType (val));
		}


		protected abstract ObjectValue CreateObjectValueImpl (EvaluationContext<TValue, TType> ctx, IObjectValueSource source, ObjectPath path, TValue obj, ObjectValueFlags flags);

		public ObjectValue[] GetObjectValueChildren (EvaluationContext<TValue, TType> ctx, TValue obj, int firstItemIndex, int count)
		{
			return GetObjectValueChildren (ctx, obj, firstItemIndex, count, true);
		}

		public virtual ObjectValue[] GetObjectValueChildren (EvaluationContext<TValue, TType> ctx, TValue obj, int firstItemIndex, int count, bool dereferenceProxy)
		{
			obj = GetRealObject (ctx, obj);

			if (IsArray (obj)) {
				ArrayElementGroup<TValue, TType> agroup = new ArrayElementGroup<TValue, TType> (ctx, CreateArrayAdaptor (ctx, obj));
				return agroup.GetChildren ();
			}

			if (IsPrimitive (obj))
				return new ObjectValue[0];

			// If there is a proxy, it has to show the members of the proxy
			TValue proxy = dereferenceProxy ? GetProxyObject (ctx, obj) : obj;

			TypeDisplayData tdata = GetTypeDisplayData (ctx, GetValueType (proxy));
			List<ObjectValue> values = new List<ObjectValue> ();
			ReqMemberAccess access = tdata.IsProxyType ? ReqMemberAccess.Public : ReqMemberAccess.Auto;
			foreach (ValueReference<TValue,TType> val in GetMembers (ctx, GetValueType (proxy), proxy, access)) {
				try {
					DebuggerBrowsableState state = tdata.GetMemberBrowsableState (val.Name);
					if (state == DebuggerBrowsableState.Never)
						continue;

					if (state == DebuggerBrowsableState.RootHidden) {
						TValue ob = val.Value;
						if (ob != null)
							values.AddRange (GetObjectValueChildren (ctx, ob, -1, -1));
					}
					else {
						values.Add (val.CreateObjectValue (true));
					}

				}
				catch (Exception ex) {
					ctx.WriteDebuggerError (ex);
					values.Add (ObjectValue.CreateError (null, new ObjectPath (val.Name), GetTypeName (val.Type), ex.Message, val.Flags));
				}
			}
			if (tdata.IsProxyType) {
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
			}
			return values.ToArray ();
		}

		public virtual IEnumerable<ValueReference<TValue,TType>> GetLocalVariables (EvaluationContext<TValue, TType> ctx)
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

		public abstract object StringToObject (TType type, string value);

		public IEnumerable<ValueReference<TValue, TType>> GetMembers (EvaluationContext<TValue, TType> ctx, TType t, TValue co)
		{
			return GetMembers (ctx, t, co, ReqMemberAccess.Auto);
		}

		public virtual ValueReference<TValue, TType> GetMember (EvaluationContext<TValue, TType> ctx, TType t, TValue co, string name)
		{
			foreach (ValueReference<TValue, TType> var in GetMembers (ctx, t, co))
				if (var.Name == name)
					return var;
			return null;
		}

		public abstract IEnumerable<ValueReference<TValue, TType>> GetMembers (EvaluationContext<TValue, TType> ctx, TType t, TValue co, ReqMemberAccess access);

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
			TypeDisplayData data = GetTypeDisplayData (ctx, GetValueType (obj));
			if (data.ProxyType == null)
				return obj;

			return default(TValue);
			/*			CorType ttype = ctx.Frame.Language.LookupType (data.ProxyType);
						if (ttype == null) {
							int i = data.ProxyType.IndexOf (',');
							if (i != -1)
								ttype = ctx.Frame.Language.LookupType (data.ProxyType.Substring (0, i).Trim ());
						}
						if (ttype == null)
							throw new EvaluatorException ("Unknown type '{0}'", data.ProxyType);

						TargetObject proxy = CreateObject (ctx, ttype, obj);
						return GetRealObject (ctx, proxy);*/
		}

		public TypeDisplayData GetTypeDisplayData (EvaluationContext<TValue, TType> ctx, TType type)
		{
			if (!IsClass (type))
				return TypeDisplayData.Default;

			TypeDisplayData td = null;
			try {
				td = GetTypeDisplayDataInternal (ctx, type);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
			}
			if (td == null)
				typeDisplayData[GetTypeName (type)] = td = TypeDisplayData.Default;
			return td;
		}

		TypeDisplayData GetTypeDisplayDataInternal (EvaluationContext<TValue, TType> ctx, TType type)
		{
			string tname = GetTypeName (type);
			TypeDisplayData data;
			if (typeDisplayData.TryGetValue (tname, out data))
				return data;

			data = new TypeDisplayData ();
			typeDisplayData[tname] = data;
			return data;
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

				ValueReference<TValue, TType> member = GetMember (ctx, GetValueType (obj), obj, mem);
				TValue val = member.Value;
				sb.Append (ctx.Evaluator.TargetObjectToString (ctx, val));
				last = j + 1;
				i = exp.IndexOf ("{", last);
			}
			sb.Append (exp.Substring (last));
			return sb.ToString ();
		}

		public ObjectValue[] GetExpressionValuesAsync (EvaluationContext<TValue, TType> ctx, string[] expressions, bool evaluateMethods, int timeout)
		{
			ObjectValue[] values = new ObjectValue[expressions.Length];
			for (int n = 0; n < values.Length; n++) {
				string exp = expressions[n];
				values[n] = ctx.AsyncEvaluationTracker.Run (exp, ObjectValueFlags.Literal, delegate {
					return GetExpressionValue (ctx, exp, evaluateMethods);
				});
			}
			return values;
		}

		ObjectValue GetExpressionValue (EvaluationContext<TValue, TType> ctx, string exp, bool evaluateMethods)
		{
			try {
				EvaluationOptions<TType> ops = new EvaluationOptions<TType> ();
				ops.CanEvaluateMethods = evaluateMethods;
				ValueReference<TValue, TType> var = ctx.Evaluator.Evaluate (ctx, exp, ops);
				if (var != null)
					return var.CreateObjectValue ();
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

	public enum ReqMemberAccess
	{
		All,
		Auto,
		Public
	}
}
