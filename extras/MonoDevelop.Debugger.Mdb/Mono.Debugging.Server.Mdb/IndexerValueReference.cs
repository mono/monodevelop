// IndexerValueReference.cs
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
using System.Reflection;
using Mono.Debugger;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;
using System.Collections.Generic;
using Mono.Debugging.Evaluation;
using System.Text;

namespace DebuggerServer
{
	public class IndexerValueReference: ValueReference
	{
		TargetPropertyInfo indexer;
		TargetStructObject target;
		TargetObject[] index;
		
		public IndexerValueReference (EvaluationContext ctx, TargetStructObject target, TargetObject[] index, TargetPropertyInfo indexerProp): base (ctx)
		{
			this.indexer = indexerProp;
			this.target = target;
			this.index = index;
		}
		
		public static ValueReference CreateIndexerValueReference (MdbEvaluationContext ctx, TargetObject target, TargetObject[] index)
		{
			TargetFundamentalObject mstr = target as TargetFundamentalObject;
			if (mstr != null && mstr.TypeName == "string") {
				// Special case for strings
				string name = "[" + ctx.Evaluator.TargetObjectToExpression (ctx, index[0]) + "]";
				string val = (string) mstr.GetObject (ctx.Thread);
				object oo = ctx.Adapter.TargetObjectToObject (ctx, index[0]);
				int idx = (int) Convert.ChangeType (oo, typeof(int));
				return LiteralValueReference.CreateObjectLiteral (ctx, name, val [idx]);
			}
			
			TargetStructObject sob = target as TargetStructObject;
			if (sob == null)
				return null;
			
			TargetPropertyInfo indexerProp = null;
			foreach (MemberReference mem in ObjectUtil.GetTypeMembers (ctx, target.Type, false, true, true, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
				if (mem.Member.IsStatic)
					continue;
				if (mem.Member is TargetPropertyInfo) {
					TargetPropertyInfo prop = (TargetPropertyInfo) mem.Member;
					if (prop.CanRead && prop.Getter.ParameterTypes.Length == 1) {
						indexerProp = prop;
						break;
					}
				}
			}
			if (indexerProp != null)
				return new IndexerValueReference (ctx, sob, index, indexerProp);
			else
				return null;
		}
		
		public override object Value {
			get {
				MdbEvaluationContext ctx = (MdbEvaluationContext) Context;
				return ObjectUtil.GetRealObject (ctx, Server.Instance.RuntimeInvoke (ctx, indexer.Getter, target, index));
			}
			set {
				MdbEvaluationContext ctx = (MdbEvaluationContext) Context;
				TargetObject[] cparams = new TargetObject [index.Length + 1];
				for (int n=0; n<index.Length; n++)
					cparams[n] = TargetObjectConvert.Cast (ctx, index[n], indexer.Setter.ParameterTypes [n]);
				cparams [cparams.Length - 1] = TargetObjectConvert.Cast (ctx, (TargetObject) value, indexer.Setter.ParameterTypes [cparams.Length - 1]);
				Server.Instance.RuntimeInvoke (ctx, indexer.Setter, target, cparams);
			}
		}
		
		protected override bool CanEvaluate (EvaluationOptions options)
		{
			return options.AllowTargetInvoke;
		}
		
		public override object Type {
			get {
				if (indexer.CanRead)
					return indexer.Getter.ReturnType;
				else
					return indexer.Setter.ParameterTypes [1];
			}
		}

		
		public override string Name {
			get {
				StringBuilder sb = new StringBuilder ("[");
				for (int n=0; n<index.Length; n++) {
					if (n > 0)
						sb.Append (',');
					sb.Append (Server.Instance.Evaluator.TargetObjectToExpression (Context, index[n]));
				}
				sb.Append (']');
				return sb.ToString ();
			}
		}

		
		public override ObjectValueFlags Flags {
			get {
				if (!indexer.CanWrite)
					return ObjectValueFlags.ArrayElement | ObjectValueFlags.ReadOnly;
				else
					return ObjectValueFlags.ArrayElement;
			}
		}
	}
}
