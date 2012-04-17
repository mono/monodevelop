using System.Reflection;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Specialized;

namespace Sharpen
{
	using ICSharpCode.SharpZipLib.Zip.Compression;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Text.RegularExpressions;

	internal static class Extensions
	{
		private static readonly long EPOCH_TICKS;

		static Extensions ()
		{
			DateTime time = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			EPOCH_TICKS = time.Ticks;
		}
		
		public static void Add<T> (this IList<T> list, int index, T item)
		{
			list.Insert (index, item);
		}
		
		public static void AddFirst<T> (this IList<T> list, T item)
		{
			list.Insert (0, item);
		}
		
		public static void AddLast<T> (this IList<T> list, T item)
		{
			list.Add (item);
		}
		
		public static void RemoveLast<T> (this IList<T> list)
		{
			if (list.Count > 0)
				list.Remove (list.Count - 1);
		}

		public static StringBuilder AppendRange (this StringBuilder sb, string str, int start, int end)
		{
			return sb.Append (str, start, end - start);
		}

		public static StringBuilder Delete (this StringBuilder sb, int start, int end)
		{
			return sb.Remove (start, end - start);
		}

		public static void SetCharAt (this StringBuilder sb, int index, char c)
		{
			sb[index] = c;
		}

		public static int IndexOf (this StringBuilder sb, string str)
		{
			return sb.ToString ().IndexOf (str);
		}

		public static Iterable<T> AsIterable<T> (this IEnumerable<T> s)
		{
			return new EnumerableWrapper<T> (s);
		}

		public static int BitCount (int val)
		{
			uint num = (uint)val;
			int count = 0;
			for (int i = 0; i < 32; i++) {
				if ((num & 1) != 0) {
					count++;
				}
				num >>= 1;
			}
			return count;
		}

		public static IndexOutOfRangeException CreateIndexOutOfRangeException (int index)
		{
			return new IndexOutOfRangeException ("Index: " + index);
		}

		public static CultureInfo CreateLocale (string language, string country, string variant)
		{
			return CultureInfo.GetCultureInfo ("en-US");
		}

		public static string Name (this Encoding e)
		{
			return e.BodyName.ToUpper ();
		}
		
		public static string Decode (this Encoding e, byte[] chars, int start, int len)
		{
			try {
				byte[] bom = e.GetPreamble ();
				if (bom != null && bom.Length > 0) {
					if (len >= bom.Length) {
						int pos = start;
						bool hasBom = true;
						for (int n=0; n<bom.Length && hasBom; n++) {
							if (bom[n] != chars [pos++])
								hasBom = false;
						}
						if (hasBom) {
							len -= pos - start;
							start = pos;
						}
					}
				}
				return e.GetString (chars, start, len);
			} catch (DecoderFallbackException) {
				throw new CharacterCodingException ();
			}
		}
		
		public static string Decode (this Encoding e, ByteBuffer buffer)
		{
			return e.Decode (buffer.Array (), buffer.ArrayOffset () + buffer.Position (), buffer.Limit () - buffer.Position ());
		}

		public static ByteBuffer Encode (this Encoding e, CharSequence str)
		{
			return ByteBuffer.Wrap (e.GetBytes (str.ToString ()));
		}

		public static ByteBuffer Encode (this Encoding e, string str)
		{
			return ByteBuffer.Wrap (e.GetBytes (str));
		}
		
		static UTF8Encoding UTF8Encoder = new UTF8Encoding (false, true);
		public static Encoding GetEncoding (string name)
		{
//			Encoding e = Encoding.GetEncoding (name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
			try {
				Encoding e = Encoding.GetEncoding (name.Replace ('_','-'));
				if (e is UTF8Encoding)
					return UTF8Encoder;
				return e;
			} catch (ArgumentException) {
				throw new UnsupportedCharsetException (name);
			}
		}
		
		public static ICollection<KeyValuePair<T, U>> EntrySet<T, U> (this IDictionary<T, U> s)
		{
			return s;
		}

		public static void Finish (this Inflater i)
		{
		}
		
		public static bool AddItem<T> (this IList<T> list, T item)
		{
			list.Add (item);
			return true;
		}

		public static bool AddItem<T> (this ICollection<T> list, T item)
		{
			list.Add (item);
			return true;
		}

