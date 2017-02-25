// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MonoDevelop.FSW.OSX
{
	// OSX
	internal static partial class Interop
	{
		internal static partial class Libraries
		{
			internal const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
			internal const string CoreServicesLibrary = "/System/Library/Frameworks/CoreServices.framework/CoreServices";
#if false
			internal const string libproc = "libproc";
			internal const string LibSystemCommonCrypto = "/usr/lib/system/libcommonCrypto";
			internal const string LibSystemKernel = "/usr/lib/system/libsystem_kernel";
			internal const string SystemConfigurationLibrary = "/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration";
			internal const string AppleCryptoNative = "System.Security.Cryptography.Native.Apple";
#endif
		}
	}

	// Unix
	internal static partial class Interop
	{
		internal static partial class Libraries
		{
			// Shims
			internal const string SystemNative = "__Internal";
#if false
			internal const string HttpNative = "System.Net.Http.Native";
			internal const string NetSecurityNative = "System.Net.Security.Native";
			internal const string CryptoNative = "System.Security.Cryptography.Native.OpenSsl";
			internal const string GlobalizationNative = "System.Globalization.Native";
			internal const string CompressionNative = "System.IO.Compression.Native";
#endif
		}
	}

}
