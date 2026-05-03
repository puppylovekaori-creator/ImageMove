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

        /// <summary> 画像ファイルパスのリスト </summary>
        private List<string> imagePaths = new List<string>();

        /// <summary> 現在表示している画像のインデックス </summary>
        private int currentImageIndex = -1;
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
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            CreateDestinationMoveButtons();

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
                UpdateNavigationButtons();
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
            UpdateNavigationButtons();
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
            UpdateDestinationActionButtons();
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

                string sourcePath = imagePaths[currentImageIndex];
                string sourceDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;

                if (string.Equals(sourceDirectory, destinationDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("同じフォルダには移動できません。");
                    return;
                }

                string destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));

                if (File.Exists(destinationPath))
                {
                    MessageBox.Show("移動先に同名ファイルが存在します。");
                    return;
                }

                File.Move(sourcePath, destinationPath);
                imagePaths.RemoveAt(currentImageIndex);

                if (currentImageIndex >= imagePaths.Count)
                {
                    currentImageIndex = imagePaths.Count - 1;
                }

                if (HasCurrentImage())
                {
                    DisplayCurrentImage();
                }
                else
                {
                    ClearDisplayedImage("画像がありません。");
                }

                SaveSettingSafe();
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

            for (int index = 0; index < destinationMoveButtons.Count; index++)
            {
                bool hasDestinationDirectory = TryGetExistingDirectory(destinationTextBoxes[index].Text, out _);
                destinationMoveButtons[index].Enabled = hasCurrentImage && hasDestinationDirectory;
            }
        }
        #endregion 移動先アクションボタン

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
                Num9 = NormalizeDirectoryPath(textBox11.Text)
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
                MessageBox.Show("設定を保存しました。", "ImageMove", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Error("設定保存に失敗しました。", ex);
                MessageBox.Show("設定保存に失敗しました。", "ImageMove", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// メニューから設定読込
        /// </summary>
        private void MenuLoadSettings_Click(object sender, EventArgs e)
        {
            if (!File.Exists(settingFileName))
            {
                MessageBox.Show("設定ファイルが見つかりません。", "ImageMove", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                LoadSetting();
                UpdateDestinationActionButtons();
                MessageBox.Show("設定を読み込みました。", "ImageMove", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Error("設定読み込みに失敗しました。", ex);
                MessageBox.Show("設定読み込みに失敗しました。", "ImageMove", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        #endregion 共通ユーティリティ
        #endregion メソッド
    }
}
