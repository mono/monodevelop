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
		
		public EvaluationContext GetContext (EvaluationOptions options)
		{
			return ctx.WithOptions (options);
		}

		public ObjectValue CreateObjectValue (bool withTimeout)
		{
			return CreateObjectValue (withTimeout, Context.Options);
		}
		
		public ObjectValue CreateObjectValue (bool withTimeout, EvaluationOptions options)
		{
			if (!CanEvaluate (options))
				return DC.ObjectValue.CreateImplicitNotSupported (this, new ObjectPath (Name), ctx.Adapter.GetTypeName (GetContext (options), Type), Flags);
			if (withTimeout) {
				return ctx.Adapter.CreateObjectValueAsync (Name, Flags, delegate {
					return CreateObjectValue (options);
				});
			} else
				return CreateObjectValue (options);
		}
		
		public ObjectValue CreateObjectValue (EvaluationOptions options)
		{
			if (!CanEvaluate (options))
				return DC.ObjectValue.CreateImplicitNotSupported (this, new ObjectPath (Name), ctx.Adapter.GetTypeName (GetContext (options), Type), Flags);
			
			Connect ();
			try {
				return OnCreateObjectValue (options);
			} catch (ImplicitEvaluationDisabledException) {
				return DC.ObjectValue.CreateImplicitNotSupported (this, new ObjectPath (Name), ctx.Adapter.GetTypeName (GetContext (options), Type), Flags);
			} catch (NotSupportedExpressionException ex) {
				return DC.ObjectValue.CreateNotSupported (this, new ObjectPath (Name), ex.Message, ctx.Adapter.GetTypeName (GetContext (options), Type), Flags);
			} catch (EvaluatorException ex) {
				return DC.ObjectValue.CreateError (this, new ObjectPath (Name), "", ex.Message, Flags);
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				return DC.ObjectValue.CreateUnknown (Name);
			}
		}
		
		protected virtual bool CanEvaluate (EvaluationOptions options)
		{
			return true;
		}
		
		protected virtual ObjectValue OnCreateObjectValue (EvaluationOptions options)
		{
			string name = Name;
			if (string.IsNullOrEmpty (name))
				name = "?";
			
			EvaluationContext newCtx = GetContext (options);
			EvaluationContext oldCtx = Context;
			object val = null;
			
			try {
				// Note: The Value property implementation may make use of the EvaluationOptions,
				// so we need to override our context temporarily to do the evaluation.
				ctx = newCtx;
				val = Value;
			} finally {
				ctx = oldCtx;
			}
			
			if (val != null)
				return newCtx.Adapter.CreateObjectValue (newCtx, this, new ObjectPath (name), val, Flags);
			else
				return Mono.Debugging.Client.ObjectValue.CreateNullObject (this, name, newCtx.Adapter.GetTypeName (newCtx, Type), Flags);
		}

		ObjectValue IObjectValueSource.GetValue (ObjectPath path, EvaluationOptions options)
		{
			return CreateObjectValue (true, options);
		}
		
		EvaluationResult IObjectValueSource.SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			try {
				ctx.WaitRuntimeInvokes ();
				EvaluationContext cctx = GetContext (options);
				cctx.Options.AllowMethodEvaluation = true;
				cctx.Options.AllowTargetInvoke = true;
				ValueReference vref = cctx.Evaluator.Evaluate (cctx, value, Type);
				object newValue = cctx.Adapter.Convert (cctx, vref.Value, Type);
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
		
		object IObjectValueSource.GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			return ctx.Adapter.ToRawValue (GetContext (options), this, Value);
		}

		void IObjectValueSource.SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			Value = ctx.Adapter.FromRawValue (GetContext (options), value);
		}

		ObjectValue[] IObjectValueSource.GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			return GetChildren (path, index, count, options);
		}
		
		public virtual string CallToString ()
		{
			return ctx.Adapter.CallToString (ctx, Value);
		}

		public virtual ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			try {
				return ctx.Adapter.GetObjectValueChildren (GetChildrenContext (options), this, Value, index, count);
			} catch (Exception ex) {
				return new ObjectValue [] { Mono.Debugging.Client.ObjectValue.CreateFatalError ("", ex.Message, ObjectValueFlags.ReadOnly) };
			}
		}

		public virtual IEnumerable<ValueReference> GetChildReferences (EvaluationOptions options)
		{
			try {
				object val = Value;
				if (ctx.Adapter.IsClassInstance (Context, val))
					return ctx.Adapter.GetMembersSorted (GetChildrenContext (options), this, Type, val);
			} catch {
				// Ignore
			}
			return new ValueReference [0];
		}
		
		public IObjectSource ParentSource { get; internal set; }

		protected EvaluationContext GetChildrenContext (EvaluationOptions options)
		{
			EvaluationContext newCtx = ctx.Clone ();
			if (options != null)
				newCtx.Options = options;
			newCtx.Options.EvaluationTimeout = originalOptions.MemberEvaluationTimeout;
			return newCtx;
		}

		public virtual ValueReference GetChild (ObjectPath vpath, EvaluationOptions options)
		{
			if (vpath.Length == 0)
				return this;

			ValueReference val = GetChild (vpath[0], options);
			if (val != null)
				return val.GetChild (vpath.GetSubpath (1), options);
			else
				return null;
		}

		public virtual ValueReference GetChild (string name, EvaluationOptions options)
		{
			object obj = Value;
			
			if (obj == null)
				return null;

			if (name[0] == '[' && ctx.Adapter.IsArray (Context, obj)) {
				// Parse the array indices
				string[] sinds = name.Substring (1, name.Length - 2).Split (',');
				int[] indices = new int [sinds.Length];
				for (int n=0; n<sinds.Length; n++)
					indices [n] = int.Parse (sinds [n]);

				return new ArrayValueReference (ctx, obj, indices);
			}
			
			if (ctx.Adapter.IsClassInstance (Context, obj))
				return ctx.Adapter.GetMember (GetChildrenContext (options), this, obj, name);

			return null;
		}
	}
}
