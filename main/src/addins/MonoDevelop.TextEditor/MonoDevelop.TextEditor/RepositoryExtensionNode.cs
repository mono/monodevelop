using System;
using Mono.Addins;

namespace MonoDevelop.TextEditor
{
	internal class TextMateRepositoryExtensionNode : ExtensionNode
	{
		[NodeAttribute ("folderPath")]
		public string FolderPath { get; set; }
	}
}
