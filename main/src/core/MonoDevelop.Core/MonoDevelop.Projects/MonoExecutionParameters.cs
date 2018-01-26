// 
// AdvancedMonoParameters.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Text;


namespace MonoDevelop.Projects
{
	public sealed class MonoExecutionParameters
	{
		sealed class EnvVarAttribute : Attribute
		{
			public string Name;
			public string TrueValue = string.Empty;

			public EnvVarAttribute (string name)
			{
				this.Name = name;
			}

			public EnvVarAttribute (string name, string trueValue)
			{
				this.Name = name;
				this.TrueValue = trueValue;
			}
		}

		sealed class MonoArgAttribute : Attribute
		{
			public string Name;

			public MonoArgAttribute (string name)
			{
				Name = name;
			}
		}

		public enum LogLevel
		{
			[MonoArg (null)]
			[LocalizedDescription ("Default")]
			Default,
			[MonoArg ("error")]
			Error,
			[MonoArg ("critical")]
			Critical,
			[MonoArg ("warning")]
			Warning,
			[MonoArg ("message")]
			Message,
			[MonoArg ("info")]
			Info,
			[MonoArg ("debug")]
			Debug
		}

		[Flags]
		public enum LogMask
		{
			[MonoArg (null)]
			None = 0,
			[MonoArg ("asm")]
			AssemblyLoader = 0x01,
			[MonoArg ("type")]
			Type = 0x02,
			[MonoArg ("dll")]
			NativeLibraryLoader = 0x04,
			[MonoArg ("cfg")]
			ConfigFileLoader = 0x08,
			[MonoArg ("gc")]
			GarbageCollector = 0x10,
			[MonoArg ("aot")]
			Aot = 0x20,
			[MonoArg ("all")]
			All = 0xff
		}

		public enum SecurityMode
		{
			[MonoArg (null)]
			[LocalizedDescription ("Disabled")]
			Disabled,
			[MonoArg ("cas")]
			Cas,
			[MonoArg ("core-clr")]
			CoreClr,
			[MonoArg ("verifiable")]
			Verifiable,
			[MonoArg ("validil")]
			ValidIL
		}

		public enum GcType
		{
			[MonoArg (null)]
			[LocalizedDescription ("Default")]
			Default,
			[MonoArg ("boehm")]
			Boehm,
			[MonoArg ("sgen")]
			SGen
		}

		public enum RuntimeArchitecture
		{
			[MonoArg (null)]
			[LocalizedDescription ("Default")]
			Default,
			[LocalizedDescription ("32-bit")]
			[MonoArg ("32")]
			b32,
			[LocalizedDescription ("64-bit")]
			[MonoArg ("64")]
			b64
		}

		static Dictionary<PropertyInfo, ItemPropertyAttribute> itemPropertyAttributes = new Dictionary<PropertyInfo, ItemPropertyAttribute> ();
		static Dictionary<PropertyInfo, MonoArgAttribute> monoArgAttributes = new Dictionary<PropertyInfo, MonoArgAttribute> ();
		static Dictionary<PropertyInfo, EnvVarAttribute> envVarAttributes = new Dictionary<PropertyInfo, EnvVarAttribute> ();
		static Dictionary<PropertyInfo, LocalizedDisplayNameAttribute> localizedDisplayNameAttributes = new Dictionary<PropertyInfo, LocalizedDisplayNameAttribute> ();

		static MonoExecutionParameters ()
		{
			foreach (PropertyInfo prop in typeof(MonoExecutionParameters).GetProperties ()) {
				var ipa = (ItemPropertyAttribute)Attribute.GetCustomAttribute (prop, typeof (ItemPropertyAttribute));
				if (ipa != null)
					itemPropertyAttributes.Add (prop, ipa);

				var maa = (MonoArgAttribute)Attribute.GetCustomAttribute (prop, typeof (MonoArgAttribute));
				if (maa != null)
					monoArgAttributes.Add (prop, maa);

				var eva = (EnvVarAttribute)Attribute.GetCustomAttribute (prop, typeof (EnvVarAttribute));
				if (eva != null)
					envVarAttributes.Add (prop, eva);

				var ldna = (LocalizedDisplayNameAttribute)Attribute.GetCustomAttribute (prop, typeof (LocalizedDisplayNameAttribute));
				if (ldna != null)
					localizedDisplayNameAttributes.Add (prop, ldna);
			}
		}

