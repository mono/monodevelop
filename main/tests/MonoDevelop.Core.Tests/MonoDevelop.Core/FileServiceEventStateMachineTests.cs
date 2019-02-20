//
// FileServiceEventStateMachineTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using NUnit.Framework;
using static MonoDevelop.Core.EventQueue;
using static MonoDevelop.Core.EventQueue.Processor;
using static MonoDevelop.Core.FileService;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class FileServiceEventStateMachineTests
	{
		[Test]
		public void Functionality ()
		{
			var fsm = new FileEventStateMachine ();

			FilePath a = "a", b = "b", c = "c";
			var eventData = new FileEventData {
				Kind = EventDataKind.Changed,
				Args = new FileEventArgs (new [] { a, b, c }, false),
			};

			fsm.Queue (eventData);

			Assert.AreEqual (false, fsm.TryGet (a, out var state));

			fsm.Set (a, 0, 0, false, FileState.Changed);
			fsm.Set (b, 0, 1, false, FileState.Changed);
			fsm.Set (c, 0, 2, false, FileState.Changed);

			Assert.AreEqual (true, fsm.TryGet (a, out state));
			AssertState (state, FileState.Changed, 1, 0, 0);

			Assert.AreEqual (true, fsm.TryGet (b, out state));
			AssertState (state, FileState.Changed, 1, 0, 1);

			Assert.AreEqual (true, fsm.TryGet (c, out state));
			AssertState (state, FileState.Changed, 1, 0, 2);

			eventData = new FileEventData {
				Kind = EventDataKind.Removed,
				Args = new FileEventArgs (new [] { b, a, c }, false),
			};

			fsm.Queue (eventData);

			fsm.Set (a, 1, 1, false, FileState.Removed);
			fsm.Set (b, 1, 0, false, FileState.Removed);
			fsm.Set (c, 1, 2, false, FileState.Removed);

			Assert.AreEqual (true, fsm.TryGet (a, out state));
			AssertState (state, FileState.Removed, 2, 1, 1);

			Assert.AreEqual (true, fsm.TryGet (b, out state));
			AssertState (state, FileState.Removed, 2, 1, 0);

			Assert.AreEqual (true, fsm.TryGet (c, out state));
			AssertState (state, FileState.Removed, 2, 1, 2);

			fsm.RemoveLastEventData (c);

			Assert.AreEqual (true, fsm.TryGet (a, out state));
			AssertState (state, FileState.Removed, 2, 1, 1);

			Assert.AreEqual (true, fsm.TryGet (b, out state));
			AssertState (state, FileState.Removed, 2, 1, 0);

			Assert.AreEqual (true, fsm.TryGet (c, out state));
			AssertState (state, FileState.Changed, 1, 0, 2);

			fsm.RemoveLastEventData (c);

			Assert.AreEqual (false, fsm.TryGet (c, out state));
			Assert.IsNull (state);

			void AssertState (FileEventState s, FileState fs, int count, int eventIndex, int fileIndex)
			{
				Assert.AreEqual (fs, state.FinalState);
				Assert.AreEqual (count, state.Indices.Count);
				if (count == 0)
					return;

				Assert.AreEqual (eventIndex, state.Indices [count - 1].EventIndex);
				Assert.AreEqual (fileIndex, state.Indices [count - 1].FileIndex);
			}
		}

		[Test]
		public void AddAndRemove ()
		{
			var fsm = new FileEventStateMachine ();

			var eventData = new FileEventData {
				Kind = EventDataKind.Changed,
				Args = new FileEventArgs ("a", false),
			};

			fsm.Queue (eventData);

			fsm.Set ("a", 0, 0, false, FileState.Removed);
			fsm.RemoveLastEventData ("a");

			Assert.IsFalse (fsm.TryGet ("a", out var state));

			fsm.RemoveLastEventData ("a");
			Assert.IsFalse (fsm.TryGet ("a", out state));
		}

		[Test]
		public void UnsyncedWillThrow()
		{
			var fsm = new FileEventStateMachine ();

			Assert.Throws<ArgumentOutOfRangeException> (() => fsm.Set ("a", 0, 0, false, FileState.Removed));

			// Add a busted FileEventData
			var eventData = new FileEventData {
				Kind = EventDataKind.Changed,
				Args = new FileEventArgs (),
			};

			Assert.Throws<ArgumentOutOfRangeException> (() => fsm.Set ("a", 0, 0, false, FileState.Removed));

		}
	}
}
