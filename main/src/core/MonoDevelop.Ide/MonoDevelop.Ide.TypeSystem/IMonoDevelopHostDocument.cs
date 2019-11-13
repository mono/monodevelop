using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Ide.TypeSystem
{
	interface IMonoDevelopHostDocument
	{
		/// <summary>
		/// Updates the text of the document.
		/// </summary>
		void UpdateText (SourceText newText);
	}

	static class MonoDevelopHostDocumentRegistration
	{
		internal static void Register (ITextBuffer textBuffer, IMonoDevelopHostDocument hostDocument)
		{
			textBuffer?.Properties.AddProperty (typeof (IMonoDevelopHostDocument), hostDocument);
		}

		internal static void UnRegister(ITextBuffer textBuffer)
		{
			textBuffer?.Properties.RemoveProperty (typeof (IMonoDevelopHostDocument));
		}

		internal static IMonoDevelopHostDocument FromDocument(Document document)
		{
			IMonoDevelopHostDocument containedDocument = null;
			if (document.TryGetText (out SourceText sourceText)) {
				ITextBuffer textBuffer = sourceText.Container.TryGetTextBuffer ();
				var properties = textBuffer?.Properties;
				if (properties == null)
					return null;
				if (properties.ContainsProperty (typeof (IMonoDevelopHostDocument)))
					containedDocument = properties.GetProperty<IMonoDevelopHostDocument> (typeof (IMonoDevelopHostDocument));
			}

			return containedDocument;
		}
	}
}
