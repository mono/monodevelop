namespace Microsoft.VisualStudio.Text.Editor
{
	internal interface IMdTextView : ITextView3
	{
		Gtk.Container VisualElement
        {
            get;
        }
	}
}