//
// From Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//

using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
	class RazorCodeTransformerBase : IRazorCodeTransformer
	{
		void IRazorCodeTransformer.Initialize(IRazorHost razorHost)
		{
			Initialize((RazorHost)razorHost);
		}

		public virtual void Initialize(RazorHost razorHost)
		{
			// do nothing
		}

		public virtual void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			// do nothing.
		}

		public virtual string ProcessOutput(string codeContent)
		{
			return codeContent;
		}
	}
}