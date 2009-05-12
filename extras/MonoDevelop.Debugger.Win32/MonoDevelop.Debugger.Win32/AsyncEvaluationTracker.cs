// AsyncEvaluationTracker.cs
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
using Mono.Debugging.Backend;

namespace MonoDevelop.Debugger.Evaluation
{
	public delegate ObjectValue ObjectEvaluatorDelegate ();

	public class AsyncEvaluationTracker: RemoteFrameObject, IObjectValueUpdater
	{
		Dictionary<string, UpdateCallback> asyncCallbacks = new Dictionary<string, UpdateCallback> ();
		Dictionary<string, ObjectValue> asyncResults = new Dictionary<string, ObjectValue> ();
		int asyncCounter = 0;
		int cancelTimestamp = 0;
		TimedEvaluator runner = new TimedEvaluator ();

		public ObjectValue Run (string name, ObjectValueFlags flags, ObjectEvaluatorDelegate evaluator)
		{
			string id;
			int tid;
			lock (asyncCallbacks) {
				tid = asyncCounter++;
				id = tid.ToString ();
			}
			
			ObjectValue val = null;
			bool done = runner.Run (delegate {
					if (tid >= cancelTimestamp)
						val = evaluator ();
			},
			delegate {
				if (tid >= cancelTimestamp)
					OnEvaluationDone (id, val);
			});
			
			if (done)
				return val;
			else
				return ObjectValue.CreateEvaluating (this, new ObjectPath (id, name), flags);
		}

		public void Stop ()
		{
			lock (asyncCallbacks) {
				cancelTimestamp = asyncCounter;
				runner.CancelAll ();
				asyncCallbacks.Clear ();
				asyncResults.Clear ();
			}
		}

		public void WaitForStopped ()
		{
			runner.WaitForStopped ();
		}

		void OnEvaluationDone (string id, ObjectValue val)
		{
			if (val == null)
				val = ObjectValue.CreateUnknown (null);
			UpdateCallback cb = null;
			lock (asyncCallbacks) {
				if (asyncCallbacks.TryGetValue (id, out cb)) {
					cb.UpdateValue (val);
					asyncCallbacks.Remove (id);
				}
				else
					asyncResults [id] = val;
			}
		}
		
		void IObjectValueUpdater.RegisterUpdateCallbacks (UpdateCallback[] callbacks)
		{
			foreach (UpdateCallback c in callbacks) {
				lock (asyncCallbacks) {
					ObjectValue val;
					string id = c.Path[0];
					if (asyncResults.TryGetValue (id, out val)) {
						c.UpdateValue (val);
						asyncResults.Remove (id);
					} else {
						asyncCallbacks [id] = c;
					}
				}
			}
		}
	}
}
