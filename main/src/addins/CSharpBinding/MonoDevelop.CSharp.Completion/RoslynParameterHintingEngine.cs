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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynParameterHintingEngine
	{
		public async Task<ParameterHintingResult> GetParameterDataProviderAsync (Document document, int position, CancellationToken token = default (CancellationToken))
		{
			var providers = CSharpCompletionTextEditorExtension.signatureProviders.Value.ToList ();
			var triggerInfo = new SignatureHelpTriggerInfo (SignatureHelpTriggerReason.TypeCharCommand, (await document.GetTextAsync (token)).ToString () [position - 1]);

			return await GetParameterDataProviderAsync (providers, document, position, triggerInfo, token);
		}

		public async Task<ParameterHintingResult> GetParameterDataProviderAsync (List<ISignatureHelpProvider> providers, Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken token = default (CancellationToken))
		{
			var hintingData = new List<ParameterHintingData> ();
			SignatureHelpItems bestSignatureHelpItems = null;
			foreach (var provider in providers) {
				try {
					if (triggerInfo.TriggerReason == SignatureHelpTriggerReason.TypeCharCommand && !provider.IsTriggerCharacter (triggerInfo.TriggerCharacter.Value))
						continue;
					if (triggerInfo.TriggerReason == SignatureHelpTriggerReason.RetriggerCommand && !provider.IsRetriggerCharacter (triggerInfo.TriggerCharacter.Value))
						continue;
					var signatureHelpItems = await provider.GetItemsAsync (document, position, triggerInfo, token).ConfigureAwait (false);
					if (signatureHelpItems == null)
						continue;
					if (bestSignatureHelpItems == null)
						bestSignatureHelpItems = signatureHelpItems;
					else if (signatureHelpItems.ApplicableSpan.Start > bestSignatureHelpItems.ApplicableSpan.Start)
						bestSignatureHelpItems = signatureHelpItems;
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting items from parameter provider " + provider, e);
				}
			}

			if (bestSignatureHelpItems != null) {
				foreach (var item in bestSignatureHelpItems.Items) {
					hintingData.Add (new SignatureHelpParameterHintingData (item));
				}
				var tree = await document.GetSyntaxTreeAsync (token);
				var tokenLeftOfPosition = tree.GetRoot (token).FindTokenOnLeftOfPosition (position);
				var syntaxNode = tokenLeftOfPosition.Parent;
				var node = syntaxNode?.FirstAncestorOrSelf<ArgumentListSyntax> ();
				return new ParameterHintingResult (hintingData) {
					ApplicableSpan = bestSignatureHelpItems.ApplicableSpan,
					SelectedItemIndex = bestSignatureHelpItems.SelectedItemIndex,
					ParameterListStart = node != null ? node.SpanStart : bestSignatureHelpItems.ApplicableSpan.Start
				};
			}
			return ParameterHintingResult.Empty;
		}
	}
}
