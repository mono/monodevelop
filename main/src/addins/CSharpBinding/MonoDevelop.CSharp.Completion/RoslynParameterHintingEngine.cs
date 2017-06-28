using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SignatureHelp;
using MonoDevelop.Ide.CodeCompletion;
using System.Collections.Concurrent;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynParameterHintingEngine
	{
		public async Task<ParameterHintingResult> GetParameterDataProviderAsync (Document document, int position, CancellationToken token = default (CancellationToken))
		{
			var providers = CSharpCompletionTextEditorExtension.signatureProviders.Value.ToList ();
			var triggerInfo = new SignatureHelpTriggerInfo (SignatureHelpTriggerReason.TypeCharCommand, (await document.GetTextAsync (token)).ToString ()[position - 1]);

			return await GetParameterDataProviderAsync (providers,  document, position, triggerInfo, token);
		}

		public async Task<ParameterHintingResult> GetParameterDataProviderAsync (List<ISignatureHelpProvider> providers, Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken token = default (CancellationToken))
		{
			var hintingData = new ConcurrentBag<ParameterHintingData> ();
			foreach (var provider in providers) {
				try {
					var signatureHelpItems = await provider.GetItemsAsync (document, position, triggerInfo, token).ConfigureAwait (false);
					if (signatureHelpItems == null)
						continue;
					foreach (var item in signatureHelpItems.Items) {
						hintingData.Add (new SignatureHelpParameterHintingData (item));
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting items from parameter provider " + provider, e);
				}
			}
			return new ParameterHintingResult (hintingData.ToList (), position);
		}
	}
}
