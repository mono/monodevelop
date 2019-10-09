//
// VsCodeDebuggerEvaluationContext.cs
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

using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using VsStackFrameFormat = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrameFormat;
using VsStackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	public class VsCodeDebuggerEvaluationContext : EvaluationContext
	{
		public VsCodeDebuggerEvaluationContext (VsCodeDebuggerSession session, VsStackFrame frame, int threadId, EvaluationOptions options) : base (options)
		{
			Adapter = session.Adapter;
			Session = session;
			ThreadId = threadId;
			Frame = frame;
		}

		public VsCodeDebuggerSession Session {
			get; private set;
		}

		public VsStackFrame Frame {
			get; private set;
		}

		public int ThreadId {
			get; private set;
		}

		public override void CopyFrom (EvaluationContext ctx)
		{
			base.CopyFrom (ctx);

			var other = (VsCodeDebuggerEvaluationContext) ctx;
			Session = other.Session;
			ThreadId = other.ThreadId;
			Frame = other.Frame;
		}
	}
}
