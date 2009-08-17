// AboutMonoDevelopTabPage.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//

using System;
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class AboutMonoDevelopTabPage: VBox
	{
        public AboutMonoDevelopTabPage ()
        {
            Label label = new Label();
            label.Markup = String.Format (
                "<b>{0}</b>\n    {1}", 
                GettextCatalog.GetString ("Version"), 
                BuildVariables.PackageVersion == BuildVariables.PackageVersionLabel ? BuildVariables.PackageVersionLabel : String.Format ("{0} ({1})", 
                BuildVariables.PackageVersionLabel, 
                BuildVariables.PackageVersion));
            HBox hBoxVersion = new HBox ();
            hBoxVersion.PackStart (label, false, false, 5);
            this.PackStart (hBoxVersion, false, true, 0);

            label = null;
            label = new Label ();
            label.Markup = GettextCatalog.GetString ("<b>License</b>\n    {0}", GettextCatalog.GetString ("Released under the GNU General Public license."));
            HBox hBoxLicense = new HBox ();
            hBoxLicense.PackStart (label, false, false, 5);
            this.PackStart (hBoxLicense, false, true, 5);

            label = null;
            label = new Label ();
            label.Markup = GettextCatalog.GetString ("<b>Copyright</b>\n    (c) 2000-2003 by icsharpcode.net\n    (c) 2004-{0} by MonoDevelop contributors", 2009);
            HBox hBoxCopyright = new HBox ();
            hBoxCopyright.PackStart (label, false, false, 5);
            this.PackStart (hBoxCopyright, false, true, 5);

            this.ShowAll ();
        }
	}
}
