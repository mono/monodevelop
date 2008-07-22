// BreakEvent.cs
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
using System.Xml;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class BreakEvent
	{
		[NonSerialized] BreakpointStore store;
		[NonSerialized] bool enabled = true;
		
		HitAction hitAction = HitAction.Break;
		string customActionId;
		string traceExpression;
		int hitCount;
		
		public BreakEvent()
		{
		}
		
		internal BreakEvent (XmlElement elem)
		{
			string s = elem.GetAttribute ("enabled");
			if (s.Length > 0)
				enabled = bool.Parse (s);
			s = elem.GetAttribute ("hitAction");
			if (s.Length > 0)
				hitAction = (HitAction) Enum.Parse (typeof(HitAction), s);
			s = elem.GetAttribute ("customActionId");
			if (s.Length > 0)
				customActionId = s;
			s = elem.GetAttribute ("traceExpression");
			if (s.Length > 0)
				traceExpression = s;
		}
		
		internal virtual XmlElement ToXml (XmlDocument doc)
		{
			XmlElement elem = doc.CreateElement (GetType().Name);
			if (!enabled)
				elem.SetAttribute ("enabled", "false");
			if (hitAction != HitAction.Break)
				elem.SetAttribute ("hitAction", hitAction.ToString ());
			if (!string.IsNullOrEmpty (customActionId))
				elem.SetAttribute ("customActionId", customActionId);
			if (!string.IsNullOrEmpty (traceExpression))
				elem.SetAttribute ("traceExpression", traceExpression);
			return elem;
		}
		
		internal static BreakEvent FromXml (XmlElement elem)
		{
			if (elem.Name == "Breakpoint")
				return new Breakpoint (elem);
			else if (elem.Name == "Catchpoint")
				return new Catchpoint (elem);
			else
				return null;
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
				store.EnableBreakEvent (this, value);
			}
		}
		
		public bool IsValid (DebuggerSession session)
		{
			if (store == null)
				throw new InvalidOperationException ();
			if (session == null)
				return true;
			return session.IsBreakEventValid (this);
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

		internal BreakpointStore Store {
			get {
				return store;
			}
			set {
				store = value;
			}
		}

		public int HitCount {
			get {
				return hitCount;
			}
			set {
				hitCount = value;
			}
		}
		
		public void CommitChanges ()
		{
			if (store != null)
				store.NotifyBreakEventChanged (this);
		}
		
		public BreakEvent Clone ()
		{
			return (BreakEvent) MemberwiseClone ();
		}
		
		public virtual void CopyFrom (BreakEvent ev)
		{
			hitAction = ev.hitAction;
			customActionId = ev.customActionId;
			traceExpression = ev.traceExpression;
			hitCount = ev.hitCount;
		}
	}
}
