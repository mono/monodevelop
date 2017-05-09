using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp.Completion;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace CSharpBinding.MonoDevelop.CSharp.Features.ParameterHinting
{

	class SignatureHelpParameterHintingData : IParameterHintingData
	{
		SignatureHelpItem item;

		public SignatureHelpParameterHintingData(SignatureHelpItem item)
		{
			this.item = item;
		}

		public ISymbol Symbol => null;

		public SignatureHelpItem Item => item;

		public int ParameterCount => item.Parameters.Length;

		public bool IsParameterListAllowed => item.IsVariadic;

		public string GetParameterName (int currentParameter)
		{
			return item.Parameters[currentParameter].Name;
		}
	}
}
