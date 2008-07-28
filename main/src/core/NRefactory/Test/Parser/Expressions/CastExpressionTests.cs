// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 2676 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class CastExpressionTests
	{
		#region C#
		[Test]
		public void CSharpSimpleCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(MyObject)o");
			Assert.AreEqual("MyObject", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void CSharpArrayCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(MyType[])o");
			Assert.AreEqual("MyType", ce.CastTo.Type);
			Assert.AreEqual(new int[] { 0 }, ce.CastTo.RankSpecifier);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void NullablePrimitiveCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(int?)o");
			Assert.AreEqual("System.Nullable", ce.CastTo.SystemType);
			Assert.AreEqual("int", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void NullableCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(MyType?)o");
			Assert.AreEqual("System.Nullable", ce.CastTo.SystemType);
			Assert.AreEqual("MyType", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void NullableTryCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("o as int?");
			Assert.AreEqual("System.Nullable", ce.CastTo.SystemType);
			Assert.AreEqual("int", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void GenericCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(List<string>)o");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("string", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void GenericArrayCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(List<string>[])o");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("string", ce.CastTo.GenericTypes[0].Type);
			Assert.AreEqual(new int[] { 0 }, ce.CastTo.RankSpecifier);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void GenericArrayAsCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("o as List<string>[]");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("string", ce.CastTo.GenericTypes[0].Type);
			Assert.AreEqual(new int[] { 0 }, ce.CastTo.RankSpecifier);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void CSharpCastMemberReferenceOnParenthesizedExpression()
		{
			// yes, we really wanted to evaluate .Member on expr and THEN cast the result to MyType
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(MyType)(expr).Member");
			Assert.AreEqual("MyType", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is MemberReferenceExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void CSharpTryCastParenthesizedExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(o) as string");
			Assert.AreEqual("string", ce.CastTo.ToString());
			Assert.IsTrue(ce.Expression is ParenthesizedExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void CSharpCastNegation()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(uint)-negativeValue");
			Assert.AreEqual("uint", ce.CastTo.ToString());
			Assert.IsTrue(ce.Expression is UnaryOperatorExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void CSharpSubtractionIsNotCast()
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("(BigInt)-negativeValue");
			Assert.IsTrue(boe.Left is ParenthesizedExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
		}
		
		[Test]
		public void CSharpIntMaxValueToBigInt()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(BigInt)int.MaxValue");
			Assert.AreEqual("BigInt", ce.CastTo.ToString());
			Assert.IsTrue(ce.Expression is MemberReferenceExpression);
		}
		#endregion
		
		#region VB.NET
		void TestSpecializedCast(string castExpression, Type castType)
		{
			CastExpression ce = ParseUtilVBNet.ParseExpression<CastExpression>(castExpression);
			Assert.AreEqual(castType.FullName, ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.PrimitiveConversion, ce.CastType);
		}
		
		
		[Test]
		public void VBNetSimpleCastExpression()
		{
			CastExpression ce = ParseUtilVBNet.ParseExpression<CastExpression>("CType(o, MyObject)");
			Assert.AreEqual("MyObject", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Conversion, ce.CastType);
		}
		
		[Test]
		public void VBNetGenericCastExpression()
		{
			CastExpression ce = ParseUtilVBNet.ParseExpression<CastExpression>("CType(o, List(of T))");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("T", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Conversion, ce.CastType);
		}
		
		[Test]
		public void VBNetSimpleDirectCastExpression()
		{
			CastExpression ce = ParseUtilVBNet.ParseExpression<CastExpression>("DirectCast(o, MyObject)");
			Assert.AreEqual("MyObject", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void VBNetGenericDirectCastExpression()
		{
			CastExpression ce = ParseUtilVBNet.ParseExpression<CastExpression>("DirectCast(o, List(of T))");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("T", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void VBNetSimpleTryCastExpression()
		{
			CastExpression ce = ParseUtilVBNet.ParseExpression<CastExpression>("TryCast(o, MyObject)");
			Assert.AreEqual("MyObject", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void VBNetGenericTryCastExpression()
		{
			CastExpression ce = ParseUtilVBNet.ParseExpression<CastExpression>("TryCast(o, List(of T))");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("T", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void VBNetSpecializedBoolCastExpression()
		{
			TestSpecializedCast("CBool(o)", typeof(System.Boolean));
		}
		
		[Test]
		public void VBNetSpecializedCharCastExpression()
		{
			TestSpecializedCast("CChar(o)", typeof(System.Char));
		}
		
		
		[Test]
		public void VBNetSpecializedStringCastExpression()
		{
			TestSpecializedCast("CStr(o)", typeof(System.String));
		}
		
		[Test]
		public void VBNetSpecializedDateTimeCastExpression()
		{
			TestSpecializedCast("CDate(o)", typeof(System.DateTime));
		}
		
		[Test]
		public void VBNetSpecializedDecimalCastExpression()
		{
			TestSpecializedCast("CDec(o)", typeof(System.Decimal));
		}
		
		[Test]
		public void VBNetSpecializedSingleCastExpression()
		{
			TestSpecializedCast("CSng(o)", typeof(System.Single));
		}
		
		[Test]
		public void VBNetSpecializedDoubleCastExpression()
		{
			TestSpecializedCast("CDbl(o)", typeof(System.Double));
		}
		
		[Test]
		public void VBNetSpecializedByteCastExpression()
		{
			TestSpecializedCast("CByte(o)", typeof(System.Byte));
		}
		
		[Test]
		public void VBNetSpecializedInt16CastExpression()
		{
			TestSpecializedCast("CShort(o)", typeof(System.Int16));
		}
		
		[Test]
		public void VBNetSpecializedInt32CastExpression()
		{
			TestSpecializedCast("CInt(o)", typeof(System.Int32));
		}
		
		[Test]
		public void VBNetSpecializedInt64CastExpression()
		{
			TestSpecializedCast("CLng(o)", typeof(System.Int64));
		}
		
		[Test]
		public void VBNetSpecializedSByteCastExpression()
		{
			TestSpecializedCast("CSByte(o)", typeof(System.SByte));
		}
		
		[Test]
		public void VBNetSpecializedUInt16CastExpression()
		{
			TestSpecializedCast("CUShort(o)", typeof(System.UInt16));
		}
		
		[Test]
		public void VBNetSpecializedUInt32CastExpression()
		{
			TestSpecializedCast("CUInt(o)", typeof(System.UInt32));
		}
		
		[Test]
		public void VBNetSpecializedUInt64CastExpression()
		{
			TestSpecializedCast("CULng(o)", typeof(System.UInt64));
		}
		
		
		[Test]
		public void VBNetSpecializedObjectCastExpression()
		{
			TestSpecializedCast("CObj(o)", typeof(System.Object));
		}
		#endregion
	}
}
