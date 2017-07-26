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
	public class GenerateRuntimeImports : Task
	{
		[Required]
		public string AddinFolder { get; set; }

		[Required]
		public string ManifestFile { get; set; }
		
		public override bool Execute ()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("    <Runtime>");

			var addinBaseDir = Path.GetFullPath(AddinFolder);
			
			AddFilesInFolder(addinBaseDir, Path.Combine(addinBaseDir, "Templates"), sb);
			AddFilesInFolder(addinBaseDir, Path.Combine(addinBaseDir, "azure-functions-cli-66a932fb"), sb);

			sb.AppendLine("    </Runtime>");

			var existingContents = File.ReadAllText(ManifestFile);
			if (existingContents.Contains("<Runtime/>"))
			{
				existingContents = existingContents.Replace("<Runtime/>", sb.ToString());
				File.WriteAllText(ManifestFile, existingContents);
			} else 
			{
				Log.LogMessage ("Manifest file was not updated.");
			}

			return true;
		}

		void AddFilesInFolder(string addinBaseDir, string folder, StringBuilder sb)
		{
			var dirs = Directory.GetDirectories(folder);
			foreach (var dir in dirs)
			{
				var dirName = Path.GetFileName(dir);
				AddFilesInFolder(addinBaseDir, Path.Combine(folder, dirName), sb);
			}

			var files = Directory.GetFiles(folder);
			foreach (var file in files)
			{
				var fileName = file.Substring(addinBaseDir.Length);
				sb.AppendLine($"        <Import file=\"{fileName}\" />");
			}
		}
	}
}
