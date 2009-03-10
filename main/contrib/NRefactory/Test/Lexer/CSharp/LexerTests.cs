// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 3715 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.CSharp;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.NRefactory.Tests.Lexer.CSharp
{
	[TestFixture]
	public sealed class LexerTests
	{
		ILexer GenerateLexer(StringReader sr)
		{
			return ParserFactory.CreateLexer(SupportedLanguage.CSharp, sr);
		}

		[Test]
		public void TestAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("="));
			Assert.AreEqual(Tokens.Assign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestPlus()
		{
			ILexer lexer = GenerateLexer(new StringReader("+"));
			Assert.AreEqual(Tokens.Plus, lexer.NextToken().Kind);
		}

		[Test]
		public void TestMinus()
		{
			ILexer lexer = GenerateLexer(new StringReader("-"));
			Assert.AreEqual(Tokens.Minus, lexer.NextToken().Kind);
		}

		[Test]
		public void TestTimes()
		{
			ILexer lexer = GenerateLexer(new StringReader("*"));
			Assert.AreEqual(Tokens.Times, lexer.NextToken().Kind);
		}

		[Test]
		public void TestDiv()
		{
			ILexer lexer = GenerateLexer(new StringReader("/"));
			Assert.AreEqual(Tokens.Div, lexer.NextToken().Kind);
		}

		[Test]
		public void TestMod()
		{
			ILexer lexer = GenerateLexer(new StringReader("%"));
			Assert.AreEqual(Tokens.Mod, lexer.NextToken().Kind);
		}

		[Test]
		public void TestColon()
		{
			ILexer lexer = GenerateLexer(new StringReader(":"));
			Assert.AreEqual(Tokens.Colon, lexer.NextToken().Kind);
		}

		[Test]
		public void TestDoubleColon()
		{
			ILexer lexer = GenerateLexer(new StringReader("::"));
			Assert.AreEqual(Tokens.DoubleColon, lexer.NextToken().Kind);
		}

		[Test]
		public void TestSemicolon()
		{
			ILexer lexer = GenerateLexer(new StringReader(";"));
			Assert.AreEqual(Tokens.Semicolon, lexer.NextToken().Kind);
		}

		[Test]
		public void TestQuestion()
		{
			ILexer lexer = GenerateLexer(new StringReader("?"));
			Assert.AreEqual(Tokens.Question, lexer.NextToken().Kind);
		}

		[Test]
		public void TestDoubleQuestion()
		{
			ILexer lexer = GenerateLexer(new StringReader("??"));
			Assert.AreEqual(Tokens.DoubleQuestion, lexer.NextToken().Kind);
		}

		[Test]
		public void TestComma()
		{
			ILexer lexer = GenerateLexer(new StringReader(","));
			Assert.AreEqual(Tokens.Comma, lexer.NextToken().Kind);
		}

		[Test]
		public void TestDot()
		{
			ILexer lexer = GenerateLexer(new StringReader("."));
			Assert.AreEqual(Tokens.Dot, lexer.NextToken().Kind);
		}

		[Test]
		public void TestOpenCurlyBrace()
		{
			ILexer lexer = GenerateLexer(new StringReader("{"));
			Assert.AreEqual(Tokens.OpenCurlyBrace, lexer.NextToken().Kind);
		}

		[Test]
		public void TestCloseCurlyBrace()
		{
			ILexer lexer = GenerateLexer(new StringReader("}"));
			Assert.AreEqual(Tokens.CloseCurlyBrace, lexer.NextToken().Kind);
		}

		[Test]
		public void TestOpenSquareBracket()
		{
			ILexer lexer = GenerateLexer(new StringReader("["));
			Assert.AreEqual(Tokens.OpenSquareBracket, lexer.NextToken().Kind);
		}

		[Test]
		public void TestCloseSquareBracket()
		{
			ILexer lexer = GenerateLexer(new StringReader("]"));
			Assert.AreEqual(Tokens.CloseSquareBracket, lexer.NextToken().Kind);
		}

		[Test]
		public void TestOpenParenthesis()
		{
			ILexer lexer = GenerateLexer(new StringReader("("));
			Assert.AreEqual(Tokens.OpenParenthesis, lexer.NextToken().Kind);
		}

		[Test]
		public void TestCloseParenthesis()
		{
			ILexer lexer = GenerateLexer(new StringReader(")"));
			Assert.AreEqual(Tokens.CloseParenthesis, lexer.NextToken().Kind);
		}

		[Test]
		public void TestGreaterThan()
		{
			ILexer lexer = GenerateLexer(new StringReader(">"));
			Assert.AreEqual(Tokens.GreaterThan, lexer.NextToken().Kind);
		}

		[Test]
		public void TestLessThan()
		{
			ILexer lexer = GenerateLexer(new StringReader("<"));
			Assert.AreEqual(Tokens.LessThan, lexer.NextToken().Kind);
		}

		[Test]
		public void TestNot()
		{
			ILexer lexer = GenerateLexer(new StringReader("!"));
			Assert.AreEqual(Tokens.Not, lexer.NextToken().Kind);
		}

		[Test]
		public void TestLogicalAnd()
		{
			ILexer lexer = GenerateLexer(new StringReader("&&"));
			Assert.AreEqual(Tokens.LogicalAnd, lexer.NextToken().Kind);
		}

		[Test]
		public void TestLogicalOr()
		{
			ILexer lexer = GenerateLexer(new StringReader("||"));
			Assert.AreEqual(Tokens.LogicalOr, lexer.NextToken().Kind);
		}

		[Test]
		public void TestBitwiseComplement()
		{
			ILexer lexer = GenerateLexer(new StringReader("~"));
			Assert.AreEqual(Tokens.BitwiseComplement, lexer.NextToken().Kind);
		}

		[Test]
		public void TestBitwiseAnd()
		{
			ILexer lexer = GenerateLexer(new StringReader("&"));
			Assert.AreEqual(Tokens.BitwiseAnd, lexer.NextToken().Kind);
		}

		[Test]
		public void TestBitwiseOr()
		{
			ILexer lexer = GenerateLexer(new StringReader("|"));
			Assert.AreEqual(Tokens.BitwiseOr, lexer.NextToken().Kind);
		}

		[Test]
		public void TestXor()
		{
			ILexer lexer = GenerateLexer(new StringReader("^"));
			Assert.AreEqual(Tokens.Xor, lexer.NextToken().Kind);
		}

		[Test]
		public void TestIncrement()
		{
			ILexer lexer = GenerateLexer(new StringReader("++"));
			Assert.AreEqual(Tokens.Increment, lexer.NextToken().Kind);
		}

		[Test]
		public void TestDecrement()
		{
			ILexer lexer = GenerateLexer(new StringReader("--"));
			Assert.AreEqual(Tokens.Decrement, lexer.NextToken().Kind);
		}

		[Test]
		public void TestEqual()
		{
			ILexer lexer = GenerateLexer(new StringReader("=="));
			Assert.AreEqual(Tokens.Equal, lexer.NextToken().Kind);
		}

		[Test]
		public void TestNotEqual()
		{
			ILexer lexer = GenerateLexer(new StringReader("!="));
			Assert.AreEqual(Tokens.NotEqual, lexer.NextToken().Kind);
		}

		[Test]
		public void TestGreaterEqual()
		{
			ILexer lexer = GenerateLexer(new StringReader(">="));
			Assert.AreEqual(Tokens.GreaterEqual, lexer.NextToken().Kind);
		}

		[Test]
		public void TestLessEqual()
		{
			ILexer lexer = GenerateLexer(new StringReader("<="));
			Assert.AreEqual(Tokens.LessEqual, lexer.NextToken().Kind);
		}

		[Test]
		public void TestShiftLeft()
		{
			ILexer lexer = GenerateLexer(new StringReader("<<"));
			Assert.AreEqual(Tokens.ShiftLeft, lexer.NextToken().Kind);
		}

		[Test]
		public void TestPlusAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("+="));
			Assert.AreEqual(Tokens.PlusAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestMinusAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("-="));
			Assert.AreEqual(Tokens.MinusAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestTimesAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("*="));
			Assert.AreEqual(Tokens.TimesAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestDivAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("/="));
			Assert.AreEqual(Tokens.DivAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestModAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("%="));
			Assert.AreEqual(Tokens.ModAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestBitwiseAndAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("&="));
			Assert.AreEqual(Tokens.BitwiseAndAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestBitwiseOrAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("|="));
			Assert.AreEqual(Tokens.BitwiseOrAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestXorAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("^="));
			Assert.AreEqual(Tokens.XorAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestShiftLeftAssign()
		{
			ILexer lexer = GenerateLexer(new StringReader("<<="));
			Assert.AreEqual(Tokens.ShiftLeftAssign, lexer.NextToken().Kind);
		}

		[Test]
		public void TestPointer()
		{
			ILexer lexer = GenerateLexer(new StringReader("->"));
			Assert.AreEqual(Tokens.Pointer, lexer.NextToken().Kind);
		}

		[Test]
		public void TestLambdaArrow()
		{
			ILexer lexer = GenerateLexer(new StringReader("=>"));
			Assert.AreEqual(Tokens.LambdaArrow, lexer.NextToken().Kind);
		}

		[Test()]
		public void TestAbstract()
		{
			ILexer lexer = GenerateLexer(new StringReader("abstract"));
			Assert.AreEqual(Tokens.Abstract, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestAs()
		{
			ILexer lexer = GenerateLexer(new StringReader("as"));
			Assert.AreEqual(Tokens.As, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestBase()
		{
			ILexer lexer = GenerateLexer(new StringReader("base"));
			Assert.AreEqual(Tokens.Base, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestBool()
		{
			ILexer lexer = GenerateLexer(new StringReader("bool"));
			Assert.AreEqual(Tokens.Bool, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestBreak()
		{
			ILexer lexer = GenerateLexer(new StringReader("break"));
			Assert.AreEqual(Tokens.Break, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestByte()
		{
			ILexer lexer = GenerateLexer(new StringReader("byte"));
			Assert.AreEqual(Tokens.Byte, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestCase()
		{
			ILexer lexer = GenerateLexer(new StringReader("case"));
			Assert.AreEqual(Tokens.Case, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestCatch()
		{
			ILexer lexer = GenerateLexer(new StringReader("catch"));
			Assert.AreEqual(Tokens.Catch, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestChar()
		{
			ILexer lexer = GenerateLexer(new StringReader("char"));
			Assert.AreEqual(Tokens.Char, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestChecked()
		{
			ILexer lexer = GenerateLexer(new StringReader("checked"));
			Assert.AreEqual(Tokens.Checked, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestClass()
		{
			ILexer lexer = GenerateLexer(new StringReader("class"));
			Assert.AreEqual(Tokens.Class, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestConst()
		{
			ILexer lexer = GenerateLexer(new StringReader("const"));
			Assert.AreEqual(Tokens.Const, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestContinue()
		{
			ILexer lexer = GenerateLexer(new StringReader("continue"));
			Assert.AreEqual(Tokens.Continue, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestDecimal()
		{
			ILexer lexer = GenerateLexer(new StringReader("decimal"));
			Assert.AreEqual(Tokens.Decimal, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestDefault()
		{
			ILexer lexer = GenerateLexer(new StringReader("default"));
			Assert.AreEqual(Tokens.Default, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestDelegate()
		{
			ILexer lexer = GenerateLexer(new StringReader("delegate"));
			Assert.AreEqual(Tokens.Delegate, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestDo()
		{
			ILexer lexer = GenerateLexer(new StringReader("do"));
			Assert.AreEqual(Tokens.Do, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestDouble()
		{
			ILexer lexer = GenerateLexer(new StringReader("double"));
			Assert.AreEqual(Tokens.Double, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestElse()
		{
			ILexer lexer = GenerateLexer(new StringReader("else"));
			Assert.AreEqual(Tokens.Else, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestEnum()
		{
			ILexer lexer = GenerateLexer(new StringReader("enum"));
			Assert.AreEqual(Tokens.Enum, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestEvent()
		{
			ILexer lexer = GenerateLexer(new StringReader("event"));
			Assert.AreEqual(Tokens.Event, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestExplicit()
		{
			ILexer lexer = GenerateLexer(new StringReader("explicit"));
			Assert.AreEqual(Tokens.Explicit, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestExtern()
		{
			ILexer lexer = GenerateLexer(new StringReader("extern"));
			Assert.AreEqual(Tokens.Extern, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestFalse()
		{
			ILexer lexer = GenerateLexer(new StringReader("false"));
			Assert.AreEqual(Tokens.False, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestFinally()
		{
			ILexer lexer = GenerateLexer(new StringReader("finally"));
			Assert.AreEqual(Tokens.Finally, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestFixed()
		{
			ILexer lexer = GenerateLexer(new StringReader("fixed"));
			Assert.AreEqual(Tokens.Fixed, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestFloat()
		{
			ILexer lexer = GenerateLexer(new StringReader("float"));
			Assert.AreEqual(Tokens.Float, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestFor()
		{
			ILexer lexer = GenerateLexer(new StringReader("for"));
			Assert.AreEqual(Tokens.For, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestForeach()
		{
			ILexer lexer = GenerateLexer(new StringReader("foreach"));
			Assert.AreEqual(Tokens.Foreach, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestGoto()
		{
			ILexer lexer = GenerateLexer(new StringReader("goto"));
			Assert.AreEqual(Tokens.Goto, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestIf()
		{
			ILexer lexer = GenerateLexer(new StringReader("if"));
			Assert.AreEqual(Tokens.If, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestImplicit()
		{
			ILexer lexer = GenerateLexer(new StringReader("implicit"));
			Assert.AreEqual(Tokens.Implicit, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestIn()
		{
			ILexer lexer = GenerateLexer(new StringReader("in"));
			Assert.AreEqual(Tokens.In, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestInt()
		{
			ILexer lexer = GenerateLexer(new StringReader("int"));
			Assert.AreEqual(Tokens.Int, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestInterface()
		{
			ILexer lexer = GenerateLexer(new StringReader("interface"));
			Assert.AreEqual(Tokens.Interface, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestInternal()
		{
			ILexer lexer = GenerateLexer(new StringReader("internal"));
			Assert.AreEqual(Tokens.Internal, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestIs()
		{
			ILexer lexer = GenerateLexer(new StringReader("is"));
			Assert.AreEqual(Tokens.Is, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestLock()
		{
			ILexer lexer = GenerateLexer(new StringReader("lock"));
			Assert.AreEqual(Tokens.Lock, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestLong()
		{
			ILexer lexer = GenerateLexer(new StringReader("long"));
			Assert.AreEqual(Tokens.Long, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestNamespace()
		{
			ILexer lexer = GenerateLexer(new StringReader("namespace"));
			Assert.AreEqual(Tokens.Namespace, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestNew()
		{
			ILexer lexer = GenerateLexer(new StringReader("new"));
			Assert.AreEqual(Tokens.New, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestNull()
		{
			ILexer lexer = GenerateLexer(new StringReader("null"));
			Assert.AreEqual(Tokens.Null, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestObject()
		{
			ILexer lexer = GenerateLexer(new StringReader("object"));
			Assert.AreEqual(Tokens.Object, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestOperator()
		{
			ILexer lexer = GenerateLexer(new StringReader("operator"));
			Assert.AreEqual(Tokens.Operator, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestOut()
		{
			ILexer lexer = GenerateLexer(new StringReader("out"));
			Assert.AreEqual(Tokens.Out, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestOverride()
		{
			ILexer lexer = GenerateLexer(new StringReader("override"));
			Assert.AreEqual(Tokens.Override, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestParams()
		{
			ILexer lexer = GenerateLexer(new StringReader("params"));
			Assert.AreEqual(Tokens.Params, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestPrivate()
		{
			ILexer lexer = GenerateLexer(new StringReader("private"));
			Assert.AreEqual(Tokens.Private, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestProtected()
		{
			ILexer lexer = GenerateLexer(new StringReader("protected"));
			Assert.AreEqual(Tokens.Protected, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestPublic()
		{
			ILexer lexer = GenerateLexer(new StringReader("public"));
			Assert.AreEqual(Tokens.Public, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestReadonly()
		{
			ILexer lexer = GenerateLexer(new StringReader("readonly"));
			Assert.AreEqual(Tokens.Readonly, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestRef()
		{
			ILexer lexer = GenerateLexer(new StringReader("ref"));
			Assert.AreEqual(Tokens.Ref, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestReturn()
		{
			ILexer lexer = GenerateLexer(new StringReader("return"));
			Assert.AreEqual(Tokens.Return, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestSbyte()
		{
			ILexer lexer = GenerateLexer(new StringReader("sbyte"));
			Assert.AreEqual(Tokens.Sbyte, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestSealed()
		{
			ILexer lexer = GenerateLexer(new StringReader("sealed"));
			Assert.AreEqual(Tokens.Sealed, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestShort()
		{
			ILexer lexer = GenerateLexer(new StringReader("short"));
			Assert.AreEqual(Tokens.Short, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestSizeof()
		{
			ILexer lexer = GenerateLexer(new StringReader("sizeof"));
			Assert.AreEqual(Tokens.Sizeof, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestStackalloc()
		{
			ILexer lexer = GenerateLexer(new StringReader("stackalloc"));
			Assert.AreEqual(Tokens.Stackalloc, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestStatic()
		{
			ILexer lexer = GenerateLexer(new StringReader("static"));
			Assert.AreEqual(Tokens.Static, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestString()
		{
			ILexer lexer = GenerateLexer(new StringReader("string"));
			Assert.AreEqual(Tokens.String, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestStruct()
		{
			ILexer lexer = GenerateLexer(new StringReader("struct"));
			Assert.AreEqual(Tokens.Struct, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestSwitch()
		{
			ILexer lexer = GenerateLexer(new StringReader("switch"));
			Assert.AreEqual(Tokens.Switch, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestThis()
		{
			ILexer lexer = GenerateLexer(new StringReader("this"));
			Assert.AreEqual(Tokens.This, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestThrow()
		{
			ILexer lexer = GenerateLexer(new StringReader("throw"));
			Assert.AreEqual(Tokens.Throw, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestTrue()
		{
			ILexer lexer = GenerateLexer(new StringReader("true"));
			Assert.AreEqual(Tokens.True, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestTry()
		{
			ILexer lexer = GenerateLexer(new StringReader("try"));
			Assert.AreEqual(Tokens.Try, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestTypeof()
		{
			ILexer lexer = GenerateLexer(new StringReader("typeof"));
			Assert.AreEqual(Tokens.Typeof, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestUint()
		{
			ILexer lexer = GenerateLexer(new StringReader("uint"));
			Assert.AreEqual(Tokens.Uint, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestUlong()
		{
			ILexer lexer = GenerateLexer(new StringReader("ulong"));
			Assert.AreEqual(Tokens.Ulong, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestUnchecked()
		{
			ILexer lexer = GenerateLexer(new StringReader("unchecked"));
			Assert.AreEqual(Tokens.Unchecked, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestUnsafe()
		{
			ILexer lexer = GenerateLexer(new StringReader("unsafe"));
			Assert.AreEqual(Tokens.Unsafe, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestUshort()
		{
			ILexer lexer = GenerateLexer(new StringReader("ushort"));
			Assert.AreEqual(Tokens.Ushort, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestUsing()
		{
			ILexer lexer = GenerateLexer(new StringReader("using"));
			Assert.AreEqual(Tokens.Using, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestVirtual()
		{
			ILexer lexer = GenerateLexer(new StringReader("virtual"));
			Assert.AreEqual(Tokens.Virtual, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestVoid()
		{
			ILexer lexer = GenerateLexer(new StringReader("void"));
			Assert.AreEqual(Tokens.Void, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestVolatile()
		{
			ILexer lexer = GenerateLexer(new StringReader("volatile"));
			Assert.AreEqual(Tokens.Volatile, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestWhile()
		{
			ILexer lexer = GenerateLexer(new StringReader("while"));
			Assert.AreEqual(Tokens.While, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestPartial()
		{
			ILexer lexer = GenerateLexer(new StringReader("partial"));
			Assert.AreEqual(Tokens.Partial, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestWhere()
		{
			ILexer lexer = GenerateLexer(new StringReader("where"));
			Assert.AreEqual(Tokens.Where, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestGet()
		{
			ILexer lexer = GenerateLexer(new StringReader("get"));
			Assert.AreEqual(Tokens.Get, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestSet()
		{
			ILexer lexer = GenerateLexer(new StringReader("set"));
			Assert.AreEqual(Tokens.Set, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestAdd()
		{
			ILexer lexer = GenerateLexer(new StringReader("add"));
			Assert.AreEqual(Tokens.Add, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestRemove()
		{
			ILexer lexer = GenerateLexer(new StringReader("remove"));
			Assert.AreEqual(Tokens.Remove, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestYield()
		{
			ILexer lexer = GenerateLexer(new StringReader("yield"));
			Assert.AreEqual(Tokens.Yield, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestSelect()
		{
			ILexer lexer = GenerateLexer(new StringReader("select"));
			Assert.AreEqual(Tokens.Select, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestGroup()
		{
			ILexer lexer = GenerateLexer(new StringReader("group"));
			Assert.AreEqual(Tokens.Group, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestBy()
		{
			ILexer lexer = GenerateLexer(new StringReader("by"));
			Assert.AreEqual(Tokens.By, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestInto()
		{
			ILexer lexer = GenerateLexer(new StringReader("into"));
			Assert.AreEqual(Tokens.Into, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestFrom()
		{
			ILexer lexer = GenerateLexer(new StringReader("from"));
			Assert.AreEqual(Tokens.From, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestAscending()
		{
			ILexer lexer = GenerateLexer(new StringReader("ascending"));
			Assert.AreEqual(Tokens.Ascending, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestDescending()
		{
			ILexer lexer = GenerateLexer(new StringReader("descending"));
			Assert.AreEqual(Tokens.Descending, lexer.NextToken().Kind);
		}
		[Test()]
		public void TestOrderby()
		{
			ILexer lexer = GenerateLexer(new StringReader("orderby"));
			Assert.AreEqual(Tokens.Orderby, lexer.NextToken().Kind);
		}
	}
}
