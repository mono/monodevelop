using Microsoft.CodeAnalysis.SignatureHelp;

namespace MonoDevelop.CSharp.Completion
{
	class SignatureHelpParameterHintingData : Ide.CodeCompletion.ParameterHintingData
	{
		public SignatureHelpParameterHintingData(SignatureHelpItem item)
		{
			Item = item;
		}

		public SignatureHelpItem Item { get; }

		public override int ParameterCount => Item.Parameters.Length;

		public override bool IsParameterListAllowed => Item.IsVariadic;

		public override string GetParameterName (int parameter) => Item.Parameters[parameter].Name;


		public override bool Equals (object obj)
		{
			var other = obj as SignatureHelpParameterHintingData;
			if (other == null)
				return false;
			return Item.ToString () == other.Item.ToString ();
		}
	}
}
