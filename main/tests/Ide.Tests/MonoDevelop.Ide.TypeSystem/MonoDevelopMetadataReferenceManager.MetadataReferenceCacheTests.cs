using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class MonoDevelopMetadataReferenceManagerMetadataReferenceCacheTests : IdeTestBase
	{
		[Test]
		public async Task ReferenceCacheWorks ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			using (var sol = (MonoDevelop.Projects.Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				var manager = ws.MetadataReferenceManager;
				var cache = new MonoDevelopMetadataReferenceManager.MetadataReferenceCache ();

				// Create one with default assembly properties
				var asm = typeof (MonoDevelopMetadataReferenceManagerMetadataReferenceCacheTests).Assembly.Location;
				var item = cache.GetOrCreate (manager, asm, MetadataReferenceProperties.Assembly);
				Assert.IsNotNull (item);

				var item2 = cache.GetOrCreate (manager, asm, MetadataReferenceProperties.Assembly);
				Assert.AreSame (item, item2, "Item that is in cache should be returned");

				// Create one with custom properties
				var item3 = cache.GetOrCreate (manager, asm, MetadataReferenceProperties.Assembly.WithAliases (new [] { "a" }));
				Assert.IsNotNull (item3);

				var item4 = cache.GetOrCreate (manager, asm, MetadataReferenceProperties.Assembly.WithAliases (new [] { "a" }));
				Assert.AreSame (item3, item4, "Item that is in cache should be returned");

				// Clear the cache, new items should be returned from now on.
				cache.ClearCache ();

				var item5 = cache.GetOrCreate (manager, asm, MetadataReferenceProperties.Assembly);
				Assert.IsNotNull (item5);

				Assert.AreNotSame (item, item5, "Cache was cleared, so new item should be returned");

				var item6 = cache.GetOrCreate (manager, asm, MetadataReferenceProperties.Assembly.WithAliases (new [] { "a" }));
				Assert.AreNotSame (item3, item6, "Cache was cleared, so new item should be returned");

				cache.ClearCache ();
			}
		}

		[Test]
		public async Task ReferenceCacheSnapshotUpdates ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			var tempPath = Path.GetFullPath (Path.GetTempFileName ());
			var oldAsm = typeof (MonoDevelopMetadataReferenceManagerTests).Assembly.Location;
			File.Copy (oldAsm, tempPath, true);

			try {
				using (var sol = (MonoDevelop.Projects.Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
				using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
					var manager = ws.MetadataReferenceManager;
					var item = manager.GetOrCreateMetadataReference (tempPath, MetadataReferenceProperties.Assembly);
					Assert.IsNotNull (item);

					await FileWatcherService.Update ();

					var initialId = item.CurrentSnapshot.GetMetadataId ();

					var taskForNewAsm = WaitForSnapshotChange (item);

					// Replace the assembly with another one.
					var newAsm = typeof (MonoDevelopMetadataReference).Assembly.Location;
					File.Copy (newAsm, tempPath, true);

					var argsForNewAsm = await taskForNewAsm;

					Assert.AreSame (item.CurrentSnapshot, argsForNewAsm.OldSnapshot);

					Assert.AreNotSame (argsForNewAsm.OldSnapshot, argsForNewAsm.NewSnapshot.Value);
					// item.CurrentSnapshot is now updated
					Assert.AreNotEqual (initialId, item.CurrentSnapshot.GetMetadataId ());

					var taskForOldAsm = WaitForSnapshotChange (item);
					File.Copy (newAsm, tempPath, true);

					var argsForOldAsm = await taskForOldAsm;

					Assert.AreSame (item.CurrentSnapshot, argsForOldAsm.OldSnapshot);

					Assert.AreNotSame (argsForNewAsm.OldSnapshot, argsForNewAsm.NewSnapshot.Value);
					// Even though the old assembly was put back, it has a new id this time.
					Assert.AreNotEqual (initialId, item.CurrentSnapshot.GetMetadataId ());
				}

				await FileWatcherService.Update ();

				// At this point, the metadata reference should be disposed.
				// Check to see if file updates will trigger a file service noification
				var tcsShouldTimeout = new TaskCompletionSource<bool> ();
				var ctsFail = new CancellationTokenSource ();
				ctsFail.Token.Register (() => tcsShouldTimeout.TrySetResult (true));
				Core.FileService.FileChanged += (sender, args) => {
					foreach (var file in args) {
						if (file.FileName == tempPath)
							tcsShouldTimeout.TrySetResult (false);
					}
				};

				ctsFail.CancelAfter (1000 * 5);
				File.WriteAllText (tempPath, "");
				Assert.AreEqual (true, await tcsShouldTimeout.Task);
			} finally {
				File.Delete (tempPath);
			}
		}

		Task<MetadataReferenceUpdatedEventArgs> WaitForSnapshotChange (MonoDevelopMetadataReference item)
		{
			var tcs = new TaskCompletionSource<MetadataReferenceUpdatedEventArgs> ();
			var cts = new CancellationTokenSource ();
			cts.Token.Register (() => tcs.TrySetResult (null));
			item.SnapshotUpdated += (sender, args) => {
				// This routes through file service
				tcs.TrySetResult (args);
			};

			// 1 minute should be enough.
			cts.CancelAfter (1000 * 60);

			return tcs.Task;
		}
	}
}
