// LocalsPad.cs
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

namespace MonoDevelop.Debugger
{
	public class LocalsPad : ObjectValuePad
	{
		static readonly bool EnableFakeNodes;

		static LocalsPad ()
		{
			var env = Environment.GetEnvironmentVariable ("VSMAC_DEBUGGER_TESTING");

			if (!string.IsNullOrEmpty (env)) {
				var options = env.Split (new char [] { ',' });

				for (int i = 0; i < options.Length; i++) {
					var option = options[i].Trim ();

					if (option == "fake-locals") {
						EnableFakeNodes = true;
						return;
					}
				}
			}

			EnableFakeNodes = false;
		}

		public LocalsPad ()
		{
			if (UseNewTreeView) {
				controller.AllowEditing = true;
			} else {
				tree.AllowEditing = true;
				tree.AllowAdding = false;
			}
		}

		void AddFakeNodes ()
		{
			var xx = new List<ObjectValueNode> ();

			xx.Add (new FakeObjectValueNode ("f1"));
			xx.Add (new FakeIsImplicitNotSupportedObjectValueNode ());

			xx.Add (new FakeEvaluatingGroupObjectValueNode (1));
			xx.Add (new FakeEvaluatingGroupObjectValueNode (0));
			xx.Add (new FakeEvaluatingGroupObjectValueNode (5));

			xx.Add (new FakeEvaluatingObjectValueNode ());
			xx.Add (new FakeEnumerableObjectValueNode (10));
			xx.Add (new FakeEnumerableObjectValueNode (20));
			xx.Add (new FakeEnumerableObjectValueNode (23));

			controller.AddValues (xx);
		}

		void ReloadValues (bool frameChanged)
		{
			var frame = DebuggingService.CurrentFrame;

			if (frame == null)
				return;

			ObjectValue[] locals;
			TimeSpan elapsed;

			using (var timer = frame.DebuggerSession.LocalVariableStats.StartTimer ()) {
				try {
					locals = frame.GetAllLocals ();
					timer.Stop (true);
				} catch {
					locals = new ObjectValue[0];
					timer.Stop (false);
				}

				elapsed = timer.Elapsed;
			}

			if (frameChanged) {
				var metadata = new Dictionary<string, object> ();
				metadata["LocalsCount"] = locals.Length;
				metadata["Elapsed"] = elapsed.TotalMilliseconds;

				Counters.LocalsPadFrameChanged.Inc (1, null, metadata);
			}

			DebuggerLoggingService.LogMessage ("Begin Local Variables:");
			foreach (var local in locals)
				DebuggerLoggingService.LogMessage ("\t{0}", local.Name);
			DebuggerLoggingService.LogMessage ("End Local Variables");

			if (UseNewTreeView) {
				_treeview.BeginUpdates ();
				try {
					controller.ClearValues ();
					controller.AddValues (locals);
				} finally {
					_treeview.EndUpdates ();
				}

				if (EnableFakeNodes)
					AddFakeNodes ();
			} else {
				tree.ClearValues ();
				tree.AddValues (locals);
			}
		}

		public override void OnUpdateFrame ()
		{
			base.OnUpdateFrame ();
			ReloadValues (true);
		}

		public override void OnUpdateValues ()
		{
			base.OnUpdateValues ();
			ReloadValues (false);
		}
	}
}
