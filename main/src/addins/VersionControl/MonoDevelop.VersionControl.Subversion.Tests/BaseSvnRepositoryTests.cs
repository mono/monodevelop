//
// RepositoryTests.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

using MonoDevelop.Core;
using MonoDevelop.VersionControl.Tests;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace MonoDevelop.VersionControl.Subversion.Tests
{
	[TestFixture]
	public abstract class BaseSvnUtilsTest : BaseRepoUtilsTest
	{
		protected Process svnServe = null;

		[SetUp]
		public override void Setup ()
		{
			Process svnAdmin;
			ProcessStartInfo info;

			// Generate directories and a svn util.
			RootCheckout = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);

			// Create repo in "repo".
			svnAdmin = new Process ();
			info = new ProcessStartInfo ();
			info.FileName = "svnadmin";
			info.Arguments = "create " + RootUrl + Path.DirectorySeparatorChar + "repo";
			info.WindowStyle = ProcessWindowStyle.Hidden;
			svnAdmin.StartInfo = info;
			svnAdmin.Start ();
			svnAdmin.WaitForExit ();

			// Create host (Win32)
			// This needs to be done after doing the svnAdmin creation.
			// And before checkout.
			if (svnServe != null) {
				info = new ProcessStartInfo ();
				info.FileName = "svnserve";
				info.Arguments = "-dr " + RootUrl;
				info.WindowStyle = ProcessWindowStyle.Hidden;
				svnServe.StartInfo = info;
				svnServe.Start ();

				// Create user to auth.
				using (var perm = File. CreateText (RootUrl + Path.DirectorySeparatorChar + "repo" +
				                                    Path.DirectorySeparatorChar + "conf" + Path.DirectorySeparatorChar + "svnserve.conf")) {
					perm.WriteLine ("[general]");
					perm.WriteLine ("anon-access = write");
					perm.WriteLine ("[sasl]");
				}
			}

			// Check out the repository.
			Checkout (RootCheckout, RepoLocation);
			Repo = GetRepo (RootCheckout, RepoLocation);
			DotDir = ".svn";
		}

		[Test]
		[Ignore ("Subversion fails to revert special kind revisions.")]
		public override void RevertsRevision ()
		{
		}

		protected override NUnit.Framework.Constraints.IResolveConstraint IsCorrectType ()
		{
			return Is.InstanceOf<SubversionRepository> ();
		}

		protected override Revision GetHeadRevision ()
		{
			return SvnRevision.Head;
		}

		protected override void BlameExtraInternals (Annotation [] annotations)
		{
			for (int i = 0; i < 2; i++) {
				Assert.IsFalse (annotations [i].HasEmail);
				// Subversion for Unix gives an author.
//				Assert.IsNull (annotations [i].Author);
				Assert.IsNull (annotations [i].Email);
			}
			Assert.IsFalse (annotations [2].HasEmail);
			Assert.IsFalse (annotations [2].HasDate);
		}
	}
}

