// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1609 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
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
