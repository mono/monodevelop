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
using MonoDevelop.CSharp.Refactoring.CreateMethod;
using NUnit.Framework;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Projects.CodeGeneration;
using System.Linq;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.Projects.Dom;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Refactoring
{
	[TestFixture()]
	public class ImplementInterfaceTests : UnitTests.TestBase
	{
		void TestCreateInterface (string interfacecode, string outputString)
		{
			var dom = new SimpleProjectDom ();
			
			var parser = new McsParser ();
			var unit = parser.Parse (dom, "Interface.cs", interfacecode);
			
			DomType stubType = new DomType ("Stub");
			stubType.SourceProjectDom = dom;
			stubType.CompilationUnit = new CompilationUnit ("Stub.cs");
			var iface = unit.CompilationUnit.Types[0];
			var gen = new CSharpCodeGenerator ();
			gen.EolMarker = "\n";
			string generated = gen.CreateInterfaceImplementation (stubType, iface, false);
			// crop #region
			generated = generated.Substring (generated.IndexOf ("implementation") + "implementation".Length);
			generated = generated.Substring (0, generated.LastIndexOf ("#"));
			generated = generated.Trim ();
			System.Console.WriteLine (generated);
			Assert.AreEqual (outputString, generated);
		}
		
		/// <summary>
		/// Bug 663842 - Interface implementation does not include constraints
		/// </summary>
		[Test()]
		public void TestBug663842 ()
		{
			TestCreateInterface (@"interface ITest {
	void MyMethod1<T> (T t) where T : new ();
	void MyMethod2<T> (T t) where T : class;
	void MyMethod3<T> (T t) where T : struct;
	void MyMethod4<T> (T t) where T : A, B;
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

	public void MyMethod4<T> (T t) where T : A, B
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
	}
}

