// 
// AndroidToolbox.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.MonoDroid
{
	public class AndroidToolbox
	{
		FilePath androidToolsPath, javaBinPath;
		FilePath androidPlatformToolsPath;
		string pathOverride;
		
		public AndroidToolbox (FilePath androidPath, FilePath javaBinPath)
		{
			this.androidToolsPath = androidPath.Combine ("tools");
			this.androidPlatformToolsPath = androidPath.Combine ("platform-tools");
			this.javaBinPath = javaBinPath;
			
			pathOverride = Environment.GetEnvironmentVariable ("PATH");
			if (string.IsNullOrEmpty (pathOverride))
				pathOverride = javaBinPath;
			else
				pathOverride = javaBinPath + Path.PathSeparator + pathOverride;
		}
		
		public string AdbExe {
			get {
				return androidPlatformToolsPath.Combine (PropertyService.IsWindows? "adb.exe" : "adb");
			}
		}
		
		public string AndroidExe {
			get {
				return androidToolsPath.Combine (PropertyService.IsWindows? "android.bat" : "android");
			}
		}
		
		public string EmulatorExe {
			get {
				return androidToolsPath.Combine (PropertyService.IsWindows? "emulator.exe" : "emulator");
			}
		}
		
		public string JarsignerExe {
			get {
				return javaBinPath.Combine (PropertyService.IsWindows? "jarsigner.exe" : "jarsigner");
			}
		}
		
		public string KeytoolExe {
			get {
				return javaBinPath.Combine (PropertyService.IsWindows? "keytool.exe" : "keytool");
			}
		}
		
		public IProcessAsyncOperation SignPackage (AndroidSigningOptions options, string unsignedApk,
			string signedApk, TextWriter outputLog, TextWriter errorLog)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-keystore");
			args.AddQuoted (options.KeyStore);
			args.Add ("-storepass");
			args.AddQuoted (options.StorePass);
			args.Add ("-keypass");
			args.AddQuoted (options.KeyPass);
			args.Add ("-signedjar");
			args.AddQuoted (signedApk, unsignedApk, options.KeyAlias);
			
			return StartProcess (JarsignerExe, args.ToString (), outputLog, errorLog);
		}
		
		//string dname = "CN=Android Debug,O=Android,C=US";
		public IProcessAsyncOperation Genkeypair (AndroidSigningOptions options, string dname,
			TextWriter outputLog, TextWriter errorLog)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-genkeypair");
			args.Add ("-alias");
			args.AddQuoted (options.KeyAlias);
			args.Add ("-dname");
			args.AddQuoted (dname);
			args.Add ("-storepass");
			args.AddQuoted (options.StorePass);
			args.Add ("-keypass");
			args.AddQuoted (options.KeyPass);
			args.Add ("-keystore");
			args.AddQuoted (options.KeyStore);
			
			return StartProcess (KeytoolExe, args.ToString (), outputLog, errorLog);
		}
		
		ProcessWrapper StartProcess (string name, string args, TextWriter outputLog, TextWriter errorLog)
		{
			var psi = new ProcessStartInfo (name, args) {
				UseShellExecute = false,
			};
			if (outputLog != null)
				psi.RedirectStandardOutput = true;
			if (errorLog != null)
				psi.RedirectStandardError = true;
			psi.EnvironmentVariables["PATH"] = pathOverride;
			return Runtime.ProcessService.StartProcess (psi, outputLog, outputLog, null);
		}
		
		ProcessWrapper StartProcess (string name, string args, ProcessEventHandler outputLog, ProcessEventHandler errorLog)
		{
			var psi = new ProcessStartInfo (name, args) {
				UseShellExecute = false,
			};
			if (outputLog != null)
				psi.RedirectStandardOutput = true;
			if (errorLog != null)
				psi.RedirectStandardError = true;
			psi.EnvironmentVariables["PATH"] = pathOverride;
			return Runtime.ProcessService.StartProcess (psi, outputLog, outputLog, null);
		}
		
		public IProcessAsyncOperation EnsureServerRunning (TextWriter outputLog, TextWriter errorLog)
		{
			return StartProcess (AdbExe, "start-server", outputLog, errorLog);
		}
		
		public StartAvdOperation StartAvd (AndroidVirtualDevice avd)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-partition-size", "512", "-avd");
			args.AddQuoted (avd.Name);
			args.Add ("-prop");
			args.AddQuoted ("monodroid.avdname=" + avd.Name);

			var error = new StringWriter ();
			var process = StartProcess (EmulatorExe, args.ToString (), null, error);
			return new StartAvdOperation (process, error);
		}
		
		public ProcessWrapper StartAvdManager ()
		{
			//FIXME: don't create multiple instances. if it's running, focus it.
			//and kill it when quitting MD, or it will keep MD open
			return Runtime.ProcessService.StartProcess (AndroidExe, "", null, (TextWriter)null, null, null);
		}

		public GetDateOperation GetDeviceDate (AndroidDevice device)
		{
			var args = String.Format ("-s {0} shell date +%s", device.ID);
			var sw = new StringWriter ();
			return new GetDateOperation (StartProcess (AdbExe, args, sw, sw), sw);
		}
		
		public AdbOutputOperation SetProperty (AndroidDevice device, string property, string value)
		{
			var sw = new StringWriter ();
			return new AdbOutputOperation (SetProperty (device, property, value, sw, sw), sw);
		}
		
		public IProcessAsyncOperation SetProperty (AndroidDevice device, string property, string value, TextWriter outputLog, TextWriter errorLog)
		{
			if (property == null)
				throw new ArgumentNullException ("property");
			if (value == null)
				throw new ArgumentNullException ("value");
			
			var args = new ProcessArgumentBuilder ();
			args.Add ("-s", device.ID, "shell", "setprop");
			args.AddQuoted (property, value);
			
			return StartProcess (AdbExe, args.ToString (), outputLog, errorLog);
		}
		
		public IProcessAsyncOperation PushFile (AndroidDevice device, string source, string destination,
			TextWriter outputLog, TextWriter errorLog)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-s", device.ID, "push");
			args.AddQuoted (source, destination);
			
			return StartProcess (AdbExe, args.ToString (), outputLog, errorLog);
		}

		public IProcessAsyncOperation WaitForDevice (AndroidDevice device, TextWriter outputLog, TextWriter errorLog)
		{
			var args = string.Format ("-s {0} wait-for-device", device.ID);
			return StartProcess (AdbExe, args, outputLog, errorLog);
		}

		public IProcessAsyncOperation PullFile (AndroidDevice device, string source, string destination,
			TextWriter outputLog, TextWriter errorLog)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-s", device.ID, "pull");
			args.AddQuoted (source, destination);
			
			return StartProcess (AdbExe, args.ToString (), outputLog, errorLog);
		}

		public InstallPackageOperation Install (AndroidDevice device, string package, TextWriter outputLog, TextWriter errorLog)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-s", device.ID, "install");
			args.AddQuoted (package);
			
			var errorCapture = new StringWriter ();
			var errorWriter = TeeTextWriter.ForNonNull (errorCapture, errorCapture);
			return new InstallPackageOperation (StartProcess (AdbExe, args.ToString (), outputLog, errorWriter), errorCapture);
		}

		public IProcessAsyncOperation Uninstall (AndroidDevice device, string package, TextWriter outputLog, TextWriter errorLog)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-s", device.ID, "uninstall");
			args.AddQuoted (package);
			
			return StartProcess (AdbExe, args.ToString (), outputLog, errorLog);
		}
		
		public IProcessAsyncOperation StartActivity (AndroidDevice device, string activity,
			TextWriter outputLog, TextWriter errorLog)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-s", device.ID, "shell", "am", "start", "-a", "android.intent.action.MAIN", "-n");
			args.AddQuoted (activity);
			
			return StartProcess (AdbExe, args.ToString (), outputLog, errorLog);
		}
		
		public IProcessAsyncOperation StartActivity (AndroidDevice device, string activity,
			ProcessEventHandler outputLog, ProcessEventHandler errorLog)
		{
			var args = new ProcessArgumentBuilder ();
			args.Add ("-s", device.ID, "shell", "am", "start", "-a", "android.intent.action.MAIN", "-n");
			args.AddQuoted (activity);
			
			return StartProcess (AdbExe, args.ToString (), outputLog, errorLog);
		}

		public IProcessAsyncOperation ForwardPort (AndroidDevice device, int devicePort, int localPort,
			ProcessEventHandler outputLog, ProcessEventHandler errorLog)
		{
			var args = string.Format ("-s {0} forward tcp:{1} tcp:{2}", device.ID, localPort, devicePort);
			return StartProcess (AdbExe, args, outputLog, errorLog);
		}
		
		public ProcessWrapper LogCat (AndroidDevice device, ProcessEventHandler outputLog,
			ProcessEventHandler errorLog)
		{
			var args = string.Format ("-s {0} logcat", device.ID);
			return StartProcess (AdbExe, args, outputLog, errorLog);
		}

		public bool IsSharedRuntimeInstalled (List<string> packages)
		{
			return packages.Contains ("com.novell.monodroid.runtimeservice");
		}
		
		public class AdbOutputOperation : WrapperOperation
		{
			StringWriter output;
			IProcessAsyncOperation process;
			
			public AdbOutputOperation (IProcessAsyncOperation process, StringWriter output)
			{
				this.process = process;
				this.output = output;
			}
			
			protected override IAsyncOperation Wrapped {
				get { return process; }
			}
			
			public string GetOutput ()
			{
				return output.ToString ();
			}
		}
		
		public class GetDateOperation : AdbOutputOperation
		{
			long? date;
			
			public GetDateOperation (IProcessAsyncOperation process, StringWriter output) : base (process, output)
			{
			}
			
			public override bool Success {
				get {
					if (!base.Success)
						return false;
					if (!date.HasValue) {
						long value;
						date = long.TryParse (GetOutput (), out value)? value : -1;
					}
					return date >= 0;
				}
			}
			
			public long Date {
				get {
					if (!Success)
						throw new InvalidOperationException ("Error getting date from device:\n" + GetOutput ());
					//Success will have parsed the value 
					return date.Value;
				}
			}
		}
		
		public class StartAvdOperation : WrapperOperation
		{
			IProcessAsyncOperation process;
			StringWriter error;
			
			public StartAvdOperation (IProcessAsyncOperation process, StringWriter error)
			{
				this.process = process;
				this.error = error;
			}
			
			protected override IAsyncOperation Wrapped {
				get { return process; }
			}
			
			public override bool Success {
				get {
					return base.Success || error.ToString ().Contains ("used by another emulator");
				}
			}
			
			public string ErrorText {
				get { return error.ToString (); }
			}
		}
		
		public class InstallPackageOperation : WrapperOperation
		{
			IProcessAsyncOperation process;
			StringWriter error;
			
			public InstallPackageOperation (IProcessAsyncOperation process, StringWriter error)
			{
				this.process = process;
				this.error = error;
			}
			
			protected override IAsyncOperation Wrapped {
				get { return process; }
			}
			
			public override bool Success {
				get {
					// adb exit code is 0 with some errors
					return base.Success && !error.ToString ().Contains ("[INSTALL_FAILED");
				}
			}
			
			//FIXME: use this
			public string FriendlyError {
				get {
					string err = error.ToString ();
					if (err.Contains ("[INSTALL_FAILED_INSUFFICIENT_STORAGE]"))
						return "There is not enough storage space on the device to store the package.";
					if (err.Contains ("[INSTALL_FAILED_ALREADY_EXISTS]"))
						return "The package already exists on the device.";
					return null;
				}
			}
		}
	}
	
	public class AndroidSigningOptions
	{
		public string KeyStore { get; set; }
		public string KeyAlias { get; set; }
		public string KeyPass { get; set; }
		public string StorePass { get; set; }
	}
	
	public class AndroidDevice
	{
		public string ID { get; private set; }
		public string State { get; private set; }
		public IDictionary<string,string> Properties { get; internal set; }

		public AndroidDevice (string id, string state)
		{
			ID = id;
			State = state;
		}

		public bool IsEmulator {
			get { return ID.ToLowerInvariant ().StartsWith ("emulator"); }
		}
		
		public bool IsOnline {
			get { return State == "device"; }
		}

		public override string ToString ()
		{
			return string.Format ("Device: {0} [{1}]", ID, State);
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			var other = obj as MonoDevelop.MonoDroid.AndroidDevice;
			return other != null && ID == other.ID && State == other.State && IsEmulator == other.IsEmulator;
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				return (ID != null ? ID.GetHashCode () : 0) 
					^ (State != null ? State.GetHashCode () : 0)
					^ IsEmulator.GetHashCode ();
			}
		}
	}
	
	public abstract class WrapperOperation : IAsyncOperation, IDisposable
	{
		bool disposed;
		
		protected abstract IAsyncOperation Wrapped { get; }
		
		public virtual event OperationHandler Completed {
			add { Wrapped.Completed += value; }
			remove { Wrapped.Completed -= value; }
		}
		
		public virtual void Cancel ()
		{
			Wrapped.Cancel ();
		}
		
		public virtual void WaitForCompleted ()
		{
			Wrapped.WaitForCompleted ();
		}
		
		public virtual bool IsCompleted {
			get { return Wrapped.IsCompleted; }
		}
		
		public virtual bool Success {
			get { return Wrapped.Success; }
		}
		
		public virtual bool SuccessWithWarnings {
			get { return Wrapped.SuccessWithWarnings; }
		}
		
		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			if (!disposing)
				return;
			
			if (Wrapped is IDisposable)
				((IDisposable)Wrapped).Dispose ();
		}
		
		~WrapperOperation ()
		{
			
		}
	}
	
	class TeeTextWriter : TextWriter
	{
		TextWriter [] writers;
		
		public TeeTextWriter (params TextWriter [] writers)
		{
			this.writers = writers;
			foreach (var w in writers)
				if (w.Encoding != writers[0].Encoding)
					throw new ArgumentException ("writers must have same encoding");
		}
		
		public override void Write (string value)
		{
			foreach (var writer in writers)
				writer.Write (value);
		}
		
		public static TextWriter ForNonNull (params TextWriter [] writers)
		{
			var nonNull = writers.Where (w => w != null).ToArray ();
			if (nonNull.Length == 0)
				return null;
			if (nonNull.Length == 1)
				return nonNull[0];
			return new TeeTextWriter (nonNull);
		}
		
		public override System.Text.Encoding Encoding {
			get {
				return writers[0].Encoding;
			}
		}
	}
}

