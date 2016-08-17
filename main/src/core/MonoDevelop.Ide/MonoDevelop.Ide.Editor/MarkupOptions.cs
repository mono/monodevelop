namespace MonoDevelop.Ide.Editor
{

	public enum MarkupFormat {
		Pango,
		Html,
		RichtText
	}

	public class MarkupOptions
	{
		public bool FitIdeStyle { get; set; } = false;
		public MarkupFormat MarkupFormat { get; set; } = MarkupFormat.Pango;

		public MarkupOptions (MarkupFormat markupFormat, bool fitIdeStyle = false)
		{
			FitIdeStyle = fitIdeStyle;
			MarkupFormat = markupFormat;
		}
	}
}