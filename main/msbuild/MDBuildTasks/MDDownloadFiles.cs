using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
					Log.LogError (string.Format ("Download has invalid URL '{0}'", url));
					return false;
				}

				string sha1 = taskItem.GetMetadata ("SHA1");
				if (string.IsNullOrEmpty (sha1)) {
					Log.LogError (string.Format ("Item '{0}' has no SHA metadata", url));
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
			string cacheFile = Path.Combine (cacheDir, string.Format("{0}-{1}", sha1.Substring(0, 8), outputName));
			string verifiedFile = cacheFile + ".verified";

			if (!File.Exists (verifiedFile)) {
				Log.LogMessage (string.Format ("File '{0}' not found in cache, downloading", url));
				if (File.Exists (cacheFile)) {
					File.Delete (cacheFile);
				}
				Directory.CreateDirectory (Path.GetDirectoryName (cacheFile));
				var webClient = new WebClient ();
				webClient.DownloadFile (url, cacheFile);
				Log.LogMessage (string.Format ("File '{0}' downloaded to {1}", url, cacheFile));
				string fileSha = GetFileSha1 (cacheFile);
				if (!string.Equals (fileSha, sha1, StringComparison.OrdinalIgnoreCase)) {
					Log.LogError (string.Format ("Hash mismatch for file '{0}': expected {1}, got {2}", cacheFile, sha1, fileSha));
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
				Log.LogMessage (string.Format ("Download {0} is up to date, skipping", destFile));
			} else {
				Log.LogMessage (string.Format ("Copying {0} to {1}", srcFile, destFile));
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
				Log.LogMessage (string.Format ("Download {0} is up to date, skipping", destDir));
			} else {
				Log.LogMessage (string.Format( "Extracting {0} to {1}", srcFile, destDir));
				Directory.CreateDirectory (destDir);
				if (Directory.Exists (destDir)) {
					Directory.Delete (destDir, true);
				}

				using (var zip = ZipFile.Open (srcFile, ZipArchiveMode.Read)) {
					var prefix = Path.GetFileName (destDir) + Path.DirectorySeparatorChar;
					var trimChars = new [] { Path.DirectorySeparatorChar };
					foreach (var e in zip.Entries) {
						//don't care about bare directories
						if (e.Length == 0) {
							continue;
						}

						//strip redundant prefix
						var fileDest = e.FullName;
						if (fileDest.StartsWith (prefix, StringComparison.Ordinal)) {
							fileDest = fileDest.Substring (prefix.Length).TrimStart (trimChars);
						}

						//guard against breaking out with ..
						fileDest = Path.GetFullPath (Path.Combine (destDir,  fileDest));
						if (!fileDest.StartsWith (destDir, StringComparison.Ordinal)) {
							throw new Exception (string.Format ("Cannot extract outside target dir: '{0}'", e.FullName));
						}

						Directory.CreateDirectory (Path.GetDirectoryName (fileDest));
						e.ExtractToFile (fileDest);
					}
				}
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
					sb.Append (string.Format ("{0:x2}", b));
				}
				return sb.ToString ();
			}
		}
	}
}
