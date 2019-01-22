namespace MonoDevelop.Debugger
{
	public interface IActiveGlyphDropHandler
	{
		bool CanDrop (int line, int column);
		void DropAtLocation (int line, int column);
	}
}
