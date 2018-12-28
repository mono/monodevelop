namespace Microsoft.VisualStudio.Text.Editor
{
	internal interface IMdTextView :
#if WINDOWS
        ITextView2
#else
		ITextView3
#endif
    {
#if WINDOWS
        ISpaceReservationManager GetSpaceReservationManager(string name);
#endif

        Gtk.Container VisualElement
        {
            get;
        }
	}
}