//
// From Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//

using System.CodeDom;
using System.Diagnostics.CodeAnalysis;

namespace RazorGenerator.Core
{
	class ExcludeFromCodeCoverageTransformer : RazorCodeTransformerBase
	{
		public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			var codeTypeReference = new CodeTypeReference(typeof(ExcludeFromCodeCoverageAttribute));
			generatedClass.CustomAttributes.Add(new CodeAttributeDeclaration(codeTypeReference));
		}
	}
}