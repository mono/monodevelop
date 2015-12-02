//
// GenerateIndentedClassCodeTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using NUnit.Framework;
using System.IO;

namespace Mono.TextTemplating.Tests
{
	[TestFixture]
	public class GenerateIndentedClassCodeTests
	{
		[Test]
		public void FieldAndPropertyGenerated ()
		{
			var provider = CodeDomProvider.CreateProvider ("C#");
			var field = CreateBoolField ();
			var property = CreateBoolProperty ();

			string output = TemplatingEngine.GenerateIndentedClassCode (provider, field, property);
			output = FixOutput (output);
			string expectedOutput = FixOutput (MethodAndFieldGeneratedOutput);

			Assert.AreEqual (expectedOutput, output);
		}

		static CodeTypeMember CreateVoidMethod ()
		{
			var meth = new CodeMemberMethod { Name = "MyMethod" };
			meth.ReturnType = new CodeTypeReference (typeof(void));
			return meth;
		}

		static CodeTypeMember CreateBoolField ()
		{
			var type = new CodeTypeReference (typeof(bool));
			return new CodeMemberField { Name = "myField", Type = type };
		}

		static CodeTypeMember CreateBoolProperty ()
		{
			var type = new CodeTypeReference (typeof(bool));
			var prop = new CodeMemberProperty { Name = "MyProperty", Type = type };
			prop.GetStatements.Add (
				new CodeMethodReturnStatement (
					new CodePrimitiveExpression (true)
				)
			);
			return prop;
		}

		/// <summary>
		/// Remove empty lines which are not generated on Mono.
		/// </summary>
		static string FixOutput (string output, string newLine = "\n")
		{
			using (var writer = new StringWriter ()) {
				using (var reader = new StringReader (output)) {

					string line;
					while ((line = reader.ReadLine ()) != null) {
						if (!String.IsNullOrWhiteSpace (line)) {
							writer.Write (line);
							writer.Write (newLine);
						}
					}
				}
				return writer.ToString ();
			}
		}

		public static string MethodAndFieldGeneratedOutput = 
@"        
        private bool myField;
        
        private bool MyProperty {
            get {
                return true;
            }
        }
";
	}
}
