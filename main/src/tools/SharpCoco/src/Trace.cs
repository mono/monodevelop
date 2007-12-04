using System;
using System.IO;

namespace at.jku.ssw.Coco {

class Trace {
	const string FILENAME = "trace.txt";
	static StreamWriter trace;
	
	public static void Init (String dir) {
		FileStream s;
		try {
			s = new FileStream(dir + FILENAME, FileMode.Create);  /* AW use FILENAME */
			trace = new StreamWriter(s);
		} catch (IOException) {
			Errors.Exception("-- could not open trace file");
		}
	}

	public static void Write (string s) { trace.Write(s); }

	public static void Write (string s, params object[] args) {
		trace.Write(s, args);
	}

	public static void WriteLine (string s) { trace.WriteLine(s); }
	
	public static void WriteLine (string s, params object[] args) {
		trace.WriteLine(s, args);
	}
	
	public static void WriteLine () { trace.WriteLine(); }
	
	public static void Close () { trace.Close(); }
}

}
