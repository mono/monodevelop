using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TextMate.Core.Hosting;
using Mono.Addins;
using MonoDevelop.TextEditor;

namespace Microsoft.VisualStudio.TextMate.VSMac
{
	[Export (typeof (ICustomRepositoryService))]
	sealed class CustomRepositoryService : ICustomRepositoryService
	{
		public Task<IList<(string repositoryPath, string cachePath)>> GetCollectionPathsAsync ()
		{
			var starterKitPath = Path.Combine (
				Path.GetDirectoryName (typeof (CustomRepositoryService).Assembly.Location),
				"Starterkit");

			var collectionPaths = new List<(string, string)>
			{
				(starterKitPath, Path.Combine (starterKitPath, "TextMate.cache"))
			};
			foreach (var node in AddinManager.GetExtensionNodes<TextMateRepositoryExtensionNode> ("/MonoDevelop/Ide/Editor/TextMate")) {
				var path = node.Addin.GetFilePath(node.FolderPath);
				collectionPaths.Add ((path, Path.Combine (path, "TextMate.cache")));
			}

			return Task.FromResult<IList<(string, string)>> (collectionPaths);
		}
	}
}