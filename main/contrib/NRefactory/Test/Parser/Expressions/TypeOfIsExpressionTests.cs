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
	public class TypeOfIsExpressionTests
	{
		#region C#
		[Test]
		public void GenericArrayIsExpression()
		{
			TypeOfIsExpression ce = ParseUtilCSharp.ParseExpression<TypeOfIsExpression>("o is List<string>[]");
			Assert.AreEqual("List", ce.TypeReference.Type);
			Assert.AreEqual("System.String", ce.TypeReference.GenericTypes[0].Type);
			Assert.AreEqual(new int[] { 0 }, ce.TypeReference.RankSpecifier);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
		
		[Test]
		public void NullableIsExpression()
		{
			TypeOfIsExpression ce = ParseUtilCSharp.ParseExpression<TypeOfIsExpression>("o is int?");
			Assert.AreEqual("System.Nullable", ce.TypeReference.Type);
			Assert.AreEqual("System.Int32", ce.TypeReference.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
		
		[Test]
		public void NullableIsExpressionInBinaryOperatorExpression()
		{
			BinaryOperatorExpression boe;
			boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("o is int? == true");
			TypeOfIsExpression ce = (TypeOfIsExpression)boe.Left;
			Assert.AreEqual("System.Nullable", ce.TypeReference.Type);
			Assert.AreEqual("System.Int32", ce.TypeReference.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleTypeOfIsExpression()
		{
			TypeOfIsExpression ce = ParseUtilVBNet.ParseExpression<TypeOfIsExpression>("TypeOf o Is MyObject");
			Assert.AreEqual("MyObject", ce.TypeReference.Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
		
		[Test]
		public void VBNetGenericTypeOfIsExpression()
		{
			TypeOfIsExpression ce = ParseUtilVBNet.ParseExpression<TypeOfIsExpression>("TypeOf o Is List(of T)");
			Assert.AreEqual("List", ce.TypeReference.Type);
			Assert.AreEqual("T", ce.TypeReference.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
		#endregion
	}
}
