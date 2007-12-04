// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.VB;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.NRefactory.Tests.Lexer.VB
{
	[TestFixture]
	public sealed class LexerTests
	{
		ILexer GenerateLexer(StringReader sr)
		{
			return ParserFactory.CreateLexer(SupportedLanguage.VBNet, sr);
		}

		[Test]
		public void TestDot()
		{
			ILexer lexer = GenerateLexer(new StringReader("."));
			Assert.AreEqual(Tokens.Dot, lexer.NextToken().kind);
		}

		[Test]
		public void TestAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("="));
			Assert.AreEqual(Tokens.Assign, lexer.NextToken().kind);
		}

		[Test]
		public void TestComma()
		{
			ILexer lexer = GenerateLexer(new StringReader(","));
			Assert.AreEqual(Tokens.Comma, lexer.NextToken().kind);
		}

		[Test]
		public void TestColon()
		{
			ILexer lexer = GenerateLexer(new StringReader(":"));
			Assert.AreEqual(Tokens.Colon, lexer.NextToken().kind);
		}

		[Test]
		public void TestPlus()
		{
			ILexer lexer = GenerateLexer(new StringReader("+"));
			Assert.AreEqual(Tokens.Plus, lexer.NextToken().kind);
		}

		[Test]
		public void TestMinus()
		{
			ILexer lexer = GenerateLexer(new StringReader("-"));
			Assert.AreEqual(Tokens.Minus, lexer.NextToken().kind);
		}

		[Test]
		public void TestTimes()
		{
			ILexer lexer = GenerateLexer(new StringReader("*"));
			Assert.AreEqual(Tokens.Times, lexer.NextToken().kind);
		}

		[Test]
		public void TestDiv()
		{
			ILexer lexer = GenerateLexer(new StringReader("/"));
			Assert.AreEqual(Tokens.Div, lexer.NextToken().kind);
		}

		[Test]
		public void TestDivInteger()
		{
			ILexer lexer = GenerateLexer(new StringReader("\\"));
			Assert.AreEqual(Tokens.DivInteger, lexer.NextToken().kind);
		}

		[Test]
		public void TestConcatString()
		{
			ILexer lexer = GenerateLexer(new StringReader("&"));
			Assert.AreEqual(Tokens.ConcatString, lexer.NextToken().kind);
		}

		[Test]
		public void TestPower()
		{
			ILexer lexer = GenerateLexer(new StringReader("^"));
			Assert.AreEqual(Tokens.Power, lexer.NextToken().kind);
		}

		[Test]
		public void TestQuestionMark()
		{
			ILexer lexer = GenerateLexer(new StringReader("?"));
			Assert.AreEqual(Tokens.QuestionMark, lexer.NextToken().kind);
		}

		[Test]
		public void TestOpenCurlyBrace()
		{
			ILexer lexer = GenerateLexer(new StringReader("{"));
			Assert.AreEqual(Tokens.OpenCurlyBrace, lexer.NextToken().kind);
		}

		[Test]
		public void TestCloseCurlyBrace()
		{
			ILexer lexer = GenerateLexer(new StringReader("}"));
			Assert.AreEqual(Tokens.CloseCurlyBrace, lexer.NextToken().kind);
		}

		[Test]
		public void TestOpenParenthesis()
		{
			ILexer lexer = GenerateLexer(new StringReader("("));
			Assert.AreEqual(Tokens.OpenParenthesis, lexer.NextToken().kind);
		}

		[Test]
		public void TestCloseParenthesis()
		{
			ILexer lexer = GenerateLexer(new StringReader(")"));
			Assert.AreEqual(Tokens.CloseParenthesis, lexer.NextToken().kind);
		}

		[Test]
		public void TestGreaterThan()
		{
			ILexer lexer = GenerateLexer(new StringReader(">"));
			Assert.AreEqual(Tokens.GreaterThan, lexer.NextToken().kind);
		}

		[Test]
		public void TestLessThan()
		{
			ILexer lexer = GenerateLexer(new StringReader("<"));
			Assert.AreEqual(Tokens.LessThan, lexer.NextToken().kind);
		}

		[Test]
		public void TestNotEqual()
		{
			ILexer lexer = GenerateLexer(new StringReader("<>"));
			Assert.AreEqual(Tokens.NotEqual, lexer.NextToken().kind);
		}

		[Test]
		public void TestGreaterEqual()
		{
			ILexer lexer = GenerateLexer(new StringReader(">="));
			Assert.AreEqual(Tokens.GreaterEqual, lexer.NextToken().kind);
		}

		[Test]
		public void TestLessEqual()
		{
			ILexer lexer = GenerateLexer(new StringReader("<="));
			Assert.AreEqual(Tokens.LessEqual, lexer.NextToken().kind);
		}

		[Test]
		public void TestShiftLeft()
		{
			ILexer lexer = GenerateLexer(new StringReader("<<"));
			Assert.AreEqual(Tokens.ShiftLeft, lexer.NextToken().kind);
		}

		[Test]
		public void TestShiftRight()
		{
			ILexer lexer = GenerateLexer(new StringReader(">>"));
			Assert.AreEqual(Tokens.ShiftRight, lexer.NextToken().kind);
		}

		[Test]
		public void TestPlusAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("+="));
			Assert.AreEqual(Tokens.PlusAssign, lexer.NextToken().kind);
		}

		[Test]
		public void TestPowerAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("^="));
			Assert.AreEqual(Tokens.PowerAssign, lexer.NextToken().kind);
		}

		[Test]
		public void TestMinusAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("-="));
			Assert.AreEqual(Tokens.MinusAssign, lexer.NextToken().kind);
		}

		[Test]
		public void TestTimesAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("*="));
			Assert.AreEqual(Tokens.TimesAssign, lexer.NextToken().kind);
		}

		[Test]
		public void TestDivAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("/="));
			Assert.AreEqual(Tokens.DivAssign, lexer.NextToken().kind);
		}

		[Test]
		public void TestDivIntegerAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("\\="));
			Assert.AreEqual(Tokens.DivIntegerAssign, lexer.NextToken().kind);
		}

		[Test]
		public void TestShiftLeftAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("<<="));
			Assert.AreEqual(Tokens.ShiftLeftAssign, lexer.NextToken().kind);
		}

		[Test]
		public void TestShiftRightAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader(">>="));
			Assert.AreEqual(Tokens.ShiftRightAssign, lexer.NextToken().kind);
		}

		[Test()]
		public void TestAddHandler()
		{
			ILexer lexer = GenerateLexer(new StringReader("AddHandler"));
			Assert.AreEqual(Tokens.AddHandler, lexer.NextToken().kind);
		}
		[Test()]
		public void TestAddressOf()
		{
			ILexer lexer = GenerateLexer(new StringReader("AddressOf"));
			Assert.AreEqual(Tokens.AddressOf, lexer.NextToken().kind);
		}
		[Test()]
		public void TestAlias()
		{
			ILexer lexer = GenerateLexer(new StringReader("Alias"));
			Assert.AreEqual(Tokens.Alias, lexer.NextToken().kind);
		}
		[Test()]
		public void TestAnd()
		{
			ILexer lexer = GenerateLexer(new StringReader("And"));
			Assert.AreEqual(Tokens.And, lexer.NextToken().kind);
		}
		[Test()]
		public void TestAndAlso()
		{
			ILexer lexer = GenerateLexer(new StringReader("AndAlso"));
			Assert.AreEqual(Tokens.AndAlso, lexer.NextToken().kind);
		}
		[Test()]
		public void TestAnsi()
		{
			ILexer lexer = GenerateLexer(new StringReader("Ansi"));
			Assert.AreEqual(Tokens.Ansi, lexer.NextToken().kind);
		}
		[Test()]
		public void TestAs()
		{
			ILexer lexer = GenerateLexer(new StringReader("As"));
			Assert.AreEqual(Tokens.As, lexer.NextToken().kind);
		}
		[Test()]
		public void TestAssembly()
		{
			ILexer lexer = GenerateLexer(new StringReader("Assembly"));
			Assert.AreEqual(Tokens.Assembly, lexer.NextToken().kind);
		}
		[Test()]
		public void TestAuto()
		{
			ILexer lexer = GenerateLexer(new StringReader("Auto"));
			Assert.AreEqual(Tokens.Auto, lexer.NextToken().kind);
		}
		[Test()]
		public void TestBinary()
		{
			ILexer lexer = GenerateLexer(new StringReader("Binary"));
			Assert.AreEqual(Tokens.Binary, lexer.NextToken().kind);
		}
		[Test()]
		public void TestBoolean()
		{
			ILexer lexer = GenerateLexer(new StringReader("Boolean"));
			Assert.AreEqual(Tokens.Boolean, lexer.NextToken().kind);
		}
		[Test()]
		public void TestByRef()
		{
			ILexer lexer = GenerateLexer(new StringReader("ByRef"));
			Assert.AreEqual(Tokens.ByRef, lexer.NextToken().kind);
		}
		[Test()]
		public void TestByte()
		{
			ILexer lexer = GenerateLexer(new StringReader("Byte"));
			Assert.AreEqual(Tokens.Byte, lexer.NextToken().kind);
		}
		[Test()]
		public void TestByVal()
		{
			ILexer lexer = GenerateLexer(new StringReader("ByVal"));
			Assert.AreEqual(Tokens.ByVal, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCall()
		{
			ILexer lexer = GenerateLexer(new StringReader("Call"));
			Assert.AreEqual(Tokens.Call, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCase()
		{
			ILexer lexer = GenerateLexer(new StringReader("Case"));
			Assert.AreEqual(Tokens.Case, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCatch()
		{
			ILexer lexer = GenerateLexer(new StringReader("Catch"));
			Assert.AreEqual(Tokens.Catch, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCBool()
		{
			ILexer lexer = GenerateLexer(new StringReader("CBool"));
			Assert.AreEqual(Tokens.CBool, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCByte()
		{
			ILexer lexer = GenerateLexer(new StringReader("CByte"));
			Assert.AreEqual(Tokens.CByte, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCChar()
		{
			ILexer lexer = GenerateLexer(new StringReader("CChar"));
			Assert.AreEqual(Tokens.CChar, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCDate()
		{
			ILexer lexer = GenerateLexer(new StringReader("CDate"));
			Assert.AreEqual(Tokens.CDate, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCDbl()
		{
			ILexer lexer = GenerateLexer(new StringReader("CDbl"));
			Assert.AreEqual(Tokens.CDbl, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCDec()
		{
			ILexer lexer = GenerateLexer(new StringReader("CDec"));
			Assert.AreEqual(Tokens.CDec, lexer.NextToken().kind);
		}
		[Test()]
		public void TestChar()
		{
			ILexer lexer = GenerateLexer(new StringReader("Char"));
			Assert.AreEqual(Tokens.Char, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCInt()
		{
			ILexer lexer = GenerateLexer(new StringReader("CInt"));
			Assert.AreEqual(Tokens.CInt, lexer.NextToken().kind);
		}
		[Test()]
		public void TestClass()
		{
			ILexer lexer = GenerateLexer(new StringReader("Class"));
			Assert.AreEqual(Tokens.Class, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCLng()
		{
			ILexer lexer = GenerateLexer(new StringReader("CLng"));
			Assert.AreEqual(Tokens.CLng, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCObj()
		{
			ILexer lexer = GenerateLexer(new StringReader("CObj"));
			Assert.AreEqual(Tokens.CObj, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCompare()
		{
			ILexer lexer = GenerateLexer(new StringReader("Compare"));
			Assert.AreEqual(Tokens.Compare, lexer.NextToken().kind);
		}
		[Test()]
		public void TestConst()
		{
			ILexer lexer = GenerateLexer(new StringReader("Const"));
			Assert.AreEqual(Tokens.Const, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCShort()
		{
			ILexer lexer = GenerateLexer(new StringReader("CShort"));
			Assert.AreEqual(Tokens.CShort, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCSng()
		{
			ILexer lexer = GenerateLexer(new StringReader("CSng"));
			Assert.AreEqual(Tokens.CSng, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCStr()
		{
			ILexer lexer = GenerateLexer(new StringReader("CStr"));
			Assert.AreEqual(Tokens.CStr, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCType()
		{
			ILexer lexer = GenerateLexer(new StringReader("CType"));
			Assert.AreEqual(Tokens.CType, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDate()
		{
			ILexer lexer = GenerateLexer(new StringReader("Date"));
			Assert.AreEqual(Tokens.Date, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDecimal()
		{
			ILexer lexer = GenerateLexer(new StringReader("Decimal"));
			Assert.AreEqual(Tokens.Decimal, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDeclare()
		{
			ILexer lexer = GenerateLexer(new StringReader("Declare"));
			Assert.AreEqual(Tokens.Declare, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDefault()
		{
			ILexer lexer = GenerateLexer(new StringReader("Default"));
			Assert.AreEqual(Tokens.Default, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDelegate()
		{
			ILexer lexer = GenerateLexer(new StringReader("Delegate"));
			Assert.AreEqual(Tokens.Delegate, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDim()
		{
			ILexer lexer = GenerateLexer(new StringReader("Dim"));
			Assert.AreEqual(Tokens.Dim, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDirectCast()
		{
			ILexer lexer = GenerateLexer(new StringReader("DirectCast"));
			Assert.AreEqual(Tokens.DirectCast, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDo()
		{
			ILexer lexer = GenerateLexer(new StringReader("Do"));
			Assert.AreEqual(Tokens.Do, lexer.NextToken().kind);
		}
		[Test()]
		public void TestDouble()
		{
			ILexer lexer = GenerateLexer(new StringReader("Double"));
			Assert.AreEqual(Tokens.Double, lexer.NextToken().kind);
		}
		[Test()]
		public void TestEach()
		{
			ILexer lexer = GenerateLexer(new StringReader("Each"));
			Assert.AreEqual(Tokens.Each, lexer.NextToken().kind);
		}
		[Test()]
		public void TestElse()
		{
			ILexer lexer = GenerateLexer(new StringReader("Else"));
			Assert.AreEqual(Tokens.Else, lexer.NextToken().kind);
		}
		[Test()]
		public void TestElseIf()
		{
			ILexer lexer = GenerateLexer(new StringReader("ElseIf"));
			Assert.AreEqual(Tokens.ElseIf, lexer.NextToken().kind);
		}
		[Test()]
		public void TestEnd()
		{
			ILexer lexer = GenerateLexer(new StringReader("End"));
			Assert.AreEqual(Tokens.End, lexer.NextToken().kind);
		}
		[Test()]
		public void TestEndIf()
		{
			ILexer lexer = GenerateLexer(new StringReader("EndIf"));
			Assert.AreEqual(Tokens.EndIf, lexer.NextToken().kind);
		}
		[Test()]
		public void TestEnum()
		{
			ILexer lexer = GenerateLexer(new StringReader("Enum"));
			Assert.AreEqual(Tokens.Enum, lexer.NextToken().kind);
		}
		[Test()]
		public void TestErase()
		{
			ILexer lexer = GenerateLexer(new StringReader("Erase"));
			Assert.AreEqual(Tokens.Erase, lexer.NextToken().kind);
		}
		[Test()]
		public void TestError()
		{
			ILexer lexer = GenerateLexer(new StringReader("Error"));
			Assert.AreEqual(Tokens.Error, lexer.NextToken().kind);
		}
		[Test()]
		public void TestEvent()
		{
			ILexer lexer = GenerateLexer(new StringReader("Event"));
			Assert.AreEqual(Tokens.Event, lexer.NextToken().kind);
		}
		[Test()]
		public void TestExit()
		{
			ILexer lexer = GenerateLexer(new StringReader("Exit"));
			Assert.AreEqual(Tokens.Exit, lexer.NextToken().kind);
		}
		[Test()]
		public void TestExplicit()
		{
			ILexer lexer = GenerateLexer(new StringReader("Explicit"));
			Assert.AreEqual(Tokens.Explicit, lexer.NextToken().kind);
		}
		[Test()]
		public void TestFalse()
		{
			ILexer lexer = GenerateLexer(new StringReader("False"));
			Assert.AreEqual(Tokens.False, lexer.NextToken().kind);
		}
		[Test()]
		public void TestFinally()
		{
			ILexer lexer = GenerateLexer(new StringReader("Finally"));
			Assert.AreEqual(Tokens.Finally, lexer.NextToken().kind);
		}
		[Test()]
		public void TestFor()
		{
			ILexer lexer = GenerateLexer(new StringReader("For"));
			Assert.AreEqual(Tokens.For, lexer.NextToken().kind);
		}
		[Test()]
		public void TestFriend()
		{
			ILexer lexer = GenerateLexer(new StringReader("Friend"));
			Assert.AreEqual(Tokens.Friend, lexer.NextToken().kind);
		}
		[Test()]
		public void TestFunction()
		{
			ILexer lexer = GenerateLexer(new StringReader("Function"));
			Assert.AreEqual(Tokens.Function, lexer.NextToken().kind);
		}
		[Test()]
		public void TestGet()
		{
			ILexer lexer = GenerateLexer(new StringReader("Get"));
			Assert.AreEqual(Tokens.Get, lexer.NextToken().kind);
		}
		[Test()]
		public void TestGetType()
		{
			ILexer lexer = GenerateLexer(new StringReader("GetType"));
			Assert.AreEqual(Tokens.GetType, lexer.NextToken().kind);
		}
		[Test()]
		public void TestGoSub()
		{
			ILexer lexer = GenerateLexer(new StringReader("GoSub"));
			Assert.AreEqual(Tokens.GoSub, lexer.NextToken().kind);
		}
		[Test()]
		public void TestGoTo()
		{
			ILexer lexer = GenerateLexer(new StringReader("GoTo"));
			Assert.AreEqual(Tokens.GoTo, lexer.NextToken().kind);
		}
		[Test()]
		public void TestHandles()
		{
			ILexer lexer = GenerateLexer(new StringReader("Handles"));
			Assert.AreEqual(Tokens.Handles, lexer.NextToken().kind);
		}
		[Test()]
		public void TestIf()
		{
			ILexer lexer = GenerateLexer(new StringReader("If"));
			Assert.AreEqual(Tokens.If, lexer.NextToken().kind);
		}
		[Test()]
		public void TestImplements()
		{
			ILexer lexer = GenerateLexer(new StringReader("Implements"));
			Assert.AreEqual(Tokens.Implements, lexer.NextToken().kind);
		}
		[Test()]
		public void TestImports()
		{
			ILexer lexer = GenerateLexer(new StringReader("Imports"));
			Assert.AreEqual(Tokens.Imports, lexer.NextToken().kind);
		}
		[Test()]
		public void TestIn()
		{
			ILexer lexer = GenerateLexer(new StringReader("In"));
			Assert.AreEqual(Tokens.In, lexer.NextToken().kind);
		}
		[Test()]
		public void TestInherits()
		{
			ILexer lexer = GenerateLexer(new StringReader("Inherits"));
			Assert.AreEqual(Tokens.Inherits, lexer.NextToken().kind);
		}
		[Test()]
		public void TestInteger()
		{
			ILexer lexer = GenerateLexer(new StringReader("Integer"));
			Assert.AreEqual(Tokens.Integer, lexer.NextToken().kind);
		}
		[Test()]
		public void TestInterface()
		{
			ILexer lexer = GenerateLexer(new StringReader("Interface"));
			Assert.AreEqual(Tokens.Interface, lexer.NextToken().kind);
		}
		[Test()]
		public void TestIs()
		{
			ILexer lexer = GenerateLexer(new StringReader("Is"));
			Assert.AreEqual(Tokens.Is, lexer.NextToken().kind);
		}
		[Test()]
		public void TestLet()
		{
			ILexer lexer = GenerateLexer(new StringReader("Let"));
			Assert.AreEqual(Tokens.Let, lexer.NextToken().kind);
		}
		[Test()]
		public void TestLib()
		{
			ILexer lexer = GenerateLexer(new StringReader("Lib"));
			Assert.AreEqual(Tokens.Lib, lexer.NextToken().kind);
		}
		[Test()]
		public void TestLike()
		{
			ILexer lexer = GenerateLexer(new StringReader("Like"));
			Assert.AreEqual(Tokens.Like, lexer.NextToken().kind);
		}
		[Test()]
		public void TestLong()
		{
			ILexer lexer = GenerateLexer(new StringReader("Long"));
			Assert.AreEqual(Tokens.Long, lexer.NextToken().kind);
		}
		[Test()]
		public void TestLoop()
		{
			ILexer lexer = GenerateLexer(new StringReader("Loop"));
			Assert.AreEqual(Tokens.Loop, lexer.NextToken().kind);
		}
		[Test()]
		public void TestMe()
		{
			ILexer lexer = GenerateLexer(new StringReader("Me"));
			Assert.AreEqual(Tokens.Me, lexer.NextToken().kind);
		}
		[Test()]
		public void TestMod()
		{
			ILexer lexer = GenerateLexer(new StringReader("Mod"));
			Assert.AreEqual(Tokens.Mod, lexer.NextToken().kind);
		}
		[Test()]
		public void TestModule()
		{
			ILexer lexer = GenerateLexer(new StringReader("Module"));
			Assert.AreEqual(Tokens.Module, lexer.NextToken().kind);
		}
		[Test()]
		public void TestMustInherit()
		{
			ILexer lexer = GenerateLexer(new StringReader("MustInherit"));
			Assert.AreEqual(Tokens.MustInherit, lexer.NextToken().kind);
		}
		[Test()]
		public void TestMustOverride()
		{
			ILexer lexer = GenerateLexer(new StringReader("MustOverride"));
			Assert.AreEqual(Tokens.MustOverride, lexer.NextToken().kind);
		}
		[Test()]
		public void TestMyBase()
		{
			ILexer lexer = GenerateLexer(new StringReader("MyBase"));
			Assert.AreEqual(Tokens.MyBase, lexer.NextToken().kind);
		}
		[Test()]
		public void TestMyClass()
		{
			ILexer lexer = GenerateLexer(new StringReader("MyClass"));
			Assert.AreEqual(Tokens.MyClass, lexer.NextToken().kind);
		}
		[Test()]
		public void TestNamespace()
		{
			ILexer lexer = GenerateLexer(new StringReader("Namespace"));
			Assert.AreEqual(Tokens.Namespace, lexer.NextToken().kind);
		}
		[Test()]
		public void TestNew()
		{
			ILexer lexer = GenerateLexer(new StringReader("New"));
			Assert.AreEqual(Tokens.New, lexer.NextToken().kind);
		}
		[Test()]
		public void TestNext()
		{
			ILexer lexer = GenerateLexer(new StringReader("Next"));
			Assert.AreEqual(Tokens.Next, lexer.NextToken().kind);
		}
		[Test()]
		public void TestNot()
		{
			ILexer lexer = GenerateLexer(new StringReader("Not"));
			Assert.AreEqual(Tokens.Not, lexer.NextToken().kind);
		}
		[Test()]
		public void TestNothing()
		{
			ILexer lexer = GenerateLexer(new StringReader("Nothing"));
			Assert.AreEqual(Tokens.Nothing, lexer.NextToken().kind);
		}
		[Test()]
		public void TestNotInheritable()
		{
			ILexer lexer = GenerateLexer(new StringReader("NotInheritable"));
			Assert.AreEqual(Tokens.NotInheritable, lexer.NextToken().kind);
		}
		[Test()]
		public void TestNotOverridable()
		{
			ILexer lexer = GenerateLexer(new StringReader("NotOverridable"));
			Assert.AreEqual(Tokens.NotOverridable, lexer.NextToken().kind);
		}
		[Test()]
		public void TestObject()
		{
			ILexer lexer = GenerateLexer(new StringReader("Object"));
			Assert.AreEqual(Tokens.Object, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOff()
		{
			ILexer lexer = GenerateLexer(new StringReader("Off"));
			Assert.AreEqual(Tokens.Off, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOn()
		{
			ILexer lexer = GenerateLexer(new StringReader("On"));
			Assert.AreEqual(Tokens.On, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOption()
		{
			ILexer lexer = GenerateLexer(new StringReader("Option"));
			Assert.AreEqual(Tokens.Option, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOptional()
		{
			ILexer lexer = GenerateLexer(new StringReader("Optional"));
			Assert.AreEqual(Tokens.Optional, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOr()
		{
			ILexer lexer = GenerateLexer(new StringReader("Or"));
			Assert.AreEqual(Tokens.Or, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOrElse()
		{
			ILexer lexer = GenerateLexer(new StringReader("OrElse"));
			Assert.AreEqual(Tokens.OrElse, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOverloads()
		{
			ILexer lexer = GenerateLexer(new StringReader("Overloads"));
			Assert.AreEqual(Tokens.Overloads, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOverridable()
		{
			ILexer lexer = GenerateLexer(new StringReader("Overridable"));
			Assert.AreEqual(Tokens.Overridable, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOverrides()
		{
			ILexer lexer = GenerateLexer(new StringReader("Overrides"));
			Assert.AreEqual(Tokens.Overrides, lexer.NextToken().kind);
		}
		[Test()]
		public void TestParamArray()
		{
			ILexer lexer = GenerateLexer(new StringReader("ParamArray"));
			Assert.AreEqual(Tokens.ParamArray, lexer.NextToken().kind);
		}
		[Test()]
		public void TestPreserve()
		{
			ILexer lexer = GenerateLexer(new StringReader("Preserve"));
			Assert.AreEqual(Tokens.Preserve, lexer.NextToken().kind);
		}
		[Test()]
		public void TestPrivate()
		{
			ILexer lexer = GenerateLexer(new StringReader("Private"));
			Assert.AreEqual(Tokens.Private, lexer.NextToken().kind);
		}
		[Test()]
		public void TestProperty()
		{
			ILexer lexer = GenerateLexer(new StringReader("Property"));
			Assert.AreEqual(Tokens.Property, lexer.NextToken().kind);
		}
		[Test()]
		public void TestProtected()
		{
			ILexer lexer = GenerateLexer(new StringReader("Protected"));
			Assert.AreEqual(Tokens.Protected, lexer.NextToken().kind);
		}
		[Test()]
		public void TestPublic()
		{
			ILexer lexer = GenerateLexer(new StringReader("Public"));
			Assert.AreEqual(Tokens.Public, lexer.NextToken().kind);
		}
		[Test()]
		public void TestRaiseEvent()
		{
			ILexer lexer = GenerateLexer(new StringReader("RaiseEvent"));
			Assert.AreEqual(Tokens.RaiseEvent, lexer.NextToken().kind);
		}
		[Test()]
		public void TestReadOnly()
		{
			ILexer lexer = GenerateLexer(new StringReader("ReadOnly"));
			Assert.AreEqual(Tokens.ReadOnly, lexer.NextToken().kind);
		}
		[Test()]
		public void TestReDim()
		{
			ILexer lexer = GenerateLexer(new StringReader("ReDim"));
			Assert.AreEqual(Tokens.ReDim, lexer.NextToken().kind);
		}
		[Test()]
		public void TestRemoveHandler()
		{
			ILexer lexer = GenerateLexer(new StringReader("RemoveHandler"));
			Assert.AreEqual(Tokens.RemoveHandler, lexer.NextToken().kind);
		}
		[Test()]
		public void TestResume()
		{
			ILexer lexer = GenerateLexer(new StringReader("Resume"));
			Assert.AreEqual(Tokens.Resume, lexer.NextToken().kind);
		}
		[Test()]
		public void TestReturn()
		{
			ILexer lexer = GenerateLexer(new StringReader("Return"));
			Assert.AreEqual(Tokens.Return, lexer.NextToken().kind);
		}
		[Test()]
		public void TestSelect()
		{
			ILexer lexer = GenerateLexer(new StringReader("Select"));
			Assert.AreEqual(Tokens.Select, lexer.NextToken().kind);
		}
		[Test()]
		public void TestSet()
		{
			ILexer lexer = GenerateLexer(new StringReader("Set"));
			Assert.AreEqual(Tokens.Set, lexer.NextToken().kind);
		}
		[Test()]
		public void TestShadows()
		{
			ILexer lexer = GenerateLexer(new StringReader("Shadows"));
			Assert.AreEqual(Tokens.Shadows, lexer.NextToken().kind);
		}
		[Test()]
		public void TestShared()
		{
			ILexer lexer = GenerateLexer(new StringReader("Shared"));
			Assert.AreEqual(Tokens.Shared, lexer.NextToken().kind);
		}
		[Test()]
		public void TestShort()
		{
			ILexer lexer = GenerateLexer(new StringReader("Short"));
			Assert.AreEqual(Tokens.Short, lexer.NextToken().kind);
		}
		[Test()]
		public void TestSingle()
		{
			ILexer lexer = GenerateLexer(new StringReader("Single"));
			Assert.AreEqual(Tokens.Single, lexer.NextToken().kind);
		}
		[Test()]
		public void TestStatic()
		{
			ILexer lexer = GenerateLexer(new StringReader("Static"));
			Assert.AreEqual(Tokens.Static, lexer.NextToken().kind);
		}
		[Test()]
		public void TestStep()
		{
			ILexer lexer = GenerateLexer(new StringReader("Step"));
			Assert.AreEqual(Tokens.Step, lexer.NextToken().kind);
		}
		[Test()]
		public void TestStop()
		{
			ILexer lexer = GenerateLexer(new StringReader("Stop"));
			Assert.AreEqual(Tokens.Stop, lexer.NextToken().kind);
		}
		[Test()]
		public void TestStrict()
		{
			ILexer lexer = GenerateLexer(new StringReader("Strict"));
			Assert.AreEqual(Tokens.Strict, lexer.NextToken().kind);
		}
		[Test()]
		public void TestString()
		{
			ILexer lexer = GenerateLexer(new StringReader("String"));
			Assert.AreEqual(Tokens.String, lexer.NextToken().kind);
		}
		[Test()]
		public void TestStructure()
		{
			ILexer lexer = GenerateLexer(new StringReader("Structure"));
			Assert.AreEqual(Tokens.Structure, lexer.NextToken().kind);
		}
		[Test()]
		public void TestSub()
		{
			ILexer lexer = GenerateLexer(new StringReader("Sub"));
			Assert.AreEqual(Tokens.Sub, lexer.NextToken().kind);
		}
		[Test()]
		public void TestSyncLock()
		{
			ILexer lexer = GenerateLexer(new StringReader("SyncLock"));
			Assert.AreEqual(Tokens.SyncLock, lexer.NextToken().kind);
		}
		[Test()]
		public void TestText()
		{
			ILexer lexer = GenerateLexer(new StringReader("Text"));
			Assert.AreEqual(Tokens.Text, lexer.NextToken().kind);
		}
		[Test()]
		public void TestThen()
		{
			ILexer lexer = GenerateLexer(new StringReader("Then"));
			Assert.AreEqual(Tokens.Then, lexer.NextToken().kind);
		}
		[Test()]
		public void TestThrow()
		{
			ILexer lexer = GenerateLexer(new StringReader("Throw"));
			Assert.AreEqual(Tokens.Throw, lexer.NextToken().kind);
		}
		[Test()]
		public void TestTo()
		{
			ILexer lexer = GenerateLexer(new StringReader("To"));
			Assert.AreEqual(Tokens.To, lexer.NextToken().kind);
		}
		[Test()]
		public void TestTrue()
		{
			ILexer lexer = GenerateLexer(new StringReader("True"));
			Assert.AreEqual(Tokens.True, lexer.NextToken().kind);
		}
		[Test()]
		public void TestTry()
		{
			ILexer lexer = GenerateLexer(new StringReader("Try"));
			Assert.AreEqual(Tokens.Try, lexer.NextToken().kind);
		}
		[Test()]
		public void TestTypeOf()
		{
			ILexer lexer = GenerateLexer(new StringReader("TypeOf"));
			Assert.AreEqual(Tokens.TypeOf, lexer.NextToken().kind);
		}
		[Test()]
		public void TestUnicode()
		{
			ILexer lexer = GenerateLexer(new StringReader("Unicode"));
			Assert.AreEqual(Tokens.Unicode, lexer.NextToken().kind);
		}
		[Test()]
		public void TestUntil()
		{
			ILexer lexer = GenerateLexer(new StringReader("Until"));
			Assert.AreEqual(Tokens.Until, lexer.NextToken().kind);
		}
		[Test()]
		public void TestVariant()
		{
			ILexer lexer = GenerateLexer(new StringReader("Variant"));
			Assert.AreEqual(Tokens.Variant, lexer.NextToken().kind);
		}
		[Test()]
		public void TestWend()
		{
			ILexer lexer = GenerateLexer(new StringReader("Wend"));
			Assert.AreEqual(Tokens.Wend, lexer.NextToken().kind);
		}
		[Test()]
		public void TestWhen()
		{
			ILexer lexer = GenerateLexer(new StringReader("When"));
			Assert.AreEqual(Tokens.When, lexer.NextToken().kind);
		}
		[Test()]
		public void TestWhile()
		{
			ILexer lexer = GenerateLexer(new StringReader("While"));
			Assert.AreEqual(Tokens.While, lexer.NextToken().kind);
		}
		[Test()]
		public void TestWith()
		{
			ILexer lexer = GenerateLexer(new StringReader("With"));
			Assert.AreEqual(Tokens.With, lexer.NextToken().kind);
		}
		[Test()]
		public void TestWithEvents()
		{
			ILexer lexer = GenerateLexer(new StringReader("WithEvents"));
			Assert.AreEqual(Tokens.WithEvents, lexer.NextToken().kind);
		}
		[Test()]
		public void TestWriteOnly()
		{
			ILexer lexer = GenerateLexer(new StringReader("WriteOnly"));
			Assert.AreEqual(Tokens.WriteOnly, lexer.NextToken().kind);
		}
		[Test()]
		public void TestXor()
		{
			ILexer lexer = GenerateLexer(new StringReader("Xor"));
			Assert.AreEqual(Tokens.Xor, lexer.NextToken().kind);
		}
		[Test()]
		public void TestContinue()
		{
			ILexer lexer = GenerateLexer(new StringReader("Continue"));
			Assert.AreEqual(Tokens.Continue, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOperator()
		{
			ILexer lexer = GenerateLexer(new StringReader("Operator"));
			Assert.AreEqual(Tokens.Operator, lexer.NextToken().kind);
		}
		[Test()]
		public void TestUsing()
		{
			ILexer lexer = GenerateLexer(new StringReader("Using"));
			Assert.AreEqual(Tokens.Using, lexer.NextToken().kind);
		}
		[Test()]
		public void TestIsNot()
		{
			ILexer lexer = GenerateLexer(new StringReader("IsNot"));
			Assert.AreEqual(Tokens.IsNot, lexer.NextToken().kind);
		}
		[Test()]
		public void TestSByte()
		{
			ILexer lexer = GenerateLexer(new StringReader("SByte"));
			Assert.AreEqual(Tokens.SByte, lexer.NextToken().kind);
		}
		[Test()]
		public void TestUInteger()
		{
			ILexer lexer = GenerateLexer(new StringReader("UInteger"));
			Assert.AreEqual(Tokens.UInteger, lexer.NextToken().kind);
		}
		[Test()]
		public void TestULong()
		{
			ILexer lexer = GenerateLexer(new StringReader("ULong"));
			Assert.AreEqual(Tokens.ULong, lexer.NextToken().kind);
		}
		[Test()]
		public void TestUShort()
		{
			ILexer lexer = GenerateLexer(new StringReader("UShort"));
			Assert.AreEqual(Tokens.UShort, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCSByte()
		{
			ILexer lexer = GenerateLexer(new StringReader("CSByte"));
			Assert.AreEqual(Tokens.CSByte, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCUShort()
		{
			ILexer lexer = GenerateLexer(new StringReader("CUShort"));
			Assert.AreEqual(Tokens.CUShort, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCUInt()
		{
			ILexer lexer = GenerateLexer(new StringReader("CUInt"));
			Assert.AreEqual(Tokens.CUInt, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCULng()
		{
			ILexer lexer = GenerateLexer(new StringReader("CULng"));
			Assert.AreEqual(Tokens.CULng, lexer.NextToken().kind);
		}
		[Test()]
		public void TestGlobal()
		{
			ILexer lexer = GenerateLexer(new StringReader("Global"));
			Assert.AreEqual(Tokens.Global, lexer.NextToken().kind);
		}
		[Test()]
		public void TestTryCast()
		{
			ILexer lexer = GenerateLexer(new StringReader("TryCast"));
			Assert.AreEqual(Tokens.TryCast, lexer.NextToken().kind);
		}
		[Test()]
		public void TestOf()
		{
			ILexer lexer = GenerateLexer(new StringReader("Of"));
			Assert.AreEqual(Tokens.Of, lexer.NextToken().kind);
		}
		[Test()]
		public void TestNarrowing()
		{
			ILexer lexer = GenerateLexer(new StringReader("Narrowing"));
			Assert.AreEqual(Tokens.Narrowing, lexer.NextToken().kind);
		}
		[Test()]
		public void TestWidening()
		{
			ILexer lexer = GenerateLexer(new StringReader("Widening"));
			Assert.AreEqual(Tokens.Widening, lexer.NextToken().kind);
		}
		[Test()]
		public void TestPartial()
		{
			ILexer lexer = GenerateLexer(new StringReader("Partial"));
			Assert.AreEqual(Tokens.Partial, lexer.NextToken().kind);
		}
		[Test()]
		public void TestCustom()
		{
			ILexer lexer = GenerateLexer(new StringReader("Custom"));
			Assert.AreEqual(Tokens.Custom, lexer.NextToken().kind);
		}
	}
}
