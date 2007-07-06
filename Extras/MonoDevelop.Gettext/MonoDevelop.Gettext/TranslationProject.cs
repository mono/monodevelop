//
// TranslationProject.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Gettext
{
	public class TranslationProject : CombineEntry
	{
		[ItemProperty]
		List<Translation> translations = new List<Translation> ();
		
		public ReadOnlyCollection<Translation> Translations {
			get { return translations.AsReadOnly (); }
		}
		
		public TranslationProject()
		{
		}
		
		string GetFileName (string isoCode)
		{
			return Path.Combine (base.BaseDirectory, isoCode + ".po");
		}
		
		public void AddNewTranslation (string isoCode, IProgressMonitor monitor)
		{
			try {
				translations.Add (new Translation (isoCode));
				File.WriteAllText (GetFileName (isoCode), "");
				monitor.ReportSuccess (String.Format (GettextCatalog.GetString ("Language '{0}' successfully added."), isoCode));
				monitor.Step (1);
				this.Save (monitor);
				OnTranslationAdded (EventArgs.Empty);
			} catch (Exception e) {
				monitor.ReportError (String.Format ( GettextCatalog.GetString ("Language '{0}' could not be added: "), isoCode), e);
			} finally {
				monitor.EndTask ();
			}
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			return new TranslationProjectConfiguration (name);
		}
		
		protected override void OnClean (IProgressMonitor monitor)
		{
		}
		
		protected override ICompilerResult OnBuild (IProgressMonitor monitor)
		{
			return null;
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context)
		{
		}
		
		protected override bool OnGetNeedsBuilding ()
		{
			return false;
		}
		
		protected override void OnSetNeedsBuilding (bool val)
		{
		}
		
		protected virtual void OnTranslationAdded (EventArgs e)
		{
			if (TranslationAdded != null)
				TranslationAdded (this, e);
		}
		
		public event EventHandler TranslationAdded;
	}
	
	public class TranslationProjectConfiguration : IConfiguration
	{
		[ItemProperty("name")]
		string name = null;
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public TranslationProjectConfiguration ()
		{
		}
		
		public TranslationProjectConfiguration (string name)
		{
			this.name = name;
		}

		public object Clone ()
		{
			IConfiguration conf = (IConfiguration) MemberwiseClone ();
			conf.CopyFrom (this);
			return conf;
		}
		
		public virtual void CopyFrom (IConfiguration configuration)
		{
		}
	}
}
