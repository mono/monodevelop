using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using CustomControls.OS;

namespace MonoDevelop.Platform
{
    class WinFormsRoot : Form
    {
        public WinFormsRoot()
        {
            this.Text = "";
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-32000, -32000);
            this.ShowInTaskbar = false;
            Show();
            //            Win32.SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, OpenFileDialogEx.UFLAGSHIDE);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)Msg.WM_ENTERIDLE)
                MonoDevelop.Ide.DispatchService.RunPendingEvents();

            base.WndProc(ref m);
        }
    }

    class WinFormsRunner
    {
        bool firstRun = true;
        EventHandler action;

        public void Run(EventHandler action)
        {
            this.action = action;
            Application.Idle += WinFormsIdle;
            Application.Run();
            Application.Idle -= WinFormsIdle;
        }

        void WinFormsIdle(object sender, EventArgs e)
        {
            if (firstRun)
            {
                firstRun = false;
                action(null, null);
            }
            else
                MonoDevelop.Ide.DispatchService.RunPendingEvents();
        }
    }
}