		public static U Get<T, U> (this IDictionary<T, U> d, T key)
		{
			U val;
			d.TryGetValue (key, out val);
			return val;
		}

		public static U Put<T, U> (this IDictionary<T, U> d, T key, U value)
		{
			U old;
			d.TryGetValue (key, out old);
			d [key] = value;
			return old;
		}
		
		public static void PutAll<T, U> (this IDictionary<T, U> d, IDictionary<T, U> values)
		{
			foreach (KeyValuePair<T,U> val in values)
				d[val.Key] = val.Value;
		}

		public static object Put (this Hashtable d, object key, object value)
		{
			object old = d [key];
			d[key] = value;
			return old;
		}

		public static string Put (this StringDictionary d, string key, string value)
		{
			string old = d [key];
			d[key] = value;
			return old;
		}

		public static CultureInfo GetEnglishCulture ()
		{
			return CultureInfo.GetCultureInfo ("en-US");
		}

		public static T GetFirst<T> (this IList<T> list)
		{
			return ((list.Count == 0) ? default(T) : list[0]);
		}

		public static CultureInfo GetGermanCulture ()
		{
			CultureInfo r =  CultureInfo.GetCultureInfo ("de-DE");
			return r;
		}

		public static T GetLast<T> (this IList<T> list)
		{
			return ((list.Count == 0) ? default(T) : list[list.Count - 1]);
		}

		public static int GetOffset (this TimeZoneInfo tzone, long date)
		{
			return (int)tzone.GetUtcOffset (MillisToDateTimeOffset (date, 0).DateTime).TotalMilliseconds;
		}

		public static InputStream GetResourceAsStream (this Type type, string name)
		{
			string str2 = type.Assembly.GetName ().Name;
			string[] textArray1 = new string[] { str2, ".resources.", type.Namespace, ".", name };
			string str = string.Concat (textArray1);
			Stream manifestResourceStream = type.Assembly.GetManifestResourceStream (str);
			if (manifestResourceStream == null) {
				return null;
			}
			return InputStream.Wrap (manifestResourceStream);
		}

		public static long GetTime (this DateTime dateTime)
		{
			return new DateTimeOffset (DateTime.SpecifyKind (dateTime, DateTimeKind.Utc), TimeSpan.Zero).ToMillisecondsSinceEpoch ();
		}

		public static TimeZoneInfo GetTimeZone (string tzone)
		{
			try {
				TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById (tzone);
				return tz;
			} catch {
				// Not found
			}
			char[] separator = new char[] { ':' };
			string[] strArray = tzone.Substring (4).Split (separator);
			int hours, minutes;
			if (strArray.Length == 1 && strArray[0].Length > 2) {
				hours = int.Parse (strArray[0].Substring (0, 2));
				minutes = int.Parse (strArray[0].Substring (2));
			} else {
				hours = int.Parse (strArray[0]);
				minutes = (strArray.Length <= 1) ? 0 : int.Parse (strArray[1]);
			}
			TimeSpan t = new TimeSpan (0, hours, minutes, 0, 0);
			if (tzone[3] == '-')
				t = -t;
			return TimeZoneInfo.CreateCustomTimeZone (tzone, t, tzone, tzone);
		}

		public static void InitCause (this Exception ex, Exception cause)
		{
			Console.WriteLine (cause);
		}
		
		public static bool IsEmpty<T> (this ICollection<T> col)
		{
			return (col.Count == 0);
		}

		public static bool IsEmpty<T> (this Stack<T> col)
		{
			return (col.Count == 0);
		}

		public static bool IsLower (this char c)
		{
			return char.IsLower (c);
		}

		public static bool IsUpper (this char c)
		{
			return char.IsUpper (c);
		}

		public static Sharpen.Iterator<T> Iterator<T> (this ICollection<T> col)
		{
			return new EnumeratorWrapper<T> (col, col.GetEnumerator ());
		}

		public static Sharpen.Iterator<T> Iterator<T> (this IEnumerable<T> col)
		{
			return new EnumeratorWrapper<T> (col, col.GetEnumerator ());
		}

		public static T Last<T> (this ICollection<T> col)
		{
			IList<T> list = col as IList<T>;
			if (list != null) {
				return list[list.Count - 1];
			}
			return col.Last<T> ();
		}

