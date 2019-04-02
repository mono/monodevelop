using System;
using UserInterfaceTests;

namespace MonoDevelop.StressTest
{
	public static class Properties
	{
		const string useNewEditorProperty = "MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableNewEditor";

		public static bool UseNewEditor {
			get => TestService.Session.GetGlobalValue<bool> (useNewEditorProperty);
			set => TestService.Session.SetGlobalValue (useNewEditorProperty, value);
		}
	}
}
