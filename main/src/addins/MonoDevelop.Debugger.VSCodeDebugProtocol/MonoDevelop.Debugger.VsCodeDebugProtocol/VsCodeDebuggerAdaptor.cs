//
// VsCodeDebuggerAdaptor.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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
using System.Reflection;
using System.Collections.Generic;

using Mono.Debugging.Evaluation;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	public class VsCodeDebuggerAdaptor : ObjectValueAdaptor
	{
		readonly VsCodeDebuggerSession session;

		public VsCodeDebuggerAdaptor (VsCodeDebuggerSession session)
		{
			this.session = session;
		}

		protected override ValueReference OnGetLocalVariable (EvaluationContext ctx, string name)
		{
			var cx = (VsCodeDebuggerEvaluationContext) ctx;

			var scopeBody = session.protocolClient.SendRequestSync (new ScopesRequest (cx.Frame.Id));

			foreach (var scope in scopeBody.Scopes) {
				using (var timer = session.EvaluationStats.StartTimer ()) {
					var variables = session.protocolClient.SendRequestSync (new VariablesRequest (scope.VariablesReference));
					foreach (var variable in variables.Variables) {
						if (variable.Name.Equals (name, ctx.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) {

						}

						results.Add (ObjectValue.CreatePrimitive (null, new ObjectPath (variable.Name), variable.Type ?? "<unknown>", new EvaluationResult (variable.Value), ObjectValueFlags.None));
					}
					timer.Success = true;
				}
			}
		}

		protected override ValueReference OnGetThisReference (EvaluationContext ctx)
		{
			return base.OnGetThisReference (ctx);
		}

		public override ICollectionAdaptor CreateArrayAdaptor (EvaluationContext ctx, object arr)
		{
			throw new NotImplementedException ();
		}

		public override object CreateNullValue (EvaluationContext ctx, object type)
		{
			throw new NotImplementedException ();
		}

		public override IStringAdaptor CreateStringAdaptor (EvaluationContext ctx, object str)
		{
			throw new NotImplementedException ();
		}

		public override object CreateValue (EvaluationContext ctx, object value)
		{
			throw new NotImplementedException ();
		}

		public override object CreateValue (EvaluationContext ctx, object type, params object[] args)
		{
			throw new NotImplementedException ();
		}

		public override object GetBaseType (EvaluationContext ctx, object type)
		{
			throw new NotImplementedException ();
		}

		public override object GetType (EvaluationContext ctx, string name, object[] typeArgs)
		{
			throw new NotImplementedException ();
		}

		public override object[] GetTypeArgs (EvaluationContext ctx, object type)
		{
			throw new NotImplementedException ();
		}

		public override string GetTypeName (EvaluationContext ctx, object type)
		{
			throw new NotImplementedException ();
		}

		public override object GetValueType (EvaluationContext ctx, object val)
		{
			throw new NotImplementedException ();
		}

		public override bool HasMember (EvaluationContext ctx, object type, string memberName, BindingFlags bindingFlags)
		{
			throw new NotImplementedException ();
		}

		public override bool HasMethod (EvaluationContext ctx, object targetType, string methodName, object[] genericTypeArgs, object[] argTypes, BindingFlags flags)
		{
			throw new NotImplementedException ();
		}

		public override bool IsArray (EvaluationContext ctx, object val)
		{
			throw new NotImplementedException ();
		}

		public override bool IsClass (EvaluationContext ctx, object type)
		{
			throw new NotImplementedException ();
		}

		public override bool IsEnum (EvaluationContext ctx, object val)
		{
			throw new NotImplementedException ();
		}

		public override bool IsNull (EvaluationContext ctx, object val)
		{
			throw new NotImplementedException ();
		}

		public override bool IsPointer (EvaluationContext ctx, object val)
		{
			throw new NotImplementedException ();
		}

		public override bool IsPrimitive (EvaluationContext ctx, object val)
		{
			throw new NotImplementedException ();
		}

		public override bool IsString (EvaluationContext ctx, object val)
		{
			throw new NotImplementedException ();
		}

		public override bool IsValueType (object type)
		{
			throw new NotImplementedException ();
		}

		public override object RuntimeInvoke (EvaluationContext ctx, object targetType, object target, string methodName, object [] genericTypeArgs, object [] argTypes, object [] argValues)
		{
			throw new NotImplementedException ();
		}

		public override object TryCast (EvaluationContext ctx, object val, object type)
		{
			throw new NotImplementedException ();
		}

		protected override IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, object t, object co, BindingFlags bindingFlags)
		{
			throw new NotImplementedException ();
		}
	}
}
