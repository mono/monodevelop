// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Windows.Forms;
using System.Globalization;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

namespace D2DPaint
{
    public partial class TextDialog : Form
    {
        private Paint2DForm parent;
        #region TextLayout
        private TextLayout textLayout;
        internal TextLayout TextLayout
        {
            get
            {
                TextFormat textFormat = parent.dwriteFactory.CreateTextFormat(
                    fontFamilyCombo.Text,
                    float.Parse(sizeCombo.Text),
                    (FontWeight)((weightCombo.SelectedIndex + 1) * 100),
                    (FontStyle)styleCombo.SelectedIndex,
                    (FontStretch)stretchCombo.SelectedIndex, CultureInfo.CurrentUICulture);
                textLayout = parent.dwriteFactory.CreateTextLayout(textBox.Text, textFormat, 100, 100);
                if (underLineCheckBox.Checked)
                {
                    textLayout.SetUnderline(true, new TextRange(0, (uint)textBox.Text.Length));
                }
                if (strikethroughCheckBox.Checked)
                {
                    textLayout.SetStrikethrough(true, new TextRange(0, (uint)textBox.Text.Length));
                }
                return textLayout;
            }
            set { textLayout = value; }
        } 
        #endregion

        public TextDialog(Paint2DForm parent)
        {
            InitializeComponent();

            this.parent = parent;

            if (!DesignMode)
            {
                fontFamilyCombo.Initialize();
            }
            fontFamilyCombo.SelectedIndex = 0; // First Choice
            sizeCombo.SelectedIndex = 7; // 24.0
            weightCombo.SelectedIndex = 3; // Normal
            styleCombo.SelectedIndex = 0; // Normal
            stretchCombo.SelectedIndex = 5; // Normal
        }

        private void AddTextButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
