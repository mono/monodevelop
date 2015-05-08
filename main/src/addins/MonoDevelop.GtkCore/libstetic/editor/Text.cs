using System;

namespace Stetic.Editor 
{
	public class Text : PropertyTextEditor 
	{
		public Text ()
		{
			// Don't allow editing the text in the editor
			// since there is no room for multiline edit in the grid.
			entry.Sensitive = false;
		}
	}
}
