using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace MonoDevelop.Core.Execution
{
	public class BinaryMessage
	{
		enum TypeCode
		{
			Array = 1,
			String = 2,
			Object = 3,
			Map = 4,
			Null = 5,
			Boolean = 6,
			Single = 7,
			Double = 8,
			Int16 = 9,
			Int32 = 10,
			Int64 = 11,
			Byte = 12,
			DateTime = 13,
			TimeSpan = 14,
		}

		static readonly string NullString = "\0";
		static object dataTypesLock = new object ();
		static Dictionary<Type, TypeMap> dataTypes = new Dictionary<Type, TypeMap> ();

		List<BinaryMessageArgument> args = new List<BinaryMessageArgument> ();
		static int nextId;

		public int Id { get; set; }
		public string Name { get; set; }
		public string Target { get; set; }
		public DateTime SentTime { get; set; }
		public bool OneWay { get; set; }
		internal bool BypassConnection { get; set; }

		internal long ProcessingTime { get; set; }

		public BinaryMessage CreateResponse ()
		{
			return new BinaryMessage {
				Name = Name + "Response",
				Id = Id
			};
		}

		public BinaryMessage CreateResponse (object result)
		{
			return new BinaryMessage {
				Name = Name + "Response",
				Id = Id
			}.AddArgument ("Result", result);
		}

		public BinaryMessage CreateErrorResponse (string message, bool isInternal = false)
		{
			return new BinaryMessage ("Error") { Id = Id }.AddArgument ("Message", message).AddArgument ("IsInternal", isInternal);
		}

		public BinaryMessage AddArgument (string name, object value)
		{
			args.Add (new BinaryMessageArgument () { Name = name, Value = value });
			return this;
		}
		
		public object GetArgument (string name)
		{
			var arg = args.FirstOrDefault (a => a.Name == name);
			return arg != null ? arg.Value : null;
		}
		
		public T GetArgument<T> (string name)
		{
			var r = GetArgument (name);
			return (T)ConvertToType (r, typeof (T));
		}
		
		public List<BinaryMessageArgument> Args {
			get { return args; }
		}
		
		protected BinaryMessage ()
		{
			Id = System.Threading.Interlocked.Increment (ref nextId);
		}

		public BinaryMessage (string name)
		{
			Id = System.Threading.Interlocked.Increment (ref nextId);
			Name = name ?? "";
			Target = "";
		}
		
		public BinaryMessage (string name, string target)
		{
			Id = System.Threading.Interlocked.Increment (ref nextId);
			Name = name ?? "";
			Target = target ?? "";
		}

		public void CopyFrom (BinaryMessage msg)
		{
			Id = msg.Id;
			Name = msg.Name;
			Target = msg.Target;
			SentTime = msg.SentTime;
			OneWay = msg.OneWay;
			BypassConnection = msg.BypassConnection;
			ProcessingTime = msg.ProcessingTime;
			args = new List<BinaryMessageArgument> (msg.args);
			LoadCustomData ();
		}

		internal protected virtual Type GetResponseType ()
		{
			return typeof (BinaryMessage);
		}

		internal void ReadCustomData ()
		{
			var att = (MessageDataTypeAttribute) Attribute.GetCustomAttribute (GetType (), typeof (MessageDataTypeAttribute));
			if (att == null)
				return;
			if (string.IsNullOrEmpty (Name)) {
				Name = att.Name ?? GetType ().FullName;
			}
			var data = WriteMessageData (this);
			foreach (var e in data)
				AddArgument (e.Key, e.Value);
		}

		internal void LoadCustomData ()
		{
			if (!Attribute.IsDefined (GetType (), typeof (MessageDataTypeAttribute)))
				return;
			ReadMessageData ();
		}

		public void Write (Stream outStream)
		{
			MemoryStream s = new MemoryStream ();
			BinaryWriter bw = new BinaryWriter (s);
			bw.Write (Id);
			bw.Write (Name ?? "");
			bw.Write (Target ?? "");
			bw.Write (args.Count);
			foreach (var arg in args) {
				bw.Write (arg.Name ?? "");
				WriteValue (bw, arg.Value);
			}
			var data = s.ToArray ();
			bw = new BinaryWriter (outStream);
			bw.Write (data.Length);
			bw.Write (data);
		}
		
		void WriteValue (BinaryWriter bw, object val)
		{
			if (val == null)
				bw.Write ((byte)TypeCode.Null);
			else if (val is Array) {
				bw.Write ((byte)TypeCode.Array);
				WriteArray (bw, val);
			} else if (val is byte) {
				bw.Write ((byte)TypeCode.Byte);
				bw.Write ((byte)val);
			} else if (val is short) {
				bw.Write ((byte)TypeCode.Int16);
				bw.Write ((short)val);
			} else if (val is int) {
				bw.Write ((byte)TypeCode.Int32);
				bw.Write ((int)val);
			} else if (val is long) {
				bw.Write ((byte)TypeCode.Int64);
				bw.Write ((long)val);
			} else if (val is float) {
				bw.Write ((byte)TypeCode.Single);
				bw.Write ((float)val);
			} else if (val is double) {
				bw.Write ((byte)TypeCode.Double);
				bw.Write ((double)val);
			} else if (val is string) {
				bw.Write ((byte)TypeCode.String);
				bw.Write ((string)val);
			} else if (val is bool) {
				bw.Write ((byte)TypeCode.Boolean);
				bw.Write ((bool)val);
			} else if (val is DateTime) {
				bw.Write ((byte)TypeCode.DateTime);
				bw.Write (((DateTime)val).Ticks);
			} else if (val is TimeSpan) {
				bw.Write ((byte)TypeCode.TimeSpan);
				bw.Write (((TimeSpan)val).Ticks);
			} else if (val.GetType ().IsGenericType && val.GetType ().GetGenericTypeDefinition () == typeof(Dictionary<,>)) {
				bw.Write ((byte)TypeCode.Map);
				var t = val.GetType ();
				var keyType = t.GetGenericArguments () [0];
				var valueType = t.GetGenericArguments () [1];
				var dict = (IDictionary)val;
				var keys = Array.CreateInstance (keyType, dict.Count);
				var values = Array.CreateInstance (valueType, dict.Count);
				int n = 0;
				foreach (DictionaryEntry e in dict) {
					keys.SetValue (e.Key, n);
					values.SetValue (e.Value, n);
					n++;
				}
				WriteArray (bw, keys);
				WriteArray (bw, values);
			} else if (val.GetType ().IsEnum) {
				WriteValue (bw, Convert.ToInt64(val));
			} else {
				var d = WriteMessageData (val);
				WriteValue (bw, d);
			}
		}

		void WriteArray (BinaryWriter bw, object val)
		{
			Array array = (Array)val;
			int rank = array.Rank;
			bw.Write (array.Rank);
			for (int i = 0; i < rank; ++i)
				bw.Write (array.GetLength (rank));

			var type = val.GetType ();
			var et = type.GetElementType ();

			if (rank == 1) {
				if (et == typeof (byte)) {
					bw.Write ((byte)TypeCode.Byte);
					bw.Write ((byte [])val);
				} else if (et == typeof (short)) {
					bw.Write ((byte)TypeCode.Int16);
					foreach (var v in (short [])val)
						bw.Write (v);
				} else if (et == typeof (int)) {
					bw.Write ((byte)TypeCode.Int32);
					foreach (var v in (int [])val)
						bw.Write (v);
				} else if (et == typeof (long)) {
					bw.Write ((byte)TypeCode.Int64);
					foreach (var v in (long [])val)
						bw.Write (v);
				} else if (et == typeof (float)) {
					bw.Write ((byte)TypeCode.Single);
					foreach (var v in (float [])val)
						bw.Write (v);
				} else if (et == typeof (double)) {
					bw.Write ((byte)TypeCode.Double);
					foreach (var v in (double [])val)
						bw.Write (v);
				} else if (et == typeof (string)) {
					bw.Write ((byte)TypeCode.String);
					foreach (var v in (string [])val)
						bw.Write (v ?? NullString);
				} else if (et == typeof (bool)) {
					bw.Write ((byte)TypeCode.Boolean);
					foreach (var v in (bool [])val)
						bw.Write (v);
				} else if (et == typeof (DateTime)) {
					bw.Write ((byte)TypeCode.DateTime);
					foreach (var v in (DateTime [])val)
						bw.Write (v.Ticks);
				} else if (et == typeof (TimeSpan)) {
					bw.Write ((byte)TypeCode.TimeSpan);
					foreach (var v in (TimeSpan [])val)
						bw.Write (v.Ticks);
				} else {
					bw.Write ((byte)TypeCode.Object);
					foreach (var elem in array)
						WriteValue (bw, elem);
				}
			} else {
				if (et == typeof (byte)) {
					bw.Write ((byte)TypeCode.Byte);
					foreach (byte v in array)
						bw.Write (v);
				} else if (et == typeof (short)) {
					bw.Write ((byte)TypeCode.Int16);
					foreach (short v in array)
						bw.Write (v);
				} else if (et == typeof (int)) {
					bw.Write ((byte)TypeCode.Int32);
					foreach (int v in array)
						bw.Write (v);
				} else if (et == typeof (long)) {
					bw.Write ((byte)TypeCode.Int64);
					foreach (long v in array)
						bw.Write (v);
				} else if (et == typeof (float)) {
					bw.Write ((byte)TypeCode.Single);
					foreach (float v in array)
						bw.Write (v);
				} else if (et == typeof (double)) {
					bw.Write ((byte)TypeCode.Double);
					foreach (double v in array)
						bw.Write (v);
				} else if (et == typeof (string)) {
					bw.Write ((byte)TypeCode.String);
					foreach (string v in array)
						bw.Write (v ?? NullString);
				} else if (et == typeof (bool)) {
					bw.Write ((byte)TypeCode.Boolean);
					foreach (bool v in array)
						bw.Write (v);
				} else if (et == typeof (DateTime)) {
					bw.Write ((byte)TypeCode.DateTime);
					foreach (DateTime v in array)
						bw.Write (v.Ticks);
				} else if (et == typeof (TimeSpan)) {
					bw.Write ((byte)TypeCode.TimeSpan);
					foreach (TimeSpan v in array)
						bw.Write (v.Ticks);
				} else {
					bw.Write ((byte)TypeCode.Object);
					foreach (var elem in array)
						WriteValue (bw, elem);
				}
			}
		}

		public static BinaryMessage Read (Stream s)
		{
			BinaryReader br = new BinaryReader (s);
			br.ReadInt32 (); // length

			BinaryMessage msg = new BinaryMessage ();
			msg.Id = br.ReadInt32 ();
			msg.Name = br.ReadString ();
			msg.Target = br.ReadString ();
			int ac = br.ReadInt32 ();
			for (int n=0; n<ac; n++) {
				BinaryMessageArgument arg = new BinaryMessageArgument ();
				arg.Name = br.ReadString ();
				arg.Value = ReadValue (br);
				msg.Args.Add (arg);
			}
			return msg;
		}
		
		public static object ReadValue (BinaryReader br)
		{
			byte t = br.ReadByte ();
			switch ((TypeCode)t) {
			case TypeCode.Null:
				return null;
			case TypeCode.Array:
				return ReadArray (br);
			case TypeCode.Double:
				return br.ReadDouble ();
			case TypeCode.Byte:
				return br.ReadByte ();
			case TypeCode.Int16:
				return br.ReadInt16 ();
			case TypeCode.Int32:
				return br.ReadInt32 ();
			case TypeCode.Int64:
				return br.ReadInt64 ();
			case TypeCode.Single:
				return br.ReadSingle ();
			case TypeCode.String:
				return br.ReadString ();
			case TypeCode.Boolean:
				return br.ReadBoolean ();
			case TypeCode.DateTime:
				return new DateTime (br.ReadInt64 ());
			case TypeCode.TimeSpan:
				return new TimeSpan (br.ReadInt64 ());
			case TypeCode.Map: {
					var keys = (Array)ReadArray (br);
					var values = (Array)ReadArray (br);
					var dt = typeof (Dictionary<,>);
					var dictType = dt.MakeGenericType (keys.GetType ().GetElementType (), values.GetType ().GetElementType ());

					var dict = (IDictionary)Activator.CreateInstance (dictType);
					for (int n = 0; n < keys.Length; n++)
						dict [keys.GetValue (n)] = values.GetValue (n);
					return dict;
				}
			}
			throw new NotSupportedException ("code: " + t);
		}

		internal class MultiDimensionalIterator
		{
			Array arr;
			readonly int [] indices, lengths;
			bool first;

			public MultiDimensionalIterator (Array arr)
			{
				this.arr = arr;
				lengths = new int [arr.Rank];
				indices = new int [arr.Rank];
				first = true;

				for (int i = 0; i < arr.Rank; ++i)
					lengths [i] = arr.GetLength (i);
			}

			public void Fill (Func<object> fill)
			{
				(bool, int []) res;
				while ((res = TryMoveNext ()).Item1) {
					arr.SetValue (fill (), res.Item2);
				}
			}

			public (bool, int []) TryMoveNext ()
			{
				return TryMoveNextInternal (indices.Length - 1);
			}

			(bool, int []) TryMoveNextInternal (int lastRank)
			{
				if (lastRank == -1)
					return (false, indices);

				if (lengths [lastRank] != 0) {
					if (first) {
						first = false;
						return (true, indices);
					}

					indices [lastRank]++;
					// We reached the last element, try bumping previous dimension
					if (indices [lastRank] != lengths [lastRank])
						return (true, indices);

					// We're done.
					if (lastRank == 0)
						return (false, indices);

					for (int i = lastRank; i < indices.Length; ++i)
						indices [i] = 0;

				}
				int newRank = lastRank - 1;
				return TryMoveNextInternal (newRank);
			}
		}

		static object ReadArray (BinaryReader br)
		{
			int rank = br.ReadInt32 ();
			int[] lengths = new int [rank];

			var type = (TypeCode)br.ReadByte ();

			if (rank == 1) {
				int count = lengths [0];
				switch (type) {
				case TypeCode.Object: {
						var a = new object [count];
						for (int n = 0; n < count; n++)
							a [n] = ReadValue (br);
						return a;
					}
				case TypeCode.Double: {
						var a = new double [count];
						for (int n = 0; n < count; n++)
							a [n] = br.ReadDouble ();
						return a;
					}
				case TypeCode.Byte: {
						return br.ReadBytes (count);
					}
				case TypeCode.Int16: {
						var a = new short [count];
						for (int n = 0; n < count; n++)
							a [n] = br.ReadInt16 ();
						return a;
					}
				case TypeCode.Int32: {
						var a = new int [count];
						for (int n = 0; n < count; n++)
							a [n] = br.ReadInt32 ();
						return a;
					}
				case TypeCode.Int64: {
						var a = new long [count];
						for (int n = 0; n < count; n++)
							a [n] = br.ReadInt64 ();
						return a;
					}
				case TypeCode.Single: {
						var a = new float [count];
						for (int n = 0; n < count; n++)
							a [n] = br.ReadSingle ();
						return a;
					}
				case TypeCode.String: {
						var a = new string [count];
						for (int n = 0; n < count; n++) {
							string s = br.ReadString ();
							if (s == NullString)
								s = null;
							a [n] = s;
						}
						return a;
					}
				case TypeCode.Boolean: {
						var a = new bool [count];
						for (int n = 0; n < count; n++)
							a [n] = br.ReadBoolean ();
						return a;
					}
				case TypeCode.DateTime: {
						var a = new DateTime [count];
						for (int n = 0; n < count; n++)
							a [n] = new DateTime (br.ReadInt64 ());
						return a;
					}
				case TypeCode.TimeSpan: {
						var a = new TimeSpan [count];
						for (int n = 0; n < count; n++)
							a [n] = new TimeSpan (br.ReadInt64 ());
						return a;
					}
				}
			} else {
				int [] indices = new int [lengths.Length];
				switch (type) {
				case TypeCode.Object: {
						var a = Array.CreateInstance (typeof (object), lengths);
						new MultiDimensionalIterator (a).Fill (() => ReadValue (br));
						return a;
				}
				case TypeCode.Double: {
						var a = Array.CreateInstance (typeof (double), lengths);
						new MultiDimensionalIterator (a).Fill (() => br.ReadDouble ());
						return a;
					}
				case TypeCode.Byte: {
						var a = Array.CreateInstance (typeof (byte), lengths);
						new MultiDimensionalIterator (a).Fill (() => br.ReadByte ());
						return a;
						//return br.ReadBytes (count);
					}
				case TypeCode.Int16: {
						var a = Array.CreateInstance (typeof (short), lengths);
						new MultiDimensionalIterator (a).Fill (() => br.ReadInt16 ());
						return a;
					}
				case TypeCode.Int32: {
						var a = Array.CreateInstance (typeof (int), lengths);
						new MultiDimensionalIterator (a).Fill (() => br.ReadInt32 ());
						return a;
					}
				case TypeCode.Int64: {
						var a = Array.CreateInstance (typeof (long), lengths);
						new MultiDimensionalIterator (a).Fill (() => br.ReadInt64 ());
						return a;
					}
				case TypeCode.Single: {
						var a = Array.CreateInstance (typeof (float), lengths);
						new MultiDimensionalIterator (a).Fill (() => br.ReadSingle ());
						return a;
					}
				case TypeCode.String: {
						var a = Array.CreateInstance (typeof (string), lengths);
						new MultiDimensionalIterator (a).Fill (() => {
							string s = br.ReadString ();
							if (s == NullString)
								s = null;
							return s;
						});
						return a;
					}
				case TypeCode.Boolean: {
						var a = Array.CreateInstance (typeof (bool), lengths);
						new MultiDimensionalIterator (a).Fill (() => br.ReadBoolean ());
						return a;
					}
				case TypeCode.DateTime: {
						var a = Array.CreateInstance (typeof (DateTime), lengths);
						new MultiDimensionalIterator (a).Fill (() => new DateTime (br.ReadInt64 ()));
						return a;
					}
				case TypeCode.TimeSpan: {
						var a = Array.CreateInstance (typeof (TimeSpan), lengths);
						new MultiDimensionalIterator (a).Fill (() => new TimeSpan (br.ReadInt64 ()));
						return a;
					}
				}
			}
			throw new NotSupportedException ("Array of " + type);
		}

		bool IsSerializableType (Type type)
		{
			return Type.GetTypeCode (type) == System.TypeCode.Object && !type.IsArray && !typeof(IDictionary).IsAssignableFrom (type);
		}
		
		public override string ToString ()
		{
			var sb = new StringBuilder ();
			foreach (var ar in args) {
				if (sb.Length > 0)
					sb.Append (", ");
				sb.Append (ar.Name).Append (":");
				AppendArg (sb, ar.Value);
			}
			return string.Format ("({3}) [{0} Target={1}, Args=[{2}]]", Name, Target, sb, Id);
		}

		void AppendArg (StringBuilder sb, object arg)
		{
			if (arg == null)
				sb.Append ("(null)");
			else if (arg is IDictionary) {
				sb.Append ("{");
				foreach (DictionaryEntry e in (IDictionary)arg) {
					if (sb.Length > 0)
						sb.Append (", ");
					sb.Append (e.Key).Append (":");
					AppendArg (sb, e.Value);
				}
				sb.Append ("}");
			} else if (arg is byte[])
				sb.Append ("byte[").Append (((byte[])arg).Length).Append ("]");
			else if (arg is Array) {
				sb.Append ("[");
				foreach (object e in (Array)arg) {
					if (sb.Length > 0)
						sb.Append (", ");
					AppendArg (sb, e);
				}
				sb.Append ("]");
			}
			else
				sb.Append (arg);
		}

		TypeMap GetTypeMap (Type type)
		{
			TypeMap map;
			if (dataTypes.TryGetValue (type, out map))
				return map;

			lock (dataTypesLock) {
				var newTypes = new Dictionary<Type, TypeMap> (dataTypes);
				if (!Attribute.IsDefined (type, typeof (MessageDataTypeAttribute))) {
					map = newTypes [type] = null;
				} else {
					map = new TypeMap ();
					foreach (var m in type.GetMembers (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
						var at = (MessageDataPropertyAttribute)Attribute.GetCustomAttribute (m, typeof(MessageDataPropertyAttribute));
						if (at == null || (!(m is PropertyInfo) && !(m is FieldInfo)))
							continue;
						map [at.Name ?? m.Name] = m;
					}
					newTypes [type] = map;
				}
				dataTypes = newTypes;
				return map;
			}
		}

		void ReadMessageData ()
		{
			TypeMap map = GetTypeMap (GetType ());
			if (map == null)
				throw new InvalidOperationException ("Type '" + GetType ().FullName + "' can't be read from message data. The type must have the [MessageDataType] attribute applied to it");

			foreach (var e in map) {
				var m = e.Value;
				object val = GetArgument (e.Key);
				if (m is PropertyInfo) {
					val = ConvertToType (val, ((PropertyInfo)m).PropertyType);
					((PropertyInfo)m).SetValue (this, val, null);
				} else {
					val = ConvertToType (val, ((FieldInfo)m).FieldType);
					((FieldInfo)m).SetValue (this, val);
				}
			}
		}

		object ReadMessageData (Type type, Dictionary<string, object> data)
		{
			TypeMap map = GetTypeMap (type);
			if (map == null)
				throw new InvalidOperationException ("Type '" + type.FullName + "' can't be read from message data. The type must have the [MessageDataType] attribute applied to it");

			var result = Activator.CreateInstance (type, true);
			foreach (var e in map) {
				var m = e.Value;
				object val;
				if (data.TryGetValue (e.Key, out val)) {
					if (m is PropertyInfo) {
						var prop = (PropertyInfo)m;
						val = ConvertToType (val, prop.PropertyType);
						prop.SetValue (result, val, null);
					} else {
						var field = (FieldInfo)m;
						val = ConvertToType (val, field.FieldType);
						field.SetValue (result, val);
					}
				}
			}
			return result;
		}

		object ConvertToType (object ob, Type type)
		{
			if (ob == null)
				return null;
			
			if (type.IsAssignableFrom (ob.GetType ()))
				return ob;
			
		    if (ob is Dictionary<string, object> && IsSerializableType (type))
				return ReadMessageData (type, (Dictionary<string, object>)ob);

			if (ob.GetType ().IsGenericType && ob.GetType().GetGenericTypeDefinition () == typeof(Dictionary<,>)) {
				if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof(Dictionary<,>)) {
					var targs = type.GetGenericArguments ();
					var keyType = targs [0];
					var elemType = targs [1];
					var dict = (IDictionary)Activator.CreateInstance (type);
					foreach (DictionaryEntry e in (IDictionary)ob) {
						var key = ConvertToType (e.Key, keyType);
						var val = ConvertToType (e.Value, elemType);
						dict [key] = val;
					}
					return dict;
				}
			}
			if (type.IsEnum && !ob.GetType ().IsEnum) {
				return Enum.ToObject (type, ob);
			}
			
			var array = ob as Array;
			if (array != null) {
				if (type.IsArray && ob.GetType ().GetElementType () != type.GetElementType ()) {
					// Array types are different. Convert!
					var targetType = type.GetElementType ();
					var newArray = Array.CreateInstance (targetType, array.Length);
					for (int n = 0; n < array.Length; n++) {
						var val = ConvertToType (array.GetValue (n), targetType);
						newArray.SetValue (val, n);
					}
					return newArray;
				}
			}
			return ob;
		}

		Dictionary<string, object> WriteMessageData (object instance)
		{
			TypeMap map = GetTypeMap (instance.GetType ());
			if (map == null)
				throw new InvalidOperationException ("Type '" + instance.GetType ().FullName + "' can't be serialized. The type must have the [MessageDataType] attribute applied to it");

			Dictionary<string, object> data = new Dictionary<string, object> ();
			foreach (var e in map) {
				var m = e.Value;
				object val;
				if (m is PropertyInfo)
					val = ((PropertyInfo)m).GetValue (instance, null);
				else
					val = ((FieldInfo)m).GetValue (instance);
				data [e.Key] = val;
			}
			return data;
		}

		class TypeMap: Dictionary<string,MemberInfo>
		{
		}
	}
	
	public class BinaryMessageArgument
	{
		public string Name { get; set; }
		public object Value { get; set; }
	}

	public class BinaryMessage<RT>: BinaryMessage where RT:BinaryMessage
	{
		internal protected override Type GetResponseType ()
		{
			return typeof (RT);
		}
	}

	public class MessageListener
	{
		Dictionary<string, MethodInfo> handlers = new Dictionary<string, MethodInfo> ();
		object target;

		public MessageListener ()
		{
			target = this;
			RegisterHandlers ();
		}

		internal MessageListener (object target)
		{
			this.target = target;
			RegisterHandlers ();
		}

		internal object Target {
			get {
				return target;
			}
		}

		public virtual string TargetId {
			get { return ""; }
		}

		void RegisterHandlers ()
		{
			foreach (var m in target.GetType ().GetMethods (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				var att = (MessageHandlerAttribute) Attribute.GetCustomAttribute (m, typeof (MessageHandlerAttribute));
				if (att != null) {
					var pars = m.GetParameters ();
					if (pars.Length != 1 || !typeof (BinaryMessage).IsAssignableFrom (pars [0].ParameterType))
						continue;
					var name = att.Name;
					if (name == null) {
						var ma = (MessageDataTypeAttribute) Attribute.GetCustomAttribute (pars [0].ParameterType, typeof (MessageDataTypeAttribute));
						if (ma != null)
							name = ma.Name;
						if (name == null) {
							if (pars [0].ParameterType != typeof (BinaryMessage))
								name = pars [0].ParameterType.FullName;
							else
								name = m.Name;
						}
					}
					handlers [name] = m;
				}
			}
		}

		internal BinaryMessage ProcessMessage (BinaryMessage msg)
		{
			MethodInfo m;
			if (handlers.TryGetValue (msg.Name, out m)) {
				var r = m.Invoke (target, new object [] { msg });
				if (r is BinaryMessage)
					return (BinaryMessage)r;
				else
					return msg.CreateResponse ();
			}
			return null;
		}

		internal Type [] GetMessageTypes ()
		{
			HashSet<Type> res = new HashSet<Type> ();
			foreach (var h in handlers.Values) {
				res.Add (h.GetParameters () [0].ParameterType);
			}
			return res.ToArray ();
		}
	}

	class RemoteProcessException: Exception
	{
		public string ExtendedDetails { get; set; }

		public RemoteProcessException ()
		{
		}

		public RemoteProcessException (string message, Exception wrappedException = null): base (message, wrappedException)
		{
			if (wrappedException != null)
				ExtendedDetails = wrappedException.ToString ();
		}

	}

	[AttributeUsage (AttributeTargets.Class)]
	public class MessageDataTypeAttribute : Attribute
	{
		public MessageDataTypeAttribute ()
		{
		}

		public MessageDataTypeAttribute (string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}

	[AttributeUsage (AttributeTargets.Property | AttributeTargets.Field)]
	public class MessageDataPropertyAttribute : Attribute
	{
		public MessageDataPropertyAttribute ()
		{
		}

		public MessageDataPropertyAttribute (string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}

	[AttributeUsage (AttributeTargets.Method)]
	public class MessageHandlerAttribute : Attribute
	{
		public MessageHandlerAttribute ()
		{
		}

		public MessageHandlerAttribute (string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}
}

