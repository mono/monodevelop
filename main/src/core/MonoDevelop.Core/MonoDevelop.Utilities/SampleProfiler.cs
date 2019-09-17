//
// SampleProfiler.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
#nullable enable

using System;
using System.Diagnostics;
using MonoDevelop.Core;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Utilities
{
	public class SampleProfiler
	{
		readonly Func<string> getOutputPath;
		public SampleProfiler (ConfigurationProperty<string> option)
		{
			getOutputPath = () => option.Value;
		}

		public SampleProfiler (string outputPath)
		{
			getOutputPath = () => outputPath;
		}

		public bool ToggleProfilingChecked => sampleProcessPid != -1;

		int sampleProcessPid = -1;
		public void ToggleProfiling (bool spinDump)
		{
			if (sampleProcessPid != -1) {
				Mono.Unix.Native.Syscall.kill (sampleProcessPid, Mono.Unix.Native.Signum.SIGINT);
				sampleProcessPid = -1;
				return;
			}

			sampleProcessPid = Profile (spinDump, 10000).Id;
		}

		public Process Profile (bool spinDump, int seconds)
		{
			var outputFilePath = Path.GetTempFileName ();
			var psi = spinDump ? GetSpinDumpStartInfo (seconds, outputFilePath) : GetSampleStartInfo (seconds, outputFilePath);

			var sampleProcess = Process.Start (psi);

			sampleProcess.EnableRaisingEvents = true;
			sampleProcess.Exited += (sender, args) => {
				if (spinDump && sampleProcess.ExitCode != 0) {
					const string errorMessage = "Administrative privileges required: spindump profiler is intended as a diagnostic tool. To enable spindump profiler handling, add the required sudoers entry";
					LoggingService.LogError (errorMessage);
					return;
				}
				ConvertJITAddressesToMethodNames (getOutputPath (), outputFilePath, "Profile");
			};
			return sampleProcess;
		}

		ProcessStartInfo GetSampleStartInfo (int seconds, string outputFilePath)
			=>  new ProcessStartInfo ("sample") {
				UseShellExecute = false,
				Arguments = $"{Process.GetCurrentProcess ().Id} {seconds} -file {outputFilePath}"
			};

		ProcessStartInfo GetSpinDumpStartInfo (int seconds, string outputFilePath)
		{
			const int millisBetweenSamples = 1;

			if (!Platform.IsMac)
				throw new InvalidOperationException ("Spindump is only available on macOS");

			// We need to delete the file before using it as an output target, otherwise it will error.
			File.Delete (outputFilePath);

			return new ProcessStartInfo ("sudo") {
				UseShellExecute = false,
				// Some weird things happen when using -o, so write to stdout and manually pipe the text
				Arguments = $"-n spindump {Process.GetCurrentProcess ().Id} {seconds} {millisBetweenSamples} -noBinary -onlyRunnable -onlyTarget -o {outputFilePath}",
				RedirectStandardOutput = true,
			};
		}

		[DllImport ("__Internal")]
		extern static string mono_pmip (long offset);
		readonly static Dictionary<long, string?> methodsCache = new Dictionary<long, string?> ();

		public static void ConvertJITAddressesToMethodNames (string outputPath, string fileName, string profilingType)
		{
			var matchRegexes = new Regex [] {
				// sample line output
				// ???  (in <unknown binary>)  [0x103648455]
				new Regex (@"\?\?\?  \(in <unknown binary>\)  \[0x([0-9a-f]+)\]", RegexOptions.Compiled),
				// spindump line output
				new Regex (@"\?\?\? \(.* \+ \d+\) \[0x([0-9a-f]+)\]", RegexOptions.Compiled),
				new Regex (@"\?\?\? \[0x([0-9a-f]+)\]", RegexOptions.Compiled),
			};

			// When using spindump, this format means kernel code, so don't bother writing it, we have a toplevel usercode function
			if (File.Exists (fileName) && new FileInfo (fileName).Length > 0) {
				Directory.CreateDirectory (outputPath);
				var outputFilename = Path.Combine (outputPath, $"{BrandingService.ApplicationName}_{profilingType}_{DateTime.Now:yyyy-MM-dd__HH-mm-ss}.txt");

				using (var sr = new StreamReader (fileName))
				using (var sw = new StreamWriter (outputFilename)) {
					string line;
					while ((line = sr.ReadLine ()) != null) {
						bool printLine = true;

						foreach (var rx in matchRegexes) {
							try {
								var match = rx.Match (line);
								if (match.Success) {
									var offset = long.Parse (match.Groups [1].Value, NumberStyles.HexNumber);
									if (offset < 0) {
										// This is kernel code, no use writing redundant stack frames.
										printLine = false;
										break;
									}

									var pmipMethodName = GetSymbolicatedLine (offset);
									if (pmipMethodName != null) {
										line = line
											.Remove (match.Index, match.Length)
											.Insert (match.Index, pmipMethodName);
									}

									// Stop processing other regexes, we have a match
									break;
								}
							} catch (Exception e) {
								LoggingService.LogError ($"Failed to parse address sample output line {line}", e);
							}
						}

						if (printLine)
							sw.WriteLine (line);
					}
				}
			}

			static string? GetSymbolicatedLine (long offset)
			{
				if (!methodsCache.TryGetValue (offset, out string? pmipMethodName)) {
					pmipMethodName = mono_pmip (offset)?.TrimStart ();
					if (pmipMethodName != null)
						pmipMethodName = PmipParser.ToSample (pmipMethodName, offset);
					methodsCache.Add (offset, pmipMethodName);
				}

				return pmipMethodName;
			}
		}

		static class PmipParser
		{
			// pmip output:
			// (wrapper managed-to-native) Gtk.Application:gtk_main () [{0x7f968e48d1e8} + 0xdf]  (0x122577d50 0x122577f28) [0x7f9682702c90 - MonoDevelop.exe]
			// MonoDevelop.Startup.MonoDevelopMain:Main (string[]) [{0x7faef5700948} + 0x93] [/Users/therzok/Work/md/monodevelop/main/src/core/MonoDevelop.Startup/MonoDevelop.Startup/MonoDevelopMain.cs :: 39u] (0x10e7609c0 0x10e760aa8) [0x7faef7002d80 - MonoDevelop.exe]

			// sample symbolified line:
			// start  (in libdyld.dylib) + 1  [0x7fff79c7ded9]
			// mono_hit_runtime_invoke  (in mono64) + 1619  [0x102f90083]  mini-runtime.c:3148
			public static string ToSample (string initialInput, long offset)
			{
				try {
					var input = initialInput.AsSpan ();
					var sb = new StringBuilder ();

					// Cut off wrapper part.
					if (input.StartsWith ("(wrapper".AsSpan ())) {
						input = input.Slice (input.IndexOf (')') + 1).TrimStart ();
					}

					// If it starts with <Module>:, trim it.
					if (input.StartsWith ("<Module>:".AsSpan ())) {
						input = input.Slice ("<Module>:".Length);
					}

					// Usually a generic trampoline marker, don't bother parsing.
					if (input[0] == '<')
						return input.ToString ();

					// Decode method signature
					// Gtk.Application:gtk_main () [{0x7f968e48d1e8} + 0xdf]
					var endMethodSignature = input.IndexOf ('{');
					var methodSignature = input.Slice (0, endMethodSignature - 2); // " ["
					input = input.Slice (endMethodSignature + 1).TrimStart ();

					// Append chars, escaping what might be unreadable by instruments.
					for (int i = 0; i < methodSignature.Length; ++i) {
						var ch = methodSignature[i];
						if (ch == ' ')
							continue;

						if (ch == ':') {
							sb.Append ("::");
							continue;
						}

						if (ch == '.') {
							sb.Append ("_");
							continue;
						}

						if (ch == '[' && methodSignature[i + 1] == ']') {
							sb.Append ("*");
							i++;
							continue;
						}

						sb.Append (ch);
					}

					// Add some data to match format, + 0 is because it doesn't matter, we're not looking at native code.
					sb.Append ("  (in MonoDevelop.exe) + 0  [");
					sb.AppendFormat ("0x{0:x}", offset);
					sb.Append ("]");

					// Skip the rest of the block(s) after the method signature until we get a path.
					input = input.Slice (input.IndexOf ('[') + 1).TrimStart ();

					string? filename = null;
					if (input[0] == '/') {
						// We have a filename
						var end = input.IndexOf (']');
						var filepath = input.Slice (0, end - 1).Trim (); // trim u
						filename = filepath.ToString ();
					}

					if (filename != null) {
						sb.Append ("  ");
						sb.Append (filename);
					}

					return sb.ToString ();
				} catch (Exception e) {
					LoggingService.LogInternalError ($"Failed to parse line '{initialInput}'", e);
					return initialInput;
				}
			}
		}
	}
}
