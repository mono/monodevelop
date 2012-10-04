// 
// ExceptionInfoSource.cs
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
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mono.Debugging.Evaluation
{
	public class ExceptionInfoSource
	{
		ValueReference exception;
		EvaluationContext ctx;
		
		public ExceptionInfoSource (EvaluationContext ctx, ValueReference exception)
		{
			this.exception = exception;
			this.ctx = ctx;
		}
		
		public ValueReference Exception {
			get { return this.exception; }
		}
		
		public ObjectValue CreateObjectValue (bool withTimeout, EvaluationOptions options)
		{
			string type = ctx.Adapter.GetTypeName (ctx, exception.Type);
			
			ObjectValue excInstance = exception.CreateObjectValue (withTimeout, options);
			excInstance.Name = "Instance";
			
			ObjectValue messageValue = null;
			
			// Get the message
			
			if (withTimeout) {
				messageValue = ctx.Adapter.CreateObjectValueAsync ("Message", ObjectValueFlags.None, delegate {
					ValueReference mref = exception.GetChild ("Message", options);
					if (mref != null) {
						string val = (string) mref.ObjectValue;
						return ObjectValue.CreatePrimitive (null, new ObjectPath ("Message"), "System.String", new EvaluationResult (val), ObjectValueFlags.Literal);
					}
					else
						return ObjectValue.CreateUnknown ("Message");
				});
			} else {
				ValueReference mref = exception.GetChild ("Message", options);
				if (mref != null) {
					string val = (string) mref.ObjectValue;
					messageValue = ObjectValue.CreatePrimitive (null, new ObjectPath ("Message"), "System.String", new EvaluationResult (val), ObjectValueFlags.Literal);
				}
			}
			if (messageValue == null)
				messageValue = ObjectValue.CreateUnknown ("Message");
			
			messageValue.Name = "Message";

			// Inner exception
			
			ObjectValue childExceptionValue = null;
			
			if (withTimeout) {
				childExceptionValue = ctx.Adapter.CreateObjectValueAsync ("InnerException", ObjectValueFlags.None, delegate {
					ValueReference inner = exception.GetChild ("InnerException", options);
					if (inner != null && !ctx.Adapter.IsNull (ctx, inner.Value)) {
						//Console.WriteLine ("pp got child:" + type);
						ExceptionInfoSource innerSource = new ExceptionInfoSource (ctx, inner);
						ObjectValue res = innerSource.CreateObjectValue (false, options);
						return res;
					}
					else
						return ObjectValue.CreateUnknown ("InnerException");
				});
			} else {
				ValueReference inner = exception.GetChild ("InnerException", options);
				if (inner != null && !ctx.Adapter.IsNull (ctx, inner.Value)) {
					//Console.WriteLine ("pp got child:" + type);
					ExceptionInfoSource innerSource = new ExceptionInfoSource (ctx, inner);
					childExceptionValue = innerSource.CreateObjectValue (false, options);
					childExceptionValue.Name = "InnerException";
				}
			}
			if (childExceptionValue == null)
				childExceptionValue = ObjectValue.CreateUnknown ("InnerException");
			
			// Stack trace
			
			ObjectValue stackTraceValue;
			if (withTimeout) {
				stackTraceValue = ctx.Adapter.CreateObjectValueAsync ("StackTrace", ObjectValueFlags.None, delegate {
					return GetStackTrace (options);
				});
			} else
				stackTraceValue = GetStackTrace (options);
			
			ObjectValue[] children = new ObjectValue [] { excInstance, messageValue, stackTraceValue, childExceptionValue };
			return ObjectValue.CreateObject (null, new ObjectPath ("InnerException"), type, "", ObjectValueFlags.None, children);
		}
		
		ObjectValue GetStackTrace (EvaluationOptions options)
		{
			ValueReference st = exception.GetChild ("StackTrace", options);
			if (st == null)
				return ObjectValue.CreateUnknown ("StackTrace");
			string trace = st.ObjectValue as string;
			if (trace == null)
				return ObjectValue.CreateUnknown ("StackTrace");
			
			List<ObjectValue> frames = new List<ObjectValue> ();

			var regex = new Regex ("at (.*) in (.*):(.*)");
			
			foreach (string sframe in trace.Split ('\n')) {
				string txt = sframe.Trim (' ', '\r','\n');
				string file = "";
				int line = 0;
				int col = 0;
				var match = regex.Match (sframe);
				if (match.Success) {
					txt = match.Groups [1].ToString ();
					file = match.Groups [2].ToString ();
					int.TryParse (match.Groups [3].ToString (), out line);
				}
				ObjectValue fileVal = ObjectValue.CreatePrimitive (null, new ObjectPath("File"), "", new EvaluationResult (file), ObjectValueFlags.None);
				ObjectValue lineVal = ObjectValue.CreatePrimitive (null, new ObjectPath("Line"), "", new EvaluationResult (line.ToString ()), ObjectValueFlags.None);
				ObjectValue colVal = ObjectValue.CreatePrimitive (null, new ObjectPath("Column"), "", new EvaluationResult (col.ToString ()), ObjectValueFlags.None);
				ObjectValue[] children = new ObjectValue[] { fileVal, lineVal, colVal };
				ObjectValue frame = ObjectValue.CreateObject (null, new ObjectPath (), "", new EvaluationResult (txt), ObjectValueFlags.None, children);
				frames.Add (frame);
			}
			return ObjectValue.CreateArray (null, new ObjectPath ("StackTrace"), "", frames.Count,ObjectValueFlags.None, frames.ToArray ());
		}
	}
}

