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

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (IStreamingFindUsagesPresenter))]
	internal class StreamingFindUsagesPresenter : IStreamingFindUsagesPresenter
	{
		public void ClearAll ()
		{
			throw new NotImplementedException ();
		}

		public FindUsagesContext StartSearch (string title, bool supportsReferences)
		{
			throw new NotImplementedException ();
		}
	}
}
