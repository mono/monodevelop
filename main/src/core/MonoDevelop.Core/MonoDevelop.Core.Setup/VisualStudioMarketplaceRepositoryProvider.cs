//
// VisualStudioMarketplaceRepositoryProvider.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Mono.Addins;
using Mono.Addins.Setup;
using Newtonsoft.Json;

namespace MonoDevelop.Core.Setup
{
	class VisualStudioMarketplaceRepositoryProvider : AddinRepositoryProvider
	{
		public override Repository DownloadRepository (IProgressMonitor monitor, Uri absUri, AddinRepository rr)
		{
			return VisualStudioMarketplaceApi.Instance.DownloadRepository (monitor, absUri, rr.File);
		}

		class VisualStudioMarketplaceApi
		{
			public static VisualStudioMarketplaceApi Instance = new VisualStudioMarketplaceApi ();

			public SearchResult Search (Uri url, MarketplaceQuery query)
			{
				var searchResult = WebRequestHelper.GetResponse (() => {
					var request = (HttpWebRequest)WebRequest.Create (new Uri (url, "_apis/public/gallery/extensionquery"));
					request.Method = "POST";
					request.Accept = "application/json;api-version=3.0-preview.1";
					request.ContentType = "application/json";
					//request.Headers.Add ("X-Market-User-Id", "");//TODO: Get MachineId(MAC?) from IDE?
					request.Headers.Add ("X-Market-Client-Id", $"VSMac");//TODO: Add version
					request.UserAgent = "VSMac";//TODO: Add version
					return request;
				}, (r) => {
					var json = JsonConvert.SerializeObject (query);
					var reqBytes = Encoding.UTF8.GetBytes (json);
					var stream = r.GetRequestStream ();
					stream.Write (reqBytes, 0, reqBytes.Length);
				});
				var serializer = new JsonSerializer ();
				using (var sr = new StreamReader (searchResult.GetResponseStream ()))
				using (var jsonTextReader = new JsonTextReader (sr)) {
					var resp = serializer.Deserialize<SearchResult> (jsonTextReader);
					return resp;
				}
			}

			static string DownloadFile (IProgressMonitor monitor, string url)
			{
				string file = null;
				FileStream fs = null;
				Stream s = null;

				try {
					monitor.BeginTask ("Requesting " + url, 2);
					var resp = WebRequestHelper.GetResponse (
						() => (HttpWebRequest)WebRequest.Create (url),
						r => r.Headers ["Pragma"] = "no-cache"
					);
					monitor.Step (1);
					monitor.BeginTask ("Downloading " + url, (int)resp.ContentLength);

					file = Path.GetTempFileName ();
					fs = new FileStream (file, FileMode.Create, FileAccess.Write);
					s = resp.GetResponseStream ();
					byte [] buffer = new byte [4096];

					int n;
					while ((n = s.Read (buffer, 0, buffer.Length)) != 0) {
						monitor.Step (n);
						fs.Write (buffer, 0, n);
						if (monitor.IsCancelRequested)
							throw new InstallException ("Installation cancelled.");
					}
					fs.Close ();
					s.Close ();
					return file;
				} catch {
					if (fs != null)
						fs.Close ();
					if (s != null)
						s.Close ();
					if (file != null)
						File.Delete (file);
					throw;
				} finally {
					monitor.EndTask ();
					monitor.EndTask ();
				}
			}

			internal Repository DownloadRepository (IProgressMonitor monitor, Uri url, string file)
			{
				var cacheDir = Path.Combine (Path.GetDirectoryName (file), Path.GetFileNameWithoutExtension (file) + "_files");
				const int AllExtensions = 300;
				var extensions = Search (url, new MarketplaceQuery () {
					Filters = new Filter []{ new Filter(){
						PageSize = AllExtensions,
						SortBy = SortBy.LastUpdatedDate,
						PageNumber = 0,
						SortOrder = SortOrder.Descending,
						Criteria = new Criterion []{new Criterion{
							FilterType = FilterType.Target,
							Value = "Microsoft.VisualStudio.Mac"
							}}
					}},
					Flags = Flags.IncludeLatestVersionOnly | Flags.IncludeFiles,
					AssetTypes = new [] {
					"Microsoft.VisualStudio.Services.VSIXPackage",
					"Microsoft.VisualStudio.Mac.AddinInfo"
				}
				}).Results.SelectMany (r => r.Extensions);

				// We should probably reconsider fetching logic instread of fetching everything when this warning starts poping up...
				if (extensions.Count () == AllExtensions)
					monitor.ReportWarning ("Number of all extensions on marketplace is past page size.");

				var repo = new Repository () {
					Name = "Visual Studio Marketplace"
				};
				foreach (var extension in extensions) {
					var vsixDownloadUrl = extension.Versions [0].Files.Single (f => f.AssetType == "Microsoft.VisualStudio.Services.VSIXPackage").Source;
					if (string.IsNullOrEmpty (vsixDownloadUrl)) {
						monitor.ReportWarning ($"Extension {extension.ExtensionId}:{extension.Versions [0].Version} does not have Microsoft.VisualStudio.Services.VSIXPackage file.");
						continue;
					}
					var addinInfoUrl = extension.Versions [0].Files.Single (f => f.AssetType == "Microsoft.VisualStudio.Mac.AddinInfo").Source;
					if (string.IsNullOrEmpty (addinInfoUrl)) {
						monitor.ReportWarning ($"Extension {extension.ExtensionId}:{extension.Versions [0].Version} does not have Microsoft.VisualStudio.Mac.AddinInfo file.");
						continue;
					}
					//TODO: Cache addinInfoUrl file
					using (var fs = new StreamReader (DownloadFile (monitor, addinInfoUrl))) {
						repo.Addins.Add (new PackageRepositoryEntry () {
							Addin = AddinInfo.ReadFromAddinFile (fs),
							Url = vsixDownloadUrl
						});
					}
				}
				return repo;
			}

