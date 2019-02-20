//
// SdkResolverTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Linq;
using Microsoft.Build.Framework;
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class SdkResolverTests : TestBase
	{
		/// <summary>
		/// Tests that the MSBuild version is passed to sdk resolvers. The DotNetMSBuildSdkResolver throws an
		/// ArgumentNullException if it is not set. Have to use an unknown sdk otherwise other resolvers will
		/// be used and the DotNetMSBuildSdkResolver will not be used.
		/// </summary>
		[Test]
		public void UnknownSdk_DotNetMSBuildSdkResolverDoesNotFatalReportError ()
		{
			var resolution = SdkResolution.GetResolver (Runtime.SystemAssemblyService.CurrentRuntime);
			// Using a sdk with an invalid version to prevent the NuGet sdk resolver causing a test crash.
			// Invalid version numbers cause the NuGet sdk resolver to not try to resolve the sdk. The crash
			// only seems to happen with this test - using the IDE does not trigger the crash. There is a
			// separate NuGet sdk resolver test that runs the resolver. Crash error:
			// NuGet.Configuration.NuGetPathContext doesn't implement interface NuGet.Common.INuGetPathContext
			var sdkReference = new SdkReference ("MonoDevelop.Unknown.Test.Sdk", "InvalidVersion", null);
			var logger = new TestLoggingService ();
			var context = new MSBuildContext ();
			var result = resolution.GetSdkPath (sdkReference, logger, context, null, null);

			var error = logger.FatalBuildErrors.FirstOrDefault ();
			Assert.AreEqual (0, logger.FatalBuildErrors.Count, "First error: " + error);
			Assert.IsNull (result);
		}

		[Test]
		public void SdkReference_DifferentCase_StillFindsMatchWithMonoDevelopResolver ()
		{
			var sdks = new List<SdkInfo> ();
			var info = new SdkInfo ("MonoDevelop.Test.NET.Sdk", SdkVersion.Parse ("1.2.3"), null);
			sdks.Add (info);

			var resolver = new Resolver (() => sdks);

			var sdkReference = new SdkReference ("monodevelop.test.net.sdk", null, null);
			var factory = new SdkResolution.SdkResultFactoryImpl (sdkReference);
			var result = resolver.Resolve (sdkReference, null, factory);

			Assert.IsTrue (result.Success);
			Assert.AreEqual (result.Version, info.Version.ToString ());
		}

		class TestLoggingService : ILoggingService
		{
			public List<Exception> FatalBuildErrors = new List<Exception> ();

			public void LogCommentFromText (MSBuildContext buildEventContext, MessageImportance messageImportance, string message)
			{
			}

			public void LogErrorFromText (MSBuildContext buildEventContext, object subcategoryResourceName, object errorCode, object helpKeyword, string file, string message)
			{
			}

			public void LogFatalBuildError (MSBuildContext buildEventContext, Exception e, string projectFile)
			{
				FatalBuildErrors.Add (e);
			}

			public void LogWarning (string message)
			{
			}

			public void LogWarningFromText (MSBuildContext bec, object p1, object p2, object p3, string projectFile, string warning)
			{
			}
		}
	}
}
