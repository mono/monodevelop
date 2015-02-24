//
// FileTemplateDirectiveProcessor.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using Microsoft.VisualStudio.TextTemplating;
using System.CodeDom.Compiler;
using Mono.TextTemplating;
using System.CodeDom;

namespace MonoDevelop.TextTemplating
{
	//injects helper members into the generated T4 class so templates are cleaner
	//
	//  bool IsTrue (string key)
	//  - e.g. <# if(IsTrue("Key")){#>
	//
	//  FileTemplateHost Tags {get; }
	//  - e.g. <#=Tags["Key"]#>
	//
	class FileTemplateDirectiveProcessor : DirectiveProcessor, IRecognizeHostSpecific
	{
		CodeDomProvider languageProvider;

		void IRecognizeHostSpecific.SetProcessingRunIsHostSpecific (bool hostSpecific)
		{
			if (!hostSpecific)
				throw new InvalidOperationException ();
		}

		public bool RequiresProcessingRunIsHostSpecific {
			get { return true; }
		}

		public override void StartProcessingRun (CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors)
		{
			this.languageProvider = languageProvider;
			base.StartProcessingRun (languageProvider, templateContents, errors);
		}

		public override string GetClassCodeForProcessingRun ()
		{
			var hostProp = new CodePropertyReferenceExpression (new CodeThisReferenceExpression (), "Host");
			return TemplatingEngine.GenerateIndentedClassCode (
				languageProvider,
				CreateIsTrueMethod (hostProp),
				CreateTagsProperty (hostProp)
			);
		}

		static CodeTypeMember CreateIsTrueMethod (CodePropertyReferenceExpression hostProp)
		{
			var stringTypeRef = new CodeTypeReference (typeof(string));
			var boolTypeRef = new CodeTypeReference (typeof(bool));
			var meth = new CodeMemberMethod { Name = "IsTrue" };
			meth.Parameters.Add (new CodeParameterDeclarationExpression (stringTypeRef, "key"));
			meth.ReturnType = boolTypeRef;
			meth.Statements.Add (
				new CodeMethodReturnStatement (
					new CodeMethodInvokeExpression (
						hostProp, "IsTrue", new CodeArgumentReferenceExpression (meth.Parameters[0].Name)
					)
				)
			);
			return meth;
		}

		static CodeTypeMember CreateTagsProperty (CodePropertyReferenceExpression hostProp)
		{
			var hostTypeRef = new CodeTypeReference (typeof (IFileTagProvider));
			var prop = new CodeMemberProperty { Name = "Tags", Type = hostTypeRef };
			prop.GetStatements.Add (
				new CodeMethodReturnStatement (
					new CodePropertyReferenceExpression (hostProp, "Tags")
				)
			);
			return prop;
		}

		public override void FinishProcessingRun ()
		{
		}

		public override string[] GetImportsForProcessingRun ()
		{
			return null;
		}

		public override string GetPostInitializationCodeForProcessingRun ()
		{
			return null;
		}

		public override string GetPreInitializationCodeForProcessingRun ()
		{
			return null;
		}

		public override string[] GetReferencesForProcessingRun ()
		{
			return null;
		}

		public override bool IsDirectiveSupported (string directiveName)
		{
			return true;
		}

		public override void ProcessDirective (string directiveName, System.Collections.Generic.IDictionary<string, string> arguments)
		{
		}
	}
}
