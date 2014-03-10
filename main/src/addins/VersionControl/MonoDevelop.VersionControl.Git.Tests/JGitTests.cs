//
// JGitTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using NUnit.Framework;
using System.Text;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public class JGitTests
	{
		static void BaseRun (java.lang.Class[] packages)
		{
			var core = new org.junit.runner.JUnitCore ();
			var result = core.run (packages);
			int failures = result.getFailureCount ();
			if (failures == 0)
				return;

			var sb = new StringBuilder ();
			var iterator = result.getFailures ().listIterator ();
			while (iterator.hasNext ()) {
				var fail = (org.junit.runner.notification.Failure)iterator.next ();
				sb.Append ("Failure in test: ");
				sb.AppendLine (fail.getTestHeader ());
				sb.AppendLine (fail.getTrace ());
			}
			Assert.Fail (sb.ToString ());
		}

		static readonly java.lang.Class[] api = {
			typeof (org.eclipse.jgit.api.AddCommandTest),
			typeof (org.eclipse.jgit.api.ApplyCommandTest),
			typeof (org.eclipse.jgit.api.BlameCommandTest),
			typeof (org.eclipse.jgit.api.BranchCommandTest),
			typeof (org.eclipse.jgit.api.CheckoutCommandTest),
			typeof (org.eclipse.jgit.api.CherryPickCommandTest),
			typeof (org.eclipse.jgit.api.CleanCommandTest),
			typeof (org.eclipse.jgit.api.CloneCommandTest),
			typeof (org.eclipse.jgit.api.CommitCommandTest),
			typeof (org.eclipse.jgit.api.CommitOnlyTest),
			typeof (org.eclipse.jgit.api.DescribeCommandTest),
			typeof (org.eclipse.jgit.api.DiffCommandTest),
			typeof (org.eclipse.jgit.api.FetchCommandTest),
			typeof (org.eclipse.jgit.api.GarbageCollectCommandTest),
			typeof (org.eclipse.jgit.api.GitConstructionTest),
			typeof (org.eclipse.jgit.api.HugeFileTest),
			typeof (org.eclipse.jgit.api.InitCommandTest),
			typeof (org.eclipse.jgit.api.LogCommandTest),
			typeof (org.eclipse.jgit.api.LsRemoteCommandTest),
			typeof (org.eclipse.jgit.api.MergeCommandTest),
			typeof (org.eclipse.jgit.api.NameRevCommandTest),
			typeof (org.eclipse.jgit.api.NotesCommandTest),
			typeof (org.eclipse.jgit.api.PathCheckoutCommandTest),
			typeof (org.eclipse.jgit.api.PullCommandTest),
			typeof (org.eclipse.jgit.api.PullCommandWithRebaseTest),
			typeof (org.eclipse.jgit.api.PushCommandTest),
			typeof (org.eclipse.jgit.api.RebaseCommandTest),
			typeof (org.eclipse.jgit.api.ReflogCommandTest),
			typeof (org.eclipse.jgit.api.RenameBranchCommandTest),
			typeof (org.eclipse.jgit.api.ResetCommandTest),
			typeof (org.eclipse.jgit.api.RevertCommandTest),
			typeof (org.eclipse.jgit.api.RmCommandTest),
			typeof (org.eclipse.jgit.api.StashApplyCommandTest),
			typeof (org.eclipse.jgit.api.StashCreateCommandTest),
			typeof (org.eclipse.jgit.api.StashDropCommandTest),
			typeof (org.eclipse.jgit.api.StashListCommandTest),
			typeof (org.eclipse.jgit.api.StatusCommandTest),
			typeof (org.eclipse.jgit.api.TagCommandTest),
			typeof (org.eclipse.jgit.api.blame.BlameGeneratorTest),
		};
		static readonly java.lang.Class[] diff = {
			typeof (org.eclipse.jgit.diff.DiffEntryTest),
			typeof (org.eclipse.jgit.diff.DiffFormatterReflowTest),
			typeof (org.eclipse.jgit.diff.DiffFormatterTest),
			typeof (org.eclipse.jgit.diff.EditListTest),
			typeof (org.eclipse.jgit.diff.EditTest),
			typeof (org.eclipse.jgit.diff.HistogramDiffTest),
			typeof (org.eclipse.jgit.diff.MyersDiffTest),
			typeof (org.eclipse.jgit.diff.PatchIdDiffFormatterTest),
			typeof (org.eclipse.jgit.diff.RawTextIgnoreAllWhitespaceTest),
			typeof (org.eclipse.jgit.diff.RawTextIgnoreLeadingWhitespaceTest),
			typeof (org.eclipse.jgit.diff.RawTextIgnoreTrailingWhitespaceTest),
			typeof (org.eclipse.jgit.diff.RawTextIgnoreWhitespaceChangeTest),
			typeof (org.eclipse.jgit.diff.RawTextTest),
			typeof (org.eclipse.jgit.diff.RenameDetectorTest),
			typeof (org.eclipse.jgit.diff.SimilarityIndexTest),
		};
		static readonly java.lang.Class[] dircache = {
			typeof (org.eclipse.jgit.dircache.DirCacheBasicTest),
			typeof (org.eclipse.jgit.dircache.DirCacheBuilderIteratorTest),
			typeof (org.eclipse.jgit.dircache.DirCacheBuilderTest),
			typeof (org.eclipse.jgit.dircache.DirCacheCGitCompatabilityTest),
			typeof (org.eclipse.jgit.dircache.DirCacheEntryTest),
			typeof (org.eclipse.jgit.dircache.DirCacheFindTest),
			typeof (org.eclipse.jgit.dircache.DirCacheIteratorTest),
			typeof (org.eclipse.jgit.dircache.DirCacheLargePathTest),
			typeof (org.eclipse.jgit.dircache.DirCachePathEditTest),
			typeof (org.eclipse.jgit.dircache.DirCacheTreeTest)
		};
		static readonly java.lang.Class[] @internal = {
			typeof (org.eclipse.jgit.events.ConfigChangeEventTest),
			typeof (org.eclipse.jgit.fnmatch.FileNameMatcherTest),
			typeof (org.eclipse.jgit.ignore.IgnoreMatcherTest),
			typeof (org.eclipse.jgit.ignore.IgnoreNodeTest),
			typeof (org.eclipse.jgit.@internal.storage.dfs.DfsOutputStreamTest),
			typeof (org.eclipse.jgit.@internal.storage.file.AbbreviationTest),
			typeof (org.eclipse.jgit.@internal.storage.file.ConcurrentRepackTest),
			typeof (org.eclipse.jgit.@internal.storage.file.FileRepositoryBuilderTest),
			typeof (org.eclipse.jgit.@internal.storage.file.FileSnapshotTest),
			typeof (org.eclipse.jgit.@internal.storage.file.InflatingBitSetTest),
			typeof (org.eclipse.jgit.@internal.storage.file.LockFileTest),
			typeof (org.eclipse.jgit.@internal.storage.file.ObjectDirectoryTest),
			typeof (org.eclipse.jgit.@internal.storage.file.PackFileTest),
			typeof (org.eclipse.jgit.@internal.storage.file.PackIndexV1Test),
			typeof (org.eclipse.jgit.@internal.storage.file.PackIndexV2Test),
			typeof (org.eclipse.jgit.@internal.storage.file.PackReverseIndexTest),
			typeof (org.eclipse.jgit.@internal.storage.file.PackWriterTest),
			typeof (org.eclipse.jgit.@internal.storage.file.RefDirectoryTest),
			typeof (org.eclipse.jgit.@internal.storage.file.ReflogReaderTest),
			typeof (org.eclipse.jgit.@internal.storage.file.RefUpdateTest),
			typeof (org.eclipse.jgit.@internal.storage.file.RepositorySetupWorkDirTest),
			typeof (org.eclipse.jgit.@internal.storage.file.StoredBitmapTest),
			typeof (org.eclipse.jgit.@internal.storage.file.T0003_BasicTest),
			typeof (org.eclipse.jgit.@internal.storage.file.T0004_PackReaderTest),
			typeof (org.eclipse.jgit.@internal.storage.file.UnpackedObjectTest),
			typeof (org.eclipse.jgit.@internal.storage.file.WindowCacheGetTest),
			typeof (org.eclipse.jgit.@internal.storage.file.WindowCacheReconfigureTest),
			typeof (org.eclipse.jgit.@internal.storage.pack.DeltaIndexTest),
			typeof (org.eclipse.jgit.@internal.storage.pack.DeltaStreamTest),
			typeof (org.eclipse.jgit.@internal.storage.pack.IntSetTest),
			/* Ignore GC tests. They take up too much time.
			typeof (org.eclipse.jgit.@internal.storage.file.GcBasicPackingTest),
			typeof (org.eclipse.jgit.@internal.storage.file.GcBranchPrunedTest),
			typeof (org.eclipse.jgit.@internal.storage.file.GcConcurrentTest),
			typeof (org.eclipse.jgit.@internal.storage.file.GcDirCacheSavesObjectsTest),
			typeof (org.eclipse.jgit.@internal.storage.file.GcKeepFilesTest),
			typeof (org.eclipse.jgit.@internal.storage.file.GcPackRefsTest),
			typeof (org.eclipse.jgit.@internal.storage.file.GcPruneNonReferencedTest),
			typeof (org.eclipse.jgit.@internal.storage.file.GcReflogTest),
			typeof (org.eclipse.jgit.@internal.storage.file.GcTagTest),
			*/
		};
		static readonly java.lang.Class[] lib = {
			typeof (org.eclipse.jgit.lib.AbbreviatedObjectIdTest),
			typeof (org.eclipse.jgit.lib.BranchConfigTest),
			typeof (org.eclipse.jgit.lib.BranchTrackingStatusTest),
			typeof (org.eclipse.jgit.lib.ConfigTest),
			typeof (org.eclipse.jgit.lib.ConstantsEncodingTest),
			typeof (org.eclipse.jgit.lib.DirCacheCheckoutMaliciousPathTest),
			typeof (org.eclipse.jgit.lib.DirCacheCheckoutTest),
			typeof (org.eclipse.jgit.lib.IndexDiffTest),
			typeof (org.eclipse.jgit.lib.IndexModificationTimesTest),
			typeof (org.eclipse.jgit.lib.MergeHeadMsgTest),
			typeof (org.eclipse.jgit.lib.ObjectCheckerTest),
			typeof (org.eclipse.jgit.lib.ObjectIdOwnerMapTest),
			typeof (org.eclipse.jgit.lib.ObjectIdRefTest),
			typeof (org.eclipse.jgit.lib.ObjectIdSubclassMapTest),
			typeof (org.eclipse.jgit.lib.ObjectIdTest),
			typeof (org.eclipse.jgit.lib.ObjectLoaderTest),
			typeof (org.eclipse.jgit.lib.RefDatabaseConflictingNamesTest),
			typeof (org.eclipse.jgit.lib.ReflogConfigTest),
			typeof (org.eclipse.jgit.lib.ReflogResolveTest),
			typeof (org.eclipse.jgit.lib.RefTest),
			typeof (org.eclipse.jgit.lib.RepositoryCacheTest),
			typeof (org.eclipse.jgit.lib.RepositoryResolveTest),
			typeof (org.eclipse.jgit.lib.SquashCommitMsgTest),
			typeof (org.eclipse.jgit.lib.SymbolicRefTest),
			typeof (org.eclipse.jgit.lib.T0001_PersonIdentTest),
			typeof (org.eclipse.jgit.lib.T0002_TreeTest),
			typeof (org.eclipse.jgit.lib.ThreadSafeProgressMonitorTest),
			typeof (org.eclipse.jgit.lib.ValidRefNameTest),
		};
		static readonly java.lang.Class[] merge = {
			typeof (org.eclipse.jgit.merge.CherryPickTest),
			typeof (org.eclipse.jgit.merge.MergeAlgorithmTest),
			typeof (org.eclipse.jgit.merge.MergeMessageFormatterTest),
			typeof (org.eclipse.jgit.merge.RecursiveMergerTest),
			typeof (org.eclipse.jgit.merge.ResolveMergerTest),
			typeof (org.eclipse.jgit.merge.SimpleMergeTest),
			typeof (org.eclipse.jgit.merge.SquashMessageFormatterTest),
		};
		static readonly java.lang.Class[] misc = {
			typeof (org.eclipse.jgit.nls.NLSTest),
			typeof (org.eclipse.jgit.nls.TranslationBundleTest),
			typeof (org.eclipse.jgit.notes.DefaultNoteMergerTest),
			typeof (org.eclipse.jgit.notes.LeafBucketTest),
			typeof (org.eclipse.jgit.notes.NoteMapMergerTest),
			typeof (org.eclipse.jgit.notes.NoteMapTest),
			typeof (org.eclipse.jgit.patch.EditListTest),
			typeof (org.eclipse.jgit.patch.FileHeaderTest),
			typeof (org.eclipse.jgit.patch.GetTextTest),
			typeof (org.eclipse.jgit.patch.PatchCcErrorTest),
			typeof (org.eclipse.jgit.patch.PatchCcTest),
			typeof (org.eclipse.jgit.patch.PatchErrorTest),
			typeof (org.eclipse.jgit.patch.PatchTest),
			typeof (org.eclipse.jgit.revplot.PlotCommitListTest),
		};
		static readonly java.lang.Class[] revwalk = {
			typeof (org.eclipse.jgit.revwalk.AlwaysEmptyRevQueueTest),
			typeof (org.eclipse.jgit.revwalk.DateRevQueueTest),
			typeof (org.eclipse.jgit.revwalk.FIFORevQueueTest),
			typeof (org.eclipse.jgit.revwalk.FooterLineTest),
			typeof (org.eclipse.jgit.revwalk.LIFORevQueueTest),
			typeof (org.eclipse.jgit.revwalk.MaxCountRevFilterTest),
			typeof (org.eclipse.jgit.revwalk.ObjectWalkTest),
			typeof (org.eclipse.jgit.revwalk.RevCommitListTest),
			typeof (org.eclipse.jgit.revwalk.RevCommitParseTest),
			typeof (org.eclipse.jgit.revwalk.RevFlagSetTest),
			typeof (org.eclipse.jgit.revwalk.RevObjectTest),
			typeof (org.eclipse.jgit.revwalk.RevTagParseTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkCullTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkFilterTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkFollowFilterTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkMergeBaseTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkPathFilter1Test),
			typeof (org.eclipse.jgit.revwalk.RevWalkPathFilter6012Test),
			typeof (org.eclipse.jgit.revwalk.RevWalkResetTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkShallowTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkSortTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkUtilsCountTest),
			typeof (org.eclipse.jgit.revwalk.RevWalkUtilsReachableTest),
			typeof (org.eclipse.jgit.revwalk.SkipRevFilterTest),
			typeof (org.eclipse.jgit.storage.file.FileBasedConfigTest),
			typeof (org.eclipse.jgit.submodule.SubmoduleAddTest),
			typeof (org.eclipse.jgit.submodule.SubmoduleInitTest),
			typeof (org.eclipse.jgit.submodule.SubmoduleStatusTest),
			typeof (org.eclipse.jgit.submodule.SubmoduleSyncTest),
			typeof (org.eclipse.jgit.submodule.SubmoduleUpdateTest),
			typeof (org.eclipse.jgit.submodule.SubmoduleWalkTest),
		};
		static readonly java.lang.Class[] transport = {
			typeof (org.eclipse.jgit.transport.BundleWriterTest),
			typeof (org.eclipse.jgit.transport.HttpAuthTest),
			typeof (org.eclipse.jgit.transport.LongMapTest),
			typeof (org.eclipse.jgit.transport.OpenSshConfigTest),
			typeof (org.eclipse.jgit.transport.PacketLineInTest),
			typeof (org.eclipse.jgit.transport.PacketLineOutTest),
			typeof (org.eclipse.jgit.transport.PackParserTest),
			typeof (org.eclipse.jgit.transport.PushProcessTest),
			typeof (org.eclipse.jgit.transport.ReceivePackAdvertiseRefsHookTest),
			typeof (org.eclipse.jgit.transport.RefSpecTest),
			typeof (org.eclipse.jgit.transport.RemoteConfigTest),
			typeof (org.eclipse.jgit.transport.SideBandOutputStreamTest),
			typeof (org.eclipse.jgit.transport.TransportTest),
			typeof (org.eclipse.jgit.transport.URIishTest),
		};
		static readonly java.lang.Class[] treewalk = {
			typeof (org.eclipse.jgit.treewalk.AbstractTreeIteratorTest),
			typeof (org.eclipse.jgit.treewalk.CanonicalTreeParserTest),
			typeof (org.eclipse.jgit.treewalk.EmptyTreeIteratorTest),
			typeof (org.eclipse.jgit.treewalk.FileTreeIteratorTest),
			typeof (org.eclipse.jgit.treewalk.ForPathTest),
			typeof (org.eclipse.jgit.treewalk.NameConflictTreeWalkTest),
			typeof (org.eclipse.jgit.treewalk.PostOrderTreeWalkTest),
			typeof (org.eclipse.jgit.treewalk.TreeWalkBasicDiffTest),
			typeof (org.eclipse.jgit.treewalk.filter.IndexDiffFilterTest),
			typeof (org.eclipse.jgit.treewalk.filter.InterIndexDiffFilterTest),
			typeof (org.eclipse.jgit.treewalk.filter.NotTreeFilterTest),
			typeof (org.eclipse.jgit.treewalk.filter.PathFilterGroupTest),
			typeof (org.eclipse.jgit.treewalk.filter.PathSuffixFilterTest),
			typeof (org.eclipse.jgit.treewalk.filter.TreeFilterTest),
		};
		static readonly java.lang.Class[] util = {
			typeof (org.eclipse.jgit.util.Base64Test),
			typeof (org.eclipse.jgit.util.BlockListTest),
			typeof (org.eclipse.jgit.util.ChangeIdUtilTest),
			typeof (org.eclipse.jgit.util.FileUtilTest),
			typeof (org.eclipse.jgit.util.GitDateFormatterTest),
			typeof (org.eclipse.jgit.util.GitDateParserBadlyFormattedTest),
			typeof (org.eclipse.jgit.util.GitDateParserTest),
			typeof (org.eclipse.jgit.util.IntListTest),
			typeof (org.eclipse.jgit.util.NBTest),
			typeof (org.eclipse.jgit.util.QuotedStringBourneStyleTest),
			typeof (org.eclipse.jgit.util.QuotedStringBourneUserPathStyleTest),
			typeof (org.eclipse.jgit.util.QuotedStringGitPathStyleTest),
			typeof (org.eclipse.jgit.util.RawCharUtilTest),
			typeof (org.eclipse.jgit.util.RawParseUtils_FormatTest),
			typeof (org.eclipse.jgit.util.RawParseUtils_HexParseTest),
			typeof (org.eclipse.jgit.util.RawParseUtils_LineMapTest),
			typeof (org.eclipse.jgit.util.RawParseUtils_MatchTest),
			typeof (org.eclipse.jgit.util.RawParseUtils_ParsePersonIdentTest),
			typeof (org.eclipse.jgit.util.RawParseUtilsTest),
			typeof (org.eclipse.jgit.util.RawSubStringPatternTest),
			typeof (org.eclipse.jgit.util.ReadLinesTest),
			typeof (org.eclipse.jgit.util.RefListTest),
			typeof (org.eclipse.jgit.util.RefMapTest),
			typeof (org.eclipse.jgit.util.RelativeDateFormatterTest),
			typeof (org.eclipse.jgit.util.StringUtilsTest),
			typeof (org.eclipse.jgit.util.TemporaryBufferTest),
			typeof (org.eclipse.jgit.util.io.AutoCRLFInputStreamTest),
			typeof (org.eclipse.jgit.util.io.AutoCRLFOutputStreamTest),
			typeof (org.eclipse.jgit.util.io.EolCanonicalizingInputStreamTest),
			typeof (org.eclipse.jgit.util.io.TimeoutInputStreamTest),
			typeof (org.eclipse.jgit.util.io.TimeoutOutputStreamTest),
			typeof (org.eclipse.jgit.util.io.UnionInputStreamTest),
		};

		// Split the tests so we don't wait a long time for all to run.
		[Test]
		public void TestApi ()
		{
			BaseRun (api);
		}

		[Test]
		public void TestDirCache ()
		{
			BaseRun (dircache);
		}

		[Test]
		public void TestDiff ()
		{
			BaseRun (diff);
		}

		[Test]
		public void TestInternal ()
		{
			BaseRun (@internal);
		}

		[Test]
		public void TestLib ()
		{
			BaseRun (lib);
		}

		[Test]
		public void TestMerge ()
		{
			BaseRun (merge);
		}

		[Test]
		public void TestMisc ()
		{
			BaseRun (misc);
		}

		[Test]
		public void TestRevwalk ()
		{
			BaseRun (revwalk);
		}

		[Test]
		public void TestTransport ()
		{
			BaseRun (transport);
		}

		[Test]
		public void TestTreewalk ()
		{
			BaseRun (treewalk);
		}

		[Test]
		public void TestUtil ()
		{
			BaseRun (util);
		}
	}
}

