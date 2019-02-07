namespace Microsoft.VisualStudio.Text.Editor
{
	internal interface IMdTextView :
#if WINDOWS
        ITextView2
#else
		ITextView3
#endif
    {
        MonoDevelop.SourceEditor.IMDSpaceReservationManager GetSpaceReservationManager(string name);

        Gtk.Container VisualElement
        {
            get;
        }

        void Focus();
	}
}