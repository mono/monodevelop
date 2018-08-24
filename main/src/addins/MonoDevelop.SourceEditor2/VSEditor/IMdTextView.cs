namespace Microsoft.VisualStudio.Text.Editor
{
	internal interface IMdTextView : ITextView2
	{
		/// <summary>
		/// Gets a named <see cref="ISpaceReservationManager"/>.
		/// </summary>
		/// <param name="name">The name of the manager.</param>
		/// <returns>An instance of the manager in this view. Not null.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="name"/> is not registered via an <see cref="SpaceReservationManagerDefinition"/>.</exception>
		/// <remarks>
		/// <para>Managers must be exported using <see cref="SpaceReservationManagerDefinition"/> component parts.</para>
		/// </remarks>
		ISpaceReservationManager GetSpaceReservationManager (string name);

		Gtk.Container VisualElement
        {
            get;
        }
	}
}