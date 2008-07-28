// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 2819 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class PrimitiveExpressionTests
	{
		#region C#
		[Test]
		public void CSharpHexIntegerTest1()
		{
			InvocationExpression invExpr = ParseUtilCSharp.ParseExpression<InvocationExpression>("0xAFFE.ToString()");
			Assert.AreEqual(0, invExpr.Arguments.Count);
			Assert.IsTrue(invExpr.TargetObject is MemberReferenceExpression);
			MemberReferenceExpression fre = invExpr.TargetObject as MemberReferenceExpression;
			Assert.AreEqual("ToString", fre.MemberName);
			
			Assert.IsTrue(fre.TargetObject is PrimitiveExpression);
			PrimitiveExpression pe = fre.TargetObject as PrimitiveExpression;
			
			Assert.AreEqual("0xAFFE", pe.StringValue);
			Assert.AreEqual(0xAFFE, (int)pe.Value);
			
		}
		
		[Test]
		public void CSharpDoubleTest1()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>(".5e-06");
			Assert.AreEqual(".5e-06", pe.StringValue);
			Assert.AreEqual(.5e-06, (double)pe.Value);
		}
		
		[Test]
		public void CSharpCharTest1()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("'\\u0356'");
			Assert.AreEqual("'\\u0356'", pe.StringValue);
			Assert.AreEqual('\u0356', (char)pe.Value);
		}
		
		[Test]
		public void IntMinValueTest()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("-2147483648");
			Assert.AreEqual(-2147483648, (int)pe.Value);
		}
		
		[Test]
		public void IntMaxValueTest()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("2147483647");
			Assert.AreEqual(2147483647, (int)pe.Value);
			
			pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("2147483648");
			Assert.AreEqual(2147483648, (uint)pe.Value);
		}
		
		[Test]
		public void LongMinValueTest()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("-9223372036854775808");
			Assert.AreEqual(-9223372036854775808, (long)pe.Value);
		}
		
		[Test]
		public void LongMaxValueTest()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("9223372036854775807");
			Assert.AreEqual(9223372036854775807, (long)pe.Value);
			
			pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("9223372036854775808");
			Assert.AreEqual(9223372036854775808, (ulong)pe.Value);
		}
		
		[Test]
		public void CSharpStringTest1()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("\"\\n\\t\\u0005 Hello World !!!\"");
			Assert.AreEqual("\"\\n\\t\\u0005 Hello World !!!\"", pe.StringValue);
			Assert.AreEqual("\n\t\u0005 Hello World !!!", (string)pe.Value);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void PrimitiveExpression1Test()
		{
			InvocationExpression ie = ParseUtilVBNet.ParseExpression<InvocationExpression>("546.ToString()");
		}
		#endregion
	}
}
