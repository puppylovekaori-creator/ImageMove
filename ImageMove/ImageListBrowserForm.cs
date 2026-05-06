using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMove
{
    internal sealed class ImageListBrowserForm : Form
    {
        private const int MaxVisibleRows = 100000;
        private const int ParallelFilterThreshold = 50000;

        private readonly Main ownerMain;
        private readonly HashSet<string> checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly TextBox filterTextBox;
        private readonly TableLayoutPanel gridHostLayout;
        private readonly TableLayoutPanel summaryPanel;
        private readonly DataGridView summaryGrid;
        private readonly DataGridView imageGrid;
        private readonly Label summaryGroupLabel;
        private readonly Label summaryLabel;
        private readonly ProgressBar batchMoveProgressBar;
        private readonly Label batchMoveStatusLabel;
        private readonly List<Control> batchMoveBusyControls = new List<Control>();
        private CancellationTokenSource filterCts;
        private ImageBrowserSnapshot currentSnapshot = ImageBrowserSnapshot.Empty;
        private SequenceSummaryGroup[] summaryGroups = Array.Empty<SequenceSummaryGroup>();
        private int[] filteredIndices = Array.Empty<int>();
        private string currentPath = string.Empty;
        private string pendingPreferredSelectedPath = string.Empty;
        private string appliedFilterText = string.Empty;
        private string[] appliedFilterSnapshotPaths = Array.Empty<string>();
        private bool filterInProgress;
        private bool showAllRows = true;
        private int visibleRowCount;
        private int visibleStartIndex;
        private int matchedRowCount;
        private string truncationMessage = string.Empty;
        private bool batchMoveInProgress;
        private bool checkApplyInProgress;
        private bool closeRequested;

        internal ImageListBrowserForm(Main ownerMain)
        {
            this.ownerMain = ownerMain ?? throw new ArgumentNullException(nameof(ownerMain));

            Text = "画像一覧と一括移動";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1120, 760);
            MinimumSize = new Size(920, 620);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));

            var filterPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5
            };
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));

            filterPanel.Controls.Add(new Label
            {
                Text = "名前で絞り込み",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            filterTextBox = new TextBox
            {
                Dock = DockStyle.Fill
            };
            filterTextBox.TextChanged += FilterTextBox_TextChanged;
            filterPanel.Controls.Add(filterTextBox, 1, 0);

            var applyFilterButton = new Button
            {
                Text = "絞り込み",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            applyFilterButton.Click += (_, __) => ApplyCurrentFilter();
            filterPanel.Controls.Add(applyFilterButton, 2, 0);

            var clearFilterButton = new Button
            {
                Text = "絞り込み解除",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            clearFilterButton.Click += (_, __) =>
            {
                filterTextBox.Clear();
                ApplyCurrentFilter();
            };
            filterPanel.Controls.Add(clearFilterButton, 3, 0);

            var refreshButton = new Button
            {
                Text = "再読込",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            refreshButton.Click += (_, __) => RefreshItems();
            filterPanel.Controls.Add(refreshButton, 4, 0);

            summaryGroupLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "連番サマリ",
                TextAlign = ContentAlignment.MiddleLeft
            };

            summaryGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                VirtualMode = true,
                ReadOnly = false
            };
            summaryGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "checked",
                HeaderText = "選択",
                Width = 60
            });
            summaryGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "count",
                HeaderText = "件数",
                Width = 70,
                ReadOnly = true
            });
            summaryGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "checkedCount",
                HeaderText = "選択済み",
                Width = 90,
                ReadOnly = true
            });
            summaryGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "groupName",
                HeaderText = "連番前",
                Width = 260,
                ReadOnly = true
            });
            summaryGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "sampleFileName",
                HeaderText = "代表ファイル名",
                Width = 260,
                ReadOnly = true
            });
            summaryGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "sampleRelativePath",
                HeaderText = "代表相対パス",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            summaryGrid.CurrentCellDirtyStateChanged += SummaryGrid_CurrentCellDirtyStateChanged;
            summaryGrid.CellValueNeeded += SummaryGrid_CellValueNeeded;
            summaryGrid.CellValuePushed += SummaryGrid_CellValuePushed;

            imageGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                VirtualMode = true,
                ReadOnly = false
            };
            imageGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "checked",
                HeaderText = "選択",
                Width = 60
            });
            imageGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "current",
                HeaderText = "現在",
                Width = 55,
                ReadOnly = true
            });
            imageGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "fileName",
                HeaderText = "ファイル名",
                Width = 260,
                ReadOnly = true
            });
            imageGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "relativePath",
                HeaderText = "相対パス",
                Width = 360,
                ReadOnly = true
            });
            imageGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "fullPath",
                HeaderText = "フルパス",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            imageGrid.CurrentCellDirtyStateChanged += ImageGrid_CurrentCellDirtyStateChanged;
            imageGrid.CellValueNeeded += ImageGrid_CellValueNeeded;
            imageGrid.CellValuePushed += ImageGrid_CellValuePushed;
            imageGrid.CellDoubleClick += (_, __) => JumpToSelectedImage();

            summaryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            summaryPanel.Controls.Add(summaryGroupLabel, 0, 0);
            summaryPanel.Controls.Add(summaryGrid, 0, 1);

            gridHostLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            gridHostLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            gridHostLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            gridHostLayout.Controls.Add(summaryPanel, 0, 0);
            gridHostLayout.Controls.Add(imageGrid, 0, 1);
            summaryPanel.Visible = false;

            var batchMoveStatusPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2
            };
            batchMoveStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
            batchMoveStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            batchMoveProgressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 1,
                Value = 0,
                Margin = new Padding(0, 4, 12, 4)
            };
            batchMoveStatusPanel.Controls.Add(batchMoveProgressBar, 0, 0);

            batchMoveStatusLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            batchMoveStatusPanel.Controls.Add(batchMoveStatusLabel, 1, 0);

            var actionPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7
            };
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));

            var jumpButton = new Button
            {
                Text = "選択画像を表示",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            jumpButton.Click += (_, __) => JumpToSelectedImage();
            actionPanel.Controls.Add(jumpButton, 0, 0);

            var checkAllButton = new Button
            {
                Text = "全部チェック",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            checkAllButton.Click += async (_, __) => await ApplyCheckStateAsync(CheckApplyMode.AllTargets, true);
            actionPanel.Controls.Add(checkAllButton, 1, 0);

            var checkVisibleButton = new Button
            {
                Text = "表示中を全部チェック",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            checkVisibleButton.Click += async (_, __) => await ApplyCheckStateAsync(CheckApplyMode.VisibleOnly, true);
            actionPanel.Controls.Add(checkVisibleButton, 2, 0);

            var clearCheckButton = new Button
            {
                Text = "全解除",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            clearCheckButton.Click += (_, __) =>
            {
                checkedPaths.Clear();
                summaryGrid.Invalidate();
                imageGrid.InvalidateColumn(imageGrid.Columns["checked"].Index);
                UpdateSummaryLabel();
            };
            actionPanel.Controls.Add(clearCheckButton, 3, 0);

            var batchMoveButton = new Button
            {
                Text = "チェック画像を移動",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            batchMoveButton.Click += BatchMoveButton_Click;
            actionPanel.Controls.Add(batchMoveButton, 4, 0);

            summaryLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            actionPanel.Controls.Add(summaryLabel, 5, 0);

            var closeButton = new Button
            {
                Text = "閉じる",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            closeButton.Click += (_, __) => Close();
            actionPanel.Controls.Add(closeButton, 6, 0);

            rootLayout.RowCount = 4;
            rootLayout.RowStyles.Clear();
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            rootLayout.Controls.Add(filterPanel, 0, 0);
            rootLayout.Controls.Add(gridHostLayout, 0, 1);
            rootLayout.Controls.Add(batchMoveStatusPanel, 0, 2);
            rootLayout.Controls.Add(actionPanel, 0, 3);
            Controls.Add(rootLayout);

            batchMoveBusyControls.Add(filterTextBox);
            batchMoveBusyControls.Add(applyFilterButton);
            batchMoveBusyControls.Add(clearFilterButton);
            batchMoveBusyControls.Add(refreshButton);
            batchMoveBusyControls.Add(summaryGrid);
            batchMoveBusyControls.Add(imageGrid);
            batchMoveBusyControls.Add(jumpButton);
            batchMoveBusyControls.Add(checkAllButton);
            batchMoveBusyControls.Add(checkVisibleButton);
            batchMoveBusyControls.Add(clearCheckButton);
            batchMoveBusyControls.Add(batchMoveButton);
            batchMoveBusyControls.Add(closeButton);

            ResetBatchMoveProgress();
        }

        internal void RefreshItems()
        {
            if (closeRequested || IsDisposed)
            {
                return;
            }

            string selectedPath = GetSelectedPath();
            currentSnapshot = ownerMain.GetImageBrowserSnapshot();
            currentPath = currentSnapshot.CurrentPath ?? string.Empty;
            BeginApplyFilterAsync((filterTextBox.Text ?? string.Empty).Trim(), selectedPath);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            closeRequested = true;
            filterInProgress = false;
            checkApplyInProgress = false;
            batchMoveInProgress = false;
            CancelPendingFilter();
            PrepareGridForDispose(summaryGrid);
            PrepareGridForDispose(imageGrid);
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            CancelPendingFilter();
            base.OnFormClosed(e);
        }

        internal void UpdateCurrentPath(string newCurrentPath)
        {
            if (closeRequested || IsDisposed)
            {
                return;
            }

            currentPath = newCurrentPath ?? string.Empty;
            imageGrid.InvalidateColumn(imageGrid.Columns["current"].Index);
        }

        internal bool IsFilterInProgressForTest()
        {
            return filterInProgress;
        }

        internal int VisibleRowCountForTest()
        {
            return visibleRowCount;
        }

        internal int SummaryGroupCountForTest()
        {
            return summaryGroups.Length;
        }

        internal int CheckedPathCountForTest()
        {
            return checkedPaths.Count;
        }

        internal void StartImmediateFilterForTest(string filterText)
        {
            filterTextBox.Text = filterText ?? string.Empty;
            BeginApplyFilterAsync(filterTextBox.Text.Trim(), GetSelectedPath());
        }

        internal void SetSummaryGroupCheckedForTest(int groupIndex, bool isChecked)
        {
            ApplySummaryGroupCheckState(GetSummaryGroup(groupIndex), groupIndex, isChecked);
        }

        private void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateSummaryLabel();
        }

        private void BeginApplyFilterAsync(string filterText, string preferredSelectedPath)
        {
            CancelPendingFilter();
            if (closeRequested || IsDisposed)
            {
                return;
            }

            filterCts = new CancellationTokenSource();
            CancellationToken token = filterCts.Token;
            ImageBrowserSnapshot snapshot = currentSnapshot;
            FilterSeed filterSeed = CreateFilterSeed(snapshot, filterText);

            filterInProgress = true;
            UpdateUiBusyState();
            UpdateSummaryLabel();

            Task.Run(() => BuildFilteredRows(snapshot, filterText, filterSeed, token), token)
                .ContinueWith(
                    task =>
                    {
                        if (!CanUpdateUi() || token.IsCancellationRequested)
                        {
                            return;
                        }

                        filterInProgress = false;
                        UpdateUiBusyState();

                        if (task.IsCanceled)
                        {
                            UpdateSummaryLabel();
                            return;
                        }

                        if (!CanUpdateUi())
                        {
                            return;
                        }

                        if (task.IsFaulted)
                        {
                            UpdateSummaryLabel();
                            ShowOwnedMessage("画像一覧の絞り込み中にエラーが発生しました。", "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        FilterResult result = task.Result;
                        appliedFilterText = filterText;
                        appliedFilterSnapshotPaths = snapshot.FullPaths;
                        showAllRows = result.ShowAllRows;
                        summaryGroups = result.SummaryGroups;
                        filteredIndices = result.Indices;
                        visibleRowCount = result.RowCount;
                        visibleStartIndex = result.StartIndex;
                        matchedRowCount = result.MatchedCount;
                        truncationMessage = result.TruncationMessage;
                        imageGrid.SuspendLayout();
                        try
                        {
                            UpdateSummaryGrid();
                            imageGrid.RowCount = visibleRowCount;
                            UpdateSummaryLabel();
                            RestoreSelectedRow(preferredSelectedPath);
                        }
                        finally
                        {
                            imageGrid.ResumeLayout();
                        }

                        imageGrid.Invalidate();
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ApplyCurrentFilter()
        {
            pendingPreferredSelectedPath = GetSelectedPath() ?? currentPath;
            BeginApplyFilterAsync((filterTextBox.Text ?? string.Empty).Trim(), pendingPreferredSelectedPath);
        }

        private FilterSeed CreateFilterSeed(ImageBrowserSnapshot snapshot, string filterText)
        {
            if (snapshot == null || snapshot.FullPaths.Length == 0 || string.IsNullOrWhiteSpace(filterText))
            {
                return FilterSeed.Empty;
            }

            if (showAllRows ||
                string.IsNullOrWhiteSpace(appliedFilterText) ||
                !ReferenceEquals(snapshot.FullPaths, appliedFilterSnapshotPaths))
            {
                return FilterSeed.Empty;
            }

            if (!filterText.StartsWith(appliedFilterText, StringComparison.OrdinalIgnoreCase))
            {
                return FilterSeed.Empty;
            }

            return new FilterSeed(appliedFilterText, filteredIndices, true);
        }

        private static FilterResult BuildFilteredRows(ImageBrowserSnapshot snapshot, string filterText, FilterSeed filterSeed, CancellationToken token)
        {
            string[] paths = snapshot.FullPaths;
            if (paths.Length == 0)
            {
                return FilterResult.Empty;
            }

            if (string.IsNullOrWhiteSpace(filterText))
            {
                int visibleCount = Math.Min(paths.Length, MaxVisibleRows);
                int startIndex = 0;
                string truncationMessage = string.Empty;
                if (paths.Length > MaxVisibleRows)
                {
                    int preferredIndex = snapshot.CurrentIndex >= 0 ? snapshot.CurrentIndex : 0;
                    startIndex = Math.Max(0, Math.Min(preferredIndex - (MaxVisibleRows / 2), paths.Length - MaxVisibleRows));
                    truncationMessage = $"件数が多いため現在位置付近 {visibleCount:N0} 件だけ表示しています。";
                }

                SequenceSummaryGroup[] summaryGroups = BuildSequenceSummaryGroups(snapshot, null);
                return FilterResult.AllRows(visibleCount, startIndex, paths.Length, truncationMessage, summaryGroups);
            }

            int[] matchedIndices = CollectMatchedIndices(snapshot, filterText, filterSeed, token);
            int filteredVisibleCount = Math.Min(matchedIndices.Length, MaxVisibleRows);

            string filteredTruncationMessage = matchedIndices.Length > MaxVisibleRows
                ? $"一致件数が多いため先頭 {MaxVisibleRows:N0} 件だけ表示しています。"
                : string.Empty;

            SequenceSummaryGroup[] filteredSummaryGroups = BuildSequenceSummaryGroups(snapshot, matchedIndices);
            return FilterResult.Filtered(matchedIndices, filteredVisibleCount, matchedIndices.Length, filteredTruncationMessage, filteredSummaryGroups);
        }

        private static SequenceSummaryGroup[] BuildSequenceSummaryGroups(ImageBrowserSnapshot snapshot, int[] candidateIndices)
        {
            int candidateCount = candidateIndices != null ? candidateIndices.Length : snapshot.FullPaths.Length;
            if (snapshot == null || snapshot.FullPaths.Length == 0 || candidateCount < 2)
            {
                return Array.Empty<SequenceSummaryGroup>();
            }

            var groupsByPrefix = new Dictionary<string, SequenceSummaryGroupAccumulator>(StringComparer.OrdinalIgnoreCase);
            if (candidateIndices == null)
            {
                for (int index = 0; index < snapshot.FullPaths.Length; index++)
                {
                    AddSequenceSummaryCandidate(snapshot, index, groupsByPrefix);
                }
            }
            else
            {
                for (int index = 0; index < candidateIndices.Length; index++)
                {
                    AddSequenceSummaryCandidate(snapshot, candidateIndices[index], groupsByPrefix);
                }
            }

            int groupCount = 0;
            foreach (SequenceSummaryGroupAccumulator accumulator in groupsByPrefix.Values)
            {
                if (accumulator.Count > 1)
                {
                    groupCount++;
                }
            }

            if (groupCount == 0)
            {
                return Array.Empty<SequenceSummaryGroup>();
            }

            var groups = new SequenceSummaryGroup[groupCount];
            int writeIndex = 0;
            foreach (SequenceSummaryGroupAccumulator accumulator in groupsByPrefix.Values)
            {
                if (accumulator.Count <= 1)
                {
                    continue;
                }

                groups[writeIndex++] = accumulator.ToSummaryGroup(snapshot);
            }

            Array.Sort(groups, (left, right) => left.FirstIndex.CompareTo(right.FirstIndex));
            return groups;
        }

        private static void AddSequenceSummaryCandidate(
            ImageBrowserSnapshot snapshot,
            int masterIndex,
            Dictionary<string, SequenceSummaryGroupAccumulator> groupsByPrefix)
        {
            if (masterIndex < 0 || masterIndex >= snapshot.FullPaths.Length)
            {
                return;
            }

            string groupPrefix = snapshot.GetSequenceGroupPrefix(masterIndex);
            if (string.IsNullOrEmpty(groupPrefix))
            {
                return;
            }

            if (!groupsByPrefix.TryGetValue(groupPrefix, out SequenceSummaryGroupAccumulator accumulator))
            {
                accumulator = new SequenceSummaryGroupAccumulator(
                    groupPrefix,
                    BuildSequenceSummaryDisplayText(groupPrefix),
                    snapshot.FileNames[masterIndex],
                    masterIndex);
                groupsByPrefix.Add(groupPrefix, accumulator);
                return;
            }

            accumulator.Add(masterIndex);
        }

        private static string BuildSequenceSummaryDisplayText(string rawPrefix)
        {
            string displayText = (rawPrefix ?? string.Empty).TrimEnd(' ', '_', '-', '.');
            return string.IsNullOrWhiteSpace(displayText) ? (rawPrefix ?? string.Empty) : displayText;
        }

        private static int[] CollectMatchedIndices(ImageBrowserSnapshot snapshot, string filterText, FilterSeed filterSeed, CancellationToken token)
        {
            int[] candidateIndices = filterSeed != null && filterSeed.CanReuseFor(filterText)
                ? filterSeed.CandidateIndices
                : null;
            int candidateCount = candidateIndices != null ? candidateIndices.Length : snapshot.FullPaths.Length;
            if (candidateCount < ParallelFilterThreshold)
            {
                return CollectMatchedIndicesSequential(snapshot, filterText, candidateIndices, token);
            }

            return CollectMatchedIndicesParallel(snapshot, filterText, candidateIndices, token);
        }

        private static int[] CollectMatchedIndicesSequential(ImageBrowserSnapshot snapshot, string filterText, int[] candidateIndices, CancellationToken token)
        {
            int initialCapacity = candidateIndices != null ? Math.Min(candidateIndices.Length, 4096) : 256;
            var matches = new List<int>(initialCapacity);
            if (candidateIndices == null)
            {
                for (int index = 0; index < snapshot.FullPaths.Length; index++)
                {
                    token.ThrowIfCancellationRequested();
                    if (IsPathMatch(snapshot, filterText, index))
                    {
                        matches.Add(index);
                    }
                }

                return matches.ToArray();
            }

            for (int index = 0; index < candidateIndices.Length; index++)
            {
                token.ThrowIfCancellationRequested();
                int masterIndex = candidateIndices[index];
                if (IsPathMatch(snapshot, filterText, masterIndex))
                {
                    matches.Add(masterIndex);
                }
            }

            return matches.ToArray();
        }

        private static int[] CollectMatchedIndicesParallel(ImageBrowserSnapshot snapshot, string filterText, int[] candidateIndices, CancellationToken token)
        {
            var partitionResults = new ConcurrentBag<MatchedChunk>();
            int candidateCount = candidateIndices != null ? candidateIndices.Length : snapshot.FullPaths.Length;
            int rangeSize = Math.Max(2048, candidateCount / Math.Max(Environment.ProcessorCount * 8, 1));
            var partitions = Partitioner.Create(0, candidateCount, rangeSize);
            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(
                partitions,
                options,
                range =>
                {
                    int localCapacity = Math.Min(range.Item2 - range.Item1, 2048);
                    var localMatches = new List<int>(localCapacity);
                    for (int index = range.Item1; index < range.Item2; index++)
                    {
                        token.ThrowIfCancellationRequested();
                        int masterIndex = candidateIndices != null ? candidateIndices[index] : index;
                        if (IsPathMatch(snapshot, filterText, masterIndex))
                        {
                            localMatches.Add(masterIndex);
                        }
                    }

                    partitionResults.Add(new MatchedChunk(range.Item1, localMatches.ToArray()));
                });

            MatchedChunk[] orderedChunks = partitionResults.ToArray();
            Array.Sort(orderedChunks, (left, right) => left.StartIndex.CompareTo(right.StartIndex));

            int totalMatchCount = 0;
            foreach (MatchedChunk chunk in orderedChunks)
            {
                totalMatchCount += chunk.Indices.Length;
            }

            var matches = new int[totalMatchCount];
            int offset = 0;
            foreach (MatchedChunk chunk in orderedChunks)
            {
                Array.Copy(chunk.Indices, 0, matches, offset, chunk.Indices.Length);
                offset += chunk.Indices.Length;
            }

            return matches;
        }

        private static bool IsPathMatch(ImageBrowserSnapshot snapshot, string filterText, int index)
        {
            string fileName = snapshot.FileNames[index];
            if (!string.IsNullOrEmpty(fileName) &&
                fileName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string path = snapshot.FullPaths[index];
            return path.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RestoreSelectedRow(string preferredSelectedPath)
        {
            string targetPath = preferredSelectedPath;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                targetPath = currentPath;
            }

            if (string.IsNullOrWhiteSpace(targetPath) || visibleRowCount == 0)
            {
                return;
            }

            int directRowIndex = TryResolveDirectRowIndex(targetPath);
            if (directRowIndex >= 0)
            {
                SelectRow(directRowIndex);
            }
        }

        private string GetSelectedPath()
        {
            if (imageGrid.CurrentRow == null)
            {
                return null;
            }

            int masterIndex = GetMasterIndex(imageGrid.CurrentRow.Index);
            if (masterIndex < 0)
            {
                return null;
            }

            return currentSnapshot.FullPaths[masterIndex];
        }

        private void JumpToSelectedImage()
        {
            string targetPath = GetSelectedPath();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                ShowOwnedMessage("表示したい画像を選択してください。", "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ownerMain.ShowImageByPath(targetPath))
            {
                ShowOwnedMessage("選択した画像は現在の一覧にありません。", "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UpdateCurrentPath(targetPath);
        }

        private void BatchMoveButton_Click(object sender, EventArgs e)
        {
            HashSet<string> currentPaths = new HashSet<string>(currentSnapshot.FullPaths, StringComparer.OrdinalIgnoreCase);
            List<string> selectedPaths = checkedPaths
                .Where(currentPaths.Contains)
                .ToList();

            if (selectedPaths.Count == 0)
            {
                ShowOwnedMessage("一括移動したい画像にチェックを付けてください。", "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new BatchMoveTargetDialog(ownerMain.GetDestinationChoices()))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    batchMoveInProgress = true;
                    UpdateUiBusyState();
                    UpdateBatchMoveProgress(new BatchMoveProgressInfo(selectedPaths.Count, 0, 0, 0, "一括移動を開始します。", string.Empty));
                    Application.DoEvents();

                    BatchMoveExecutionResult result = ownerMain.MoveImagesFromBrowser(
                        selectedPaths,
                        dialog.SelectedFolderPath,
                        dialog.SelectedLabel,
                        UpdateBatchMoveProgress);

                    foreach (string movedPath in result.MovedSourcePaths)
                    {
                        checkedPaths.Remove(movedPath);
                    }

                    RefreshItems();

                    UpdateBatchMoveProgress(new BatchMoveProgressInfo(
                        selectedPaths.Count,
                        selectedPaths.Count,
                        result.MovedCount,
                        result.SkippedMessages.Count,
                        $"一括移動が完了しました。 移動 {result.MovedCount:N0} 件 / スキップ {result.SkippedMessages.Count:N0} 件",
                        string.Empty));

                    string message = $"移動完了: {result.MovedCount} 件";
                    if (result.SkippedMessages.Count > 0)
                    {
                        message += Environment.NewLine + string.Join(Environment.NewLine, result.SkippedMessages.Take(5));
                    }

                    ShowOwnedMessage(message, "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                finally
                {
                    batchMoveInProgress = false;
                    ResetBatchMoveProgress();
                    UpdateUiBusyState();
                }
            }
        }

        private void UpdateSummaryLabel()
        {
            if (!CanUpdateUi())
            {
                return;
            }

            string state = string.Empty;
            if (batchMoveInProgress)
            {
                state = " / 一括移動中";
            }
            else if (checkApplyInProgress)
            {
                state = " / チェック更新中";
            }
            else if (filterInProgress)
            {
                state = " / 絞り込み中";
            }
            else if (HasPendingFilterChange())
            {
                state = " / 条件未適用";
            }

            string suffix = string.IsNullOrWhiteSpace(truncationMessage) ? string.Empty : $" / {truncationMessage}";
            summaryLabel.Text = $"表示 {visibleRowCount:N0} 件 / 対象 {matchedRowCount:N0} 件 / 全体 {currentSnapshot.FullPaths.Length:N0} 件 / サマリ {summaryGroups.Length:N0} グループ / チェック {checkedPaths.Count:N0} 件{state}{suffix}";
        }

        private void UpdateSummaryGrid()
        {
            if (!CanUpdateUi())
            {
                return;
            }

            summaryGroupLabel.Text = $"連番サマリ {summaryGroups.Length:N0} グループ";
            bool hasGroups = summaryGroups.Length > 0;
            summaryGrid.SuspendLayout();
            try
            {
                summaryGrid.RowCount = summaryGroups.Length;
                summaryPanel.Visible = hasGroups;
                gridHostLayout.RowStyles[0].Height = hasGroups ? 180F : 0F;
            }
            finally
            {
                summaryGrid.ResumeLayout();
            }

            summaryGrid.Invalidate();
        }

        private bool HasPendingFilterChange()
        {
            string currentFilterText = (filterTextBox.Text ?? string.Empty).Trim();
            return !string.Equals(currentFilterText, appliedFilterText ?? string.Empty, StringComparison.Ordinal);
        }

        private void UpdateUiBusyState()
        {
            if (!CanUpdateUi())
            {
                return;
            }

            bool isBusy = batchMoveInProgress || filterInProgress || checkApplyInProgress;
            UseWaitCursor = isBusy;

            foreach (Control control in batchMoveBusyControls)
            {
                control.Enabled = !isBusy;
            }

            batchMoveProgressBar.Refresh();
            batchMoveStatusLabel.Refresh();
            summaryLabel.Refresh();
        }

        private void ResetBatchMoveProgress()
        {
            if (batchMoveProgressBar.IsDisposed || batchMoveStatusLabel.IsDisposed)
            {
                return;
            }

            batchMoveProgressBar.Minimum = 0;
            batchMoveProgressBar.Maximum = 1;
            batchMoveProgressBar.Value = 0;
            batchMoveStatusLabel.Text = "一括移動待機中";
        }

        private void UpdateBatchMoveProgress(BatchMoveProgressInfo progress)
        {
            if (progress == null || !CanUpdateUi())
            {
                return;
            }

            int total = Math.Max(progress.TotalCount, 1);
            int processed = Math.Max(0, Math.Min(progress.ProcessedCount, total));
            batchMoveProgressBar.Minimum = 0;
            batchMoveProgressBar.Maximum = total;
            batchMoveProgressBar.Value = processed;

            string currentFile = string.IsNullOrWhiteSpace(progress.CurrentFileName)
                ? string.Empty
                : $" / {progress.CurrentFileName}";
            batchMoveStatusLabel.Text =
                $"{processed:N0}/{progress.TotalCount:N0} 件処理済み  移動 {progress.MovedCount:N0} 件  スキップ {progress.SkippedCount:N0} 件  {progress.StatusText}{currentFile}".Trim();

            batchMoveProgressBar.Refresh();
            batchMoveStatusLabel.Refresh();

            if (batchMoveInProgress)
            {
                Application.DoEvents();
            }
        }

        private async Task ApplyCheckStateAsync(CheckApplyMode mode, bool isChecked)
        {
            try
            {
                checkApplyInProgress = true;
                UpdateUiBusyState();
                UpdateSummaryLabel();
                string filterText = (filterTextBox.Text ?? string.Empty).Trim();
                ImageBrowserSnapshot snapshot = currentSnapshot;
                string[] targetPaths = await Task.Run(() => CollectTargetPaths(snapshot, filterText, mode));
                if (!CanUpdateUi())
                {
                    return;
                }

                if (isChecked)
                {
                    checkedPaths.UnionWith(targetPaths);
                }
                else
                {
                    foreach (string path in targetPaths)
                    {
                        checkedPaths.Remove(path);
                    }
                }

                summaryGrid.Invalidate();
                imageGrid.InvalidateColumn(imageGrid.Columns["checked"].Index);
                UpdateSummaryLabel();
            }
            finally
            {
                checkApplyInProgress = false;
                UpdateUiBusyState();
                UpdateSummaryLabel();
            }
        }

        private string[] CollectTargetPaths(ImageBrowserSnapshot snapshot, string filterText, CheckApplyMode mode)
        {
            if (snapshot.FullPaths.Length == 0)
            {
                return Array.Empty<string>();
            }

            if (mode == CheckApplyMode.VisibleOnly)
            {
                int count = visibleRowCount;
                string[] paths = new string[count];
                int written = 0;
                for (int rowIndex = 0; rowIndex < count; rowIndex++)
                {
                    int masterIndex = GetMasterIndex(rowIndex);
                    if (masterIndex < 0)
                    {
                        continue;
                    }

                    string path = snapshot.FullPaths[masterIndex];
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    paths[written++] = path;
                }

                if (written == count)
                {
                    return paths;
                }

                if (written == 0)
                {
                    return Array.Empty<string>();
                }

                var compactedPaths = new string[written];
                Array.Copy(paths, compactedPaths, written);
                return compactedPaths;
            }

            if (string.IsNullOrWhiteSpace(filterText))
            {
                return snapshot.FullPaths;
            }

            if (!filterInProgress && string.Equals(appliedFilterText, filterText, StringComparison.Ordinal))
            {
                if (matchedRowCount == 0)
                {
                    return Array.Empty<string>();
                }

                int count = filteredIndices.Length;
                var filteredPaths = new string[count];
                for (int index = 0; index < count; index++)
                {
                    filteredPaths[index] = snapshot.FullPaths[filteredIndices[index]];
                }

                return filteredPaths;
            }

            int[] matchedIndices = CollectMatchedIndices(snapshot, filterText, FilterSeed.Empty, CancellationToken.None);
            var matchedPaths = new string[matchedIndices.Length];
            for (int index = 0; index < matchedIndices.Length; index++)
            {
                matchedPaths[index] = snapshot.FullPaths[matchedIndices[index]];
            }

            return matchedPaths;
        }

        private void ImageGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (imageGrid.IsCurrentCellDirty)
            {
                imageGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void SummaryGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (summaryGrid.IsCurrentCellDirty)
            {
                summaryGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void SummaryGrid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            SequenceSummaryGroup group = GetSummaryGroup(e.RowIndex);
            if (group == null)
            {
                return;
            }

            string columnName = summaryGrid.Columns[e.ColumnIndex].Name;
            switch (columnName)
            {
                case "checked":
                    e.Value = AreAllSummaryGroupPathsChecked(group);
                    break;
                case "count":
                    e.Value = group.ItemCount;
                    break;
                case "checkedCount":
                    e.Value = GetCheckedCountForSummaryGroup(group);
                    break;
                case "groupName":
                    e.Value = group.DisplayText;
                    break;
                case "sampleFileName":
                    e.Value = group.SampleFileName;
                    break;
                case "sampleRelativePath":
                    e.Value = group.SampleRelativePath;
                    break;
            }
        }

        private void SummaryGrid_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.ColumnIndex != summaryGrid.Columns["checked"].Index)
            {
                return;
            }

            SequenceSummaryGroup group = GetSummaryGroup(e.RowIndex);
            if (group == null)
            {
                return;
            }

            bool isChecked = e.Value != null && Convert.ToBoolean(e.Value);
            ApplySummaryGroupCheckState(group, e.RowIndex, isChecked);
        }

        private void ApplySummaryGroupCheckState(SequenceSummaryGroup group, int rowIndex, bool isChecked)
        {
            if (group == null)
            {
                return;
            }

            foreach (int masterIndex in group.MasterIndices)
            {
                string path = currentSnapshot.FullPaths[masterIndex];
                if (isChecked)
                {
                    checkedPaths.Add(path);
                }
                else
                {
                    checkedPaths.Remove(path);
                }
            }

            if (rowIndex >= 0 && rowIndex < summaryGrid.RowCount)
            {
                summaryGrid.InvalidateRow(rowIndex);
            }

            imageGrid.InvalidateColumn(imageGrid.Columns["checked"].Index);
            UpdateSummaryLabel();
        }

        private void ImageGrid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            int masterIndex = GetMasterIndex(e.RowIndex);
            if (masterIndex < 0)
            {
                return;
            }

            string path = currentSnapshot.FullPaths[masterIndex];
            string columnName = imageGrid.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "checked":
                    e.Value = checkedPaths.Contains(path);
                    break;
                case "current":
                    e.Value = string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase) ? "●" : string.Empty;
                    break;
                case "fileName":
                    e.Value = currentSnapshot.FileNames[masterIndex];
                    break;
                case "relativePath":
                    e.Value = currentSnapshot.GetRelativePath(masterIndex);
                    break;
                case "fullPath":
                    e.Value = path;
                    break;
            }
        }

        private void ImageGrid_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.ColumnIndex != imageGrid.Columns["checked"].Index)
            {
                return;
            }

            int masterIndex = GetMasterIndex(e.RowIndex);
            if (masterIndex < 0)
            {
                return;
            }

            string path = currentSnapshot.FullPaths[masterIndex];
            bool isChecked = e.Value != null && Convert.ToBoolean(e.Value);
            if (isChecked)
            {
                checkedPaths.Add(path);
            }
            else
            {
                checkedPaths.Remove(path);
            }

            summaryGrid.Invalidate();
            UpdateSummaryLabel();
        }

        private SequenceSummaryGroup GetSummaryGroup(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= summaryGroups.Length)
            {
                return null;
            }

            return summaryGroups[rowIndex];
        }

        private bool AreAllSummaryGroupPathsChecked(SequenceSummaryGroup group)
        {
            return group != null &&
                group.ItemCount > 0 &&
                GetCheckedCountForSummaryGroup(group) == group.ItemCount;
        }

        private int GetCheckedCountForSummaryGroup(SequenceSummaryGroup group)
        {
            if (group == null || group.ItemCount == 0)
            {
                return 0;
            }

            int checkedCount = 0;
            foreach (int masterIndex in group.MasterIndices)
            {
                string path = currentSnapshot.FullPaths[masterIndex];
                if (checkedPaths.Contains(path))
                {
                    checkedCount++;
                }
            }

            return checkedCount;
        }

        private int TryResolveDirectRowIndex(string targetPath)
        {
            if (!currentSnapshot.TryGetAbsoluteIndex(targetPath, out int absoluteIndex))
            {
                return -1;
            }

            if (showAllRows)
            {
                if (absoluteIndex < visibleStartIndex || absoluteIndex >= visibleStartIndex + visibleRowCount)
                {
                    return -1;
                }

                return absoluteIndex - visibleStartIndex;
            }

            int filteredRowIndex = Array.BinarySearch(filteredIndices, absoluteIndex);
            if (filteredRowIndex < 0 || filteredRowIndex >= visibleRowCount)
            {
                return -1;
            }

            return filteredRowIndex;
        }

        private int GetMasterIndex(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= visibleRowCount)
            {
                return -1;
            }

            return showAllRows ? visibleStartIndex + rowIndex : filteredIndices[rowIndex];
        }

        private void SelectRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= imageGrid.RowCount)
            {
                return;
            }

            imageGrid.ClearSelection();
            imageGrid.Rows[rowIndex].Selected = true;
            if (imageGrid.Columns.Count > 1)
            {
                imageGrid.CurrentCell = imageGrid.Rows[rowIndex].Cells[1];
            }
        }

        private void ShowOwnedMessage(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (closeRequested)
            {
                return;
            }

            IWin32Window ownerWindow;
            if (ownerMain != null && !ownerMain.IsDisposed)
            {
                ownerWindow = ownerMain;
            }
            else
            {
                ownerWindow = this;
            }

            TopMostMessageBox.Show(ownerWindow, text, caption, buttons, icon);
        }

        private void CancelPendingFilter()
        {
            CancellationTokenSource previous = Interlocked.Exchange(ref filterCts, null);
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

            previous.Dispose();
        }

        private bool CanUpdateUi()
        {
            return !closeRequested &&
                !IsDisposed &&
                !Disposing &&
                summaryGrid != null &&
                !summaryGrid.IsDisposed &&
                imageGrid != null &&
                !imageGrid.IsDisposed;
        }

        private static void PrepareGridForDispose(DataGridView grid)
        {
            if (grid == null || grid.IsDisposed)
            {
                return;
            }

            try
            {
                grid.CancelEdit();
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                grid.CurrentCell = null;
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                grid.RowCount = 0;
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
    }

    internal sealed class BatchMoveTargetDialog : Form
    {
        private readonly IReadOnlyList<DestinationChoice> destinationChoices;
        private readonly RadioButton configuredDestinationRadioButton;
        private readonly ComboBox configuredDestinationComboBox;
        private readonly RadioButton customDestinationRadioButton;
        private readonly TextBox customFolderTextBox;
        private readonly Button customBrowseButton;

        internal BatchMoveTargetDialog(IReadOnlyList<DestinationChoice> destinationChoices)
        {
            this.destinationChoices = destinationChoices ?? Array.Empty<DestinationChoice>();

            Text = "一括移動先の選択";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 220);
            MinimumSize = new Size(620, 220);
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 5,
                Padding = new Padding(12)
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            rootLayout.Controls.Add(new Label
            {
                Text = "移動方法",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            configuredDestinationRadioButton = new RadioButton
            {
                Text = "親画面の移動先から選ぶ",
                Dock = DockStyle.Fill
            };
            configuredDestinationRadioButton.CheckedChanged += (_, __) => UpdateInputState();
            rootLayout.Controls.Add(configuredDestinationRadioButton, 1, 0);
            rootLayout.SetColumnSpan(configuredDestinationRadioButton, 2);

            configuredDestinationComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            configuredDestinationComboBox.Items.AddRange(this.destinationChoices.Cast<object>().ToArray());
            configuredDestinationComboBox.DisplayMember = nameof(DestinationChoice.DisplayName);
            rootLayout.Controls.Add(configuredDestinationComboBox, 1, 1);
            rootLayout.SetColumnSpan(configuredDestinationComboBox, 2);

            customDestinationRadioButton = new RadioButton
            {
                Text = "任意フォルダを使う",
                Dock = DockStyle.Fill
            };
            customDestinationRadioButton.CheckedChanged += (_, __) => UpdateInputState();
            rootLayout.Controls.Add(customDestinationRadioButton, 1, 2);
            rootLayout.SetColumnSpan(customDestinationRadioButton, 2);

            customFolderTextBox = new TextBox
            {
                Dock = DockStyle.Fill
            };
            rootLayout.Controls.Add(customFolderTextBox, 1, 3);

            customBrowseButton = new Button
            {
                Text = "参照",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            customBrowseButton.Click += CustomBrowseButton_Click;
            rootLayout.Controls.Add(customBrowseButton, 2, 3);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var okButton = new Button
            {
                Text = "実行",
                DialogResult = DialogResult.None,
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
            okButton.Click += OkButton_Click;
            var cancelButton = new Button
            {
                Text = "キャンセル",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
            buttonsPanel.Controls.Add(okButton);
            buttonsPanel.Controls.Add(cancelButton);
            rootLayout.Controls.Add(buttonsPanel, 0, 4);
            rootLayout.SetColumnSpan(buttonsPanel, 3);

            Controls.Add(rootLayout);

            if (this.destinationChoices.Count > 0)
            {
                configuredDestinationRadioButton.Checked = true;
                configuredDestinationComboBox.SelectedIndex = 0;
            }
            else
            {
                customDestinationRadioButton.Checked = true;
            }

            UpdateInputState();
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        internal string SelectedFolderPath { get; private set; }

        internal string SelectedLabel { get; private set; }

        private void UpdateInputState()
        {
            bool useConfigured = configuredDestinationRadioButton.Checked;
            configuredDestinationComboBox.Enabled = useConfigured;
            customFolderTextBox.Enabled = !useConfigured;
            customBrowseButton.Enabled = !useConfigured;
        }

        private void CustomBrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderPickerDialog())
            {
                dialog.Title = "一括移動先フォルダを選択";
                dialog.InitialFolder = Directory.Exists(customFolderTextBox.Text) ? customFolderTextBox.Text : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (dialog.ShowDialog(Handle))
                {
                    customFolderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (configuredDestinationRadioButton.Checked)
            {
                var choice = configuredDestinationComboBox.SelectedItem as DestinationChoice;
                if (choice == null)
                {
                    TopMostMessageBox.Show(this, "親画面で設定済みの移動先を選択してください。", "一括移動", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SelectedFolderPath = choice.FolderPath;
                SelectedLabel = choice.DisplayName;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            string customPath = (customFolderTextBox.Text ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(customPath) || !Directory.Exists(customPath))
            {
                TopMostMessageBox.Show(this, "任意フォルダを指定してください。", "一括移動", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectedFolderPath = customPath;
            SelectedLabel = "任意フォルダ";
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class ImageBrowserSnapshot
    {
        internal static readonly ImageBrowserSnapshot Empty = new ImageBrowserSnapshot(Array.Empty<string>(), string.Empty, string.Empty, -1);
        private readonly Dictionary<string, int> pathToIndexMap;
        private readonly string[] relativePathCache;
        private readonly string[] sequenceGroupPrefixCache;

        internal ImageBrowserSnapshot(string[] fullPaths, string sourceRoot, string currentPath, int currentIndex)
            : this(fullPaths, null, null, sourceRoot, currentPath, currentIndex)
        {
        }

        internal ImageBrowserSnapshot(
            string[] fullPaths,
            string[] fileNames,
            Dictionary<string, int> existingPathToIndexMap,
            string sourceRoot,
            string currentPath,
            int currentIndex)
        {
            FullPaths = fullPaths ?? Array.Empty<string>();
            SourceRoot = sourceRoot ?? string.Empty;
            CurrentPath = currentPath ?? string.Empty;
            CurrentIndex = currentIndex;
            FileNames = fileNames != null && fileNames.Length == FullPaths.Length
                ? fileNames
                : BuildFileNames(FullPaths);
            relativePathCache = new string[FullPaths.Length];
            sequenceGroupPrefixCache = new string[FullPaths.Length];
            pathToIndexMap = existingPathToIndexMap != null && existingPathToIndexMap.Count > 0
                ? existingPathToIndexMap
                : BuildPathToIndexMap(FullPaths);
        }

        internal string[] FullPaths { get; }

        internal string[] FileNames { get; }

        internal string SourceRoot { get; }

        internal string CurrentPath { get; }

        internal int CurrentIndex { get; }

        internal string GetRelativePath(int index)
        {
            if (index < 0 || index >= FullPaths.Length)
            {
                return string.Empty;
            }

            string cachedPath = relativePathCache[index];
            if (!string.IsNullOrEmpty(cachedPath))
            {
                return cachedPath;
            }

            string relativePath = BuildRelativePath(SourceRoot, FullPaths[index]);
            relativePathCache[index] = relativePath;
            return relativePath;
        }

        internal string GetSequenceGroupPrefix(int index)
        {
            if (index < 0 || index >= FullPaths.Length)
            {
                return string.Empty;
            }

            string cachedPrefix = sequenceGroupPrefixCache[index];
            if (cachedPrefix != null)
            {
                return cachedPrefix;
            }

            string fileName = FileNames[index];
            if (string.IsNullOrWhiteSpace(fileName))
            {
                sequenceGroupPrefixCache[index] = string.Empty;
                return string.Empty;
            }

            int extensionIndex = fileName.LastIndexOf('.');
            int suffixEnd = extensionIndex > 0 ? extensionIndex : fileName.Length;
            int digitStart = suffixEnd;
            while (digitStart > 0 && char.IsDigit(fileName[digitStart - 1]))
            {
                digitStart--;
            }

            if (digitStart == suffixEnd || digitStart <= 0)
            {
                sequenceGroupPrefixCache[index] = string.Empty;
                return string.Empty;
            }

            string prefix = fileName.Substring(0, digitStart);
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = string.Empty;
            }

            sequenceGroupPrefixCache[index] = prefix;
            return prefix;
        }

        internal bool TryGetAbsoluteIndex(string path, out int index)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                index = -1;
                return false;
            }

            return pathToIndexMap.TryGetValue(path, out index);
        }

        private static string BuildRelativePath(string sourceRoot, string fullPath)
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

        private static string[] BuildFileNames(string[] fullPaths)
        {
            if (fullPaths == null || fullPaths.Length == 0)
            {
                return Array.Empty<string>();
            }

            var fileNames = new string[fullPaths.Length];
            for (int index = 0; index < fullPaths.Length; index++)
            {
                string path = fullPaths[index];
                fileNames[index] = string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFileName(path) ?? string.Empty;
            }

            return fileNames;
        }

        private static Dictionary<string, int> BuildPathToIndexMap(string[] fullPaths)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (fullPaths == null)
            {
                return map;
            }

            for (int index = 0; index < fullPaths.Length; index++)
            {
                string path = fullPaths[index];
                if (string.IsNullOrWhiteSpace(path) || map.ContainsKey(path))
                {
                    continue;
                }

                map[path] = index;
            }

            return map;
        }
    }

    internal sealed class FilterSeed
    {
        internal static readonly FilterSeed Empty = new FilterSeed(string.Empty, null, false);

        internal FilterSeed(string baseFilterText, int[] candidateIndices, bool hasCandidateScope)
        {
            BaseFilterText = baseFilterText ?? string.Empty;
            CandidateIndices = candidateIndices;
            HasCandidateScope = hasCandidateScope;
        }

        internal string BaseFilterText { get; }

        internal int[] CandidateIndices { get; }

        internal bool HasCandidateScope { get; }

        internal bool CanReuseFor(string filterText)
        {
            return HasCandidateScope &&
                !string.IsNullOrWhiteSpace(BaseFilterText) &&
                !string.IsNullOrWhiteSpace(filterText) &&
                filterText.StartsWith(BaseFilterText, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class ImageBrowserItem
    {
        public string FileName { get; set; }

        public string RelativePath { get; set; }

        public string FullPath { get; set; }

        public bool IsCurrent { get; set; }
    }

    internal enum CheckApplyMode
    {
        AllTargets,
        VisibleOnly
    }

    internal sealed class FilterResult
    {
        private FilterResult(
            bool showAllRows,
            int[] indices,
            int rowCount,
            int startIndex,
            int matchedCount,
            string truncationMessage,
            SequenceSummaryGroup[] summaryGroups)
        {
            ShowAllRows = showAllRows;
            Indices = indices ?? Array.Empty<int>();
            RowCount = rowCount;
            StartIndex = startIndex;
            MatchedCount = matchedCount;
            TruncationMessage = truncationMessage ?? string.Empty;
            SummaryGroups = summaryGroups ?? Array.Empty<SequenceSummaryGroup>();
        }

        internal bool ShowAllRows { get; }

        internal int[] Indices { get; }

        internal int RowCount { get; }

        internal int StartIndex { get; }

        internal int MatchedCount { get; }

        internal string TruncationMessage { get; }

        internal SequenceSummaryGroup[] SummaryGroups { get; }

        internal static FilterResult Empty { get; } = new FilterResult(false, Array.Empty<int>(), 0, 0, 0, string.Empty, Array.Empty<SequenceSummaryGroup>());

        internal static FilterResult AllRows(int rowCount, int startIndex, int matchedCount, string truncationMessage, SequenceSummaryGroup[] summaryGroups)
        {
            return new FilterResult(true, Array.Empty<int>(), rowCount, startIndex, matchedCount, truncationMessage, summaryGroups);
        }

        internal static FilterResult Filtered(int[] indices, int rowCount, int matchedCount, string truncationMessage, SequenceSummaryGroup[] summaryGroups)
        {
            return new FilterResult(false, indices, rowCount, 0, matchedCount, truncationMessage, summaryGroups);
        }
    }

    internal sealed class SequenceSummaryGroup
    {
        internal SequenceSummaryGroup(string displayText, string sampleFileName, string sampleRelativePath, int firstIndex, int[] masterIndices)
        {
            DisplayText = displayText ?? string.Empty;
            SampleFileName = sampleFileName ?? string.Empty;
            SampleRelativePath = sampleRelativePath ?? string.Empty;
            FirstIndex = firstIndex;
            MasterIndices = masterIndices ?? Array.Empty<int>();
        }

        internal string DisplayText { get; }

        internal string SampleFileName { get; }

        internal string SampleRelativePath { get; }

        internal int FirstIndex { get; }

        internal int[] MasterIndices { get; }

        internal int ItemCount => MasterIndices.Length;
    }

    internal sealed class SequenceSummaryGroupAccumulator
    {
        private readonly string displayText;
        private readonly string sampleFileName;
        private readonly int firstIndex;
        private List<int> masterIndices;

        internal SequenceSummaryGroupAccumulator(string rawPrefix, string displayText, string sampleFileName, int firstIndex)
        {
            RawPrefix = rawPrefix ?? string.Empty;
            this.displayText = displayText ?? string.Empty;
            this.sampleFileName = sampleFileName ?? string.Empty;
            this.firstIndex = firstIndex;
            Count = 1;
        }

        internal string RawPrefix { get; }

        internal int Count { get; private set; }

        internal void Add(int masterIndex)
        {
            Count++;
            if (masterIndices == null)
            {
                masterIndices = new List<int>(4) { firstIndex, masterIndex };
                return;
            }

            masterIndices.Add(masterIndex);
        }

        internal SequenceSummaryGroup ToSummaryGroup(ImageBrowserSnapshot snapshot)
        {
            int[] indices = masterIndices != null ? masterIndices.ToArray() : Array.Empty<int>();
            string sampleRelativePath = snapshot != null ? snapshot.GetRelativePath(firstIndex) : string.Empty;
            return new SequenceSummaryGroup(displayText, sampleFileName, sampleRelativePath, firstIndex, indices);
        }
    }

    internal sealed class MatchedChunk
    {
        internal MatchedChunk(int startIndex, int[] indices)
        {
            StartIndex = startIndex;
            Indices = indices ?? Array.Empty<int>();
        }

        internal int StartIndex { get; }

        internal int[] Indices { get; }
    }

    internal sealed class DestinationChoice
    {
        public string DisplayName { get; set; }

        public string FolderPath { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
