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
using System.Linq;

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
		
		/// <summary>
		/// Alters configuration to make output name valid for iOS. 
		/// Should only be used when creating configurations.
		/// </summary>
		public void SanitizeAppName ()
		{
			if (string.IsNullOrEmpty (OutputAssembly))
				return;
			
			var sb = new StringBuilder (OutputAssembly.Length);
			foreach (var c in OutputAssembly)
				if (AppNameCharIsValid (c))
					sb.Append (c);
			OutputAssembly = sb.ToString ();
		}
		
		bool AppNameCharIsValid (char c)
		{
			return char.IsLetterOrDigit (c) || c == '_';
		}
		
		public bool IsValidAppName {
			get {
				return !string.IsNullOrEmpty (OutputAssembly) && OutputAssembly.All (AppNameCharIsValid);
			}
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
		
		//this has special serialization handling as a trigger for magically migrating settings
		public bool MtouchDebug { get; set; }
		
		[ItemProperty ("MtouchLink", DefaultValue = MtouchLinkMode.SdkOnly)]
		MtouchLinkMode mtouchLink = MtouchLinkMode.SdkOnly;
		public MtouchLinkMode MtouchLink {
			get { return mtouchLink; }
			set { mtouchLink = value; }
		}
		
		//for serialization
		[ItemProperty ("MtouchSdkVersion")]
		[MonoDevelop.Projects.Formats.MSBuild.MergeToProject]
		private string mtouchSdkVersion {
			get {
				return MtouchSdkVersion.IsUseDefault ? null : MtouchSdkVersion.ToString ();
			}	
			set {
				MtouchSdkVersion = IPhoneSdkVersion.UseDefault;
				if (value != null) {
					try {
						MtouchSdkVersion = IPhoneSdkVersion.Parse (value);
					} catch {
						LoggingService.LogWarning ("Discarding invalid SDK version '{0}'", value);
					}
				}
			}
		}
		
		public IPhoneSdkVersion MtouchSdkVersion { get; set; }
		
		[ItemProperty ("MtouchMinimumOS")]
		[MonoDevelop.Projects.Formats.MSBuild.MergeToProject]
		string mtouchMinimumOSVersion = "3.0";
		public string MtouchMinimumOSVersion {
			get { return mtouchMinimumOSVersion; }
			set {
				if (string.IsNullOrEmpty (value))
					value = "3.0";
				mtouchMinimumOSVersion = value;
			}
		}
		
		[ItemProperty ("MtouchExtraArgs")]
		public string MtouchExtraArgs { get; set; }
		
		[ItemProperty ("MtouchI18n", DefaultValue = null)]
		string mtouchI18n;
		public string MtouchI18n {
			get { return mtouchI18n; }
			set {
				if (string.IsNullOrEmpty (value))
					mtouchI18n = null;
				mtouchI18n = value;
			}
		}
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			var cfg = configuration as IPhoneProjectConfiguration;
			if (cfg == null)
				return;
			
			CodesignProvision = cfg.CodesignProvision;
			CodesignKey = cfg.CodesignKey;
			CodesignEntitlements = cfg.CodesignEntitlements;
			CodesignResourceRules = cfg.CodesignResourceRules;
			CodesignExtraArgs = cfg.CodesignExtraArgs;
			
			MtouchDebug = cfg.MtouchDebug;
			MtouchLink = cfg.MtouchLink;
			MtouchSdkVersion = cfg.MtouchSdkVersion;
			MtouchMinimumOSVersion = cfg.MtouchMinimumOSVersion;
			MtouchExtraArgs = cfg.MtouchExtraArgs;
			MtouchI18n = cfg.MtouchI18n;
		}
		
		//always set the MtouchDebug element
		public DataCollection Serialize (ITypeSerializer handler)
		{
			var collection = handler.Serialize (this);
			collection.Add (new DataValue ("MtouchDebug", MtouchDebug.ToString ()));
			return collection;
		}
		
		// if MtouchDebug element is not present, this handler migrates args
		// and sets default values for the new Mtouch* properties
		public void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			var argsToMigrate = data.Extract ("ExtraMtouchArgs") as DataValue;
			var mtouchDebugData = data.Extract ("MtouchDebug") as DataValue;
			handler.Deserialize (this, data);
			
			if (mtouchDebugData == null || string.IsNullOrEmpty (mtouchDebugData.Value)) {
				if (Name == "Debug") {
					MtouchDebug = true;
					if (Platform == IPhoneProject.PLAT_SIM)
						MtouchLink = MtouchLinkMode.None;
				}
				if (argsToMigrate != null && string.IsNullOrEmpty (argsToMigrate.Value)) {
					if (argsToMigrate.Value.Contains ("-debug"))
						MtouchDebug = true;
					if (argsToMigrate.Value.Contains ("-nolink"))
						MtouchLink = MtouchLinkMode.None;
					MtouchExtraArgs = new StringBuilder (argsToMigrate.Value)
						.Replace ("-nolink", "").Replace ("-linksdkonly", "").Replace ("-debug", "").Replace ("  ", " ")
						.ToString ();
				}
			} else {
				MtouchDebug = mtouchDebugData.Value.Equals ("true", StringComparison.OrdinalIgnoreCase);
			}
		}
	}
	
	public enum MtouchLinkMode
	{
		None = 0,
		SdkOnly,
		Full,
	}
}
