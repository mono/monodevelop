// 
// IPhoneProjectConfiguration.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.IO;
using System.Text;

namespace MonoDevelop.IPhone
{
	
	
	public class IPhoneProjectConfiguration : DotNetProjectConfiguration, ICustomDataItem
	{
		public IPhoneProjectConfiguration () : base ()
		{
		}
		
		public IPhoneProjectConfiguration (string name) : base (name)
		{
		}
		
		public FilePath AppDirectory {
			get {
				IPhoneProject proj = ParentItem as IPhoneProject;
				if (proj != null)
					return OutputDirectory.Combine (proj.Name + ".app");
				return FilePath.Null;
			}
		}
		
		public FilePath NativeExe {
			get {
				IPhoneProject proj = ParentItem as IPhoneProject;
				if (proj != null)
					return OutputDirectory.Combine (proj.Name + ".app")
						.Combine (Path.GetFileNameWithoutExtension (OutputAssembly));
				return null;
			}
		}
				
		[ItemProperty ("CodesignProvision")]
		public string CodesignProvision { get; set; }
		
		[ItemProperty ("CodesignKey")]
		public string CodesignKey { get; set; }
		
		[ProjectPathItemProperty ("CodesignEntitlements")]
		string codesignEntitlements;
		public FilePath CodesignEntitlements {
			get { return codesignEntitlements; }
			set { codesignEntitlements = value; }
		}
		
		[ProjectPathItemProperty ("CodesignResourceRules")]
		string codesignResourceRules;
		public FilePath CodesignResourceRules {
			get { return codesignResourceRules; }
			set { codesignResourceRules = value; }
		}
		
		[ItemProperty ("CodesignExtraArgs")]
		public string CodesignExtraArgs { get; set; }
		
		[ItemProperty ("MtouchDebug")]
		public bool MtouchDebug { get; set; }
		
		[ItemProperty ("MtouchLink", DefaultValue = MtouchLinkMode.SdkOnly)]
		MtouchLinkMode mtouchLink = MtouchLinkMode.SdkOnly;
		public MtouchLinkMode MtouchLink {
			get { return mtouchLink; }
			set { mtouchLink = value; }
		}
		
		[ItemProperty ("MtouchSdkVersion", DefaultValue = "3.0")]
		[MonoDevelop.Projects.Formats.MSBuild.MergeToProject]
		string mtouchSdkVersion = "3.0";
		public string MtouchSdkVersion {
			get { return mtouchSdkVersion; }
			set {
				if (string.IsNullOrEmpty (value))
					value = "3.0";
				mtouchSdkVersion = value;
			}
		}
		
		[ItemProperty ("MtouchExtraArgs")]
		public string MtouchExtraArgs { get; set; }
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			var cfg = (IPhoneProjectConfiguration) configuration;
			base.CopyFrom (configuration);
			
			CodesignProvision = cfg.CodesignProvision;
			CodesignKey = cfg.CodesignKey;
			CodesignEntitlements = cfg.CodesignEntitlements;
			CodesignResourceRules = cfg.CodesignResourceRules;
			CodesignExtraArgs = cfg.CodesignExtraArgs;
			
			MtouchExtraArgs = cfg.MtouchExtraArgs;
			MtouchDebug = cfg.MtouchDebug;
			MtouchLink = cfg.MtouchLink;
		}
		
		void MigrateExtraMtouchArgs (string value)
		{
			if (string.IsNullOrEmpty (value))
					return;
			
			if (value.Contains ("-debug") || Name == "Debug")
				MtouchDebug = true;
			
			if (value.Contains ("-nolink") || (Name == "Debug" && Platform == IPhoneProject.PLAT_SIM))
				MtouchLink = MtouchLinkMode.None;
			
			var sb = new StringBuilder (value);
			sb.Replace ("-nolink", "");
			sb.Replace ("-linksdkonly", "");
			sb.Replace ("-debug", "");
			sb.Replace ("  ", " ");
			MtouchExtraArgs = sb.ToString ();
		}
		
		public DataCollection Serialize (ITypeSerializer handler)
		{
			return handler.Serialize (this);
		}
		
		public void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			var argsToMigrate = data.Extract ("ExtraMtouchArgs") as DataValue;
			handler.Deserialize (this, data);
			if (argsToMigrate != null)
				MigrateExtraMtouchArgs (argsToMigrate.Value);
		}
	}
	
	public enum MtouchLinkMode
	{
		None = 0,
		Full,
		SdkOnly,
	}
}
