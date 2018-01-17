using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Editor
{
	interface IMdTextView : ITextView
	{
		/// <summary>
		/// Gets a named <see cref="ISpaceReservationManager"/>.
		/// </summary>
		/// <param name="name">The name of the manager.</param>
		/// <returns>An instance of the manager in this view. Not null.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="name"/> is not registered via an <see cref="SpaceReservationManagerDefinition"/>.</exception>
		/// <remarks>
		/// <para>Managers must be exported using <see cref="SpaceReservationManagerDefinition"/> component parts.</para>
		/// </remarks>
		ISpaceReservationManager GetSpaceReservationManager (string name);

		Mono.TextEditor.MonoTextEditor VisualElement
        {
            get;
        }
}
}