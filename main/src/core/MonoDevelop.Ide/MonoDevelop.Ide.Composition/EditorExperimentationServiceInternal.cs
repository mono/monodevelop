using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text.Utilities;

using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (IExperimentationServiceInternal))]
	internal class EditorExperimentationServiceInternal : IExperimentationServiceInternal
	{
		public bool IsCachedFlightEnabled (string flightName)
			=> flightName == "CompletionAPI" && DefaultSourceEditorOptions.Instance.EnableNewEditor;
	}
}
