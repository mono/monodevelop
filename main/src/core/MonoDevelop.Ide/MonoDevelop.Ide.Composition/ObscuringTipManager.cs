using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Ide.Composition
{
	//[Export (typeof (IObscuringTipManager))]
	class ObscuringTipManager : IObscuringTipManager
	{
		public void PushTip (ITextView view, IObscuringTip tip)
		{
			throw new NotImplementedException ();
		}

		public void RemoveTip (ITextView view, IObscuringTip tip)
		{
			throw new NotImplementedException ();
		}
	}
}
