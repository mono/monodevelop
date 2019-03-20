using System;
using System.IO;
using MonoDevelop.StressTest;
using Newtonsoft.Json;
using Xamarin.TestReporting.CloudStorage;

namespace ResultParser
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			bool hasLeaks = false;
			foreach (var file in Directory.EnumerateFiles(".", "*.json")) {
				var serializer = new JsonSerializer {
					NullValueHandling = NullValueHandling.Ignore,
				};

				using (var fs = File.OpenRead (file))
				using (var sr = new StreamReader (fs)) 
				using (var jr = new JsonTextReader (sr)) {
					Console.WriteLine (file);

					var result = serializer.Deserialize<ResultDataModel> (jr);
					foreach (var iteration in result.Iterations) {
						if (iteration.Leaks.Count == 0)
							continue;

						hasLeaks = true;
						Console.WriteLine ("Leak detected {0}:", iteration.Id);

						foreach (var kvp in iteration.Leaks) {
							var leak = kvp.Value;
							var url = AzureBlobStorage.DefaultInstance.UploadFile (leak.GraphFileName, "image/svg+xml").ToString ();
							Console.WriteLine ("{0}: {1} {2}", leak.ClassName, leak.Count, url);
						}
					}
				}
			}

			return hasLeaks ? 1 : 0;
		}
	}
}
