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
	public class IndexerDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpIndexerDeclarationTest()
		{
			IndexerDeclaration id = ParseUtilCSharp.ParseTypeMember<IndexerDeclaration>("int this[int a, string b] { get { } set { } }");
			Assert.AreEqual(2, id.Parameters.Count);
			Assert.IsTrue(id.HasGetRegion, "No get region found!");
			Assert.IsTrue(id.HasSetRegion, "No set region found!");
		}
		
		[Test]
		public void CSharpIndexerImplementingInterfaceTest()
		{
			IndexerDeclaration id = ParseUtilCSharp.ParseTypeMember<IndexerDeclaration>("int MyInterface.this[int a, string b] { get { } set { } }");
			Assert.AreEqual(2, id.Parameters.Count);
			Assert.IsTrue(id.HasGetRegion, "No get region found!");
			Assert.IsTrue(id.HasSetRegion, "No set region found!");
			
			Assert.AreEqual("MyInterface", id.InterfaceImplementations[0].InterfaceType.Type);
		}
		
		[Test]
		public void CSharpIndexerImplementingGenericInterfaceTest()
		{
			IndexerDeclaration id = ParseUtilCSharp.ParseTypeMember<IndexerDeclaration>("int MyInterface<string>.this[int a, string b] { get { } set { } }");
			Assert.AreEqual(2, id.Parameters.Count);
			Assert.IsTrue(id.HasGetRegion, "No get region found!");
			Assert.IsTrue(id.HasSetRegion, "No set region found!");
			
			Assert.AreEqual("MyInterface", id.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", id.InterfaceImplementations[0].InterfaceType.GenericTypes[0].SystemType);
		}
		#endregion
		
		#region VB.NET
		// no vb.net representation (indexers are properties named "item" in vb.net)
		#endregion
	}
}
