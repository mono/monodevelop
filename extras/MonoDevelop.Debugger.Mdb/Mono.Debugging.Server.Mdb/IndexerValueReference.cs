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
using Mono.Debugger;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;
using System.Collections.Generic;

namespace DebuggerServer
{
	public class IndexerValueReference: ValueReference
	{
		TargetPropertyInfo indexer;
		TargetStructObject target;
		TargetObject index;
		
		public IndexerValueReference (EvaluationContext ctx, TargetStructObject target, TargetObject index, TargetPropertyInfo indexerProp): base (ctx)
		{
			this.indexer = indexerProp;
			this.target = target;
			this.index = index;
		}
		
		public static IndexerValueReference CreateIndexerValueReference (EvaluationContext ctx, TargetObject target, TargetObject index)
		{
			TargetStructObject sob = target as TargetStructObject;
			if (sob == null)
				return null;
			
			TargetPropertyInfo indexerProp = null;
			foreach (MemberReference mem in ObjectUtil.GetTypeMembers (ctx, target.Type, false, false, true, true, ReqMemberAccess.All)) {
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
		
		public override TargetObject Value {
			get {
				return ObjectUtil.GetRealObject (Context, Server.Instance.RuntimeInvoke (Context, indexer.Getter, target, new TargetObject [] {index}));
			}
			set {
				TargetObject cindex = TargetObjectConvert.Cast (Context, index, indexer.Setter.ParameterTypes [0]);
				TargetObject cvalue = TargetObjectConvert.Cast (Context, value, indexer.Setter.ParameterTypes [1]);
				Server.Instance.RuntimeInvoke (Context, indexer.Setter, target, new TargetObject [] {cindex, cvalue});
			}
		}

		
		public override Mono.Debugger.Languages.TargetType Type {
			get {
				if (indexer.CanRead)
					return indexer.Getter.ReturnType;
				else
					return indexer.Setter.ParameterTypes [1];
			}
		}

		
		public override string Name {
			get {
				return "[" + Server.Instance.Evaluator.TargetObjectToExpression (Context, index) + "]";
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
