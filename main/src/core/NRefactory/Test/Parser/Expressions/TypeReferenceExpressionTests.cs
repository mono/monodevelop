// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1301 $</version>
// </file>

/*
 * Created by SharpDevelop.
 * User: Omnibrain
 * Date: 13.09.2004
 * Time: 19:54
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class TypeReferenceExpressionTests
	{
		#region C#
		[Test]
		public void GlobalTypeReferenceExpression()
		{
			TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::System");
			Assert.AreEqual("System", tr.TypeReference.Type);
			Assert.IsTrue(tr.TypeReference.IsGlobal);
		}
		
		[Test]
		public void GlobalTypeReferenceExpressionWithoutTypeName()
		{
			TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::", true);
			Assert.AreEqual("?", tr.TypeReference.Type);
			Assert.IsTrue(tr.TypeReference.IsGlobal);
		}
		
		[Test]
		public void IntReferenceExpression()
		{
			FieldReferenceExpression fre = ParseUtilCSharp.ParseExpression<FieldReferenceExpression>("int.MaxValue");
			Assert.AreEqual("MaxValue", fre.FieldName);
			Assert.AreEqual("System.Int32", ((TypeReferenceExpression)fre.TargetObject).TypeReference.SystemType);
		}
		
		[Test]
		public void StandaloneIntReferenceExpression()
		{
			// this is propably not what really should be returned for a standalone int
			// reference, but it has to stay consistent because NRefactoryResolver depends
			// on this trick.
			FieldReferenceExpression fre = ParseUtilCSharp.ParseExpression<FieldReferenceExpression>("int", true);
			Assert.AreEqual("", fre.FieldName);
			Assert.AreEqual("System.Int32", ((TypeReferenceExpression)fre.TargetObject).TypeReference.SystemType);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBIntReferenceExpression()
		{
			FieldReferenceExpression fre = ParseUtilVBNet.ParseExpression<FieldReferenceExpression>("inTeGer.MaxValue");
			Assert.AreEqual("MaxValue", fre.FieldName);
			Assert.AreEqual("System.Int32", ((TypeReferenceExpression)fre.TargetObject).TypeReference.SystemType);
		}
		
		[Test]
		public void VBStandaloneIntReferenceExpression()
		{
			// this is propably not what really should be returned for a standalone int
			// reference, but it has to stay consistent because NRefactoryResolver depends
			// on this trick.
			FieldReferenceExpression fre = ParseUtilVBNet.ParseExpression<FieldReferenceExpression>("inTeGer", true);
			Assert.AreEqual("", fre.FieldName);
			Assert.AreEqual("System.Int32", ((TypeReferenceExpression)fre.TargetObject).TypeReference.SystemType);
		}
		#endregion
		
	}
}
