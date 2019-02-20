//
// FileServiceEventQueueTests.cs
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
using static MonoDevelop.Core.FileService;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class FileServiceEventQueueTests
	{
		readonly FilePath a = "a", b = "b", c = "c", d = "d", e = "e", btmp = "b.tmp";

		[Test]
		public void TestMergeRemovesEmptyChangeSets()
		{
			var processor = new Processor ();
			processor.Queue (CreateData (EventDataKind.Changed, Array.Empty<FilePath> ()));
			processor.Merge ();

			Assert.AreEqual (0, processor.Events.Count);
		}

		[Test]
		public void TestQueueChanged()
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Changed, a, c));
				processor.Queue (CreateData (EventDataKind.Changed, a, b));
				processor.Queue (CreateData (EventDataKind.Changed, d));
				processor.Queue (CreateData (EventDataKind.Changed, c));
			});

			Assert.AreEqual (1, events.Length);

			var ev = events [0];
			Assert.AreEqual (4, ev.Args.Count);
			Assert.AreEqual (d, ev.Args [0].FileName);
			Assert.AreEqual (b, ev.Args [1].FileName);
			Assert.AreEqual (a, ev.Args [2].FileName);
			Assert.AreEqual (c, ev.Args [3].FileName);
		}

		[Test]
		public void TestQueueChangedCreatedWithSkip()
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Changed, a));
				processor.Queue (CreateData (EventDataKind.Created, b));
				processor.Queue (CreateData (EventDataKind.Created, b));
				processor.Queue (CreateData (EventDataKind.Changed, a, b));
			});

			Assert.AreEqual (3, events.Length);

			var ev = events [0];
			Assert.AreEqual (EventDataKind.Changed, ev.Kind);
			Assert.AreEqual (1, ev.Args.Count);
			Assert.AreEqual (a, ev.Args [0].FileName);

			ev = events [1];
			Assert.AreEqual (EventDataKind.Created, ev.Kind);
			Assert.AreEqual (1, ev.Args.Count);
			Assert.AreEqual (b, ev.Args [0].FileName);

			ev = events [2];
			Assert.AreEqual (EventDataKind.Changed, ev.Kind);
			Assert.AreEqual (1, ev.Args.Count);
			Assert.AreEqual (b, ev.Args [0].FileName);
		}

		[TestCase(true)]
		[TestCase(false)]
		public void FileRemovedAfterCreated(bool withSkip)
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Created, a));
				if (withSkip)
					processor.Queue (CreateData (EventDataKind.Changed, b));
				processor.Queue (CreateData (EventDataKind.Removed, a));
			});

			if (withSkip) {
				Assert.AreEqual (1, events.Length);

				var ev = events [0];
				Assert.AreEqual (EventDataKind.Changed, ev.Kind);
				Assert.AreEqual (1, ev.Args.Count);
				Assert.AreEqual (b, ev.Args[0].FileName);
			} else {
				Assert.AreEqual (0, events.Length);
			}
		}

		[TestCase(true)]
		[TestCase(false)]
		public void FileCreatedAfterRemoved(bool withSkip)
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Removed, a));
				if (withSkip)
					processor.Queue (CreateData (EventDataKind.Changed, b));
				processor.Queue (CreateData (EventDataKind.Created, a));
				if (withSkip)
					processor.Queue (CreateData (EventDataKind.Changed, b));
				processor.Queue (CreateData (EventDataKind.Removed, c));
				processor.Queue (CreateData (EventDataKind.Created, c));
			});

			Assert.AreEqual (1, events.Length);
			var ev = events [0];
			Assert.AreEqual (EventDataKind.Changed, ev.Kind);
			Assert.AreEqual (withSkip ? 3 : 2, ev.Args.Count);
			Assert.AreEqual (c, ev.Args [0].FileName);

			Assert.AreEqual (a, ev.Args [1].FileName);
			if (withSkip) {
				Assert.AreEqual (b, ev.Args [2].FileName);
			}
		}

		[TestCase (true)]
		[TestCase (false)]
		public void FileRemovedAfterChanged (bool withSkip)
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Changed, a));
				if (withSkip)
					processor.Queue (CreateData (EventDataKind.Changed, b));
				processor.Queue (CreateData (EventDataKind.Removed, a));
			});

			FileEventData ev;
			if (withSkip) {
				Assert.AreEqual (2, events.Length);

				ev = events [0];
				Assert.AreEqual (EventDataKind.Changed, ev.Kind);
				Assert.AreEqual (1, ev.Args.Count);
				Assert.AreEqual (b, ev.Args [0].FileName);

				ev = events [1];
			} else {
				Assert.AreEqual (1, events.Length);
				ev = events [0];
			}

			Assert.AreEqual (EventDataKind.Removed, ev.Kind);
			Assert.AreEqual (1, ev.Args.Count);
			Assert.AreEqual (a, ev.Args [0].FileName);
		}

		[Test]
		public void DetectFileRenamePatternCopy ()
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Removed, b));
				processor.Queue (CreateData (EventDataKind.Copied, (a, b)));
			});

			Assert.AreEqual (1, events.Length);

			var ev = events [0];
			Assert.AreEqual (1, ev.Args.Count);
			Assert.AreEqual (EventDataKind.Copied, ev.Kind);
			Assert.AreEqual (a, ev.Args [0].SourceFile);
			Assert.AreEqual (b, ev.Args [0].TargetFile);
		}

		[Test]
		public void DetectFileRenamePatternMove ()
		{
			var kinds = new [] {
				EventDataKind.Moved,
				EventDataKind.Renamed,
			};

			foreach (var kind in kinds) {
				var events = DoRun (processor => {
					processor.Queue (CreateData (EventDataKind.Removed, b));
					processor.Queue (CreateData (kind, (a, b)));
					processor.Queue (CreateData (EventDataKind.Created, a));
				});

				Assert.AreEqual (2, events.Length);

				var ev = events [0];
				Assert.AreEqual (1, ev.Args.Count);
				Assert.AreEqual (kind, ev.Kind);
				Assert.AreEqual (a, ev.Args [0].SourceFile);
				Assert.AreEqual (b, ev.Args [0].TargetFile);

				ev = events [1];
				Assert.AreEqual (1, ev.Args.Count);
				Assert.AreEqual (EventDataKind.Created, ev.Kind);
				Assert.AreEqual (a, ev.Args [0].FileName);
			}
		}

		[Test]
		public void DetectAutoSavePatternMove ()
		{
			var kinds = new [] {
				EventDataKind.Moved,
				EventDataKind.Renamed,
			};

			foreach (var kind in kinds) {
				var events = DoRun (processor => {
					// Renamed Program.cs -> Program.cs~hash.tmp
					// Renamed .#Program.cs -> Program.cs
					// Removed Program.cs~hash.tmp
					processor.Queue (CreateData (kind, (b, btmp)));
					processor.Queue (CreateData (EventDataKind.Changed, btmp)); // FileService does this.
					processor.Queue (CreateData (kind, (a, b)));
					processor.Queue (CreateData (EventDataKind.Changed, b)); // FileService does this.
					processor.Queue (CreateData (EventDataKind.Removed, btmp));

					// This should become
					// Renamed .#Program.cs -> Program.cs
					// Changed Program.cs
				});

				Assert.AreEqual (2, events.Length);

				var ev = events [0];
				Assert.AreEqual (kind, ev.Kind);
				Assert.AreEqual (1, ev.Args.Count);
				Assert.AreEqual (a, ev.Args [0].SourceFile);
				Assert.AreEqual (b, ev.Args [0].TargetFile);

				ev = events [1];
				Assert.AreEqual (EventDataKind.Changed, ev.Kind);
				Assert.AreEqual (1, ev.Args.Count);
				Assert.AreEqual (b, ev.Args [0].FileName);
			}
		}

		[Test]
		public void ReduceGoesRecursive()
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Created, b));
				processor.Queue (CreateData (EventDataKind.Changed, b));
				processor.Queue (CreateData (EventDataKind.Removed, b));
			});

			Assert.AreEqual (0, events.Length);
		}

		[Test]
		public void NothingToReduce ()
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Changed, a));
				processor.Queue (CreateData (EventDataKind.Created, b));
				processor.Queue (CreateData (EventDataKind.Removed, c));
				processor.Queue (CreateData (EventDataKind.Moved, (c, d)));
				processor.Queue (CreateData (EventDataKind.Copied, (d, btmp)));
				processor.Queue (CreateData (EventDataKind.Renamed, (e, e)));
			});

			Assert.AreEqual (6, events.Length);
		}

		[Test]
		public void DirectoryDoesNotAffect ()
		{
			var events = DoRun (processor => {
				processor.Queue (CreateData (EventDataKind.Changed, a));
				processor.Queue (CreateDirectoryData (EventDataKind.Changed, a));
			});

			Assert.AreEqual (1, events.Length);
			Assert.AreEqual (2, events [0].Args.Count);
		}

		FileEventData [] DoRun(Action<Processor> callback)
		{
			var processor = new Processor ();
			callback (processor);
			processor.Merge ();

			return processor.Events.OfType<FileEventData> ().ToArray ();

		}

		static FileEventData CreateDirectoryData (EventDataKind kind, params FilePath [] paths)
			=> CreateData (kind, true, paths);

		static FileEventData CreateData (EventDataKind kind, params FilePath [] paths)
			=> CreateData (kind, false, paths);

		static FileEventData CreateData (EventDataKind kind, bool isDirectory, params FilePath [] paths)
		{
			if (kind != EventDataKind.Created && kind != EventDataKind.Changed && kind != EventDataKind.Removed) {
				Assert.Fail ("Wrong data kind passed to this method");
			}

			var args = new FileEventArgs ();
			foreach (var path in paths) {
				args.Add (new FileEventInfo (path, isDirectory));
			}

			return new FileEventData {
				Kind = kind,
				Args = args,
			};
		}

		static FileEventData CreateData (EventDataKind kind, params (FilePath, FilePath) [] paths)
		{
			if (kind != EventDataKind.Moved && kind != EventDataKind.Copied && kind != EventDataKind.Renamed) {
				Assert.Fail ("Wrong data kind passed to this method");
			}

			var args = new FileCopyEventArgs ();
			foreach (var path in paths) {
				args.Add (new FileEventInfo (path.Item1, path.Item2, false));
			}

			return new FileEventData {
				Kind = kind,
				Args = args,
			};
		}
	}
}
