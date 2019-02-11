//
// MonoDevelopProjectCacheHostServiceTests.cs
//
// Author:
//       Marius <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.RoslynServices
{
	[TestFixture]
	public class MonoDevelopProjectCacheHostServiceTests
	{
		static class FinalizerHelpers
		{
			static IntPtr aptr;

			static unsafe void NoPinActionHelper (Action act, int depth)
			{
				// Avoid tail calls
				int* values = stackalloc int [20];
				aptr = new IntPtr (values);

				if (depth <= 0) {
					//
					// When the action is called, this new thread might have not allocated
					// anything yet in the nursery. This means that the address of the first
					// object that would be allocated would be at the start of the tlab and
					// implicitly the end of the previous tlab (address which can be in use
					// when allocating on another thread, at checking if an object fits in
					// this other tlab). We allocate a new dummy object to avoid this type
					// of false pinning for most common cases.
					//
					new object ();
					act ();
				} else {
					NoPinActionHelper (act, depth - 1);
				}
			}

			public static void PerformNoPinAction (Action act)
			{
				Thread thr = new Thread (() => NoPinActionHelper (act, 128));
				thr.Start ();
				thr.Join ();
			}
		}

		// Copied tests from roslyn
		void Test (Action<IProjectCacheHostService, ProjectId, ICachedObjectOwner, ObjectReference<object>> action)
		{
			// Putting cacheService.CreateStrongReference in a using statement
			// creates a temporary local that isn't collected in Debug builds
			// Wrapping it in a lambda allows it to get collected.
			var cacheService = new ProjectCacheService (null, int.MaxValue);
			var projectId = ProjectId.CreateNewId ();
			var owner = new Owner ();
			var instance = ObjectReference.CreateFromFactory (() => new object ());

			action (cacheService, projectId, owner, instance);
		}

		[Test]
		public void TestCacheKeepsObjectAlive1 ()
		{
			Test ((cacheService, projectId, owner, instance) => {
				using (cacheService.EnableCaching (projectId)) {
					instance.UseReference (i => cacheService.CacheObjectIfCachingEnabledForKey (projectId, (object)owner, i));

					instance.AssertHeld ();
				}

				instance.AssertReleased ();

				GC.KeepAlive (owner);
			});
		}

		[Test]
		public void TestCacheKeepsObjectAlive2 ()
		{
			Test ((cacheService, projectId, owner, instance) => {
				using (cacheService.EnableCaching (projectId)) {
					instance.UseReference (i => cacheService.CacheObjectIfCachingEnabledForKey (projectId, owner, i));

					instance.AssertHeld ();
				}

				instance.AssertReleased ();

				GC.KeepAlive (owner);
			});
		}

		[Test]
		public void TestCacheDoesNotKeepObjectsAliveAfterOwnerIsCollected1 ()
		{
			Test ((cacheService, projectId, owner, instance) => {
				using (cacheService.EnableCaching (projectId)) {
					cacheService.CacheObjectIfCachingEnabledForKey (projectId, (object)owner, instance);
					owner = null;

					instance.AssertReleased ();
				}
			});
		}

		[Test]
		[Ignore ("Disabled for now")]
		public void TestCacheDoesNotKeepObjectsAliveAfterOwnerIsCollected2 ()
		{
			Test ((cacheService, projectId, owner, instance) => {
				using (cacheService.EnableCaching (projectId)) {
					cacheService.CacheObjectIfCachingEnabledForKey (projectId, owner, instance);
					owner = null;

					instance.AssertReleased ();
				}
			});
		}

		[Test]
		public void TestImplicitCacheKeepsObjectAlive1 ()
		{
			var workspace = new AdhocWorkspace (MockHostServices.Instance, workspaceKind: WorkspaceKind.Host);
			var cacheService = new ProjectCacheService (workspace, int.MaxValue);
			var reference = ObjectReference.CreateFromFactory (() => new object ());
			reference.UseReference (r => cacheService.CacheObjectIfCachingEnabledForKey (ProjectId.CreateNewId (), (object)null, r));
			reference.AssertHeld ();

			GC.KeepAlive (cacheService);
		}

		static ObjectReference<object> PutObjectInImplicitCache (ProjectCacheService cacheService)
		{
			var reference = ObjectReference.CreateFromFactory (() => new object ());

			reference.UseReference (r => cacheService.CacheObjectIfCachingEnabledForKey (ProjectId.CreateNewId (), (object)null, r));

			return reference;
		}

		[Test]
		public void TestP2PReference ()
		{
			var workspace = new AdhocWorkspace ();

			var project1 = ProjectInfo.Create (ProjectId.CreateNewId (), VersionStamp.Default, "proj1", "proj1", LanguageNames.CSharp);
			var project2 = ProjectInfo.Create (ProjectId.CreateNewId (), VersionStamp.Default, "proj2", "proj2", LanguageNames.CSharp, projectReferences: new List<ProjectReference> { new ProjectReference (project1.Id) });
			var solutionInfo = SolutionInfo.Create (SolutionId.CreateNewId (), VersionStamp.Default, projects: new ProjectInfo [] { project1, project2 });

			var solution = workspace.AddSolution (solutionInfo);

			var instanceTracker = ObjectReference.CreateFromFactory (() => new object ());

			var cacheService = new ProjectCacheService (workspace, int.MaxValue);
			using (var cache = cacheService.EnableCaching (project2.Id)) {
				instanceTracker.UseReference (r => cacheService.CacheObjectIfCachingEnabledForKey (project1.Id, (object)null, r));
				solution = null;

				workspace.OnProjectRemoved (project1.Id);
				workspace.OnProjectRemoved (project2.Id);
			}

			// make sure p2p reference doesn't go to implicit cache
			instanceTracker.AssertReleased ();
		}

		[Test]
		public void TestEjectFromImplicitCache ()
		{
			ProjectCacheService cache = null;
			ObjectReference<Compilation> weakFirst = null, weakLast = null;

			FinalizerHelpers.PerformNoPinAction (() => {
				int total = ProjectCacheService.ImplicitCacheSize + 1;
				var compilations = new Compilation [total];
				for (int i = 0; i < total; i++) {
					compilations [i] = CSharpCompilation.Create (i.ToString ());
				}

				weakFirst = ObjectReference.Create (compilations [0]);
				weakLast = ObjectReference.Create (compilations [total - 1]);

				var workspace = new AdhocWorkspace (MockHostServices.Instance, workspaceKind: WorkspaceKind.Host);
				cache = new ProjectCacheService (workspace, int.MaxValue);
				for (int i = 0; i < total; i++) {
					cache.CacheObjectIfCachingEnabledForKey (ProjectId.CreateNewId (), (object)null, compilations [i]);
				}
			});

			weakFirst.AssertReleased ();
			weakLast.AssertHeld ();

			GC.KeepAlive (cache);
		}

		[Test]
		public void TestCacheCompilationTwice ()
		{
			ObjectReference<CSharpCompilation> weak1 = null, weak3 = null;
			ProjectCacheService cache = null;

			FinalizerHelpers.PerformNoPinAction (() => {
				var comp1 = CSharpCompilation.Create ("1");
				var comp2 = CSharpCompilation.Create ("2");
				var comp3 = CSharpCompilation.Create ("3");

				weak3 = ObjectReference.Create (comp3);
				weak1 = ObjectReference.Create (comp1);

				var workspace = new AdhocWorkspace (MockHostServices.Instance, workspaceKind: WorkspaceKind.Host);
				cache = new ProjectCacheService (workspace, int.MaxValue);
				var key = ProjectId.CreateNewId ();
				var owner = new object ();
				cache.CacheObjectIfCachingEnabledForKey (key, owner, comp1);
				cache.CacheObjectIfCachingEnabledForKey (key, owner, comp2);
				cache.CacheObjectIfCachingEnabledForKey (key, owner, comp3);

				// When we cache 3 again, 1 should stay in the cache
				cache.CacheObjectIfCachingEnabledForKey (key, owner, comp3);
			});

			weak3.AssertHeld ();
			weak1.AssertHeld ();

			GC.KeepAlive (cache);
		}

		class Owner : ICachedObjectOwner
		{
			object ICachedObjectOwner.CachedObject { get; set; }
		}

		class MockHostServices : HostServices
		{
			public static readonly MockHostServices Instance = new MockHostServices ();

			MockHostServices () { }

			protected internal override HostWorkspaceServices CreateWorkspaceServices (Workspace workspace)
			{
				return new MockHostWorkspaceServices (this, workspace);
			}
		}

		class MockHostWorkspaceServices : HostWorkspaceServices
		{
			readonly HostServices _hostServices;
			readonly Workspace _workspace;
			static readonly IWorkspaceTaskSchedulerFactory s_taskSchedulerFactory = new WorkspaceTaskSchedulerFactory ();

			public MockHostWorkspaceServices (HostServices hostServices, Workspace workspace)
			{
				_hostServices = hostServices;
				_workspace = workspace;
			}

			public override HostServices HostServices => _hostServices;

			public override Workspace Workspace => _workspace;

			public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService> (MetadataFilter filter)
			{
				return ImmutableArray<TLanguageService>.Empty;
			}

			public override TWorkspaceService GetService<TWorkspaceService> ()
			{
				if (s_taskSchedulerFactory is TWorkspaceService) {
					return (TWorkspaceService)s_taskSchedulerFactory;
				}

				return default (TWorkspaceService);
			}
		}
	}
}
