namespace D2DPaint
{
    partial class BrushDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Label gammaLabel;
            System.Windows.Forms.Label label6;
            System.Windows.Forms.Label label5;
            System.Windows.Forms.Label label7;
            this.transparencyValues = new System.Windows.Forms.ComboBox();
            this.transparency = new System.Windows.Forms.Label();
            this.solidColorButton = new System.Windows.Forms.Button();
            this.colorLabel = new System.Windows.Forms.Label();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.addBrushButton = new System.Windows.Forms.Button();
            this.brushesTabs = new System.Windows.Forms.TabControl();
            this.solidColorPage = new System.Windows.Forms.TabPage();
            this.bitmapBrushPage = new System.Windows.Forms.TabPage();
            this.imageFileLabel = new System.Windows.Forms.Label();
            this.extendedModeYComboBox = new System.Windows.Forms.ComboBox();
            this.extendedModeXComboBox = new System.Windows.Forms.ComboBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.addBitmapBrushBotton = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.linearBrushPage = new System.Windows.Forms.TabPage();
            this.button3 = new System.Windows.Forms.Button();
            this.gradBrushExtendModeCombo = new System.Windows.Forms.ComboBox();
            this.gammaComboBox = new System.Windows.Forms.ComboBox();
            this.gradBrushColor2Label = new System.Windows.Forms.Label();
            this.gradBrushColor1Label = new System.Windows.Forms.Label();
            this.gradiantBrushColor2Button = new System.Windows.Forms.Button();
            this.gradiantBrushColor1button = new System.Windows.Forms.Button();
            this.graidantBrushPage = new System.Windows.Forms.TabPage();
            this.button4 = new System.Windows.Forms.Button();
            this.radialExtendCombo = new System.Windows.Forms.ComboBox();
            this.radialGammaCombo = new System.Windows.Forms.ComboBox();
            this.radialBrushColor2Label = new System.Windows.Forms.Label();
            this.radialBrushColor1Label = new System.Windows.Forms.Label();
            this.SelectRadialColor2 = new System.Windows.Forms.Button();
            this.SelectRadialColor1 = new System.Windows.Forms.Button();
            this.brushesList = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            gammaLabel = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            this.brushesTabs.SuspendLayout();
            this.solidColorPage.SuspendLayout();
            this.bitmapBrushPage.SuspendLayout();
            this.linearBrushPage.SuspendLayout();
            this.graidantBrushPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // gammaLabel
            // 
            gammaLabel.AutoSize = true;
            gammaLabel.Location = new System.Drawing.Point(16, 97);
            gammaLabel.Name = "gammaLabel";
            gammaLabel.Size = new System.Drawing.Size(43, 13);
            gammaLabel.TabIndex = 4;
            gammaLabel.Text = "Gamma";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(16, 128);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(70, 13);
            label6.TabIndex = 5;
            label6.Text = "Extend Mode";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(43, 136);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(70, 13);
            label5.TabIndex = 13;
            label5.Text = "Extend Mode";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(43, 105);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(43, 13);
            label7.TabIndex = 12;
            label7.Text = "Gamma";
            // 
            // transparencyValues
            // 
            this.transparencyValues.FormattingEnabled = true;
            this.transparencyValues.Items.AddRange(new object[] {
            "0.00",
            "0.10",
            "0.25",
            "0.40",
            "0.50",
            "0.60",
            "0.75",
            "0.90",
            "0.95",
            "1.00"});
            this.transparencyValues.Location = new System.Drawing.Point(161, 67);
            this.transparencyValues.Name = "transparencyValues";
            this.transparencyValues.Size = new System.Drawing.Size(121, 21);
            this.transparencyValues.TabIndex = 3;
            this.transparencyValues.Text = "1.00";
            this.transparencyValues.SelectedIndexChanged += new System.EventHandler(this.transparencyValues_SelectedIndexChanged);
            // 
            // transparency
            // 
            this.transparency.AutoSize = true;
            this.transparency.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.transparency.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.transparency.Location = new System.Drawing.Point(24, 65);
            this.transparency.MinimumSize = new System.Drawing.Size(113, 23);
            this.transparency.Name = "transparency";
            this.transparency.Size = new System.Drawing.Size(113, 23);
            this.transparency.TabIndex = 2;
            this.transparency.Text = "Transparency";
            // 
            // solidColorButton
            // 
            this.solidColorButton.Location = new System.Drawing.Point(24, 32);
            this.solidColorButton.Name = "solidColorButton";
            this.solidColorButton.Size = new System.Drawing.Size(113, 23);
            this.solidColorButton.TabIndex = 1;
            this.solidColorButton.Text = "Select Color...";
            this.solidColorButton.UseVisualStyleBackColor = true;
            this.solidColorButton.Click += new System.EventHandler(this.SelectColorClick);
            // 
            // colorLabel
            // 
            this.colorLabel.AutoSize = true;
            this.colorLabel.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.colorLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.colorLabel.Location = new System.Drawing.Point(161, 32);
            this.colorLabel.MinimumSize = new System.Drawing.Size(130, 23);
            this.colorLabel.Name = "colorLabel";
            this.colorLabel.Size = new System.Drawing.Size(130, 23);
            this.colorLabel.TabIndex = 0;
            this.colorLabel.Text = "R = 0, G = 0, B = 0, A = 1";
            // 
            // addBrushButton
            // 
            this.addBrushButton.Location = new System.Drawing.Point(114, 193);
            this.addBrushButton.Name = "addBrushButton";
            this.addBrushButton.Size = new System.Drawing.Size(75, 23);
            this.addBrushButton.TabIndex = 2;
            this.addBrushButton.Text = "Add Brush";
            this.addBrushButton.UseVisualStyleBackColor = true;
            this.addBrushButton.Click += new System.EventHandler(this.addBrushButton_Click);
            // 
            // brushesTabs
            // 
            this.brushesTabs.Controls.Add(this.solidColorPage);
            this.brushesTabs.Controls.Add(this.bitmapBrushPage);
            this.brushesTabs.Controls.Add(this.linearBrushPage);
            this.brushesTabs.Controls.Add(this.graidantBrushPage);
            this.brushesTabs.Location = new System.Drawing.Point(12, 12);
            this.brushesTabs.Name = "brushesTabs";
            this.brushesTabs.SelectedIndex = 0;
            this.brushesTabs.Size = new System.Drawing.Size(361, 248);
            this.brushesTabs.TabIndex = 4;
            // 
            // solidColorPage
            // 
            this.solidColorPage.BackColor = System.Drawing.SystemColors.Control;
            this.solidColorPage.Controls.Add(this.transparencyValues);
            this.solidColorPage.Controls.Add(this.transparency);
            this.solidColorPage.Controls.Add(this.addBrushButton);
            this.solidColorPage.Controls.Add(this.colorLabel);
            this.solidColorPage.Controls.Add(this.solidColorButton);
            this.solidColorPage.Location = new System.Drawing.Point(4, 22);
            this.solidColorPage.Name = "solidColorPage";
            this.solidColorPage.Padding = new System.Windows.Forms.Padding(3);
            this.solidColorPage.Size = new System.Drawing.Size(353, 222);
            this.solidColorPage.TabIndex = 0;
            this.solidColorPage.Text = "Solid Color";
            // 
            // bitmapBrushPage
            // 
            this.bitmapBrushPage.BackColor = System.Drawing.SystemColors.Control;
            this.bitmapBrushPage.Controls.Add(this.imageFileLabel);
            this.bitmapBrushPage.Controls.Add(this.extendedModeYComboBox);
            this.bitmapBrushPage.Controls.Add(this.extendedModeXComboBox);
            this.bitmapBrushPage.Controls.Add(this.comboBox2);
            this.bitmapBrushPage.Controls.Add(this.label4);
            this.bitmapBrushPage.Controls.Add(this.label3);
            this.bitmapBrushPage.Controls.Add(this.comboBox1);
            this.bitmapBrushPage.Controls.Add(this.label2);
            this.bitmapBrushPage.Controls.Add(this.label1);
            this.bitmapBrushPage.Controls.Add(this.addBitmapBrushBotton);
            this.bitmapBrushPage.Controls.Add(this.button2);
            this.bitmapBrushPage.Location = new System.Drawing.Point(4, 22);
            this.bitmapBrushPage.Name = "bitmapBrushPage";
            this.bitmapBrushPage.Padding = new System.Windows.Forms.Padding(3);
            this.bitmapBrushPage.Size = new System.Drawing.Size(353, 222);
            this.bitmapBrushPage.TabIndex = 1;
            this.bitmapBrushPage.Text = "Bitmap";
            // 
            // imageFileLabel
            // 
            this.imageFileLabel.AutoSize = true;
            this.imageFileLabel.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.imageFileLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.imageFileLabel.Location = new System.Drawing.Point(158, 29);
            this.imageFileLabel.MinimumSize = new System.Drawing.Size(130, 23);
            this.imageFileLabel.Name = "imageFileLabel";
            this.imageFileLabel.Size = new System.Drawing.Size(130, 23);
            this.imageFileLabel.TabIndex = 7;
            // 
            // extendedModeYComboBox
            // 
            this.extendedModeYComboBox.FormattingEnabled = true;
            this.extendedModeYComboBox.Items.AddRange(new object[] {
            "Clamp",
            "Wrap",
            "Mirror"});
            this.extendedModeYComboBox.Location = new System.Drawing.Point(158, 152);
            this.extendedModeYComboBox.Name = "extendedModeYComboBox";
            this.extendedModeYComboBox.Size = new System.Drawing.Size(121, 21);
            this.extendedModeYComboBox.TabIndex = 6;
            this.extendedModeYComboBox.Text = "Mirror";
            // 
            // extendedModeXComboBox
            // 
            this.extendedModeXComboBox.FormattingEnabled = true;
            this.extendedModeXComboBox.Items.AddRange(new object[] {
            "Clamp",
            "Wrap",
            "Mirror"});
            this.extendedModeXComboBox.Location = new System.Drawing.Point(158, 114);
            this.extendedModeXComboBox.Name = "extendedModeXComboBox";
            this.extendedModeXComboBox.Size = new System.Drawing.Size(121, 21);
            this.extendedModeXComboBox.TabIndex = 6;
            this.extendedModeXComboBox.Text = "Mirror";
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Items.AddRange(new object[] {
            "0.00",
            "0.10",
            "0.25",
            "0.40",
            "0.50",
            "0.60",
            "0.75",
            "0.90",
            "0.95",
            "1.00"});
            this.comboBox2.Location = new System.Drawing.Point(158, 77);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(121, 21);
            this.comboBox2.TabIndex = 6;
            this.comboBox2.Text = "1.00";
            this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.OpacityButtonClicked);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label4.Location = new System.Drawing.Point(21, 150);
            this.label4.MinimumSize = new System.Drawing.Size(113, 23);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(113, 23);
            this.label4.TabIndex = 5;
            this.label4.Text = "Extend Mode Y";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label3.Location = new System.Drawing.Point(21, 112);
            this.label3.MinimumSize = new System.Drawing.Size(113, 23);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 23);
            this.label3.TabIndex = 5;
            this.label3.Text = "Extend Mode X";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "0.00",
            "0.10",
            "0.25",
            "0.40",
            "0.50",
            "0.60",
            "0.75",
            "0.90",
            "0.95",
            "1.00"});
            this.comboBox1.Location = new System.Drawing.Point(158, 77);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 21);
            this.comboBox1.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label2.Location = new System.Drawing.Point(21, 75);
            this.label2.MinimumSize = new System.Drawing.Size(113, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(113, 23);
            this.label2.TabIndex = 5;
            this.label2.Text = "Transparency";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label1.Location = new System.Drawing.Point(21, 75);
            this.label1.MinimumSize = new System.Drawing.Size(113, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(113, 23);
            this.label1.TabIndex = 5;
            this.label1.Text = "Transparency";
            // 
            // addBitmapBrushBotton
            // 
            this.addBitmapBrushBotton.Location = new System.Drawing.Point(121, 193);
            this.addBitmapBrushBotton.Name = "addBitmapBrushBotton";
            this.addBitmapBrushBotton.Size = new System.Drawing.Size(75, 23);
            this.addBitmapBrushBotton.TabIndex = 4;
            this.addBitmapBrushBotton.Text = "Add Brush";
            this.addBitmapBrushBotton.UseVisualStyleBackColor = true;
            this.addBitmapBrushBotton.Click += new System.EventHandler(this.addBitmapBrushBotton_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(21, 29);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(130, 23);
            this.button2.TabIndex = 0;
            this.button2.Text = "Select Image File...";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // linearBrushPage
            // 
            this.linearBrushPage.BackColor = System.Drawing.SystemColors.Control;
            this.linearBrushPage.Controls.Add(this.button3);
            this.linearBrushPage.Controls.Add(this.gradBrushExtendModeCombo);
            this.linearBrushPage.Controls.Add(this.gammaComboBox);
            this.linearBrushPage.Controls.Add(label6);
            this.linearBrushPage.Controls.Add(gammaLabel);
            this.linearBrushPage.Controls.Add(this.gradBrushColor2Label);
            this.linearBrushPage.Controls.Add(this.gradBrushColor1Label);
            this.linearBrushPage.Controls.Add(this.gradiantBrushColor2Button);
            this.linearBrushPage.Controls.Add(this.gradiantBrushColor1button);
            this.linearBrushPage.Location = new System.Drawing.Point(4, 22);
            this.linearBrushPage.Name = "linearBrushPage";
            this.linearBrushPage.Size = new System.Drawing.Size(353, 222);
            this.linearBrushPage.TabIndex = 2;
            this.linearBrushPage.Text = "Linear Gradiant";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(101, 161);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 7;
            this.button3.Text = "Add Brush";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.LinearGradientBrushAddClicked);
            // 
            // gradBrushExtendModeCombo
            // 
            this.gradBrushExtendModeCombo.FormattingEnabled = true;
            this.gradBrushExtendModeCombo.Items.AddRange(new object[] {
            "Clamp",
            "Wrap",
            "Mirror"});
            this.gradBrushExtendModeCombo.Location = new System.Drawing.Point(153, 125);
            this.gradBrushExtendModeCombo.Name = "gradBrushExtendModeCombo";
            this.gradBrushExtendModeCombo.Size = new System.Drawing.Size(121, 21);
            this.gradBrushExtendModeCombo.TabIndex = 6;
            this.gradBrushExtendModeCombo.Text = "Wrap";
            // 
            // gammaComboBox
            // 
            this.gammaComboBox.FormattingEnabled = true;
            this.gammaComboBox.Items.AddRange(new object[] {
            "Linear (1.0)",
            "StandardRgb (2.2)"});
            this.gammaComboBox.Location = new System.Drawing.Point(153, 94);
            this.gammaComboBox.Name = "gammaComboBox";
            this.gammaComboBox.Size = new System.Drawing.Size(121, 21);
            this.gammaComboBox.TabIndex = 6;
            this.gammaComboBox.Text = "Linear (1.0)";
            // 
            // gradBrushColor2Label
            // 
            this.gradBrushColor2Label.AutoSize = true;
            this.gradBrushColor2Label.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.gradBrushColor2Label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.gradBrushColor2Label.Location = new System.Drawing.Point(153, 51);
            this.gradBrushColor2Label.MinimumSize = new System.Drawing.Size(130, 23);
            this.gradBrushColor2Label.Name = "gradBrushColor2Label";
            this.gradBrushColor2Label.Size = new System.Drawing.Size(130, 23);
            this.gradBrushColor2Label.TabIndex = 2;
            this.gradBrushColor2Label.Text = "R = 1, G = 1, B = 1, A = 1";
            // 
            // gradBrushColor1Label
            // 
            this.gradBrushColor1Label.AutoSize = true;
            this.gradBrushColor1Label.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.gradBrushColor1Label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.gradBrushColor1Label.Location = new System.Drawing.Point(153, 22);
            this.gradBrushColor1Label.MinimumSize = new System.Drawing.Size(130, 23);
            this.gradBrushColor1Label.Name = "gradBrushColor1Label";
            this.gradBrushColor1Label.Size = new System.Drawing.Size(130, 23);
            this.gradBrushColor1Label.TabIndex = 2;
            this.gradBrushColor1Label.Text = "R = 0, G = 0, B = 0, A = 1";
            // 
            // gradiantBrushColor2Button
            // 
            this.gradiantBrushColor2Button.Location = new System.Drawing.Point(16, 51);
            this.gradiantBrushColor2Button.Name = "gradiantBrushColor2Button";
            this.gradiantBrushColor2Button.Size = new System.Drawing.Size(113, 23);
            this.gradiantBrushColor2Button.TabIndex = 3;
            this.gradiantBrushColor2Button.Text = "Select Color 2...";
            this.gradiantBrushColor2Button.UseVisualStyleBackColor = true;
            this.gradiantBrushColor2Button.Click += new System.EventHandler(this.gradiantBrushColor2Button_Click);
            // 
            // gradiantBrushColor1button
            // 
            this.gradiantBrushColor1button.Location = new System.Drawing.Point(16, 22);
            this.gradiantBrushColor1button.Name = "gradiantBrushColor1button";
            this.gradiantBrushColor1button.Size = new System.Drawing.Size(113, 23);
            this.gradiantBrushColor1button.TabIndex = 3;
            this.gradiantBrushColor1button.Text = "Select Color 1...";
            this.gradiantBrushColor1button.UseVisualStyleBackColor = true;
            this.gradiantBrushColor1button.Click += new System.EventHandler(this.gradiantBrushColor1button_Click);
            // 
            // graidantBrushPage
            // 
            this.graidantBrushPage.BackColor = System.Drawing.SystemColors.Control;
            this.graidantBrushPage.Controls.Add(this.button4);
            this.graidantBrushPage.Controls.Add(this.radialExtendCombo);
            this.graidantBrushPage.Controls.Add(this.radialGammaCombo);
            this.graidantBrushPage.Controls.Add(label5);
            this.graidantBrushPage.Controls.Add(label7);
            this.graidantBrushPage.Controls.Add(this.radialBrushColor2Label);
            this.graidantBrushPage.Controls.Add(this.radialBrushColor1Label);
            this.graidantBrushPage.Controls.Add(this.SelectRadialColor2);
            this.graidantBrushPage.Controls.Add(this.SelectRadialColor1);
            this.graidantBrushPage.Location = new System.Drawing.Point(4, 22);
            this.graidantBrushPage.Name = "graidantBrushPage";
            this.graidantBrushPage.Size = new System.Drawing.Size(353, 222);
            this.graidantBrushPage.TabIndex = 3;
            this.graidantBrushPage.Text = "Radial  Gradiant";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(128, 169);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 16;
            this.button4.Text = "Add Brush";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.RadialGradientBrushAddClicked);
            // 
            // radialExtendCombo
            // 
            this.radialExtendCombo.FormattingEnabled = true;
            this.radialExtendCombo.Items.AddRange(new object[] {
            "Clamp",
            "Wrap",
            "Mirror"});
            this.radialExtendCombo.Location = new System.Drawing.Point(180, 133);
            this.radialExtendCombo.Name = "radialExtendCombo";
            this.radialExtendCombo.Size = new System.Drawing.Size(121, 21);
            this.radialExtendCombo.TabIndex = 14;
            this.radialExtendCombo.Text = "Wrap";
            // 
            // radialGammaCombo
            // 
            this.radialGammaCombo.FormattingEnabled = true;
            this.radialGammaCombo.Items.AddRange(new object[] {
            "Linear (1.0)",
            "StandardRgb (2.2)"});
            this.radialGammaCombo.Location = new System.Drawing.Point(180, 102);
            this.radialGammaCombo.Name = "radialGammaCombo";
            this.radialGammaCombo.Size = new System.Drawing.Size(121, 21);
            this.radialGammaCombo.TabIndex = 15;
            this.radialGammaCombo.Text = "Linear (1.0)";
            // 
            // radialBrushColor2Label
            // 
            this.radialBrushColor2Label.AutoSize = true;
            this.radialBrushColor2Label.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.radialBrushColor2Label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.radialBrushColor2Label.Location = new System.Drawing.Point(180, 59);
            this.radialBrushColor2Label.MinimumSize = new System.Drawing.Size(130, 23);
            this.radialBrushColor2Label.Name = "radialBrushColor2Label";
            this.radialBrushColor2Label.Size = new System.Drawing.Size(130, 23);
            this.radialBrushColor2Label.TabIndex = 9;
            this.radialBrushColor2Label.Text = "R = 1, G = 1, B = 1, A = 1";
            // 
            // radialBrushColor1Label
            // 
            this.radialBrushColor1Label.AutoSize = true;
            this.radialBrushColor1Label.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.radialBrushColor1Label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.radialBrushColor1Label.Location = new System.Drawing.Point(180, 30);
            this.radialBrushColor1Label.MinimumSize = new System.Drawing.Size(130, 23);
            this.radialBrushColor1Label.Name = "radialBrushColor1Label";
            this.radialBrushColor1Label.Size = new System.Drawing.Size(130, 23);
            this.radialBrushColor1Label.TabIndex = 8;
            this.radialBrushColor1Label.Text = "R = 0, G = 0, B = 0, A = 1";
            // 
            // SelectRadialColor2
            // 
            this.SelectRadialColor2.Location = new System.Drawing.Point(43, 59);
            this.SelectRadialColor2.Name = "SelectRadialColor2";
            this.SelectRadialColor2.Size = new System.Drawing.Size(113, 23);
            this.SelectRadialColor2.TabIndex = 11;
            this.SelectRadialColor2.Text = "Select Color 2...";
            this.SelectRadialColor2.UseVisualStyleBackColor = true;
            this.SelectRadialColor2.Click += new System.EventHandler(this.SelectRadialColor2_Click);
            // 
            // SelectRadialColor1
            // 
            this.SelectRadialColor1.Location = new System.Drawing.Point(43, 30);
            this.SelectRadialColor1.Name = "SelectRadialColor1";
            this.SelectRadialColor1.Size = new System.Drawing.Size(113, 23);
            this.SelectRadialColor1.TabIndex = 10;
            this.SelectRadialColor1.Text = "Select Color 1...";
            this.SelectRadialColor1.UseVisualStyleBackColor = true;
            this.SelectRadialColor1.Click += new System.EventHandler(this.SelectRadialColor1_Click);
            // 
            // brushesList
            // 
            this.brushesList.FormattingEnabled = true;
            this.brushesList.Location = new System.Drawing.Point(16, 266);
            this.brushesList.Name = "brushesList";
            this.brushesList.Size = new System.Drawing.Size(353, 173);
            this.brushesList.TabIndex = 5;
            this.brushesList.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(114, 459);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(157, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.CloseButtonClicked);
            // 
            // BrushDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(385, 494);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.brushesList);
            this.Controls.Add(this.brushesTabs);
            this.Name = "BrushDialog";
            this.RightToLeftLayout = true;
            this.Text = "Select Brush";
            this.brushesTabs.ResumeLayout(false);
            this.solidColorPage.ResumeLayout(false);
            this.solidColorPage.PerformLayout();
            this.bitmapBrushPage.ResumeLayout(false);
            this.bitmapBrushPage.PerformLayout();
            this.linearBrushPage.ResumeLayout(false);
            this.linearBrushPage.PerformLayout();
            this.graidantBrushPage.ResumeLayout(false);
            this.graidantBrushPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Label colorLabel;
        private System.Windows.Forms.Button solidColorButton;
        private System.Windows.Forms.Button addBrushButton;
        private System.Windows.Forms.ComboBox transparencyValues;
        private System.Windows.Forms.Label transparency;
        private System.Windows.Forms.TabControl brushesTabs;
        private System.Windows.Forms.TabPage solidColorPage;
        private System.Windows.Forms.TabPage bitmapBrushPage;
        private System.Windows.Forms.TabPage linearBrushPage;
        private System.Windows.Forms.TabPage graidantBrushPage;
        private System.Windows.Forms.ListBox brushesList;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label imageFileLabel;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button addBitmapBrushBotton;
        private System.Windows.Forms.ComboBox extendedModeYComboBox;
        private System.Windows.Forms.ComboBox extendedModeXComboBox;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label gradBrushColor2Label;
        private System.Windows.Forms.Label gradBrushColor1Label;
        private System.Windows.Forms.Button gradiantBrushColor2Button;
        private System.Windows.Forms.Button gradiantBrushColor1button;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.ComboBox gradBrushExtendModeCombo;
        private System.Windows.Forms.ComboBox gammaComboBox;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ComboBox radialExtendCombo;
        private System.Windows.Forms.ComboBox radialGammaCombo;
        private System.Windows.Forms.Label radialBrushColor2Label;
        private System.Windows.Forms.Label radialBrushColor1Label;
        private System.Windows.Forms.Button SelectRadialColor2;
        private System.Windows.Forms.Button SelectRadialColor1;
    }
}