		public static ListIterator<T> ListIterator<T> (this IList<T> col, int n)
		{
			return new ListIterator<T> (col, n);
		}

		public static int LowestOneBit (int val)
		{
			return (((int)1) << NumberOfTrailingZeros (val));
		}

		public static bool Matches (this string str, string regex)
		{
			Regex regex2 = new Regex (regex);
			return regex2.IsMatch (str);
		}
		
		public static DateTime CreateDate (long milliSecondsSinceEpoch)
		{
			long num = EPOCH_TICKS + (milliSecondsSinceEpoch * 10000);
			return new DateTime (num);
		}

		public static DateTimeOffset MillisToDateTimeOffset (long milliSecondsSinceEpoch, long offsetMinutes)
		{
			TimeSpan offset = TimeSpan.FromMinutes ((double)offsetMinutes);
			long num = EPOCH_TICKS + (milliSecondsSinceEpoch * 10000);
			return new DateTimeOffset (num + offset.Ticks, offset);
		}

		public static CharsetDecoder NewDecoder (this Encoding enc)
		{
			return new CharsetDecoder (enc);
		}

		public static CharsetEncoder NewEncoder (this Encoding enc)
		{
			return new CharsetEncoder (enc);
		}

		public static int NumberOfLeadingZeros (int val)
		{
			uint num = (uint)val;
			int count = 0;
			while ((num & 0x80000000) == 0) {
				num = num << 1;
				count++;
			}
			return count;
		}

		public static int NumberOfTrailingZeros (int val)
		{
			uint num = (uint)val;
			int count = 0;
			while ((num & 1) == 0) {
				num = num >> 1;
				count++;
			}
			return count;
		}

		public static int Read (this StreamReader reader, char[] data)
		{
			return reader.Read (data, 0, data.Length);
		}

		public static T Remove<T> (this IList<T> list, T item)
		{
			int index = list.IndexOf (item);
			if (index == -1) {
				return default(T);
			}
			T local = list[index];
			list.RemoveAt (index);
			return local;
		}

		public static T Remove<T> (this IList<T> list, int i)
		{
			T old;
			try {
				old = list[i];
				list.RemoveAt (i);
			} catch (IndexOutOfRangeException) {
				throw new NoSuchElementException ();
			}
			return old;
		}

		public static T RemoveFirst<T> (this IList<T> list)
		{
			return list.Remove<T> (0);
		}

		public static string ReplaceAll (this string str, string regex, string replacement)
		{
			Regex rgx = new Regex (regex);
			
			if (replacement.IndexOfAny (new char[] { '\\','$' }) != -1) {
				// Back references not yet supported
				StringBuilder sb = new StringBuilder ();
				for (int n=0; n<replacement.Length; n++) {
					char c = replacement [n];
					if (c == '$')
						throw new NotSupportedException ("Back references not supported");
					if (c == '\\')
						c = replacement [++n];
					sb.Append (c);
				}
				replacement = sb.ToString ();
			}
			
			return rgx.Replace (str, replacement);
		}
		
		public static bool RegionMatches (this string str, bool ignoreCase, int toOffset, string other, int ooffset, int len)
		{
			if (toOffset < 0 || ooffset < 0 || toOffset + len > str.Length || ooffset + len > other.Length)
				return false;
			return string.Compare (str, toOffset, other, ooffset, len) == 0;
		}

		public static T Set<T> (this IList<T> list, int index, T item)
		{
			T old = list[index];
			list[index] = item;
			return old;
		}

		public static int Signum (long val)
		{
			if (val < 0) {
				return -1;
			}
			if (val > 0) {
				return 1;
			}
			return 0;
		}
		
		public static void RemoveAll<T,U> (this ICollection<T> col, ICollection<U> items) where U:T
		{
			foreach (var u in items)
				col.Remove (u);
		}

		public static bool ContainsAll<T,U> (this ICollection<T> col, ICollection<U> items) where U:T
		{
			foreach (var u in items)
				if (!col.Any (n => (object.ReferenceEquals (n, u)) || n.Equals (u)))
					return false;
			return true;
		}

		public static bool Contains<T> (this ICollection<T> col, object item)
		{
			if (!(item is T))
				return false;
			return col.Any (n => (object.ReferenceEquals (n, item)) || n.Equals (item));
		}

