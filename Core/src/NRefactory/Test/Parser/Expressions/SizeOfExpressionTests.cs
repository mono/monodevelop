// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class SizeOfExpressionTests
	{
		#region C#
		[Test]
		public void CSharpSizeOfExpressionTest()
		{
			SizeOfExpression soe = ParseUtilCSharp.ParseExpression<SizeOfExpression>("sizeof(MyType)");
			Assert.AreEqual("MyType", soe.TypeReference.Type);
		}
		#endregion
		
		#region VB.NET
			// No VB.NET representation
		#endregion
	}
}
