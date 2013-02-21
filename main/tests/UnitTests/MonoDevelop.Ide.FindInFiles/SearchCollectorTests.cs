// 
// SearchCollectorTests.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang
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
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using NUnit.Framework;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture]
	public class SearchCollectorTests : UnitTests.TestBase
	{

		void VerifyResult<T> (List<T> result, List<T> expected)
		{
			Assert.AreEqual (expected.Count (), result.Count);
			foreach (var item in expected)
				Assert.AreEqual (true, result.Remove(item));
		}

		void TestCollectProjects (Solution solution, IEnumerable<IEntity> entities, IEnumerable<Project> expected)
		{
			VerifyResult (SearchCollector.CollectProjects (solution, entities).ToList (), expected.ToList ());
		}

		void VerifyResult (List<SearchCollector.FileList> result, List<Tuple<Project, IEnumerable<FilePath>>> expected)
		{
			Assert.AreEqual (expected.Count, result.Count);
			Console.WriteLine (result [0].Files.Count ());
			foreach (var item in expected ) {
				var tuple = item;
				Assert.AreEqual (1, result.RemoveAll (
					f => tuple.Item1 == f.Project && tuple.Item2.All (fileName => f.Files.Any (p => p == fileName))));
			}
		}

		void TestCollectFiles (Solution solution, IEnumerable<IEntity> entities, IEnumerable<Tuple<Project, IEnumerable<FilePath>>> expected)
		{
			VerifyResult (SearchCollector.CollectFiles (solution, entities).ToList (), expected.ToList ());
		}

		void TestCollectFiles (Project project, IEnumerable<IEntity> entities, IEnumerable<Tuple<Project, IEnumerable<FilePath>>> expected)
		{
			VerifyResult (SearchCollector.CollectFiles (project, entities).ToList (), expected.ToList ());
		}

		static Tuple<Project, IEnumerable<FilePath>> CreateTestTuple (Project project, IEnumerable<FilePath> files)
		{
			return Tuple.Create (project, files);
		}
		
		static Tuple<Project, IEnumerable<FilePath>> CreateTestTuple (Project project)
		{
			return Tuple.Create (project, project.Files.Select (f => f.FilePath));
		}

		[Test]
		public void TestCollectFiles ()
		{
			var code1 = @"
namespace project1 {
	class A
	{
		private void Method1() { }
		public void Method2() { }
	}
	public class B
	{ }
}";
			var project1 = new UnknownProject { FileName = "projectc1.csproj" };
			var project2 = new DotNetAssemblyProject { FileName = "projectc2.csproj" };
			project2.References.Add (new MonoDevelop.Projects.ProjectReference (project1));

			var solution = new Solution ();
			solution.RootFolder.AddItem (project1);
			solution.RootFolder.AddItem (project2);
			solution.RootFolder.AddItem (new UnknownProject { FileName = "dummy.csproj" });

			project1.AddFile (new ProjectFile ("dummy.cs"));
			TypeSystemService.LoadProject (project1);
			TypeSystemService.ParseFile (project1, "test.cs", "text/x-csharp", code1);
			var compilation = TypeSystemService.GetCompilation (project1);

			var typeA = compilation.MainAssembly.GetTypeDefinition ("project1", "A", 0);

			TestCollectFiles (project1, typeA.GetMembers (m => m.Name == "Method1"),
							  new [] { CreateTestTuple (project1, new [] { (FilePath)"test.cs" }) });
			TestCollectFiles (project1, new [] { typeA }, new [] { CreateTestTuple (project1) });
			TestCollectFiles (project1, typeA.GetMembers (m => m.Name == "Method2"), new [] { CreateTestTuple (project1) });
			TestCollectFiles (project1, typeA.GetMembers(), new [] { CreateTestTuple (project1) });

			TestCollectFiles (solution, typeA.GetMembers (m => m.Name == "Method1"),
							  new [] { CreateTestTuple (project1, new [] { (FilePath)"test.cs" }) });
			TestCollectFiles (solution, typeA.GetMembers (), new [] { CreateTestTuple (project1) });

			var typeB = compilation.MainAssembly.GetTypeDefinition ("project1", "B", 0);
			TestCollectFiles (solution, new [] { typeB }, new [] { CreateTestTuple (project1), CreateTestTuple (project2) });
			TestCollectFiles (solution, new [] { typeA, typeB }, new [] { CreateTestTuple (project1), CreateTestTuple (project2) });
		}

		[Test]
		public void TestCollectProjects ()
		{
			var code = @"
namespace project1 {
	class A
	{
		private void Method1() { }
		public void Method2() { }
	}
	public class B
	{
		private void Method1() { }
		protected void Method2() { }
	}
}";
			var project1 = new UnknownProject { FileName = "project1.csproj" };
			var project2 = new DotNetAssemblyProject { FileName = "project2.csproj" };
			var solution = new Solution ();
			solution.RootFolder.AddItem (project1);
			solution.RootFolder.AddItem (project2);
			solution.RootFolder.AddItem (new UnknownProject { FileName = "project3.csproj" });

			TypeSystemService.LoadProject (project1);
			TypeSystemService.ParseFile (project1, "test.cs", "text/x-csharp", code);
			var compilation = TypeSystemService.GetCompilation (project1);

			var typeA = compilation.MainAssembly.GetTypeDefinition ("project1", "A", 0);
			TestCollectProjects (solution, new [] { typeA }, new [] { project1 });
			TestCollectProjects (solution, typeA.GetMembers (), new [] { project1 });
			TestCollectProjects (solution, typeA.GetMembers (m => m.Name == "Method1"), new [] { project1 });
			TestCollectProjects (solution, typeA.GetMembers (m => m.Name == "Method2"), new [] { project1 });

			project2.References.Add (new MonoDevelop.Projects.ProjectReference (project1));
			var typeB = compilation.MainAssembly.GetTypeDefinition ("project1", "B", 0);
			TestCollectProjects (solution, new [] { typeB }, new Project [] { project1, project2 });
			TestCollectProjects (solution, typeB.GetMembers (), new Project [] { project1, project2 });
			TestCollectProjects (solution, typeB.GetMembers (m => m.Name == "Method1"), new [] { project1 });
			TestCollectProjects (solution, typeB.GetMembers (m => m.Name == "Method2"), new Project [] { project1, project2 });
		}

		[Test]
		public void TestCollectForExternalReference ()
		{
			var projects = new List<Project> ();
			var solution = new Solution ();
			for (int i = 0; i < 3; i++) {
				var project = new DotNetAssemblyProject { FileName = String.Format ("projectx{0}.csproj", i) };
				projects.Add (project);
				solution.RootFolder.AddItem (project);
				project.AddFile (new ProjectFile (String.Format ("dummy{0}.cs", i)));
				project.AddReference (typeof (object).Assembly.Location);
				TypeSystemService.LoadProject (project);
				TypeSystemService.GetProjectContentWrapper (project).ReconnectAssemblyReferences ();
			}
			solution.RootFolder.AddItem (new UnknownProject { FileName = "test.csproj" });

			var compilation = TypeSystemService.GetCompilation (projects[0]);
			var intType = compilation.GetAllTypeDefinitions ().First(t => t.Name == "Int32");
			Assert.AreEqual (null, TypeSystemService.GetProject (intType));
			TestCollectProjects (solution, new [] { intType }, projects);
			TestCollectFiles (solution, new [] { intType }, projects.Select (CreateTestTuple));
		}

	}
}
