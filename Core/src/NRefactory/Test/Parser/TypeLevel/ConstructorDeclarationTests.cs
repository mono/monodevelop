// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.Drawing;
using System.IO;

using NUnit.Framework;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class ConstructorDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpConstructorDeclarationTest1()
		{
			ConstructorDeclaration cd = ParseUtilCSharp.ParseTypeMember<ConstructorDeclaration>("MyClass() {}");
			Assert.IsTrue(cd.ConstructorInitializer.IsNull);
		}
		
		[Test]
		public void CSharpConstructorDeclarationTest2()
		{
			ConstructorDeclaration cd = ParseUtilCSharp.ParseTypeMember<ConstructorDeclaration>("MyClass() : this(5) {}");
			Assert.AreEqual(ConstructorInitializerType.This, cd.ConstructorInitializer.ConstructorInitializerType);
			Assert.AreEqual(1, cd.ConstructorInitializer.Arguments.Count);
		}
		
		[Test]
		public void CSharpConstructorDeclarationTest3()
		{
			ConstructorDeclaration cd = ParseUtilCSharp.ParseTypeMember<ConstructorDeclaration>("MyClass() : base(1, 2, 3) {}");
			Assert.AreEqual(ConstructorInitializerType.Base, cd.ConstructorInitializer.ConstructorInitializerType);
			Assert.AreEqual(3, cd.ConstructorInitializer.Arguments.Count);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetConstructorDeclarationTest1()
		{
			string program = @"Sub New()
								End Sub";
			ConstructorDeclaration cd = ParseUtilVBNet.ParseTypeMember<ConstructorDeclaration>(program);
			Assert.IsTrue(cd.ConstructorInitializer.IsNull);
		}
		
		[Test]
		public void VBNetConstructorDeclarationTest2()
		{
			ConstructorDeclaration cd = ParseUtilVBNet.ParseTypeMember<ConstructorDeclaration>("Sub New(x As Integer, Optional y As String) \nEnd Sub");
			Assert.AreEqual(2, cd.Parameters.Count);
			Assert.AreEqual("Integer", cd.Parameters[0].TypeReference.Type);
			Assert.AreEqual("String", cd.Parameters[1].TypeReference.Type);
			Assert.AreEqual(ParamModifier.Optional, cd.Parameters[1].ParamModifier & ParamModifier.Optional);
		}
		#endregion 
	}
}
