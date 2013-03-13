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
		string lastTraceValue;
		
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
			s = elem.GetAttribute ("hitCount");
			if (s.Length > 0)
				hitCount = int.Parse (s);
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
			if (hitCount > 0)
				elem.SetAttribute ("hitCount", hitCount.ToString ());
			return elem;
		}
		
		internal static BreakEvent FromXml (XmlElement elem)
		{
			if (elem.Name == "FunctionBreakpoint")
				return new FunctionBreakpoint (elem);
			if (elem.Name == "Breakpoint")
				return new Breakpoint (elem);
			if (elem.Name == "Catchpoint")
				return new Catchpoint (elem);

			return null;
		}
		
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Mono.Debugging.Client.BreakEvent"/> is enabled.
		/// </summary>
		/// <value>
		/// <c>true</c> if enabled; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>
		/// Changes in this property are automatically applied. There is no need to call CommitChanges().
		/// </remarks>
		public bool Enabled {
			get {
				return enabled;
			}
			set {
				if (store != null && store.IsReadOnly)
					return;
				enabled = value;
				if (store != null)
					store.EnableBreakEvent (this, value);
			}
		}

		/// <summary>
		/// Gets the status of the break event
		/// </summary>
		/// <returns>
		/// The status of the break event for the given debug session
		/// </returns>
		/// <param name='session'>
		/// Session for which to get the status of the break event
		/// </param>
		public BreakEventStatus GetStatus (DebuggerSession session)
		{
			if (store == null || session == null)
				return BreakEventStatus.Disconnected;
			return session.GetBreakEventStatus (this);
		}
		
		/// <summary>
		/// Gets a message describing the status of the break event
		/// </summary>
		/// <returns>
		/// The status message of the break event for the given debug session
		/// </returns>
		/// <param name='session'>
		/// Session for which to get the status message of the break event
		/// </param>
		public string GetStatusMessage (DebuggerSession session)
		{
			if (store == null || session == null)
				return string.Empty;
			return session.GetBreakEventStatusMessage (this);
		}
		
		/// <summary>
		/// Gets or sets the expression to be traced when the breakpoint is hit
		/// </summary>
		/// <remarks>
		/// If this break event is hit and the HitAction is TraceExpression, the debugger
		/// will evaluate and print the value of this property.
		/// The CommitChanges() method has to be called for changes in this
		/// property to take effect.
		/// </remarks>
		public string TraceExpression {
			get {
				return traceExpression;
			}
			set {
				traceExpression = value;
			}
		}

		/// <summary>
		/// Gets or sets the action to be performed when the breakpoint is hit
		/// </summary>
		/// <remarks>
		/// If the value is Break, the debugger will pause the execution.
		/// If the value is PrintExpression, the debugger will evaluate and
		/// print the value of the TraceExpression property.
		/// If the value is CustomAction, the debugger will execute the
		/// CustomBreakEventHitHandler callback specified in DebuggerSession,
		/// and will provide the value of CustomActionId as argument.
		/// The CommitChanges() method has to be called for changes in this
		/// property to take effect.
		/// </remarks>
		public HitAction HitAction {
			get {
				return hitAction;
			}
			set {
				hitAction = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the custom action identifier.
		/// </summary>
		/// <remarks>
		/// If this break event is hit and the value of HitAction is CustomAction,
		/// the debugger will execute the CustomBreakEventHitHandler callback
		/// specified in DebuggerSession, and will provide the value of this property
		/// as argument.
		/// The CommitChanges() method has to be called for changes in this
		/// property to take effect.
		/// </remarks>
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
		
		/// <summary>
		/// Gets or sets the hit count.
		/// </summary>
		/// <remarks>
		/// When the break event is hit, if the value of this propery is greater than 0 then
		/// the value will be decremented and execution will be immediately resumed.
		/// The CommitChanges() method has to be called for changes in this
		/// property to take effect.
		/// </remarks>
		public int HitCount {
			get {
				return hitCount;
			}
			set {
				hitCount = value;
			}
		}
		
		/// <summary>
		/// Gets the last value traced.
		/// </summary>
		/// <remarks>
		/// This property returns the last evaluation of TraceExpression.
		/// </remarks>
		public string LastTraceValue {
			get {
				return lastTraceValue;
			}
			internal set {
				lastTraceValue = value;
			}
		}
		
		/// <summary>
		/// Commits changes done in the break event properties
		/// </summary>
		/// <remarks>
		/// This method must be called after doing changes in the break event properties.
		/// </remarks>
		public void CommitChanges ()
		{
			if (store != null)
				store.NotifyBreakEventChanged (this);
		}
		
		internal void NotifyUpdate ()
		{
			if (store != null)
				store.NotifyBreakEventUpdated (this);
		}
		
		/// <summary>
		/// Clone this instance.
		/// </summary>
		public BreakEvent Clone ()
		{
			return (BreakEvent) MemberwiseClone ();
		}
		
		/// <summary>
		/// Makes a copy of this instance
		/// </summary>
		/// <param name='ev'>
		/// A break event from which to copy the data.
		/// </param>
		public virtual void CopyFrom (BreakEvent ev)
		{
			hitAction = ev.hitAction;
			customActionId = ev.customActionId;
			traceExpression = ev.traceExpression;
			hitCount = ev.hitCount;
		}
	}
}
