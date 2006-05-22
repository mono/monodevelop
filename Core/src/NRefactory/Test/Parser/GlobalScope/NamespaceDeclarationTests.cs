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
	public class NamespaceDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpSimpleNamespaceTest()
		{
			string program = "namespace TestNamespace {\n" +
			                 "}\n";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			Assert.AreEqual("TestNamespace", ns.Name);
		}

		
		[Test]
		public void CSharpJuggedNamespaceTest()
		{
			string program = "namespace N1 {//TestNamespace\n" +
			                 "    namespace N2 {// Declares a namespace named N2 within N1.\n" +
			                 "    }\n" +
			                 "}\n";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			
			Assert.AreEqual("N1", ns.Name);
			
			Assert.IsTrue(ns.Children[0] is NamespaceDeclaration);
			
			ns = (NamespaceDeclaration)ns.Children[0];
			
			Assert.AreEqual("N2", ns.Name);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleNamespaceTest()
		{
			string program = "Namespace TestNamespace" + Environment.NewLine +
			                 "End Namespace" +Environment.NewLine;
			NamespaceDeclaration ns = ParseUtilVBNet.ParseGlobal<NamespaceDeclaration>(program);
			Assert.AreEqual("TestNamespace", ns.Name);
		}
		
		[Test]
		public void VBNetJuggedNamespaceTest()
		{
			string program = "Namespace N1 'TestNamespace\n" +
			                 "    Namespace N2   ' Declares a namespace named N2 within N1.\n" +
			                 "    End Namespace\n" +
			                 "End Namespace\n";
			
			NamespaceDeclaration ns = ParseUtilVBNet.ParseGlobal<NamespaceDeclaration>(program);
			
			Assert.AreEqual("N1", ns.Name);
			
			Assert.IsTrue(ns.Children[0] is NamespaceDeclaration);
			
			ns = (NamespaceDeclaration)ns.Children[0];
			
			Assert.AreEqual("N2", ns.Name);
		}
		#endregion
	}
}
