// 
// TemplateEnginePreprocessTemplateTests.cs
//  
// Author:
//       Matt Ward
// 
// Copyright (c) 2011 Matt Ward
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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Tests
{
	[TestFixture]
	public class TemplateEnginePreprocessTemplateTests
	{	
		[Test]
		public void Preprocess ()
		{
			string input = 
				"<#@ template language=\"C#\" #>\r\n" +
				"Test\r\n";
			
			string expectedOutput = OutputSample1;
			string output = Preprocess (input);
			
			Assert.AreEqual (expectedOutput, output);
		}
		
		[Test]
		public void Preprocess_ControlBlockAfterIncludedTemplateWithClassFeatureBlock_ReturnsValidCSharpOutput ()
		{
			string input = InputTemplate_ControlBlockAfterIncludedTemplateWithClassFeatureBlock;
			DummyHost host = CreateDummyHostForControlBlockAfterIncludedTemplateWithClassFeatureBlockTest ();
			
			string expectedOutput = Output_ControlBlockAfterIncludedTemplateWithClassFeatureBlock;
			string output = Preprocess (input, host);
			
			Assert.AreEqual (expectedOutput, output, output);
		}
		
		#region Helpers
		
		string Preprocess (string input)
		{
			DummyHost host = new DummyHost ();
			return Preprocess (input, host);
		}
		
		string Preprocess (string input, DummyHost host)
		{
			string className = "PreprocessedTemplate";
			string classNamespace = "Templating";
			string language = null;
			string[] references = null;
			
			TemplatingEngine engine = new TemplatingEngine ();
			string output = engine.PreprocessTemplate (input, host, className, classNamespace, out language, out references);
			ReportErrors (host.Errors);
			if (output != null) {
				output = output.Replace ("\r\n", "\n");
				return TemplatingEngineHelper.StripHeader (output, "\n");
			}
			return null;
		}
		
		void ReportErrors(CompilerErrorCollection errors)
		{
			foreach (CompilerError error in errors) {
				Console.WriteLine(error.ErrorText);
			}
		}
		
		DummyHost CreateDummyHostForControlBlockAfterIncludedTemplateWithClassFeatureBlockTest()
		{
			DummyHost host = new DummyHost ();
			
			string includeTemplateFileName = @"d:\test\IncludedFile.tt";
			host.Locations.Add (includeTemplateFileName, includeTemplateFileName);
			host.Contents.Add (includeTemplateFileName, IncludedTemplate_ControlBlockAfterIncludedTemplate);
			
			return host;
		}
		
		#endregion

		#region Input templates

		public static string InputTemplate_ControlBlockAfterIncludedTemplateWithClassFeatureBlock =
@"
<#@ template debug=""false"" language=""C#"" #>
<#@ output extension="".cs"" #>
Text Block 1
<#
    this.TemplateMethod();
#>
Text Block 2
<#@ include file=""d:\test\IncludedFile.tt"" #>
Text Block 3
<#
    this.IncludedMethod();
#>
<#+
    void TemplateMethod()
    {
    }
#>
";
		
		public static string IncludedTemplate_ControlBlockAfterIncludedTemplate =
@"
<#@ template debug=""false"" language=""C#"" #>
<#@ output extension="".cs"" #>
Included Text Block 1
<# this.WriteLine(""Included statement block""); #>
Included Text Block 2
<#+
    void IncludedMethod()
    {
#>
Included Method Body Text Block
<#+
    }
#>
";

		#endregion
		
		#region Expected output strings
		
		public static string OutputSample1 = 
@"
namespace Templating {
    
    
    public partial class PreprocessedTemplate : PreprocessedTemplateBase {
        
        public virtual string TransformText() {
            this.GenerationEnvironment = null;
            
            #line 2 """"
            this.Write(""Test\r\n"");
            
            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }
        
        protected virtual void Initialize() {
        }
    }
    
    public class PreprocessedTemplateBase {
        
        private global::System.Text.StringBuilder builder;
        
        private global::System.Collections.Generic.IDictionary<string, object> session;
        
        private global::System.CodeDom.Compiler.CompilerErrorCollection errors;
        
        private string currentIndent = string.Empty;
        
        private global::System.Collections.Generic.Stack<int> indents;
        
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();
        
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session {
            get {
                return this.session;
            }
            set {
                this.session = value;
            }
        }
        
        public global::System.Text.StringBuilder GenerationEnvironment {
            get {
                if ((this.builder == null)) {
                    this.builder = new global::System.Text.StringBuilder();
                }
                return this.builder;
            }
            set {
                this.builder = value;
            }
        }
        
        protected global::System.CodeDom.Compiler.CompilerErrorCollection Errors {
            get {
                if ((this.errors == null)) {
                    this.errors = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errors;
            }
        }
        
        public string CurrentIndent {
            get {
                return this.currentIndent;
            }
        }
        
        private global::System.Collections.Generic.Stack<int> Indents {
            get {
                if ((this.indents == null)) {
                    this.indents = new global::System.Collections.Generic.Stack<int>();
                }
                return this.indents;
            }
        }
        
        public ToStringInstanceHelper ToStringHelper {
            get {
                return this._toStringHelper;
            }
        }
        
        public void Error(string message) {
            this.Errors.Add(new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message));
        }
        
        public void Warning(string message) {
            global::System.CodeDom.Compiler.CompilerError val = new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            this.Errors.Add(val);
        }
        
        public string PopIndent() {
            if ((this.Indents.Count == 0)) {
                return string.Empty;
            }
            int lastPos = (this.currentIndent.Length - this.Indents.Pop());
            string last = this.currentIndent.Substring(lastPos);
            this.currentIndent = this.currentIndent.Substring(0, lastPos);
            return last;
        }
        
        public void PushIndent(string indent) {
            this.Indents.Push(indent.Length);
            this.currentIndent = (this.currentIndent + indent);
        }
        
        public void ClearIndent() {
            this.currentIndent = string.Empty;
            this.Indents.Clear();
        }
        
        public void Write(string textToAppend) {
            this.GenerationEnvironment.Append(textToAppend);
        }
        
        public void Write(string format, params object[] args) {
            this.GenerationEnvironment.AppendFormat(format, args);
        }
        
        public void WriteLine(string textToAppend) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendLine(textToAppend);
        }
        
        public void WriteLine(string format, params object[] args) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendFormat(format, args);
            this.GenerationEnvironment.AppendLine();
        }
        
        public class ToStringInstanceHelper {
            
            private global::System.IFormatProvider formatProvider = global::System.Globalization.CultureInfo.InvariantCulture;
            
            public global::System.IFormatProvider FormatProvider {
                get {
                    return this.formatProvider;
                }
                set {
                    if ((this.formatProvider == null)) {
                        throw new global::System.ArgumentNullException(""formatProvider"");
                    }
                    this.formatProvider = value;
                }
            }
            
            public string ToStringWithCulture(object objectToConvert) {
                if ((objectToConvert == null)) {
                    throw new global::System.ArgumentNullException(""objectToConvert"");
                }
                global::System.Type type = objectToConvert.GetType();
                global::System.Type iConvertibleType = typeof(global::System.IConvertible);
                if (iConvertibleType.IsAssignableFrom(type)) {
                    return ((global::System.IConvertible)(objectToConvert)).ToString(this.formatProvider);
                }
                global::System.Reflection.MethodInfo methInfo = type.GetMethod(""ToString"", new global::System.Type[] {
                            iConvertibleType});
                if ((methInfo != null)) {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                this.formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}
";

		public static string Output_ControlBlockAfterIncludedTemplateWithClassFeatureBlock =
@"
namespace Templating {
    
    
    public partial class PreprocessedTemplate : PreprocessedTemplateBase {
        
        
        #line 14 """"
        
    void TemplateMethod()
    {
    }

        #line default
        #line hidden
        
        
        #line 7 ""d:\test\IncludedFile.tt""
        
    void IncludedMethod()
    {

        #line default
        #line hidden
        
        
        #line 11 ""d:\test\IncludedFile.tt""
        this.Write(""Included Method Body Text Block\n"");

        #line default
        #line hidden
        
        
        #line 12 ""d:\test\IncludedFile.tt""
        
    }

        #line default
        #line hidden
        
        public virtual string TransformText() {
            this.GenerationEnvironment = null;
            
            #line 1 """"
            this.Write(""\n"");
            
            #line default
            #line hidden
            
            #line 4 """"
            this.Write(""Text Block 1\n"");
            
            #line default
            #line hidden
            
            #line 5 """"

    this.TemplateMethod();

            
            #line default
            #line hidden
            
            #line 8 """"
            this.Write(""Text Block 2\n"");
            
            #line default
            #line hidden
            
            #line 1 ""d:\test\IncludedFile.tt""
            this.Write(""\n"");
            
            #line default
            #line hidden
            
            #line 4 ""d:\test\IncludedFile.tt""
            this.Write(""Included Text Block 1\n"");
            
            #line default
            #line hidden
            
            #line 5 ""d:\test\IncludedFile.tt""
 this.WriteLine(""Included statement block""); 
            
            #line default
            #line hidden
            
            #line 6 ""d:\test\IncludedFile.tt""
            this.Write(""Included Text Block 2\n"");
            
            #line default
            #line hidden
            
            #line 10 """"
            this.Write(""Text Block 3\n"");
            
            #line default
            #line hidden
            
            #line 11 """"

    this.IncludedMethod();

            
            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }
        
        protected virtual void Initialize() {
        }
    }
    
    public class PreprocessedTemplateBase {
        
        private global::System.Text.StringBuilder builder;
        
        private global::System.Collections.Generic.IDictionary<string, object> session;
        
        private global::System.CodeDom.Compiler.CompilerErrorCollection errors;
        
        private string currentIndent = string.Empty;
        
        private global::System.Collections.Generic.Stack<int> indents;
        
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();
        
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session {
            get {
                return this.session;
            }
            set {
                this.session = value;
            }
        }
        
        public global::System.Text.StringBuilder GenerationEnvironment {
            get {
                if ((this.builder == null)) {
                    this.builder = new global::System.Text.StringBuilder();
                }
                return this.builder;
            }
            set {
                this.builder = value;
            }
        }
        
        protected global::System.CodeDom.Compiler.CompilerErrorCollection Errors {
            get {
                if ((this.errors == null)) {
                    this.errors = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errors;
            }
        }
        
        public string CurrentIndent {
            get {
                return this.currentIndent;
            }
        }
        
        private global::System.Collections.Generic.Stack<int> Indents {
            get {
                if ((this.indents == null)) {
                    this.indents = new global::System.Collections.Generic.Stack<int>();
                }
                return this.indents;
            }
        }
        
        public ToStringInstanceHelper ToStringHelper {
            get {
                return this._toStringHelper;
            }
        }
        
        public void Error(string message) {
            this.Errors.Add(new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message));
        }
        
        public void Warning(string message) {
            global::System.CodeDom.Compiler.CompilerError val = new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            this.Errors.Add(val);
        }
        
        public string PopIndent() {
            if ((this.Indents.Count == 0)) {
                return string.Empty;
            }
            int lastPos = (this.currentIndent.Length - this.Indents.Pop());
            string last = this.currentIndent.Substring(lastPos);
            this.currentIndent = this.currentIndent.Substring(0, lastPos);
            return last;
        }
        
        public void PushIndent(string indent) {
            this.Indents.Push(indent.Length);
            this.currentIndent = (this.currentIndent + indent);
        }
        
        public void ClearIndent() {
            this.currentIndent = string.Empty;
            this.Indents.Clear();
        }
        
        public void Write(string textToAppend) {
            this.GenerationEnvironment.Append(textToAppend);
        }
        
        public void Write(string format, params object[] args) {
            this.GenerationEnvironment.AppendFormat(format, args);
        }
        
        public void WriteLine(string textToAppend) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendLine(textToAppend);
        }
        
        public void WriteLine(string format, params object[] args) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendFormat(format, args);
            this.GenerationEnvironment.AppendLine();
        }
        
        public class ToStringInstanceHelper {
            
            private global::System.IFormatProvider formatProvider = global::System.Globalization.CultureInfo.InvariantCulture;
            
            public global::System.IFormatProvider FormatProvider {
                get {
                    return this.formatProvider;
                }
                set {
                    if ((this.formatProvider == null)) {
                        throw new global::System.ArgumentNullException(""formatProvider"");
                    }
                    this.formatProvider = value;
                }
            }
            
            public string ToStringWithCulture(object objectToConvert) {
                if ((objectToConvert == null)) {
                    throw new global::System.ArgumentNullException(""objectToConvert"");
                }
                global::System.Type type = objectToConvert.GetType();
                global::System.Type iConvertibleType = typeof(global::System.IConvertible);
                if (iConvertibleType.IsAssignableFrom(type)) {
                    return ((global::System.IConvertible)(objectToConvert)).ToString(this.formatProvider);
                }
                global::System.Reflection.MethodInfo methInfo = type.GetMethod(""ToString"", new global::System.Type[] {
                            iConvertibleType});
                if ((methInfo != null)) {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                this.formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}
";
		#endregion
	}
}
