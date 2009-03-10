// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3660 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class ArrayCreateExpressionTests
	{
		#region C#
		[Test]
		public void CSharpArrayCreateExpressionTest1()
		{
			ArrayCreateExpression ace = ParseUtilCSharp.ParseExpression<ArrayCreateExpression>("new int[5]");
			Assert.AreEqual("System.Int32", ace.CreateType.Type);
			Assert.IsTrue(ace.CreateType.IsKeyword);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(new int[] {0}, ace.CreateType.RankSpecifier);
		}
		
		[Test]
		public void CSharpImplicitlyTypedArrayCreateExpression()
		{
			ArrayCreateExpression ace = ParseUtilCSharp.ParseExpression<ArrayCreateExpression>("new[] { 1, 10, 100, 1000 }");
			Assert.AreEqual("", ace.CreateType.Type);
			Assert.AreEqual(0, ace.Arguments.Count);
			Assert.AreEqual(4, ace.ArrayInitializer.CreateExpressions.Count);
		}
		#endregion
		
		#region VB.NET
		
		[Test]
		public void VBNetArrayCreateExpressionTest1()
		{
			ArrayCreateExpression ace = ParseUtilVBNet.ParseExpression<ArrayCreateExpression>("new Integer() {1, 2, 3, 4}");
			
			Assert.AreEqual("System.Int32", ace.CreateType.Type);
			Assert.AreEqual(0, ace.Arguments.Count);
			Assert.AreEqual(new int[] {0}, ace.CreateType.RankSpecifier);
		}
		
		[Test]
		public void VBNetArrayCreateExpressionTest2()
		{
			ArrayCreateExpression ace = ParseUtilVBNet.ParseExpression<ArrayCreateExpression>("New Integer(0 To 5){0, 1, 2, 3, 4, 5}");
			
			Assert.AreEqual("System.Int32", ace.CreateType.Type);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(5, (ace.Arguments[0] as PrimitiveExpression).Value);
			Assert.AreEqual(new int[] {0}, ace.CreateType.RankSpecifier);
		}
		#endregion
		
	}
}
