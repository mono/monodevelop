// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
#if !CLR2COMPATIBILITY
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using Microsoft.Build.Utilities;
using MonoDevelop.Core;

namespace Microsoft.Build.Shared
{
	/// <summary>
	/// This class contains utility methods for file IO.
	/// PERF\COVERAGE NOTE: Try to keep classes in 'shared' as granular as possible. All the methods in
	/// each class get pulled into the resulting assembly.
	/// </summary>
	internal static partial class FileUtilities
	{
		// A list of possible test runners. If the program running has one of these substrings in the name, we assume
		// this is a test harness.


		// This flag, when set, indicates that we are running tests. Initially assume it's true. It also implies that
		// the currentExecutableOverride is set to a path (that is non-null). Assume this is not initialized when we
		// have the impossible combination of runningTests = false and currentExecutableOverride = null.

		// This is the fake current executable we use in case we are running tests.

		// MaxPath accounts for the null-terminating character, for example, the maximum path on the D drive is "D:\<256 chars>\0".
		// See: ndp\clr\src\BCL\System\IO\Path.cs
		internal const int MaxPath = 260;

		/// <summary>
		/// The directory where MSBuild stores cache information used during the build.
		/// </summary>
		internal static string cacheDirectory = null;

		/// <summary>
		/// FOR UNIT TESTS ONLY
		/// Clear out the static variable used for the cache directory so that tests that
		/// modify it can validate their modifications.
		/// </summary>
		internal static void ClearCacheDirectoryPath ()
		{
			cacheDirectory = null;
		}

		// TODO: assumption on file system case sensitivity: https://github.com/Microsoft/msbuild/issues/781
		internal static readonly StringComparison PathComparison = StringComparison.OrdinalIgnoreCase;

		/// <summary>
		/// Copied from https://github.com/dotnet/corefx/blob/056715ff70e14712419d82d51c8c50c54b9ea795/src/Common/src/System/IO/PathInternal.Windows.cs#L61
		/// MSBuild should support the union of invalid path chars across the supported OSes, so builds can have the same behaviour crossplatform: https://github.com/Microsoft/msbuild/issues/781#issuecomment-243942514
		/// </summary>

