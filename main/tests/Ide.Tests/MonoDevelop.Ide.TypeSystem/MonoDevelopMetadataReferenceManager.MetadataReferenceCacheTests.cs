using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class MonoDevelopMetadataReferenceManagerMetadataReferenceCacheTests
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
			}
		}
	}
}
