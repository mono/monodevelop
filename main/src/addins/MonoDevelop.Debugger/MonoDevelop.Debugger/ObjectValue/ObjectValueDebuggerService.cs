//
// ObjectValueDebuggerService.cs
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

namespace MonoDevelop.Debugger
{
	sealed class ProxyDebuggerService : IDebuggerService
	{
		public bool IsConnected => DebuggingService.IsConnected;

		public bool IsPaused => DebuggingService.IsPaused;

		public void NotifyVariableChanged ()
		{
			DebuggingService.NotifyVariableChanged ();
		}

		public bool HasValueVisualizers (ObjectValueNode node)
		{
			var val = node.GetDebuggerObjectValue ();
			if (val != null) {
				return DebuggingService.HasValueVisualizers (val);
			}

			return false;
		}

		public bool HasInlineVisualizer (ObjectValueNode node)
		{
			var val = node.GetDebuggerObjectValue ();
			if (val != null) {
				return DebuggingService.HasInlineVisualizer (val);
			}

			return false;
		}

		public bool ShowValueVisualizer (ObjectValueNode node)
		{
			var val = node.GetDebuggerObjectValue ();
			if (val != null) {
				return DebuggingService.ShowValueVisualizer (val);
			}

			return false;
		}
	}
}
