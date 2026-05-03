namespace ImageMove
{
    partial class Main
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.button6 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.button7 = new System.Windows.Forms.Button();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.button8 = new System.Windows.Forms.Button();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.button9 = new System.Windows.Forms.Button();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.button10 = new System.Windows.Forms.Button();
            this.textBox7 = new System.Windows.Forms.TextBox();
            this.button11 = new System.Windows.Forms.Button();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.button12 = new System.Windows.Forms.Button();
            this.textBox9 = new System.Windows.Forms.TextBox();
            this.button13 = new System.Windows.Forms.Button();
            this.textBox10 = new System.Windows.Forms.TextBox();
            this.button14 = new System.Windows.Forms.Button();
            this.textBox11 = new System.Windows.Forms.TextBox();
            this.button15 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.label12 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadImagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.previousImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nextImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.rightTabControl = new System.Windows.Forms.TabControl();
            this.operationTabPage = new System.Windows.Forms.TabPage();
            this.operationLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sourceLabel = new System.Windows.Forms.Label();
            this.navigationPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.currentFileCaptionLabel = new System.Windows.Forms.Label();
            this.currentCountCaptionLabel = new System.Windows.Forms.Label();
            this.destinationTabPage = new System.Windows.Forms.TabPage();
            this.destinationLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.rightTabControl.SuspendLayout();
            this.operationTabPage.SuspendLayout();
            this.operationLayout.SuspendLayout();
            this.navigationPanel.SuspendLayout();
            this.destinationTabPage.SuspendLayout();
            this.destinationLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.runToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(1280, 33);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveSettingsToolStripMenuItem,
            this.loadSettingsToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(76, 29);
            this.fileToolStripMenuItem.Text = "ファイル";
            // 
            // saveSettingsToolStripMenuItem
            // 
            this.saveSettingsToolStripMenuItem.Name = "saveSettingsToolStripMenuItem";
            this.saveSettingsToolStripMenuItem.Size = new System.Drawing.Size(182, 34);
            this.saveSettingsToolStripMenuItem.Text = "設定保存";
            this.saveSettingsToolStripMenuItem.Click += new System.EventHandler(this.MenuSaveSettings_Click);
            // 
            // loadSettingsToolStripMenuItem
            // 
            this.loadSettingsToolStripMenuItem.Name = "loadSettingsToolStripMenuItem";
            this.loadSettingsToolStripMenuItem.Size = new System.Drawing.Size(182, 34);
            this.loadSettingsToolStripMenuItem.Text = "設定読込";
            this.loadSettingsToolStripMenuItem.Click += new System.EventHandler(this.MenuLoadSettings_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(179, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(182, 34);
            this.exitToolStripMenuItem.Text = "終了";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.button5_Click_1);
            // 
            // runToolStripMenuItem
            // 
            this.runToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectFolderToolStripMenuItem,
            this.reloadImagesToolStripMenuItem,
            this.previousImageToolStripMenuItem,
            this.nextImageToolStripMenuItem});
            this.runToolStripMenuItem.Name = "runToolStripMenuItem";
            this.runToolStripMenuItem.Size = new System.Drawing.Size(58, 29);
            this.runToolStripMenuItem.Text = "実行";
            // 
            // selectFolderToolStripMenuItem
            // 
            this.selectFolderToolStripMenuItem.Name = "selectFolderToolStripMenuItem";
            this.selectFolderToolStripMenuItem.Size = new System.Drawing.Size(284, 34);
            this.selectFolderToolStripMenuItem.Text = "読み込みフォルダ選択";
            this.selectFolderToolStripMenuItem.Click += new System.EventHandler(this.button1_Click);
            // 
            // reloadImagesToolStripMenuItem
            // 
            this.reloadImagesToolStripMenuItem.Name = "reloadImagesToolStripMenuItem";
            this.reloadImagesToolStripMenuItem.Size = new System.Drawing.Size(284, 34);
            this.reloadImagesToolStripMenuItem.Text = "画像読み込み / 再読み込み";
            this.reloadImagesToolStripMenuItem.Click += new System.EventHandler(this.button2_Click);
            // 
            // previousImageToolStripMenuItem
            // 
            this.previousImageToolStripMenuItem.Name = "previousImageToolStripMenuItem";
            this.previousImageToolStripMenuItem.Size = new System.Drawing.Size(284, 34);
            this.previousImageToolStripMenuItem.Text = "前の画像";
            this.previousImageToolStripMenuItem.Click += new System.EventHandler(this.button3_Click);
            // 
            // nextImageToolStripMenuItem
            // 
            this.nextImageToolStripMenuItem.Name = "nextImageToolStripMenuItem";
            this.nextImageToolStripMenuItem.Size = new System.Drawing.Size(284, 34);
            this.nextImageToolStripMenuItem.Text = "次の画像";
            this.nextImageToolStripMenuItem.Click += new System.EventHandler(this.button4_Click);
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 33);
            this.mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.pictureBox1);
            this.mainSplitContainer.Panel1.Padding = new System.Windows.Forms.Padding(12);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.rightTabControl);
            this.mainSplitContainer.Panel2.Padding = new System.Windows.Forms.Padding(0, 12, 12, 12);
            this.mainSplitContainer.Size = new System.Drawing.Size(1280, 991);
            this.mainSplitContainer.SplitterDistance = 760;
            this.mainSplitContainer.TabIndex = 1;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(736, 967);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // rightTabControl
            // 
            this.rightTabControl.Controls.Add(this.operationTabPage);
            this.rightTabControl.Controls.Add(this.destinationTabPage);
            this.rightTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightTabControl.Location = new System.Drawing.Point(0, 12);
            this.rightTabControl.Name = "rightTabControl";
            this.rightTabControl.SelectedIndex = 0;
            this.rightTabControl.Size = new System.Drawing.Size(504, 967);
            this.rightTabControl.TabIndex = 0;
            // 
            // operationTabPage
            // 
            this.operationTabPage.Controls.Add(this.operationLayout);
            this.operationTabPage.Location = new System.Drawing.Point(4, 33);
            this.operationTabPage.Name = "operationTabPage";
            this.operationTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.operationTabPage.Size = new System.Drawing.Size(496, 930);
            this.operationTabPage.TabIndex = 0;
            this.operationTabPage.Text = "操作";
            this.operationTabPage.UseVisualStyleBackColor = true;
            // 
            // operationLayout
            // 
            this.operationLayout.ColumnCount = 3;
            this.operationLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.operationLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.operationLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.operationLayout.Controls.Add(this.sourceLabel, 0, 0);
            this.operationLayout.Controls.Add(this.textBox1, 1, 0);
            this.operationLayout.Controls.Add(this.button1, 2, 0);
            this.operationLayout.Controls.Add(this.button2, 0, 1);
            this.operationLayout.Controls.Add(this.navigationPanel, 0, 2);
            this.operationLayout.Controls.Add(this.currentFileCaptionLabel, 0, 3);
            this.operationLayout.Controls.Add(this.label1, 1, 3);
            this.operationLayout.Controls.Add(this.currentCountCaptionLabel, 0, 4);
            this.operationLayout.Controls.Add(this.label12, 1, 4);
            this.operationLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.operationLayout.Location = new System.Drawing.Point(3, 3);
            this.operationLayout.Name = "operationLayout";
            this.operationLayout.Padding = new System.Windows.Forms.Padding(12);
            this.operationLayout.RowCount = 5;
            this.operationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.operationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.operationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.operationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.operationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.operationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.operationLayout.Size = new System.Drawing.Size(490, 924);
            this.operationLayout.TabIndex = 0;
            // 
            // sourceLabel
            // 
            this.sourceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sourceLabel.Location = new System.Drawing.Point(15, 12);
            this.sourceLabel.Name = "sourceLabel";
            this.sourceLabel.Size = new System.Drawing.Size(104, 42);
            this.sourceLabel.TabIndex = 0;
            this.sourceLabel.Text = "読込元";
            this.sourceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Location = new System.Drawing.Point(125, 15);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(252, 31);
            this.textBox1.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button1.Location = new System.Drawing.Point(383, 15);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(92, 36);
            this.button1.TabIndex = 2;
            this.button1.Text = "参照";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.operationLayout.SetColumnSpan(this.button2, 3);
            this.button2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button2.Location = new System.Drawing.Point(15, 57);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(460, 50);
            this.button2.TabIndex = 3;
            this.button2.Text = "画像読み込み / 再読み込み";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // navigationPanel
            // 
            this.operationLayout.SetColumnSpan(this.navigationPanel, 3);
            this.navigationPanel.Controls.Add(this.button3);
            this.navigationPanel.Controls.Add(this.button4);
            this.navigationPanel.Controls.Add(this.button5);
            this.navigationPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navigationPanel.Location = new System.Drawing.Point(15, 113);
            this.navigationPanel.Name = "navigationPanel";
            this.navigationPanel.Size = new System.Drawing.Size(460, 66);
            this.navigationPanel.TabIndex = 4;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(3, 3);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(130, 50);
            this.button3.TabIndex = 0;
            this.button3.Text = "前の画像";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(139, 3);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(130, 50);
            this.button4.TabIndex = 1;
            this.button4.Text = "次の画像";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(275, 3);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(130, 50);
            this.button5.TabIndex = 2;
            this.button5.Text = "終了";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click_1);
            // 
            // currentFileCaptionLabel
            // 
            this.currentFileCaptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentFileCaptionLabel.Location = new System.Drawing.Point(15, 182);
            this.currentFileCaptionLabel.Name = "currentFileCaptionLabel";
            this.currentFileCaptionLabel.Size = new System.Drawing.Size(104, 64);
            this.currentFileCaptionLabel.TabIndex = 5;
            this.currentFileCaptionLabel.Text = "現在画像";
            this.currentFileCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.operationLayout.SetColumnSpan(this.label1, 2);
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("メイリオ", 10F);
            this.label1.Location = new System.Drawing.Point(125, 182);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(350, 64);
            this.label1.TabIndex = 6;
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // currentCountCaptionLabel
            // 
            this.currentCountCaptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentCountCaptionLabel.Location = new System.Drawing.Point(15, 246);
            this.currentCountCaptionLabel.Name = "currentCountCaptionLabel";
            this.currentCountCaptionLabel.Size = new System.Drawing.Size(104, 64);
            this.currentCountCaptionLabel.TabIndex = 7;
            this.currentCountCaptionLabel.Text = "進捗";
            this.currentCountCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label12
            // 
            this.operationLayout.SetColumnSpan(this.label12, 2);
            this.label12.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label12.Font = new System.Drawing.Font("メイリオ", 10F);
            this.label12.Location = new System.Drawing.Point(125, 246);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(350, 64);
            this.label12.TabIndex = 8;
            this.label12.Text = "N枚中、N枚目";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // destinationTabPage
            // 
            this.destinationTabPage.Controls.Add(this.destinationLayoutPanel);
            this.destinationTabPage.Location = new System.Drawing.Point(4, 33);
            this.destinationTabPage.Name = "destinationTabPage";
            this.destinationTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.destinationTabPage.Size = new System.Drawing.Size(496, 930);
            this.destinationTabPage.TabIndex = 1;
            this.destinationTabPage.Text = "移動先設定";
            this.destinationTabPage.UseVisualStyleBackColor = true;
            // 
            // destinationLayoutPanel
            // 
            this.destinationLayoutPanel.ColumnCount = 4;
            this.destinationLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.destinationLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.destinationLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.destinationLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.destinationLayoutPanel.Controls.Add(this.label2, 0, 0);
            this.destinationLayoutPanel.Controls.Add(this.textBox2, 1, 0);
            this.destinationLayoutPanel.Controls.Add(this.button6, 2, 0);
            this.destinationLayoutPanel.Controls.Add(this.label3, 0, 1);
            this.destinationLayoutPanel.Controls.Add(this.textBox3, 1, 1);
            this.destinationLayoutPanel.Controls.Add(this.button7, 2, 1);
            this.destinationLayoutPanel.Controls.Add(this.label4, 0, 2);
            this.destinationLayoutPanel.Controls.Add(this.textBox4, 1, 2);
            this.destinationLayoutPanel.Controls.Add(this.button8, 2, 2);
            this.destinationLayoutPanel.Controls.Add(this.label5, 0, 3);
            this.destinationLayoutPanel.Controls.Add(this.textBox5, 1, 3);
            this.destinationLayoutPanel.Controls.Add(this.button9, 2, 3);
            this.destinationLayoutPanel.Controls.Add(this.label6, 0, 4);
            this.destinationLayoutPanel.Controls.Add(this.textBox6, 1, 4);
            this.destinationLayoutPanel.Controls.Add(this.button10, 2, 4);
            this.destinationLayoutPanel.Controls.Add(this.label7, 0, 5);
            this.destinationLayoutPanel.Controls.Add(this.textBox7, 1, 5);
            this.destinationLayoutPanel.Controls.Add(this.button11, 2, 5);
            this.destinationLayoutPanel.Controls.Add(this.label8, 0, 6);
            this.destinationLayoutPanel.Controls.Add(this.textBox8, 1, 6);
            this.destinationLayoutPanel.Controls.Add(this.button12, 2, 6);
            this.destinationLayoutPanel.Controls.Add(this.label9, 0, 7);
            this.destinationLayoutPanel.Controls.Add(this.textBox9, 1, 7);
            this.destinationLayoutPanel.Controls.Add(this.button13, 2, 7);
            this.destinationLayoutPanel.Controls.Add(this.label10, 0, 8);
            this.destinationLayoutPanel.Controls.Add(this.textBox10, 1, 8);
            this.destinationLayoutPanel.Controls.Add(this.button14, 2, 8);
            this.destinationLayoutPanel.Controls.Add(this.label11, 0, 9);
            this.destinationLayoutPanel.Controls.Add(this.textBox11, 1, 9);
            this.destinationLayoutPanel.Controls.Add(this.button15, 2, 9);
            this.destinationLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.destinationLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.destinationLayoutPanel.Name = "destinationLayoutPanel";
            this.destinationLayoutPanel.Padding = new System.Windows.Forms.Padding(12);
            this.destinationLayoutPanel.RowCount = 10;
            for (int i = 0; i < 10; i++) this.destinationLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.destinationLayoutPanel.Size = new System.Drawing.Size(490, 924);
            this.destinationLayoutPanel.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(15, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 48);
            this.label2.TabIndex = 0;
            this.label2.Text = "Num0";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(15, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 48);
            this.label3.TabIndex = 1;
            this.label3.Text = "Num1";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(15, 108);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 48);
            this.label4.TabIndex = 2;
            this.label4.Text = "Num2";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(15, 156);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 48);
            this.label5.TabIndex = 3;
            this.label5.Text = "Num3";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(15, 204);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 48);
            this.label6.TabIndex = 4;
            this.label6.Text = "Num4";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(15, 252);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(64, 48);
            this.label7.TabIndex = 5;
            this.label7.Text = "Num5";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(15, 300);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 48);
            this.label8.TabIndex = 6;
            this.label8.Text = "Num6";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label9.Location = new System.Drawing.Point(15, 348);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(64, 48);
            this.label9.TabIndex = 7;
            this.label9.Text = "Num7";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label10
            // 
            this.label10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label10.Location = new System.Drawing.Point(15, 396);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(64, 48);
            this.label10.TabIndex = 8;
            this.label10.Text = "Num8";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label11
            // 
            this.label11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label11.Location = new System.Drawing.Point(15, 444);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(64, 48);
            this.label11.TabIndex = 9;
            this.label11.Text = "Num9";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox2
            // 
            this.textBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox2.Location = new System.Drawing.Point(85, 15);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(210, 31);
            this.textBox2.TabIndex = 10;
            // 
            // button6
            // 
            this.button6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button6.Location = new System.Drawing.Point(301, 15);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(84, 42);
            this.button6.TabIndex = 11;
            this.button6.Text = "参照";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // textBox3
            // 
            this.textBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox3.Location = new System.Drawing.Point(85, 63);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(210, 31);
            this.textBox3.TabIndex = 12;
            // 
            // button7
            // 
            this.button7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button7.Location = new System.Drawing.Point(301, 63);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(84, 42);
            this.button7.TabIndex = 13;
            this.button7.Text = "参照";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // textBox4
            // 
            this.textBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox4.Location = new System.Drawing.Point(85, 111);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(210, 31);
            this.textBox4.TabIndex = 14;
            // 
            // button8
            // 
            this.button8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button8.Location = new System.Drawing.Point(301, 111);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(84, 42);
            this.button8.TabIndex = 15;
            this.button8.Text = "参照";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // textBox5
            // 
            this.textBox5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox5.Location = new System.Drawing.Point(85, 159);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(210, 31);
            this.textBox5.TabIndex = 16;
            // 
            // button9
            // 
            this.button9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button9.Location = new System.Drawing.Point(301, 159);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(84, 42);
            this.button9.TabIndex = 17;
            this.button9.Text = "参照";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // textBox6
            // 
            this.textBox6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox6.Location = new System.Drawing.Point(85, 207);
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new System.Drawing.Size(210, 31);
            this.textBox6.TabIndex = 18;
            // 
            // button10
            // 
            this.button10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button10.Location = new System.Drawing.Point(301, 207);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(84, 42);
            this.button10.TabIndex = 19;
            this.button10.Text = "参照";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // textBox7
            // 
            this.textBox7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox7.Location = new System.Drawing.Point(85, 255);
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new System.Drawing.Size(210, 31);
            this.textBox7.TabIndex = 20;
            // 
            // button11
            // 
            this.button11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button11.Location = new System.Drawing.Point(301, 255);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(84, 42);
            this.button11.TabIndex = 21;
            this.button11.Text = "参照";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.button11_Click);
            // 
            // textBox8
            // 
            this.textBox8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox8.Location = new System.Drawing.Point(85, 303);
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new System.Drawing.Size(210, 31);
            this.textBox8.TabIndex = 22;
            // 
            // button12
            // 
            this.button12.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button12.Location = new System.Drawing.Point(301, 303);
            this.button12.Name = "button12";
            this.button12.Size = new System.Drawing.Size(84, 42);
            this.button12.TabIndex = 23;
            this.button12.Text = "参照";
            this.button12.UseVisualStyleBackColor = true;
            this.button12.Click += new System.EventHandler(this.button12_Click);
            // 
            // textBox9
            // 
            this.textBox9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox9.Location = new System.Drawing.Point(85, 351);
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new System.Drawing.Size(210, 31);
            this.textBox9.TabIndex = 24;
            // 
            // button13
            // 
            this.button13.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button13.Location = new System.Drawing.Point(301, 351);
            this.button13.Name = "button13";
            this.button13.Size = new System.Drawing.Size(84, 42);
            this.button13.TabIndex = 25;
            this.button13.Text = "参照";
            this.button13.UseVisualStyleBackColor = true;
            this.button13.Click += new System.EventHandler(this.button13_Click);
            // 
            // textBox10
            // 
            this.textBox10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox10.Location = new System.Drawing.Point(85, 399);
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new System.Drawing.Size(210, 31);
            this.textBox10.TabIndex = 26;
            // 
            // button14
            // 
            this.button14.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button14.Location = new System.Drawing.Point(301, 399);
            this.button14.Name = "button14";
            this.button14.Size = new System.Drawing.Size(84, 42);
            this.button14.TabIndex = 27;
            this.button14.Text = "参照";
            this.button14.UseVisualStyleBackColor = true;
            this.button14.Click += new System.EventHandler(this.button14_Click);
            // 
            // textBox11
            // 
            this.textBox11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox11.Location = new System.Drawing.Point(85, 447);
            this.textBox11.Name = "textBox11";
            this.textBox11.Size = new System.Drawing.Size(210, 31);
            this.textBox11.TabIndex = 28;
            // 
            // button15
            // 
            this.button15.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button15.Location = new System.Drawing.Point(301, 447);
            this.button15.Name = "button15";
            this.button15.Size = new System.Drawing.Size(84, 42);
            this.button15.TabIndex = 29;
            this.button15.Text = "参照";
            this.button15.UseVisualStyleBackColor = true;
            this.button15.Click += new System.EventHandler(this.button15_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 1024);
            this.Controls.Add(this.mainSplitContainer);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(1280, 900);
            this.Name = "Main";
            this.Text = "ImageMove";
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.rightTabControl.ResumeLayout(false);
            this.operationTabPage.ResumeLayout(false);
            this.operationLayout.ResumeLayout(false);
            this.operationLayout.PerformLayout();
            this.navigationPanel.ResumeLayout(false);
            this.destinationTabPage.ResumeLayout(false);
            this.destinationLayoutPanel.ResumeLayout(false);
            this.destinationLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.TextBox textBox6;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.TextBox textBox7;
        private System.Windows.Forms.Button button11;
        private System.Windows.Forms.TextBox textBox8;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.TextBox textBox9;
        private System.Windows.Forms.Button button13;
        private System.Windows.Forms.TextBox textBox10;
        private System.Windows.Forms.Button button14;
        private System.Windows.Forms.TextBox textBox11;
        private System.Windows.Forms.Button button15;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadImagesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem previousImageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nextImageToolStripMenuItem;
        private System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.TabControl rightTabControl;
        private System.Windows.Forms.TabPage operationTabPage;
        private System.Windows.Forms.TabPage destinationTabPage;
        private System.Windows.Forms.TableLayoutPanel operationLayout;
        private System.Windows.Forms.Label sourceLabel;
        private System.Windows.Forms.FlowLayoutPanel navigationPanel;
        private System.Windows.Forms.Label currentFileCaptionLabel;
        private System.Windows.Forms.Label currentCountCaptionLabel;
        private System.Windows.Forms.TableLayoutPanel destinationLayoutPanel;
    }
}
