// 
// ImplementInterfaceTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using NUnit.Framework;
using System.Linq;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CSharpBinding.Refactoring
{
	[TestFixture()]
	public class ImplementInterfaceTests : UnitTests.TestBase
	{
		static IUnresolvedAssembly Mscorlib  = new CecilLoader().LoadAssemblyFile(typeof(object).Assembly.Location);
		static IUnresolvedAssembly SystemCore = new CecilLoader().LoadAssemblyFile(typeof(System.Linq.Enumerable).Assembly.Location);
		
		void TestCreateInterface (string interfacecode, string outputString, string stubString = null)
		{
			var project = new UnknownProject ();
			project.FileName = "test.csproj";
			
			TypeSystem.TypeSystemService.Load (project);
			
			TypeSystem.TypeSystemService.ParseFile (project, "program.cs", "text/x-csharp", interfacecode);
			TypeSystem.TypeSystemService.ParseFile (project, "stub.cs", "text/x-csharp", "class Stub {\n "+stubString+"}\n");
			
			var wrapper = TypeSystem.TypeSystemService.GetProjectContentWrapper (project);
			wrapper.UpdateContent (c => c.AddAssemblyReferences (new [] { Mscorlib, SystemCore }));
			
			var pctx = TypeSystem.TypeSystemService.GetCompilation (project);
			
			var stubType = pctx.MainAssembly.GetTypeDefinition ("", "Stub", 0);
			var iface = pctx.MainAssembly.GetTypeDefinition ("", "ITest", 0);
			
			var gen = new CSharpCodeGenerator ();
			gen.EolMarker = "\n";
			gen.Compilation = pctx;
			string generated = gen.CreateInterfaceImplementation (stubType, stubType.Parts.First (), iface, false);
			Assert.IsNotEmpty (generated);
			// crop #region
			generated = generated.Substring (generated.IndexOf ("implementation") + "implementation".Length);
			generated = generated.Substring (0, generated.LastIndexOf ("#"));
			generated = generated.Trim ();
			if (outputString != generated)
				Console.WriteLine (generated);
			Assert.AreEqual (outputString, generated);
		}
		
		/// <summary>
		/// Bug 663842 - Interface implementation does not include constraints
		/// </summary>
		[Test()]
		public void TestBug663842 ()
		{
			TestCreateInterface (@"using System;
interface ITest {
	void MyMethod1<T> (T t) where T : new ();
	void MyMethod2<T> (T t) where T : class;
	void MyMethod3<T> (T t) where T : struct;
	void MyMethod4<T> (T t) where T : IDisposable, IServiceProvider;
}", @"public void MyMethod1<T> (T t) where T : new ()
	{
		throw new System.NotImplementedException ();
	}

	public void MyMethod2<T> (T t) where T : class
	{
		throw new System.NotImplementedException ();
	}

	public void MyMethod3<T> (T t) where T : struct
	{
		throw new System.NotImplementedException ();
	}

	public void MyMethod4<T> (T t) where T : System.IDisposable, System.IServiceProvider
	{
		throw new System.NotImplementedException ();
	}");
		}
		
		/// <summary>
		/// Bug 683007 - "Refactor/Implement implicit" creates explicit implementations of methods with same names
		/// </summary>
		[Test()]
		public void TestBug683007 ()
		{
			TestCreateInterface (@"interface ITest {
	void M1();
	void M1(int x);
}", @"public void M1 ()
	{
		throw new System.NotImplementedException ();
	}

	public void M1 (int x)
	{
		throw new System.NotImplementedException ();
	}");
		}
		
		/// <summary>
		/// Bug 243 - Implement implicit interface doesn't handle overloads correctly. 
		/// </summary>
		[Test()]
		public void TestBug243 ()
		{
			TestCreateInterface (@"interface ITest {
	void Inc (int n);
	void Inc (string message);
}", @"public void Inc (int n)
	{
		throw new System.NotImplementedException ();
	}

	public void Inc (string message)
	{
		throw new System.NotImplementedException ();
	}");
		}
		
		
		/// <summary>
		/// Bug 2074 - [Regression] Implement Interface implicitly does not check the methods already exist 
		/// </summary>
		[Test()]
		public void TestBug2074 ()
		{
			TestCreateInterface (@"interface ITest {
	void Method1 ();
	void Method2 ();
}", @"public void Method1 ()
	{
		throw new System.NotImplementedException ();
	}", "public void Method2 () {}");
		}
		
		/// <summary>
		/// Bug 3365 - MD cannot implement IEnumerable interface correctly  - MD cannot implement IEnumerable interface correctly 
		/// </summary>
		[Test()]
		public void TestBug3365 ()
		{
			TestCreateInterface (@"using System;
using System.Collections;

public interface IA
{
	bool GetEnumerator ();
}

public interface ITest : IA, IEnumerable
{
}
", @"public bool GetEnumerator ()
	{
		throw new System.NotImplementedException ();
	}
	#endregion

	#region IEnumerable implementation
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
	{
		throw new System.NotImplementedException ();
	}");
		}
	}
}

