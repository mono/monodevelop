
using System;
using System.IO;
using Mono.Addins;

namespace TextEditor
{
	public class OpenFileCondition: ConditionType
	{
		public OpenFileCondition ()
		{
			// It's important to notify changes in the status of a condition,
			// to make sure the extension points are properly updated.
			TextEditorApp.OpenFileChanged += delegate {
				NotifyChanged ();
			};
		}
		
		public override bool Evaluate (NodeElement conditionNode)
		{
			// Get the required extension value from an attribute,
			// and check againts the extension of the currently open document
			string val = conditionNode.GetAttribute ("extension");
			if (val.Length > 0) {
				string ext = Path.GetExtension (TextEditorApp.OpenFileName);
				foreach (string requiredExtension in val.Split (','))
					if (ext == "." + requiredExtension)
						return true;
			}
			return false;
		}
	}
}
