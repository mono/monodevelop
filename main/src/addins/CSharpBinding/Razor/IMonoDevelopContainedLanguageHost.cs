namespace Microsoft.VisualStudio.Platform
{
	public interface IMonoDevelopContainedLanguageHost
	{
		void GetLineIndent (int lLineNumber, out string indentString, out int parentIndentLevel, out int indentSize, out bool useTabs, out int tabSize);
	}
}
