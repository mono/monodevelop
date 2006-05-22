// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1199 $</version>
// </file>

using System;
using System.Text;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.NRefactory.Tests.PrettyPrinter
{
	[TestFixture]
	public class VBToCSharpConverterTest
	{
		public void TestProgram(string input, string expectedOutput)
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.VBNet, new StringReader(input));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			parser.CompilationUnit.AcceptVisitor(new VBNetToCSharpConvertVisitor(), null);
			CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor();
			outputVisitor.Visit(parser.CompilationUnit, null);
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(expectedOutput, outputVisitor.Text);
		}
		
		public void TestMember(string input, string expectedOutput)
		{
			TestMember(input, expectedOutput, null);
		}
		
		public void TestMember(string input, string expectedOutput, string expectedAutomaticImport)
		{
			StringBuilder b = new StringBuilder();
			if (expectedAutomaticImport != null) {
				b.Append("using ");
				b.Append(expectedAutomaticImport);
				b.AppendLine(";");
			}
			b.AppendLine("class tmp1");
			b.AppendLine("{");
			using (StringReader r = new StringReader(expectedOutput)) {
				string line;
				while ((line = r.ReadLine()) != null) {
					b.Append("\t");
					b.AppendLine(line);
				}
			}
			b.AppendLine("}");
			TestProgram("Class tmp1 \n" + input + "\nEnd Class", b.ToString());
		}
		
		public void TestStatement(string input, string expectedOutput)
		{
			StringBuilder b = new StringBuilder();
			b.AppendLine("class tmp1");
			b.AppendLine("{");
			b.AppendLine("\tpublic void tmp2()");
			b.AppendLine("\t{");
			using (StringReader r = new StringReader(expectedOutput)) {
				string line;
				while ((line = r.ReadLine()) != null) {
					b.Append("\t\t");
					b.AppendLine(line);
				}
			}
			b.AppendLine("\t}");
			b.AppendLine("}");
			TestProgram("Class tmp1 \n Sub tmp2() \n" + input + "\n End Sub \n End Class", b.ToString());
		}
		
		[Test]
		public void ReferenceEquality()
		{
			TestStatement("b = a Is Nothing",
			              "b = a == null;");
			TestStatement("b = a IsNot Nothing",
			              "b = a != null;");
			TestStatement("b = Nothing Is a",
			              "b = null == a;");
			TestStatement("b = Nothing IsNot a",
			              "b = null != a;");
			TestStatement("c = a Is b",
			              "c = object.ReferenceEquals(a, b);");
			TestStatement("c = a IsNot b",
			              "c = !object.ReferenceEquals(a, b);");
		}
		
		[Test]
		public void AddHandler()
		{
			TestStatement("AddHandler someEvent, AddressOf tmp2",
			              "someEvent += tmp2;");
			TestStatement("AddHandler someEvent, AddressOf Me.tmp2",
			              "someEvent += this.tmp2;");
		}
		
		[Test]
		public void RemoveHandler()
		{
			TestStatement("RemoveHandler someEvent, AddressOf tmp2",
			              "someEvent -= tmp2;");
			TestStatement("RemoveHandler someEvent, AddressOf Me.tmp2",
			              "someEvent -= this.tmp2;");
		}
		
		[Test]
		public void RaiseEvent()
		{
			TestStatement("RaiseEvent someEvent(Me, EventArgs.Empty)",
			              "if (someEvent != null) {\n\tsomeEvent(this, EventArgs.Empty);\n}");
		}
		
		[Test]
		public void EraseStatement()
		{
			TestStatement("Erase a, b",
			              "a = null;\nb = null;");
		}
		
		[Test]
		public void StaticMethod()
		{
			TestMember("Shared Sub A()\nEnd Sub",
			           "public static void A()\n{\n}");
		}
		
		[Test]
		public void Property()
		{
			TestMember("ReadOnly Property A()\nGet\nReturn Nothing\nEnd Get\nEnd Property",
			           "public object A {\n\tget {\n\t\treturn null;\n\t}\n}");
		}
		
		[Test]
		public void PInvoke()
		{
			TestMember("Declare Function SendMessage Lib \"user32.dll\" (ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As UIntPtr, ByVal lParam As IntPtr) As IntPtr",
			           "[DllImport(\"user32.dll\", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]\n" +
			           "static extern IntPtr SendMessage(IntPtr hWnd, int Msg, UIntPtr wParam, IntPtr lParam);",
			           "System.Runtime.InteropServices");
			
			TestMember("Declare Unicode Function SendMessage Lib \"user32.dll\" Alias \"SendMessageW\" (ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As UIntPtr, ByVal lParam As IntPtr) As IntPtr",
			           "[DllImport(\"user32.dll\", EntryPoint = \"SendMessageW\", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]\n" +
			           "static extern IntPtr SendMessage(IntPtr hWnd, int Msg, UIntPtr wParam, IntPtr lParam);",
			           "System.Runtime.InteropServices");
			
			TestMember("Declare Auto Function SendMessage Lib \"user32.dll\" (ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As UIntPtr, ByVal lParam As IntPtr) As IntPtr",
			           "[DllImport(\"user32.dll\", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]\n" +
			           "static extern IntPtr SendMessage(IntPtr hWnd, int Msg, UIntPtr wParam, IntPtr lParam);",
			           "System.Runtime.InteropServices");
			
			TestMember("<DllImport(\"user32.dll\", CharSet:=CharSet.Auto)> _\n" +
			           "Shared Function MessageBox(ByVal hwnd As IntPtr, ByVal t As String, ByVal caption As String, ByVal t2 As UInt32) As Integer\n" +
			           "End Function",
			           "[DllImport(\"user32.dll\", CharSet = CharSet.Auto)]\n" +
			           "public static extern int MessageBox(IntPtr hwnd, string t, string caption, UInt32 t2);");
		}
		
		[Test]
		public void Constructor()
		{
			TestMember("Sub New()\n\tMyBase.New(1)\nEnd Sub",
			           "public tmp1() : base(1)\n{\n}");
			TestMember("Public Sub New()\n\tMe.New(1)\nEnd Sub",
			           "public tmp1() : this(1)\n{\n}");
		}
		
		[Test]
		public void Destructor()
		{
			TestMember("Protected Overrides Sub Finalize()\n" +
			           "\tTry\n" +
			           "\t\tDead()\n" +
			           "\tFinally\n" +
			           "\t\tMyBase.Finalize()\n" +
			           "\tEnd Try\n" +
			           "End Sub",
			           
			           "~tmp1()\n" +
			           "{\n" +
			           "\tDead();\n" +
			           "}");
		}
		
		[Test]
		public void IIFExpression()
		{
			TestStatement("a = iif(cond, trueEx, falseEx)",
			              "a = (cond ? trueEx : falseEx);");
		}
		
		[Test]
		public void IsNothing()
		{
			TestStatement("a = IsNothing(ex)",
			              "a = (ex == null);");
		}
		
		[Test]
		public void IsNotNothing()
		{
			TestStatement("a = Not IsNothing(ex)",
			              "a = (ex != null);");
		}
		
		[Test]
		public void CompatibilityMethods()
		{
			TestStatement("Beep()",
			              "Interaction.Beep();");
		}
		
		[Test]
		public void EqualsCall()
		{
			TestStatement("Equals(a, b)",
			              "Equals(a, b);");
		}
		
		[Test]
		public void VBConstants()
		{
			TestStatement("a = vbYesNo",
			              "a = Constants.vbYesNo;");
		}
	}
}
