using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Configuration;
using System.Xml.Serialization;
using log4net;

namespace ImageMove
{
    public partial class Main : Form
    {
        #region フィールド
        /// <summary> ロガー </summary>
        static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary> 画像ファイルパスのリスト </summary>
        private List<string> ImagePathList;
        /// <summary> 取得した画像パスの最大値 </summary>
        private int MaxGetPathCount;
        /// <summary> 現在表示している画像のパス </summary>
        private int NowPathCount = -1;
        /// <summary> 表示画像 </summary>
        private Image DisplayImage;
        /// <summary> →キー </summary>
        private const string KeysRight = "Right";
        /// <summary> ←キー </summary>
        private const string KeysLeft = "Left";
        /// <summary> ↑キー </summary>
        private const string KeysUp = "Up";
        /// <summary> ↓キー </summary>
        private const string KeysDown = "Down";

        /// <summary> 0キー </summary>
        private const string KeysNumPad0 = "NumPad0";
        /// <summary> 1キー </summary>
        private const string KeysNumPad1 = "NumPad1";
        /// <summary> 2キー </summary>
        private const string KeysNumPad2 = "NumPad2";
        /// <summary> 3キー </summary>
        private const string KeysNumPad3 = "NumPad3";
        /// <summary> 4キー </summary>
        private const string KeysNumPad4 = "NumPad4";
        /// <summary> 5キー </summary>
        private const string KeysNumPad5 = "NumPad5";
        /// <summary> 6キー </summary>
        private const string KeysNumPad6 = "NumPad6";
        /// <summary> 7キー </summary>
        private const string KeysNumPad7 = "NumPad7";
        /// <summary> 8キー </summary>
        private const string KeysNumPad8 = "NumPad8";
        /// <summary> 9キー </summary>
        private const string KeysNumPad9 = "NumPad9";
        /// <summary> 設定保存ファイル </summary>
        private readonly string SettingFileName = Directory.GetCurrentDirectory() + @"\setting.xml";

        #endregion フィールド

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Main()
        {
            InitializeComponent();
            this.KeyPreview = true;
            if (File.Exists(SettingFileName))
            {
                LoadSetting();
            }
        }
        #endregion コンストラクタ

        #region イベント
        #region 参照ボタン押下 取得パス
        /// <summary>
        /// 参照ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            logger.Info("参照ボタン押下　開始");
            try
            {
                GetDir(textBox1);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            logger.Info("参照ボタン押下　終了");
        }
        #endregion 参照ボタン押下 取得パス

        #region 画像ファイル取得ボタン押下
        /// <summary>
        /// 画像ファイル取得ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            logger.Info("画像ファイル取得ボタン押下　開始");
            try
            {
                this.Enabled = false;
                NowPathCount = 0;
                button3.Enabled = false;
                var targetDirPath = textBox1.Text.ToString();
                ImagePathList = new List<string>();
                var getJpg = Directory.EnumerateFiles(targetDirPath, "*.jpg", SearchOption.AllDirectories).ToList();
                var getPng = Directory.EnumerateFiles(targetDirPath, "*.png", SearchOption.AllDirectories).ToList();
                if (getJpg.Count > 0)
                {
                    ImagePathList.AddRange(getJpg);
                }
                if (getPng.Count > 0)
                {
                    ImagePathList.AddRange(getPng);
                }

                if (ImagePathList.Count() > 0)
                {
                    MaxGetPathCount = ImagePathList.Count() - 1;
                    ImageDisplay();
                    if(MaxGetPathCount == 1)
                    {
                        button4.Enabled = false;
                    }
                    button5.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラーです。");
                logger.Fatal("画像ファイル取得ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
            logger.Info("画像ファイル取得ボタン押下　終了");
        }
        #endregion 画像ファイル取得ボタン押下

        #region 参照ボタン押下 移動パス
        /// <summary>
        /// 参照ボタン押下 移動パス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox2);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox3);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox4);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox5);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox6);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox7);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox8);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox9);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox10);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                GetDir(textBox11);
            }
            catch (Exception ex)
            {
                logger.Fatal("参照ボタン押下でエラーが発生しました。", ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }
        #endregion 参照ボタン押下 移動パス

        #region 前の画像ボタン押下
        /// <summary>
        /// 前の画像ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            Imageback();
        }
        #endregion 前の画像ボタン押下

        #region 次の画像ボタン押下
        /// <summary>
        /// 次の画像ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            ImageForward();
        }
        #endregion 次の画像ボタン押下 

