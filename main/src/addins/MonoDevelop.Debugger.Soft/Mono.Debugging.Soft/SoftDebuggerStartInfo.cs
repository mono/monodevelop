// 
// SoftDebuggerStartInfo.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Mono.Debugging.Client;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;

namespace Mono.Debugging.Soft
{
	public class SoftDebuggerStartInfo : DebuggerStartInfo
	{
		public SoftDebuggerStartInfo (string monoRuntimePrefix, Dictionary<string,string> monoRuntimeEnvironmentVariables)
			: this (new SoftDebuggerLaunchArgs (monoRuntimePrefix, monoRuntimeEnvironmentVariables))
		{
		}
		
		public SoftDebuggerStartInfo (SoftDebuggerStartArgs startArgs)
		{
			if (startArgs == null)
				throw new ArgumentNullException ("startArgs");
			this.StartArgs = startArgs;
		}
		
		/// <summary>
		/// Names of assemblies that are user code.
		/// </summary>
		public List<AssemblyName> UserAssemblyNames { get; set; }
		
		/// <summary>
		/// The session will output this to the debug log as soon as it starts. It can be used to log warnings from
		/// creating the SoftDebuggerStartInfo
		/// </summary>
		public string LogMessage { get; set; }
		
		/// <summary>
		/// Args for starting the debugger connection.
		/// </summary>
		public SoftDebuggerStartArgs StartArgs { get; set; }
	}
	
	public abstract class SoftDebuggerStartArgs
	{
	}
	
	public abstract class SoftDebuggerRemoteArgs : SoftDebuggerStartArgs
	{
		public SoftDebuggerRemoteArgs (string appName, IPAddress address, int debugPort, int outputPort)
		{
			if (address == null)
				throw new ArgumentNullException ("address");
			if (debugPort < 0)
				throw new ArgumentException ("Debug port cannot be less than zero", "debugPort");
			
			this.AppName = appName;
			this.Address = address;
			this.DebugPort = debugPort;
			this.OutputPort = outputPort;
		}
		
		/// <summary>
		/// The IP address for the connection.
		/// </summary>
		public IPAddress Address { get; private set; }
		
		/// <summary>
		/// Port for the debugger connection. Zero means random port.
		/// </summary>
		public int DebugPort { get; private set; }
		
		/// <summary>
		/// Port for the console connection. Zero means random port, less than zero means that output is not redirected.
		/// </summary>
		public int OutputPort { get; private set; }
		
		/// <summary>
		/// Application name that will be shown in the debugger.
		/// </summary>
		public string AppName { get; private set; }
		
		public bool RedirectOutput { get { return OutputPort >= 0; } }
	}
	
	/// <summary>
	/// Args for the debugger to listen for an incoming connection from a debuggee.
	/// </summary>
	public sealed class SoftDebuggerListenArgs : SoftDebuggerRemoteArgs
	{
		public SoftDebuggerListenArgs (string appName, IPAddress address, int debugPort)
			: this (appName, address, debugPort, -1) {}
		
		public SoftDebuggerListenArgs (string appName, IPAddress address, int debugPort, int outputPort)
			: base (appName, address, debugPort, outputPort)
		{
		}
	}
	
	/// <summary>
	/// Args for the debugger to connect to target that is listening.
	/// </summary>
	public sealed class SoftDebuggerConnectArgs : SoftDebuggerRemoteArgs
	{
		public SoftDebuggerConnectArgs (string appName, IPAddress address, int debugPort)
			: this (appName, address, debugPort, -1) {}
		
		public SoftDebuggerConnectArgs (string appName, IPAddress address, int debugPort, int outputPort)
			: base (appName, address, debugPort, outputPort)
		{
			if (debugPort == 0)
				throw new ArgumentException ("Debug port cannot be zero when connecting", "debugPort");
			if (outputPort == 0)
				throw new ArgumentException ("Output port cannot be zero when connecting", "outputPort");
			
			MaxConnectionAttempts = 1;
			TimeBetweenConnectionAttempts = 500;
		}
		
		/// <summary>
		/// Maximum number of connection attempts. Zero or less means infinite attempts.
		/// </summary>
		public int MaxConnectionAttempts { get; set; }
		
		/// <summary>
		/// Tthe time between connection attempts, in milliseconds.
		/// </summary>
		public int TimeBetweenConnectionAttempts { get; set; }
	}
	
	/// <summary>
	/// Options for the debugger to start a process directly.
	/// </summary>
	public sealed class SoftDebuggerLaunchArgs : SoftDebuggerStartArgs
	{
		public SoftDebuggerLaunchArgs (string monoRuntimePrefix, Dictionary<string,string> monoRuntimeEnvironmentVariables)
		{
			if (string.IsNullOrEmpty (monoRuntimePrefix))
				throw new ArgumentException ("monoRuntimePrefix");
			
			this.MonoRuntimePrefix = monoRuntimePrefix;
			this.MonoRuntimeEnvironmentVariables = monoRuntimeEnvironmentVariables;
		}
		
		/// <summary>
		/// Prefix into which the target Mono runtime is installed.
		/// </summary>
		public string MonoRuntimePrefix { get; private set; }
		
		/// <summary>
		/// Environment variables for the Mono runtime.
		/// </summary>
		public Dictionary<string,string> MonoRuntimeEnvironmentVariables { get; private set; }
		
		/// <summary>
		/// Launcher for the external console. May be null if the app does not run on an external console.
		/// </summary>
		public Mono.Debugger.Soft.LaunchOptions.TargetProcessLauncher ExternalConsoleLauncher { get; set; }
	}
}

