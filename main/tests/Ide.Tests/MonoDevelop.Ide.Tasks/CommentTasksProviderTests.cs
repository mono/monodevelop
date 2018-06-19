//
// CommentTasksProviderTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using NUnit.Framework;

namespace MonoDevelop.Ide.Tasks
{
	[TestFixture]
	partial class CommentTasksProviderTests : Ide.IdeTestBase
	{
		[SetUp]
		public void SetUp ()
		{
			//Initialize IdeApp so IdeApp.Workspace is not null, comment tasks listen to root workspace events.
			if (!IdeApp.IsInitialized)
				IdeApp.Initialize (new ProgressMonitor ());
		}

		static async Task RunTest (Func<Controller, Task> act)
		{
			// Keep the current special comment tags and restore them after.
			var oldTags = CommentTag.SpecialCommentTags;
			var helper = new Controller ();

			try {
				await act (helper);
			} finally {
				await helper.DisposeAsync ();
				CommentTag.SpecialCommentTags = oldTags;
			}
		}

		[TestCase(false)]
		[TestCase(true)]
		public async Task TestToDoCommentsAreReported (bool withToDos)
		{
			await RunTest (async helper => {
				await helper.SetupProject (withToDos);
				await helper.LoadProject ();
			});
		}

		[Test]
		public async Task TestToDoCommentsOnFileAdded ()
		{
			await RunTest (async helper => {
				await helper.SetupProject (withToDos: false);
				await helper.LoadProject ();

				// Only one update for the added file.
				await helper.AddToDoFile (new Controller.Options (withToDos: true) {
					ExpectedFiles = new[] { Controller.FileName, },
					NotificationCount = 1,
				});
			});
		}

		[TestCase(false)]
		[TestCase(true)]
		public async Task TestToDoCommentsOnWorkspaceReopen(bool withToDos)
		{
			await RunTest (async helper => {
				await helper.SetupProject (withToDos);

				await helper.LoadProject ();
				await IdeApp.Workspace.Close ();

				await helper.LoadProject ();
			});
		}

		[Test]
		public async Task TestToDoCommentsTagsChanged ()
		{
			await RunTest (async helper => {
				await helper.SetupProject (withToDos: true);

				var oldTags = CommentTag.SpecialCommentTags;

				// Force a new list here. Fix the behaviour at some point to not do equality checks on the list.
				var currentTags = oldTags.ToList ();
				currentTags.RemoveAll (x => x.Tag == "TODO");
				currentTags.Add (new CommentTag ("CUSTOMTAG", 4));
				CommentTag.SpecialCommentTags = currentTags;

				await helper.LoadProject (new Controller.Options (withToDos: true) {
					ExpectedComments = new[] {
						("FIXME: This is broken", "FIXME", 2, 4),
						("HACK: Just for the test", "HACK", 3, 4),
						("UNDONE: Not done yet", "UNDONE", 4, 4),
						("CUSTOMTAG: Shouldn't be in first", "CUSTOMTAG", 5, 4),
					},
				});

				await helper.SetCommentTags (oldTags, new Controller.Options (withToDos: true) {
					ExpectedFiles = new [] { Controller.FileName },
					NotificationCount = 1,
				});
			});
		}

		[TestCase (true)]
		[TestCase (false)]
		public async Task TestBatchedBehaviourWorks (bool withToDos)
		{
			await RunTest (async helper => {
				var batchOptions = new Controller.Options (withToDos);
				batchOptions.ChangeCount = (count) => batchOptions.ExpectedFiles.Length;
				batchOptions.NotificationCount = 1;

				await helper.SetupProject (withToDos);

				CommentTasksProvider.ResetCachedContents (cnt => cnt == batchOptions.ExpectedFiles.Length);

				await helper.LoadProject (batchOptions, loadCachedContents: false);
			});
		}

		[Test]
		public async Task TestFileChangeTriggersNotification ()
		{
			await RunTest (async helper => {
				await helper.SetupProject (withToDos: true);
				await helper.LoadProject ();

				var options = new Controller.Options (withToDos: true) {
					ExpectedFiles = new[] { Controller.FileName, },
				};

				var list = options.ExpectedComments.ToList ();
				list.Add (("TODO: Added", "TODO", 6, 4));
				options.ExpectedComments = list.ToArray ();

				await helper.ModifyToDoFile ("// TODO: Added", options);
			});
		}

		[TestCase(true)]
		[TestCase(false)]
		public async Task TestCachedContentsAreReleasedIfNotQueried (bool withToDos)
		{
			// Simulates the use-case where the Tasks pad is not constructed.
			// Ensure we don't leak anything.

			await RunTest (async helper => {
				await helper.SetupProject (withToDos);

				var options = new Controller.Options (withToDos);
				var tcs = new TaskCompletionSource<bool> ();

				bool done = false;
				// Set it so we get notified on cache items.
				CommentTasksProvider.ResetCachedContents (shouldTriggerLoad: cnt => {
					if (cnt == options.NotificationCount)
						tcs.TrySetResult (true);
					return done;
				});

				Controller.BindTimeout (tcs);
				await helper.LoadProject (loadCachedContents: false, registerCallback: false);

				await tcs.Task;
				Assert.AreEqual (options.ExpectedFiles.Length, CommentTasksProvider.GetCachedContentsCount ());

				await IdeApp.Workspace.Close ();
				Assert.AreEqual (0, CommentTasksProvider.GetCachedContentsCount ());

				done = true;

				CommentTasksProvider.LoadCachedContents ();
				Assert.AreEqual (-1, CommentTasksProvider.GetCachedContentsCount ());
			});
		}
	}
}
