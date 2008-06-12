using System;
using System.Collections.Generic;
using System.IO;

using CA = MonoDevelop.CodeAnalysis;
using GF = Gendarme.Framework;
using Mono.Cecil;

namespace MonoDevelop.CodeAnalysis.Gendarme {
	
	public class GendarmeRunner : GF.Runner, CA.IRunner {
		
		public string Id {
			get { return "GendarmeRunner"; }
		}

		public string Name {
			get { return "Gendarme"; }
		}
		
		public GendarmeRunner ()
		{
			base.IgnoreList = new GF.BasicIgnoreList (this);
		}

		public IEnumerable<IViolation> Run (string inspectedFile, IEnumerable<CA.IRule> ruleSet)
		{
			if (!File.Exists (inspectedFile))
				throw new ArgumentException (AddinCatalog.GetString ("File does not exist: '{0}'.", inspectedFile),
				                             "inspectedFile");
			
			// assemblies
			base.Assemblies.Clear ();
			AssemblyDefinition ad = AssemblyFactory.GetAssembly (inspectedFile);
			base.Assemblies.Add (ad);

			// rules
			base.Rules.Clear ();
			foreach (CA.IRule rule in ruleSet)
				base.Rules.Add (((GendarmeRule) rule).InternalRule);
			
			// defects
			base.Reset ();
			
			base.Initialize ();
			base.Run ();
			
			foreach (GF.Defect def in base.Defects)
				yield return new GendarmeViolation (def);
		}
	}
}
