using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Text;
using System.Threading;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace Sharpen
{
	internal class Runtime
	{
		private static Runtime instance;
		private List<ShutdownHook> shutdownHooks = new List<ShutdownHook> ();

		public void AddShutdownHook (Runnable r)
		{
			ShutdownHook item = new ShutdownHook ();
			item.Runnable = r;
			this.shutdownHooks.Add (item);
		}

		public int AvailableProcessors ()
		{
			return Environment.ProcessorCount;
		}

		public static long CurrentTimeMillis ()
		{
			return DateTime.UtcNow.ToMillisecondsSinceEpoch ();
		}

		public SystemProcess Exec (string[] cmd, string[] envp, FilePath dir)
		{
			try {
				ProcessStartInfo psi = new ProcessStartInfo ();
				psi.FileName = cmd[0];
				psi.Arguments = string.Join (" ", cmd, 1, cmd.Length - 1);
				if (dir != null) {
					psi.WorkingDirectory = dir.GetPath ();
				}
				psi.UseShellExecute = false;
				psi.RedirectStandardInput = true;
				psi.RedirectStandardError = true;
				psi.RedirectStandardOutput = true;
				psi.CreateNoWindow = true;
				if (envp != null) {
					foreach (string str in envp) {
						int index = str.IndexOf ('=');
						psi.EnvironmentVariables[str.Substring (0, index)] = str.Substring (index + 1);
					}
				}
				return SystemProcess.Start (psi);
			} catch (System.ComponentModel.Win32Exception ex) {
				throw new IOException (ex.Message);
			}
		}

		public static string Getenv (string var)
		{
			return Environment.GetEnvironmentVariable (var);
		}

		public static IDictionary<string, string> GetEnv ()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string> ();
			foreach (DictionaryEntry v in Environment.GetEnvironmentVariables ()) {
				dictionary[(string)v.Key] = (string)v.Value;
			}
			return dictionary;
		}

		public static IPAddress GetLocalHost ()
		{
			return Dns.GetHostEntry (Dns.GetHostName ()).AddressList[0];
		}
		
		static Hashtable properties;
		
		public static Hashtable GetProperties ()
		{
			if (properties == null) {
				properties = new Hashtable ();
				properties ["jgit.fs.debug"] = "false";
				properties ["user.home"] = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				properties ["java.library.path"] = Environment.GetEnvironmentVariable ("PATH");
				if (Path.DirectorySeparatorChar != '\\')
					properties ["os.name"] = "Unix";
				else
					properties ["os.name"] = "Windows";
			}
			return properties;
		}

		public static string GetProperty (string key)
		{
			return ((string) GetProperties ()[key]);
		}
		
		public static void SetProperty (string key, string value)
		{
//			throw new NotImplementedException ();
		}

		public static Runtime GetRuntime ()
		{
			if (instance == null) {
				instance = new Runtime ();
			}
			return instance;
		}

		public static int IdentityHashCode (object ob)
		{
			return RuntimeHelpers.GetHashCode (ob);
		}

		public long MaxMemory ()
		{
			return int.MaxValue;
		}

		private class ShutdownHook
		{
			public Sharpen.Runnable Runnable;

			~ShutdownHook ()
			{
				this.Runnable.Run ();
			}
		}
		
		public static void DeleteCharAt (StringBuilder sb, int index)
		{
			sb.Remove (index, 1);
		}
		
		public static byte[] GetBytesForString (string str)
		{
			return Encoding.UTF8.GetBytes (str);
		}

		public static byte[] GetBytesForString (string str, string encoding)
		{
			return Encoding.GetEncoding (encoding).GetBytes (str);
		}

		public static FieldInfo[] GetDeclaredFields (Type t)
		{
			return t.GetFields (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		}

		public static void NotifyAll (object ob)
		{
			Monitor.PulseAll (ob);
		}

		public static void PrintStackTrace (Exception ex)
		{
			Console.WriteLine (ex);
		}

		public static void PrintStackTrace (Exception ex, TextWriter tw)
		{
			tw.WriteLine (ex);
		}

		public static string Substring (string str, int index)
		{
			return str.Substring (index);
		}

		public static string Substring (string str, int index, int endIndex)
		{
			return str.Substring (index, endIndex - index);
		}

		public static void Wait (object ob)
		{
			Monitor.Wait (ob);
		}

		public static bool Wait (object ob, long milis)
		{
			return Monitor.Wait (ob, (int)milis);
		}
		
		public static Type GetType (string name)
		{
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies ()) {
				Type t = a.GetType (name);
				if (t != null)
					return t;
			}
			throw new InvalidOperationException ("Type not found: " + name);
		}
		
		public static void SetCharAt (StringBuilder sb, int index, char c)
		{
			sb [index] = c;
		}
		
		public static bool EqualsIgnoreCase (string s1, string s2)
		{
			return s1.Equals (s2, StringComparison.CurrentCultureIgnoreCase);
		}
		
		public static long NanoTime ()
		{
			return Environment.TickCount * 1000 * 1000;
		}
		
		public static int CompareOrdinal (string s1, string s2)
		{
			return string.CompareOrdinal (s1, s2);
		}

		public static string GetStringForBytes (byte[] chars)
		{
			return Encoding.UTF8.GetString (chars);
		}

		public static string GetStringForBytes (byte[] chars, string encoding)
		{
			return GetEncoding (encoding).GetString (chars);
		}

		public static string GetStringForBytes (byte[] chars, int start, int len)
		{
			return Encoding.UTF8.GetString (chars, start, len);
		}

		public static string GetStringForBytes (byte[] chars, int start, int len, string encoding)
		{
			return GetEncoding (encoding).Decode (chars, start, len);
		}
		
		public static Encoding GetEncoding (string name)
		{
//			Encoding e = Encoding.GetEncoding (name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
			Encoding e = Encoding.GetEncoding (name.Replace ('_','-'));
			if (e is UTF8Encoding)
				return new UTF8Encoding (false, true);
			return e;
		}
	}
}