		public static void Sort<T> (this IList<T> list)
		{
			List<T> sorted = new List<T> (list);
			sorted.Sort ();
			for (int i = 0; i < list.Count; i++) {
				list[i] = sorted[i];
			}
		}

		public static void Sort<T> (this IList<T> list, IComparer<T> comparer)
		{
			List<T> sorted = new List<T> (list);
			sorted.Sort (comparer);
			for (int i = 0; i < list.Count; i++) {
				list[i] = sorted[i];
			}
		}

		public static string[] Split (this string str, string regex)
		{
			return str.Split (regex, 0);
		}
		
		public static string[] Split (this string str, string regex, int limit)
		{
			Regex rgx = new Regex (regex);
			List<string> list = new List<string> ();
			int startIndex = 0;
			if (limit != 1) {
				int nm = 1;
				foreach (Match match in rgx.Matches (str)) {
					list.Add (str.Substring (startIndex, match.Index - startIndex));
					startIndex = match.Index + match.Length;
					if (limit > 0 && ++nm == limit)
						break;
				}
			}
			if (startIndex < str.Length) {
				list.Add (str.Substring (startIndex));
			}
			if (limit >= 0) {
				int count = list.Count - 1;
				while ((count >= 0) && (list[count].Length == 0)) {
					count--;
				}
				list.RemoveRange (count + 1, (list.Count - count) - 1);
			}
			return list.ToArray ();
		}

		public static IList<T> SubList<T> (this IList<T> list, int start, int len)
		{
			List<T> sublist = new List<T> (len);
			for (int i = start; i < (start + len); i++) {
				sublist.Add (list[i]);
			}
			return sublist;
		}

		public static char[] ToCharArray (this string str)
		{
			char[] destination = new char[str.Length];
			str.CopyTo (0, destination, 0, str.Length);
			return destination;
		}

		public static long ToMillisecondsSinceEpoch (this DateTime dateTime)
		{
			if (dateTime.Kind != DateTimeKind.Utc) {
				throw new ArgumentException ("dateTime is expected to be expressed as a UTC DateTime", "dateTime");
			}
			return new DateTimeOffset (DateTime.SpecifyKind (dateTime, DateTimeKind.Utc), TimeSpan.Zero).ToMillisecondsSinceEpoch ();
		}

		public static long ToMillisecondsSinceEpoch (this DateTimeOffset dateTimeOffset)
		{
			return (((dateTimeOffset.Ticks - dateTimeOffset.Offset.Ticks) - EPOCH_TICKS) / TimeSpan.TicksPerMillisecond);
		}

		public static string ToOctalString (int val)
		{
			return Convert.ToString (val, 8);
		}

		public static string ToHexString (int val)
		{
			return Convert.ToString (val, 16);
		}

		public static string ToString (object val)
		{
			return val.ToString ();
		}

		public static string ToString (int val, int bas)
		{
			return Convert.ToString (val, bas);
		}

		public static IList<U> UpcastTo<T, U> (this IList<T> s) where T : U
		{
			List<U> list = new List<U> (s.Count);
			for (int i = 0; i < s.Count; i++) {
				list.Add ((U)s[i]);
			}
			return list;
		}

		public static ICollection<U> UpcastTo<T, U> (this ICollection<T> s) where T : U
		{
			List<U> list = new List<U> (s.Count);
			foreach (var v in s) {
				list.Add ((U)v);
			}
			return list;
		}

		public static T ValueOf<T> (T val)
		{
			return val;
		}

		public static int GetTotalInFixed (this Inflater inf)
		{
			if (inf.TotalIn > 0)
				return inf.TotalIn + 4;
			else
				return 0;
		}
		
		public static int GetRemainingInputFixed (this Inflater inf)
		{
			if (inf.RemainingInput >= 4)
				return inf.RemainingInput - 4;
			else
				return 0;
		}
		
		public static string GetTestName (object obj)
		{
			return GetTestName ();
		}
		
