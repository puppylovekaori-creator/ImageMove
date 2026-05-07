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
        /// <summary> 最後に使った確認モード </summary>
        public string GridReviewMode = ReviewModeKind.SingleImage.ToString();
        /// <summary> グリッド目検の最後に使った除外先 </summary>
        public string GridLastExcludeFolderPath;
        /// <summary> サムネイルサイズのプリセット </summary>
        public string GridThumbnailSizePreset = ImageMove.GridThumbnailSizePreset.Medium.ToString();
        /// <summary> サムネイルサイズのカスタム値 </summary>
        public int GridThumbnailCustomSize = 240;
        /// <summary> 1ページ表示件数のプリセット </summary>
        public string GridPageSizePreset = ImageMove.GridPageSizePreset.P1000.ToString();
        /// <summary> 1ページ表示件数のカスタム値 </summary>
        public int GridPageCustomSize = 1000;
        /// <summary> ファイル名表示 </summary>
        public bool GridShowFileName = true;
        /// <summary> 画像ID表示 </summary>
        public bool GridShowImageId = true;
        /// <summary> チェックボックス表示 </summary>
        public bool GridShowCheckBoxes = true;
        /// <summary> 並び順 </summary>
        public string GridSortMode = GridReviewSortMode.ExistingOrder.ToString();
        /// <summary> 並び順方向 </summary>
        public string GridSortDirection = GridReviewSortDirection.Ascending.ToString();
        /// <summary> ファイル名検索条件 </summary>
        public string GridSearchText;
        /// <summary> 拡張子絞り込み </summary>
        public string GridExtensionFilter;
        /// <summary> 状態絞り込み </summary>
        public string GridStatusFilter;
        /// <summary> チェック状態絞り込み </summary>
        public string GridCheckFilter = GridReviewCheckFilter.All.ToString();
        /// <summary> 縦横比絞り込み </summary>
        public string GridAspectRatioFilter = GridReviewAspectRatioFilter.All.ToString();
        /// <summary> 画像サイズ絞り込み </summary>
        public string GridImageSizeFilter = GridReviewImageSizeFilter.All.ToString();
        /// <summary> 最後に開いていたページ番号 </summary>
        public int GridCurrentPage = 1;
        /// <summary> グリッド目検サイドバー幅 </summary>
        public int GridSidebarWidth = 420;
        /// <summary> 履歴表の列幅保存 </summary>
        public string GridHistoryColumnWidths;
    }
}
