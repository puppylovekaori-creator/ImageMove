using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using log4net;

namespace ImageMove
{
    public partial class Main : Form
    {
        private const string AppDisplayName = "ImageMove";
        private const int MinimumRestoreWidth = 960;
        private const int MinimumRestoreHeight = 700;

        #region フィールド
        /// <summary> ロガー </summary>
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary> サポートする画像拡張子 </summary>
        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        /// <summary> 設定保存ファイル </summary>
        private readonly string settingFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setting.xml");

        /// <summary> 移動先テキストボックス一覧 </summary>
        private readonly List<TextBox> destinationTextBoxes;

        /// <summary> 移動先へ即時移動するボタン一覧 </summary>
        private readonly List<Button> destinationMoveButtons;

        /// <summary> 操作タブ側の移動先表示欄一覧 </summary>
        private readonly List<TextBox> operationDestinationPreviewTextBoxes;

        /// <summary> 画像ファイルパスのリスト </summary>
        private List<string> imagePaths = new List<string>();

        /// <summary> 現在表示している画像のインデックス </summary>
        private int currentImageIndex = -1;

        /// <summary> 移動履歴 </summary>
        private readonly Stack<MoveHistoryAction> moveHistoryActions = new Stack<MoveHistoryAction>();

        /// <summary> 一覧別窓 </summary>
        private ImageListBrowserForm imageListBrowserForm;

        /// <summary> 左上の現在画像名表示 </summary>
        private Label previewFileNameValueLabel;

        /// <summary> 左上の進捗表示 </summary>
        private Label previewProgressValueLabel;

        /// <summary> 元に戻すボタン </summary>
        private Button undoButton;

        /// <summary> 一覧ボタン </summary>
        private Button browserButton;

        /// <summary> 元に戻すメニュー </summary>
        private ToolStripMenuItem undoMoveToolStripMenuItem;

        /// <summary> 画像一覧メニュー </summary>
        private ToolStripMenuItem openImageListToolStripMenuItem;

        /// <summary> 移動せず次へメニュー </summary>
        private ToolStripMenuItem skipNextToolStripMenuItem;
        #endregion フィールド

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Main()
        {
            InitializeComponent();

            destinationTextBoxes = new List<TextBox>
            {
                textBox2,
                textBox3,
                textBox4,
                textBox5,
                textBox6,
                textBox7,
                textBox8,
                textBox9,
                textBox10,
                textBox11
            };

            destinationMoveButtons = new List<Button>();
            operationDestinationPreviewTextBoxes = new List<TextBox>();

            InitializeUi();
            LoadSettingIfExists();
        }
        #endregion コンストラクタ

        #region キー処理
        /// <summary>
        /// 矢印キーやテンキーをフォーム全体で処理する
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (IsPathTextBoxFocused())
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (TryHandleShortcut(keyData))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion キー処理

