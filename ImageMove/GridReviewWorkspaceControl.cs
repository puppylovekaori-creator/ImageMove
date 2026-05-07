using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMove
{
    internal sealed class GridReviewWorkspaceControl : UserControl
    {
        private const string StatusFilterAll = "すべて";
        private const string StatusFilterUnprocessed = "未処理";
        private const string StatusFilterExcluded = "除外済み";
        private const string StatusFilterRestored = "戻し済み";
        private const string ExtensionFilterAll = "すべて";
        private const int MinimumThumbnailSize = 96;
        private const int MaximumThumbnailSize = 512;
        private const int MinimumPageSize = 1;
        private const int MaximumPageSize = 1000;
        private const int MinimumSidebarWidth = 420;
        private const int MinimumThumbnailPaneWidth = 240;

        private readonly Main ownerMain;
        private SplitContainer rootSplitContainer;
        private TableLayoutPanel sidebarLayout;
        private Panel settingsScrollPanel;
        private Control settingsLayoutContent;
        private Control primaryActionGroupPanel;
        private DoubleBufferedListView thumbnailListView;
        private ImageList thumbnailImageList;
        private Label emptyStateLabel;
        private Label summaryLabel;
        private Label selectionLabel;
        private Label pageLabel;
        private Label logPathLabel;
        private Label thumbnailStatusLabel;
        private ProgressBar thumbnailProgressBar;
        private Button cancelThumbnailButton;
        private Button clearCacheButton;
        private Button applyFilterButton;
        private Button resetFilterButton;
        private Button checkAllButton;
        private Button checkVisibleButton;
        private Button invertCheckButton;
        private Button clearCheckButton;
        private Button excludeCheckedButton;
        private Button restoreCheckedButton;
        private Button previousPageButton;
        private Button nextPageButton;
        private ComboBox thumbnailSizeComboBox;
        private NumericUpDown thumbnailCustomSizeNumericUpDown;
        private ComboBox pageSizeComboBox;
        private NumericUpDown pageSizeCustomNumericUpDown;
        private CheckBox showFileNameCheckBox;
        private CheckBox showImageIdCheckBox;
        private CheckBox showCheckBoxesCheckBox;
        private ComboBox sortModeComboBox;
        private ComboBox sortDirectionComboBox;
        private TextBox searchTextBox;
        private ComboBox extensionFilterComboBox;
        private ComboBox aspectRatioFilterComboBox;
        private ComboBox imageSizeFilterComboBox;
        private ComboBox statusFilterComboBox;
        private ComboBox checkedFilterComboBox;
        private NumericUpDown pageSelectorNumericUpDown;
        private DataGridView historyGridView;
        private readonly GridReviewHistoryBindingList historyRecords = new GridReviewHistoryBindingList();
        private readonly Dictionary<string, GridReviewItemRecord> itemMap = new Dictionary<string, GridReviewItemRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly List<GridReviewItemRecord> allItems = new List<GridReviewItemRecord>();
        private readonly HashSet<string> checkedSourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly ThumbnailCacheManager thumbnailCache;
        private readonly GridReviewSessionLogger sessionLogger;
        private readonly System.Windows.Forms.Timer thumbnailSingleClickTimer;

        private List<GridReviewItemRecord> filteredItems = new List<GridReviewItemRecord>();
        private List<GridReviewItemRecord> currentPageItems = new List<GridReviewItemRecord>();
        private CancellationTokenSource thumbnailCancellationTokenSource;
        private CancellationTokenSource metadataCancellationTokenSource;
        private ListViewItem pendingThumbnailSingleClickItem;
        private string currentSourceRoot = string.Empty;
        private string lastExcludeFolderPath = string.Empty;
        private int pendingSidebarWidth = 420;
        private bool thumbnailGenerationRunning;
        private bool metadataScanRunning;
        private bool operationRunning;
        private bool workspaceVisible;
        private bool suppressPageSelectorEvent;
        private GridReviewThumbnailProgress thumbnailProgress = new GridReviewThumbnailProgress();

        internal GridReviewWorkspaceControl(Main ownerMain)
        {
            this.ownerMain = ownerMain ?? throw new ArgumentNullException(nameof(ownerMain));
            thumbnailCache = new ThumbnailCacheManager(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageMove", "thumbnail_cache"));
            string assemblyDirectory = Path.GetDirectoryName(typeof(GridReviewWorkspaceControl).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            sessionLogger = new GridReviewSessionLogger(assemblyDirectory);
            thumbnailSingleClickTimer = new System.Windows.Forms.Timer
            {
                Interval = Math.Max(200, SystemInformation.DoubleClickTime + 20)
            };
            thumbnailSingleClickTimer.Tick += ThumbnailSingleClickTimer_Tick;

            Dock = DockStyle.Fill;

            rootSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel2
            };
            rootSplitContainer.Panel1MinSize = MinimumThumbnailPaneWidth;
            rootSplitContainer.SplitterMoved += (_, __) => { };
            rootSplitContainer.SizeChanged += (_, __) => UpdateSidebarConstraints();

            var thumbnailHostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            thumbnailImageList = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(240, 240)
            };
            thumbnailImageList.Images.Add(CreatePlaceholderBitmap(thumbnailImageList.ImageSize));

            thumbnailListView = new DoubleBufferedListView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                FullRowSelect = false,
                HideSelection = false,
                LabelWrap = true,
                MultiSelect = true,
                UseCompatibleStateImageBehavior = false,
                View = View.LargeIcon,
                LargeImageList = thumbnailImageList
            };
            thumbnailListView.ItemChecked += ThumbnailListView_ItemChecked;
            thumbnailListView.ItemSelectionChanged += (_, __) => UpdateSelectionSummary();
            thumbnailListView.MouseUp += ThumbnailListView_MouseUp;
            thumbnailListView.DoubleClick += ThumbnailListView_DoubleClick;

            emptyStateLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "画像を読み込むと、ここにグリッド目検のサムネイルが表示されます。",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("メイリオ", 11F),
                BackColor = Color.Transparent
            };

            thumbnailHostPanel.Controls.Add(thumbnailListView);
            thumbnailHostPanel.Controls.Add(emptyStateLabel);

            rootSplitContainer.Panel1.Controls.Add(thumbnailHostPanel);

            sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 10,
                Padding = new Padding(12),
                AutoScroll = false
            };
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            summaryLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                Font = new Font("メイリオ", 10F, FontStyle.Bold),
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "表示待機中"
            };
            sidebarLayout.Controls.Add(summaryLabel, 0, 0);

            selectionLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "選択中 0枚 / 表示中 0枚 / 全体 0枚"
            };
            sidebarLayout.Controls.Add(selectionLabel, 0, 1);

            pageLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "ページ 0 / 0"
            };
            sidebarLayout.Controls.Add(pageLabel, 0, 2);

            logPathLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "ログ: " + sessionLogger.LogPath
            };
            sidebarLayout.Controls.Add(logPathLabel, 0, 3);

            thumbnailStatusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "サムネイル待機中"
            };
            sidebarLayout.Controls.Add(thumbnailStatusLabel, 0, 4);

            thumbnailProgressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Minimum = 0,
                Maximum = 1,
                Height = 20
            };
            sidebarLayout.Controls.Add(thumbnailProgressBar, 0, 5);

            var thumbnailActionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true
            };
            cancelThumbnailButton = new Button
            {
                Text = "生成停止",
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
            cancelThumbnailButton.Click += (_, __) => CancelThumbnailWork();
            clearCacheButton = new Button
            {
                Text = "キャッシュをクリア",
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
            clearCacheButton.Click += ClearCacheButton_Click;
            thumbnailActionPanel.Controls.Add(cancelThumbnailButton);
            thumbnailActionPanel.Controls.Add(clearCacheButton);
            sidebarLayout.Controls.Add(thumbnailActionPanel, 0, 6);

            primaryActionGroupPanel = CreateGroupPanel("実行", CreatePrimaryActionLayout());
            sidebarLayout.Controls.Add(primaryActionGroupPanel, 0, 7);

            settingsScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            settingsLayoutContent = CreateSettingsLayout();
            settingsScrollPanel.Controls.Add(settingsLayoutContent);
            sidebarLayout.Controls.Add(settingsScrollPanel, 0, 8);

            historyGridView = CreateHistoryGrid();
            sidebarLayout.Controls.Add(historyGridView, 0, 9);

            rootSplitContainer.Panel2.Controls.Add(sidebarLayout);
            Controls.Add(rootSplitContainer);

            ApplyDefaultControlValues();
            UpdateSidebarConstraints();
            UpdateSelectionSummary();
            UpdateButtonsEnabled();
        }

        internal bool HasBusyWork => thumbnailGenerationRunning || metadataScanRunning || operationRunning;

        internal BindingList<GridReviewOperationRecord> HistoryRecords => historyRecords;

        internal void SetWorkspaceVisible(bool isVisible)
        {
            workspaceVisible = isVisible;
            if (!workspaceVisible)
            {
                CancelPendingThumbnailSingleClick();
                CancelThumbnailWork();
                return;
            }

            UpdateSidebarConstraints();
            ResetSidebarScrollPositions();
            if (currentPageItems.Count > 0)
            {
                BeginPageThumbnailGeneration();
            }
        }

        internal void SyncFromSnapshot(ImageBrowserSnapshot snapshot, bool forceReset)
        {
            ImageBrowserSnapshot safeSnapshot = snapshot ?? ImageBrowserSnapshot.Empty;
            string newSourceRoot = safeSnapshot.SourceRoot ?? string.Empty;

            if (forceReset || !string.Equals(currentSourceRoot, newSourceRoot, StringComparison.OrdinalIgnoreCase))
            {
                currentSourceRoot = newSourceRoot;
                ResetSessionCatalog();
            }

            sessionLogger.Info(
                "画像一覧をグリッド目検へ同期します。",
                GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                GridReviewSessionLogger.CreateDetails("SOURCE", currentSourceRoot),
                GridReviewSessionLogger.CreateDetails("COUNT", safeSnapshot.FullPaths.Length));

            MergeSnapshot(safeSnapshot);
            RefreshExtensionFilterItems();
            ApplyFiltersAndRefreshPage(resetPage: false);
            sessionLogger.Info(
                "画像一覧のグリッド同期が完了しました。",
                GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                GridReviewSessionLogger.CreateDetails("SOURCE", currentSourceRoot),
                GridReviewSessionLogger.CreateDetails("TOTAL", allItems.Count),
                GridReviewSessionLogger.CreateDetails("FILTERED", filteredItems.Count),
                GridReviewSessionLogger.CreateDetails("VISIBLE", currentPageItems.Count));
        }

        internal void HandleActionCommitted(MoveHistoryAction action)
        {
            if (action == null)
            {
                return;
            }

            if (action.OperationKind == MoveOperationKind.ExcludeToDestination)
            {
                foreach (MoveHistoryItem item in action.Items)
                {
                    GridReviewItemRecord record = GetOrCreateItem(item.StableSourcePath ?? item.SourcePath, item.OriginalIndex);
                    record.CurrentPath = item.DestinationPath;
                    record.LastDestinationPath = item.DestinationPath;
                    record.Status = GridReviewItemStatus.Excluded;
                    record.StatusChangedAtUtc = action.ExecutedAtUtc;
                    record.WasExcludedOnce = true;
                    RefreshBasicMetadata(record);
                    checkedSourcePaths.Remove(record.OriginalSourcePath);
                }

                AddHistoryRecord(action, action.Items.Count, 0, action.Items.FirstOrDefault()?.DestinationPath ?? string.Empty, "除外へ移動");
                sessionLogger.Info(
                    "一括除外を記録しました。",
                    GridReviewSessionLogger.CreateDetails("WHO", action.Who),
                    GridReviewSessionLogger.CreateDetails("OP", action.OperationId),
                    GridReviewSessionLogger.CreateDetails("COUNT", action.Items.Count),
                    GridReviewSessionLogger.CreateDetails("KIND", action.OperationKind));
            }
            else
            {
                foreach (MoveHistoryItem item in action.Items)
                {
                    GridReviewItemRecord record = GetOrCreateItem(item.StableSourcePath ?? item.DestinationPath, item.OriginalIndex);
                    record.CurrentPath = item.DestinationPath;
                    record.Status = GridReviewItemStatus.Restored;
                    record.StatusChangedAtUtc = action.ExecutedAtUtc;
                    record.WasExcludedOnce = true;
                    RefreshBasicMetadata(record);
                    checkedSourcePaths.Remove(record.OriginalSourcePath);
                }

                AddHistoryRecord(action, action.Items.Count, 0, string.Empty, "元フォルダへ戻し");
                sessionLogger.Info(
                    "一括戻しを記録しました。",
                    GridReviewSessionLogger.CreateDetails("WHO", action.Who),
                    GridReviewSessionLogger.CreateDetails("OP", action.OperationId),
                    GridReviewSessionLogger.CreateDetails("COUNT", action.Items.Count),
                    GridReviewSessionLogger.CreateDetails("KIND", action.OperationKind));
            }
        }

        internal void HandleUndoCompleted(MoveHistoryAction action)
        {
            if (action == null)
            {
                return;
            }

            if (action.OperationKind == MoveOperationKind.ExcludeToDestination)
            {
                foreach (MoveHistoryItem item in action.Items)
                {
                    if (!itemMap.TryGetValue(item.StableSourcePath ?? item.SourcePath, out GridReviewItemRecord record))
                    {
                        continue;
                    }

                    record.CurrentPath = item.SourcePath;
                    record.Status = GridReviewItemStatus.Restored;
                    record.StatusChangedAtUtc = DateTime.UtcNow;
                    record.WasExcludedOnce = true;
                    RefreshBasicMetadata(record);
                }
            }
            else
            {
                foreach (MoveHistoryItem item in action.Items)
                {
                    if (!itemMap.TryGetValue(item.StableSourcePath ?? item.DestinationPath, out GridReviewItemRecord record))
                    {
                        continue;
                    }

                    record.CurrentPath = item.SourcePath;
                    record.LastDestinationPath = item.SourcePath;
                    record.Status = GridReviewItemStatus.Excluded;
                    record.StatusChangedAtUtc = DateTime.UtcNow;
                    record.WasExcludedOnce = true;
                    RefreshBasicMetadata(record);
                }
            }

            AddHistoryRecord(action, action.Items.Count, 0, string.Empty, "アンドゥ");
            sessionLogger.Info(
                "アンドゥを記録しました。",
                GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                GridReviewSessionLogger.CreateDetails("OP", action.OperationId),
                GridReviewSessionLogger.CreateDetails("COUNT", action.Items.Count),
                GridReviewSessionLogger.CreateDetails("KIND", action.OperationKind));
        }

        internal GridReviewSettingsState CaptureSettingsState(ReviewModeKind reviewMode)
        {
            return new GridReviewSettingsState
            {
                ReviewMode = reviewMode,
                LastExcludeFolderPath = lastExcludeFolderPath ?? string.Empty,
                ThumbnailSizePreset = ParseThumbnailSizePreset(thumbnailSizeComboBox.SelectedItem as string),
                ThumbnailCustomSize = (int)thumbnailCustomSizeNumericUpDown.Value,
                PageSizePreset = ParsePageSizePreset(pageSizeComboBox.SelectedItem as string),
                PageCustomSize = (int)pageSizeCustomNumericUpDown.Value,
                ShowFileName = showFileNameCheckBox.Checked,
                ShowImageId = showImageIdCheckBox.Checked,
                ShowCheckBoxes = showCheckBoxesCheckBox.Checked,
                SortMode = ParseSortMode(sortModeComboBox.SelectedItem as string),
                SortDirection = ParseSortDirection(sortDirectionComboBox.SelectedItem as string),
                SearchText = (searchTextBox.Text ?? string.Empty).Trim(),
                ExtensionFilter = extensionFilterComboBox.SelectedItem as string ?? ExtensionFilterAll,
                StatusFilter = statusFilterComboBox.SelectedItem as string ?? StatusFilterAll,
                CheckFilter = ParseCheckFilter(checkedFilterComboBox.SelectedItem as string),
                AspectRatioFilter = ParseAspectRatioFilter(aspectRatioFilterComboBox.SelectedItem as string),
                ImageSizeFilter = ParseImageSizeFilter(imageSizeFilterComboBox.SelectedItem as string),
                CurrentPage = Math.Max(1, (int)pageSelectorNumericUpDown.Value),
                SidebarWidth = Math.Max(ResolveSidebarMinimumWidth(), rootSplitContainer.Width - rootSplitContainer.SplitterDistance),
                HistoryColumnWidths = CaptureHistoryColumnWidths()
            };
        }

        internal void ApplySavedState(GridReviewSettingsState state)
        {
            GridReviewSettingsState safeState = state ?? GridReviewSettingsState.CreateDefault();
            lastExcludeFolderPath = safeState.LastExcludeFolderPath ?? string.Empty;

            thumbnailSizeComboBox.SelectedItem = FormatThumbnailPreset(safeState.ThumbnailSizePreset);
            thumbnailCustomSizeNumericUpDown.Value = ClampNumericValue(thumbnailCustomSizeNumericUpDown, safeState.ThumbnailCustomSize);
            pageSizeComboBox.SelectedItem = FormatPageSizePreset(safeState.PageSizePreset);
            pageSizeCustomNumericUpDown.Value = ClampNumericValue(pageSizeCustomNumericUpDown, safeState.PageCustomSize);
            showFileNameCheckBox.Checked = safeState.ShowFileName;
            showImageIdCheckBox.Checked = safeState.ShowImageId;
            showCheckBoxesCheckBox.Checked = safeState.ShowCheckBoxes;
            sortModeComboBox.SelectedItem = FormatSortMode(safeState.SortMode);
            sortDirectionComboBox.SelectedItem = FormatSortDirection(safeState.SortDirection);
            searchTextBox.Text = safeState.SearchText ?? string.Empty;
            aspectRatioFilterComboBox.SelectedItem = FormatAspectRatioFilter(safeState.AspectRatioFilter);
            imageSizeFilterComboBox.SelectedItem = FormatImageSizeFilter(safeState.ImageSizeFilter);
            checkedFilterComboBox.SelectedItem = FormatCheckFilter(safeState.CheckFilter);
            statusFilterComboBox.SelectedItem = string.IsNullOrWhiteSpace(safeState.StatusFilter) ? StatusFilterAll : safeState.StatusFilter;
            ApplyExtensionSelection(safeState.ExtensionFilter);
            UpdateSidebarConstraints();
            ApplySidebarWidth(safeState.SidebarWidth);
            ResetSidebarScrollPositions();
            ApplyHistoryColumnWidths(safeState.HistoryColumnWidths);
            UpdateDisplayOptionsFromControls();
            ApplyFiltersAndRefreshPage(resetPage: false);
        }

        internal void DisposeWorkspace()
        {
            CancelPendingThumbnailSingleClick();
            thumbnailSingleClickTimer.Dispose();
            CancelThumbnailWork();
            CancelMetadataWork();
            thumbnailCache.Dispose();
            sessionLogger.Dispose();
        }

        internal int VisibleItemCountForTest()
        {
            return currentPageItems.Count;
        }

        internal int FilteredItemCountForTest()
        {
            return filteredItems.Count;
        }

        internal int TotalItemCountForTest()
        {
            return allItems.Count;
        }

        internal int CheckedCountForTest()
        {
            return checkedSourcePaths.Count;
        }

        internal int StatusCountForTest(GridReviewItemStatus status)
        {
            return allItems.Count(item => item.Status == status);
        }

        internal bool HasSidebarHorizontalOverflowForTest()
        {
            return settingsScrollPanel != null
                && !settingsScrollPanel.IsDisposed
                && settingsScrollPanel.HorizontalScroll.Visible;
        }

        internal int SidebarMinimumWidthForTest()
        {
            return ResolveSidebarMinimumWidth();
        }

        internal int SidebarCurrentWidthForTest()
        {
            return rootSplitContainer?.Panel2?.Width ?? 0;
        }

        internal bool HasPinnedActionButtonsForTest()
        {
            if (settingsScrollPanel == null || primaryActionGroupPanel == null || excludeCheckedButton == null || restoreCheckedButton == null || clearCheckButton == null)
            {
                return false;
            }

            return ReferenceEquals(primaryActionGroupPanel.Parent, sidebarLayout)
                && primaryActionGroupPanel.Bottom <= settingsScrollPanel.Top
                && !IsDescendantOf(settingsScrollPanel, excludeCheckedButton)
                && !IsDescendantOf(settingsScrollPanel, restoreCheckedButton)
                && !IsDescendantOf(settingsScrollPanel, clearCheckButton);
        }

        internal void ToggleVisibleItemByThumbnailClickForTest(int visibleIndex)
        {
            if (visibleIndex < 0 || visibleIndex >= thumbnailListView.Items.Count)
            {
                return;
            }

            QueueThumbnailSingleClickToggle(thumbnailListView.Items[visibleIndex]);
            ApplyPendingThumbnailSingleClickToggle();
        }

        internal void SetPageSizeForTest(int pageSize)
        {
            pageSizeComboBox.SelectedItem = "カスタム";
            pageSizeCustomNumericUpDown.Value = ClampNumericValue(pageSizeCustomNumericUpDown, pageSize);
            ApplyFiltersAndRefreshPage(resetPage: true);
        }

        internal void GoToPageForTest(int pageNumber)
        {
            int safePage = Math.Max(1, Math.Min(pageNumber, GetTotalPages()));
            pageSelectorNumericUpDown.Value = safePage;
        }

        internal void CheckVisiblePageForTest()
        {
            SetCheckedStateForItems(currentPageItems, true);
        }

        internal void SetStatusFilterForTest(GridReviewItemStatus? status)
        {
            statusFilterComboBox.SelectedItem = status.HasValue ? GetStatusLabel(status.Value) : StatusFilterAll;
            ApplyFiltersAndRefreshPage(resetPage: true);
        }

        internal bool IsBusyForTest()
        {
            return HasBusyWork;
        }

        internal async Task ExcludeCheckedToFolderForTestAsync(string folderPath)
        {
            await ExcludeCheckedAsync(folderPath, "テスト指定フォルダ");
        }

        internal async Task RestoreCheckedForTestAsync()
        {
            await RestoreCheckedAsync();
        }

        private Control CreateSettingsLayout()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Name = "gridSettingsLayout"
            };

            panel.Controls.Add(CreateGroupPanel("表示設定", CreateDisplaySettingsLayout()));
            panel.Controls.Add(CreateGroupPanel("並び替えと絞り込み", CreateFilterLayout()));
            panel.Controls.Add(CreateGroupPanel("ページ移動", CreatePagingLayout()));
            panel.Controls.Add(CreateGroupPanel("選択補助", CreateSelectionActionLayout()));
            panel.Controls.Add(CreateGroupPanel("履歴", CreateHistoryGuideLayout()));
            return panel;
        }

        private Control CreatePrimaryActionLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1
            };

            var hintLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                AutoEllipsis = true,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "画像をクリックするとチェックが切り替わります。ダブルクリックで単票表示します。"
            };
            layout.Controls.Add(hintLabel, 0, 0);

            excludeCheckedButton = new Button
            {
                Name = "excludeCheckedButton",
                Text = "チェック画像を除外へ送る",
                Dock = DockStyle.Top,
                UseVisualStyleBackColor = true
            };
            excludeCheckedButton.Click += async (_, __) => await ExcludeCheckedAsync();
            layout.Controls.Add(excludeCheckedButton, 0, 1);

            restoreCheckedButton = new Button
            {
                Name = "restoreCheckedButton",
                Text = "チェック画像を戻す",
                Dock = DockStyle.Top,
                UseVisualStyleBackColor = true
            };
            restoreCheckedButton.Click += async (_, __) => await RestoreCheckedAsync();
            layout.Controls.Add(restoreCheckedButton, 0, 2);

            clearCheckButton = new Button
            {
                Name = "clearCheckButton",
                Text = "選択を解除",
                Dock = DockStyle.Top,
                UseVisualStyleBackColor = true
            };
            clearCheckButton.Click += (_, __) => ClearAllCheckedState();
            layout.Controls.Add(clearCheckButton, 0, 3);

            return layout;
        }

        private Control CreateDisplaySettingsLayout()
        {
            var layout = CreateTwoColumnLayout();

            layout.Controls.Add(CreateCaptionLabel("サムネイルサイズ"), 0, 0);
            thumbnailSizeComboBox = new ComboBox
            {
                Name = "thumbnailSizeComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            thumbnailSizeComboBox.Items.AddRange(new object[] { "小", "中", "大", "カスタム" });
            thumbnailSizeComboBox.SelectedIndexChanged += (_, __) => UpdateDisplayOptionControlState();
            layout.Controls.Add(thumbnailSizeComboBox, 1, 0);

            layout.Controls.Add(CreateCaptionLabel("カスタムサイズ"), 0, 1);
            thumbnailCustomSizeNumericUpDown = new NumericUpDown
            {
                Name = "thumbnailCustomSizeNumericUpDown",
                Dock = DockStyle.Left,
                Minimum = MinimumThumbnailSize,
                Maximum = MaximumThumbnailSize,
                Increment = 8,
                Width = 120
            };
            layout.Controls.Add(thumbnailCustomSizeNumericUpDown, 1, 1);

            layout.Controls.Add(CreateCaptionLabel("1ページ表示件数"), 0, 2);
            pageSizeComboBox = new ComboBox
            {
                Name = "pageSizeComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            pageSizeComboBox.Items.AddRange(new object[] { "100", "300", "500", "1000", "カスタム" });
            pageSizeComboBox.SelectedIndexChanged += (_, __) => UpdateDisplayOptionControlState();
            layout.Controls.Add(pageSizeComboBox, 1, 2);

            layout.Controls.Add(CreateCaptionLabel("カスタム件数"), 0, 3);
            pageSizeCustomNumericUpDown = new NumericUpDown
            {
                Name = "pageSizeCustomNumericUpDown",
                Dock = DockStyle.Left,
                Minimum = MinimumPageSize,
                Maximum = MaximumPageSize,
                Width = 120
            };
            layout.Controls.Add(pageSizeCustomNumericUpDown, 1, 3);

            showFileNameCheckBox = new CheckBox
            {
                Name = "showFileNameCheckBox",
                Dock = DockStyle.Fill,
                Text = "ファイル名を表示",
                AutoSize = true
            };
            showImageIdCheckBox = new CheckBox
            {
                Name = "showImageIdCheckBox",
                Dock = DockStyle.Fill,
                Text = "画像IDを表示",
                AutoSize = true
            };
            showCheckBoxesCheckBox = new CheckBox
            {
                Name = "showCheckBoxesCheckBox",
                Dock = DockStyle.Fill,
                Text = "チェックボックスを表示",
                AutoSize = true
            };
            layout.Controls.Add(showFileNameCheckBox, 0, 4);
            layout.SetColumnSpan(showFileNameCheckBox, 2);
            layout.Controls.Add(showImageIdCheckBox, 0, 5);
            layout.SetColumnSpan(showImageIdCheckBox, 2);
            layout.Controls.Add(showCheckBoxesCheckBox, 0, 6);
            layout.SetColumnSpan(showCheckBoxesCheckBox, 2);

            var applyDisplayButton = new Button
            {
                Text = "表示設定を反映",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            applyDisplayButton.Click += (_, __) =>
            {
                UpdateDisplayOptionsFromControls();
                ApplyFiltersAndRefreshPage(resetPage: false);
            };
            layout.Controls.Add(applyDisplayButton, 0, 7);
            layout.SetColumnSpan(applyDisplayButton, 2);

            return layout;
        }

        private Control CreateFilterLayout()
        {
            var layout = CreateTwoColumnLayout();

            layout.Controls.Add(CreateCaptionLabel("並び順"), 0, 0);
            sortModeComboBox = new ComboBox
            {
                Name = "sortModeComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            sortModeComboBox.Items.AddRange(new object[] { "既存順", "ファイル名順", "更新日時順", "作成日時順", "画像サイズ順", "ファイルサイズ順" });
            layout.Controls.Add(sortModeComboBox, 1, 0);

            layout.Controls.Add(CreateCaptionLabel("昇順 / 降順"), 0, 1);
            sortDirectionComboBox = new ComboBox
            {
                Name = "sortDirectionComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            sortDirectionComboBox.Items.AddRange(new object[] { "昇順", "降順" });
            layout.Controls.Add(sortDirectionComboBox, 1, 1);

            layout.Controls.Add(CreateCaptionLabel("ファイル名検索"), 0, 2);
            searchTextBox = new TextBox
            {
                Name = "searchTextBox",
                Dock = DockStyle.Fill
            };
            searchTextBox.KeyDown += SearchTextBox_KeyDown;
            layout.Controls.Add(searchTextBox, 1, 2);

            layout.Controls.Add(CreateCaptionLabel("拡張子"), 0, 3);
            extensionFilterComboBox = new ComboBox
            {
                Name = "extensionFilterComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            extensionFilterComboBox.Items.Add(ExtensionFilterAll);
            layout.Controls.Add(extensionFilterComboBox, 1, 3);

            layout.Controls.Add(CreateCaptionLabel("縦横比"), 0, 4);
            aspectRatioFilterComboBox = new ComboBox
            {
                Name = "aspectRatioFilterComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            aspectRatioFilterComboBox.Items.AddRange(new object[] { "すべて", "横長", "縦長", "正方形付近" });
            layout.Controls.Add(aspectRatioFilterComboBox, 1, 4);

            layout.Controls.Add(CreateCaptionLabel("画像サイズ"), 0, 5);
            imageSizeFilterComboBox = new ComboBox
            {
                Name = "imageSizeFilterComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            imageSizeFilterComboBox.Items.AddRange(new object[] { "すべて", "小さめ", "中くらい", "大きめ" });
            layout.Controls.Add(imageSizeFilterComboBox, 1, 5);

            layout.Controls.Add(CreateCaptionLabel("チェック状態"), 0, 6);
            checkedFilterComboBox = new ComboBox
            {
                Name = "checkedFilterComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            checkedFilterComboBox.Items.AddRange(new object[] { "すべて", "チェック済みのみ", "未チェックのみ" });
            layout.Controls.Add(checkedFilterComboBox, 1, 6);

            layout.Controls.Add(CreateCaptionLabel("状態"), 0, 7);
            statusFilterComboBox = new ComboBox
            {
                Name = "statusFilterComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            statusFilterComboBox.Items.AddRange(new object[] { StatusFilterAll, StatusFilterUnprocessed, StatusFilterExcluded, StatusFilterRestored });
            layout.Controls.Add(statusFilterComboBox, 1, 7);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true
            };
            applyFilterButton = new Button
            {
                Name = "applyFilterButton",
                Text = "条件を反映",
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
            applyFilterButton.Click += async (_, __) => await ApplyFiltersAndRefreshPageAsync(resetPage: true);
            resetFilterButton = new Button
            {
                Name = "resetFilterButton",
                Text = "条件を初期化",
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
            resetFilterButton.Click += (_, __) => ResetFilterControls();
            buttonPanel.Controls.Add(applyFilterButton);
            buttonPanel.Controls.Add(resetFilterButton);
            layout.Controls.Add(buttonPanel, 0, 8);
            layout.SetColumnSpan(buttonPanel, 2);

            return layout;
        }

        private Control CreatePagingLayout()
        {
            var layout = CreateTwoColumnLayout();

            layout.Controls.Add(CreateCaptionLabel("現在ページ"), 0, 0);
            pageSelectorNumericUpDown = new NumericUpDown
            {
                Name = "pageSelectorNumericUpDown",
                Dock = DockStyle.Left,
                Minimum = 1,
                Maximum = 1,
                Width = 120
            };
            pageSelectorNumericUpDown.ValueChanged += PageSelectorNumericUpDown_ValueChanged;
            layout.Controls.Add(pageSelectorNumericUpDown, 1, 0);

            previousPageButton = new Button
            {
                Name = "previousPageButton",
                Text = "前へ",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            previousPageButton.Click += (_, __) => MovePage(-1);
            nextPageButton = new Button
            {
                Name = "nextPageButton",
                Text = "次へ",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            nextPageButton.Click += (_, __) => MovePage(1);
            layout.Controls.Add(previousPageButton, 0, 1);
            layout.Controls.Add(nextPageButton, 1, 1);

            return layout;
        }

        private Control CreateSelectionActionLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1
            };

            checkAllButton = new Button
            {
                Name = "checkAllButton",
                Text = "全選択",
                Dock = DockStyle.Top,
                UseVisualStyleBackColor = true
            };
            checkAllButton.Click += (_, __) => SetCheckedStateForItems(filteredItems, true);
            layout.Controls.Add(checkAllButton, 0, 0);

            checkVisibleButton = new Button
            {
                Name = "checkVisibleButton",
                Text = "表示中のみ全選択",
                Dock = DockStyle.Top,
                UseVisualStyleBackColor = true
            };
            checkVisibleButton.Click += (_, __) => SetCheckedStateForItems(currentPageItems, true);
            layout.Controls.Add(checkVisibleButton, 0, 1);

            invertCheckButton = new Button
            {
                Name = "invertCheckButton",
                Text = "選択反転",
                Dock = DockStyle.Top,
                UseVisualStyleBackColor = true
            };
            invertCheckButton.Click += (_, __) => InvertCheckedStateForFilteredItems();
            layout.Controls.Add(invertCheckButton, 0, 2);

            return layout;
        }

        private Control CreateHistoryGuideLayout()
        {
            return new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                AutoEllipsis = true,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "下の履歴は直近の除外・戻し・アンドゥを表示します。WHO 列は操作主体の記録用です。"
            };
        }

        private DataGridView CreateHistoryGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 220,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                DataSource = historyRecords
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ExecutedAt",
                DataPropertyName = nameof(GridReviewOperationRecord.ExecutedAt),
                HeaderText = "日時",
                Width = 120
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Who",
                DataPropertyName = nameof(GridReviewOperationRecord.Who),
                HeaderText = "WHO",
                Width = 80
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "OperationType",
                DataPropertyName = nameof(GridReviewOperationRecord.OperationType),
                HeaderText = "種別",
                Width = 96
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TargetCount",
                DataPropertyName = nameof(GridReviewOperationRecord.TargetCount),
                HeaderText = "対象",
                Width = 60
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SuccessCount",
                DataPropertyName = nameof(GridReviewOperationRecord.SuccessCount),
                HeaderText = "成功",
                Width = 60
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FailureCount",
                DataPropertyName = nameof(GridReviewOperationRecord.FailureCount),
                HeaderText = "失敗",
                Width = 60
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TargetFolder",
                DataPropertyName = nameof(GridReviewOperationRecord.TargetFolder),
                HeaderText = "移動先 / 戻し先",
                Width = 180
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Note",
                DataPropertyName = nameof(GridReviewOperationRecord.Note),
                HeaderText = "詳細",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            return grid;
        }

        private void ApplyDefaultControlValues()
        {
            thumbnailSizeComboBox.SelectedIndex = 1;
            pageSizeComboBox.SelectedIndex = 3;
            sortModeComboBox.SelectedIndex = 0;
            sortDirectionComboBox.SelectedIndex = 0;
            extensionFilterComboBox.SelectedIndex = 0;
            aspectRatioFilterComboBox.SelectedIndex = 0;
            imageSizeFilterComboBox.SelectedIndex = 0;
            checkedFilterComboBox.SelectedIndex = 0;
            statusFilterComboBox.SelectedIndex = 0;
            showFileNameCheckBox.Checked = true;
            showImageIdCheckBox.Checked = true;
            showCheckBoxesCheckBox.Checked = true;
            UpdateDisplayOptionControlState();
            UpdateDisplayOptionsFromControls();
        }

        private void UpdateDisplayOptionControlState()
        {
            thumbnailCustomSizeNumericUpDown.Enabled = string.Equals(thumbnailSizeComboBox.SelectedItem as string, "カスタム", StringComparison.Ordinal);
            pageSizeCustomNumericUpDown.Enabled = string.Equals(pageSizeComboBox.SelectedItem as string, "カスタム", StringComparison.Ordinal);
        }

        private void UpdateDisplayOptionsFromControls()
        {
            thumbnailListView.CheckBoxes = showCheckBoxesCheckBox.Checked;
            int thumbnailSize = ResolveThumbnailBoxSize();
            Size imageSize = new Size(thumbnailSize, thumbnailSize);
            if (thumbnailImageList.ImageSize != imageSize)
            {
                thumbnailImageList.ImageSize = imageSize;
                thumbnailImageList.Images.Clear();
                thumbnailImageList.Images.Add(CreatePlaceholderBitmap(imageSize));
            }
        }

        private void ApplyExtensionSelection(string extensionFilter)
        {
            string candidate = string.IsNullOrWhiteSpace(extensionFilter) ? ExtensionFilterAll : extensionFilter;
            if (!extensionFilterComboBox.Items.Contains(candidate))
            {
                extensionFilterComboBox.SelectedIndex = 0;
                return;
            }

            extensionFilterComboBox.SelectedItem = candidate;
        }

        private void ApplySidebarWidth(int sidebarWidth)
        {
            pendingSidebarWidth = sidebarWidth;
            int minimumThumbnailPaneWidth = Math.Max(MinimumThumbnailPaneWidth, rootSplitContainer.Panel1MinSize);
            if (sidebarWidth <= 0 || rootSplitContainer.Width <= rootSplitContainer.Panel2MinSize + minimumThumbnailPaneWidth)
            {
                return;
            }

            int safeSidebarWidth = Math.Max(rootSplitContainer.Panel2MinSize, Math.Min(sidebarWidth, rootSplitContainer.Width - minimumThumbnailPaneWidth));
            rootSplitContainer.SplitterDistance = Math.Max(minimumThumbnailPaneWidth, rootSplitContainer.Width - safeSidebarWidth);
        }

        private void UpdateSidebarConstraints()
        {
            if (rootSplitContainer == null || rootSplitContainer.IsDisposed)
            {
                return;
            }

            int desiredSidebarMinimumWidth = ResolveSidebarMinimumWidth();
            int availableSidebarMinimumWidth = Math.Max(120, rootSplitContainer.Width - Math.Max(MinimumThumbnailPaneWidth, rootSplitContainer.Panel1MinSize) - rootSplitContainer.SplitterWidth);
            rootSplitContainer.Panel2MinSize = Math.Max(120, Math.Min(desiredSidebarMinimumWidth, availableSidebarMinimumWidth));
            ApplySidebarWidth(Math.Max(pendingSidebarWidth, rootSplitContainer.Panel2MinSize));
            ResetSidebarScrollPositions();
        }

        private int ResolveSidebarMinimumWidth()
        {
            int preferredSettingsWidth = settingsLayoutContent?.GetPreferredSize(Size.Empty).Width ?? 0;
            int scrollbarAllowance = SystemInformation.VerticalScrollBarWidth + 28;
            return Math.Max(MinimumSidebarWidth, preferredSettingsWidth + scrollbarAllowance);
        }

        private void ResetSidebarScrollPositions()
        {
            if (settingsScrollPanel != null && !settingsScrollPanel.IsDisposed)
            {
                settingsScrollPanel.AutoScrollPosition = Point.Empty;
            }
        }

        private void ApplyHistoryColumnWidths(string historyColumnWidths)
        {
            if (string.IsNullOrWhiteSpace(historyColumnWidths))
            {
                return;
            }

            string[] parts = historyColumnWidths.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < Math.Min(parts.Length, historyGridView.Columns.Count); index++)
            {
                if (!int.TryParse(parts[index], out int width) || width <= 30)
                {
                    continue;
                }

                historyGridView.Columns[index].Width = width;
            }
        }

        private string CaptureHistoryColumnWidths()
        {
            return string.Join(",", historyGridView.Columns.Cast<DataGridViewColumn>().Select(column => column.Width.ToString()));
        }

        private async Task ApplyFiltersAndRefreshPageAsync(bool resetPage)
        {
            if (!await EnsureMetadataReadyForCurrentFiltersAsync())
            {
                return;
            }

            if (IsDisposed)
            {
                return;
            }

            BeginInvoke((Action)(() => ApplyFiltersAndRefreshPage(resetPage)));
        }

        private void ApplyFiltersAndRefreshPage(bool resetPage)
        {
            UpdateDisplayOptionControlState();
            UpdateDisplayOptionsFromControls();

            IEnumerable<GridReviewItemRecord> candidates = allItems;
            GridReviewFilterState filter = ReadFilterStateFromControls();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string searchText = filter.SearchText;
                candidates = candidates.Where(item =>
                    item.DisplayFileName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.CurrentPath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.Equals(filter.ExtensionFilter, ExtensionFilterAll, StringComparison.Ordinal))
            {
                candidates = candidates.Where(item => string.Equals(item.Extension, filter.ExtensionFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(filter.StatusFilter, StatusFilterAll, StringComparison.Ordinal))
            {
                candidates = candidates.Where(item => string.Equals(item.StatusLabel, filter.StatusFilter, StringComparison.Ordinal));
            }

            switch (filter.CheckFilter)
            {
                case GridReviewCheckFilter.CheckedOnly:
                    candidates = candidates.Where(item => checkedSourcePaths.Contains(item.OriginalSourcePath));
                    break;
                case GridReviewCheckFilter.UncheckedOnly:
                    candidates = candidates.Where(item => !checkedSourcePaths.Contains(item.OriginalSourcePath));
                    break;
            }

            candidates = ApplyAspectRatioFilter(candidates, filter.AspectRatioFilter);
            candidates = ApplyImageSizeFilter(candidates, filter.ImageSizeFilter);

            filteredItems = ApplySort(candidates).ToList();

            int totalPages = GetTotalPages(filteredItems.Count);
            int targetPage = resetPage ? 1 : Math.Max(1, Math.Min((int)pageSelectorNumericUpDown.Value, totalPages));
            RebuildPage(targetPage);
        }

        private GridReviewFilterState ReadFilterStateFromControls()
        {
            return new GridReviewFilterState
            {
                SearchText = (searchTextBox.Text ?? string.Empty).Trim(),
                ExtensionFilter = extensionFilterComboBox.SelectedItem as string ?? ExtensionFilterAll,
                StatusFilter = statusFilterComboBox.SelectedItem as string ?? StatusFilterAll,
                CheckFilter = ParseCheckFilter(checkedFilterComboBox.SelectedItem as string),
                AspectRatioFilter = ParseAspectRatioFilter(aspectRatioFilterComboBox.SelectedItem as string),
                ImageSizeFilter = ParseImageSizeFilter(imageSizeFilterComboBox.SelectedItem as string)
            };
        }

        private IEnumerable<GridReviewItemRecord> ApplySort(IEnumerable<GridReviewItemRecord> candidates)
        {
            GridReviewSortMode sortMode = ParseSortMode(sortModeComboBox.SelectedItem as string);
            GridReviewSortDirection sortDirection = ParseSortDirection(sortDirectionComboBox.SelectedItem as string);

            Func<GridReviewItemRecord, object> keySelector;
            switch (sortMode)
            {
                case GridReviewSortMode.FileName:
                    keySelector = item => item.DisplayFileName;
                    break;
                case GridReviewSortMode.LastWriteTime:
                    keySelector = item => item.LastWriteTimeUtc;
                    break;
                case GridReviewSortMode.CreationTime:
                    keySelector = item => item.CreationTimeUtc;
                    break;
                case GridReviewSortMode.PixelSize:
                    keySelector = item => item.PixelSizeKnown ? Math.Max(item.PixelSize.Width, item.PixelSize.Height) : -1;
                    break;
                case GridReviewSortMode.FileSize:
                    keySelector = item => item.FileSize;
                    break;
                default:
                    keySelector = item => item.ExistingOrder;
                    break;
            }

            IOrderedEnumerable<GridReviewItemRecord> ordered = sortDirection == GridReviewSortDirection.Descending
                ? candidates.OrderByDescending(keySelector)
                : candidates.OrderBy(keySelector);

            return ordered.ThenBy(item => item.ExistingOrder);
        }

        private void RebuildPage(int targetPage)
        {
            int pageSize = ResolvePageSize();
            int totalPages = GetTotalPages(filteredItems.Count);
            int safePage = Math.Max(1, Math.Min(targetPage, totalPages));
            int skip = Math.Max(0, (safePage - 1) * pageSize);
            currentPageItems = filteredItems.Skip(skip).Take(pageSize).ToList();

            suppressPageSelectorEvent = true;
            pageSelectorNumericUpDown.Minimum = 1;
            pageSelectorNumericUpDown.Maximum = Math.Max(1, totalPages);
            pageSelectorNumericUpDown.Value = Math.Max(1, Math.Min(safePage, (int)pageSelectorNumericUpDown.Maximum));
            suppressPageSelectorEvent = false;

            previousPageButton.Enabled = safePage > 1;
            nextPageButton.Enabled = safePage < totalPages;
            pageLabel.Text = $"ページ {safePage:N0} / {totalPages:N0}";

            CancelThumbnailWork();
            CancelPendingThumbnailSingleClick();
            BuildListViewItems();
            BeginPageThumbnailGeneration();
            UpdateSelectionSummary();
            UpdateButtonsEnabled();
        }

        private void BuildListViewItems()
        {
            thumbnailListView.BeginUpdate();

            try
            {
                thumbnailImageList.Images.Clear();
                thumbnailImageList.Images.Add(CreatePlaceholderBitmap(thumbnailImageList.ImageSize));
                thumbnailListView.Items.Clear();

                foreach (GridReviewItemRecord item in currentPageItems)
                {
                    var listViewItem = new ListViewItem(BuildListViewText(item), 0)
                    {
                        Tag = item,
                        Checked = checkedSourcePaths.Contains(item.OriginalSourcePath),
                        ToolTipText = BuildToolTip(item),
                        BackColor = ResolveItemBackColor(item),
                        ForeColor = ResolveItemForeColor(item)
                    };
                    thumbnailListView.Items.Add(listViewItem);
                }

                emptyStateLabel.Visible = thumbnailListView.Items.Count == 0;
            }
            finally
            {
                thumbnailListView.EndUpdate();
            }
        }

        private void BeginPageThumbnailGeneration()
        {
            CancelThumbnailWork();

            if (!workspaceVisible || currentPageItems.Count == 0)
            {
                UpdateThumbnailProgress(0, 0, 0, false, string.Empty, "サムネイル待機中");
                return;
            }

            sessionLogger.Info(
                "サムネイル生成を開始します。",
                GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                GridReviewSessionLogger.CreateDetails("COUNT", currentPageItems.Count),
                GridReviewSessionLogger.CreateDetails("PAGE", pageSelectorNumericUpDown.Value));

            thumbnailGenerationRunning = true;
            thumbnailCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = thumbnailCancellationTokenSource.Token;
            int thumbnailSize = ResolveThumbnailBoxSize();
            UpdateThumbnailProgress(currentPageItems.Count, 0, 0, true, string.Empty, "サムネイル生成を開始しました。");

            Task.Run(async () =>
            {
                int completed = 0;
                int failed = 0;
                using (var semaphore = new SemaphoreSlim(4))
                {
                    var tasks = new List<Task>(currentPageItems.Count);
                    for (int pageIndex = 0; pageIndex < currentPageItems.Count; pageIndex++)
                    {
                        int capturedPageIndex = pageIndex;
                        GridReviewItemRecord record = currentPageItems[capturedPageIndex];
                        tasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync(token).ConfigureAwait(false);
                            try
                            {
                                token.ThrowIfCancellationRequested();
                                ThumbnailLoadResult result = await thumbnailCache.GetThumbnailAsync(record, thumbnailSize, token).ConfigureAwait(false);
                                if (result.PixelSize.Width > 0 && result.PixelSize.Height > 0)
                                {
                                    record.PixelSize = result.PixelSize;
                                    record.PixelSizeKnown = true;
                                    record.PixelSizeFailed = false;
                                }

                                if (result.Thumbnail != null && workspaceVisible)
                                {
                                    if (!IsDisposed && IsHandleCreated)
                                    {
                                        BeginInvoke((Action)(() =>
                                        {
                                            if (capturedPageIndex < thumbnailListView.Items.Count)
                                            {
                                                thumbnailImageList.Images.Add(result.Thumbnail);
                                                int imageIndex = thumbnailImageList.Images.Count - 1;
                                                thumbnailListView.Items[capturedPageIndex].ImageIndex = imageIndex;
                                            }

                                            result.Thumbnail.Dispose();
                                        }));
                                    }
                                    else
                                    {
                                        result.Thumbnail.Dispose();
                                    }
                                }

                                Interlocked.Increment(ref completed);
                                UpdateThumbnailProgressSafe(currentPageItems.Count, completed, failed, true, record.DisplayFileName, "サムネイル生成中");
                            }
                            catch (OperationCanceledException)
                            {
                            }
                            catch
                            {
                                record.PixelSizeFailed = true;
                                Interlocked.Increment(ref failed);
                                UpdateThumbnailProgressSafe(currentPageItems.Count, completed, failed, true, record.DisplayFileName, "サムネイル生成中に失敗がありました。");
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, token));
                    }

                    try
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                thumbnailGenerationRunning = false;
                if (token.IsCancellationRequested)
                {
                    UpdateThumbnailProgressSafe(currentPageItems.Count, completed, failed, false, string.Empty, "サムネイル生成を停止しました。");
                    sessionLogger.Warn(
                        "サムネイル生成を停止しました。",
                        GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                        GridReviewSessionLogger.CreateDetails("COUNT", currentPageItems.Count),
                        GridReviewSessionLogger.CreateDetails("DONE", completed),
                        GridReviewSessionLogger.CreateDetails("FAILED", failed));
                    return;
                }

                UpdateThumbnailProgressSafe(currentPageItems.Count, completed, failed, false, string.Empty, "サムネイル生成が完了しました。");
                sessionLogger.Info(
                    "サムネイル生成が完了しました。",
                    GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                    GridReviewSessionLogger.CreateDetails("COUNT", currentPageItems.Count),
                    GridReviewSessionLogger.CreateDetails("DONE", completed),
                    GridReviewSessionLogger.CreateDetails("FAILED", failed));
            }, token);
        }

        private async Task<bool> EnsureMetadataReadyForCurrentFiltersAsync()
        {
            GridReviewSortMode sortMode = ParseSortMode(sortModeComboBox.SelectedItem as string);
            GridReviewAspectRatioFilter aspectFilter = ParseAspectRatioFilter(aspectRatioFilterComboBox.SelectedItem as string);
            GridReviewImageSizeFilter imageSizeFilter = ParseImageSizeFilter(imageSizeFilterComboBox.SelectedItem as string);

            bool needsPixelSize =
                sortMode == GridReviewSortMode.PixelSize ||
                aspectFilter != GridReviewAspectRatioFilter.All ||
                imageSizeFilter != GridReviewImageSizeFilter.All;
            if (!needsPixelSize)
            {
                return true;
            }

            List<GridReviewItemRecord> targets = allItems
                .Where(item => !item.PixelSizeKnown && !item.PixelSizeFailed && File.Exists(item.CurrentPath))
                .ToList();
            if (targets.Count == 0)
            {
                return true;
            }

            metadataScanRunning = true;
            UpdateButtonsEnabled();
            CancelMetadataWork();
            metadataCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = metadataCancellationTokenSource.Token;

            sessionLogger.Info(
                "画像サイズ情報の収集を開始します。",
                GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                GridReviewSessionLogger.CreateDetails("COUNT", targets.Count));

            UpdateThumbnailProgressSafe(targets.Count, 0, 0, true, string.Empty, "画像サイズ情報を収集中");

            try
            {
                int completed = 0;
                int failed = 0;
                int thumbnailSize = ResolveThumbnailBoxSize();
                foreach (GridReviewItemRecord target in targets)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        ThumbnailLoadResult result = await thumbnailCache.GetThumbnailAsync(target, thumbnailSize, token).ConfigureAwait(false);
                        if (result.Thumbnail != null)
                        {
                            result.Thumbnail.Dispose();
                        }

                        if (result.PixelSize.Width > 0 && result.PixelSize.Height > 0)
                        {
                            target.PixelSize = result.PixelSize;
                            target.PixelSizeKnown = true;
                            target.PixelSizeFailed = false;
                        }
                    }
                    catch
                    {
                        target.PixelSizeFailed = true;
                        failed++;
                    }

                    completed++;
                    UpdateThumbnailProgressSafe(targets.Count, completed, failed, true, target.DisplayFileName, "画像サイズ情報を収集中");
                }

                UpdateThumbnailProgressSafe(targets.Count, targets.Count, failed, false, string.Empty, "画像サイズ情報の収集が完了しました。");
                sessionLogger.Info(
                    "画像サイズ情報の収集が完了しました。",
                    GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                    GridReviewSessionLogger.CreateDetails("DONE", targets.Count),
                    GridReviewSessionLogger.CreateDetails("FAILED", failed));
                return true;
            }
            catch (OperationCanceledException)
            {
                UpdateThumbnailProgressSafe(targets.Count, 0, 0, false, string.Empty, "画像サイズ情報の収集を停止しました。");
                sessionLogger.Warn(
                    "画像サイズ情報の収集を停止しました。",
                    GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                    GridReviewSessionLogger.CreateDetails("COUNT", targets.Count));
                return false;
            }
            finally
            {
                metadataScanRunning = false;
                UpdateButtonsEnabled();
            }
        }

        private async Task ExcludeCheckedAsync()
        {
            using (var dialog = new BatchMoveTargetDialog(ownerMain.GetDestinationChoices()))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                await ExcludeCheckedAsync(dialog.SelectedFolderPath, dialog.SelectedLabel);
            }
        }

        private async Task ExcludeCheckedAsync(string destinationFolderPath, string destinationLabel)
        {
            List<GridReviewItemRecord> targets = filteredItems
                .Where(item => checkedSourcePaths.Contains(item.OriginalSourcePath))
                .Where(item => item.Status != GridReviewItemStatus.Excluded)
                .ToList();
            if (targets.Count == 0)
            {
                ShowOwnedMessage("除外へ送りたい画像にチェックを付けてください。", "グリッド目検", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string safeDestination = (destinationFolderPath ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(safeDestination) || !Directory.Exists(safeDestination))
            {
                ShowOwnedMessage("除外先フォルダを指定してください。", "グリッド目検", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string confirmMessage =
                $"チェック済み {targets.Count:N0} 枚を除外へ送ります。{Environment.NewLine}{Environment.NewLine}" +
                $"除外先: {safeDestination}{Environment.NewLine}{Environment.NewLine}" +
                "元画像は削除せず、既存運用どおり移動します。実行しますか。";
            if (TopMostMessageBox.Show(this, confirmMessage, "グリッド目検", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            operationRunning = true;
            UpdateButtonsEnabled();
            lastExcludeFolderPath = safeDestination;

            try
            {
                BatchMoveExecutionResult result = await Task.Run(
                    () => ownerMain.MoveImagesFromGrid(
                        targets.Select(item => item.CurrentPath).ToList(),
                        safeDestination,
                        string.IsNullOrWhiteSpace(destinationLabel) ? "グリッド目検" : destinationLabel,
                        MoveOperationOrigin.GridReview,
                        UpdateBatchProgressFromMain));

                string resultMessage = $"除外へ送った画像: {result.MovedCount:N0} 件";
                if (result.FailureDetails.Count > 0)
                {
                    resultMessage += Environment.NewLine + string.Join(Environment.NewLine, result.FailureDetails.Take(5).Select(detail => detail.ErrorMessage));
                }

                ShowOwnedMessage(resultMessage, "グリッド目検", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                operationRunning = false;
                UpdateButtonsEnabled();
            }
        }

        private async Task RestoreCheckedAsync()
        {
            List<GridReviewRestoreRequest> targets = filteredItems
                .Where(item => checkedSourcePaths.Contains(item.OriginalSourcePath))
                .Where(item => item.Status == GridReviewItemStatus.Excluded)
                .Select(item => new GridReviewRestoreRequest
                {
                    StableSourcePath = item.OriginalSourcePath,
                    CurrentPath = item.CurrentPath,
                    RestoreTargetPath = item.OriginalSourcePath,
                    OriginalIndex = item.ExistingOrder
                })
                .ToList();
            if (targets.Count == 0)
            {
                ShowOwnedMessage("戻したい除外済み画像にチェックを付けてください。", "グリッド目検", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string confirmMessage =
                $"チェック済み {targets.Count:N0} 枚を元フォルダへ戻します。{Environment.NewLine}{Environment.NewLine}" +
                "元フォルダに同名ファイルがある場合は上書きせずスキップします。実行しますか。";
            if (TopMostMessageBox.Show(this, confirmMessage, "グリッド目検", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            operationRunning = true;
            UpdateButtonsEnabled();

            try
            {
                BatchMoveExecutionResult result = await Task.Run(
                    () => ownerMain.RestoreImagesFromGrid(targets, UpdateBatchProgressFromMain));

                string resultMessage = $"元フォルダへ戻した画像: {result.MovedCount:N0} 件";
                if (result.FailureDetails.Count > 0)
                {
                    resultMessage += Environment.NewLine + string.Join(Environment.NewLine, result.FailureDetails.Take(5).Select(detail => detail.ErrorMessage));
                }

                ShowOwnedMessage(resultMessage, "グリッド目検", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                operationRunning = false;
                UpdateButtonsEnabled();
            }
        }

        private void UpdateBatchProgressFromMain(BatchMoveProgressInfo progress)
        {
            if (progress == null)
            {
                return;
            }

            UpdateThumbnailProgressSafe(
                progress.TotalCount,
                progress.ProcessedCount,
                progress.SkippedCount,
                progress.ProcessedCount < progress.TotalCount,
                progress.CurrentFileName,
                progress.StatusText);
        }

        private void UpdateButtonsEnabled()
        {
            bool hasItems = allItems.Count > 0;
            bool hasFilteredItems = filteredItems.Count > 0;
            bool hasExcludedCheckedItems = filteredItems.Any(item => checkedSourcePaths.Contains(item.OriginalSourcePath) && item.Status == GridReviewItemStatus.Excluded);
            bool hasActiveCheckedItems = filteredItems.Any(item => checkedSourcePaths.Contains(item.OriginalSourcePath) && item.Status != GridReviewItemStatus.Excluded);
            bool metadataBusy = metadataScanRunning || operationRunning;
            int currentPage = Math.Max(1, (int)pageSelectorNumericUpDown.Value);
            int totalPages = GetTotalPages();

            applyFilterButton.Enabled = !metadataBusy;
            resetFilterButton.Enabled = !metadataBusy;
            checkAllButton.Enabled = hasFilteredItems && !metadataBusy;
            checkVisibleButton.Enabled = currentPageItems.Count > 0 && !metadataBusy;
            invertCheckButton.Enabled = hasFilteredItems && !metadataBusy;
            clearCheckButton.Enabled = checkedSourcePaths.Count > 0 && !metadataBusy;
            excludeCheckedButton.Enabled = hasActiveCheckedItems && !metadataBusy;
            restoreCheckedButton.Enabled = hasExcludedCheckedItems && !metadataBusy;
            previousPageButton.Enabled = currentPage > 1 && !metadataBusy;
            nextPageButton.Enabled = currentPage < totalPages && !metadataBusy;
            pageSelectorNumericUpDown.Enabled = hasItems && !metadataBusy;
            cancelThumbnailButton.Enabled = thumbnailGenerationRunning || metadataScanRunning;
            clearCacheButton.Enabled = !operationRunning;
        }

        private void MergeSnapshot(ImageBrowserSnapshot snapshot)
        {
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < snapshot.FullPaths.Length; index++)
            {
                string fullPath = snapshot.FullPaths[index];
                string stableKey = fullPath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(stableKey))
                {
                    continue;
                }

                GridReviewItemRecord record = GetOrCreateItem(stableKey, index);
                record.ExistingOrder = index;
                record.OriginalSourcePath = stableKey;
                record.CurrentPath = fullPath;
                record.FileName = snapshot.FileNames.Length > index ? snapshot.FileNames[index] : Path.GetFileName(fullPath) ?? string.Empty;
                record.Extension = (Path.GetExtension(fullPath) ?? string.Empty).ToLowerInvariant();
                if (!record.WasExcludedOnce)
                {
                    record.Status = GridReviewItemStatus.Unprocessed;
                }
                else if (record.Status == GridReviewItemStatus.Excluded)
                {
                    record.Status = GridReviewItemStatus.Restored;
                }

                RefreshBasicMetadata(record);
                seenKeys.Add(stableKey);
            }

            foreach (GridReviewItemRecord record in allItems.ToArray())
            {
                if (seenKeys.Contains(record.OriginalSourcePath))
                {
                    continue;
                }

                if (record.Status == GridReviewItemStatus.Excluded && File.Exists(record.CurrentPath))
                {
                    continue;
                }

                allItems.Remove(record);
                itemMap.Remove(record.OriginalSourcePath);
                checkedSourcePaths.Remove(record.OriginalSourcePath);
            }

            allItems.Sort((left, right) => left.ExistingOrder.CompareTo(right.ExistingOrder));
        }

        private GridReviewItemRecord GetOrCreateItem(string stableSourcePath, int existingOrder)
        {
            string safeKey = stableSourcePath ?? string.Empty;
            if (itemMap.TryGetValue(safeKey, out GridReviewItemRecord record))
            {
                return record;
            }

            record = new GridReviewItemRecord
            {
                ExistingOrder = existingOrder,
                OriginalSourcePath = safeKey,
                CurrentPath = safeKey,
                FileName = Path.GetFileName(safeKey) ?? string.Empty,
                Extension = (Path.GetExtension(safeKey) ?? string.Empty).ToLowerInvariant()
            };
            itemMap[safeKey] = record;
            allItems.Add(record);
            return record;
        }

        private void ResetSessionCatalog()
        {
            CancelPendingThumbnailSingleClick();
            checkedSourcePaths.Clear();
            itemMap.Clear();
            allItems.Clear();
            filteredItems.Clear();
            currentPageItems.Clear();
            historyRecords.Clear();
            CancelThumbnailWork();
            CancelMetadataWork();
        }

        private void RefreshExtensionFilterItems()
        {
            string selectedValue = extensionFilterComboBox.SelectedItem as string ?? ExtensionFilterAll;
            string[] extensions = allItems
                .Select(item => item.Extension)
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            extensionFilterComboBox.BeginUpdate();
            try
            {
                extensionFilterComboBox.Items.Clear();
                extensionFilterComboBox.Items.Add(ExtensionFilterAll);
                foreach (string extension in extensions)
                {
                    extensionFilterComboBox.Items.Add(extension);
                }

                if (extensionFilterComboBox.Items.Contains(selectedValue))
                {
                    extensionFilterComboBox.SelectedItem = selectedValue;
                }
                else
                {
                    extensionFilterComboBox.SelectedIndex = 0;
                }
            }
            finally
            {
                extensionFilterComboBox.EndUpdate();
            }
        }

        private void RefreshBasicMetadata(GridReviewItemRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.CurrentPath) || !File.Exists(record.CurrentPath))
            {
                return;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(record.CurrentPath);
                record.FileName = fileInfo.Name;
                record.Extension = fileInfo.Extension.ToLowerInvariant();
                record.FileSize = fileInfo.Length;
                record.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                record.CreationTimeUtc = fileInfo.CreationTimeUtc;
            }
            catch
            {
            }
        }

        private static IEnumerable<GridReviewItemRecord> ApplyAspectRatioFilter(IEnumerable<GridReviewItemRecord> candidates, GridReviewAspectRatioFilter filter)
        {
            switch (filter)
            {
                case GridReviewAspectRatioFilter.Landscape:
                    return candidates.Where(item => item.PixelSizeKnown && item.PixelSize.Width > item.PixelSize.Height * 1.2);
                case GridReviewAspectRatioFilter.Portrait:
                    return candidates.Where(item => item.PixelSizeKnown && item.PixelSize.Height > item.PixelSize.Width * 1.2);
                case GridReviewAspectRatioFilter.NearSquare:
                    return candidates.Where(item => item.PixelSizeKnown && Math.Abs(item.PixelSize.Width - item.PixelSize.Height) <= Math.Max(64, Math.Max(item.PixelSize.Width, item.PixelSize.Height) * 0.15));
                default:
                    return candidates;
            }
        }

        private static IEnumerable<GridReviewItemRecord> ApplyImageSizeFilter(IEnumerable<GridReviewItemRecord> candidates, GridReviewImageSizeFilter filter)
        {
            switch (filter)
            {
                case GridReviewImageSizeFilter.Small:
                    return candidates.Where(item => item.PixelSizeKnown && Math.Max(item.PixelSize.Width, item.PixelSize.Height) <= 1024);
                case GridReviewImageSizeFilter.Medium:
                    return candidates.Where(item => item.PixelSizeKnown && Math.Max(item.PixelSize.Width, item.PixelSize.Height) > 1024 && Math.Max(item.PixelSize.Width, item.PixelSize.Height) <= 2048);
                case GridReviewImageSizeFilter.Large:
                    return candidates.Where(item => item.PixelSizeKnown && Math.Max(item.PixelSize.Width, item.PixelSize.Height) > 2048);
                default:
                    return candidates;
            }
        }

        private static string BuildToolTip(GridReviewItemRecord item)
        {
            string sizeText = item.PixelSizeKnown ? $"{item.PixelSize.Width} x {item.PixelSize.Height}" : "未取得";
            return
                $"状態: {item.StatusLabel}{Environment.NewLine}" +
                $"ファイル名: {item.DisplayFileName}{Environment.NewLine}" +
                $"画像ID: {item.ExistingOrder + 1}{Environment.NewLine}" +
                $"画像サイズ: {sizeText}{Environment.NewLine}" +
                $"ファイルサイズ: {item.FileSize:N0} bytes{Environment.NewLine}" +
                $"現在パス: {item.CurrentPath}";
        }

        private string BuildListViewText(GridReviewItemRecord item)
        {
            var parts = new List<string>(2);
            if (showImageIdCheckBox.Checked)
            {
                parts.Add($"#{item.ExistingOrder + 1:D5}");
            }

            if (showFileNameCheckBox.Checked)
            {
                parts.Add(ShortenText(item.DisplayFileName, 28));
            }

            if (parts.Count == 0)
            {
                return " ";
            }

            return string.Join(" ", parts);
        }

        private static string ShortenText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
            {
                return text ?? string.Empty;
            }

            return text.Substring(0, Math.Max(1, maxLength - 1)) + "…";
        }

        private static Color ResolveItemBackColor(GridReviewItemRecord item)
        {
            switch (item.Status)
            {
                case GridReviewItemStatus.Excluded:
                    return Color.FromArgb(255, 244, 244);
                case GridReviewItemStatus.Restored:
                    return Color.FromArgb(245, 252, 245);
                default:
                    return SystemColors.Window;
            }
        }

        private static Color ResolveItemForeColor(GridReviewItemRecord item)
        {
            return item.Status == GridReviewItemStatus.Excluded ? Color.Firebrick : SystemColors.ControlText;
        }

        private static Bitmap CreatePlaceholderBitmap(Size imageSize)
        {
            var bitmap = new Bitmap(Math.Max(32, imageSize.Width), Math.Max(32, imageSize.Height));
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(245, 245, 245));
                graphics.DrawRectangle(Pens.Gainsboro, 0, 0, bitmap.Width - 1, bitmap.Height - 1);
                TextRenderer.DrawText(
                    graphics,
                    "読込中",
                    SystemFonts.DefaultFont,
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    Color.DimGray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            return bitmap;
        }

        private static bool IsDescendantOf(Control parent, Control child)
        {
            for (Control current = child; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, parent))
                {
                    return true;
                }
            }

            return false;
        }

        private void ThumbnailListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (!(e.Item.Tag is GridReviewItemRecord record))
            {
                return;
            }

            if (e.Item.Checked)
            {
                checkedSourcePaths.Add(record.OriginalSourcePath);
            }
            else
            {
                checkedSourcePaths.Remove(record.OriginalSourcePath);
            }

            UpdateSelectionSummary();
            UpdateButtonsEnabled();
        }

        private void ThumbnailListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e == null || e.Button != MouseButtons.Left || e.Clicks != 1 || !thumbnailListView.CheckBoxes || operationRunning)
            {
                return;
            }

            ListViewHitTestInfo hitTest = thumbnailListView.HitTest(e.Location);
            if (hitTest?.Item == null)
            {
                return;
            }

            if ((hitTest.Location & ListViewHitTestLocations.StateImage) == ListViewHitTestLocations.StateImage)
            {
                return;
            }

            QueueThumbnailSingleClickToggle(hitTest.Item);
        }

        private void ThumbnailSingleClickTimer_Tick(object sender, EventArgs e)
        {
            thumbnailSingleClickTimer.Stop();
            ApplyPendingThumbnailSingleClickToggle();
        }

        private void QueueThumbnailSingleClickToggle(ListViewItem item)
        {
            if (item == null || item.ListView != thumbnailListView || !thumbnailListView.CheckBoxes)
            {
                return;
            }

            pendingThumbnailSingleClickItem = item;
            thumbnailSingleClickTimer.Interval = Math.Max(200, SystemInformation.DoubleClickTime + 20);
            thumbnailSingleClickTimer.Stop();
            thumbnailSingleClickTimer.Start();
        }

        private void CancelPendingThumbnailSingleClick()
        {
            pendingThumbnailSingleClickItem = null;
            thumbnailSingleClickTimer.Stop();
        }

        private void ApplyPendingThumbnailSingleClickToggle()
        {
            ListViewItem item = pendingThumbnailSingleClickItem;
            pendingThumbnailSingleClickItem = null;
            if (item == null || item.ListView != thumbnailListView || !thumbnailListView.CheckBoxes)
            {
                return;
            }

            item.Checked = !item.Checked;
        }

        private void ThumbnailListView_DoubleClick(object sender, EventArgs e)
        {
            CancelPendingThumbnailSingleClick();
            if (!(thumbnailListView.SelectedItems.Count > 0) || !(thumbnailListView.SelectedItems[0].Tag is GridReviewItemRecord record))
            {
                return;
            }

            string pathToShow = record.Status == GridReviewItemStatus.Excluded
                ? record.CurrentPath
                : record.OriginalSourcePath;
            if (record.Status == GridReviewItemStatus.Excluded)
            {
                ShowOwnedMessage("除外済み画像は単票モードの一覧外です。元フォルダへ戻すと単票モードから再確認できます。", "グリッド目検", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ownerMain.ShowImageByPath(pathToShow);
        }

        private void UpdateSelectionSummary()
        {
            int currentPage = Math.Max(1, (int)pageSelectorNumericUpDown.Value);
            int totalPages = GetTotalPages();
            int checkedCount = checkedSourcePaths.Count;
            selectionLabel.Text = $"選択中: {checkedCount:N0}枚 / 表示中: {currentPageItems.Count:N0}枚 / 全体: {allItems.Count:N0}枚";
            summaryLabel.Text = $"対象 {filteredItems.Count:N0} 件 / ページ {currentPage:N0} / {totalPages:N0} / チェック {checkedCount:N0} 件";
        }

        private void SetCheckedStateForItems(IEnumerable<GridReviewItemRecord> items, bool isChecked)
        {
            foreach (GridReviewItemRecord item in items.Where(item => item != null))
            {
                if (isChecked)
                {
                    checkedSourcePaths.Add(item.OriginalSourcePath);
                }
                else
                {
                    checkedSourcePaths.Remove(item.OriginalSourcePath);
                }
            }

            foreach (ListViewItem listViewItem in thumbnailListView.Items)
            {
                if (!(listViewItem.Tag is GridReviewItemRecord record))
                {
                    continue;
                }

                listViewItem.Checked = checkedSourcePaths.Contains(record.OriginalSourcePath);
            }

            UpdateSelectionSummary();
            UpdateButtonsEnabled();
        }

        private void ClearAllCheckedState()
        {
            checkedSourcePaths.Clear();
            foreach (ListViewItem item in thumbnailListView.Items)
            {
                item.Checked = false;
            }

            UpdateSelectionSummary();
            UpdateButtonsEnabled();
        }

        private void InvertCheckedStateForFilteredItems()
        {
            foreach (GridReviewItemRecord item in filteredItems)
            {
                if (checkedSourcePaths.Contains(item.OriginalSourcePath))
                {
                    checkedSourcePaths.Remove(item.OriginalSourcePath);
                }
                else
                {
                    checkedSourcePaths.Add(item.OriginalSourcePath);
                }
            }

            foreach (ListViewItem listViewItem in thumbnailListView.Items)
            {
                if (!(listViewItem.Tag is GridReviewItemRecord record))
                {
                    continue;
                }

                listViewItem.Checked = checkedSourcePaths.Contains(record.OriginalSourcePath);
            }

            UpdateSelectionSummary();
            UpdateButtonsEnabled();
        }

        private void ResetFilterControls()
        {
            searchTextBox.Clear();
            if (extensionFilterComboBox.Items.Count > 0)
            {
                extensionFilterComboBox.SelectedIndex = 0;
            }

            aspectRatioFilterComboBox.SelectedIndex = 0;
            imageSizeFilterComboBox.SelectedIndex = 0;
            checkedFilterComboBox.SelectedIndex = 0;
            statusFilterComboBox.SelectedIndex = 0;
            sortModeComboBox.SelectedIndex = 0;
            sortDirectionComboBox.SelectedIndex = 0;
            ApplyFiltersAndRefreshPage(resetPage: true);
        }

        private void MovePage(int offset)
        {
            int totalPages = GetTotalPages();
            int currentPage = Math.Max(1, (int)pageSelectorNumericUpDown.Value);
            int targetPage = Math.Max(1, Math.Min(currentPage + offset, totalPages));
            pageSelectorNumericUpDown.Value = targetPage;
        }

        private void PageSelectorNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (suppressPageSelectorEvent)
            {
                return;
            }

            RebuildPage((int)pageSelectorNumericUpDown.Value);
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            _ = ApplyFiltersAndRefreshPageAsync(resetPage: true);
        }

        private void CancelThumbnailWork()
        {
            CancellationTokenSource previous = Interlocked.Exchange(ref thumbnailCancellationTokenSource, null);
            if (previous == null)
            {
                return;
            }

            try
            {
                previous.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                previous.Dispose();
                thumbnailGenerationRunning = false;
            }
        }

        private void CancelMetadataWork()
        {
            CancellationTokenSource previous = Interlocked.Exchange(ref metadataCancellationTokenSource, null);
            if (previous == null)
            {
                return;
            }

            try
            {
                previous.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                previous.Dispose();
                metadataScanRunning = false;
            }
        }

        private void ClearCacheButton_Click(object sender, EventArgs e)
        {
            if (TopMostMessageBox.Show(this, "サムネイルキャッシュを削除して再生成します。実行しますか。", "グリッド目検", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            CancelThumbnailWork();
            thumbnailCache.Clear();
            sessionLogger.Info(
                "サムネイルキャッシュをクリアしました。",
                GridReviewSessionLogger.CreateDetails("WHO", "ImageMove"),
                GridReviewSessionLogger.CreateDetails("PATH", thumbnailCache.CacheRootPath));
            BeginPageThumbnailGeneration();
        }

        private void UpdateThumbnailProgressSafe(int totalCount, int completedCount, int failedCount, bool isRunning, string currentFileName, string statusText)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke((Action)(() => UpdateThumbnailProgress(totalCount, completedCount, failedCount, isRunning, currentFileName, statusText)));
        }

        private void UpdateThumbnailProgress(int totalCount, int completedCount, int failedCount, bool isRunning, string currentFileName, string statusText)
        {
            thumbnailProgress = new GridReviewThumbnailProgress
            {
                TotalCount = totalCount,
                CompletedCount = completedCount,
                FailedCount = failedCount,
                IsRunning = isRunning,
                CurrentFileName = currentFileName ?? string.Empty,
                StatusText = statusText ?? string.Empty
            };

            int safeTotal = Math.Max(1, totalCount);
            int safeCompleted = Math.Max(0, Math.Min(completedCount, safeTotal));
            thumbnailProgressBar.Minimum = 0;
            thumbnailProgressBar.Maximum = safeTotal;
            thumbnailProgressBar.Value = safeCompleted;

            string currentFileText = string.IsNullOrWhiteSpace(currentFileName) ? string.Empty : $" / {currentFileName}";
            thumbnailStatusLabel.Text = $"{safeCompleted:N0}/{totalCount:N0} 件 / 失敗 {failedCount:N0} 件 / {statusText}{currentFileText}";
            cancelThumbnailButton.Enabled = isRunning;
        }

        private void AddHistoryRecord(MoveHistoryAction action, int successCount, int failureCount, string targetFolder, string note)
        {
            if (action == null)
            {
                return;
            }

            historyRecords.Insert(0, new GridReviewOperationRecord
            {
                OperationId = action.OperationId,
                ExecutedAt = action.ExecutedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                Who = action.Who,
                OperationType = FormatOperationKind(action.OperationKind),
                TargetCount = action.Items.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                TargetFolder = targetFolder ?? string.Empty,
                Note = note ?? string.Empty
            });
        }

        private void ShowOwnedMessage(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            IWin32Window ownerWindow = ownerMain != null && !ownerMain.IsDisposed
                ? (IWin32Window)ownerMain
                : this;
            TopMostMessageBox.Show(ownerWindow, text, caption, buttons, icon);
        }

        private int ResolveThumbnailBoxSize()
        {
            if (string.Equals(thumbnailSizeComboBox.SelectedItem as string, "小", StringComparison.Ordinal))
            {
                return 180;
            }

            if (string.Equals(thumbnailSizeComboBox.SelectedItem as string, "大", StringComparison.Ordinal))
            {
                return 320;
            }

            if (string.Equals(thumbnailSizeComboBox.SelectedItem as string, "カスタム", StringComparison.Ordinal))
            {
                return (int)thumbnailCustomSizeNumericUpDown.Value;
            }

            return 240;
        }

        private int ResolvePageSize()
        {
            string selected = pageSizeComboBox.SelectedItem as string;
            switch (selected)
            {
                case "100":
                    return 100;
                case "300":
                    return 300;
                case "500":
                    return 500;
                case "カスタム":
                    return (int)pageSizeCustomNumericUpDown.Value;
                default:
                    return 1000;
            }
        }

        private int GetTotalPages()
        {
            return GetTotalPages(filteredItems.Count);
        }

        private int GetTotalPages(int filteredCount)
        {
            int pageSize = Math.Max(1, ResolvePageSize());
            return Math.Max(1, (int)Math.Ceiling((double)Math.Max(0, filteredCount) / pageSize));
        }

        private static TableLayoutPanel CreateTwoColumnLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return layout;
        }

        private static Label CreateCaptionLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3, 8, 3, 3)
            };
        }

        private static GroupBox CreateGroupPanel(string title, Control content)
        {
            var groupBox = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Text = title
            };
            groupBox.Controls.Add(content);
            return groupBox;
        }

        private static decimal ClampNumericValue(NumericUpDown control, int rawValue)
        {
            decimal safeValue = Math.Max((int)control.Minimum, Math.Min(rawValue, (int)control.Maximum));
            return safeValue;
        }

        private static string FormatOperationKind(MoveOperationKind operationKind)
        {
            return operationKind == MoveOperationKind.RestoreToSource ? "戻し" : "除外";
        }

        private static string GetStatusLabel(GridReviewItemStatus status)
        {
            switch (status)
            {
                case GridReviewItemStatus.Excluded:
                    return StatusFilterExcluded;
                case GridReviewItemStatus.Restored:
                    return StatusFilterRestored;
                default:
                    return StatusFilterUnprocessed;
            }
        }

        private static GridThumbnailSizePreset ParseThumbnailSizePreset(string rawValue)
        {
            switch (rawValue)
            {
                case "小":
                    return GridThumbnailSizePreset.Small;
                case "大":
                    return GridThumbnailSizePreset.Large;
                case "カスタム":
                    return GridThumbnailSizePreset.Custom;
                default:
                    return GridThumbnailSizePreset.Medium;
            }
        }

        private static string FormatThumbnailPreset(GridThumbnailSizePreset preset)
        {
            switch (preset)
            {
                case GridThumbnailSizePreset.Small:
                    return "小";
                case GridThumbnailSizePreset.Large:
                    return "大";
                case GridThumbnailSizePreset.Custom:
                    return "カスタム";
                default:
                    return "中";
            }
        }

        private static GridPageSizePreset ParsePageSizePreset(string rawValue)
        {
            switch (rawValue)
            {
                case "100":
                    return GridPageSizePreset.P100;
                case "300":
                    return GridPageSizePreset.P300;
                case "500":
                    return GridPageSizePreset.P500;
                case "カスタム":
                    return GridPageSizePreset.Custom;
                default:
                    return GridPageSizePreset.P1000;
            }
        }

        private static string FormatPageSizePreset(GridPageSizePreset preset)
        {
            switch (preset)
            {
                case GridPageSizePreset.P100:
                    return "100";
                case GridPageSizePreset.P300:
                    return "300";
                case GridPageSizePreset.P500:
                    return "500";
                case GridPageSizePreset.Custom:
                    return "カスタム";
                default:
                    return "1000";
            }
        }

        private static GridReviewSortMode ParseSortMode(string rawValue)
        {
            switch (rawValue)
            {
                case "ファイル名順":
                    return GridReviewSortMode.FileName;
                case "更新日時順":
                    return GridReviewSortMode.LastWriteTime;
                case "作成日時順":
                    return GridReviewSortMode.CreationTime;
                case "画像サイズ順":
                    return GridReviewSortMode.PixelSize;
                case "ファイルサイズ順":
                    return GridReviewSortMode.FileSize;
                default:
                    return GridReviewSortMode.ExistingOrder;
            }
        }

        private static string FormatSortMode(GridReviewSortMode sortMode)
        {
            switch (sortMode)
            {
                case GridReviewSortMode.FileName:
                    return "ファイル名順";
                case GridReviewSortMode.LastWriteTime:
                    return "更新日時順";
                case GridReviewSortMode.CreationTime:
                    return "作成日時順";
                case GridReviewSortMode.PixelSize:
                    return "画像サイズ順";
                case GridReviewSortMode.FileSize:
                    return "ファイルサイズ順";
                default:
                    return "既存順";
            }
        }

        private static GridReviewSortDirection ParseSortDirection(string rawValue)
        {
            return string.Equals(rawValue, "降順", StringComparison.Ordinal)
                ? GridReviewSortDirection.Descending
                : GridReviewSortDirection.Ascending;
        }

        private static string FormatSortDirection(GridReviewSortDirection sortDirection)
        {
            return sortDirection == GridReviewSortDirection.Descending ? "降順" : "昇順";
        }

        private static GridReviewCheckFilter ParseCheckFilter(string rawValue)
        {
            switch (rawValue)
            {
                case "チェック済みのみ":
                    return GridReviewCheckFilter.CheckedOnly;
                case "未チェックのみ":
                    return GridReviewCheckFilter.UncheckedOnly;
                default:
                    return GridReviewCheckFilter.All;
            }
        }

        private static string FormatCheckFilter(GridReviewCheckFilter filter)
        {
            switch (filter)
            {
                case GridReviewCheckFilter.CheckedOnly:
                    return "チェック済みのみ";
                case GridReviewCheckFilter.UncheckedOnly:
                    return "未チェックのみ";
                default:
                    return "すべて";
            }
        }

        private static GridReviewAspectRatioFilter ParseAspectRatioFilter(string rawValue)
        {
            switch (rawValue)
            {
                case "横長":
                    return GridReviewAspectRatioFilter.Landscape;
                case "縦長":
                    return GridReviewAspectRatioFilter.Portrait;
                case "正方形付近":
                    return GridReviewAspectRatioFilter.NearSquare;
                default:
                    return GridReviewAspectRatioFilter.All;
            }
        }

        private static string FormatAspectRatioFilter(GridReviewAspectRatioFilter filter)
        {
            switch (filter)
            {
                case GridReviewAspectRatioFilter.Landscape:
                    return "横長";
                case GridReviewAspectRatioFilter.Portrait:
                    return "縦長";
                case GridReviewAspectRatioFilter.NearSquare:
                    return "正方形付近";
                default:
                    return "すべて";
            }
        }

        private static GridReviewImageSizeFilter ParseImageSizeFilter(string rawValue)
        {
            switch (rawValue)
            {
                case "小さめ":
                    return GridReviewImageSizeFilter.Small;
                case "中くらい":
                    return GridReviewImageSizeFilter.Medium;
                case "大きめ":
                    return GridReviewImageSizeFilter.Large;
                default:
                    return GridReviewImageSizeFilter.All;
            }
        }

        private static string FormatImageSizeFilter(GridReviewImageSizeFilter filter)
        {
            switch (filter)
            {
                case GridReviewImageSizeFilter.Small:
                    return "小さめ";
                case GridReviewImageSizeFilter.Medium:
                    return "中くらい";
                case GridReviewImageSizeFilter.Large:
                    return "大きめ";
                default:
                    return "すべて";
            }
        }
    }

    internal sealed class DoubleBufferedListView : ListView
    {
        internal DoubleBufferedListView()
        {
            DoubleBuffered = true;
        }
    }
}