		internal static readonly char [] InvalidPathChars = new char []
		{
			'|', '\0',
			(char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
			(char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
			(char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
			(char)31
		};

		/// <summary>
		/// Copied from https://github.com/dotnet/corefx/blob/387cf98c410bdca8fd195b28cbe53af578698f94/src/System.Runtime.Extensions/src/System/IO/Path.Windows.cs#L18
		/// MSBuild should support the union of invalid path chars across the supported OSes, so builds can have the same behaviour crossplatform: https://github.com/Microsoft/msbuild/issues/781#issuecomment-243942514
		/// </summary>
		internal static readonly char [] InvalidFileNameChars = new char []
		{
			'\"', '<', '>', '|', '\0',
			(char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
			(char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
			(char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
			(char)31, ':', '*', '?', '\\', '/'
		};

		private static readonly char [] Slashes = { '/', '\\' };

#if !CLR2COMPATIBILITY
		private static ConcurrentDictionary<string, bool> FileExistenceCache = new ConcurrentDictionary<string, bool> (StringComparer.OrdinalIgnoreCase);
#endif

		/// <summary>
		/// Retrieves the MSBuild runtime cache directory
		/// </summary>
		internal static string GetCacheDirectory ()
		{
			if (cacheDirectory == null) {
				cacheDirectory = Path.Combine (Path.GetTempPath (), String.Format (CultureInfo.CurrentUICulture, "MSBuild{0}", Process.GetCurrentProcess ().Id));
			}

			return cacheDirectory;
		}

		/// <summary>
		/// Get the hex hash string for the string
		/// </summary>
		internal static string GetHexHash (string stringToHash)
		{
			return stringToHash.GetHashCode ().ToString ("X", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// If the given path doesn't have a trailing slash then add one.
		/// If the path is an empty string, does not modify it.
		/// </summary>
		/// <param name="fileSpec">The path to check.</param>
		/// <returns>A path with a slash.</returns>
		internal static string EnsureTrailingSlash (string fileSpec)
		{
			fileSpec = FixFilePath (fileSpec);
			if (fileSpec.Length > 0 && !EndsWithSlash (fileSpec)) {
				fileSpec += Path.DirectorySeparatorChar;
			}

			return fileSpec;
		}

		/// <summary>
		/// Ensures the path does not have a leading slash.
		/// </summary>
		internal static string EnsureNoLeadingSlash (string path)
		{
			path = FixFilePath (path);
			if (path.Length > 0 && IsSlash (path [0])) {
				path = path.Substring (1);
			}

			return path;
		}

		/// <summary>
		/// Ensures the path does not have a trailing slash.
		/// </summary>
		internal static string EnsureNoTrailingSlash (string path)
		{
			path = FixFilePath (path);
			if (EndsWithSlash (path)) {
				path = path.Substring (0, path.Length - 1);
			}

			return path;
		}

		/// <summary>
		/// Indicates if the given file-spec ends with a slash.
		/// </summary>
		/// <param name="fileSpec">The file spec.</param>
		/// <returns>true, if file-spec has trailing slash</returns>
		internal static bool EndsWithSlash (string fileSpec)
		{
			return (fileSpec.Length > 0)
				? IsSlash (fileSpec [fileSpec.Length - 1])
				: false;
		}

		/// <summary>
		/// Indicates if the given character is a slash.
		/// </summary>
		/// <param name="c"></param>
		/// <returns>true, if slash</returns>
		internal static bool IsSlash (char c)
		{
			return ((c == Path.DirectorySeparatorChar) || (c == Path.AltDirectorySeparatorChar));
		}

		/// <summary>
		/// Trims the string and removes any double quotes around it.
		/// </summary>
		internal static string TrimAndStripAnyQuotes (string path)
		{
			// Trim returns the same string if trimming isn't needed
			path = path.Trim ();
			path = path.Trim (new char [] { '"' });

			return path;
		}

		/// <summary>
		/// Get the directory name of a rooted full path
		/// </summary>
		/// <param name="fullPath"></param>
		/// <returns></returns>
		internal static String GetDirectoryNameOfFullPath (String fullPath)
		{
			if (fullPath != null) {
				int i = fullPath.Length;
				while (i > 0 && fullPath [--i] != Path.DirectorySeparatorChar && fullPath [i] != Path.AltDirectorySeparatorChar) ;
				return FixFilePath (fullPath.Substring (0, i));
			}
			return null;
		}

		/// <summary>
		/// Compare an unsafe char buffer with a <see cref="System.String"/> to see if their contents are identical.
		/// </summary>
		/// <param name="buffer">The beginning of the char buffer.</param>
		/// <param name="len">The length of the buffer.</param>
		/// <param name="s">The string.</param>
		/// <returns>True only if the contents of <paramref name="s"/> and the first <paramref name="len"/> characters in <paramref name="buffer"/> are identical.</returns>
		private unsafe static bool AreStringsEqual (char* buffer, int len, string s)
		{
			if (len != s.Length) {
				return false;
			}

			foreach (char ch in s) {
				if (ch != *buffer++) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Gets the canonicalized full path of the provided path.
		/// Path.GetFullPath The pre .Net 4.6.2 implementation of Path.GetFullPath is slow and creates strings in its work.
		/// Therefore MSBuild has its own implementation on full framework.
		/// Guidance for use: call this on all paths accepted through public entry
		/// points that need normalization. After that point, only verify the path
		/// is rooted, using ErrorUtilities.VerifyThrowPathRooted.
		/// ASSUMES INPUT IS ALREADY UNESCAPED.
		/// </summary>
		internal static string NormalizePath (string path)
		{
			if (path.Length == 0)
				throw new ArgumentException (path, "path");

#if FEATURE_LEGACY_GETFULLPATH

            if (NativeMethodsShared.IsWindows)
            {
                int errorCode = 0; // 0 == success in Win32

#if _DEBUG
                // Just to make sure and exercise the code that sets the correct buffer size
                // we'll start out with it deliberately too small
                int lenDir = 1;
#else
                int lenDir = MaxPath;
#endif
                unsafe
                {
                    char* finalBuffer = stackalloc char[lenDir + 1]; // One extra for the null terminator

                    int length = NativeMethodsShared.GetFullPathName(path, lenDir + 1, finalBuffer, IntPtr.Zero);
                    errorCode = Marshal.GetLastWin32Error();

                    // If the length returned from GetFullPathName is greater than the length of the buffer we've
                    // allocated, then reallocate the buffer with the correct size, and repeat the call
                    if (length > lenDir)
                    {
                        lenDir = length;
                        char* tempBuffer = stackalloc char[lenDir];
                        finalBuffer = tempBuffer;
                        length = NativeMethodsShared.GetFullPathName(path, lenDir, finalBuffer, IntPtr.Zero);
                        errorCode = Marshal.GetLastWin32Error();
                        // If we find that the length returned from GetFullPathName is longer than the buffer capacity, then
                        // something very strange is going on!
                        ErrorUtilities.VerifyThrow(
                            length <= lenDir,
                            "Final buffer capacity should be sufficient for full path name and null terminator.");
                    }

                    if (length > 0)
                    {
                        // In order to prevent people from taking advantage of our ability to extend beyond MaxPath
                        // since it is unlikely that the CLR fix will be a complete removal of maxpath madness
                        // we reluctantly have to restrict things here.
                        if (length >= MaxPath)
                        {
                            throw new PathTooLongException();
                        }

                        // Avoid creating new strings unnecessarily
                        string finalFullPath = AreStringsEqual(finalBuffer, length, path)
                            ? path
                            : new string(
                                finalBuffer,
                                startIndex: 0,
                                length: length);

                        // We really don't care about extensions here, but Path.HasExtension provides a great way to
                        // invoke the CLR's invalid path checks (these are independent of path length)
                        Path.HasExtension(finalFullPath);

                        if (finalFullPath.StartsWith(@"\\", StringComparison.Ordinal))
                        {
                            // If we detect we are a UNC path then we need to use the regular get full path in order to do the correct checks for UNC formatting
                            // and security checks for strings like \\?\GlobalRoot
                            int startIndex = 2;
                            while (startIndex < finalFullPath.Length)
                            {
                                if (finalFullPath[startIndex] == '\\')
                                {
                                    startIndex++;
                                    break;
                                }
                                else
                                {
                                    startIndex++;
                                }
                            }

                            /*
                              From Path.cs in the CLR

                              Throw an ArgumentException for paths like \\, \\server, \\server\
                              This check can only be properly done after normalizing, so
                              \\foo\.. will be properly rejected.  Also, reject \\?\GLOBALROOT\
                              (an internal kernel path) because it provides aliases for drives.

                              throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegalUNC"));

                               // Check for \\?\Globalroot, an internal mechanism to the kernel
                               // that provides aliases for drives and other undocumented stuff.
                               // The kernel team won't even describe the full set of what
                               // is available here - we don't want managed apps mucking
                               // with this for security reasons.
                            */
                    if (startIndex == finalFullPath.Length || finalFullPath.IndexOf(@"\\?\globalroot", PathComparison) != -1)
                    {
                        finalFullPath = Path.GetFullPath(finalFullPath);
                    }
                }

                        return finalFullPath;
                    }
                }

                NativeMethodsShared.ThrowExceptionForErrorCode(errorCode);
                return null;
            }
#endif
			return FixFilePath (Path.GetFullPath (path));

		}

		internal static string FixFilePath (string path)
		{
			return string.IsNullOrEmpty (path) || Path.DirectorySeparatorChar == '\\' ? path : path.Replace ('\\', '/');//.Replace("//", "/");
		}

		/// <summary>
		/// If on Unix, convert backslashes to slashes for strings that resemble paths.
		/// The heuristic is if something resembles paths (contains slashes) check if the
		/// first segment exists and is a directory.
		/// Use a native shared method to massage file path. If the file is adjusted,
		/// that qualifies is as a path.
		///
		/// @baseDirectory is just passed to LooksLikeUnixFilePath, to help with the check
		/// </summary>
		internal static string MaybeAdjustFilePath (string value, string baseDirectory = "")
		{
			// Don't bother with arrays or properties or network paths, or those that
			// have no slashes.
			if (Platform.IsWindows || string.IsNullOrEmpty (value) ||
				value.StartsWith ("$(") || value.StartsWith ("@(") || value.StartsWith ("\\\\") ||
				value.IndexOfAny (Slashes) == -1) {
				return value;
			}

			// For Unix-like systems, we may want to convert backslashes to slashes
			string newValue = Regex.Replace (value, @"[\\/]+", "/");

			string quote = string.Empty;
			// Find the part of the name we want to check, that is remove quotes, if present
			string checkValue = newValue;
			if (newValue.Length > 2) {
				if (newValue.StartsWith ("'")) {
					if (newValue.EndsWith ("'")) {
						checkValue = newValue.Substring (1, newValue.Length - 2);
						quote = "'";
					}
				} else if (newValue.StartsWith ("\"") && newValue.EndsWith ("\"")) {
					checkValue = newValue.Substring (1, newValue.Length - 2);
					quote = "\"";
				}
			}

			return LooksLikeUnixFilePath (checkValue, baseDirectory) ? newValue : value;
		}

		/// <summary>
		/// If on Unix, check if the string looks like a file path.
		/// The heuristic is if something resembles paths (contains slashes) check if the
		/// first segment exists and is a directory.
		///
		/// If @baseDirectory is not null, then look for the first segment exists under
		/// that
		/// </summary>
		internal static bool LooksLikeUnixFilePath (string value, string baseDirectory = "")
		{
			if (Platform.IsWindows) {
				return false;
			}

			var firstSlash = value.IndexOf ('/');

			// The first slash will either be at the beginning of the string or after the first directory name
			if (firstSlash == 0) {
				firstSlash = value.Substring (1).IndexOf ('/') + 1;
			}

			if (firstSlash > 0 && Directory.Exists (Path.Combine (baseDirectory, value.Substring (0, firstSlash)))) {
				return true;
			}

			// Check for actual files or directories under / that get missed by the above logic
			if (firstSlash == 0 && value [0] == '/' && (Directory.Exists (value) || File.Exists (value))) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Extracts the directory from the given file-spec.
		/// </summary>
		/// <param name="fileSpec">The filespec.</param>
		/// <returns>directory path</returns>
		internal static string GetDirectory (string fileSpec)
		{
			string directory = Path.GetDirectoryName (FixFilePath (fileSpec));

			// if file-spec is a root directory e.g. c:, c:\, \, \\server\share
			// NOTE: Path.GetDirectoryName also treats invalid UNC file-specs as root directories e.g. \\, \\server
			if (directory == null) {
				// just use the file-spec as-is
				directory = fileSpec;
			} else if ((directory.Length > 0) && !EndsWithSlash (directory)) {
				// restore trailing slash if Path.GetDirectoryName has removed it (this happens with non-root directories)
				directory += Path.DirectorySeparatorChar;
			}

			return directory;
		}

		/// <summary>
		/// Determines whether the given assembly file name has one of the listed extensions.
		/// </summary>
		/// <param name="fileName">The name of the file</param>
		/// <param name="allowedExtensions">Array of extensions to consider.</param>
		/// <returns></returns>
		internal static bool HasExtension (string fileName, string [] allowedExtensions)
		{
			string fileExtension = Path.GetExtension (fileName);
			foreach (string extension in allowedExtensions) {
				if (String.Compare (fileExtension, extension, StringComparison.CurrentCultureIgnoreCase) == 0) {
					return true;
				}
			}
			return false;
		}

		// ISO 8601 Universal time with sortable format
		internal const string FileTimeFormat = "yyyy'-'MM'-'dd HH':'mm':'ss'.'fffffff";

		internal static string TrimTrailingSlashes (this string s)
		{
			return s.TrimEnd (Slashes);
		}

		/// <summary>
		/// Replace all backward slashes to forward slashes
		/// </summary>
		internal static string ToSlash (this string s)
		{
			return s.Replace ('\\', '/');
		}

		internal static string WithTrailingSlash (this string s)
		{
			return EnsureTrailingSlash (s);
		}

		internal static string NormalizeForPathComparison (this string s) => s.ToSlash ().TrimTrailingSlashes ();

		// TODO: assumption on file system case sensitivity: https://github.com/Microsoft/msbuild/issues/781
		internal static bool PathsEqual (string path1, string path2)
		{
			if (path1 == null && path2 == null) {
				return true;
			}
			if (path1 == null || path2 == null) {
				return false;
			}

			var endA = path1.Length - 1;
			var endB = path2.Length - 1;

			// Trim trailing slashes
			for (var i = endA; i >= 0; i--) {
				var c = path1 [i];
				if (c == '/' || c == '\\') {
					endA--;
				} else {
					break;
				}
			}

			for (var i = endB; i >= 0; i--) {
				var c = path2 [i];
				if (c == '/' || c == '\\') {
					endB--;
				} else {
					break;
				}
			}

			if (endA != endB) {
				// Lengths not the same
				return false;
			}

			for (var i = 0; i <= endA; i++) {
				var charA = (uint)path1 [i];
				var charB = (uint)path2 [i];

				if ((charA | charB) > 0x7F) {
					// Non-ascii chars move to non fast path
					return PathsEqualNonAscii (path1, path2, i, endA - i + 1);
				}

				// uppercase both chars - notice that we need just one compare per char
				if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
				if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;

				// Set path delimiters the same
				if (charA == '\\') {
					charA = '/';
				}
				if (charB == '\\') {
					charB = '/';
				}

				if (charA != charB) {
					return false;
				}
			}

			return true;
		}

		internal static StreamWriter OpenWrite (string path, bool append, Encoding encoding = null)
		{
			const int DefaultFileStreamBufferSize = 4096;
			FileMode mode = append ? FileMode.Append : FileMode.Create;
			Stream fileStream = new FileStream (path, mode, FileAccess.Write, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.SequentialScan);
			if (encoding == null) {
				return new StreamWriter (fileStream);
			} else {
				return new StreamWriter (fileStream, encoding);
			}
		}

		internal static StreamReader OpenRead (string path, Encoding encoding = null, bool detectEncodingFromByteOrderMarks = true)
		{
			const int DefaultFileStreamBufferSize = 4096;
			Stream fileStream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.SequentialScan);
			if (encoding == null) {
				return new StreamReader (fileStream);
			} else {
				return new StreamReader (fileStream, encoding, detectEncodingFromByteOrderMarks);
			}
		}

		// Method is simple set of function calls and may inline;
		// we don't want it inlining into the tight loop that calls it as an exit case,
		// so mark as non-inlining
		[MethodImpl (MethodImplOptions.NoInlining)]
		private static bool PathsEqualNonAscii (string strA, string strB, int i, int length)
		{
			if (string.Compare (strA, i, strB, i, length, StringComparison.OrdinalIgnoreCase) == 0) {
				return true;
			}

			var slash1 = strA.ToSlash ();
			var slash2 = strB.ToSlash ();

			if (string.Compare (slash1, i, slash2, i, length, StringComparison.OrdinalIgnoreCase) == 0) {
				return true;
			}

			return false;
		}

#if !CLR2COMPATIBILITY
		/// <summary>
		/// Clears the file existence cache.
		/// </summary>
		internal static void ClearFileExistenceCache ()
		{
			FileExistenceCache.Clear ();
		}
#endif
	}
}