		public MonoExecutionParameters ()
		{
			ResetProperties ();
		}

		public void ResetProperties ()
		{
			foreach (var kvp in itemPropertyAttributes) {
				var prop = kvp.Key;
				var propAttr = kvp.Value;
				if (propAttr.DefaultValue != null)
					prop.SetValue (this, propAttr.DefaultValue, null);
			}
		}

		public void GenerateOptions (IDictionary<string,string> envVars, out string options)
		{
			StringBuilder ops = new StringBuilder ();
			if (MonoStripDriveLetters || MonoCaseInsensitivePaths) {
				if (MonoStripDriveLetters && MonoCaseInsensitivePaths)
					envVars ["MONO_IOMAP"] = "all";
				else if (MonoStripDriveLetters)
					envVars ["MONO_IOMAP"] = "drive";
				else if (MonoCaseInsensitivePaths)
					envVars ["MONO_IOMAP"] = "case";
			}
			for (int n=0; n< MonoVerboseLevel; n++)
				ops.Append ("-v ");
			
			if (MonoDebugMode || MonoDebugMdbOptimizations || MonoDebugCasts || MonoGdbInfo) {
				ops.Append ("--debug=");
				if (MonoDebugMdbOptimizations)
					ops.Append ("mdb-optimizations,");
				if (MonoDebugCasts)
					ops.Append ("casts,");
				if (MonoGdbInfo)
					ops.Append ("gdb,");
				ops.Remove (ops.Length - 1, 1);
				ops.Append (' ');
			}
			
			foreach (var kvp in monoArgAttributes) {
				var prop = kvp.Key;
				var argAttr = kvp.Value;
				object val = GetValue (prop.GetValue (this, null));
				if ((val is bool) && (bool)val)
					ops.Append (argAttr.Name).Append (' ');
				else if ((val is string) && !string.IsNullOrEmpty ((string)val))
					ops.AppendFormat (argAttr.Name, val).Append (' ');
			}
			if (ops.Length > 0)
				ops.Remove (ops.Length - 1, 1);

			foreach (var kvp in envVarAttributes) {
				var prop = kvp.Key;
				var envVar = kvp.Value;

				object val = GetValue (prop.GetValue (this, null));
				if ((val is bool) && (bool)val)
					envVars [envVar.Name] = envVar.TrueValue;
				else if ((val is string) && !string.IsNullOrEmpty ((string)val))
					envVars [envVar.Name] = val.ToString ();
			}
			options = ops.ToString ();
		}
		
		object GetValue (object val)
		{
			Type etype = val.GetType ();
			if (etype.IsEnum) {
				long ival = Convert.ToInt64 (val);
				bool isFlags = etype.IsDefined (typeof(FlagsAttribute), false);
				StringBuilder flags = null;
				if (isFlags)
					flags = new StringBuilder ();
				IList names = Enum.GetNames (etype);
				foreach (FieldInfo f in etype.GetFields ()) {
					if (!names.Contains (f.Name))
						continue;
					long v = Convert.ToInt64 (Enum.Parse (etype, f.Name));
					MonoArgAttribute attr = (MonoArgAttribute) Attribute.GetCustomAttribute (f, typeof(MonoArgAttribute));
					string sval = attr != null ? attr.Name : f.Name;
					if (ival == v) {
						return sval;
					}
					else if (isFlags && (v & ival) != 0) {
						if (flags.Length > 0)
							flags.Append (',');
						flags.Append (sval);
					}
				}
				if (isFlags)
					return flags.ToString ();
			}
			return val;
		}

		public string GenerateDescription ()
		{
			StringBuilder ops = new StringBuilder ();

			foreach (var kvp in itemPropertyAttributes) {
				var prop = kvp.Key;
				var propAttr = kvp.Value;

				var pval = prop.GetValue (this, null);
				if (object.Equals (pval, propAttr.DefaultValue))
					continue;
				if (ops.Length > 0)
					ops.Append (", ");
				var nameAttr = localizedDisplayNameAttributes [prop];
				ops.Append (nameAttr.DisplayName);
				if (!(pval is bool)) {
					ops.Append (": ").Append (GetValue (pval));
				}
			}
			return ops.ToString ();
		}		
		public MonoExecutionParameters Clone ()
		{
			return (MonoExecutionParameters) MemberwiseClone ();
		}
		
