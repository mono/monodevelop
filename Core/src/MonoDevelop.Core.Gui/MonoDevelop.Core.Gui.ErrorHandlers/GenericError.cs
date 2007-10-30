// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui.ErrorHandlers
{
	class GenericError
	{
		GenericError()
		{
			
		}
		
		public static void DisplayError(string message)
		{
			Services.MessageService.ShowError (message);
		}
	}
}
