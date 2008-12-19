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
using Mono.Debugger.Languages;
using Mono.Debugger;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using MD = Mono.Debugger;
using DC = Mono.Debugging.Client;
	
namespace DebuggerServer
{
	public abstract class ValueReference: RemoteFrameObject, IObjectValueSource
	{
		EvaluationContext ctx;
		
		public ValueReference (EvaluationContext ctx)
		{
			this.ctx = ctx;
		}
		
		public virtual object ObjectValue {
			get {
				TargetObject ob = Value;
				if (ob is TargetFundamentalObject) {
					TargetFundamentalObject fob = (TargetFundamentalObject) ob;
					return fob.GetObject (ctx.Thread);
				} else
					return ob;
			}
		}
		
		public abstract TargetObject Value { get; set; }
		public abstract string Name { get; }
		public abstract TargetType Type { get; }
		public abstract ObjectValueFlags Flags { get; }

		public EvaluationContext Context {
			get {
				return ctx;
			}
		}

		public ObjectValue CreateObjectValue (bool withTimeout)
		{
			if (withTimeout) {
				return Server.Instance.AsyncEvaluationTracker.Run (Name, Flags, delegate {
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
				Server.Instance.WriteDebuggerError (ex);
				return DC.ObjectValue.CreateUnknown (Name);
			}
		}
		
		protected virtual ObjectValue OnCreateObjectValue ()
		{
			string name = Name;
			if (string.IsNullOrEmpty (name))
				name = "?";
			
			TargetObject val = Value;
			if (val != null)
				return Util.CreateObjectValue (ctx, this, new ObjectPath (name), Value, Flags);
			else
				return Mono.Debugging.Client.ObjectValue.CreateNullObject (name, Type.Name, Flags);
		}
		
		public string SetValue (ObjectPath path, string value)
		{
			try {
				Server.Instance.WaitRuntimeInvokes ();
				EvaluationOptions ops = new EvaluationOptions ();
				ops.ExpectedType = Type;
				ops.CanEvaluateMethods = true;
				ValueReference vref = Server.Instance.Evaluator.Evaluate (ctx, value, ops);
				TargetObject newValue = vref.Value;
				newValue = TargetObjectConvert.Cast (ctx, newValue, Type);
				Value = newValue;
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerError (ex);
				Server.Instance.WriteDebuggerOutput ("Value assignment failed: {0}: {1}\n", ex.GetType (), ex.Message);
			}
			
			try {
				return Server.Instance.Evaluator.TargetObjectToExpression (ctx, Value);
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerError (ex);
				Server.Instance.WriteDebuggerOutput ("Value assignment failed: {0}: {1}\n", ex.GetType (), ex.Message);
			}
			
			return value;
		}
		
		public virtual string CallToString ()
		{
			object val = ObjectValue;
			if (val is TargetStructObject)
				return ObjectUtil.CallToString (ctx, (TargetStructObject) val);
			else if (val == null)
				return string.Empty;
			else
				return val.ToString ();
		}
		
		public virtual ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			try {
				return Util.GetObjectValueChildren (GetChildrenContext (), Value, index, count);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return new ObjectValue [] { Mono.Debugging.Client.ObjectValue.CreateError ("", ex.Message, ObjectValueFlags.ReadOnly) };
			}
		}
		
		public virtual IEnumerable<ValueReference> GetChildReferences ()
		{
			try {
				TargetStructObject val = Value as TargetStructObject;
				if (val != null)
					return Util.GetMembers (GetChildrenContext (), val.Type, val);
			} catch {
				// Ignore
			}
			return new ValueReference [0];
		}

		EvaluationContext GetChildrenContext ()
		{
			int to = DebuggerServer.DefaultChildEvaluationTimeout;
			return new EvaluationContext (ctx.Thread, ctx.Frame, to);
		}
		
		public virtual ValueReference GetChild (string name)
		{
			TargetObject obj = Value;
			
			if (obj == null)
				return null;
			
			obj = ObjectUtil.GetRealObject (ctx, obj);
			
			if (obj == null)
				return null;
			
			switch (obj.Kind)
			{
				case TargetObjectKind.Array: {
					
					TargetArrayObject arr = obj as TargetArrayObject;
					if (arr == null)
						return null;
					
					// Parse the array indices
					string[] sinds = name.Substring (1, name.Length - 2).Split (',');
					int[] indices = new int [sinds.Length];
					for (int n=0; n<sinds.Length; n++)
						indices [n] = int.Parse (sinds [n]);
					
					return new ArrayValueReference (ctx, arr, indices);
				}
					
				case TargetObjectKind.Struct: 
				case TargetObjectKind.GenericInstance:
				case TargetObjectKind.Class: {
					TargetStructObject co = obj as TargetStructObject;
					if (co == null)
						return null;
					foreach (ValueReference val in Util.GetMembers (GetChildrenContext (), co.Type, co)) {
						if (val.Name == name)
							return val;
					}
					return null;
				}
					
				default:
					return null;
			}
		}
	}
}
