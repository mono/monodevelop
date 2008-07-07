// DomCompilationUnitTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MonoDevelop.Projects.Dom;

namespace UnitTests
{
	[TestFixture()]
	public class DomCompilationUnitTests
	{
		[Test()]
		public void TestGetNamespaceContentsCase1 ()
		{
			CompilationUnit unit = new CompilationUnit ("file.cs");
			unit.Add (new DomType ("ANamespace.AnotherNamespace.AClass"));
			unit.Add (new DomType ("ANamespace.AnotherNamespace.BClass"));
			unit.Add (new DomType ("ANamespace.AnotherNamespace.CClass"));
			unit.Add (new DomType ("ANamespace.AClass2"));
			unit.Add (new DomType ("ANamespace.BClass2"));
			unit.Add (new DomType ("CClass3"));
			
			List<IMember> member = new List<IMember> ();
			unit.GetNamespaceContents (member, "", true);
			Assert.AreEqual (2, member.Count);
			Namespace ns = member[0] as Namespace;
			Assert.IsNotNull (ns);
			Assert.AreEqual ("ANamespace", ns.Name);
			
			IType type = member[1] as IType;
			Assert.IsNotNull (type);
			Assert.AreEqual ("CClass3", type.FullName);
		}
		
		[Test()]
		public void TestGetNamespaceContentsCase2 ()
		{
			CompilationUnit unit = new CompilationUnit ("file.cs");
			unit.Add (new DomType ("ANamespace.AnotherNamespace.AClass"));
			unit.Add (new DomType ("ANamespace.AnotherNamespace.BClass"));
			unit.Add (new DomType ("ANamespace.AnotherNamespace.CClass"));
			unit.Add (new DomType ("ANamespace.AClass2"));
			unit.Add (new DomType ("ANamespace.BClass2"));
			unit.Add (new DomType ("CClass3"));
			
			List<IMember> member = new List<IMember> ();
			unit.GetNamespaceContents (member, "ANamespace", true);
			
			Assert.AreEqual (3, member.Count);
			Namespace ns = member[0] as Namespace;
			Assert.IsNotNull (ns);
			Assert.AreEqual ("AnotherNamespace", ns.Name);
			
			IType type = member[1] as IType;
			Assert.IsNotNull (type);
			Assert.AreEqual ("AClass2", type.Name);
			
			type = member[2] as IType;
			Assert.IsNotNull (type);
			Assert.AreEqual ("BClass2", type.Name);
		}
		
		[Test()]
		public void TestGetNamespaceContentsCase3 ()
		{
			CompilationUnit unit = new CompilationUnit ("file.cs");
			unit.Add (new DomType ("ANamespace.AnotherNamespace.AClass"));
			unit.Add (new DomType ("ANamespace.AnotherNamespace.BClass"));
			unit.Add (new DomType ("ANamespace.AnotherNamespace.CClass"));
			unit.Add (new DomType ("ANamespace.AClass2"));
			unit.Add (new DomType ("ANamespace.BClass2"));
			unit.Add (new DomType ("CClass3"));
			
			List<IMember> member = new List<IMember> ();
			unit.GetNamespaceContents (member, "ANamespace.AnotherNamespace", true);
			Assert.AreEqual (3, member.Count);
			
			IType type = member[0] as IType;
			Assert.IsNotNull (type);
			Assert.AreEqual ("AClass", type.Name);
			
			type = member[1] as IType;
			Assert.IsNotNull (type);
			Assert.AreEqual ("BClass", type.Name);
			
			type = member[2] as IType;
			Assert.IsNotNull (type);
			Assert.AreEqual ("CClass", type.Name);
		}
		
		[Test()]
		public void TestGetNamespaceContentsCase4 ()
		{
			CompilationUnit unit = new CompilationUnit ("file.cs");
			unit.Add (new DomType ("ANamespace.AnotherNamespace.AClass"));
			unit.Add (new DomType ("ANamespace.AnotherNamespace.BClass"));
			unit.Add (new DomType ("ANamespace.AnotherNamespace.CClass"));
			unit.Add (new DomType ("ANamespace.AClass2"));
			unit.Add (new DomType ("ANamespace.BClass2"));
			unit.Add (new DomType ("CClass3"));
			
			List<IMember> member = new List<IMember> ();
			unit.GetNamespaceContents (member, "ANamespace.NotExist", true);
			Assert.AreEqual (0, member.Count);
		}
	}
}
