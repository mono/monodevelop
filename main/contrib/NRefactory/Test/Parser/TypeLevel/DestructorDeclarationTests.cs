// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 2676 $</version>
// </file>

using System;
using ICSharpCode.NRefactory.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class DestructorDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpDestructorDeclarationTest()
		{
			DestructorDeclaration dd = ParseUtilCSharp.ParseTypeMember<DestructorDeclaration>("~MyClass() {}");
		}
		
		[Test]
		public void CSharpExternDestructorDeclarationTest()
		{
			DestructorDeclaration dd = ParseUtilCSharp.ParseTypeMember<DestructorDeclaration>("extern ~MyClass();");
			Assert.AreEqual(Modifiers.Extern, dd.Modifier);
		}
		
		[Test]
		public void CSharpUnsafeDestructorDeclarationTest()
		{
			DestructorDeclaration dd = ParseUtilCSharp.ParseTypeMember<DestructorDeclaration>("unsafe ~MyClass() {}");
			Assert.AreEqual(Modifiers.Unsafe, dd.Modifier);
		}
		#endregion
		
		#region VB.NET
		// No VB.NET representation
		#endregion
	}
}
