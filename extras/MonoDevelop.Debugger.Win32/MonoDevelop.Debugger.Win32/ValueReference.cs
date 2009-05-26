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
using Microsoft.Samples.Debugging.CorDebug;
using DC = Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Evaluation
{
	public abstract class ValueReference<TValue, TType>: RemoteFrameObject, IObjectValueSource
		where TValue: class
		where TType: class
	{
		EvaluationContext<TValue, TType> ctx;

		public ValueReference (EvaluationContext<TValue, TType> ctx)
		{
			this.ctx = ctx;
		}
		
		public virtual object ObjectValue {
			get {
				TValue ob = Value;
				if (ctx.Adapter.IsNull (Context, ob))
					return null;
				else if (ctx.Adapter.IsPrimitive (Context, ob))
					return ctx.Adapter.TargetObjectToObject (ctx, ob);
				else
					return ob;
			}
		}
		
		public abstract TValue Value { get; set; }
		public abstract string Name { get; }
		public abstract TType Type { get; }
		public abstract ObjectValueFlags Flags { get; }

		public EvaluationContext<TValue, TType> Context
		{
			get {
				return ctx;
			}
		}

		public ObjectValue CreateObjectValue (bool withTimeout)
		{
			if (withTimeout) {
				return ctx.Adapter.CreateObjectValueAsync (Name, Flags, delegate {
					return CreateObjectValue ();
				});
			} else
				return CreateObjectValue ();
		}
		
		public ObjectValue CreateObjectValue ()
		{
			Connect ();
			try {
				return OnCreateObjectValue ();
			} catch (NotSupportedExpressionException ex) {
				return DC.ObjectValue.CreateNotSupported (Name, ex.Message, Flags);
			} catch (EvaluatorException ex) {
				return DC.ObjectValue.CreateError (Name, ex.Message, Flags);
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				return DC.ObjectValue.CreateUnknown (Name);
			}
		}
		
		protected virtual ObjectValue OnCreateObjectValue ()
		{
			string name = Name;
			if (string.IsNullOrEmpty (name))
				name = "?";
			
			TValue val = Value;
			if (val != null)
				return ctx.Adapter.CreateObjectValue (ctx, this, new ObjectPath (name), val, Flags);
			else
				return Mono.Debugging.Client.ObjectValue.CreateNullObject (name, ctx.Adapter.GetTypeName (Context, Type), Flags);
		}

		string IObjectValueSource.SetValue (ObjectPath path, string value)
		{
			try {
				ctx.WaitRuntimeInvokes ();
				EvaluationOptions<TType> ops = new EvaluationOptions<TType> ();
				ops.ExpectedType = Type;
				ops.CanEvaluateMethods = true;
				ValueReference<TValue,TType> vref = ctx.Evaluator.Evaluate (ctx, value, ops);
				TValue newValue = vref.Value;
				newValue = ctx.Adapter.Cast (ctx, newValue, Type);
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
			
			return value;
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
				return ctx.Adapter.GetObjectValueChildren (GetChildrenContext (), Value, index, count);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return new ObjectValue [] { Mono.Debugging.Client.ObjectValue.CreateError ("", ex.Message, ObjectValueFlags.ReadOnly) };
			}
		}

		public virtual IEnumerable<ValueReference<TValue, TType>> GetChildReferences ( )
		{
			try {
				TValue val = Value;
				if (ctx.Adapter.IsClassInstance (Context, val))
					return ctx.Adapter.GetMembers (GetChildrenContext (), ctx.Adapter.GetValueType (Context, val), val);
			} catch {
				// Ignore
			}
			return new ValueReference<TValue, TType>[0];
		}

		EvaluationContext<TValue, TType> GetChildrenContext ( )
		{
			int to = ctx.Adapter.DefaultChildEvaluationTimeout;
			EvaluationContext<TValue, TType> newCtx = Context.Clone ();
			newCtx.Timeout = to;
			return newCtx;
		}

		public virtual ValueReference<TValue, TType> GetChild (ObjectPath vpath)
		{
			if (vpath.Length == 0)
				return this;

			ValueReference<TValue, TType> val = GetChild (vpath[0]);
			if (val != null)
				return val.GetChild (vpath.GetSubpath (1));
			else
				return null;
		}

		public virtual ValueReference<TValue,TType> GetChild (string name)
		{
			TValue obj = Value;
			
			if (obj == null)
				return null;

			if (ctx.Adapter.IsArray (Context, obj)) {
				// Parse the array indices
				string[] sinds = name.Substring (1, name.Length - 2).Split (',');
				int[] indices = new int [sinds.Length];
				for (int n=0; n<sinds.Length; n++)
					indices [n] = int.Parse (sinds [n]);

				return new ArrayValueReference<TValue, TType> (ctx, obj, indices);
			}

			if (ctx.Adapter.IsClassInstance (Context, obj)) {
				foreach (ValueReference<TValue, TType> val in ctx.Adapter.GetMembers (GetChildrenContext (), ctx.Adapter.GetValueType (Context, obj), obj)) {
					if (val.Name == name)
						return val;
				}
				return null;
			}
					
			return null;
		}
	}
}
