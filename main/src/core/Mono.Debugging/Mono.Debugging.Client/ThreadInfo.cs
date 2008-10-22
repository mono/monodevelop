// ThreadInfo.cs
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
	public class ThreadInfo
	{
		int id;
		string name;
		int processId;
		string location;
		Backtrace backtrace;
		
		[NonSerialized]
		DebuggerSession session;
		
		internal void Attach (DebuggerSession session)
		{
			this.session = session;
		}
		
		public int Id {
			get {
				return id;
			}
		}

		public string Name {
			get {
				return name;
			}
		}
		
		public string Location {
			get {
				if (location == null) {
					Backtrace bt = Backtrace;
					if (bt != null && bt.FrameCount > 0)
						location = bt.GetFrame (0).ToString ();
				}
				return location;
			}
		}
		
		internal int ProcessId {
			get { return processId; }
		}

		public Backtrace Backtrace {
			get {
				if (backtrace == null)
					backtrace = session.GetBacktrace (processId, id);
				return backtrace;
			}
		}
		
		public void SetActive ()
		{
			session.ActiveThread = this;
		}
		
		public ThreadInfo (int processId, int id, string name, string location): this (processId, id, name, location, null)
		{
		}
		
		public ThreadInfo (int processId, int id, string name, string location, Backtrace backtrace)
		{
			this.id = id;
			this.name = name;
			this.processId = processId;
			this.location = location;
			this.backtrace = backtrace;
		}
		
		public override bool Equals (object obj)
		{
			ThreadInfo ot = obj as ThreadInfo;
			if (ot == null)
				return false;
			return id == ot.id && processId == ot.processId;
		}
		
		public override int GetHashCode ()
		{
			return id + processId*1000;
		}
		
		public static bool operator == (ThreadInfo t1, ThreadInfo t2)
		{
			if (object.ReferenceEquals (t1, t2))
				return true;
			if ((object)t1 == null || (object)t2 == null)
				return false;
			return t1.Equals (t2);
		}
		
		public static bool operator != (ThreadInfo t1, ThreadInfo t2)
		{
			if (object.ReferenceEquals (t1, t2))
				return false;
			if ((object)t1 == null || (object)t2 == null)
				return true;
			return !t1.Equals (t2);
		}
	}
}
