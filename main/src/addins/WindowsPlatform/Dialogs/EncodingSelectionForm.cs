// 
// EncodingSelectionForm.cs
//  
// Author:
//       Carlos Alberto Cortez <calberto.cortez@gmail.com>
// 
// Copyright (c) 2011 Carlos Alberto Cortez
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MonoDevelop.Core;
using MonoDevelop.Projects.Text;

using CustomControls.OS;

namespace MonoDevelop.Platform
{
    public partial class EncodingSelectionForm : Form
    {
        public EncodingSelectionForm ()
        {
            InitializeComponent ();
			Populate ();
        }
		
		void Populate ()
		{			
			var availableEncodings = new Dictionary<string,TextEncoding> ();
			foreach (var encoding in TextEncoding.SupportedEncodings)
				availableEncodings [encoding.Id] = encoding;
			
			var shownEncodings = TextEncoding.ConversionEncodings;
			
			shownListView.BeginUpdate ();
			foreach (var encoding in shownEncodings) {
				var item = new ListViewItem (new string [] { encoding.Id, encoding.Name }) {
					Tag = encoding
				};
				shownListView.Items.Add (item);
				
				// Don't show on the available list the encodings
				// that are already being shown
				availableEncodings.Remove (encoding.Id);
			}
			shownListView.AutoResizeColumns (ColumnHeaderAutoResizeStyle.HeaderSize);
			shownListView.EndUpdate ();
			
			availableListView.BeginUpdate ();
			foreach (var encoding in availableEncodings) {
				var item = new ListViewItem (new string [] { encoding.Value.Id, encoding.Value.Name }) {
					Tag = encoding.Value
				};
				availableListView.Items.Add (item);
			}
			availableListView.AutoResizeColumns (ColumnHeaderAutoResizeStyle.HeaderSize);
			availableListView.EndUpdate ();
		}
		
		public TextEncoding [] SelectedEncodings {
			get {
				var encodings = shownListView.Items.Cast<ListViewItem> ().Select (item => (TextEncoding)item.Tag);
				return encodings.ToArray ();
			}
		}

        void MoveItem (ListView srcView, ListView destView)
        {
            if (srcView.SelectedIndices.Count == 0)
                return;

            int selectedIndex = srcView.SelectedIndices [0];
            var item = srcView.Items [selectedIndex];

            srcView.Items.RemoveAt (selectedIndex);
            destView.Items.Add (item);
			destView.AutoResizeColumns (ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void addButtonClick (object sender, EventArgs e)
        {
            MoveItem (availableListView, shownListView);
        }

        private void removeButtonClick (object sender, EventArgs e)
        {
            MoveItem (shownListView, availableListView);
        }

        void ShiftItem (ListView listView, int shift)
        {
            if (listView.SelectedIndices.Count == 0)
                return;

            int selectedIndex = listView.SelectedIndices[0];
            int newIndex = selectedIndex + shift;
            if (newIndex < 0 || newIndex >= listView.Items.Count)
                return;

            listView.BeginUpdate ();

            var item = listView.Items[selectedIndex];
            listView.Items.RemoveAt (selectedIndex);
            listView.Items.Insert (newIndex, item);
            item.Selected = true;

            listView.EndUpdate ();
        }

        private void upButtonClick (object sender, EventArgs e)
        {
            ShiftItem (shownListView, -1);
        }

        private void downButtonClick (object sender, EventArgs e)
        {
            ShiftItem (shownListView, 1);
        }

        private void okButtonClick (object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void cancelButtonClick (object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
		
		protected override void WndProc (ref Message m)
		{
			base.WndProc (ref m);
			
			switch (m.Msg) {
				// No need to take into account the idle event, as
				// we are handling it already from WinFormsRoot
				case (int) Msg.WM_WINDOWPOSCHANGED:
					MonoDevelop.Ide.DispatchService.RunPendingEvents ();
					break;
			}
		}
    }

    public class EncodingListView : ListView
    {
        public EncodingListView ()
        {
            SuspendLayout ();

            View = View.Details;
            MultiSelect = false;
			FullRowSelect = true;
            HideSelection = false;

            Columns.Add ("Name");
            Columns.Add ("Encoding");

            ResumeLayout ();
        }
    }
}
