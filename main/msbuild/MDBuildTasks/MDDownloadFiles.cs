using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MDBuildTasks
{
	public class MDDownloadFiles : Task
	{
		[Required]
		public ITaskItem [] Downloads { get; set; }

		public override bool Execute ()
		{
			string cacheDirectory = GetCacheDirectory ("MDBuild");

			foreach (var taskItem in Downloads) {
				string url = taskItem.ItemSpec;
				Uri uriObj;
				if (!Uri.TryCreate (url, UriKind.Absolute, out uriObj)) {
					Log.LogError ($"Download has invalid URL '{url}'");
					return false;
				}

				string sha1 = taskItem.GetMetadata ("SHA1");
				if (string.IsNullOrEmpty (sha1)) {
					Log.LogError ($"Item '{url}' has no SHA metadata");
					return false;
				}

				string outputName = taskItem.GetMetadata ("OutputName");
				if (string.IsNullOrEmpty (outputName)) {
					if (string.Equals (taskItem.GetMetadata ("Unpack"), "true", StringComparison.OrdinalIgnoreCase)) {
						outputName = Path.GetFileNameWithoutExtension (uriObj.LocalPath);
					} else {
						outputName = Path.GetFileName (uriObj.LocalPath);
					}
				}

				var outDir = taskItem.GetMetadata ("OutputDir");
				if (string.IsNullOrEmpty (outDir)) {
					outDir = Environment.CurrentDirectory;
				} else {
					outDir = Path.GetFullPath (outDir);
				}

				if (!DownloadFile (cacheDirectory, url, sha1, outDir, outputName, string.Equals (taskItem.GetMetadata ("Unpack"), "true", StringComparison.OrdinalIgnoreCase))) {
					return false;
				}
			}

			return true;
		}

		bool DownloadFile (string cacheDir, string url, string sha1, string outputDir, string outputName, bool unpack)
		{
			string cacheFile = Path.Combine (cacheDir, $"{sha1.Substring (0, 8)}-{outputName}");
			string verifiedFile = cacheFile + ".verified";

			if (!File.Exists (verifiedFile)) {
				Log.LogMessage ($"File '{url}' not found in cache, downloading");
				if (File.Exists (cacheFile)) {
					File.Delete (cacheFile);
				}
				Directory.CreateDirectory (Path.GetDirectoryName (cacheFile));
				var webClient = new WebClient ();
				webClient.DownloadFile (url, cacheFile);
				Log.LogMessage ($"File '{url}' downloaded to {cacheFile}");
				string fileSha = GetFileSha1 (cacheFile);
				if (!string.Equals (fileSha, sha1, StringComparison.OrdinalIgnoreCase)) {
					Log.LogError ($"Hash mismatch for file '{cacheFile}': expected {sha1}, got {fileSha}");
					return false;
				}
				File.WriteAllText (verifiedFile, "");
			} else {
				File.SetLastWriteTimeUtc (verifiedFile, DateTime.UtcNow);
			}

			string dest = Path.Combine (outputDir, outputName);
			if (unpack) {
				UnpackIfChanged (cacheFile, dest);
			} else {
				CopyIfChanged (cacheFile, dest);
			}
			return true;
		}

		void CopyIfChanged (string srcFile, string destFile)
		{
			FileInfo srcInfo = new FileInfo (srcFile);
			FileInfo destInfo = new FileInfo (destFile);
			if (destInfo.Exists && destInfo.LastWriteTimeUtc == srcInfo.LastWriteTimeUtc && destInfo.Length == srcInfo.Length) {
				Log.LogMessage ($"Download {destFile} is up to date, skipping");
			} else {
				Log.LogMessage ($"Copying {srcFile} to {destFile}");
				Directory.CreateDirectory (Path.GetDirectoryName (destFile));
				if (destInfo.Exists) {
					File.Delete (destFile);
				}
				File.Copy (srcFile, destFile);
				File.SetLastWriteTimeUtc (destFile, srcInfo.LastWriteTimeUtc);
			}
		}

		void UnpackIfChanged (string srcFile, string destDir)
		{
			var srcInfo = new FileInfo (srcFile);
			var destInfo = new DirectoryInfo (destDir);

			if (destInfo.Exists && destInfo.LastWriteTimeUtc == srcInfo.LastWriteTimeUtc) {
				Log.LogMessage ($"Download {destDir} is up to date, skipping");
			} else {
				Log.LogMessage ($"Extracting {srcFile} to {destDir}");
				Directory.CreateDirectory (destDir);
				if (Directory.Exists (destDir)) {
					Directory.Delete (destDir, true); 
				}
				ZipFile.ExtractToDirectory (srcFile, destDir);
				Directory.SetLastWriteTimeUtc (destDir, srcInfo.LastWriteTimeUtc);
			}
		}

		string GetCacheDirectory (string name)
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
				string folderPath = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
				return Path.Combine (folderPath, string.Format ("{0}Cache", name));
			}

			if (Directory.Exists ("/System/Library/Frameworks/CoreFoundation.framework")) {
				return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Library", "Caches", name);
			}

			string cacheDir = Environment.GetEnvironmentVariable ("XDG_CACHE_HOME");
			if (string.IsNullOrEmpty (cacheDir)) {
				cacheDir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".cache");
			}
			return Path.Combine (cacheDir, name);
		}

		static string GetFileSha1 (string filename)
		{
			using (FileStream fileStream = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var provider = SHA1.Create ()) {
				byte [] hash = provider.ComputeHash (fileStream);
				var sb = new StringBuilder (hash.Length);
				foreach (var b in hash) {
					sb.Append ($"{b:x2}");
				}
				return sb.ToString ();
			}
		}
	}
}
