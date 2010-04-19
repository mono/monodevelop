// ValueReference.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using DC = Mono.Debugging.Client;

namespace Mono.Debugging.Evaluation
{
	public abstract class ValueReference: RemoteFrameObject, IObjectValueSource, IObjectSource
	{
		EvaluationContext ctx;
		EvaluationOptions originalOptions;

		public ValueReference (EvaluationContext ctx)
		{
			this.ctx = ctx;
			originalOptions = ctx.Options;
		}
		
		public virtual object ObjectValue {
			get {
				object ob = Value;
				if (ctx.Adapter.IsNull (Context, ob))
					return null;
				else if (ctx.Adapter.IsPrimitive (Context, ob))
					return ctx.Adapter.TargetObjectToObject (ctx, ob);
				else
					return ob;
			}
		}
		
		public abstract object Value { get; set; }
		public abstract string Name { get; }
		public abstract object Type { get; }
		public abstract ObjectValueFlags Flags { get; }
		
		// For class members, the type declaring the member (null otherwise)
		public virtual object DeclaringType {
			get { return null; }
		}

		public EvaluationContext Context
		{
			get {
				return ctx;
			}
		}

		public ObjectValue CreateObjectValue (bool withTimeout)
		{
			return CreateObjectValue (withTimeout, Context.Options);
		}
		
		public ObjectValue CreateObjectValue (bool withTimeout, EvaluationOptions options)
		{
			EvaluationContext oldCtx = ctx;
			ctx = ctx.Clone (options);
			try {
				if (!CanEvaluate ())
					return DC.ObjectValue.CreateImplicitNotSupported (this, new ObjectPath (Name), ctx.Adapter.GetTypeName (Context, Type), Flags);
				if (withTimeout) {
					return ctx.Adapter.CreateObjectValueAsync (Name, Flags, delegate {
						return CreateObjectValue ();
					});
				} else
					return CreateObjectValue ();
			} finally {
				ctx = oldCtx;
			}
		}
		
		public ObjectValue CreateObjectValue ()
		{
			if (!CanEvaluate ())
				return DC.ObjectValue.CreateImplicitNotSupported (this, new ObjectPath (Name), ctx.Adapter.GetTypeName (Context, Type), Flags);
			
			Connect ();
			try {
				return OnCreateObjectValue ();
			} catch (ImplicitEvaluationDisabledException) {
				return DC.ObjectValue.CreateImplicitNotSupported (this, new ObjectPath (Name), ctx.Adapter.GetTypeName (Context, Type), Flags);
			} catch (NotSupportedExpressionException ex) {
				return DC.ObjectValue.CreateNotSupported (this, new ObjectPath (Name), ex.Message, ctx.Adapter.GetTypeName (Context, Type), Flags);
			} catch (EvaluatorException ex) {
				return DC.ObjectValue.CreateError (this, new ObjectPath (Name), "", ex.Message, Flags);
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				return DC.ObjectValue.CreateUnknown (Name);
			}
		}
		
		protected virtual bool CanEvaluate ()
		{
			return true;
		}
		
		protected virtual ObjectValue OnCreateObjectValue ()
		{
			string name = Name;
			if (string.IsNullOrEmpty (name))
				name = "?";
			
			object val = Value;
			if (val != null)
				return ctx.Adapter.CreateObjectValue (ctx, this, new ObjectPath (name), val, Flags);
			else
				return Mono.Debugging.Client.ObjectValue.CreateNullObject (this, name, ctx.Adapter.GetTypeName (Context, Type), Flags);
		}

		ObjectValue IObjectValueSource.GetValue (ObjectPath path, EvaluationOptions options)
		{
			ctx = ctx.Clone ();
			ctx.Options = options;
			return CreateObjectValue (true);
		}
		
		EvaluationResult IObjectValueSource.SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			try {
				ctx.WaitRuntimeInvokes ();
				EvaluationContext cctx = ctx.Clone ();
				EvaluationOptions ops = options ?? cctx.Options;
				ops.AllowMethodEvaluation = true;
				ops.AllowTargetInvoke = true;
				cctx.Options = ops;
				ValueReference vref = ctx.Evaluator.Evaluate (ctx, value, Type);
				object newValue = ctx.Adapter.Convert (ctx, vref.Value, Type);
				Value = newValue;
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				ctx.WriteDebuggerOutput ("Value assignment failed: {0}: {1}\n", ex.GetType (), ex.Message);
			}
			
			try {
				return ctx.Evaluator.TargetObjectToExpression (ctx, Value);
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				ctx.WriteDebuggerOutput ("Value assignment failed: {0}: {1}\n", ex.GetType (), ex.Message);
			}
			
			return null;
		}
		
		object IObjectValueSource.GetRawValue (ObjectPath path)
		{
			return ctx.Adapter.ToRawValue (ctx, this, Value);
		}

		void IObjectValueSource.SetRawValue (ObjectPath path, object value)
		{
			Value = ctx.Adapter.FromRawValue (ctx, value);
		}

		ObjectValue[] IObjectValueSource.GetChildren (ObjectPath path, int index, int count)
		{
			return GetChildren (path, index, count);
		}
		
		public virtual string CallToString ( )
		{
			return ctx.Adapter.CallToString (ctx, Value);
		}

		public virtual ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			try {
				return ctx.Adapter.GetObjectValueChildren (GetChildrenContext (), this, Value, index, count);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return new ObjectValue [] { Mono.Debugging.Client.ObjectValue.CreateFatalError ("", ex.Message, ObjectValueFlags.ReadOnly) };
			}
		}

		public virtual IEnumerable<ValueReference> GetChildReferences ( )
		{
			try {
				object val = Value;
				if (ctx.Adapter.IsClassInstance (Context, val))
					return ctx.Adapter.GetMembersSorted (GetChildrenContext (), this, ctx.Adapter.GetValueType (Context, val), val);
			} catch {
				// Ignore
			}
			return new ValueReference [0];
		}
		
		public IObjectSource ParentSource { get; internal set; }

		EvaluationContext GetChildrenContext ( )
		{
			EvaluationContext newCtx = Context.Clone ();
			EvaluationOptions ops = originalOptions;
			ops.EvaluationTimeout = originalOptions.MemberEvaluationTimeout;
			newCtx.Options = ops;
			return newCtx;
		}

		public virtual ValueReference GetChild (ObjectPath vpath)
		{
			if (vpath.Length == 0)
				return this;

			ValueReference val = GetChild (vpath[0]);
			if (val != null)
				return val.GetChild (vpath.GetSubpath (1));
			else
				return null;
		}

		public virtual ValueReference GetChild (string name)
		{
			object obj = Value;
			
			if (obj == null)
				return null;

			if (ctx.Adapter.IsArray (Context, obj)) {
				// Parse the array indices
				string[] sinds = name.Substring (1, name.Length - 2).Split (',');
				int[] indices = new int [sinds.Length];
				for (int n=0; n<sinds.Length; n++)
					indices [n] = int.Parse (sinds [n]);

				return new ArrayValueReference (ctx, obj, indices);
			}
			
			if (ctx.Adapter.IsClassInstance (Context, obj)) {
				ValueReference val = ctx.Adapter.GetMember (GetChildrenContext (), this, obj, name);
				return val;
			}
					
			return null;
		}
	}
}
