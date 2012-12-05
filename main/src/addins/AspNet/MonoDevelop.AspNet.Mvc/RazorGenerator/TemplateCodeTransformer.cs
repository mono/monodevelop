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
			new DirectivesBasedTransformers(),
			new AddBaseType(),
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
			string tool = "RazorTemplateGenerator";
			Version version = GetType().Assembly.GetName().Version;
			generatedClass.CustomAttributes.Add(
				new CodeAttributeDeclaration(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName,
					new CodeAttributeArgument(new CodePrimitiveExpression(tool)),
			    	new CodeAttributeArgument(new CodePrimitiveExpression(version.ToString()))
			));
		}
	}

	class AddBaseType : RazorCodeTransformerBase
	{
		bool hasBaseType;

		public override void Initialize (RazorHost razorHost, IDictionary<string, string> directives)
		{
			string baseType;
			if ((hasBaseType = directives.TryGetValue ("Inherits", out baseType))) {
			} else {
				baseType = "System.Object";
			}
			razorHost.DefaultBaseClass = baseType;
		}

		public override void ProcessGeneratedCode (CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			if (hasBaseType)
				return;

			generatedClass.Members.Add (new CodeSnippetTypeMember (@"
System.IO.TextWriter writer;

private void WriteLiteral (string value)
{
	writer.Write (value);
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
	this.writer = writer;
	Execute ();
	this.writer = null;
}
"));
		}

		public override string ProcessOutput (string codeContent)
		{
			if (hasBaseType)
				return codeContent.Replace ("public override void Execute()", "private override void Execute()");

			return codeContent.Replace ("public override void Execute()", "private void Execute()");
		}
	}

	class FixMonoPragmas : RazorCodeTransformerBase
	{
		public override string ProcessOutput (string codeContent)
		{
			return codeContent.Replace ("#line hiddenpublic", "#line hidden" + Environment.NewLine + "public");
		}
	}
}