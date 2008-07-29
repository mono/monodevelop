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
	public class FieldDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpSimpleFieldDeclarationTest()
		{
			FieldDeclaration fd = ParseUtilCSharp.ParseTypeMember<FieldDeclaration>("int[,,,] myField;");
			Assert.AreEqual("int", fd.TypeReference.Type);
			Assert.AreEqual(new int[] { 3 } , fd.TypeReference.RankSpecifier);
			Assert.AreEqual(1, fd.Fields.Count);
			
			Assert.AreEqual("myField", ((VariableDeclaration)fd.Fields[0]).Name);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleFieldDeclarationTest()
		{
			FieldDeclaration fd = ParseUtilVBNet.ParseTypeMember<FieldDeclaration>("myField As Integer(,,,)");
			Assert.AreEqual(1, fd.Fields.Count);
			
			Assert.AreEqual("Integer", ((VariableDeclaration)fd.Fields[0]).TypeReference.Type);
			Assert.AreEqual("System.Int32", ((VariableDeclaration)fd.Fields[0]).TypeReference.SystemType);
			Assert.AreEqual("myField", ((VariableDeclaration)fd.Fields[0]).Name);
			Assert.AreEqual(new int[] { 3 } , ((VariableDeclaration)fd.Fields[0]).TypeReference.RankSpecifier);
		}
		#endregion 
	}
}