		[LocalizedCategory ("Debug")]
		[LocalizedDisplayName ("Debug Mode")]
		[LocalizedDescription ("Enable debugging support.")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoDebugMode { get; set; }
		
		[LocalizedCategory ("Debug")]
		[LocalizedDisplayName ("Debug Casts")]
		[LocalizedDescription ("Enable more detailed InvalidCastException messages.")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoDebugCasts { get; set; }
		
		[LocalizedCategory ("Debug")]
		[LocalizedDisplayName ("MDB Mode")]
		[LocalizedDescription ("Disable some JIT optimizations which are normally " +
		              "disabled when running inside the debugger. This is useful " +
		              "if you plan to attach to the running process with the debugger.")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoDebugMdbOptimizations { get; set; }
		
		[LocalizedCategory ("Debug")]
		[LocalizedDisplayName ("GDB Symbols")]
		[LocalizedDescription ("Generate and register debugging information with gdb. " +
                     "This is only supported on some platforms, and only when " +
                     "using gdb 7.0 or later.")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoGdbInfo { get; set; }
		
		[LocalizedCategory ("Runtime")]
		[LocalizedDisplayName ("Profiler")]
		[LocalizedDescription ("Runs in profiling mode with the specified profiler module.")]
		[ItemProperty (DefaultValue="")]
		[MonoArg ("--profile={0}")]
		public string MonoProfile { get; set; }
		
		[LocalizedCategory ("Debug")]
		[LocalizedDisplayName ("Verbose Level")]
		[LocalizedDescription ("Increases the verbosity level.")]
		[ItemProperty (DefaultValue=0)]
		public int MonoVerboseLevel { get; set; }
		
		[LocalizedCategory ("Runtime")]
		[LocalizedDisplayName ("Runtime Version")]
		[LocalizedDescription ("Use the specified runtime version, instead of autodetecting")]
		[ItemProperty (DefaultValue="")]
		[MonoArg ("--runtime={0}")]
		public string MonoRuntimeVersion { get; set; }
		
		[LocalizedCategory ("Security")]
		[LocalizedDisplayName ("Security Mode")]
		[LocalizedDescription ("Turns on the unsupported security manager (off by default).")]
		[MonoArg ("--security={0}")]
		[ItemProperty (DefaultValue=SecurityMode.Disabled)]
		public SecurityMode MonoSecurityMode { get; set; }
		
		[LocalizedCategory ("Security")]
		[LocalizedDisplayName ("Verify All")]
		[LocalizedDescription ("Verifies mscorlib and assemblies in the global assembly cache " +
		              "for valid IL, and all user code for IL verifiability.")]
		[MonoArg ("--verify-all")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoVerifyAll { get; set; }
		
		[LocalizedCategory ("Tracing")]
		[LocalizedDisplayName ("Trace Expression")]
		[LocalizedDescription ("Comma separated list of expressions to trace. " +
		              "'all' all assemblies, " +
		              "'none' no assemblies, " +
		              "'program' entry point assembly, " +
		              "'assembly' specifies an assembly, " +
		              "'T:Type' specifies a type, " +
		              "'M:Type:Method'  a method, " +
		              "'N:Namespace' a namespace. " +
		              "'disabled' don't print any output until toggled via SIGUSR2. " +
		              "Prefix with '-' to exclude an expression.")]
		[MonoArg ("--trace={0}")]
		[ItemProperty (DefaultValue="")]
		public string MonoTraceExpressions { get; set; }
		
		[LocalizedCategory ("Logging")]
		[LocalizedDisplayName ("Log Level")]
		[LocalizedDescription ("Possible values are 'error', 'critical', 'warning', " +
		              "'message', 'info', 'debug'. The default  value  is  'error'. " +
		              "Messages with a logging level greater then or equal to the log level " +
		              "will be printed to stdout/stderr.")]
		[EnvVar ("MONO_LOG_LEVEL")]
		[ItemProperty (DefaultValue=LogLevel.Default)]
		public LogLevel MonoLogLevel { get; set; }
		
		[LocalizedCategory ("Logging")]
		[LocalizedDisplayName ("Log Mask")]
		[LocalizedDescription ("Possible values are 'asm'  (assembly  loader),  'type'," +
		              "  'dll'  (native library  loader), 'gc' (garbage collector), " +
		              "'cfg' (config file loader), 'aot' (precompiler) and 'all'. " +
		              "The default value is 'all'. Changing the mask value allows you " +
		              "to display only messages for a certain component. You can use " +
		              "multiple masks by comma separating them. For example to  see " +
		              "config file messages and assembly loader messages set you mask " +
		              "to 'asm,cfg'.")]
		[EnvVar ("MONO_LOG_MASK")]
		[ItemProperty (DefaultValue=LogMask.None)]
		public LogMask MonoLogMask { get; set; }
		
		[LocalizedCategory ("Library Options")]
		[LocalizedDisplayName ("Serializer Generation")]
		[LocalizedDescription ("The  possible values are `no' to disable the use of a C# customized " +
		              "serializer, or an integer that is the minimum number of uses before the " +
		              "runtime will produce a custom serializer (0 will produce a custom " +
		              "serializer on the first access, 50 will produce a serializer on the 50th " +
		              "use). Mono will fallback to an interpreted serializer if the serializer " +
		              "generation somehow fails. This behavior can be disabled by setting the " +
		              "option `nofallback' (for example: '0,nofallback').")]
		[EnvVar ("MONO_XMLSERIALIZER_THS")]
		[ItemProperty (DefaultValue="")]
		public string MonoXmlSerializerGeneration { get; set; }
		
		[LocalizedCategory ("Configuration")]
		[LocalizedDisplayName ("Mono Configuration Directory")]
		[LocalizedDescription ("Overrides the default system configuration directory ($PREFIX/etc). " +
		              "It's used to locate machine.config file.")]
		[EnvVar ("MONO_CFG_DIR")]
		[ItemProperty (DefaultValue="")]
		public string MonoConfigDir { get; set; }
		
		[LocalizedCategory ("Configuration")]
		[LocalizedDisplayName ("Mono Configuration File")]
		[LocalizedDescription ("Overrides the default runtime configuration file ($PREFIX/etc/mono/config).")]
		[MonoArg ("--config {0}")]
		[ItemProperty (DefaultValue="")]
		public string MonoConfigFile { get; set; }
		
		[LocalizedCategory ("Runtime")]
		[LocalizedDisplayName ("Disable AIO")]
		[LocalizedDescription ("If  set, tells mono NOT to attempt using native asynchronous I/O " +
		              "services. In that case, a default select/poll implementation is " +
		              "used. Currently only epoll() is supported.")]
		[EnvVar ("MONO_DISABLE_AIO")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoDisableAIO { get; set; }
		
		[LocalizedCategory ("Library Options")]
		[LocalizedDisplayName ("Disable Managed Collation")]
		[LocalizedDescription ("If set, the runtime uses unmanaged collation (which actually " +
		              "means no culture-sensitive collation). It internally disables " +
		              "managed collation functionality invoked via the members of " +
		              "System.Globalization.CompareInfo class.")]
		[EnvVar ("MONO_DISABLE_MANAGED_COLLATION", "yes")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoDisableManagedCollation { get; set; }
		
		[LocalizedCategory ("Library Options")]
		[LocalizedDisplayName ("External Encodings")]
		[LocalizedDescription ("A colon-separated list of text encodings to try when turning " +
		              "externally-generated text (e.g. command-line arguments or " +
		              "filenames) into Unicode.")]
		[EnvVar ("MONO_EXTERNAL_ENCODINGS")]
		[ItemProperty (DefaultValue="")]
		public string MonoExternalEncodings { get; set; }
		
		[LocalizedCategory ("Configuration")]
		[LocalizedDisplayName ("GAC Prefix")]
		[LocalizedDescription ("Provides a prefix the runtime uses to look for Global Assembly " +
		              "Caches. Directories are separated by the platform path separator " +
		              "(colons on unix). MONO_GAC_PREFIX should point to the top " +
		              "directory of a prefixed install. Or to the directory provided in " +
		              "the gacutil /gacdir command. Example: /home/username/.mono:/usr/local/mono/")]
		[EnvVar ("MONO_GAC_PREFIX")]
		[ItemProperty (DefaultValue="")]
		public string MonoGacPrefix { get; set; }
		
		[LocalizedCategory ("Compatibility")]
		[LocalizedDisplayName ("Strip Drive Letters")]
		[LocalizedDescription ("When enabled, Mono removes the drive letter from Windows paths.")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoStripDriveLetters { get; set; }
		
		[LocalizedCategory ("Compatibility")]
		[LocalizedDisplayName ("Case Insensitive Paths")]
		[LocalizedDescription ("When enabled, Mono does case-insensitive file matching in every directory in a path.")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoCaseInsensitivePaths { get; set; }
		
		[LocalizedCategory ("Library Options")]
		[LocalizedDisplayName ("Managed Watcher")]
		[LocalizedDescription ("When set, System.IO.FileSystemWatcher will use the default managed " +
		              "implementation  (slow).")]
		[EnvVar ("MONO_MANAGED_WATCHER", "yes")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoManagedWatcher { get; set; }
		
		[LocalizedCategory ("Runtime")]
		[LocalizedDisplayName ("No SMP")]
		[LocalizedDescription ("If set, causes the mono process to be bound to a single processor. " +
		              "This may be useful when debugging or working around race conditions.")]
		[EnvVar ("MONO_NO_SMP")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoNoSmp { get; set; }
		
		[LocalizedCategory ("Configuration")]
		[LocalizedDisplayName ("Mono Path")]
		[LocalizedDescription ("Provides a search path to the runtime where to look for library " +
		              "files. This is a tool convenient for debugging applications, " +
		              "but should not be used by deployed applications as it breaks the " +
		              "assembly loader in subtle ways. Directories are separated by " +
		              "the platform path separator (colons on unix). Example: " +
		              "/home/username/lib:/usr/local/mono/lib")]
		[EnvVar ("MONO_PATH")]
		[ItemProperty (DefaultValue="")]
		public string MonoPath { get; set; }
		
		[LocalizedCategory ("Library Options")]
		[LocalizedDisplayName ("Windows Forms Theme")]
		[LocalizedDescription ("The name of the theme to be used by Windows.Forms. Available " +
		              "themes include 'clearlooks', 'nice' and 'win32'. The default is 'win32'")]
		[EnvVar ("MONO_THEME")]
		[ItemProperty (DefaultValue="")]
		public string MonoWindowsFormsTheme { get; set; }
		
		[LocalizedCategory ("Runtime")]
		[LocalizedDisplayName ("Threads Per Cpu")]
		[LocalizedDescription ("The maximum number of threads in the general threadpool will be " +
		              "20 + (ThreadsPerCpu * number of CPUs). The default value" +
		              "for this variable is 10.")]
		[EnvVar ("MONO_THREADS_PER_CPU")]
		[ItemProperty (DefaultValue="")]
		public string MonoThreadsPerCpu { get; set; }
		
		[LocalizedCategory ("Library Options")]
		[LocalizedDisplayName ("Keep ASP.NET Temporary Files")]
		[LocalizedDescription ("If set, temporary source files generated by ASP.NET support " +
		              "classes will not be removed. They will be kept in the " +
		              "user's temporary directory.")]
		[EnvVar ("MONO_ASPNET_NODELETE")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoAspNetNoDelete { get; set; }
		
		[LocalizedCategory ("Tracing")]
		[LocalizedDisplayName ("Trace Listener")]
		[LocalizedDescription ("If set, enables the System.Diagnostics.DefaultTraceListener, " +
		              "which will print the output of the System.Diagnostics Trace and " +
		              "Debug classes. It can be set to a filename, and to Console.Out " +
		              "or Console.Error to display output to standard output or standard " +
		              "error, respectively. If it's set to Console.Out or Console.Error " +
		              "you can append an optional prefix that will be used when writing " +
		              "messages like this: Console.Error:MyProgramName.")]
		[EnvVar ("MONO_TRACE_LISTENER")]
		[ItemProperty (DefaultValue="")]
		public string MonoTraceListener { get; set; }
		
		[LocalizedCategory ("Runtime")]
		[LocalizedDisplayName ("X11 Exceptions")]
		[LocalizedDescription ("If set, an exception is thrown when a X11 error is encountered. " +
		              "By default a message is displayed but execution continues.")]
		[EnvVar ("MONO_XEXCEPTIONS")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoXExceptions { get; set; }
		
		[LocalizedCategory ("Debug")]
		[LocalizedDisplayName ("XDebug")]
		[LocalizedDescription ("When the the MONO_XDEBUG env var is set, debugging info for JITted " +
		              "code is emitted into a shared library, loadable into gdb. " +
		              "This enables, for example, to see managed frame names on gdb backtraces.")]
		[EnvVar ("MONO_XDEBUG")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoXDebug { get; set; }
		
		[LocalizedCategory ("Runtime")]
		[LocalizedDisplayName ("Garbage Collector")]
		[LocalizedDescription ("Selects  the  Garbage Collector engine for Mono to use.")]
		[MonoArg ("--gc={0}")]
		[ItemProperty (DefaultValue=GcType.Default)]
		public GcType MonoGcType { get; set; }
		
		[LocalizedCategory ("LLVM")]
		[LocalizedDisplayName ("Enable LLVM")]
		[LocalizedDescription ("If the Mono runtime has been compiled  with LLVM support (not " +
              "available in all configurations), this option enables use the LLVM optimization " +
              "and code generation engine to JIT or AOT compile. For more information " +
              "consult: http://www.mono-project.com/Mono_LLVM")]
		[MonoArg ("--llvm")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoLlvm { get; set; }
		
		[LocalizedCategory ("LLVM")]
		[LocalizedDisplayName ("Disable LLVM")]
		[LocalizedDescription ("When using a Mono that has been compiled with LLVM support, it " +
              "forces Mono to fallback to its JIT engine and not use the LLVM backend")]
		[MonoArg ("--nollvm")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoNoLlvm { get; set; }
		
		[LocalizedCategory ("Optimizations")]
		[LocalizedDisplayName ("Desktop Mode")]
		[LocalizedDescription ("Configures the virtual machine to be better suited for desktop " +
              "applications. Currently this sets the GC system to avoid " +
              "expanding the heap as much as possible at the expense of slowing " +
              "down garbage collection a bit.")]
		[MonoArg ("--desktop")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoDesktopMode { get; set; }

		[LocalizedCategory ("Optimizations")]
		[LocalizedDisplayName ("Server Mode")]
		[LocalizedDescription ("Configures the virtual machine to be better  suited  for  server operations.")]
		[MonoArg ("--server")]
		[ItemProperty (DefaultValue=false)]
		public bool MonoServerMode { get; set; }

		[LocalizedCategory ("Additional Options")]
		[LocalizedDisplayName ("Additional Options")]
		[LocalizedDescription ("Additional command line options to be provided to the Mono command.")]
		[MonoArg ("{0}")]
		[ItemProperty (DefaultValue="")]
		public string MonoAdditionalOptions { get; set; }

		[LocalizedCategory ("Runtime")]
		[LocalizedDisplayName ("Architecture")]
		[LocalizedDescription ("Selects the bitness of the Mono binary used, if available. If the binary used is already for the selected bitness, nothing changes. If not, the execution switches to a binary with the selected bitness suffix installed side by side (architecture=64 will switch to '/bin/mono64' if '/bin/mono' is a 32-bit build).")]
		[MonoArg ("--arch={0}")]
		[ItemProperty ("MonoArchitecture", DefaultValue = RuntimeArchitecture.Default)]
		public RuntimeArchitecture Architecture { get; set; }

		string legacyArchitecture;
		[ItemProperty("Architecture", DefaultValue = null)]
		internal string LegacyArchitecture {
			get { return legacyArchitecture; }
			set {
				legacyArchitecture = value;

				// The Architecture property is now serialized as "MonoArchitecture" to avoid conflicts with existing MSBuild properties.
				// If the value being read from the "Architecture" property is a valid value for the Mono architecture, then let's assume
				// that the property is actually the Mono architecture, and set it to the right property.
				if (Enum.TryParse<RuntimeArchitecture>(value, out RuntimeArchitecture arch) && arch != RuntimeArchitecture.Default)
					Architecture = arch;
			}
		}
	}
}
