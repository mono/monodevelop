using System;
using Mono.Debugger;

namespace MonoDevelop.DebuggerVisualizers {

	public abstract class DialogDebuggerVisualizer {
		protected DialogDebuggerVisualizer ()
		{
		}

		protected internal abstract void Show (IDialogVisualizerService visualizerService,
						       IVisualizerObjectProvider objectProvider);
	}
}
