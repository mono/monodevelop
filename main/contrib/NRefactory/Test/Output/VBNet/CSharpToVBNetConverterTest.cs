// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 3841 $</version>
// </file>

using System;
using System.Text;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.NRefactory.Tests.PrettyPrinter
{
	[TestFixture]
	public class CSharpToVBNetConverterTest
	{
		public void TestProgram(string input, string expectedOutput)
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(input));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			parser.CompilationUnit.AcceptVisitor(new CSharpConstructsConvertVisitor(), null);
			parser.CompilationUnit.AcceptVisitor(new ToVBNetConvertVisitor(), null);
			VBNetOutputVisitor outputVisitor = new VBNetOutputVisitor();
			outputVisitor.Options.IndentationChar = ' ';
			outputVisitor.Options.IndentSize = 2;
			outputVisitor.Options.OutputByValModifier = true;
			outputVisitor.VisitCompilationUnit(parser.CompilationUnit, null);
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(expectedOutput, outputVisitor.Text);
		}
		
		public void TestMember(string input, string expectedOutput)
		{
			StringBuilder b = new StringBuilder();
			b.AppendLine("Class tmp1");
			using (StringReader r = new StringReader(expectedOutput)) {
				string line;
				while ((line = r.ReadLine()) != null) {
					b.Append("  ");
					b.AppendLine(line);
				}
			}
			b.AppendLine("End Class");
			TestProgram("class tmp1 { \n" + input + "\n}", b.ToString());
		}
		
		public void TestStatement(string input, string expectedOutput)
		{
			StringBuilder b = new StringBuilder();
			b.AppendLine("Class tmp1");
			b.AppendLine("  Private Sub tmp2()");
			using (StringReader r = new StringReader(expectedOutput)) {
				string line;
				while ((line = r.ReadLine()) != null) {
					b.Append("    ");
					b.AppendLine(line);
				}
			}
			b.AppendLine("  End Sub");
			b.AppendLine("End Class");
			TestProgram("class tmp1 { void tmp2() {\n" + input + "\n}}", b.ToString());
		}
		
		[Test]
		public void MoveImportsStatement()
		{
			TestProgram("namespace test { using SomeNamespace; }",
			            "Imports SomeNamespace" + Environment.NewLine +
			            "Namespace test" + Environment.NewLine +
			            "End Namespace" + Environment.NewLine);
		}
		
		[Test]
		public void ClassImplementsInterface()
		{
			TestProgram("class test : IComparable { }",
			            "Class test" + Environment.NewLine +
			            "  Implements IComparable" + Environment.NewLine +
			            "End Class" + Environment.NewLine);
		}
		
		[Test]
		public void ClassImplementsInterface2()
		{
			TestProgram("class test : System.IComparable { }",
			            "Class test" + Environment.NewLine +
			            "  Implements System.IComparable" + Environment.NewLine +
			            "End Class" + Environment.NewLine);
		}
		
		[Test]
		public void ClassInheritsClass()
		{
			TestProgram("class test : InvalidDataException { }",
			            "Class test" + Environment.NewLine +
			            "  Inherits InvalidDataException" + Environment.NewLine +
			            "End Class"+ Environment.NewLine);
		}
		
		[Test]
		public void ClassInheritsClass2()
		{
			TestProgram("class test : System.IO.InvalidDataException { }",
			            "Class test" + Environment.NewLine +
			            "  Inherits System.IO.InvalidDataException" + Environment.NewLine +
			            "End Class" + Environment.NewLine);
		}
		
		[Test]
		public void ForWithUnknownConditionAndSingleStatement()
		{
			TestStatement("for (i = 0; unknownCondition; i++) b[i] = s[i];",
			              "i = 0\n" +
			              "While unknownCondition\n" +
			              "  b(i) = s(i)\n" +
			              "  i += 1\n" +
			              "End While");
		}
		
		[Test]
		public void ForWithUnknownConditionAndBlock()
		{
			TestStatement("for (i = 0; unknownCondition; i++) { b[i] = s[i]; }",
			              "i = 0\n" +
			              "While unknownCondition\n" +
			              "  b(i) = s(i)\n" +
			              "  i += 1\n" +
			              "End While");
		}
		
		[Test]
		public void ForWithSingleStatement()
		{
			TestStatement("for (i = 0; i < end; i++) b[i] = s[i];",
			              "For i = 0 To [end] - 1\n" +
			              "  b(i) = s(i)\n" +
			              "Next");
		}
		[Test]
		public void ForWithBlock()
		{
			TestStatement("for (i = 0; i < end; i++) { b[i] = s[i]; }",
			              "For i = 0 To [end] - 1\n" +
			              "  b(i) = s(i)\n" +
			              "Next");
		}
		
		[Test]
		public void RaiseEvent()
		{
			TestStatement("if (MyEvent != null) MyEvent(this, EventArgs.Empty);",
			              "RaiseEvent MyEvent(Me, EventArgs.Empty)");
			TestStatement("if ((MyEvent != null)) MyEvent(this, EventArgs.Empty);",
			              "RaiseEvent MyEvent(Me, EventArgs.Empty)");
			TestStatement("if (null != MyEvent) { MyEvent(this, EventArgs.Empty); }",
			              "RaiseEvent MyEvent(Me, EventArgs.Empty)");
			TestStatement("if (this.MyEvent != null) MyEvent(this, EventArgs.Empty);",
			              "RaiseEvent MyEvent(Me, EventArgs.Empty)");
			TestStatement("if (MyEvent != null) this.MyEvent(this, EventArgs.Empty);",
			              "RaiseEvent MyEvent(Me, EventArgs.Empty)");
			TestStatement("if ((this.MyEvent != null)) { this.MyEvent(this, EventArgs.Empty); }",
			              "RaiseEvent MyEvent(Me, EventArgs.Empty)");
		}
		
		[Test]
		public void IfStatementSimilarToRaiseEvent()
		{
			TestStatement("if (FullImage != null) DrawImage();",
			              "If FullImage IsNot Nothing Then\n" +
			              "  DrawImage()\n" +
			              "End If");
			// regression test:
			TestStatement("if (FullImage != null) e.DrawImage();",
			              "If FullImage IsNot Nothing Then\n" +
			              "  e.DrawImage()\n" +
			              "End If");
			// with braces:
			TestStatement("if (FullImage != null) { DrawImage(); }",
			              "If FullImage IsNot Nothing Then\n" +
			              "  DrawImage()\n" +
			              "End If");
			TestStatement("if (FullImage != null) { e.DrawImage(); }",
			              "If FullImage IsNot Nothing Then\n" +
			              "  e.DrawImage()\n" +
			              "End If");
			// another bug related to the IfStatement code:
			TestStatement("if (Tiles != null) foreach (Tile t in Tiles) this.TileTray.Controls.Remove(t);",
			              "If Tiles IsNot Nothing Then\n" +
			              "  For Each t As Tile In Tiles\n" +
			              "    Me.TileTray.Controls.Remove(t)\n" +
			              "  Next\n" +
			              "End If");
		}
		
		[Test]
		public void ElseIfStatement()
		{
			TestStatement("if (a) {} else if (b) {} else {}",
			              "If a Then\n" +
			              "ElseIf b Then\n" +
			              "Else\n" +
			              "End If");
		}
		
		[Test]
		public void AnonymousMethod()
		{
			TestMember("void A() { Converter<int, int> i = delegate(int argument) { return argument * 2; }; }",
			           "Private Sub A()\n" +
			           "  Dim i As Converter(Of Integer, Integer) = Function(ByVal argument As Integer) argument * 2\n" +
			           "End Sub");
		}
		
		[Test]
		public void StaticMethod()
		{
			TestMember("static void A() {}",
			           "Private Shared Sub A()\nEnd Sub");
		}
		
		[Test]
		public void PInvoke()
		{
			TestMember("[DllImport(\"user32.dll\", CharSet = CharSet.Auto)]" + Environment.NewLine +
			           "public static extern int MessageBox(IntPtr hwnd, string t, string caption, UInt32 t2);",
			           "<DllImport(\"user32.dll\", CharSet := CharSet.Auto)> _" + Environment.NewLine +
			           "Public Shared Function MessageBox(ByVal hwnd As IntPtr, ByVal t As String, ByVal caption As String, ByVal t2 As UInt32) As Integer\n" +
			           "End Function");
			
			TestMember("[DllImport(\"user32.dll\", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]\n" +
			           "public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, UIntPtr wParam, IntPtr lParam);",
			           "Public Declare Ansi Function SendMessage Lib \"user32.dll\" (ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As UIntPtr, ByVal lParam As IntPtr) As IntPtr");
			
			TestMember("[DllImport(\"user32.dll\", SetLastError = true, ExactSpelling = true, EntryPoint = \"SendMessageW\")]\n" +
			           "public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, UIntPtr wParam, IntPtr lParam);",
			           "Public Declare Auto Function SendMessage Lib \"user32.dll\" Alias \"SendMessageW\" (ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As UIntPtr, ByVal lParam As IntPtr) As IntPtr");
		}
		
		[Test]
		public void PInvokeSub()
		{
			TestMember("[DllImport(\"kernel32\", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]\n" +
			           "private static extern void Sleep(long dwMilliseconds);",
			           "Private Declare Ansi Sub Sleep Lib \"kernel32\" (ByVal dwMilliseconds As Long)");
		}
		
		[Test]
		public void Constructor()
		{
			TestMember("public tmp1() : base(1) { }",
			           "Public Sub New()\n  MyBase.New(1)\nEnd Sub");
			TestMember("public tmp1() : this(1) { }",
			           "Public Sub New()\n  Me.New(1)\nEnd Sub");
		}
		
		[Test]
		public void StaticConstructor()
		{
			TestMember("static tmp1() { }",
			           "Shared Sub New()\nEnd Sub");
		}
		
		[Test]
		public void Destructor()
		{
			TestMember("~tmp1() { Dead(); }",
			           "Protected Overrides Sub Finalize()\n" +
			           "  Try\n" +
			           "    Dead()\n" +
			           "  Finally\n" +
			           "    MyBase.Finalize()\n" +
			           "  End Try\n" +
			           "End Sub");
		}
		
		[Test]
		public void Indexer()
		{
			TestMember("public CategoryInfo this[int index] { get { return List[index] as CategoryInfo; } }",
			           "Public Default ReadOnly Property Item(ByVal index As Integer) As CategoryInfo\n" +
			           "  Get\n" +
			           "    Return TryCast(List(index), CategoryInfo)\n" +
			           "  End Get\n" +
			           "End Property");
		}
		
		[Test]
		public void RenameConflictingNames()
		{
			TestMember("int count;" +
			           "public int Count { get { return count; } }" +
			           "void Test1(int count) { count = 3; }" +
			           "void Test2() { int count; count = 3; }" +
			           "void Test3() { foreach (int count in someList) { count = 3; } }",
			           
			           "Private m_count As Integer\n" +
			           "Public ReadOnly Property Count() As Integer\n" +
			           "  Get\n" +
			           "    Return m_count\n" +
			           "  End Get\n" +
			           "End Property\n" +
			           "Private Sub Test1(ByVal count As Integer)\n" +
			           "  count = 3\n" +
			           "End Sub\n" +
			           "Private Sub Test2()\n" +
			           "  Dim count As Integer\n" +
			           "  count = 3\n" +
			           "End Sub\n" +
			           "Private Sub Test3()\n" +
			           "  For Each count As Integer In someList\n" +
			           "    count = 3\n" +
			           "  Next\n" +
			           "End Sub");
		}
		
		[Test]
		public void NullCoalescing()
		{
			TestStatement("c = a ?? b;",
			              "c = If(a, b)");
		}
		
		[Test]
		public void Ternary()
		{
			TestStatement("d = a ? b : c;",
			              "d = If(a, b, c)");
		}
		
		[Test]
		public void ConvertedLoop()
		{
			TestStatement("while (cond) example();",
			              "While cond\n" +
			              "  example()\n" +
			              "End While");
		}
		
		[Test]
		public void UIntVariableDeclaration()
		{
			TestStatement("uint s = 0;", "Dim s As UInteger = 0");
		}
		
		[Test]
		public void BreakInWhileLoop()
		{
			TestStatement("while (test != null) { break; }",
			              "While test IsNot Nothing\n" +
			              "  Exit While\n" +
			              "End While");
		}
		
		[Test]
		public void BreakInDoLoop()
		{
			TestStatement("do { break; } while (test != null);",
			              "Do\n" +
			              "  Exit Do\n" +
			              "Loop While test IsNot Nothing");
		}
		
		[Test]
		public void StructFieldVisibility()
		{
			TestMember("public struct A { int field; }",
			           "Public Structure A\n" +
			           "  Private field As Integer\n" +
			           "End Structure");
		}
		
		[Test]
		public void InnerClassVisibility()
		{
			TestMember("class Inner\n{\n}",
			           "Private Class Inner\n" +
			           "End Class");
		}
		
		[Test]
		public void InnerDelegateVisibility()
		{
			TestMember("delegate void Test();",
			           "Private Delegate Sub Test()");
		}
		
		[Test]
		public void InterfaceVisibility()
		{
			TestMember("public interface ITest {\n" +
			           "  void Test();\n" +
			           "  string Name { get; set; }\n" +
			           "}",
			           "Public Interface ITest\n" +
			           "  Sub Test()\n" +
			           "  Property Name() As String\n" +
			           "End Interface");
		}
		
		[Test]
		public void ImportAliasPrimitiveType()
		{
			TestProgram("using T = System.Boolean;", "Imports T = System.Boolean"+ Environment.NewLine);
		}
		
		[Test]
		public void DefaultExpression()
		{
			TestStatement("T oldValue = default(T);", "Dim oldValue As T = Nothing");
		}
		
		[Test]
		public void StaticClass()
		{
			TestProgram("public static class Test {}", @"Public NotInheritable Class Test" + Environment.NewLine +
"  Private Sub New()" + Environment.NewLine +
"  End Sub" + Environment.NewLine +
"End Class" + Environment.NewLine);
		}
		
		[Test]
		public void GlobalTypeReference()
		{
			TestStatement("global::System.String a;", "Dim a As Global.System.String");
		}
		
		[Test]
		public void TestMethodCallOnCastExpression()
		{
			TestStatement("((IDisposable)o).Dispose();", "DirectCast(o, IDisposable).Dispose()");
		}
		
		[Test]
		public void CaseConflictingMethod()
		{
			TestMember("void T(int v) { int V = v; M(V, v); }",
			           "Private Sub T(ByVal v__1 As Integer)\n" +
			           "  Dim V__2 As Integer = v__1\n" +
			           "  M(V__2, v__1)\n" +
			           "End Sub");
		}
		
		[Test]
		public void ArrayCreationUpperBound()
		{
			TestStatement("string[] i = new string[2];",
			              "Dim i As String() = New String(1) {}");
			TestStatement("string[] i = new string[2] { \"0\", \"1\" };",
			              "Dim i As String() = New String(1) {\"0\", \"1\"}");
			TestStatement("string[,] i = new string[6, 6];",
			              "Dim i As String(,) = New String(5, 5) {}");
		}
		
		[Test]
		public void VariableNamedRem()
		{
			TestStatement("int rem;", "Dim [rem] As Integer");
			TestStatement("int Rem;", "Dim [Rem] As Integer");
			TestStatement("int a = rem;", "Dim a As Integer = [rem]");
		}
		
		[Test]
		public void ArrayCast()
		{
			TestStatement("string[] i = (string[])obj;",
			              "Dim i As String() = DirectCast(obj, String())");
			
			// ensure the converter does not use CInt:
			TestStatement("int[] i = (int[])obj;",
			              "Dim i As Integer() = DirectCast(obj, Integer())");
		}
		
		
		[Test]
		public void PrimitiveCast()
		{
			TestStatement("int a = (int)number;", "Dim a As Integer = CInt(number)");
			TestStatement("byte i = (byte)obj;", "Dim i As Byte = CByte(obj)");
			TestStatement("short i = (short)obj;", "Dim i As Short = CShort(obj)");
			TestStatement("long i = (long)obj;", "Dim i As Long = CLng(obj)");
		}
		
		[Test]
		public void PrimitiveUnsignedCast()
		{
			TestStatement("uint i = (uint)obj;", "Dim i As UInteger = CUInt(obj)");
			TestStatement("sbyte i = (sbyte)obj;", "Dim i As SByte = CSByte(obj)");
			TestStatement("ushort i = (ushort)obj;", "Dim i As UShort = CUShort(obj)");
			TestStatement("ulong i = (ulong)obj;", "Dim i As ULong = CULng(obj)");
		}
		
		[Test]
		public void InlineAssignment()
		{
			TestProgram(@"public class Convert { void Run(string s) { char c; if ((c = s[0]) == '\n') { c = ' '; } } }",
			            @"Public Class Convert" + Environment.NewLine +
"  Private Sub Run(ByVal s As String)" + Environment.NewLine +
"    Dim c As Char" + Environment.NewLine +
"    If (InlineAssignHelper(c, s(0))) = ControlChars.Lf Then" + Environment.NewLine +
"      c = \" \"C" + Environment.NewLine +
"    End If" + Environment.NewLine +
"  End Sub" + Environment.NewLine +
"  Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T" + Environment.NewLine +
"    target = value" + Environment.NewLine +
"    Return value" + Environment.NewLine +
"  End Function" + Environment.NewLine +
"End Class" + Environment.NewLine);
		}
		
		[Test]
		public void StandaloneBlockStatement()
		{
			TestStatement("{ int a; } { string a; }",
			              "If True Then\n" +
			              "  Dim a As Integer\n" +
			              "End If\n" +
			              "If True Then\n" +
			              "  Dim a As String\n" +
			              "End If");
		}
		
		[Test]
		public void CSharpLinefeedToVBString()
		{
			TestStatement(@"string Test = ""My Test\n"";",
			              @"Dim Test As String = ""My Test"" & vbLf");
		}
		
		[Test]
		public void CSharpTabToVBString()
		{
			TestStatement(@"string Test = ""\t\a"";",
			              @"Dim Test As String = vbTab & ChrW(7)");
		}
	}
}
