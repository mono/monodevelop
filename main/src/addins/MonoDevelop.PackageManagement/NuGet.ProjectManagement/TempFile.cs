// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//

using System;
using System.IO;
using NuGet.Common;

namespace NuGet.ProjectManagement
{
	class TempFile : IDisposable
	{
		private readonly string filePath;

		/// <summary>
		/// Constructor. It creates an empty temp file under the temp directory / NuGet, with
		/// extension <paramref name="extension" />.
		/// </summary>
		/// <param name="extension">The extension of the temp file.</param>
		public TempFile (string extension)
		{
			if (string.IsNullOrEmpty (extension))
				throw new ArgumentNullException (nameof (extension));

			string tempDirectory = NuGetEnvironment.GetFolderPath (NuGetFolderPath.Temp);

			Directory.CreateDirectory (tempDirectory);

			int count = 0;
			do {
				filePath = Path.Combine (tempDirectory, Path.GetRandomFileName () + extension);

				if (!File.Exists (filePath)) {
					try {
						// create an empty file
						using (var filestream = File.Open (filePath, FileMode.CreateNew)) {
						}

						// file is created successfully.
						return;
					} catch {
						// Ignore and try again
					}
				}

				count++;
			} while (count < 3);

			throw new InvalidOperationException ("Failed to create NuGet temp file.");
		}

		public override string ToString ()
		{
			return filePath;
		}

		public static implicit operator string (TempFile f)
		{
			return f.filePath;
		}

		public void Dispose ()
		{
			try {
				FileUtility.Delete (filePath);
			} catch {
				// Ignore failures
			}
		}
	}
}