        #region 画像の移動ボタン押下
        /// <summary>
        ///  画像の移動ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            MoveImage(textBox2);
        }
        #endregion 画像の移動ボタン押下

        #region 終了ボタン押下
        /// <summary>
        /// 終了ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click_1(object sender, EventArgs e)
        {
            SaveSetting();
            Environment.Exit(0);
        }
        #endregion 終了ボタン押下

        #region フォーム上でキーボード押下された
        /// <summary>
        /// フォーム上でキーボード押下された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            var keyData = e.KeyData.ToString();

            switch (keyData)
            {
                case KeysRight:
                    if (button4.Enabled == true)
                    {
                        ImageForward();
                    }
                    break;
                case KeysLeft:
                    if (button3.Enabled == true)
                    {
                        Imageback();
                    }
                    break;
                case KeysUp:
                    if (button4.Enabled == true)
                    {
                        ImageRightTurn90();
                    }
                    break;
                case KeysDown:

                    if (button3.Enabled == true)
                    {
                        ImageLeftTurn90();
                    }
                    break;
                case KeysNumPad0:
                    MoveImage(textBox2);
                    break;
                case KeysNumPad1:
                    MoveImage(textBox3);
                    break;
                case KeysNumPad2:
                    MoveImage(textBox4);
                    break;
                case KeysNumPad3:
                    MoveImage(textBox5);
                    break;
                case KeysNumPad4:
                    MoveImage(textBox6);
                    break;
                case KeysNumPad5:
                    MoveImage(textBox7);
                    break;
                case KeysNumPad6:
                    MoveImage(textBox8);
                    break;
                case KeysNumPad7:
                    MoveImage(textBox9);
                    break;
                case KeysNumPad8:
                    MoveImage(textBox10);
                    break;
                case KeysNumPad9:
                    MoveImage(textBox11);
                    break;
                default:
                    break;
            }
        }
        #endregion フォーム上でキーボード押下された
        #endregion イベント

        #region メソッド
        #region 画像表示
        /// <summary>
        /// 画像表示
        /// </summary>
        private void ImageDisplay()
        {
            using (FileStream fs = new FileStream(ImagePathList[NowPathCount], FileMode.Open))
            {
                var abyData = new byte[fs.Length];
                fs.Read(abyData, 0, (int)fs.Length);

                using(MemoryStream ms = new MemoryStream(abyData))
                {
                    var m_bmp = new Bitmap(ms);
                    DisplayImage = (Image)m_bmp;
                }
            }
            pictureBox1.Image = DisplayImage;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            label1.Text = Path.GetFileName(ImagePathList[NowPathCount]);
            label12.Text = (MaxGetPathCount + 1).ToString() + "枚中" + (NowPathCount + 1).ToString() + "枚目";
        }
        #endregion 画像表示

        #region 次へ
        /// <summary>
        /// 次へ
        /// </summary>
        private void ImageForward()
        {
            try
            {
                if (NowPathCount == -1)
                {
                    return;
                }

                NowPathCount++;
                if (NowPathCount == 0)
                {
                    button3.Enabled = true;
                }
                ImageDisplay();

                if (NowPathCount == MaxGetPathCount)
                {
                    button4.Enabled = false;
                    button3.Enabled = true;
                }
                else if (MaxGetPathCount > NowPathCount)
                {
                    button3.Enabled = true;
                }
                else
                {
                    button4.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(string.Format("{0}でエラーが発生しました。", "次の画像ボタン押下"), ex);
            }

        }
        #endregion 次へ

        #region 前へ
        /// <summary>
        /// 前へ
        /// </summary>
        private void Imageback()
        {
            try
            {
                if (NowPathCount == -1)
                {
                    return;
                }

                NowPathCount--;
                ImageDisplay();

                if (NowPathCount == 0)
                {
                    button3.Enabled = false;
                    button4.Enabled = true;
                }
                else if (MaxGetPathCount > NowPathCount)
                {
                    button4.Enabled = true;
                }
                else
                {
                    button3.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(string.Format("{0}でエラーが発生しました。", "前の画像ボタン押下"), ex);
            }
        }
        #endregion 前へ

        #region 右へ90度回転
        /// <summary>
        /// 右へ90度回転
        /// </summary>
        private void ImageRightTurn90()
        {
            try
            {
                pictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                pictureBox1.Refresh();
            }
            catch (Exception ex)
            {
                logger.Fatal(string.Format("{0}でエラーが発生しました。", "↑ボタン押下"), ex);
            }
        }
        #endregion 右へ90度回転

        #region 左へ90度回転
        /// <summary>
        /// 前へ
        /// </summary>
        private void ImageLeftTurn90()
        {
            try
            {
                pictureBox1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                pictureBox1.Refresh();
            }
            catch (Exception ex)
            {
                logger.Fatal(string.Format("{0}でエラーが発生しました。", "↓ボタン押下"), ex);
            }
        }
        #endregion 左へ90度回転

        #region 画像移動
        private void MoveImage(TextBox MovePath)
        {
            try
            {
                if (string.IsNullOrEmpty(MovePath.Text))
                {
                    MessageBox.Show("移動先フォルダを選択してください。");
                    return;
                }

                var moveTargetPath = MovePath.Text;
                moveTargetPath = moveTargetPath + @"\" + Path.GetFileName(ImagePathList[NowPathCount]);

                File.Move(ImagePathList[NowPathCount], moveTargetPath);

                var removePath = ImagePathList[NowPathCount];

                ImageForward();

                ImagePathList.Remove(removePath);

                MaxGetPathCount--;
                NowPathCount--;
                if(NowPathCount == 0)
                {
                    button3.Enabled = false;
                }

                if(MaxGetPathCount == 0)
                {
                    button3.Enabled = false;
                    button4.Enabled = false;
                    button5.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(string.Format("{0}でエラーが発生しました。", ""), ex);
            }
            finally
            {

            }
        }
        #endregion 画像移動

        #region フォルダ選択ダイアログ表示＆フォルダ選択
        /// <summary>
        /// フォルダ選択ダイアログ表示＆フォルダ選択
        /// </summary>
        /// <param name="targetTexbox">表示対象のテキストボックス</param>
        private void GetDir(TextBox targetTexbox)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                //上部に表示する説明テキストを指定する
                fbd.Description = "フォルダを指定してください。";
                //ルートフォルダを指定する
                //デフォルトでDesktop
                fbd.RootFolder = Environment.SpecialFolder.Desktop;
                //最初に選択するフォルダを指定する
                //RootFolder以下にあるフォルダである必要がある
                fbd.SelectedPath = @"C:\Windows";
                //ユーザーが新しいフォルダを作成できるようにする
                //デフォルトでTrue
                fbd.ShowNewFolderButton = true;

                //ダイアログを表示する
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    //選択されたフォルダを表示する
                    targetTexbox.Text = fbd.SelectedPath;
                }
            }
        }
        #endregion フォルダ選択ダイアログ表示＆フォルダ選択

        #region 移動先保存
        /// <summary>
        /// 移動先保存
        /// </summary>
        private void SaveSetting()
        {
            var obj = new SaveSetting();
            obj.From = textBox1.Text;
            obj.Num0 = textBox2.Text;
            obj.Num1 = textBox3.Text;
            obj.Num2 = textBox4.Text;
            obj.Num3 = textBox5.Text;
            obj.Num4 = textBox6.Text;
            obj.Num5 = textBox7.Text;
            obj.Num6 = textBox8.Text;
            obj.Num7 = textBox9.Text;
            obj.Num8 = textBox10.Text;
            obj.Num9 = textBox11.Text;

            var serializer = new XmlSerializer(typeof(SaveSetting));
            using (var sw = new StreamWriter(SettingFileName, false, new UTF8Encoding(false)))
            {
                serializer.Serialize(sw, obj);
            }
        }
        #endregion 移動先保存

        #region 移動先復元
        /// <summary>
        /// 移動先復元
        /// </summary>
        private void LoadSetting()
        {
            var serializer = new XmlSerializer(typeof(SaveSetting));
            using (var sr = new StreamReader(SettingFileName, new UTF8Encoding(false)))
            {
                var obj = (SaveSetting)serializer.Deserialize(sr);
                textBox1.Text = obj.From;
                textBox2.Text = obj.Num0;
                textBox3.Text = obj.Num1;
                textBox4.Text = obj.Num2;
                textBox5.Text = obj.Num3;
                textBox6.Text = obj.Num4;
                textBox7.Text = obj.Num5;
                textBox8.Text = obj.Num6;
                textBox9.Text = obj.Num7;
                textBox10.Text = obj.Num8;
                textBox11.Text = obj.Num9;
            }
        }
        #endregion 移動先復元
        #endregion メソッド
    }
}
