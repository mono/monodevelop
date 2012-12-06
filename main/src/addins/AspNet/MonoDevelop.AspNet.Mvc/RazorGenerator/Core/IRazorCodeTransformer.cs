//
// From Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//

using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
	public interface IRazorCodeTransformer
	{
		void Initialize(IRazorHost razorHost);

		void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod);

		string ProcessOutput(string codeContent);
	}
}