			#region Request objects

			[Flags]
			public enum Flags
			{
				None = 0x0,
				IncludeVersions = 0x1,
				IncludeFiles = 0x2,
				IncludeCategoryAndTags = 0x4,
				IncludeSharedAccounts = 0x8,
				IncludeVersionProperties = 0x10,
				ExcludeNonValidated = 0x20,
				IncludeInstallationTargets = 0x40,
				IncludeAssetUri = 0x80,
				IncludeStatistics = 0x100,
				IncludeLatestVersionOnly = 0x200,
				Unpublished = 0x1000
			}

			public enum FilterType
			{
				Tag = 1,
				ExtensionId = 4,
				Category = 5,
				ExtensionName = 7,
				Target = 8,
				Featured = 9,
				SearchText = 10,
				ExcludeWithFlags = 12
			}

			public enum SortBy
			{
				NoneOrRelevance = 0,
				LastUpdatedDate = 1,
				Title = 2,
				PublisherName = 3,
				InstallCount = 4,
				PublishedDate = 5,
				AverageRating = 6,
				WeightedRating = 12
			}

			public enum SortOrder
			{
				Default = 0,
				Ascending = 1,
				Descending = 2
			}

			public class Criterion
			{
				[JsonProperty ("filterType")]
				public FilterType FilterType { get; set; }

				[JsonProperty ("value")]
				public string Value { get; set; }
			}

			public class Filter
			{
				[JsonProperty ("criteria")]
				public IList<Criterion> Criteria { get; set; }

				[JsonProperty ("pageNumber")]
				public int PageNumber { get; set; }

				[JsonProperty ("pageSize")]
				public int PageSize { get; set; }

				[JsonProperty ("sortBy")]
				public SortBy SortBy { get; set; }

				[JsonProperty ("sortOrder")]
				public SortOrder SortOrder { get; set; }
			}

			public class MarketplaceQuery
			{
				[JsonProperty ("filters")]
				public Filter [] Filters { get; set; }

				[JsonProperty ("assetTypes")]
				public string [] AssetTypes { get; set; }

				[JsonProperty ("flags")]
				public Flags Flags { get; set; }
			}

			#endregion

			#region Response objects

			public class Publisher
			{
				[JsonProperty ("publisherId")]
				public string PublisherId { get; set; }

				[JsonProperty ("publisherName")]
				public string PublisherName { get; set; }

				[JsonProperty ("displayName")]
				public string DisplayName { get; set; }

				[JsonProperty ("flags")]
				public string Flags { get; set; }
			}

			public class ExtensionFile
			{
				[JsonProperty ("assetType")]
				public string AssetType { get; set; }

				[JsonProperty ("source")]
				public string Source { get; set; }
			}

			public class Property
			{
				[JsonProperty ("key")]
				public string Key { get; set; }

				[JsonProperty ("value")]
				public string Value { get; set; }
			}

			public class ExtensionVersion
			{
				[JsonProperty ("version")]
				public string Version { get; set; }

				[JsonProperty ("flags")]
				public string Flags { get; set; }

				[JsonProperty ("lastUpdated")]
				public DateTime LastUpdated { get; set; }

				[JsonProperty ("files")]
				public ExtensionFile [] Files { get; set; }

				[JsonProperty ("properties")]
				public Property [] Properties { get; set; }

				[JsonProperty ("assetUri")]
				public string AssetUri { get; set; }

				[JsonProperty ("fallbackAssetUri")]
				public string FallbackAssetUri { get; set; }
			}

			public class Statistic
			{
				[JsonProperty ("statisticName")]
				public string StatisticName { get; set; }

				[JsonProperty ("value")]
				public double Value { get; set; }
			}

			public class Extension
			{
				[JsonProperty ("publisher")]
				public Publisher Publisher { get; set; }

				[JsonProperty ("extensionId")]
				public string ExtensionId { get; set; }

				[JsonProperty ("extensionName")]
				public string ExtensionName { get; set; }

				[JsonProperty ("displayName")]
				public string DisplayName { get; set; }

				[JsonProperty ("flags")]
				public string Flags { get; set; }

				[JsonProperty ("lastUpdated")]
				public DateTime LastUpdated { get; set; }

				[JsonProperty ("publishedDate")]
				public DateTime PublishedDate { get; set; }

				[JsonProperty ("releaseDate")]
				public DateTime ReleaseDate { get; set; }

				[JsonProperty ("shortDescription")]
				public string ShortDescription { get; set; }

				[JsonProperty ("versions")]
				public ExtensionVersion [] Versions { get; set; }

				[JsonProperty ("statistics")]
				public Statistic [] Statistics { get; set; }

				[JsonProperty ("deploymentType")]
				public int DeploymentType { get; set; }
			}

			public class MetadataItem
			{
				[JsonProperty ("name")]
				public string Name { get; set; }

				[JsonProperty ("count")]
				public int Count { get; set; }
			}

			public class ResultMetadata
			{
				[JsonProperty ("metadataType")]
				public string MetadataType { get; set; }

				[JsonProperty ("metadataItems")]
				public MetadataItem [] MetadataItems { get; set; }
			}

			public class Result
			{
				[JsonProperty ("extensions")]
				public Extension [] Extensions { get; set; }

				[JsonProperty ("pagingToken")]
				public string PagingToken { get; set; }

				[JsonProperty ("resultMetadata")]
				public ResultMetadata [] ResultMetadata { get; set; }
			}

			public class SearchResult
			{
				[JsonProperty ("results")]
				public Result [] Results { get; set; }
			}

			#endregion
		}
	}
}
