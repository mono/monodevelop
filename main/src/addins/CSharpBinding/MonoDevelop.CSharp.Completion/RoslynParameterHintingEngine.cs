using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SignatureHelp;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynParameterHintingEngine
	{
		public async Task<ParameterHintingResult> GetParameterDataProviderAsync (List<ISignatureHelpProvider> providers, Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken token = default (CancellationToken))
		{
			var tasks = providers
				.Select (provider => provider.GetItemsAsync (document, position, triggerInfo, token))
				.ToArray();

			var res = await Task.WhenAll (tasks).ConfigureAwait(false);

			List<ParameterHintingData> hintingData = res
				.Where (list => list != null)
				.SelectMany (list => list.Items)
				.Select (x => (ParameterHintingData)new SignatureHelpParameterHintingData (x))
				.ToList ();

			return new ParameterHintingResult (hintingData, position);
		}
	}
}
