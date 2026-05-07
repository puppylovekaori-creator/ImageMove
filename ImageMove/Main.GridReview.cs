using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageMove
{
    public partial class Main
    {
        private TableLayoutPanel reviewModePanel;
        private ComboBox reviewModeComboBox;
        private Label reviewModeNoteLabel;
        private GridReviewWorkspaceControl gridReviewWorkspaceControl;
        private ReviewModeKind activeReviewMode = ReviewModeKind.SingleImage;

        private void InitializeGridReviewModeUi()
        {
            if (reviewModeComboBox != null)
            {
                return;
            }

            SuspendLayout();

            try
            {
                reviewModePanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 3,
                    Height = 52,
                    Padding = new Padding(12, 6, 12, 6)
                };
                reviewModePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
                reviewModePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
                reviewModePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                var modeCaptionLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "確認モード",
                    TextAlign = ContentAlignment.MiddleLeft
                };
                reviewModePanel.Controls.Add(modeCaptionLabel, 0, 0);

                reviewModeComboBox = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                reviewModeComboBox.Items.AddRange(new object[] { "1枚ずつ確認", "グリッド目検" });
                reviewModeComboBox.SelectedIndexChanged += ReviewModeComboBox_SelectedIndexChanged;
                reviewModePanel.Controls.Add(reviewModeComboBox, 1, 0);

                reviewModeNoteLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "グリッド目検は元画像を削除せず、除外フォルダへの移動とアンドゥで運用します。",
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true
                };
                reviewModePanel.Controls.Add(reviewModeNoteLabel, 2, 0);

                gridReviewWorkspaceControl = new GridReviewWorkspaceControl(this)
                {
                    Visible = false
                };

                Controls.Add(gridReviewWorkspaceControl);
                Controls.Add(reviewModePanel);

                mainSplitContainer.SendToBack();
                gridReviewWorkspaceControl.BringToFront();
                reviewModePanel.BringToFront();
                menuStrip1.BringToFront();

                reviewModeComboBox.SelectedIndex = 0;
                ApplyReviewMode(ReviewModeKind.SingleImage);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void ReviewModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (reviewModeComboBox == null)
            {
                return;
            }

            ReviewModeKind nextMode = reviewModeComboBox.SelectedIndex == 1
                ? ReviewModeKind.GridReview
                : ReviewModeKind.SingleImage;
            ApplyReviewMode(nextMode);
            RequestSettingsSave();
        }

        private void ApplyReviewMode(ReviewModeKind nextMode)
        {
            activeReviewMode = nextMode;
            bool useGridReview = activeReviewMode == ReviewModeKind.GridReview;
            mainSplitContainer.Visible = !useGridReview;
            if (gridReviewWorkspaceControl != null)
            {
                gridReviewWorkspaceControl.Visible = useGridReview;
                gridReviewWorkspaceControl.SetWorkspaceVisible(useGridReview);
                if (useGridReview)
                {
                    SyncGridReviewFromCurrentState(false);
                }
            }
        }

        private void SyncGridReviewFromCurrentState(bool forceReset)
        {
            if (gridReviewWorkspaceControl == null || gridReviewWorkspaceControl.IsDisposed)
            {
                return;
            }

            gridReviewWorkspaceControl.SyncFromSnapshot(GetImageBrowserSnapshot(), forceReset);
        }

        private void ApplySavedGridReviewState(SaveSetting setting)
        {
            if (gridReviewWorkspaceControl == null)
            {
                return;
            }

            var state = GridReviewSettingsState.CreateDefault();
            if (setting != null)
            {
                state.ReviewMode = ParseSavedReviewMode(setting.GridReviewMode);
                state.LastExcludeFolderPath = NormalizeDirectoryPath(setting.GridLastExcludeFolderPath);
                state.ThumbnailSizePreset = ParseSavedThumbnailPreset(setting.GridThumbnailSizePreset);
                state.ThumbnailCustomSize = setting.GridThumbnailCustomSize > 0 ? setting.GridThumbnailCustomSize : state.ThumbnailCustomSize;
                state.PageSizePreset = ParseSavedPageSizePreset(setting.GridPageSizePreset);
                state.PageCustomSize = setting.GridPageCustomSize > 0 ? setting.GridPageCustomSize : state.PageCustomSize;
                state.ShowFileName = setting.GridShowFileName;
                state.ShowImageId = setting.GridShowImageId;
                state.ShowCheckBoxes = setting.GridShowCheckBoxes;
                state.SortMode = ParseSavedSortMode(setting.GridSortMode);
                state.SortDirection = ParseSavedSortDirection(setting.GridSortDirection);
                state.SearchText = setting.GridSearchText ?? string.Empty;
                state.ExtensionFilter = setting.GridExtensionFilter ?? string.Empty;
                state.StatusFilter = setting.GridStatusFilter ?? string.Empty;
                state.CheckFilter = ParseSavedCheckFilter(setting.GridCheckFilter);
                state.AspectRatioFilter = ParseSavedAspectFilter(setting.GridAspectRatioFilter);
                state.ImageSizeFilter = ParseSavedImageSizeFilter(setting.GridImageSizeFilter);
                state.CurrentPage = setting.GridCurrentPage > 0 ? setting.GridCurrentPage : 1;
                state.SidebarWidth = setting.GridSidebarWidth > 0 ? setting.GridSidebarWidth : state.SidebarWidth;
                state.HistoryColumnWidths = setting.GridHistoryColumnWidths ?? string.Empty;
            }

            gridReviewWorkspaceControl.ApplySavedState(state);
            if (reviewModeComboBox != null)
            {
                reviewModeComboBox.SelectedIndex = state.ReviewMode == ReviewModeKind.GridReview ? 1 : 0;
            }
            else
            {
                ApplyReviewMode(state.ReviewMode);
            }
        }

        private void PopulateGridReviewSetting(SaveSetting setting)
        {
            if (setting == null || gridReviewWorkspaceControl == null)
            {
                return;
            }

            GridReviewSettingsState state = gridReviewWorkspaceControl.CaptureSettingsState(activeReviewMode);
            setting.GridReviewMode = state.ReviewMode.ToString();
            setting.GridLastExcludeFolderPath = state.LastExcludeFolderPath ?? string.Empty;
            setting.GridThumbnailSizePreset = state.ThumbnailSizePreset.ToString();
            setting.GridThumbnailCustomSize = state.ThumbnailCustomSize;
            setting.GridPageSizePreset = state.PageSizePreset.ToString();
            setting.GridPageCustomSize = state.PageCustomSize;
            setting.GridShowFileName = state.ShowFileName;
            setting.GridShowImageId = state.ShowImageId;
            setting.GridShowCheckBoxes = state.ShowCheckBoxes;
            setting.GridSortMode = state.SortMode.ToString();
            setting.GridSortDirection = state.SortDirection.ToString();
            setting.GridSearchText = state.SearchText ?? string.Empty;
            setting.GridExtensionFilter = state.ExtensionFilter ?? string.Empty;
            setting.GridStatusFilter = state.StatusFilter ?? string.Empty;
            setting.GridCheckFilter = state.CheckFilter.ToString();
            setting.GridAspectRatioFilter = state.AspectRatioFilter.ToString();
            setting.GridImageSizeFilter = state.ImageSizeFilter.ToString();
            setting.GridCurrentPage = state.CurrentPage;
            setting.GridSidebarWidth = state.SidebarWidth;
            setting.GridHistoryColumnWidths = state.HistoryColumnWidths ?? string.Empty;
        }

        private bool ConfirmGridReviewCloseIfNeeded(FormClosingEventArgs e)
        {
            if (e == null || e.CloseReason != CloseReason.UserClosing || gridReviewWorkspaceControl == null || !gridReviewWorkspaceControl.HasBusyWork)
            {
                return true;
            }

            DialogResult result = TopMostMessageBox.Show(
                this,
                "グリッド目検でサムネイル生成または一括処理が進行中です。閉じると処理を停止します。終了しますか。",
                AppDisplayName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                return true;
            }

            e.Cancel = true;
            return false;
        }

        private void DisposeGridReviewWorkspace()
        {
            gridReviewWorkspaceControl?.DisposeWorkspace();
        }

        private void NotifyGridReviewActionCommitted(MoveHistoryAction action)
        {
            if (gridReviewWorkspaceControl == null || action == null)
            {
                return;
            }

            gridReviewWorkspaceControl.HandleActionCommitted(action);
        }

        private void NotifyGridReviewUndoCompleted(MoveHistoryAction action)
        {
            if (gridReviewWorkspaceControl == null || action == null)
            {
                return;
            }

            gridReviewWorkspaceControl.HandleUndoCompleted(action);
        }

        internal BatchMoveExecutionResult MoveImagesFromGrid(
            IReadOnlyList<string> sourcePaths,
            string destinationDirectory,
            string selectionLabel,
            MoveOperationOrigin origin,
            Action<BatchMoveProgressInfo> progressCallback = null)
        {
            return MoveImagesToDirectory(sourcePaths, destinationDirectory, selectionLabel, origin, progressCallback);
        }

        internal BatchMoveExecutionResult RestoreImagesFromGrid(
            IReadOnlyList<GridReviewRestoreRequest> restoreRequests,
            Action<BatchMoveProgressInfo> progressCallback = null)
        {
            var result = new BatchMoveExecutionResult();
            List<GridReviewRestoreRequest> distinctRequests = (restoreRequests ?? Array.Empty<GridReviewRestoreRequest>())
                .Where(request => request != null && !string.IsNullOrWhiteSpace(request.RestoreTargetPath) && !string.IsNullOrWhiteSpace(request.CurrentPath))
                .GroupBy(request => request.StableSourcePath ?? request.RestoreTargetPath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            int totalCount = distinctRequests.Count;
            int processedCount = 0;
            int skippedCount = 0;
            progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, 0, 0, 0, "元フォルダへの戻しを開始しました。", string.Empty));

            var movedItems = new List<MoveHistoryItem>();
            foreach (GridReviewRestoreRequest request in distinctRequests)
            {
                string currentFileName = Path.GetFileName(request.CurrentPath);
                string restoreTargetPath = request.RestoreTargetPath;
                string restoreTargetDirectory = Path.GetDirectoryName(restoreTargetPath) ?? string.Empty;
                string stableSourcePath = string.IsNullOrWhiteSpace(request.StableSourcePath) ? restoreTargetPath : request.StableSourcePath;

                if (!File.Exists(request.CurrentPath))
                {
                    string errorMessage = $"戻し元ファイルが見つからないため戻せません: {currentFileName}";
                    result.SkippedMessages.Add(errorMessage);
                    result.FailureDetails.Add(new FileOperationFailureDetail
                    {
                        SourcePath = request.CurrentPath,
                        DestinationPath = restoreTargetPath,
                        ErrorMessage = errorMessage
                    });
                    processedCount++;
                    skippedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "戻し元ファイルが見つからないためスキップしました。", currentFileName));
                    continue;
                }

                if (File.Exists(restoreTargetPath))
                {
                    string errorMessage = $"元フォルダに同名ファイルがあるため戻せません: {Path.GetFileName(restoreTargetPath)}";
                    result.SkippedMessages.Add(errorMessage);
                    result.FailureDetails.Add(new FileOperationFailureDetail
                    {
                        SourcePath = request.CurrentPath,
                        DestinationPath = restoreTargetPath,
                        ErrorMessage = errorMessage
                    });
                    processedCount++;
                    skippedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "元フォルダに同名ファイルがあるためスキップしました。", currentFileName));
                    continue;
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(restoreTargetDirectory))
                    {
                        Directory.CreateDirectory(restoreTargetDirectory);
                    }

                    File.Move(request.CurrentPath, restoreTargetPath);
                    movedItems.Add(new MoveHistoryItem
                    {
                        StableSourcePath = stableSourcePath,
                        SourcePath = request.CurrentPath,
                        DestinationPath = restoreTargetPath,
                        OriginalIndex = Math.Max(0, request.OriginalIndex)
                    });
                    result.MovedSourcePaths.Add(restoreTargetPath);
                    processedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "元フォルダへ戻しました。", currentFileName));
                }
                catch (Exception ex)
                {
                    string errorMessage = $"元フォルダへの戻しに失敗しました: {currentFileName}";
                    logger.Error($"画像の戻しに失敗しました。 Source={request.CurrentPath} Destination={restoreTargetPath}", ex);
                    result.SkippedMessages.Add(errorMessage);
                    result.FailureDetails.Add(new FileOperationFailureDetail
                    {
                        SourcePath = request.CurrentPath,
                        DestinationPath = restoreTargetPath,
                        ErrorMessage = errorMessage
                    });
                    processedCount++;
                    skippedCount++;
                    progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "元フォルダへの戻しに失敗しました。", currentFileName));
                }
            }

            if (movedItems.Count > 0)
            {
                var historyAction = new MoveHistoryAction(
                    "グリッド目検の戻し",
                    movedItems,
                    MoveOperationKind.RestoreToSource,
                    MoveOperationOrigin.GridReview);
                ApplyQueueChangesForCompletedAction(historyAction, false);
                moveHistoryActions.Push(historyAction);
                result.HistoryAction = historyAction;

                FocusAfterRestoreExecution(historyAction);
                UpdateUndoState();
                RefreshImageBrowserItemsIfOpen();
                RequestSettingsSave();
                NotifyGridReviewActionCommitted(historyAction);
            }
            else
            {
                UpdateNavigationButtons();
            }

            result.MovedCount = movedItems.Count;
            result.ActionLabel = "グリッド目検の戻し";
            progressCallback?.Invoke(new BatchMoveProgressInfo(totalCount, processedCount, movedItems.Count, skippedCount, "元フォルダへの戻しが完了しました。", string.Empty));
            return result;
        }

        private void ApplyQueueChangesForCompletedAction(MoveHistoryAction action, bool undoing)
        {
            if (action == null || action.Items.Count == 0)
            {
                return;
            }

            if (!undoing && action.OperationKind == MoveOperationKind.ExcludeToDestination)
            {
                RemoveImagePathsFromQueue(action.Items.Select(item => item.SourcePath).ToArray());
                return;
            }

            if (!undoing && action.OperationKind == MoveOperationKind.RestoreToSource)
            {
                InsertImagePathsIntoQueue(action.Items, useSourcePath: false);
                return;
            }

            if (undoing && action.OperationKind == MoveOperationKind.ExcludeToDestination)
            {
                InsertImagePathsIntoQueue(action.Items, useSourcePath: true);
                return;
            }

            RemoveImagePathsFromQueue(action.Items.Select(item => item.DestinationPath).ToArray());
        }

        private void InsertImagePathsIntoQueue(IEnumerable<MoveHistoryItem> items, bool useSourcePath)
        {
            List<MoveHistoryItem> orderedItems = (items ?? Array.Empty<MoveHistoryItem>())
                .Where(item => item != null)
                .OrderBy(item => item.OriginalIndex)
                .ToList();
            if (orderedItems.Count == 0)
            {
                return;
            }

            int insertedBeforeCurrent = 0;
            foreach (MoveHistoryItem item in orderedItems)
            {
                string pathToInsert = useSourcePath ? item.SourcePath : item.DestinationPath;
                if (string.IsNullOrWhiteSpace(pathToInsert))
                {
                    continue;
                }

                int insertIndex = Math.Max(0, Math.Min(item.OriginalIndex, imagePaths.Count));
                imagePaths.Insert(insertIndex, pathToInsert);
                if (currentImageIndex >= 0 && insertIndex <= currentImageIndex)
                {
                    insertedBeforeCurrent++;
                }
            }

            if (currentImageIndex >= 0)
            {
                currentImageIndex += insertedBeforeCurrent;
            }

            NormalizeCurrentIndex();
            InvalidateImagePathCache();
        }

        private void FocusAfterRestoreExecution(MoveHistoryAction action)
        {
            if (action == null)
            {
                return;
            }

            string focusPath = action.Items
                .OrderBy(item => item.OriginalIndex)
                .Select(item => item.DestinationPath)
                .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
            if (!string.IsNullOrWhiteSpace(focusPath) && TryGetImagePathIndex(focusPath, out int nextIndex))
            {
                currentImageIndex = nextIndex;
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

        private ReviewModeKind ParseSavedReviewMode(string rawValue)
        {
            return string.Equals(rawValue, ReviewModeKind.GridReview.ToString(), StringComparison.OrdinalIgnoreCase)
                ? ReviewModeKind.GridReview
                : ReviewModeKind.SingleImage;
        }

        private GridThumbnailSizePreset ParseSavedThumbnailPreset(string rawValue)
        {
            return Enum.TryParse(rawValue, true, out GridThumbnailSizePreset preset)
                ? preset
                : GridThumbnailSizePreset.Medium;
        }

        private GridPageSizePreset ParseSavedPageSizePreset(string rawValue)
        {
            return Enum.TryParse(rawValue, true, out GridPageSizePreset preset)
                ? preset
                : GridPageSizePreset.P1000;
        }

        private GridReviewSortMode ParseSavedSortMode(string rawValue)
        {
            return Enum.TryParse(rawValue, true, out GridReviewSortMode sortMode)
                ? sortMode
                : GridReviewSortMode.ExistingOrder;
        }

        private GridReviewSortDirection ParseSavedSortDirection(string rawValue)
        {
            return Enum.TryParse(rawValue, true, out GridReviewSortDirection sortDirection)
                ? sortDirection
                : GridReviewSortDirection.Ascending;
        }

        private GridReviewCheckFilter ParseSavedCheckFilter(string rawValue)
        {
            return Enum.TryParse(rawValue, true, out GridReviewCheckFilter filter)
                ? filter
                : GridReviewCheckFilter.All;
        }

        private GridReviewAspectRatioFilter ParseSavedAspectFilter(string rawValue)
        {
            return Enum.TryParse(rawValue, true, out GridReviewAspectRatioFilter filter)
                ? filter
                : GridReviewAspectRatioFilter.All;
        }

        private GridReviewImageSizeFilter ParseSavedImageSizeFilter(string rawValue)
        {
            return Enum.TryParse(rawValue, true, out GridReviewImageSizeFilter filter)
                ? filter
                : GridReviewImageSizeFilter.All;
        }

        internal void SetReviewModeForTest(int modeValue)
        {
            if (reviewModeComboBox == null)
            {
                return;
            }

            reviewModeComboBox.SelectedIndex = modeValue == 1 ? 1 : 0;
        }

        internal bool IsGridReviewBusyForTest()
        {
            return gridReviewWorkspaceControl != null && gridReviewWorkspaceControl.IsBusyForTest();
        }

        internal int GridReviewVisibleItemCountForTest()
        {
            return gridReviewWorkspaceControl?.VisibleItemCountForTest() ?? 0;
        }

        internal int GridReviewFilteredItemCountForTest()
        {
            return gridReviewWorkspaceControl?.FilteredItemCountForTest() ?? 0;
        }

        internal int GridReviewTotalItemCountForTest()
        {
            return gridReviewWorkspaceControl?.TotalItemCountForTest() ?? 0;
        }

        internal int GridReviewCheckedCountForTest()
        {
            return gridReviewWorkspaceControl?.CheckedCountForTest() ?? 0;
        }

        internal int GridReviewStatusCountForTest(int statusValue)
        {
            return gridReviewWorkspaceControl?.StatusCountForTest((GridReviewItemStatus)statusValue) ?? 0;
        }

        internal void GridReviewSetPageSizeForTest(int pageSize)
        {
            gridReviewWorkspaceControl?.SetPageSizeForTest(pageSize);
        }

        internal void GridReviewGoToPageForTest(int pageNumber)
        {
            gridReviewWorkspaceControl?.GoToPageForTest(pageNumber);
        }

        internal void GridReviewCheckVisiblePageForTest()
        {
            gridReviewWorkspaceControl?.CheckVisiblePageForTest();
        }

        internal void GridReviewSetStatusFilterForTest(int statusValue)
        {
            GridReviewItemStatus? nullableStatus = statusValue < 0
                ? (GridReviewItemStatus?)null
                : (GridReviewItemStatus)statusValue;
            gridReviewWorkspaceControl?.SetStatusFilterForTest(nullableStatus);
        }

        internal void GridReviewExcludeCheckedToFolderForTest(object folderPath)
        {
            string normalizedFolderPath = Convert.ToString(folderPath) ?? string.Empty;
            gridReviewWorkspaceControl?.ExcludeCheckedToFolderForTestAsync(normalizedFolderPath).GetAwaiter().GetResult();
        }

        internal void GridReviewRestoreCheckedForTest()
        {
            gridReviewWorkspaceControl?.RestoreCheckedForTestAsync().GetAwaiter().GetResult();
        }
    }
}
