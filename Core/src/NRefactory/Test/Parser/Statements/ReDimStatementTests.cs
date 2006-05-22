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
	public class ReDimStatementTests
	{
		#region C#
		// No C# representation
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetReDimStatementTest()
		{
			ReDimStatement reDimStatement = ParseUtilVBNet.ParseStatement<ReDimStatement>("ReDim Preserve MyArray(15)");
		}
		
		[Test]
		public void VBNetReDimStatementTest2()
		{
			ReDimStatement reDimStatement = ParseUtilVBNet.ParseStatement<ReDimStatement>("ReDim calCheckData(channelNum, lambdaNum).ShiftFromLastFullCalPixels(CalCheckPeak.HighWavelength)");
		}
		
		[Test]
		public void VBNetBigReDimStatementTest()
		{
			string program = @"
Class X
	Sub x
		ReDim sU(m - 1, n - 1)
		ReDim sW(n - 1)
		ReDim sV(n - 1, n - 1)
		ReDim rv1(n - 1)
		ReDim sMt(iNrCols - 1, 0)
		ReDim Preserve sMt(iNrCols - 1, iRowNr)
		ReDim sM(iRowNr - 1, iNrCols - 1)
		If (IsNothing(ColLengths)) Then ReDim ColLengths(0)
		If (ColLengths.Length = (SubItem + 1)) Then ReDim Preserve ColLengths(SubItem + 1)
		ReDim sTransform(2, iTransformType - 1)
		ReDim Preserve _Items(_Count)
		ReDim Preserve _Items(nCapacity)
		ReDim Preserve _Items(_Count)
		ReDim Preserve _Items(nCapacity)
		ReDim Preserve _Items(_Count)
		ReDim Preserve _Items(nCapacity)
		ReDim sU(m - 1, n - 1)
		ReDim sW(n - 1)
		ReDim sV(n - 1, n - 1)
		ReDim rv1(n - 1)
		ReDim sMt(iNrCols - 1, 0)
		ReDim Preserve sMt(iNrCols - 1, iRowNr)
		ReDim sM(iRowNr - 1, iNrCols - 1)
		If (IsNothing(ColLengths)) Then ReDim ColLengths(0)
		If (ColLengths.Length = (SubItem + 1)) Then ReDim Preserve ColLengths(SubItem + 1)
		ReDim sTransform(2, iTransformType - 1)
		ReDim Preserve _Items(_Count)
		ReDim Preserve _Items(nCapacity)
		ReDim Preserve _Items(_Count)
		ReDim Preserve _Items(nCapacity)
		ReDim Preserve _Items(_Count)
		ReDim Preserve _Items(nCapacity)
		ReDim Preserve Samples(Samples.GetUpperBound(0) + 1)
		ReDim Samples(0)
		ReDim BaseCssContent(BaseCssContentRows - 1)
		ReDim mabtRxBuf(Bytes2Read - 1)
		ReDim mabtRxBuf(Bytes2Read - 1)
		ReDim Preserve primarykey(primarykey.Length)
		ReDim Preserve primarykey(primarykey.Length)
		ReDim Preserve IntArray(10, 10, 20)
		ReDim Preserve IntArray(10, 10, 15)
		ReDim X(10, 10)
		ReDim Preserve IntArray(10, 10, 20)
		ReDim Preserve IntArray(10, 10, 15)
		ReDim X(10, 10)
	End Sub
End Class";
			TypeDeclaration typeDeclaration = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
		}

		#endregion
	}
}
