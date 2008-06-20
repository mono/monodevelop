// ProcessInfo.cs
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
	public class ProcessInfo
	{
		int id;
		string name;
		
		[NonSerialized]
		ThreadInfo[] currentThreads;
		
		[NonSerialized]
		DebuggerSession session;
		
		internal void Attach (DebuggerSession session)
		{
			this.session = session;
			if (currentThreads != null) {
				foreach (ThreadInfo t in currentThreads)
					t.Attach (session);
			}
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

		public ProcessInfo (int id, string name)
		{
			this.id = id;
			this.name = name;
		}
		
		public ThreadInfo[] GetThreads ()
		{
			if (currentThreads == null)
				currentThreads = session.GetThreads (id);
			return currentThreads;
		}
	}
}
