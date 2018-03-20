//
// NuGetOptions.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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

namespace MonoDevelop.UserInterfaceTesting.Controllers
{
	public class NuGetPackageOptions
	{
		public NuGetPackageOptions ()
		{
			RetryCount = 3;
		}

		public string PackageName { get; set;}

		public string Version { get; set;}

		public bool IsPreRelease { get; set;}

		public int RetryCount { get; set;}

		public override string ToString ()
		{
			return string.Format ("PackageName={0}, Version={1}, IsPreRelease={2}, RetryCount={3}",
				PackageName, Version, IsPreRelease, RetryCount);
		}
	}

	public enum NuGetOperations
	{
		Add,
		Remove,
		Update
	}

	public class WaitForNuGet
	{
		public WaitForNuGet ()
		{
			TimeOutSeconds = 180;
			PollStepSeconds = 1;
		}

		public NuGetOperations Operation { get; set;}

		public string PackageName { get; set;}

		public bool WaitForSuccess { get; set;}

		public bool WaitForWarning { get; set;}

		public bool WaitForError { get; set;}

		public int TimeOutSeconds  { get; set;}

		public int PollStepSeconds  { get; set;}

		public override string ToString ()
		{
			return string.Format ("Operation={0}, PackageName={1}, WaitForSuccess={2}, WaitForWarning={3}, WaitForError={4}, TimeOutSeconds={5}, PollStepSeconds={6}",
				Operation, PackageName, WaitForSuccess, WaitForWarning, WaitForError, TimeOutSeconds, PollStepSeconds);
		}

		public static void UpdateSuccess (string packageName, bool waitForWarning = true, UITestBase testContext = null)
		{
			Success (packageName, NuGetOperations.Update, waitForWarning, testContext);
		}

		public static void AddSuccess (string packageName, bool waitForWarning = true, UITestBase testContext = null)
		{
			Success (packageName, NuGetOperations.Add, waitForWarning, testContext);
		}

		public static void Success (string packageName, NuGetOperations operation, bool waitForWarning = true, UITestBase testContext = null)
		{
			var waitPackage = new WaitForNuGet {
				Operation = operation,
				PackageName = packageName,
				WaitForSuccess = true,
				WaitForWarning = waitForWarning
			};
			if (testContext != null) {
				testContext.ReproStep (string.Format ("Wait for one of these messages:\n\t{0}",
					string.Join ("\t\n", waitPackage.ToMessages ())));
			}
			waitPackage.Wait ();
		}

		public void Wait ()
		{
			Ide.WaitForStatusMessage (ToMessages (), TimeOutSeconds, PollStepSeconds);
		}

		public string [] ToMessages ()
		{
			if ((WaitForSuccess | WaitForWarning | WaitForError) == false)
				throw new ArgumentException ("Atleast one of the 'WaitForSuccess', 'WaitForWarning', 'WaitForError' needs to be true");

			List<string> waitForMessages = new List<string> ();

			if (WaitForSuccess) {
				if (Operation == NuGetOperations.Add) {
					waitForMessages.Add (string.Format ("{0} successfully added.", PackageName));
					waitForMessages.Add ("Packages successfully added.");
					waitForMessages.Add ("packages successfully added.");
				}
				if (Operation == NuGetOperations.Update) {
					waitForMessages.Add (string.Format ("{0} is up to date.", PackageName));
					waitForMessages.Add (string.Format ("{0} successfully updated.", PackageName));
					waitForMessages.Add ("Packages successfully updated.");
					waitForMessages.Add ("packages successfully updated.");
					waitForMessages.Add ("successfully updated.");
					waitForMessages.Add ("Packages are up to date.");
				}
				if (Operation == NuGetOperations.Remove) {
					waitForMessages.Add (string.Format ("{0} successfully removed.", PackageName));
				}
			}

			if (WaitForWarning) {
				if (Operation == NuGetOperations.Add) {
					waitForMessages.Add (string.Format ("{0} added with warnings.", PackageName));
					waitForMessages.Add ("Packages added with warnings.");
					waitForMessages.Add ("packages added with warnings.");
				}
				if (Operation == NuGetOperations.Update) {
					waitForMessages.Add (string.Format ("{0} updated with warnings.", PackageName));
					waitForMessages.Add ("Packages updated with warnings.");
					waitForMessages.Add ("packages updated with warnings.");
					waitForMessages.Add ("No update found but warnings were reported.");
					waitForMessages.Add ("No updates found but warnings were reported.");
				}
				if (Operation == NuGetOperations.Remove) {
					waitForMessages.Add (string.Format ("{0} removed with warnings.", PackageName));
				}
			}

			if (WaitForError) {
				if (Operation == NuGetOperations.Add) {
					waitForMessages.Add (string.Format ("Could not add {0}.", PackageName));
					waitForMessages.Add ("Could not add packages.");
				}
				if (Operation == NuGetOperations.Update) {
					waitForMessages.Add (string.Format ("Could not update {0}.", PackageName));
					waitForMessages.Add ("Could not update packages.");
				}
				if (Operation == NuGetOperations.Remove) {
					waitForMessages.Add (string.Format ("Could not remove {0}.", PackageName));
				}
			}
			return waitForMessages.ToArray ();
		}
	}
}

