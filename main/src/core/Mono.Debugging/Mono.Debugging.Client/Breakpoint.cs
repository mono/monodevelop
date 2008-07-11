// Breakpoint.cs
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

namespace Mono.Debugging.Client
{
	[Serializable]
	public class Breakpoint
	{
		[NonSerialized] BreakpointStore store;
		[NonSerialized] bool enabled = true;
		
		string fileName;
		int line;
		
		HitAction hitAction;
		string customActionId;
		string traceExpression;
		string conditionExpression;
		bool breakIfConditionChanges;
		
		public Breakpoint (string fileName, int line)
		{
			this.fileName = fileName;
			this.line = line;
		}
		
		public bool Enabled {
			get {
				if (store == null)
					throw new InvalidOperationException ();
				return enabled;
			}
			set {
				if (store == null)
					throw new InvalidOperationException ();
				enabled = value;
				store.EnableBreakpoint (this, value);
			}
		}
		
		public bool IsValid (DebuggerSession session)
		{
			if (store == null)
				throw new InvalidOperationException ();
			if (session == null)
				return true;
			return session.IsBreakpointValid (this);
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public int Line {
			get { return line; }
		}

		internal BreakpointStore Store {
			get {
				return store;
			}
			set {
				store = value;
			}
		}

		public string TraceExpression {
			get {
				return traceExpression;
			}
			set {
				traceExpression = value;
			}
		}

		public HitAction HitAction {
			get {
				return hitAction;
			}
			set {
				hitAction = value;
			}
		}

		public string CustomActionId {
			get {
				return customActionId;
			}
			set {
				customActionId = value;
			}
		}

		public string ConditionExpression {
			get {
				return conditionExpression;
			}
			set {
				conditionExpression = value;
			}
		}

		public bool BreakIfConditionChanges {
			get {
				return breakIfConditionChanges;
			}
			set {
				breakIfConditionChanges = value;
			}
		}
		
		public void CommitChanges ()
		{
			if (store != null)
				store.NotifyBreakpointChanged (this);
		}
	}
	
	public enum HitAction
	{
		Break,
		PrintExpression,
		CustomAction
	}
	
	public delegate bool BreakpointHitHandler (string actionId, Breakpoint bp);
}
