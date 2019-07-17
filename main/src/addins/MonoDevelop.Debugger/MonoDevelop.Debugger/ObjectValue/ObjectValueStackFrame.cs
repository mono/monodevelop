//
// ObjectValueStackFrame.cs
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
using System.Linq;
using System.Collections.Generic;

using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	sealed class ProxyStackFrame : IStackFrame
	{
		readonly Dictionary<string, ObjectValue> cachedValues = new Dictionary<string, ObjectValue> ();

		public ProxyStackFrame (StackFrame frame)
		{
			StackFrame = frame;
		}

		public StackFrame StackFrame {
			get; private set;
		}

		public EvaluationOptions CloneSessionEvaluationOpions ()
		{
			return StackFrame.DebuggerSession.Options.EvaluationOptions.Clone ();
		}

		public ObjectValueNode EvaluateExpression (string expression)
		{
			if (cachedValues.TryGetValue (expression, out var value))
				return new DebuggerObjectValueNode (value);

			if (StackFrame != null)
				value = StackFrame.GetExpressionValue (expression, true);
			else
				value = ObjectValue.CreateUnknown (expression);

			cachedValues[expression] = value;

			return new DebuggerObjectValueNode (value);
		}

		public ObjectValueNode[] EvaluateExpressions (IList<string> expressions)
		{
			var values = new ObjectValue[expressions.Count];
			var unknown = new List<string> ();

			for (int i = 0; i < expressions.Count; i++) {
				if (!cachedValues.TryGetValue (expressions[i], out var value))
					unknown.Add (expressions[i]);
				else
					values[i] = value;
			}

			ObjectValue[] qvalues;

			if (StackFrame != null) {
				qvalues = StackFrame.GetExpressionValues (unknown.ToArray (), true);
			} else {
				qvalues = new ObjectValue[unknown.Count];
				for (int i = 0; i < qvalues.Length; i++)
					qvalues[i] = ObjectValue.CreateUnknown (unknown[i]);
			}

			for (int i = 0, v = 0; i < values.Length; i++) {
				if (values[i] == null) {
					var value = qvalues[v++];

					cachedValues[expressions[i]] = value;
					values[i] = value;
				}
			}

			return values.Select (v => new DebuggerObjectValueNode (v)).ToArray ();
		}
	}
}
