// 
// AddinSetupService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

using Mono.Addins;
using Mono.Addins.Setup;
using MonoDevelop.Core;

namespace MonoDevelop.Core.Setup
{
	public class AddinSetupService: SetupService
	{
		internal AddinSetupService (AddinRegistry r): base (r)
		{
		}
		
		public bool IsMainRepositoryRegistered (UpdateLevel level)
		{
			string url = GetMainRepositoryUrl (level);
			return Repositories.ContainsRepository (url);
		}
		
		public void RegisterMainRepository (UpdateLevel level, bool enable)
		{
			string url = GetMainRepositoryUrl (level);
			if (!Repositories.ContainsRepository (url)) {
				var rep = Repositories.RegisterRepository (null, url, false, AddinRepositoryType.MonoAddins);
				rep.Name = BrandingService.BrandApplicationName ("MonoDevelop Extension Repository");
				if (level != UpdateLevel.Stable)
					rep.Name += " (" + level + " channel)";
				if (!enable)
					Repositories.SetRepositoryEnabled (url, false);
			}
		}
		
		public string GetMainRepositoryUrl (UpdateLevel level)
		{
			string platform;
			if (Platform.IsWindows)
				platform = "Win32";
			else if (Platform.IsMac)
				platform = "Mac";
			else
				platform = "Linux";
			
			return "http://addins.monodevelop.com/" + level + "/" + platform + "/" + AddinManager.CurrentAddin.Version + "/main.mrep";
		}

		public void RegisterOfficalVisualStudioMarketplace ()
		{
			var url = "https://marketplace.visualstudio.com/";
			if (!Repositories.ContainsRepository (url)) {
				var rep = Repositories.RegisterRepository (null, url, false, AddinRepositoryType.VisualStudioMarketplace);
				rep.Name = GettextCatalog.GetString ("Visual Studio Marketplace");
			}
		}
	}
}
