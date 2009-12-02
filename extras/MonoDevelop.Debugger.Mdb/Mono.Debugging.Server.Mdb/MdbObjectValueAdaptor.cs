// 
// MdbObjectValueAdaptor.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;
using System.Collections;
using ST = System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Debugging.Evaluation;
using Mono.Debugger.Languages;
using Mono.Debugger;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace DebuggerServer
{
	public class MdbObjectValueAdaptor : ObjectValueAdaptor
	{
		public override string CallToString (EvaluationContext gctx, object obj)
		{
			if (!gctx.Options.AllowTargetInvoke)
				return GetValueTypeName (gctx, obj);
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetObject retval = CallMethod (ctx, "ToString", (TargetStructObject) obj);
			object s = ((TargetFundamentalObject) retval).GetObject (ctx.Thread);
			return s != null ? s.ToString () : "";
		}

		public override object Cast (EvaluationContext gctx, object obj, object targetType)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return TargetObjectConvert.Cast (ctx, (TargetObject) obj, (TargetType) targetType);
		}

		public override object TryCast (EvaluationContext gctx, object val, object type)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext)gctx;
			TargetObject tval = ctx.GetRealObject (val);
			if (type is TargetObjectType)
				return val;
			if (type is TargetClassType)
				return TargetObjectConvert.TryCast (ctx, tval, (TargetClassType)type);
			else
				return null;
		}


		public override object TargetObjectToObject (EvaluationContext gctx, object vobj)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetObject obj = ctx.GetRealObject (vobj);
			
			switch (obj.Kind) {
				case Mono.Debugger.Languages.TargetObjectKind.Array:
					TargetArrayObject arr = obj as TargetArrayObject;
					if (arr == null)
						return null;
					StringBuilder tn = new StringBuilder (arr.Type.ElementType.Name);
					tn.Append ("[");
					TargetArrayBounds ab = arr.GetArrayBounds (ctx.Thread);
					if (ab.IsMultiDimensional) {
						for (int n=0; n<ab.Rank; n++) {
							if (n>0)
								tn.Append (',');
							tn.Append (ab.UpperBounds [n] - ab.LowerBounds [n] + 1);
						}
					}
					else if (!ab.IsUnbound) {
						tn.Append (ab.Length.ToString ());
					}
					tn.Append ("]");
					return new LiteralExp (tn.ToString ());
					
				case TargetObjectKind.GenericInstance:
				case TargetObjectKind.Struct:
				case TargetObjectKind.Class:
					TypeDisplayData tdata = GetTypeDisplayData (ctx, obj.Type);
					TargetStructObject co = obj as TargetStructObject;
					if (co == null)
						return null;
					if (ctx.Options.AllowTargetInvoke) {
						if (co.TypeName == "System.Decimal")
							return new LiteralExp (CallToString (ctx, co));
						if (tdata.ValueDisplayString != null && ctx.Options.AllowDisplayStringEvaluation)
							return new LiteralExp (EvaluateDisplayString (ctx, co, tdata.ValueDisplayString));
					}
					
					// Return the type name
					if (tdata.TypeDisplayString != null)
						return new LiteralExp ("{" + tdata.TypeDisplayString + "}");
					return new LiteralExp ("{" + obj.Type.Name + "}");
					
				case TargetObjectKind.Enum:
					TargetEnumObject eob = (TargetEnumObject) obj;
					return new LiteralExp (Server.Instance.Evaluator.TargetObjectToString (ctx, eob.GetValue (ctx.Thread)));
					
				case TargetObjectKind.Fundamental:
					TargetFundamentalObject fob = obj as TargetFundamentalObject;
					if (fob == null)
						return "null";
					return fob.GetObject (ctx.Thread);
					
				case TargetObjectKind.Pointer:
					if (IntPtr.Size < 8)
						return new IntPtr ((int)obj.GetAddress (ctx.Thread).Address);
					else
						return new IntPtr (obj.GetAddress (ctx.Thread).Address);
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return null;
					else
						return new LiteralExp ("{" + oob.TypeName + "}");
					
				case TargetObjectKind.Nullable:
					TargetNullableObject nob = (TargetNullableObject) obj;
					if (nob.HasValue (ctx.Thread))
						return TargetObjectToObject (ctx, nob.GetValue (ctx.Thread));
					else
						return null;
			}
			return new LiteralExp ("?");
		}


		public override bool HasMethod (EvaluationContext gctx, object targetType, string methodName, object[] argTypes, BindingFlags flags)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			
			if (argTypes == null) {
				foreach (MemberReference mm in ObjectUtil.GetTypeMembers (ctx, (TargetType) targetType, false, false, true, flags | BindingFlags.Public | BindingFlags.NonPublic)) {
					TargetMethodInfo met = (TargetMethodInfo) mm.Member;
					if (met.Name == methodName)
						return true;
				}
				return false;
			}
			
			TargetStructType stype = targetType as TargetStructType;
			if (stype == null)
				return false;
		
			TargetType[] types = new TargetType [argTypes.Length];
			Array.Copy (argTypes, types, argTypes.Length);
			
			MemberReference mem;
			mem = OverloadResolve (ctx, methodName, stype, types, (flags & BindingFlags.Instance) != 0, (flags & BindingFlags.Static) != 0, false);
			return mem != null;
		}
		
		public override object RuntimeInvoke (EvaluationContext gctx, object targetType, object target, string methodName, object[] argTypes, object[] argValues)
		{
			gctx.AssertTargetInvokeAllowed ();
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetObject[] lst = new TargetObject [argValues.Length];
			Array.Copy (argValues, lst, argValues.Length);
			if (target != null)
				return CallMethod (ctx, methodName, (TargetObject) target, lst);
			else
				return CallStaticMethod (ctx, methodName, (TargetType) targetType, lst);
		}

		protected override TypeDisplayData OnGetTypeDisplayData (EvaluationContext gctx, object type)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetType tt = (TargetType) type;
			if (tt.HasClassType) {
				TypeDisplayData data = new TypeDisplayData ();
				if (tt.ClassType.DebuggerTypeProxyAttribute != null) {
					data.ProxyType = tt.ClassType.DebuggerTypeProxyAttribute.ProxyTypeName;
				}
				if (tt.ClassType.DebuggerDisplayAttribute != null) {
					data.NameDisplayString = tt.ClassType.DebuggerDisplayAttribute.Name;
					data.TypeDisplayString = tt.ClassType.DebuggerDisplayAttribute.Type;
					data.ValueDisplayString = tt.ClassType.DebuggerDisplayAttribute.Value;
				}
				foreach (MemberReference mem in ObjectUtil.GetTypeMembers (ctx, tt, true, true, true, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
					if (mem.Member.DebuggerBrowsableState.HasValue) {
						if (data.MemberData == null)
							data.MemberData = new Dictionary<string, DebuggerBrowsableState> ();
						data.MemberData [mem.Member.Name] = mem.Member.DebuggerBrowsableState.Value;
					}
				}
				return data;
			}
			return base.OnGetTypeDisplayData (ctx, type);
		}

		public override bool IsPrimitive (EvaluationContext gctx, object val)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.GetRealObject (val) is TargetFundamentalObject;
		}

		public override bool IsEnum (EvaluationContext gctx, object val)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.GetRealObject (val) is TargetEnumObject;
		}

		public override bool IsNull (EvaluationContext gctx, object val)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetObject ob = ctx.GetRealObject (val);
			if (ob.Kind == TargetObjectKind.Null)
				return true;
			return (ob.HasAddress && ob.GetAddress (ctx.Thread).IsNull);
		}


		public override bool IsClass (object type)
		{
			return type is TargetClassType;
		}


		public override bool IsClassInstance (EvaluationContext gctx, object val)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.GetRealObject (val) is TargetStructObject;
		}

		public override bool IsArray (EvaluationContext gctx, object val)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.GetRealObject (val) is TargetArrayObject;
		}


		public override object GetValueType (EvaluationContext gctx, object val)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.GetRealObject (val).Type;
		}


		public override string GetTypeName (EvaluationContext ctx, object val)
		{
			return ObjectUtil.FixTypeName (((TargetType)val).Name);
		}


		public override object GetType (EvaluationContext ctx, string name, object[] typeArgs)
		{
			MdbEvaluationContext mctx = (MdbEvaluationContext) ctx;
			name = name.Replace ('+','/');
			return mctx.Frame.Language.LookupType (name);
		}

		public override object GetBaseType (EvaluationContext ctx, object t)
		{
			MdbEvaluationContext mctx = (MdbEvaluationContext) ctx;
			TargetStructType type = t as TargetStructType;
			if (type != null && type.HasParent)
				return type.GetParentType (mctx.Thread);
			else
				return null;
		}

		public override object[] GetTypeArgs (EvaluationContext ctx, object type)
		{
			return new object [0];
		}


		public override ValueReference GetThisReference (EvaluationContext gctx)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			if (ctx.Frame.Method != null && ctx.Frame.Method.HasThis) {
				ObjectValueFlags flags = ObjectValueFlags.Field | ObjectValueFlags.ReadOnly;
				TargetVariable var = ctx.Frame.Method.GetThis (ctx.Thread);
				VariableReference vref = new VariableReference (ctx, var, flags);
				return vref;
			}
			else
				return null;
		}


		public override IEnumerable<ValueReference> GetParameters (EvaluationContext gctx)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			if (ctx.Frame.Method != null) {
				foreach (TargetVariable var in ctx.Frame.Method.GetParameters (ctx.Thread))
					yield return new VariableReference (ctx, var, ObjectValueFlags.Parameter);
			}
		}

		public override IEnumerable<object> GetNestedTypes (EvaluationContext gctx, object type)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			string[] tname = GetTypeName (ctx, type).Split ('+');
			object td = FindType (ctx, tname[0]);
			if (td != null) {
				td = FindNestedType (td, tname, 1);
				IEnumerable nestedTypes = (IEnumerable) GetProp (td, "NestedTypes");
				foreach (object nt in nestedTypes) {
					string name = (string) GetProp (nt, "FullName");
					object tt = GetType (ctx, name);
					if (tt != null)
						yield return tt;
				}
			}
		}
		
		object FindNestedType (object td, string[] names, int index)
		{
			if (index == names.Length)
				return td;
			IEnumerable nestedTypes = (IEnumerable) GetProp (td, "NestedTypes");
			foreach (object nt in nestedTypes) {
				string name = (string) GetProp (nt, "Name");
				if (name == names [index])
					return FindNestedType (nt, names, index + 1);
			}
			return null;
		}
		
		object FindType (MdbEvaluationContext ctx, string typeName)
		{
			foreach (object typeDefinition in GetAllTypeDefinitions (ctx, true)) {
				string fullName = (string) GetProp (typeDefinition, "FullName");
				if (fullName == typeName)
					return typeDefinition;
			}
			return null;
		}

		public override void GetNamespaceContents (EvaluationContext gctx, string namspace, out string[] childNamespaces, out string[] childTypes)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			
			// Child types
			
			List<string> types = new List<string> ();
			HashSet<string> namespaces = new HashSet<string> ();
			string namspaceDotted = namspace + ".";
			foreach (object typeDefinition in GetAllTypeDefinitions (ctx, false)) {
				string typeNamespace = (string) GetProp (typeDefinition, "Namespace");
				if (typeNamespace == namspace)
					types.Add ((string) GetProp (typeDefinition, "FullName"));
				else if (typeNamespace.StartsWith (namspaceDotted)) {
					int i = typeNamespace.IndexOf ('.', namspaceDotted.Length);
					if (i != -1)
						typeNamespace = typeNamespace.Substring (0, i);
					namespaces.Add (typeNamespace);
				}
			}
			childTypes = types.ToArray ();
			childNamespaces = new string [namespaces.Count];
			namespaces.CopyTo (childNamespaces);
		}
		
		public IEnumerable<object> GetAllTypeDefinitions (MdbEvaluationContext ctx, bool includePrivate)
		{
			HashSet<object> visited = new HashSet<object> ();
			object methodHandle = ctx.Frame.Method.MethodHandle;

			if (methodHandle != null && methodHandle.GetType ().FullName == "Mono.Cecil.MethodDefinition") {
				object declaringType = GetProp (methodHandle, "DeclaringType");
				object module = GetProp (declaringType, "Module");
				object assembly = GetProp (module, "Assembly");
				object resolver = GetProp (assembly, "Resolver");
				
				foreach (object typeDefinition in GetAllTypeDefinitions (includePrivate, resolver, visited, assembly))
					yield return typeDefinition;
			}
		}
		
		public IEnumerable<object> GetAllTypeDefinitions (bool includePrivate, object resolver, HashSet<object> visited, object asm)
		{
			if (!visited.Add (asm))
				yield break;

			object mainModule = GetProp (asm, "MainModule");
			foreach (object typeDefinition in (IEnumerable) GetProp (mainModule, "Types")) {
				bool isPublic = includePrivate || (bool) GetProp (typeDefinition, "IsPublic");
				bool isInterface = (bool) GetProp (typeDefinition, "IsInterface");
				bool isEnum = (bool) GetProp (typeDefinition, "IsEnum");
				if (isPublic && !isInterface && !isEnum)
					yield return typeDefinition;
			}

			Type assemblyNameReferenceType = resolver.GetType ().Assembly.GetType ("Mono.Cecil.AssemblyNameReference");
			MethodInfo resolveMet = resolver.GetType ().GetMethod ("Resolve", new Type[] { assemblyNameReferenceType });
			foreach (object an in (IEnumerable) GetProp (mainModule, "AssemblyReferences")) {
				object refAsm = resolveMet.Invoke (resolver, new object[] {an});
				if (refAsm != null) {
					foreach (object td in GetAllTypeDefinitions (includePrivate, resolver, visited, refAsm))
						yield return td;
				}
			}
		}

		static object GetProp (object obj, string name)
		{
			return obj.GetType ().GetProperty (name).GetValue (obj, null);
		}

		public override IEnumerable<ValueReference> GetMembers (EvaluationContext gctx, object tt, object ob, BindingFlags bindingFlags)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext)gctx;
			TargetStructObject co = ctx.GetRealObject (ob) as TargetStructObject; // It can be a TargetObjectObject
			TargetType t = (TargetType)tt;
			if (co == null) {
				bindingFlags |= BindingFlags.Static;
				bindingFlags &= ~BindingFlags.Instance;
			}
			foreach (MemberReference mem in ObjectUtil.GetTypeMembers (ctx, t, true, true, false, bindingFlags)) {
				if (mem.Member is TargetFieldInfo) {
					TargetFieldInfo field = (TargetFieldInfo)mem.Member;
					yield return new FieldReference (ctx, co, mem.DeclaringType, field);
				}
				if (mem.Member is TargetPropertyInfo) {
					TargetPropertyInfo prop = (TargetPropertyInfo) mem.Member;
					if (prop.CanRead && (prop.Getter.ParameterTypes == null || prop.Getter.ParameterTypes.Length == 0))
						yield return new PropertyReference (ctx, prop, co);
				}
			}
		}


		public override IEnumerable<ValueReference> GetLocalVariables (EvaluationContext gctx)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			foreach (TargetVariable var in ctx.Frame.Method.GetLocalVariables (ctx.Thread)) {
				yield return new VariableReference (ctx, var, ObjectValueFlags.Variable);
			}
		}


		public override ValueReference GetIndexerReference (EvaluationContext gctx, object target, object index)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return IndexerValueReference.CreateIndexerValueReference (ctx, (TargetObject) target, (TargetObject) index);
		}


		public override string[] GetImportedNamespaces (EvaluationContext gctx)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.Frame.Method.GetNamespaces ();
		}


		public override object GetEnclosingType (EvaluationContext gctx)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.Frame.Method.GetDeclaringType (ctx.Frame.Thread);
		}


		public override object GetBaseValue (EvaluationContext gctx, object val)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetStructObject sob = (TargetStructObject) ctx.GetRealObject (val);
			return sob.GetParentObject (ctx.Thread);
		}


		public override object CreateValue (EvaluationContext gctx, object value)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.Frame.Language.CreateInstance (ctx.Thread, value);
		}


		public override object CreateValue (EvaluationContext gctx, object type, params object[] args)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetType tt = (TargetType) type;
			TargetObject[] lst = new TargetObject [args.Length];
			Array.Copy (args, lst, args.Length);
			return CallStaticMethod (ctx, ".ctor", tt, lst);
		}


		public override object CreateTypeObject (EvaluationContext gctx, object type)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return GetTypeOf (ctx, ((TargetType)type).Name);
		}

		public static TargetStructObject GetTypeOf (MdbEvaluationContext ctx, string typeName)
		{
			ctx.AssertTargetInvokeAllowed ();
			TargetType tt = ctx.Frame.Language.LookupType ("System.Type");
			if (tt == null)
				return null;

			TargetObject tn = ctx.Frame.Language.CreateInstance (ctx.Thread, ObjectUtil.FixTypeName (typeName));
			TargetObject res = CallStaticMethod (ctx, "GetType", tt, tn);
			return (TargetStructObject) ctx.GetRealObject (res);
		}

		protected override ObjectValue CreateObjectValueImpl (EvaluationContext gctx, IObjectValueSource source, ObjectPath path, object vobj, ObjectValueFlags flags)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetObject obj = ctx.GetRealObject (vobj);
			
			if (obj == null)
				return ObjectValue.CreateObject (null, path, "", null, flags | ObjectValueFlags.ReadOnly, null);

			if (obj.HasAddress && obj.GetAddress (ctx.Thread).IsNull)
				return ObjectValue.CreateObject (null, path, obj.TypeName, ctx.Evaluator.ToExpression (null), flags, null);
			
			switch (obj.Kind) {
				
				case TargetObjectKind.Struct:
				case TargetObjectKind.GenericInstance:
				case TargetObjectKind.Class:
					TypeDisplayData tdata = GetTypeDisplayData (ctx, obj.Type);
					
					TargetStructObject co = obj as TargetStructObject;
					if (co == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else {
						string tvalue;
						if (!string.IsNullOrEmpty (tdata.ValueDisplayString) && ctx.Options.AllowDisplayStringEvaluation)
							tvalue = EvaluateDisplayString (ctx, co, tdata.ValueDisplayString);
						else
							tvalue = ctx.Evaluator.TargetObjectToExpression (ctx, obj);
						
						string tname;
						if (!string.IsNullOrEmpty (tdata.TypeDisplayString) && ctx.Options.AllowDisplayStringEvaluation)
							tname = EvaluateDisplayString (ctx, co, tdata.TypeDisplayString);
						else
							tname = obj.TypeName;
						
						ObjectValue val = ObjectValue.CreateObject (source, path, tname, tvalue, flags, null);
						if (!string.IsNullOrEmpty (tdata.NameDisplayString) && ctx.Options.AllowDisplayStringEvaluation)
							val.Name = EvaluateDisplayString (ctx, co, tdata.NameDisplayString);
						return val;
					}
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else
						return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToExpression (ctx, obj), flags, null);
					
				case TargetObjectKind.Array:
					return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToExpression (ctx, obj), flags, null);
					
				case TargetObjectKind.Fundamental:
					TargetFundamentalObject fob = (TargetFundamentalObject) obj;
					return ObjectValue.CreatePrimitive (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToExpression (ctx, fob), flags);
					
				case TargetObjectKind.Enum:
					Console.WriteLine ("pp ??? ENUM: " + obj.GetType ());
					TargetEnumObject enumobj = (TargetEnumObject) obj;
					return CreateObjectValue (ctx, source, path, enumobj.GetValue (ctx.Thread), flags);
					
				case TargetObjectKind.Pointer:
					return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToExpression (ctx, obj), flags, null);
					
				case TargetObjectKind.Nullable:
					TargetNullableObject tn = (TargetNullableObject) obj;
					if (tn.HasValue (ctx.Thread)) {
						ObjectValue val = CreateObjectValue (ctx, source, path, tn.GetValue (ctx.Thread), flags);
						val.TypeName = obj.TypeName;
						return val;
					}
					else {
						flags |= ObjectValueFlags.Primitive;
						return ObjectValue.CreateObject (source, path, obj.TypeName, ctx.Evaluator.ToExpression (null), flags, new ObjectValue [0]);
					}
				default:
					return ObjectValue.CreateFatalError (path.LastName, "Unknown value type: " + obj.Kind, flags);
			}
		}

		public override object CreateNullValue (EvaluationContext gctx, object type)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			return ctx.Frame.Language.CreateNullObject (ctx.Thread, (TargetType)type);
		}

		public override ICollectionAdaptor CreateArrayAdaptor (EvaluationContext gctx, object arr)
		{
			MdbEvaluationContext ctx = (MdbEvaluationContext) gctx;
			TargetArrayObject aob = arr as TargetArrayObject;
			if (aob != null)
				return new ArrayAdaptor (ctx, aob);
			else
				return null;
		}

		public static TargetObject CallMethod (MdbEvaluationContext ctx, string name,
							  TargetObject target,
							  params TargetObject[] args)
		{
#if REFLECTION_INVOKE
			if (target is TargetGenericInstanceObject || !(target is TargetStructObject)) {
				// Calling methods on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				
				if (name != "ToString" || args.Length != 0) {
					GetTypeOf (ctx, "System.Convert");
					TargetType cc = ctx.Frame.Language.LookupType ("System.Convert");
					SR.BindingFlags sf = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Static | SR.BindingFlags.FlattenHierarchy | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
					CallMethodWithReflection (ctx, sf, "ToString", cc, null, target);
				}

				SR.BindingFlags f = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Instance | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (ctx, f, name, target.Type, target, args);
			}
#endif
			ctx.AssertTargetInvokeAllowed ();
			
			TargetType[] types = new TargetType [args.Length];
			for (int n=0; n<types.Length; n++)
				types [n] = args [n].Type;
			
			TargetStructObject starget = (TargetStructObject) target;
			MemberReference mem = OverloadResolve (ctx, name, starget.Type, types, true, false, true);
			TargetFunctionType function = (TargetFunctionType) ((TargetMethodInfo) mem.Member).Type;
			
			while (target.Type != mem.DeclaringType) {
				TargetStructObject par = starget.GetParentObject (ctx.Thread);
				if (par != null)
					target = par;
				else
					break;
			}
			
			TargetMethodSignature sig = function.GetSignature (ctx.Thread);

			TargetObject[] objs = new TargetObject [args.Length];
			for (int i = 0; i < args.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (
					ctx, args [i], sig.ParameterTypes [i]);
			}

			return Server.Instance.RuntimeInvoke (ctx, function, starget, objs);
		}
		
		public static TargetObject CallStaticMethod (MdbEvaluationContext ctx, string name,
							  string typeName,
							  params TargetObject[] args)
		{
			return CallStaticMethod (ctx, name, ctx.Frame.Language.LookupType (typeName), args);
		}
		
		public static TargetObject CallStaticMethod (MdbEvaluationContext ctx, string name,
							  TargetType type,
							  params TargetObject[] args)
		{
#if REFLECTION_INVOKE
			if (type is TargetGenericInstanceType || !(type is TargetStructType)) {
				// Calling methods on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				
				SR.BindingFlags f = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Static | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (ctx, f, name, type, null, args);
			}
#endif			
			ctx.AssertTargetInvokeAllowed ();
			
			TargetType[] types = new TargetType [args.Length];
			for (int n=0; n<types.Length; n++)
				types [n] = args [n].Type;
			
			MemberReference mem = OverloadResolve (ctx, name, (TargetStructType) type, types, false, true, true);
			TargetFunctionType function = (TargetFunctionType) ((TargetMethodInfo) mem.Member).Type;
			
			TargetMethodSignature sig = function.GetSignature (ctx.Thread);

			TargetObject[] objs = new TargetObject [args.Length];
			for (int i = 0; i < args.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (ctx, args [i], sig.ParameterTypes [i]);
			}

			return Server.Instance.RuntimeInvoke (ctx, function, null, objs);
		}
		
		public static MemberReference OverloadResolve (MdbEvaluationContext ctx, string methodName, TargetStructType type, TargetType[] argtypes, bool allowInstance, bool allowStatic, bool throwIfNotFound)
		{
			List<MemberReference> candidates = new List<MemberReference> ();

			if (methodName == ".ctor") {
				TargetClassType ct = type as TargetClassType;
				if (ct == null && type.HasClassType)
					ct = type.ClassType;
				
				foreach (TargetMethodInfo met in ct.Constructors) {
					if (met.Type.ParameterTypes.Length == argtypes.Length)
						candidates.Add (new MemberReference (met, type));
				}
			}
			else {
				foreach (MemberReference mem in ObjectUtil.GetTypeMembers (ctx, type, false, false, true, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
					TargetMethodInfo met = (TargetMethodInfo) mem.Member;
					if (met.Name == methodName && met.Type.ParameterTypes.Length == argtypes.Length && (met.IsStatic && allowStatic || !met.IsStatic && allowInstance))
						candidates.Add (mem);
				}
			}
			
			if (candidates.Count == 1) {
				TargetFunctionType func = (TargetFunctionType) ((TargetMethodInfo) candidates [0].Member).Type;
				string error;
				int matchCount;
				if (IsApplicable (ctx, func, argtypes, out error, out matchCount))
					return candidates [0];

				if (throwIfNotFound)
					throw new EvaluatorException ("Invalid arguments for method `{0}': {1}", methodName, error);
				else
					return null;
			}

			if (candidates.Count == 0) {
				if (throwIfNotFound)
					throw new EvaluatorException ("Method `{0}' not found in type `{1}'.", methodName, type.Name);
				else
					return null;
			}

			return OverloadResolve (ctx, methodName, argtypes, candidates, throwIfNotFound);
		}

		static bool IsApplicable (MdbEvaluationContext ctx, TargetFunctionType method, TargetType[] types, out string error, out int matchCount)
		{
			TargetMethodSignature sig = method.GetSignature (ctx.Thread);
			matchCount = 0;

			for (int i = 0; i < types.Length; i++) {
				TargetType param_type = sig.ParameterTypes [i];

				if (param_type == types [i]) {
					matchCount++;
					continue;
				}

				if (TargetObjectConvert.ImplicitConversionExists (ctx, types [i], param_type))
					continue;

				error = String.Format (
					"Argument {0}: Cannot implicitly convert `{1}' to `{2}'",
					i, types [i].Name, param_type.Name);
				return false;
			}

			error = null;
			return true;
		}

		static MemberReference OverloadResolve (MdbEvaluationContext ctx, string methodName, TargetType[] argtypes, List<MemberReference> candidates, bool throwIfNotFound)
		{
			// Ok, no we need to find an exact match.
			MemberReference match = null;
			int bestCount = -1;
			bool repeatedBestCount = false;
			
			foreach (MemberReference method in candidates) {
				string error;
				int matchCount;
				TargetFunctionType func;
				if (method.Member is TargetMethodInfo)
					func = (TargetFunctionType) ((TargetMethodInfo) method.Member).Type;
				else
					func = (TargetFunctionType) ((TargetPropertyInfo) method.Member).Getter;
				
				if (!IsApplicable (ctx, func, argtypes, out error, out matchCount))
					continue;

				if (matchCount == bestCount) {
					repeatedBestCount = true;
				} else if (matchCount > bestCount) {
					match = method;
					bestCount = matchCount;
					repeatedBestCount = false;
				}
			}
			
			if (match == null) {
				if (!throwIfNotFound)
					return null;
				if (methodName != null)
					throw new EvaluatorException ("Invalid arguments for method `{0}'.", methodName);
				else
					throw new EvaluatorException ("Invalid arguments for indexer.");
			}
			
			if (repeatedBestCount) {
				// If there is an ambiguous match, just pick the first match. If the user was expecting
				// something else, he can provide more specific arguments
				
/*				if (!throwIfNotFound)
					return null;
				if (methodName != null)
					throw new EvaluatorException ("Ambiguous method `{0}'; need to use full name", methodName);
				else
					throw new EvaluatorException ("Ambiguous arguments for indexer.", methodName);
*/			}
			 
			return match;
		}		
	}
	
	class ObjectUtil
	{
		public static TargetObject GetRealObject (MdbEvaluationContext ctx, TargetObject obj)
		{
			if (obj == null)
				return null;

			try {
				switch (obj.Kind) {
					case TargetObjectKind.Array:
					case TargetObjectKind.Fundamental:
						return obj;
						
					case TargetObjectKind.Struct:
					case TargetObjectKind.GenericInstance:
					case TargetObjectKind.Class:
						TargetStructObject co = obj as TargetStructObject;
						if (co == null)
							return null;
						TargetObject res = co.GetCurrentObject (ctx.Thread);
						return res ?? obj;
						
					case TargetObjectKind.Enum:
						TargetEnumObject eob = (TargetEnumObject) obj;
						return eob.GetValue (ctx.Thread);
						
					case TargetObjectKind.Object:
						TargetObjectObject oob = obj as TargetObjectObject;
						if (oob == null)
							return null;
						if (oob.Type.CanDereference)
							return oob.GetDereferencedObject (ctx.Thread);
						else
							return oob;
				}
			}
			catch {
				// Ignore
			}
			return obj;
		}
		
		public static IEnumerable<MemberReference> GetTypeMembers (MdbEvaluationContext ctx, TargetType t, bool includeFields, bool includeProps, bool includeMethods, BindingFlags flags)
		{
			// Don't use yield in this method because the whole list of members
			// must be retrieved before we can do anything with them.
			List<MemberReference> members = new List<MemberReference> ();
			
			TargetStructType type = t as TargetStructType;
			Dictionary<string,string> foundMethods = new Dictionary<string,string> ();

			while (type != null) {
				
				TargetFieldInfo[] fields = null;
				TargetPropertyInfo[] properties = null;
				TargetMethodInfo[] methods = null;

				TargetClass cls = type.GetClass (ctx.Thread);
				if (cls != null) {
					if (includeFields)
						fields = cls.GetFields (ctx.Thread);
					if (includeProps)
						properties = cls.GetProperties (ctx.Thread);
					if (includeMethods)
						methods = cls.GetMethods (ctx.Thread);
				}
				else {
					TargetClassType ct = type as TargetClassType;
					if (ct == null && type.HasClassType)
						ct = type.ClassType;
					if (ct != null) {
						if (includeFields)
							fields = ct.Fields;
						if (includeProps)
							properties = ct.Properties;
						if (includeMethods)
							methods = ct.Methods;
					}
				}
				
				if (fields != null) {
					foreach (TargetFieldInfo field in fields) {
						if (field.IsCompilerGenerated)
							continue;
						if (field.Accessibility == TargetMemberAccessibility.Public && (flags & BindingFlags.Public) == 0)
							continue;
						if (field.Accessibility != TargetMemberAccessibility.Public && (flags & BindingFlags.NonPublic) == 0)
							continue;
						if (field.IsStatic && (flags & BindingFlags.Static) == 0)
							continue;
						if (!field.IsStatic && (flags & BindingFlags.Instance) == 0)
							continue;
						members.Add (new MemberReference (field, type));
					}
				}
				
				if (properties != null) {
					foreach (TargetPropertyInfo prop in properties) {
						if (prop.Accessibility == TargetMemberAccessibility.Public && (flags & BindingFlags.Public) == 0)
							continue;
						if (prop.Accessibility != TargetMemberAccessibility.Public && (flags & BindingFlags.NonPublic) == 0)
							continue;
						if (prop.IsStatic && (flags & BindingFlags.Static) == 0)
							continue;
						if (!prop.IsStatic && (flags & BindingFlags.Instance) == 0)
							continue;
						members.Add (new MemberReference (prop, type));
					}
				}
				
				if (methods != null) {
					foreach (TargetMethodInfo met in methods) {
						if (met.Accessibility == TargetMemberAccessibility.Public && (flags & BindingFlags.Public) == 0)
							continue;
						if (met.Accessibility != TargetMemberAccessibility.Public && (flags & BindingFlags.NonPublic) == 0)
							continue;
						if (met.IsStatic && (flags & BindingFlags.Static) == 0)
							continue;
						if (!met.IsStatic && (flags & BindingFlags.Instance) == 0)
							continue;
						string sig = met.FullName;
						if (!foundMethods.ContainsKey (sig)) {
							foundMethods [sig] = sig;
							members.Add (new MemberReference (met, type));
						}
					}
				}
				
				if (type.HasParent && (flags & BindingFlags.DeclaredOnly) == 0)
					type = type.GetParentType (ctx.Thread);
				else
					break;
			}
			return members;
		}
		
		public static ObjectValueFlags GetAccessibility (TargetMemberAccessibility ma)
		{
			switch (ma) {
				case TargetMemberAccessibility.Internal: return ObjectValueFlags.Internal;
				case TargetMemberAccessibility.Protected: return ObjectValueFlags.Protected;
				case TargetMemberAccessibility.Public: return ObjectValueFlags.Public;
				default: return ObjectValueFlags.Private;
			}
		}
		
		public static string FixTypeName (string typeName)
		{
			// Required since the debugger uses C# type aliases for fundamental types, 
			// which is silly, but looks like it won't be fixed any time soon.
			
			typeName = typeName.Replace ('/','+');
			
			switch (typeName) {
				case "short":   return "System.Int16";
				case "ushort":  return "System.UInt16";
				case "int":     return "System.Int32";
				case "uint":    return "System.UInt32";
				case "long":    return "System.Int64";
				case "ulong":   return "System.UInt64";
				case "float":   return "System.Single";
				case "double":  return "System.Double";
				case "char":    return "System.Char";
				case "byte":    return "System.Byte";
				case "sbyte":   return "System.SByte";
				case "object":  return "System.Object";
				case "string":  return "System.String";
				case "bool":    return "System.Boolean";
				case "void":    return "System.Void";
				case "decimal": return "System.Decimal";
			}
			
			int i = typeName.IndexOf ('<');
			if (i != -1) {
				StringBuilder sb = new StringBuilder (typeName.Substring (0, i));
				sb.Append ('[');
				i++;
				int e;
				do {
					e = FindTypeEnd (typeName, i);
					sb.Append (FixTypeName (typeName.Substring (i, e-i)));
					sb.Append (',');
					i = e + 1;
				} while (typeName [e] == ',');
				
				sb.Remove (sb.Length - 1, 1);
				sb.Append (']');
				return sb.ToString ();
			}
			
			return typeName;
		}
		
		static int FindTypeEnd (string str, int index)
		{
			int level = 0;
			while (index < str.Length) {
				char c = str [index];
				if (c == '[' || c == '<')
					level++;
				if (c == ']' || c == '>') {
					if (level == 0)
						return index;
					else
						level--;
				}
				if (c == ',' && level == 0)
					return index;
				index++;
			}
			return index;
		}
	}
	
	public class MemberReference
	{
		public readonly TargetMemberInfo Member;
		public readonly TargetStructType DeclaringType;
		
		public MemberReference (TargetMemberInfo member, TargetStructType type)
		{
			Member = member;
			DeclaringType = type;
		}
		
		public TargetObject GetValue (MdbEvaluationContext ctx, TargetStructObject thisObj)
		{
			if (Member is TargetPropertyInfo) {
				TargetPropertyInfo prop = (TargetPropertyInfo) Member;
				return ObjectUtil.GetRealObject (ctx, Server.Instance.RuntimeInvoke (ctx, prop.Getter, thisObj));
			}
			else if (Member is TargetFieldInfo) {
				TargetFieldInfo field = (TargetFieldInfo) Member;
				if (field.HasConstValue)
					return ctx.Frame.Language.CreateInstance (ctx.Thread, field.ConstValue);
				TargetClass cls = DeclaringType.GetClass (ctx.Thread);
				return ObjectUtil.GetRealObject (ctx, cls.GetField (ctx.Thread, thisObj, field));
			}
			else {
				TargetMethodInfo met = (TargetMethodInfo) Member;
				return ObjectUtil.GetRealObject (ctx, Server.Instance.RuntimeInvoke (ctx, met.Type, thisObj));
			}
		}
	}
	
	class MethodCall: AsyncOperation
	{
		MdbEvaluationContext ctx;
		TargetFunctionType function;
		TargetStructObject object_argument;
		TargetObject[] param_objects;
		RuntimeInvokeResult res;
		
		public MethodCall (MdbEvaluationContext ctx, TargetFunctionType function, TargetStructObject object_argument, TargetObject[] param_objects)
		{
			this.ctx = ctx;
			this.function = function;
			this.object_argument = object_argument;
			this.param_objects = param_objects;
		}
		
		public override string Description {
			get {
				if (function.DeclaringType != null)
					return function.DeclaringType.Name + "." + function.Name;
				else
					return function.Name;
			}
		}

		public override void Invoke ( )
		{
			res = ctx.Thread.RuntimeInvoke (function, object_argument, param_objects, true, false);
		}

		public override void Abort ( )
		{
			res.Abort ();
			res.CompletedEvent.WaitOne ();
			Server.Instance.MdbAdaptor.AbortThread (ctx.Thread, res);
			WaitToStop (ctx.Thread);
		}
		
		public override void Shutdown ()
		{
			res.Abort ();
			if (!res.CompletedEvent.WaitOne (200))
				return;
			Server.Instance.MdbAdaptor.AbortThread (ctx.Thread, res);
		}

		public override bool WaitForCompleted (int timeout)
		{
			if (timeout != -1) {
				if (!res.CompletedEvent.WaitOne (timeout, false))
					return false;
			}
			else {
				res.Wait ();
			}
			WaitToStop (ctx.Thread);
			return true;
		}

		void WaitToStop (Thread thread)
		{
			thread.WaitHandle.WaitOne ();
			while (!thread.IsStopped)
				ST.Thread.Sleep (1);
		}
		
		public TargetObject ReturnValue {
			get {
				if (res.ExceptionMessage != null)
					throw new EvaluatorException (res.ExceptionMessage);
				return res.ReturnObject;
			}
		}
	}
}
