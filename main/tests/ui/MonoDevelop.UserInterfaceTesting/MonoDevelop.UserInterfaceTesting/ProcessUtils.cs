//
// ProcessUtils.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
//       Jérémie Laval <jeremie.laval@xamarin.com>
//
// Copyright (c) 2012 Xamarin, Inc.
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
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.UserInterfaceTesting
{
	public static class ProcessUtils
	{
		public static Task<int> StartProcess (ProcessStartInfo psi, TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<int> ();
			if (cancellationToken.CanBeCanceled && cancellationToken.IsCancellationRequested) {
				tcs.TrySetCanceled ();
				return tcs.Task;
			}

			psi.UseShellExecute = false;
			psi.RedirectStandardOutput |= stdout != null;
			psi.RedirectStandardError |= stderr != null;

			var p = Process.Start (psi);

			if (cancellationToken.CanBeCanceled) {
				cancellationToken.Register (() => {
					try {
						if (!p.HasExited) {
							p.Kill ();
						}
					} catch (InvalidOperationException ex) {
						if (ex.Message.IndexOf ("already exited", StringComparison.Ordinal) < 0)
							throw;
					}
				});
			}

			bool outputDone = false;
			bool errorDone = false;
			bool exitDone = false;

			p.EnableRaisingEvents = true;
			if (psi.RedirectStandardOutput) {
				bool stdOutInitialized = false;
				p.OutputDataReceived += (sender, e) => {
					try {
						if (e.Data == null) {
							outputDone = true;
							if (exitDone && errorDone)
								tcs.TrySetResult (p.ExitCode);
							return;
						}

						if (stdOutInitialized)
							stdout.WriteLine ();
						stdout.Write (e.Data);
						stdOutInitialized = true;
					} catch (Exception ex) {
						tcs.TrySetException (ex);
					}
				};
				p.BeginOutputReadLine ();
			} else {
				outputDone = true;
			}

			if (psi.RedirectStandardError) {
				bool stdErrInitialized = false;
				p.ErrorDataReceived += (sender, e) => {
					try {
						if (e.Data == null) {
							errorDone = true;
							if (exitDone && outputDone)
								tcs.TrySetResult (p.ExitCode);
							return;
						}

						if (stdErrInitialized)
							stderr.WriteLine ();
						stderr.Write (e.Data);
						stdErrInitialized = true;
					} catch (Exception ex) {
						tcs.TrySetException (ex);
					}
				};
				p.BeginErrorReadLine ();
			} else {
				errorDone = true;
			}

			p.Exited += (sender, e) => {
				exitDone = true;
				if (errorDone && outputDone)
					tcs.TrySetResult (p.ExitCode);
			};

			return tcs.Task;
		}
	}
}
