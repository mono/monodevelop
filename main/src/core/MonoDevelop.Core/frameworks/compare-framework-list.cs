using System.Xml.Linq;
using System;
using System.Linq;
using System.Collections.Generic;

class CompareFrameworkList
{
	// compares a MSBuild framework list to a MonoDevelop framework list
	static void Main (string[] args)
	{
		var netHash = LoadMSBuildFrameworkList (args[0]);
		var monoHash = LoadMDFrameworkList (args [1]);
		Console.WriteLine ("MONOONLY");
		foreach (var s in monoHash.Except (netHash))
			Console.WriteLine ("  {0}", s);
		Console.WriteLine ("NETONLY");
		foreach (var s in netHash.Except (monoHash))
			Console.WriteLine ("  {0}", s);
	}

	HashSet<string> LoadMSBuildFrameworkList (string file)
	{
		var doc = XDocument.Load (file);
		var hash = new HashSet<string> ();
		foreach (var el in doc.Elements ("File")) {
			string name = (string)el.Attribute ("AssemblyName") + ","
				+ (string)el.Attribute ("Version") + ","
					+ (string)el.Attribute ("PublicKeyToken");
			if (!hash.Add (name))
				Console.WriteLine ("NETDUP {0}", name);
		}
		return hash;
	}

	HashSet<string> LoadMDFrameworkList (string file)
	{
		var doc = XDocument.Load (file);
		var hash = new HashSet<string> ();
		foreach (var el in doc.Root.Element ("Assemblies").Elements ("Assembly")) {
			string name = (string)el.Attribute ("name") + ","
				+ (string)el.Attribute ("version") + "," 
					+ (string)el.Attribute ("publicKeyToken");
			if (!hash.Add (name))
				Console.WriteLine ("MONODUP {0}", name);
		}
		return hash;
	}
}
