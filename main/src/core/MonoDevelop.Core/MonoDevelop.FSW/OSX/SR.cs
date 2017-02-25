// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Resources;
using System.Runtime.CompilerServices;

namespace MonoDevelop.FSW.OSX
{
	internal partial class SR
	{
#if false
		private static ResourceManager s_resourceManager;

		private static ResourceManager ResourceManager
			=> s_resourceManager ?? (s_resourceManager = new ResourceManager (ResourceType));
#endif

		// This method is used to decide if we need to append the exception message parameters to the message when calling SR.Format.
		// by default it returns false.
		[MethodImpl (MethodImplOptions.NoInlining)]
		private static bool UsingResourceKeys ()
		{
			return false;
		}

#if false
		internal static string GetResourceString (string resourceKey, string defaultString)
		{
			string resourceString = null;
			try { resourceString = ResourceManager.GetString (resourceKey); } catch (MissingManifestResourceException) { }

			if (defaultString != null && resourceKey.Equals (resourceString, StringComparison.Ordinal)) {
				return defaultString;
			}

			return resourceString;
		}
#endif

		internal static string Format (string resourceFormat, params object [] args)
		{
			if (args != null) {
				if (UsingResourceKeys ()) {
					return resourceFormat + string.Join (", ", args);
				}

				return string.Format (resourceFormat, args);
			}

			return resourceFormat;
		}

		internal static string Format (string resourceFormat, object p1)
		{
			if (UsingResourceKeys ()) {
				return string.Join (", ", resourceFormat, p1);
			}

			return string.Format (resourceFormat, p1);
		}

		internal static string Format (string resourceFormat, object p1, object p2)
		{
			if (UsingResourceKeys ()) {
				return string.Join (", ", resourceFormat, p1, p2);
			}

			return string.Format (resourceFormat, p1, p2);
		}

		internal static string Format (string resourceFormat, object p1, object p2, object p3)
		{
			if (UsingResourceKeys ()) {
				return string.Join (", ", resourceFormat, p1, p2, p3);
			}

			return string.Format (resourceFormat, p1, p2, p3);
		}

		internal static readonly string Argument_InvalidPathChars = "Illegal characters in path.";
		internal static readonly string ArgumentOutOfRange_FileLengthTooBig = "Specified file length was too large for the file system.";
		internal static readonly string BufferSizeTooLarge = "The specified buffer size is too large. FileSystemWatcher cannot allocate {0} bytes for the internal buffer.";
		internal static readonly string EventStream_FailedToStart = "Failed to start the EventStream";
		internal static readonly string FSW_BufferOverflow = "Too many changes at once in directory:{0}.";
		internal static readonly string InvalidDirName = "The directory name {0} is invalid.";
		internal static readonly string InvalidEnumArgument = "The value of argument '{0}' ({1}) is invalid for Enum type '{2}'.";
		internal static readonly string IO_FileExists_Name = "The file '{0}' already exists.";
		internal static readonly string IO_FileNotFound = "Unable to find the specified file.";
		internal static readonly string IO_FileNotFound_FileName = "Could not find file '{0}'.";
		internal static readonly string IO_PathNotFound_NoPathName = "Could not find a part of the path.";
		internal static readonly string IO_PathNotFound_Path = "Could not find a part of the path '{0}'.";
		internal static readonly string IO_PathTooLong = "The specified file name or path is too long, or a component of the specified path is too long.";
		internal static readonly string IO_SharingViolation_File = "The process cannot access the file '{0}' because it is being used by another process.";
		internal static readonly string IO_SharingViolation_NoFileName = "The process cannot access the file because it is being used by another process.";
		internal static readonly string UnauthorizedAccess_IODenied_NoPathName = "Access to the path is denied.";
		internal static readonly string UnauthorizedAccess_IODenied_Path = "Access to the path '{0}' is denied.";
	}
}
