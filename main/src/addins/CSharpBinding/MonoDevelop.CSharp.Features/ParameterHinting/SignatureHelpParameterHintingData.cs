using Microsoft.CodeAnalysis.SignatureHelp;

namespace MonoDevelop.CSharp.Completion
{

	class SignatureHelpParameterHintingData : Ide.CodeCompletion.ParameterHintingData
	{
		public SignatureHelpParameterHintingData(SignatureHelpItem item) : base(null)
		{
			Item = item;
		}

		public SignatureHelpItem Item { get; }

		public override int ParameterCount => Item.Parameters.Length;

		public override bool IsParameterListAllowed => Item.IsVariadic;

		public override string GetParameterName (int parameter) => Item.Parameters[parameter].Name;
	}
}
