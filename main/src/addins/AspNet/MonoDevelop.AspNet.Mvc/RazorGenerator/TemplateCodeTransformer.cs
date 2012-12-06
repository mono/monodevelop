//
// Based on Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//
// Changes:
//     Author: Michael Hutchinson <mhutch@xamarin.com>
//     Copyright (c) 2012 Xamarin Inc (http://xamarin.com)

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RazorGenerator.Core
{
	class TemplateCodeTransformer : AggregateCodeTransformer
	{
		private const string GenerationEnvironmentPropertyName = "GenerationEnvironment";
		private static readonly IEnumerable<string> _defaultImports = new[] {
			"System",
			"System.Collections.Generic",
			"System.Linq",
			"System.Text"
		};

		private readonly RazorCodeTransformerBase[] _codeTransforms = new RazorCodeTransformerBase[] {
			new SetImports(_defaultImports, replaceExisting: true),
			new AddGeneratedTemplateClassAttribute(),
			new ReplaceBaseType(),
			new FixMonoPragmas (),
		};

		protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
		{
			get { return _codeTransforms; }
		}

		public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
			generatedClass.IsPartial = true;
			// The generated class has a constructor in there by default.
			generatedClass.Members.Remove(generatedClass.Members.OfType<CodeConstructor>().SingleOrDefault());
		}
	}

	class AddGeneratedTemplateClassAttribute : RazorCodeTransformerBase
	{
		public override void ProcessGeneratedCode (CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			string tool = "RazorTemplatePreprocessor";
			Version version = GetType().Assembly.GetName().Version;
			generatedClass.CustomAttributes.Add(
				new CodeAttributeDeclaration(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName,
					new CodeAttributeArgument(new CodePrimitiveExpression(tool)),
			    	new CodeAttributeArgument(new CodePrimitiveExpression(version.ToString()))
			));
		}
	}

	class ReplaceBaseType : RazorCodeTransformerBase
	{
		public override void Initialize (RazorHost razorHost, IDictionary<string, string> directives)
		{
			razorHost.DefaultBaseClass = "System.Object";
		}

		bool hasBaseType;

		public override void ProcessGeneratedCode (CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			hasBaseType = generatedClass.BaseTypes [0].BaseType != "System.Object";
			if (hasBaseType)
				return;

			executeMethod.Attributes = (executeMethod.Attributes & (~MemberAttributes.AccessMask | ~MemberAttributes.Override))
				| MemberAttributes.Private | MemberAttributes.Final;

			generatedClass.Members.Add (new CodeSnippetTypeMember (@"
        System.IO.TextWriter __razor_writer;

        private void WriteLiteral (string value)
        {
            __razor_writer.Write (value);
        }

        public string GenerateString ()
        {
            using (var sw = new System.IO.StringWriter ()) {
                Generate (sw);
                return sw.ToString();
	        }
        }

        public void Generate (System.IO.TextWriter writer)
        {
            this.__razor_writer = writer;
            Execute ();
            this.__razor_writer = null;
        }

        private void Write (object value)
        {
            __razor_writer.Write (value);
        }
        "));
		}
	}

	class FixMonoPragmas : RazorCodeTransformerBase
	{
		bool isMono = Type.GetType ("Mono.Runtime") != null;

		public override string ProcessOutput (string codeContent)
		{
			return isMono ? codeContent.Replace ("#line hidden", "#line hidden" + Environment.NewLine) : codeContent;
		}
	}
}