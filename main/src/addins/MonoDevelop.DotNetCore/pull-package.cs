using System;
using System.IO;
using System.Linq;
using System.Net;

public class App
{
	static string CacheDirectory {
		get {
			return Path.Combine (
				Environment.GetFolderPath (Environment.SpecialFolder.Personal),
				"Cache",
				"xs-compilation"
			);
		}
	}

	public static int Main (string [] args)
	{
		//Reason for this cache is not to save time on downloading file on fresh build
		//but in case downloading fails/you don't have internet connection it uses old cache
		//so this cache should probably be called backup :)

		var sourceUrl = new Uri (args [0]);
		var destFile = args [1];
		var cachePath = Path.Combine (CacheDirectory, Path.GetFileName (destFile));
		var cacheTmpPath = cachePath + ".tmp";

		Console.WriteLine ("Creating directories: {0} and {1}", CacheDirectory, Path.GetDirectoryName (destFile));
		Directory.CreateDirectory (CacheDirectory);
		Directory.CreateDirectory (Path.GetDirectoryName (destFile));

		Console.WriteLine ("Downloading file: '{0}' to cache at '{1}'", sourceUrl, cachePath);
		try {
			WebClient wc = new WebClient ();
			wc.DownloadFile (sourceUrl, cacheTmpPath);
			File.Delete (cachePath);
			File.Move (cacheTmpPath, cachePath);
		} catch (Exception ex) {
			Console.WriteLine ("Could not download the package from {0}. {1}", sourceUrl, ex);
		}

		if (File.Exists (cachePath)) {
			Console.WriteLine ("Using the file from the cache directory. Copying {0} to {1}", cachePath, destFile);
			File.Delete (destFile);
			File.Copy (cachePath, destFile);
			return 0;
		} else {
			Console.WriteLine ("The file was not found in the cache directory... failing");
			return 1;
		}
	}
}