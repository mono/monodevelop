// 
// MemberCollectorTests.cs
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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture ()]
	public class MemberCollectorTests : UnitTests.TestBase
	{
		IAssembly GenerateAssembly(Project project, string code)
		{
			var wrapper = TypeSystemService.LoadProject (project);
			project.Files.Add (new ProjectFile ("test.cs", BuildAction.Compile));
			TypeSystemService.ParseFile (project, "test.cs", "text/x-csharp", code);
			return wrapper.Compilation.MainAssembly;
		}

		List<IMember> CollectMembers (string code, string typeName, Predicate<IUnresolvedMember> filter1, Predicate<IMember> filter2,
									  bool includeOverloads, bool matchDeclaringType)
		{
			var fileName = string.Format ("test{0}.csproj", Environment.TickCount); // use a new file name for each test to avoid conflicts
			var project = new UnknownProject { FileName = fileName };

			var solution = new Solution ();
			solution.RootFolder.AddItem (project);

			var baseType = GenerateAssembly (project, code).GetTypeDefinition ("", typeName, 0);

			var members = baseType.GetMembers (filter1).Concat (baseType.GetConstructors (filter1));
			if (filter2 != null)
				members = members.Where (m => filter2(m));
			return MemberCollector.CollectMembers (solution, members.First (), ReferenceFinder.RefactoryScope.Solution,
				includeOverloads, matchDeclaringType).ToList ();
		}

		List<IMember> CollectMembers (string code, string typeName, string memberName, Predicate<IMember> searchMemberFilter,
									  bool includeOverloads, bool matchDeclaringType)
		{
			return CollectMembers (code, typeName, m => m.Name == memberName && m.DeclaringTypeDefinition.Name == typeName,
				searchMemberFilter, includeOverloads, matchDeclaringType);
		}
		
		void TestCollectMembers (string code, string typeName, string memberName, IEnumerable<Predicate<IMember>> expected,
								 Predicate<IMember> searchMemberFilter = null, bool includeOverloads = true, bool matchDeclaringType = false)
		{
			var result = CollectMembers (code, typeName, memberName, searchMemberFilter, includeOverloads, matchDeclaringType);
			VerifyResult (result, expected);
		}
		
		void TestCollectMembersForAllTypes (string code, string memberName, IList<String> typeNames, 
		                              	    IList<Predicate<IMember>> filters = null)
		{
			// all the members should be in the result
			var expected = new List<Predicate<IMember>>();
			for (int i = 0; i < typeNames.Count; i++)
				expected.Add (GetMemberFilter (typeNames[i], memberName, filters == null ? null : filters[i]));
			
			for (int i = 0; i < typeNames.Count; i++)
				TestCollectMembers (code, typeNames[i], memberName, expected, filters == null ? null : filters[i]);
		}
		
		void VerifyResult<T>(List<T> result, IEnumerable<Predicate<T>> expected)
		{
			Assert.AreEqual (expected.Count (), result.Count);
			foreach (var pred in expected)
				Assert.AreEqual (1, result.RemoveAll (pred));
		}
		
		bool MatchParameters (IMember m, IList<string> paramTypes)
		{
			var member = (IParameterizedMember)m;
			if (member.Parameters.Count != paramTypes.Count) return false;
			for (int i = 0; i < paramTypes.Count; i++) {
				if (member.Parameters[i].Type.Name != paramTypes[i]) return false;
			}
			return true;
		}
		
		Predicate<IMember> GetMemberFilter (string declaringType, string memberName, Predicate<IMember> filter = null)
		{
			return m => m.Name == memberName && m.DeclaringType.Name == declaringType && (filter == null || filter (m));
		}
		
		[Test]
		public void TestMethodOverrides ()
		{
			var code = @"
class A
{
	public virtual void Method () { }
}
class B : A
{
	public override void Method () { }
}
class C : B
{
	public sealed void Method () { }
}
class D : A
{
	public override void Method () { }
}";
			
			var memberName = "Method";
			var types = new [] {"A", "B", "C", "D"};
			TestCollectMembersForAllTypes (code, memberName, types);
		}
		
		[Test]
		public void TestEventOverrides ()
		{
			var code = @"
class A
{
	public virtual event EventHandler Event;
}
class B : A
{
	public override event EventHandler Event;
}
class C : B
{
	public sealed override event EventHandler Event;
}
class D : A
{
	public override event EventHandler Event;
}";
			var memberName = "Event";
			var types = new [] {"A", "B", "C", "D"};
			TestCollectMembersForAllTypes (code, memberName, types);
		}
		
		[Test]
		public void TestPropertyOverrides ()
		{
			var code = @"
class A
{
	public virtual int Prop
	{ get; set; }
}
class B : A
{
	public override int Prop
	{ get; set; }
}
class C : B
{
	public override sealed int Prop
	{ get; set; }
}
class D : A
{
	public override int Prop
	{ get; set; }
}";
			var memberName = "Prop";
			var types = new [] {"A", "B", "C", "D"};
			TestCollectMembersForAllTypes (code, memberName, types);
		}

		[Test]
		public void TestSingleInterfaceImpl ()
		{
			var code = @"
interface IA
{
	void Method();
}
class A : IA
{
	public virtual void Method() { };
}
class B : A
{
	public override void Method() { };
}
class C : IA
{
	public void Method() { };
}";
			var memberName = "Method";
			var types = new [] {"A", "B", "C", "IA"};
			TestCollectMembersForAllTypes (code, memberName, types);
		}
		
		[Test]
		public void TestMultiInterfacesImpl1 ()
		{
			var code = @"
interface IA
{
	void Method();
}
interface IB
{
	void Method();
}
class A : IA, IB
{
	public void Method() { }
}
class B : IA
{
	public void Method() { }
}
class C : IB
{
	public void Method() { }
}";
			string memberName = "Method";
			
			var expected1 = new List<Predicate<IMember>>();
			expected1.Add (GetMemberFilter ("A", memberName));
			expected1.Add (GetMemberFilter ("B", memberName));
			expected1.Add (GetMemberFilter ("C", memberName));
			expected1.Add (GetMemberFilter ("IA", memberName));
			expected1.Add (GetMemberFilter ("IB", memberName));
			TestCollectMembers (code, "A", memberName, expected1);
		}

		[Test]
		public void TestMultiInterfacesImpl2 ()
		{
			var code = @"
interface IA
{
	void Method();
}
interface IB
{
	void Method();
}
class A : IA, IB
{
	public void Method() { }
}
class B : IA
{
	public void Method() { }
}
class C : IB
{
	public void Method() { }
}";
			string memberName = "Method";

			var expected2 = new List<Predicate<IMember>>();
			expected2.Add (GetMemberFilter ("A", memberName));
			expected2.Add (GetMemberFilter ("B", memberName));
			expected2.Add (GetMemberFilter ("IA", memberName));
			TestCollectMembers (code, "B", memberName, expected2);
			TestCollectMembers (code, "IA", memberName, expected2);
		}

		[Test]
		public void TestMultiInterfacesImpl3 ()
		{
			var code = @"
interface IA
{
	void Method();
}
interface IB
{
	void Method();
}
class A : IA, IB
{
	public void Method() { }
}
class B : IA
{
	public void Method() { }
}
class C : IB
{
	public void Method() { }
}";
			string memberName = "Method";
			
			var expected3 = new List<Predicate<IMember>>();
			expected3.Add (GetMemberFilter ("A", memberName));
			expected3.Add (GetMemberFilter ("C", memberName));
			expected3.Add (GetMemberFilter ("IB", memberName));
			TestCollectMembers (code, "C", memberName, expected3);
			TestCollectMembers (code, "IB", memberName, expected3);
		}
		
		[Test]
		public void TestMethodOverloads ()
		{
			var code = @"
class A
{
	public void Method () { }
	public void Method (int i) { }
	public void Method (string i) { }
}
struct B
{
	public void Method () { }
	public void Method (int i) { }
	public void Method (string i) { }
}";
			var emptyParam = new string [] { };
			var intParam = new [] {"Int32"};
			var strParam = new [] {"String"};
			var paramList = new [] {emptyParam, intParam, strParam};
			var paramFilters = paramList.Select (p => new Predicate<IMember> (m => MatchParameters (m, p)));
			
			var memberName = "Method";
			var typeNames = new [] {"A", "B"};
			foreach (var typeName in typeNames) {
				var expected = paramFilters.Select (p => GetMemberFilter (typeName, memberName, p)).ToList ();
				foreach (var filter in paramFilters)
					TestCollectMembers (code, typeName, memberName, expected, filter);
			}
		}
		
		[Test]
		public void TestIncludeOverloads ()
		{
			var code = @"
class A
{
	public virtual void Method () { }
	public void Method (int i) { }
}
class B : A
{
	public override void Method () { }
	public void Method (string i) { }
}
class C : B
{
	public override void Method () { }
	public void Method (double i) { }
}
class D : A
{
	public override void Method () { }
	public void Method (char i) { }
}";
			var emptyParam = new string [] { };
			var intParam = new [] {"Int32"};
			var strParam = new [] {"String"};
			var doubleParam = new [] {"Double"};
			var charParam = new [] {"Char"};
			var paramList = new [] { emptyParam, emptyParam, emptyParam, emptyParam, intParam, strParam, doubleParam, charParam };
			var paramFilters = paramList.Select (p => new Predicate<IMember> (m => MatchParameters (m, p)));
			
			var memberName = "Method";
			var types = new [] {"A", "B", "C", "D", "A", "B", "C", "D"};
			TestCollectMembersForAllTypes (code, memberName, types, paramFilters.ToList ());
			
		}
		
		[Test]
		public void TestExcludeOverloads ()
		{
			var code = @"
class A
{
	public virtual void Method () { }
	public void Method (int i) { }
}
class B : A
{
	public override void Method () { }
	public void Method (string i) { }
}
class C : B
{
	public override void Method () { }
	public void Method (double i) { }
}
class D : A
{
	public override void Method () { }
	public void Method (char i) { }
}";
			var emptyParam = new string [] { };
			var intParam = new [] {"Int32"};
			var strParam = new [] {"String"};
			var doubleParam = new [] {"Double"};
			var charParam = new [] {"Char"};
			
			string memberName = "Method";
			
			var types = new [] {"A", "B", "C", "D"};
			var paramList = new [] {intParam, strParam, doubleParam, charParam};
			for (int i = 0; i < types.Length; i++) {
				Predicate<IMember> paramFilter = m => MatchParameters (m, paramList[i]);
				TestCollectMembers (code, types[i], memberName, new [] { GetMemberFilter (types[i], memberName, paramFilter)}, paramFilter, false);
			}
			
			var expected = types.Select (t => new Predicate<IMember> (GetMemberFilter (t, memberName, m => MatchParameters (m,emptyParam))))
				.ToList ();
			foreach (var type in types)
				TestCollectMembers (code, type,memberName, expected, m => MatchParameters (m, emptyParam), false);
			
		}
		
		[Test]
		public void TestInterfacePlusOverrides ()
		{
			string code = @"
class A
{
	public virtual void Method() { };
}
interface IA
{
	void Method();
}
class B : A, IA
{
	public override void Method() { };
}";
			string memberName = "Method";
			
			var expected1 = new List<Predicate<IMember>> ();
			expected1.Add (GetMemberFilter ("A", memberName));
			expected1.Add (GetMemberFilter ("B", memberName));
			
			var expected2 = new List<Predicate<IMember>> ();
			expected2.Add (GetMemberFilter ("IA", memberName));
			expected2.Add (GetMemberFilter ("B", memberName));
			
			var expected3 = new List<Predicate<IMember>> ();
			expected3.Add (GetMemberFilter ("A", memberName));
			expected3.Add (GetMemberFilter ("IA", memberName));
			expected3.Add (GetMemberFilter ("B", memberName));
			
			TestCollectMembers (code, "A", memberName, expected1);
			TestCollectMembers (code, "IA", memberName, expected2);
			TestCollectMembers (code, "B", memberName, expected3);
		}
		
		[Test]
		public void TestGetBaseTypes ()
		{
			string code = @"
class A { }
class B : A { }
interface IA { }
class C : A, IA { }
interface IB { }
class D : B, IA, IB { }
";
			var project = new UnknownProject ();
			project.FileName = "test.csproj";
			var assembly = GenerateAssembly (project, code);
			
			var A = assembly.GetTypeDefinition ("", "A", 0);
			var B = assembly.GetTypeDefinition ("", "B", 0);
			var C = assembly.GetTypeDefinition ("", "C", 0);
			var D = assembly.GetTypeDefinition ("", "D", 0);
			var IA = assembly.GetTypeDefinition ("", "IA", 0);
			var IB = assembly.GetTypeDefinition ("", "IB", 0);
			
			var result1 = MemberCollector.GetBaseTypes (new [] {A, B, C, D}).ToList ();
			VerifyResult (result1, new Predicate<ITypeDefinition>[] {t => t == A});
			
			var result2 = MemberCollector.GetBaseTypes (new [] {A, B, C, IA}).ToList ();
			VerifyResult (result2, new Predicate<ITypeDefinition>[] 
			             {t => t == A, t => t == IA});
			
			var result3 = MemberCollector.GetBaseTypes (new [] {A, B, C, D, IA, IB}).ToList ();
			VerifyResult (result3, new Predicate<ITypeDefinition>[] 
			             {t => t == A, t => t == IA, t => t == IB});
		}

		[Test]
		public void TestMatchDeclaringType ()
		{
			var code = @"
class A
{
	public virtual void Method() { };
	public void Method(int i) { };
}
class B : A
{
	public override void Method() { };
}";
			var memberName = "Method";
			var emptyParam = new string [] { };
			var intParam = new [] { "Int32" };

			var paramList = new [] { emptyParam, intParam };
			var expected1 = paramList.Select (p => GetMemberFilter ("A", memberName, m => MatchParameters (m, p))).ToList ();
			foreach (var filter in expected1)
				TestCollectMembers (code, "A", memberName, expected1, filter, true, true);

			var expected2 = new List<Predicate<IMember>> { GetMemberFilter ("A", memberName, m => MatchParameters (m, emptyParam)) };
			TestCollectMembers (code, "A", memberName, expected2, expected2 [0], false, true);

			var expected3 = new List<Predicate<IMember>> { GetMemberFilter ("B", memberName, m => MatchParameters (m, emptyParam)) };
			TestCollectMembers (code, "B", memberName, expected3, expected3 [0], false, true);
		}

		[Test]
		public void TestConstructor ()
		{
			var code = @"
class A
{
public A() { }
public A(int i) { }
}";
			var emptyParam = new string [] { };
			var intParam = new [] { "Int32" };
			var filters = new List<Predicate<IMember>> 
			{
				m => m.SymbolKind == SymbolKind.Constructor && MatchParameters(m, emptyParam),
				m => m.SymbolKind == SymbolKind.Constructor && MatchParameters(m, intParam)
			};
			
			foreach (var filter in filters) {
				var result1 = CollectMembers (code, "A", m => true, filter, true, false);
				VerifyResult (result1, filters);
			}
			
			var result2 = CollectMembers (code, "A", m => true, filters [0], false, true);
			VerifyResult (result2, new [] { filters [0] });
		}


		[Test]
		public void TestStaticConstructor ()
		{
			var code = @"
class A
{
public A() { }
static A() { }
}";
			var emptyParam = new string [] { };
			Predicate<IMember> filter = m => m.SymbolKind == SymbolKind.Constructor && MatchParameters(m, emptyParam);
			var result1 = CollectMembers (code, "A", m => true, filter, true, false);
			Assert.AreEqual (2, result1.Count);
		}


		[Test]
		public void TestShadowedMember ()
		{
			var code = @"
class A
{
	public int Prop
	{ get; set; }
}
class B : A
{
	public int Prop
	{ get; set; }
}";
			var members = CollectMembers (code, "A", "Prop", m => true, true, false);
			Assert.AreEqual (1, members.Count);
		}

		/// <summary>
		/// Bug 11714 - Rename interface member does not affect other implementations 
		/// </summary>
		[Test]
		public void TestBug11714 ()
		{
			var code = @"
class A : IA
{
	public int Prop
	{ get; set; }
}

interface IA { int Prop { get; set; } }

class B : IA
{
	public int Prop
	{ get; set; }
}";
			var members = CollectMembers (code, "A", "Prop", m => true, true, false);
			Assert.AreEqual (3, members.Count);
		}

	}
}
