using System.Composition;
using Microsoft.VisualStudio.Text.Utilities;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (IExperimentationServiceInternal))]
	internal class EditorExperimentationServiceInternal : IExperimentationServiceInternal
	{
		public bool IsCachedFlightEnabled (string flightName) => flightName == "CompletionAPI";
	}
}