		public static string GetTestName ()
		{
			MethodBase met;
			int n = 0;
			do {
				met = new StackFrame (n).GetMethod ();
				if (met != null) {
					foreach (Attribute at in met.GetCustomAttributes (true)) {
						if (at.GetType().FullName == "NUnit.Framework.TestAttribute") {
							// Convert back to camel case
							string name = met.Name;
							if (char.IsUpper (name[0]))
								name = char.ToLower (name[0]) + name.Substring (1);
							return name;
						}
					}
				}
				n++;
			} while (met != null);
			return "";
		}
		
		public static string GetHostAddress (this IPAddress addr)
		{
			return addr.ToString ();
		}
		
		public static IPAddress GetAddressByName (string name)
		{
			if (name == "0.0.0.0")
				return IPAddress.Any;
			return Dns.GetHostAddresses (name).FirstOrDefault ();
		}
		
		public static string GetImplementationVersion (this System.Reflection.Assembly asm)
		{
			return asm.GetName ().Version.ToString ();
		}
		
		public static string GetHost (this Uri uri)
		{
			return string.IsNullOrEmpty (uri.Host) ? null : uri.Host;
		}
		
		public static string GetUserInfo (this Uri uri)
		{
			return string.IsNullOrEmpty (uri.UserInfo) ? null : uri.UserInfo;
		}
		
		public static string GetQuery (this Uri uri)
		{
			return string.IsNullOrEmpty (uri.Query) ? null : uri.Query;
		}
		
		public static HttpURLConnection OpenConnection (this Uri uri, Proxy p)
		{
			return new HttpsURLConnection (uri, p);
		}
		
		public static Uri ToURI (this Uri uri)
		{
			return uri;
		}
		
		public static Uri ToURL (this Uri uri)
		{
			return uri;
		}
		
		public static InputStream GetInputStream (this Socket socket)
		{
			return new System.Net.Sockets.NetworkStream (socket);
		}
		
		public static OutputStream GetOutputStream (this Socket socket)
		{
			return new System.Net.Sockets.NetworkStream (socket);
		}
		
		public static int GetLocalPort (this Socket socket)
		{
			return ((IPEndPoint)socket.LocalEndPoint).Port;
		}
		
		public static int GetPort (this Socket socket)
		{
			return ((IPEndPoint)socket.RemoteEndPoint).Port;
		}
		
		public static IPAddress GetInetAddress (this Socket socket)
		{
			return ((IPEndPoint)socket.RemoteEndPoint).Address;
		}
		
		public static void Bind2 (this Socket socket, EndPoint ep)
		{
			if (ep == null)
				socket.Bind (new IPEndPoint (IPAddress.Any, 0));
			else
				socket.Bind (ep);
		}
		
		
		public static void Connect (this Socket socket, EndPoint ep, int timeout)
		{
			try {
				IAsyncResult res = socket.BeginConnect (ep,null, null);
				if (!res.AsyncWaitHandle.WaitOne (timeout > 0 ? timeout : Timeout.Infinite, true))
					throw new IOException ("Connection timeout");
			} catch (SocketException se) {
				throw new IOException (se.Message);
			}
		}
		
		public static Socket CreateServerSocket (int port, int backlog, IPAddress addr)
		{
			Socket s = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			s.Bind (new IPEndPoint (addr, port));
			return s;
		}
		
		public static Socket CreateSocket (string host, int port)
		{
			Socket s = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			s.Connect (host, port);
			return s;
		}
		
		public static Socket CreateSocket ()
		{
			return new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}
		
		public static bool RemoveElement (this ArrayList list, object elem)
		{
			int i = list.IndexOf (elem);
			if (i == -1)
				return false;
			else {
				list.RemoveAt (i);
				return true;
			}
		}
		
		public static System.Threading.Semaphore CreateSemaphore (int count)
		{
			return new System.Threading.Semaphore (count, int.MaxValue);
		}
		
		public static void SetCommand (this ProcessStartInfo si, IList<string> args)
		{
			si.FileName = args[0];
			si.Arguments = string.Join (" ", args.Skip (1).Select (a => "\"" + a + "\"").ToArray ());
		}
		
		public static SystemProcess Start (this ProcessStartInfo si)
		{
			si.UseShellExecute = false;
			si.RedirectStandardInput = true;
			si.RedirectStandardError = true;
			si.RedirectStandardOutput = true;
			si.CreateNoWindow = true;
			return SystemProcess.Start (si);
		}
	}
}
