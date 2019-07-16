using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TextMate.Core.Hosting;

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

			return Task.FromResult<IList<(string, string)>> (collectionPaths);
		}
	}
}