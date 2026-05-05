using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMove
{
    /// <summary>
    /// 移動先パス保存クラス
    /// </summary>
    public class SaveSetting
    {
        /// <summary> 取得元 </summary>
        public string From;
        /// <summary> Num0 </summary>
        public string Num0;
        /// <summary> Num1 </summary>
        public string Num1;
        /// <summary> Num2 </summary>
        public string Num2;
        /// <summary> Num3 </summary>
        public string Num3;
        /// <summary> Num4 </summary>
        public string Num4;
        /// <summary> Num5 </summary>
        public string Num5;
        /// <summary> Num6 </summary>
        public string Num6;
        /// <summary> Num7 </summary>
        public string Num7;
        /// <summary> Num8 </summary>
        public string Num8;
        /// <summary> Num9 </summary>
        public string Num9;
        /// <summary> 画面左位置 </summary>
        public int WindowLeft;
        /// <summary> 画面上位置 </summary>
        public int WindowTop;
        /// <summary> 画面幅 </summary>
        public int WindowWidth;
        /// <summary> 画面高さ </summary>
        public int WindowHeight;
        /// <summary> 画面状態 </summary>
        public string WindowState;
        /// <summary> 左右ペインの境界位置 </summary>
        public int MainSplitterDistance;
        /// <summary> 読込元の最近使ったフォルダ </summary>
        public List<string> RecentSourceFolders = new List<string>();
        /// <summary> 移動先の最近使ったフォルダ </summary>
        public List<string> RecentDestinationFolders = new List<string>();
        /// <summary> フォルダ履歴の保存件数 </summary>
        public int RecentFolderHistoryLimit = 10;
    }
}
