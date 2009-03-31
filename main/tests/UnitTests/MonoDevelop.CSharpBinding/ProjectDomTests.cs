// 
// ProjectDomTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using NUnit.Framework;
using MonoDevelop.CSharpBinding.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.CSharpBinding
{
/*	[TestFixture()]
	public class ProjectDomTests : UnitTests.TestBase
	{
		public delegate void DomCallback (ProjectDom dom);
		
		public static void CheckDomCorrectness (IType type, DomCallback callback)
		{
			CheckDomCorrectness (new IType[] { type }, callback);
		}
		
		public static void CheckDomCorrectness (IEnumerable<IType> types, DomCallback callback)
		{
			//IParserDatabase database = new MonoDevelop.Projects.Dom.MemoryDatabase.MemoryDatabase ();
			IParserDatabase database = new MonoDevelop.Projects.Dom.Serialization.ParserDatabase ();
			ProjectDom dom = database.LoadSingleFileDom ("a.cs");
			Console.WriteLine ("dom:" + dom);
			CompilationUnit unit = new CompilationUnit ("a.cs");
			foreach (IType type in types) {
				unit.Add (type);
			}
			dom.UpdateFromParseInfo (unit);
			callback (dom);
		}
		
		[Test()]
		public void TestTypeInstantiation ()
		{
			DomType type = new DomType () {
				Name = "MyClass",
				ClassType = ClassType.Class
			};
			type.AddTypeParameter (new TypeParameter ("T"));
			IProperty prop = new DomProperty () {
				Name = "Prop",
				ReturnType = new DomReturnType ("T")
			};
			type.Add (prop);
			
			
			CheckDomCorrectness	 (type, delegate (ProjectDom dom) {
				IType result = dom.GetType ("MyClass", new IReturnType [] { new DomReturnType ("SomeNamespace.OtherType") }, true, true);
				Assert.IsNotNull (result);
				prop = result.Properties.FirstOrDefault ();
				Assert.IsNotNull (prop);
				Assert.AreEqual ("SomeNamespace.OtherType", prop.ReturnType.FullName);
			});
		}
		
		[Test()]
		public void TestGetNamespaceExists ()
		{
			CheckDomCorrectness	 (new DomType ("A.B.C.TestClass"), delegate (ProjectDom dom) {
				Assert.IsTrue (dom.NamespaceExists("A"));
				Assert.IsTrue (dom.NamespaceExists("A.B"));
				Assert.IsTrue (dom.NamespaceExists("A.B.C"));
				Assert.IsFalse (dom.NamespaceExists("B"));
				Assert.IsFalse (dom.NamespaceExists("C"));
				Assert.IsFalse (dom.NamespaceExists("B.C"));
				Assert.IsFalse (dom.NamespaceExists("A.C"));
				Assert.IsFalse (dom.NamespaceExists(".B"));
				Assert.IsFalse (dom.NamespaceExists(".C"));
			});
		}
		
		[Test()]
		public void TestGetInheritanceTree ()
		{
			DomType[] types = new DomType[] {
				new DomType ("A"),
				new DomType ("B") {
					BaseType = new DomReturnType ("A")
				},
				new DomType ("C") {
					BaseType = new DomReturnType ("B")
				}
			};
			
			CheckDomCorrectness	 (types, delegate (ProjectDom dom) {
				IType result = dom.GetType ("C");
				Assert.IsNotNull (result);
				HashSet<string> resTypes = new HashSet<string> ();
				foreach (IType t in dom.GetInheritanceTree (result)) {
					resTypes.Add (t.FullName);
				}
				Assert.IsTrue (resTypes.Contains ("A"));
				Assert.IsTrue (resTypes.Contains ("B"));
				Assert.IsTrue (resTypes.Contains ("C"));
			});
		}
			
		[Test()]
		public void TestGetInnerInheritedType ()
		{
			DomType[] types = new DomType[] {
				new DomType ("A"),
				new DomType ("B") {
					BaseType = new DomReturnType ("A")
				}
			};
			types[0].Add (new DomType ("Inner"));
			
			CheckDomCorrectness	 (types, delegate (ProjectDom dom) {
				IType result = dom.GetType ("B.Inner");
				Assert.IsNotNull (result);
			});
		}
	
		[Test()]
		public void TestGetSubclasses ()
		{
			DomType[] types = new DomType[] {
				new DomType ("A"),
				new DomType ("B") {
					BaseType = new DomReturnType ("A")
				},
				new DomType ("C") {
					BaseType = new DomReturnType ("B")
				}
			};
			
			CheckDomCorrectness	 (types, delegate (ProjectDom dom) {
				IType result = dom.GetType ("A");
				Assert.IsNotNull (result);
				HashSet<string> resTypes = new HashSet<string> ();
				foreach (IType t in dom.GetSubclasses (result)) {
					resTypes.Add (t.FullName);
				}
				Assert.IsTrue (resTypes.Contains ("A"));
				Assert.IsTrue (resTypes.Contains ("B"));
				Assert.IsTrue (resTypes.Contains ("C"));
			});
		}
	}
	*/
}
