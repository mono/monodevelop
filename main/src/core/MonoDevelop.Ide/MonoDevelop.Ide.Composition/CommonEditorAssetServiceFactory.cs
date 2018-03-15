using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (ICommonEditorAssetServiceFactory))]
	internal class CommonEditorAssetServiceFactory : ICommonEditorAssetServiceFactory
	{
		public ICommonEditorAssetService GetOrCreate (ITextBuffer textBuffer)
		{
			return new CommonEditorAssetService (textBuffer);
		}
	}

	public class CommonEditorAssetService : ICommonEditorAssetService
	{
		private ITextBuffer textBuffer;

		public CommonEditorAssetService (ITextBuffer textBuffer)
		{
			this.textBuffer = textBuffer;
		}

		public T FindAsset<T> (Predicate<ICommonEditorAssetMetadata> isMatch = null) where T : class
		{
			return default(T);
		}
	}
}
