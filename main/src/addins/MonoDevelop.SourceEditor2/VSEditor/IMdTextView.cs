namespace Microsoft.VisualStudio.Text.Editor
{
	internal interface IMdTextView : ITextView2
    {
        MonoDevelop.SourceEditor.IMDSpaceReservationManager GetSpaceReservationManager(string name);

        Gtk.Container VisualElement
        {
            get;
        }

        void Focus();
	}
}