using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (ISignatureHelpBroker))]
	internal class SignatureHelpBroker : ISignatureHelpBroker
	{
		public ISignatureHelpSession CreateSignatureHelpSession (ITextView textView, ITrackingPoint triggerPoint, bool trackCaret)
		{
			throw new NotImplementedException ();
		}

		public void DismissAllSessions (ITextView textView)
		{
			throw new NotImplementedException ();
		}

		public ReadOnlyCollection<ISignatureHelpSession> GetSessions (ITextView textView)
		{
			throw new NotImplementedException ();
		}

		public bool IsSignatureHelpActive (ITextView textView)
		{
			return false;
		}

		public ISignatureHelpSession TriggerSignatureHelp (ITextView textView)
		{
			throw new NotImplementedException ();
		}

		public ISignatureHelpSession TriggerSignatureHelp (ITextView textView, ITrackingPoint triggerPoint, bool trackCaret)
		{
			throw new NotImplementedException ();
		}
	}
}
