// 
// RemoteRawValue.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Debugging.Backend;
using System.Collections;
using Mono.Debugging.Client;

namespace Mono.Debugging.Evaluation
{
	internal class RemoteRawValue: RemoteFrameObject, IRawValue
	{
		object targetObject;
		EvaluationContext ctx;
		IObjectSource source;
		
		public RemoteRawValue (EvaluationContext gctx, IObjectSource source, object targetObject)
		{
			this.ctx = gctx.Clone ();
			ctx.Options.AllowTargetInvoke = true;
			ctx.Options.AllowMethodEvaluation = true;
			this.targetObject = targetObject;
			this.source = source;
			Connect ();
		}
		
		public object TargetObject {
			get { return this.targetObject; }
		}
		
		#region IRawValue implementation
		public object CallMethod (string name, object[] parameters, EvaluationOptions options)
		{
			EvaluationContext localContext = ctx.WithOptions (options);
			
			object[] argValues = new object [parameters.Length];
			object[] argTypes = new object [parameters.Length];
			for (int n=0; n<argValues.Length; n++) {
				argValues[n] = localContext.Adapter.FromRawValue (localContext, parameters[n]);
				argTypes[n] = localContext.Adapter.GetValueType (localContext, argValues[n]);
			}
			object type = localContext.Adapter.GetValueType (localContext, targetObject);
			object res = localContext.Adapter.RuntimeInvoke (localContext, type, targetObject, name, argTypes, argValues);
			return localContext.Adapter.ToRawValue (localContext, null, res);
		}
		
		public object GetMemberValue (string name, EvaluationOptions options)
		{
			EvaluationContext localContext = ctx.WithOptions (options);
			object type = localContext.Adapter.GetValueType (localContext, targetObject);
			ValueReference val = localContext.Adapter.GetMember (localContext, source, type, targetObject, name);
			if (val == null)
				throw new EvaluatorException ("Member '{0}' not found", name);
			return localContext.Adapter.ToRawValue (localContext, val, val.Value);
		}
		
		public void SetMemberValue (string name, object value, EvaluationOptions options)
		{
			EvaluationContext localContext = ctx.WithOptions (options);
			object type = localContext.Adapter.GetValueType (localContext, targetObject);
			ValueReference val = localContext.Adapter.GetMember (localContext, source, type, targetObject, name);
			if (val == null)
				throw new EvaluatorException ("Member '{0}' not found", name);
			val.Value = localContext.Adapter.FromRawValue (localContext, value);
		}
		
		#endregion
	}
	
	internal class RemoteRawValueArray: RemoteFrameObject, IRawValueArray
	{
		object targetObject;
		EvaluationContext ctx;
		IObjectSource source;
		ICollectionAdaptor targetArray;
		
		public RemoteRawValueArray (EvaluationContext ctx, IObjectSource source, ICollectionAdaptor targetArray, object targetObject)
		{
			this.ctx = ctx;
			this.targetArray = targetArray;
			this.targetObject = targetObject;
			this.source = source;
			Connect ();
		}
		
		public object TargetObject {
			get { return this.targetObject; }
		}
		
		public object GetValue (int[] index)
		{
			return ctx.Adapter.ToRawValue (ctx, source, targetArray.GetElement (index));
		}
		
		public void SetValue (int[] index, object value)
		{
			targetArray.SetElement (index, ctx.Adapter.FromRawValue (ctx, value));
		}
		
		public int[] Dimensions {
			get {
				return targetArray.GetDimensions ();
			}
		}
		
		public Array ToArray ()
		{
			ArrayList array = new ArrayList ();
			int[] dims = targetArray.GetDimensions ();
			if (dims.Length != 1)
				throw new NotSupportedException ();
			int[] idx = new int [1];
			Type commonType = null;
			for (int n=0; n<dims[0]; n++) {
				idx[0] = n;
				object rv = ctx.Adapter.ToRawValue (ctx, new ArrayObjectSource (targetArray, idx), targetArray.GetElement (idx));
				if (commonType == null)
					commonType = rv.GetType ();
				else if (commonType != rv.GetType ())
					commonType = typeof(void);
				array.Add (rv);
			}
			if (array.Count > 0 && commonType != typeof(void))
				return array.ToArray (commonType);
			else
				return array.ToArray ();
		}
	}
	
	internal class RemoteRawValueString: RemoteFrameObject, IRawValueString
	{
		object targetObject;
		IStringAdaptor targetString;
		
		public RemoteRawValueString (IStringAdaptor targetString, object targetObject)
		{
			this.targetString = targetString;
			this.targetObject = targetObject;
			Connect ();
		}
		
		public object TargetObject {
			get { return this.targetObject; }
		}
		
		public int Length {
			get { return targetString.Length; }
		}
		
		public string Value {
			get { return targetString.Value; }
		}
		
		public string Substring (int index, int length)
		{
			return targetString.Substring (index, length);
		}
	}
}