        #region イベント
        #region 参照ボタン押下 取得パス
        /// <summary>
        /// 参照ボタン押下
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox1);
        }
        #endregion 参照ボタン押下 取得パス

        #region 画像ファイル取得ボタン押下
        /// <summary>
        /// 画像ファイル取得ボタン押下
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            ReloadImages();
        }
        #endregion 画像ファイル取得ボタン押下

        #region 参照ボタン押下 移動パス
        /// <summary>
        /// 参照ボタン押下 移動パス
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox2);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox3);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox4);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox5);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox6);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox7);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox8);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox9);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox10);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            BrowseForFolder(textBox11);
        }
        #endregion 参照ボタン押下 移動パス

        #region 前の画像ボタン押下
        /// <summary>
        /// 前の画像ボタン押下
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            ShowPreviousImage();
        }
        #endregion 前の画像ボタン押下

        #region 次の画像ボタン押下
        /// <summary>
        /// 次の画像ボタン押下
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            ShowNextImage();
        }
        #endregion 次の画像ボタン押下

        #region 画像の移動ボタン押下
        /// <summary>
        /// 画像の移動ボタン押下
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            MoveCurrentImage(textBox2);
        }
        #endregion 画像の移動ボタン押下

        #region 終了ボタン押下
        /// <summary>
        /// 終了ボタン押下
        /// </summary>
        private void button5_Click_1(object sender, EventArgs e)
        {
            Close();
        }
        #endregion 終了ボタン押下

        #region フォーム上でキーボード押下された
        /// <summary>
        /// フォーム上でキーボード押下された
        /// </summary>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (TryHandleShortcut(e.KeyData))
            {
                e.Handled = true;
            }
        }
        #endregion フォーム上でキーボード押下された

        /// <summary>
        /// パス編集終了時に設定を保存する
        /// </summary>
        private void PathTextBox_Leave(object sender, EventArgs e)
        {
            SaveSettingSafe();
        }

        /// <summary>
        /// 移動先パス編集時にボタン活性を更新する
        /// </summary>
        private void DestinationTextBox_TextChanged(object sender, EventArgs e)
        {
            SyncOperationDestinationPaths();
            UpdateDestinationActionButtons();
        }

        /// <summary>
        /// フォーム終了時に設定を保存する
        /// </summary>
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettingSafe();
        }
        #endregion イベント

        #region メソッド
        #region 初期化
        /// <summary>
        /// UI初期化
        /// </summary>
        private void InitializeUi()
        {
            KeyPreview = true;
            button2.Text = "画像読み込み / 再読み込み";
            button4.Text = "移動せず次へ";
            label1.AutoEllipsis = true;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            ConfigurePreviewPanel();
            ConfigureNavigationPanel();
            ConfigureAdditionalMenus();
            CreateOperationDestinationRows();
            CreateDestinationMoveButtons();
            SyncOperationDestinationPaths();

            foreach (var textBox in EnumeratePathTextBoxes())
            {
                textBox.ReadOnly = false;
                textBox.Leave += PathTextBox_Leave;
            }

            foreach (var destinationTextBox in destinationTextBoxes)
            {
                destinationTextBox.TextChanged += DestinationTextBox_TextChanged;
            }

            FormClosing += Main_FormClosing;
            ClearDisplayedImage("画像がありません。");
            UpdateDestinationActionButtons();
            UpdateUndoState();
        }

        /// <summary>
        /// 左側プレビューの上部情報欄を構築する
        /// </summary>
        private void ConfigurePreviewPanel()
        {
            var previewHost = mainSplitContainer.Panel1;
            previewHost.SuspendLayout();

            try
            {
                previewHost.Controls.Remove(pictureBox1);

                var previewLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 3,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
                previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
                previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
                previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                var currentImageCaptionLabel = new Label
                {
                    Text = "現在画像",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                previewFileNameValueLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true,
                    Font = new Font("メイリオ", 10F)
                };

                var currentProgressCaptionLabel = new Label
                {
                    Text = "進捗",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                previewProgressValueLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("メイリオ", 10F)
                };

                pictureBox1.Dock = DockStyle.Fill;

                previewLayout.Controls.Add(currentImageCaptionLabel, 0, 0);
                previewLayout.Controls.Add(previewFileNameValueLabel, 1, 0);
                previewLayout.Controls.Add(currentProgressCaptionLabel, 0, 1);
                previewLayout.Controls.Add(previewProgressValueLabel, 1, 1);
                previewLayout.Controls.Add(pictureBox1, 0, 2);
                previewLayout.SetColumnSpan(pictureBox1, 2);

                previewHost.Controls.Add(previewLayout);
            }
            finally
            {
                previewHost.ResumeLayout(false);
            }
        }

        /// <summary>
        /// 操作用ボタン行を拡張する
        /// </summary>
        private void ConfigureNavigationPanel()
        {
            navigationPanel.SuspendLayout();

            try
            {
                navigationPanel.Controls.Clear();
                navigationPanel.WrapContents = true;
                navigationPanel.AutoScroll = true;
                operationLayout.RowStyles[2].Height = 118F;

                button3.Width = 130;
                button4.Width = 130;
                button5.Width = 110;

                undoButton = new Button
                {
                    Name = "undoButton",
                    Text = "元に戻す",
                    Size = new Size(110, 50),
                    UseVisualStyleBackColor = true
                };
                undoButton.Click += UndoButton_Click;

                browserButton = new Button
                {
                    Name = "browserButton",
                    Text = "一覧",
                    Size = new Size(110, 50),
                    UseVisualStyleBackColor = true
                };
                browserButton.Click += OpenImageListBrowser_Click;

                navigationPanel.Controls.Add(button3);
                navigationPanel.Controls.Add(button4);
                navigationPanel.Controls.Add(undoButton);
                navigationPanel.Controls.Add(browserButton);
                navigationPanel.Controls.Add(button5);
            }
            finally
            {
                navigationPanel.ResumeLayout(false);
            }
        }

        /// <summary>
        /// メニューバーへ補助操作を追加する
        /// </summary>
        private void ConfigureAdditionalMenus()
        {
            var editToolStripMenuItem = new ToolStripMenuItem("編集");
            undoMoveToolStripMenuItem = new ToolStripMenuItem("元に戻す", null, UndoButton_Click)
            {
                ShortcutKeys = Keys.Control | Keys.Z
            };
            editToolStripMenuItem.DropDownItems.Add(undoMoveToolStripMenuItem);

            skipNextToolStripMenuItem = new ToolStripMenuItem("移動せず次へ (→)", null, button4_Click);
            openImageListToolStripMenuItem = new ToolStripMenuItem("画像一覧を開く", null, OpenImageListBrowser_Click);

            runToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            runToolStripMenuItem.DropDownItems.Add(skipNextToolStripMenuItem);
            runToolStripMenuItem.DropDownItems.Add(openImageListToolStripMenuItem);

            menuStrip1.Items.Insert(1, editToolStripMenuItem);
        }

        /// <summary>
        /// パス入力欄を列挙する
        /// </summary>
        private IEnumerable<TextBox> EnumeratePathTextBoxes()
        {
            yield return textBox1;

            foreach (var destinationTextBox in destinationTextBoxes)
            {
                yield return destinationTextBox;
            }
        }

        /// <summary>
        /// 操作タブに移動先の一覧を作成する
        /// </summary>
        private void CreateOperationDestinationRows()
        {
            operationDestinationPanel.SuspendLayout();

            try
            {
                operationDestinationPanel.Controls.Clear();
                operationDestinationPreviewTextBoxes.Clear();

                string[] labels =
                {
                    label2.Text,
                    label3.Text,
                    label4.Text,
                    label5.Text,
                    label6.Text,
                    label7.Text,
                    label8.Text,
                    label9.Text,
                    label10.Text,
                    label11.Text
                };

                for (int index = 0; index < destinationTextBoxes.Count; index++)
                {
                    var label = new Label
                    {
                        Dock = DockStyle.Fill,
                        Text = labels[index],
                        TextAlign = ContentAlignment.MiddleLeft,
                        Margin = new Padding(3)
                    };

                    var moveButton = new Button
                    {
                        Name = "operationMoveButton" + index,
                        Text = "移動",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(3),
                        Tag = index,
                        UseVisualStyleBackColor = true
                    };
                    moveButton.Click += DestinationMoveButton_Click;

                    var pathTextBox = new TextBox
                    {
                        Name = "operationDestinationPreviewTextBox" + index,
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        Margin = new Padding(3)
                    };

                    var browseButton = new Button
                    {
                        Name = "operationBrowseButton" + index,
                        Text = "参照",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(3),
                        Tag = index,
                        UseVisualStyleBackColor = true
                    };
                    browseButton.Click += OperationDestinationBrowseButton_Click;

                    operationDestinationPanel.Controls.Add(label, 0, index);
                    operationDestinationPanel.Controls.Add(moveButton, 1, index);
                    operationDestinationPanel.Controls.Add(pathTextBox, 2, index);
                    operationDestinationPanel.Controls.Add(browseButton, 3, index);

                    destinationMoveButtons.Add(moveButton);
                    operationDestinationPreviewTextBoxes.Add(pathTextBox);
                }
            }
            finally
            {
                operationDestinationPanel.ResumeLayout(false);
            }
        }

        /// <summary>
        /// 各移動先テキスト欄の左に即時移動ボタンを作成する
        /// </summary>
        private void CreateDestinationMoveButtons()
        {
            destinationLayoutPanel.SuspendLayout();

            try
            {
                for (int index = 0; index < destinationTextBoxes.Count; index++)
                {
                    TextBox destinationTextBox = destinationTextBoxes[index];
                    var moveButton = new Button
                    {
                        Name = "destinationMoveButton" + index,
                        Text = "移動",
                        Dock = DockStyle.Fill,
                        AutoSize = true,
                        Margin = new Padding(6, 3, 6, 3),
                        Tag = index,
                        TabIndex = destinationTextBox.TabIndex,
                        UseVisualStyleBackColor = true
                    };

                    moveButton.Click += DestinationMoveButton_Click;

                    destinationLayoutPanel.Controls.Add(moveButton, 3, index);
                    destinationMoveButtons.Add(moveButton);
                }
            }
            finally
            {
                destinationLayoutPanel.ResumeLayout(false);
            }
        }

        /// <summary>
        /// 操作タブ側の移動先表示を同期する
        /// </summary>
        private void SyncOperationDestinationPaths()
        {
            int count = Math.Min(operationDestinationPreviewTextBoxes.Count, destinationTextBoxes.Count);
            for (int index = 0; index < count; index++)
            {
                operationDestinationPreviewTextBoxes[index].Text = NormalizeDirectoryPath(destinationTextBoxes[index].Text);
            }
        }

        /// <summary>
        /// 操作タブ側の参照ボタン押下
        /// </summary>
        private void OperationDestinationBrowseButton_Click(object sender, EventArgs e)
        {
            if (!(sender is Button browseButton) || browseButton.Tag == null)
            {
                return;
            }

            int destinationIndex = Convert.ToInt32(browseButton.Tag);
            if (destinationIndex < 0 || destinationIndex >= destinationTextBoxes.Count)
            {
                return;
            }

            BrowseForFolder(destinationTextBoxes[destinationIndex]);
        }
        #endregion 初期化

        #region 画像読み込み
        /// <summary>
        /// 画像を読み込みまたは再読み込みする
        /// </summary>
        private void ReloadImages()
        {
            logger.Info("画像読み込み / 再読み込み 開始");

            try
            {
                if (!TryGetExistingDirectory(textBox1.Text, out string sourceDirectory))
                {
                    MessageBox.Show("読み込みフォルダを指定してください。");
                    return;
                }

                string previousImagePath = GetCurrentImagePath();
                int previousIndex = currentImageIndex;

                imagePaths = CollectImagePaths(sourceDirectory);
                currentImageIndex = ResolveReloadIndex(previousImagePath, previousIndex);

                if (!HasCurrentImage())
                {
                    ClearDisplayedImage("画像がありません。");
                    return;
                }

                DisplayCurrentImage();
                RefreshImageBrowserItemsIfOpen();
                SaveSettingSafe();
            }
            catch (Exception ex)
            {
                MessageBox.Show("画像の読み込みに失敗しました。");
                logger.Fatal("画像読み込み / 再読み込みでエラーが発生しました。", ex);
            }
            finally
            {
                logger.Info("画像読み込み / 再読み込み 終了");
            }
        }

        /// <summary>
        /// 読み込み対象の画像パスを収集する
        /// </summary>
        private List<string> CollectImagePaths(string sourceDirectory)
        {
            return Directory
                .EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
                .Where(path => SupportedImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 再読み込み後の表示位置を決定する
        /// </summary>
        private int ResolveReloadIndex(string previousImagePath, int previousIndex)
        {
            if (imagePaths.Count == 0)
            {
                return -1;
            }

            if (!string.IsNullOrWhiteSpace(previousImagePath))
            {
                int existingIndex = imagePaths.FindIndex(
                    path => string.Equals(path, previousImagePath, StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                {
                    return existingIndex;
                }
            }

            if (previousIndex < 0)
            {
                return 0;
            }

            if (previousIndex >= imagePaths.Count)
            {
                return imagePaths.Count - 1;
            }

            return previousIndex;
        }
        #endregion 画像読み込み

        #region 画像表示
        /// <summary>
        /// 現在位置の画像を表示する
        /// </summary>
        private void DisplayCurrentImage()
        {
            if (!HasCurrentImage())
            {
                ClearDisplayedImage("画像がありません。");
                return;
            }

            string imagePath = imagePaths[currentImageIndex];

            try
            {
                using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sourceImage = Image.FromStream(fileStream))
                {
                    ReplaceDisplayedImage(new Bitmap(sourceImage));
                }

                label1.Text = Path.GetFileName(imagePath);
                label12.Text = string.Format("{0}枚中{1}枚目", imagePaths.Count, currentImageIndex + 1);
                if (previewFileNameValueLabel != null)
                {
                    previewFileNameValueLabel.Text = label1.Text;
                }

                if (previewProgressValueLabel != null)
                {
                    previewProgressValueLabel.Text = label12.Text;
                }

                UpdateNavigationButtons();
                UpdateImageBrowserCurrentPathIfOpen(imagePath);
            }
            catch (Exception ex)
            {
                logger.Fatal(string.Format("画像表示でエラーが発生しました。 Path={0}", imagePath), ex);
                MessageBox.Show("画像の表示に失敗しました。");
            }
        }

        /// <summary>
        /// 現在の表示画像を入れ替える
        /// </summary>
        private void ReplaceDisplayedImage(Image nextImage)
        {
            Image previousImage = pictureBox1.Image;
            pictureBox1.Image = nextImage;
            previousImage?.Dispose();
        }

        /// <summary>
        /// 画像表示をクリアする
        /// </summary>
        private void ClearDisplayedImage(string statusText)
        {
            ReplaceDisplayedImage(null);
            label1.Text = string.Empty;
            label12.Text = statusText;
            if (previewFileNameValueLabel != null)
            {
                previewFileNameValueLabel.Text = string.Empty;
            }

            if (previewProgressValueLabel != null)
            {
                previewProgressValueLabel.Text = statusText;
            }

            UpdateNavigationButtons();
            UpdateImageBrowserCurrentPathIfOpen(string.Empty);
        }
        #endregion 画像表示

        #region ナビゲーション
        /// <summary>
        /// 次の画像へ進む
        /// </summary>
        private void ShowNextImage()
        {
            if (!CanShowNextImage())
            {
                return;
            }

            currentImageIndex++;
            DisplayCurrentImage();
        }

        /// <summary>
        /// 前の画像へ戻る
        /// </summary>
        private void ShowPreviousImage()
        {
            if (!CanShowPreviousImage())
            {
                return;
            }

            currentImageIndex--;
            DisplayCurrentImage();
        }

        /// <summary>
        /// ボタンの活性状態を更新する
        /// </summary>
        private void UpdateNavigationButtons()
        {
            button3.Enabled = CanShowPreviousImage();
            button4.Enabled = CanShowNextImage();
            if (browserButton != null)
            {
                browserButton.Enabled = imagePaths.Count > 0;
            }

            UpdateDestinationActionButtons();
            UpdateUndoState();
        }

        /// <summary>
        /// 前の画像へ戻れるか
        /// </summary>
        private bool CanShowPreviousImage()
        {
            return imagePaths.Count > 0 && currentImageIndex > 0;
        }

        /// <summary>
        /// 次の画像へ進めるか
        /// </summary>
        private bool CanShowNextImage()
        {
            return imagePaths.Count > 0 && currentImageIndex >= 0 && currentImageIndex < imagePaths.Count - 1;
        }
        #endregion ナビゲーション

        #region 画像回転
        /// <summary>
        /// 右へ90度回転
        /// </summary>
        private void ImageRightTurn90()
        {
            RotateCurrentImage(RotateFlipType.Rotate90FlipNone);
        }

        /// <summary>
        /// 左へ90度回転
        /// </summary>
        private void ImageLeftTurn90()
        {
            RotateCurrentImage(RotateFlipType.Rotate270FlipNone);
        }

        /// <summary>
        /// 現在の表示画像を回転する
        /// </summary>
        private void RotateCurrentImage(RotateFlipType rotateFlipType)
        {
            try
            {
                if (pictureBox1.Image == null)
                {
                    return;
                }

                pictureBox1.Image.RotateFlip(rotateFlipType);
                pictureBox1.Refresh();
            }
            catch (Exception ex)
            {
                logger.Fatal("画像回転でエラーが発生しました。", ex);
            }
        }
        #endregion 画像回転

        #region 画像移動
        /// <summary>
        /// 現在の画像を指定先へ移動する
        /// </summary>
        private void MoveCurrentImage(TextBox destinationTextBox)
        {
            try
            {
                if (!HasCurrentImage())
                {
                    MessageBox.Show("移動する画像がありません。");
                    return;
                }

                if (!TryGetExistingDirectory(destinationTextBox.Text, out string destinationDirectory))
                {
                    MessageBox.Show("移動先フォルダを指定してください。");
                    return;
                }

                var result = MoveImagesToDirectory(
                    new[] { imagePaths[currentImageIndex] },
                    destinationDirectory,
                    GetDestinationLabel(destinationTextBox));

                if (result.MovedCount == 0 && result.SkippedMessages.Count > 0)
                {
                    MessageBox.Show(result.SkippedMessages[0], AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("画像移動でエラーが発生しました。", ex);
                MessageBox.Show("画像の移動に失敗しました。");
            }
        }
        #endregion 画像移動

        #region フォルダ選択
        /// <summary>
        /// Explorer 風のフォルダ選択ダイアログを表示する
        /// </summary>
        private void BrowseForFolder(TextBox targetTextBox)
        {
            try
            {
                using (var dialog = new FolderPickerDialog())
                {
                    dialog.Title = "フォルダを指定してください。";
                    dialog.InitialFolder = ResolveInitialDirectory(targetTextBox);

                    if (dialog.ShowDialog(Handle))
                    {
                        targetTextBox.Text = dialog.SelectedPath;
                        SaveSettingSafe();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("フォルダ選択でエラーが発生しました。", ex);
                MessageBox.Show("フォルダの選択に失敗しました。");
            }
        }

        /// <summary>
        /// ダイアログの初期表示フォルダを決定する
        /// </summary>
        private string ResolveInitialDirectory(TextBox targetTextBox)
        {
            if (TryGetExistingDirectory(targetTextBox.Text, out string currentPath))
            {
                return currentPath;
            }

            if (!ReferenceEquals(targetTextBox, textBox1) && TryGetExistingDirectory(textBox1.Text, out string sourcePath))
            {
                return sourcePath;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }
        #endregion フォルダ選択

        #region 移動先アクションボタン
        /// <summary>
        /// 移動先アクションボタン押下
        /// </summary>
        private void DestinationMoveButton_Click(object sender, EventArgs e)
        {
            if (!(sender is Button moveButton) || moveButton.Tag == null)
            {
                return;
            }

            int destinationIndex = Convert.ToInt32(moveButton.Tag);
            if (destinationIndex < 0 || destinationIndex >= destinationTextBoxes.Count)
            {
                return;
            }

            MoveCurrentImage(destinationTextBoxes[destinationIndex]);
        }

        /// <summary>
        /// 移動先アクションボタンの活性状態を更新する
        /// </summary>
        private void UpdateDestinationActionButtons()
        {
            if (destinationMoveButtons.Count == 0)
            {
                return;
            }

            bool hasCurrentImage = HasCurrentImage();

            foreach (Button moveButton in destinationMoveButtons)
            {
                int destinationIndex = moveButton.Tag is int tagValue ? tagValue : Convert.ToInt32(moveButton.Tag);
                bool hasDestinationDirectory =
                    destinationIndex >= 0 &&
                    destinationIndex < destinationTextBoxes.Count &&
                    TryGetExistingDirectory(destinationTextBoxes[destinationIndex].Text, out _);
                moveButton.Enabled = hasCurrentImage && hasDestinationDirectory;
            }
        }
        #endregion 移動先アクションボタン

        #region アンドゥ
        /// <summary>
        /// 元に戻すボタン押下
        /// </summary>
        private void UndoButton_Click(object sender, EventArgs e)
        {
            UndoLastMoveAction();
        }

        /// <summary>
        /// 直前の移動を元に戻す
        /// </summary>
        private void UndoLastMoveAction()
        {
            if (moveHistoryActions.Count == 0)
            {
                MessageBox.Show("元に戻せる移動がありません。", AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MoveHistoryAction action = moveHistoryActions.Peek();
            string validationMessage = ValidateUndoAction(action);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                MessageBox.Show(validationMessage, AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                foreach (MoveHistoryItem item in action.Items.OrderBy(historyItem => historyItem.OriginalIndex))
                {
                    string sourceDirectory = Path.GetDirectoryName(item.SourcePath) ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(sourceDirectory))
                    {
                        Directory.CreateDirectory(sourceDirectory);
                    }

                    File.Move(item.DestinationPath, item.SourcePath);
                    RestoreMovedImagePath(item);
                }

                moveHistoryActions.Pop();
                FocusRestoredImage(action);
                UpdateUndoState();
                SaveSettingSafe();
                RefreshImageBrowserItemsIfOpen();
            }
            catch (Exception ex)
            {
                logger.Error("アンドゥに失敗しました。", ex);
                MessageBox.Show("元に戻す処理に失敗しました。", AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// アンドゥ可否表示を更新する
        /// </summary>
        private void UpdateUndoState()
        {
            bool canUndo = moveHistoryActions.Count > 0;

            if (undoButton != null)
            {
                undoButton.Enabled = canUndo;
            }

            if (undoMoveToolStripMenuItem != null)
            {
                undoMoveToolStripMenuItem.Enabled = canUndo;
            }
        }

        /// <summary>
        /// アンドゥ可能か検証する
        /// </summary>
        private string ValidateUndoAction(MoveHistoryAction action)
        {
            foreach (MoveHistoryItem item in action.Items)
            {
                if (!File.Exists(item.DestinationPath))
                {
                    return "元に戻す対象ファイルが移動先に見つかりません。";
                }

                if (File.Exists(item.SourcePath))
                {
                    return "元の場所に同名ファイルがあるため、元に戻せません。";
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 元に戻した画像を一覧へ差し戻す
        /// </summary>
        private void RestoreMovedImagePath(MoveHistoryItem item)
        {
            int insertIndex = Math.Max(0, Math.Min(item.OriginalIndex, imagePaths.Count));
            imagePaths.Insert(insertIndex, item.SourcePath);
        }

        /// <summary>
        /// 元に戻した画像へフォーカスする
        /// </summary>
        private void FocusRestoredImage(MoveHistoryAction action)
        {
            string focusPath = action.Items
                .OrderBy(historyItem => historyItem.OriginalIndex)
                .Select(historyItem => historyItem.SourcePath)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(focusPath))
            {
                int restoredIndex = imagePaths.FindIndex(path => string.Equals(path, focusPath, StringComparison.OrdinalIgnoreCase));
                if (restoredIndex >= 0)
                {
                    currentImageIndex = restoredIndex;
                }
            }

            NormalizeCurrentIndex();

            if (HasCurrentImage())
            {
                DisplayCurrentImage();
            }
            else
            {
                ClearDisplayedImage("画像がありません。");
            }
        }
        #endregion アンドゥ

        #region 一覧別窓
        /// <summary>
        /// 一覧別窓を開く
        /// </summary>
        private void OpenImageListBrowser_Click(object sender, EventArgs e)
        {
            if (imageListBrowserForm == null || imageListBrowserForm.IsDisposed)
            {
                imageListBrowserForm = new ImageListBrowserForm(this);
                imageListBrowserForm.FormClosed += (_, __) => imageListBrowserForm = null;
                imageListBrowserForm.Show(this);
            }
            else
            {
                imageListBrowserForm.BringToFront();
            }

            imageListBrowserForm.RefreshItems();
        }

        /// <summary>
        /// 一覧別窓が開いていれば一覧を再構築する
        /// </summary>
        private void RefreshImageBrowserItemsIfOpen()
        {
            if (imageListBrowserForm != null && !imageListBrowserForm.IsDisposed)
            {
                imageListBrowserForm.RefreshItems();
            }
        }

        /// <summary>
        /// 一覧別窓が開いていれば現在位置だけ更新する
        /// </summary>
        private void UpdateImageBrowserCurrentPathIfOpen(string currentPath)
        {
            if (imageListBrowserForm != null && !imageListBrowserForm.IsDisposed)
            {
                imageListBrowserForm.UpdateCurrentPath(currentPath);
            }
        }

        /// <summary>
        /// 一覧別窓用の画像一覧スナップショットを返す
        /// </summary>
        internal ImageBrowserSnapshot GetImageBrowserSnapshot()
        {
            string sourceRoot = NormalizeDirectoryPath(textBox1.Text);
            return new ImageBrowserSnapshot(
                imagePaths.ToArray(),
                sourceRoot,
                GetCurrentImagePath() ?? string.Empty,
                currentImageIndex);
        }

        /// <summary>
        /// 親画面で設定済みの移動先候補を返す
        /// </summary>
        internal IReadOnlyList<DestinationChoice> GetDestinationChoices()
        {
            var labels = new[]
            {
                label2.Text,
                label3.Text,
                label4.Text,
                label5.Text,
                label6.Text,
                label7.Text,
                label8.Text,
                label9.Text,
                label10.Text,
                label11.Text
            };

            var choices = new List<DestinationChoice>();
            for (int index = 0; index < destinationTextBoxes.Count; index++)
            {
                if (!TryGetExistingDirectory(destinationTextBoxes[index].Text, out string directory))
                {
                    continue;
                }

                choices.Add(new DestinationChoice
                {
                    DisplayName = $"{labels[index]}: {directory}",
                    FolderPath = directory
                });
            }

            return choices;
        }

        /// <summary>
        /// 一覧別窓から指定画像へジャンプする
        /// </summary>
        internal bool ShowImageByPath(string fullPath)
        {
            int nextIndex = imagePaths.FindIndex(path => string.Equals(path, fullPath, StringComparison.OrdinalIgnoreCase));
            if (nextIndex < 0)
            {
                return false;
            }

            currentImageIndex = nextIndex;
            DisplayCurrentImage();
            Activate();
            return true;
        }

        /// <summary>
        /// 一覧別窓から複数画像を移動する
        /// </summary>
        internal BatchMoveExecutionResult MoveImagesFromBrowser(
            IReadOnlyList<string> sourcePaths,
            string destinationDirectory,
            string selectionLabel,
            Action<BatchMoveProgressInfo> progressCallback = null)
        {
            return MoveImagesToDirectory(sourcePaths, destinationDirectory, selectionLabel, progressCallback);
        }
        #endregion 一覧別窓

        #region 設定保存・復元
        /// <summary>
        /// 設定を安全に保存する
        /// </summary>
        private void SaveSettingSafe()
        {
            try
            {
                SaveSetting();
            }
            catch (Exception ex)
            {
                logger.Error("設定保存に失敗しました。", ex);
            }
        }

        /// <summary>
        /// 設定が存在すれば読み込む
        /// </summary>
        private void LoadSettingIfExists()
        {
            if (!File.Exists(settingFileName))
            {
                return;
            }

            try
            {
                LoadSetting();
            }
            catch (Exception ex)
            {
                logger.Error("設定読み込みに失敗しました。", ex);
            }
        }

        /// <summary>
        /// 移動先保存
        /// </summary>
        private void SaveSetting()
        {
            Rectangle windowBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            var setting = new SaveSetting
            {
                From = NormalizeDirectoryPath(textBox1.Text),
                Num0 = NormalizeDirectoryPath(textBox2.Text),
                Num1 = NormalizeDirectoryPath(textBox3.Text),
                Num2 = NormalizeDirectoryPath(textBox4.Text),
                Num3 = NormalizeDirectoryPath(textBox5.Text),
                Num4 = NormalizeDirectoryPath(textBox6.Text),
                Num5 = NormalizeDirectoryPath(textBox7.Text),
                Num6 = NormalizeDirectoryPath(textBox8.Text),
                Num7 = NormalizeDirectoryPath(textBox9.Text),
                Num8 = NormalizeDirectoryPath(textBox10.Text),
                Num9 = NormalizeDirectoryPath(textBox11.Text),
                WindowLeft = windowBounds.Left,
                WindowTop = windowBounds.Top,
                WindowWidth = windowBounds.Width,
                WindowHeight = windowBounds.Height,
                WindowState = WindowState.ToString()
            };

            var serializer = new XmlSerializer(typeof(SaveSetting));
            using (var streamWriter = new StreamWriter(settingFileName, false, new UTF8Encoding(false)))
            {
                serializer.Serialize(streamWriter, setting);
            }
        }

        /// <summary>
        /// 移動先復元
        /// </summary>
        private void LoadSetting()
        {
            var serializer = new XmlSerializer(typeof(SaveSetting));

            using (var streamReader = new StreamReader(settingFileName, new UTF8Encoding(false)))
            {
                var setting = (SaveSetting)serializer.Deserialize(streamReader);

                textBox1.Text = NormalizeDirectoryPath(setting.From);
                textBox2.Text = NormalizeDirectoryPath(setting.Num0);
                textBox3.Text = NormalizeDirectoryPath(setting.Num1);
                textBox4.Text = NormalizeDirectoryPath(setting.Num2);
                textBox5.Text = NormalizeDirectoryPath(setting.Num3);
                textBox6.Text = NormalizeDirectoryPath(setting.Num4);
                textBox7.Text = NormalizeDirectoryPath(setting.Num5);
                textBox8.Text = NormalizeDirectoryPath(setting.Num6);
                textBox9.Text = NormalizeDirectoryPath(setting.Num7);
                textBox10.Text = NormalizeDirectoryPath(setting.Num8);
                textBox11.Text = NormalizeDirectoryPath(setting.Num9);
                ApplySavedWindowBounds(setting);
            }
        }

        /// <summary>
        /// メニューから設定保存
        /// </summary>
        private void MenuSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSetting();
                MessageBox.Show("設定を保存しました。", AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Error("設定保存に失敗しました。", ex);
                MessageBox.Show("設定保存に失敗しました。", AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// メニューから設定読込
        /// </summary>
        private void MenuLoadSettings_Click(object sender, EventArgs e)
        {
            if (!File.Exists(settingFileName))
            {
                MessageBox.Show("設定ファイルが見つかりません。", AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                LoadSetting();
                UpdateDestinationActionButtons();
                MessageBox.Show("設定を読み込みました。", AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Error("設定読み込みに失敗しました。", ex);
                MessageBox.Show("設定読み込みに失敗しました。", AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion 設定保存・復元

        #region ショートカット
        /// <summary>
        /// ショートカットキーを処理する
        /// </summary>
        private bool TryHandleShortcut(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                    ShowNextImage();
                    return true;
                case Keys.Left:
                    ShowPreviousImage();
                    return true;
                case Keys.Up:
                    ImageRightTurn90();
                    return true;
                case Keys.Down:
                    ImageLeftTurn90();
                    return true;
                case Keys.F5:
                    ReloadImages();
                    return true;
                case Keys.Control | Keys.Z:
                    UndoLastMoveAction();
                    return true;
                case Keys.D0:
                case Keys.NumPad0:
                    return MoveByShortcut(0);
                case Keys.D1:
                case Keys.NumPad1:
                    return MoveByShortcut(1);
                case Keys.D2:
                case Keys.NumPad2:
                    return MoveByShortcut(2);
                case Keys.D3:
                case Keys.NumPad3:
                    return MoveByShortcut(3);
                case Keys.D4:
                case Keys.NumPad4:
                    return MoveByShortcut(4);
                case Keys.D5:
                case Keys.NumPad5:
                    return MoveByShortcut(5);
                case Keys.D6:
                case Keys.NumPad6:
                    return MoveByShortcut(6);
                case Keys.D7:
                case Keys.NumPad7:
                    return MoveByShortcut(7);
                case Keys.D8:
                case Keys.NumPad8:
                    return MoveByShortcut(8);
                case Keys.D9:
                case Keys.NumPad9:
                    return MoveByShortcut(9);
                default:
                    return false;
            }
        }

        /// <summary>
        /// キーに対応する移動先へ画像を移動する
        /// </summary>
        private bool MoveByShortcut(int destinationIndex)
        {
            if (destinationIndex < 0 || destinationIndex >= destinationTextBoxes.Count)
            {
                return false;
            }

            MoveCurrentImage(destinationTextBoxes[destinationIndex]);
            return true;
        }

        /// <summary>
        /// 複数画像を指定先へ移動する共通処理
        /// </summary>
        private BatchMoveExecutionResult MoveImagesToDirectory(
            IEnumerable<string> sourcePaths,
            string destinationDirectory,
            string actionLabel,
            Action<BatchMoveProgressInfo> progressCallback = null)
        {
            var result = new BatchMoveExecutionResult();
            string normalizedDestinationDirectory = NormalizeDirectoryPath(destinationDirectory);
            List<string> distinctSourcePaths = sourcePaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            int totalCount = distinctSourcePaths.Count;
            int processedCount = 0;
            int skippedCount = 0;

            if (string.IsNullOrWhiteSpace(normalizedDestinationDirectory) || !Directory.Exists(normalizedDestinationDirectory))
            {
                result.SkippedMessages.Add("移動先フォルダを指定してください。");
                progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, 0, 0, 1, "移動先フォルダを指定してください。", string.Empty));
                return result;
            }

            var movedItems = new List<MoveHistoryItem>();
            progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, 0, 0, 0, "一括移動を開始しました。", string.Empty));

            foreach (string sourcePath in distinctSourcePaths)
            {
                string currentFileName = Path.GetFileName(sourcePath);
                string sourceDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                if (!File.Exists(sourcePath))
                {
                    result.SkippedMessages.Add($"ファイルが見つからないため移動できません: {Path.GetFileName(sourcePath)}");
                    processedCount++;
                    skippedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "ファイルが見つからないためスキップしました。", currentFileName));
                    continue;
                }

                if (string.Equals(sourceDirectory, normalizedDestinationDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    result.SkippedMessages.Add($"同じフォルダには移動できません: {Path.GetFileName(sourcePath)}");
                    processedCount++;
                    skippedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "同じフォルダへの移動をスキップしました。", currentFileName));
                    continue;
                }

                string destinationPath = Path.Combine(normalizedDestinationDirectory, Path.GetFileName(sourcePath));
                if (File.Exists(destinationPath))
                {
                    result.SkippedMessages.Add($"移動先に同名ファイルがあります: {Path.GetFileName(sourcePath)}");
                    processedCount++;
                    skippedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "移動先に同名ファイルがあるためスキップしました。", currentFileName));
                    continue;
                }

                int originalIndex = imagePaths.FindIndex(path => string.Equals(path, sourcePath, StringComparison.OrdinalIgnoreCase));

                try
                {
                    File.Move(sourcePath, destinationPath);
                    RemoveImagePathFromQueue(sourcePath);

                    movedItems.Add(new MoveHistoryItem
                    {
                        SourcePath = sourcePath,
                        DestinationPath = destinationPath,
                        OriginalIndex = originalIndex >= 0 ? originalIndex : imagePaths.Count
                    });

                    result.MovedSourcePaths.Add(sourcePath);
                    processedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "移動しました。", currentFileName));
                }
                catch (Exception ex)
                {
                    logger.Error($"画像移動に失敗しました。 Source={sourcePath} Destination={destinationPath}", ex);
                    result.SkippedMessages.Add($"移動に失敗しました: {Path.GetFileName(sourcePath)}");
                    processedCount++;
                    skippedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "移動に失敗しました。", currentFileName));
                }
            }

            if (movedItems.Count > 0)
            {
                moveHistoryActions.Push(new MoveHistoryAction(actionLabel, movedItems));
                NormalizeCurrentIndex();

                if (HasCurrentImage())
                {
                    DisplayCurrentImage();
                }
                else
                {
                    ClearDisplayedImage("画像がありません。");
                }

                RefreshImageBrowserItemsIfOpen();
                SaveSettingSafe();
            }
            else
            {
                UpdateNavigationButtons();
                RefreshImageBrowserItemsIfOpen();
            }

            result.MovedCount = movedItems.Count;
            result.ActionLabel = actionLabel;
            UpdateUndoState();
            progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "一括移動の処理が終わりました。", string.Empty));
            return result;
        }

        /// <summary>
        /// キューから指定画像を除外する
        /// </summary>
        private void RemoveImagePathFromQueue(string sourcePath)
        {
            int removeIndex = imagePaths.FindIndex(path => string.Equals(path, sourcePath, StringComparison.OrdinalIgnoreCase));
            if (removeIndex < 0)
            {
                return;
            }

            imagePaths.RemoveAt(removeIndex);

            if (removeIndex < currentImageIndex)
            {
                currentImageIndex--;
            }
            else if (removeIndex == currentImageIndex && currentImageIndex >= imagePaths.Count)
            {
                currentImageIndex = imagePaths.Count - 1;
            }
        }

        /// <summary>
        /// 現在インデックスを正規化する
        /// </summary>
        private void NormalizeCurrentIndex()
        {
            if (imagePaths.Count == 0)
            {
                currentImageIndex = -1;
                return;
            }

            if (currentImageIndex < 0)
            {
                currentImageIndex = 0;
            }
            else if (currentImageIndex >= imagePaths.Count)
            {
                currentImageIndex = imagePaths.Count - 1;
            }
        }

        /// <summary>
        /// 表示用の移動先名を取得する
        /// </summary>
        private string GetDestinationLabel(TextBox destinationTextBox)
        {
            int destinationIndex = destinationTextBoxes.IndexOf(destinationTextBox);
            if (destinationIndex < 0)
            {
                return "指定フォルダ";
            }

            string[] labels = { label2.Text, label3.Text, label4.Text, label5.Text, label6.Text, label7.Text, label8.Text, label9.Text, label10.Text, label11.Text };
            return labels[destinationIndex];
        }

        /// <summary>
        /// パス入力欄にフォーカスがあるか
        /// </summary>
        private bool IsPathTextBoxFocused()
        {
            return EnumeratePathTextBoxes().Any(textBox => textBox.Focused);
        }
        #endregion ショートカット

        #region 共通ユーティリティ
        /// <summary>
        /// 現在表示中の画像パスを取得する
        /// </summary>
        private string GetCurrentImagePath()
        {
            if (!HasCurrentImage())
            {
                return null;
            }

            return imagePaths[currentImageIndex];
        }

        /// <summary>
        /// 現在有効な画像があるか
        /// </summary>
        private bool HasCurrentImage()
        {
            return currentImageIndex >= 0 && currentImageIndex < imagePaths.Count;
        }

        /// <summary>
        /// フォルダパスを正規化する
        /// </summary>
        private string NormalizeDirectoryPath(string rawPath)
        {
            return string.IsNullOrWhiteSpace(rawPath) ? string.Empty : rawPath.Trim().Trim('"');
        }

        /// <summary>
        /// 実在するフォルダか確認する
        /// </summary>
        private bool TryGetExistingDirectory(string rawPath, out string normalizedPath)
        {
            normalizedPath = NormalizeDirectoryPath(rawPath);
            return !string.IsNullOrWhiteSpace(normalizedPath) && Directory.Exists(normalizedPath);
        }

        /// <summary>
        /// 相対パスを構築する
        /// </summary>
        private string BuildRelativePath(string sourceRoot, string fullPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(sourceRoot) && Directory.Exists(sourceRoot))
                {
                    string normalizedRoot = Path.GetFullPath(sourceRoot);
                    if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    {
                        normalizedRoot += Path.DirectorySeparatorChar;
                    }

                    Uri rootUri = new Uri(normalizedRoot);
                    Uri pathUri = new Uri(Path.GetFullPath(fullPath));
                    string relativePath = Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString());
                    return relativePath.Replace('/', Path.DirectorySeparatorChar);
                }
            }
            catch
            {
                // 相対パス化に失敗した場合はフルパスを返す
            }

            return fullPath;
        }

        /// <summary>
        /// 保存済みの画面サイズと位置を復元する
        /// </summary>
        private void ApplySavedWindowBounds(SaveSetting setting)
        {
            if (!TryGetRestorableWindowBounds(setting, out Rectangle restoredBounds))
            {
                return;
            }

            StartPosition = FormStartPosition.Manual;
            Bounds = restoredBounds;

            if (string.Equals(setting.WindowState, FormWindowState.Maximized.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        /// <summary>
        /// 復元可能な画面サイズと位置かを判定する
        /// </summary>
        private bool TryGetRestorableWindowBounds(SaveSetting setting, out Rectangle restoredBounds)
        {
            restoredBounds = Rectangle.Empty;

            if (setting == null || setting.WindowWidth < MinimumRestoreWidth || setting.WindowHeight < MinimumRestoreHeight)
            {
                return false;
            }

            Rectangle candidateBounds = new Rectangle(
                setting.WindowLeft,
                setting.WindowTop,
                setting.WindowWidth,
                setting.WindowHeight);

            foreach (Screen screen in Screen.AllScreens)
            {
                Rectangle workingArea = screen.WorkingArea;
                if (!workingArea.IntersectsWith(candidateBounds))
                {
                    continue;
                }

                int width = Math.Min(candidateBounds.Width, workingArea.Width);
                int height = Math.Min(candidateBounds.Height, workingArea.Height);
                int left = Math.Min(Math.Max(candidateBounds.Left, workingArea.Left), workingArea.Right - width);
                int top = Math.Min(Math.Max(candidateBounds.Top, workingArea.Top), workingArea.Bottom - height);
                restoredBounds = new Rectangle(left, top, width, height);
                return true;
            }

            return false;
        }
        #endregion 共通ユーティリティ
        #endregion メソッド
    }

    internal sealed class MoveHistoryAction
    {
        public MoveHistoryAction(string actionLabel, IEnumerable<MoveHistoryItem> items)
        {
            ActionLabel = actionLabel;
            Items = items.ToList();
        }

        public string ActionLabel { get; }

        public List<MoveHistoryItem> Items { get; }
    }

    internal sealed class MoveHistoryItem
    {
        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public int OriginalIndex { get; set; }
    }

    internal sealed class BatchMoveExecutionResult
    {
        public string ActionLabel { get; set; }

        public int MovedCount { get; set; }

        public List<string> MovedSourcePaths { get; } = new List<string>();

        public List<string> SkippedMessages { get; } = new List<string>();
    }

    internal sealed class BatchMoveProgressInfo
    {
        public BatchMoveProgressInfo(int totalCount, int processedCount, int movedCount, int skippedCount, string statusText, string currentFileName)
        {
            TotalCount = totalCount;
            ProcessedCount = processedCount;
            MovedCount = movedCount;
            SkippedCount = skippedCount;
            StatusText = statusText ?? string.Empty;
            CurrentFileName = currentFileName ?? string.Empty;
        }

        public int TotalCount { get; }

        public int ProcessedCount { get; }

        public int MovedCount { get; }

        public int SkippedCount { get; }

        public string StatusText { get; }

        public string CurrentFileName { get; }
    }
}
