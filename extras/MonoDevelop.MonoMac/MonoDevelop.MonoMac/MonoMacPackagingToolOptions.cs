// 
// MonoMacPackagingToolOptions.cs
//  
// Author:
//       David Siegel <djsiegel@gmail.com>
// 
// Copyright (c) 2011 David Siegel
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
using System.Collections.Generic;

using Mono.Options;

using MonoDevelop.MonoMac.Gui;

namespace MonoDevelop.MonoMac
{
	public class MonoMacPackagingToolOptions
	{
		public enum ParseResult
		{
			Failure,
			Success
		}

		const string DefaultConfiguration = "Release";

		public bool ShowHelp { get; private set; }
		public string Configuration { get; private set; }
		public MonoMacPackagingSettings PackagingSettings { get; private set; }

		public string ParseFailureMessage { get; private set; }

		readonly OptionSet Options;
		readonly IEnumerable<string> Arguments;

		string ConfigurationOptionHelpText {
			get {
				var configs = "Release, Debug, ...";
				var configsWithDefault = configs.Replace (DefaultConfiguration, "[" + DefaultConfiguration + "]");
				
				return "Configuration to bundle (" + configsWithDefault + ").";
			}
		}

		string LinkerModeOptionHelpText {
			get {
				var settings = GetDefaultPackagingSettings ();
				var defaultMode = Enum.GetName (typeof(MonoMacLinkerMode), settings.LinkerMode);
				var linkerModes = string.Join (", ", Enum.GetNames (typeof(MonoMacLinkerMode)));
				var linkerModesWithDefault = linkerModes.Replace (defaultMode, "[" + defaultMode + "]");
				
				return "Linker mode (" + linkerModesWithDefault + ").";
			}
		}

		public MonoMacPackagingToolOptions (string[] args)
		{
			Arguments = args;
			Configuration = DefaultConfiguration;
			PackagingSettings = GetDefaultPackagingSettings ();
            
			Options = new OptionSet {
				{ "i|include-mono", "Include Mono in the bundle.", v => {
					PackagingSettings.IncludeMono = v != null;
				}},
				{ "l|linker-mode=", LinkerModeOptionHelpText, v => {
					MonoMacLinkerMode mode;
					if (Enum.TryParse<MonoMacLinkerMode> (v, out mode))
						PackagingSettings.LinkerMode = mode;
				}},
				{ "b|sign-bundle=", "Sign bundle with specified key.", v => {
					PackagingSettings.SignBundle = v != null;
					PackagingSettings.BundleSigningKey = v;
				}},
				{ "p|sign-package=", "Sign package with specified key.", v => {
					PackagingSettings.SignPackage = v != null;
					PackagingSettings.PackageSigningKey = v;
				}},
				{ "c|configuration=", ConfigurationOptionHelpText, v => {
					if (v != null) Configuration = v;
				}},
				{ "k|create-package", "Create bundle package/installer.", v => {
					PackagingSettings.CreatePackage = v != null;
				}},
				{ "d|product-definition=", "Product definition.", v => {
					PackagingSettings.ProductDefinition = v;
				}},
				{ "h|help", "Show bundle tool help.", v => {
					ShowHelp = v != null;
				}}
			};
		}

		MonoMacPackagingSettings GetDefaultPackagingSettings ()
		{
			return new MonoMacPackagingSettings {
				IncludeMono = false,
				LinkerMode = MonoMacLinkerMode.LinkNone,
				SignBundle = false,
				SignPackage = false,
				CreatePackage = false
			};
		}

		public ParseResult Parse ()
		{
			try {
				Options.Parse (Arguments);
			} catch (OptionException e) {
				ParseFailureMessage = e.Message;
				return ParseResult.Failure;
			}
			return ParseResult.Success;
		}

		public void Show ()
		{
			Console.WriteLine ("Options:");
			Options.WriteOptionDescriptions (Console.Out);
		}
	}
}

