using System;
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

        private readonly Main ownerMain;
        private readonly HashSet<string> checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly TextBox filterTextBox;
        private readonly DataGridView imageGrid;
        private readonly Label summaryLabel;
        private readonly System.Windows.Forms.Timer filterDelayTimer;
        private CancellationTokenSource filterCts;
        private ImageBrowserSnapshot currentSnapshot = ImageBrowserSnapshot.Empty;
        private int[] filteredIndices = Array.Empty<int>();
        private readonly Dictionary<int, string> relativePathCache = new Dictionary<int, string>();
        private string currentPath = string.Empty;
        private string pendingPreferredSelectedPath = string.Empty;
        private bool filterInProgress;
        private bool showAllRows = true;
        private int visibleRowCount;
        private int visibleStartIndex;
        private int matchedRowCount;
        private string truncationMessage = string.Empty;

        internal ImageListBrowserForm(Main ownerMain)
        {
            this.ownerMain = ownerMain ?? throw new ArgumentNullException(nameof(ownerMain));

            Text = "画像一覧と一括移動";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1120, 760);
            MinimumSize = new Size(920, 620);

            filterDelayTimer = new System.Windows.Forms.Timer
            {
                Interval = 280
            };
            filterDelayTimer.Tick += FilterDelayTimer_Tick;

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
                ColumnCount = 4
            };
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
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

            var clearFilterButton = new Button
            {
                Text = "絞り込み解除",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            clearFilterButton.Click += (_, __) => filterTextBox.Clear();
            filterPanel.Controls.Add(clearFilterButton, 2, 0);

            var refreshButton = new Button
            {
                Text = "再読込",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            refreshButton.Click += (_, __) => RefreshItems();
            filterPanel.Controls.Add(refreshButton, 3, 0);

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

            var actionPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5
            };
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
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

            var batchMoveButton = new Button
            {
                Text = "チェック画像を移動",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            batchMoveButton.Click += BatchMoveButton_Click;
            actionPanel.Controls.Add(batchMoveButton, 1, 0);

            var clearCheckButton = new Button
            {
                Text = "選択解除",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            clearCheckButton.Click += (_, __) =>
            {
                checkedPaths.Clear();
                imageGrid.InvalidateColumn(imageGrid.Columns["checked"].Index);
                UpdateSummaryLabel();
            };
            actionPanel.Controls.Add(clearCheckButton, 2, 0);

            summaryLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            actionPanel.Controls.Add(summaryLabel, 3, 0);

            var closeButton = new Button
            {
                Text = "閉じる",
                Dock = DockStyle.Fill,
                UseVisualStyleBackColor = true
            };
            closeButton.Click += (_, __) => Close();
            actionPanel.Controls.Add(closeButton, 4, 0);

            rootLayout.Controls.Add(filterPanel, 0, 0);
            rootLayout.Controls.Add(imageGrid, 0, 1);
            rootLayout.Controls.Add(actionPanel, 0, 2);
            Controls.Add(rootLayout);
        }

        internal void RefreshItems()
        {
            string selectedPath = GetSelectedPath();
            currentSnapshot = ownerMain.GetImageBrowserSnapshot();
            currentPath = currentSnapshot.CurrentPath ?? string.Empty;
            relativePathCache.Clear();
            BeginApplyFilterAsync((filterTextBox.Text ?? string.Empty).Trim(), selectedPath);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            filterDelayTimer.Stop();
            filterCts?.Cancel();
            filterCts?.Dispose();
            base.OnFormClosed(e);
        }

        internal void UpdateCurrentPath(string newCurrentPath)
        {
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

        internal void StartImmediateFilterForTest(string filterText)
        {
            filterTextBox.Text = filterText ?? string.Empty;
            BeginApplyFilterAsync(filterTextBox.Text.Trim(), GetSelectedPath());
        }

        private void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            pendingPreferredSelectedPath = GetSelectedPath() ?? currentPath;
            filterDelayTimer.Stop();
            filterDelayTimer.Start();
            UpdateSummaryLabel();
        }

        private void FilterDelayTimer_Tick(object sender, EventArgs e)
        {
            filterDelayTimer.Stop();
            BeginApplyFilterAsync((filterTextBox.Text ?? string.Empty).Trim(), pendingPreferredSelectedPath);
        }

        private void BeginApplyFilterAsync(string filterText, string preferredSelectedPath)
        {
            filterCts?.Cancel();
            filterCts?.Dispose();
            filterCts = new CancellationTokenSource();
            CancellationToken token = filterCts.Token;
            ImageBrowserSnapshot snapshot = currentSnapshot;

            filterInProgress = true;
            UpdateSummaryLabel();

            Task.Run(() => BuildFilteredRows(snapshot, filterText, token), token)
                .ContinueWith(
                    task =>
                    {
                        if (IsDisposed || token.IsCancellationRequested)
                        {
                            return;
                        }

                        filterInProgress = false;

                        if (task.IsCanceled)
                        {
                            UpdateSummaryLabel();
                            return;
                        }

                        if (task.IsFaulted)
                        {
                            UpdateSummaryLabel();
                            MessageBox.Show("画像一覧の絞り込み中にエラーが発生しました。", "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        FilterResult result = task.Result;
                        showAllRows = result.ShowAllRows;
                        filteredIndices = result.Indices;
                        visibleRowCount = result.RowCount;
                        visibleStartIndex = result.StartIndex;
                        matchedRowCount = result.MatchedCount;
                        truncationMessage = result.TruncationMessage;
                        imageGrid.RowCount = visibleRowCount;
                        UpdateSummaryLabel();
                        RestoreSelectedRow(preferredSelectedPath);
                        imageGrid.Invalidate();
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private static FilterResult BuildFilteredRows(ImageBrowserSnapshot snapshot, string filterText, CancellationToken token)
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

                return FilterResult.AllRows(visibleCount, startIndex, paths.Length, truncationMessage);
            }

            var matches = new List<int>(Math.Min(paths.Length, MaxVisibleRows));
            int matchedCount = 0;
            for (int index = 0; index < paths.Length; index++)
            {
                token.ThrowIfCancellationRequested();
                string path = paths[index];
                if (path.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    Path.GetFileName(path).IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchedCount++;
                    if (matches.Count < MaxVisibleRows)
                    {
                        matches.Add(index);
                    }
                }
            }

            string filteredTruncationMessage = matchedCount > MaxVisibleRows
                ? $"一致件数が多いため先頭 {MaxVisibleRows:N0} 件だけ表示しています。"
                : string.Empty;

            return FilterResult.Filtered(matches.ToArray(), matchedCount, filteredTruncationMessage);
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
                return;
            }

            for (int rowIndex = 0; rowIndex < filteredIndices.Length; rowIndex++)
            {
                string path = currentSnapshot.FullPaths[filteredIndices[rowIndex]];
                if (!string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SelectRow(rowIndex);
                break;
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
                MessageBox.Show("表示したい画像を選択してください。", "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ownerMain.ShowImageByPath(targetPath))
            {
                MessageBox.Show("選択した画像は現在の一覧にありません。", "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("一括移動したい画像にチェックを付けてください。", "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new BatchMoveTargetDialog(ownerMain.GetDestinationChoices()))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                BatchMoveExecutionResult result = ownerMain.MoveImagesFromBrowser(selectedPaths, dialog.SelectedFolderPath, dialog.SelectedLabel);
                foreach (string movedPath in result.MovedSourcePaths)
                {
                    checkedPaths.Remove(movedPath);
                }

                RefreshItems();

                string message = $"移動完了: {result.MovedCount} 件";
                if (result.SkippedMessages.Count > 0)
                {
                    message += Environment.NewLine + string.Join(Environment.NewLine, result.SkippedMessages.Take(5));
                }

                MessageBox.Show(message, "画像一覧", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateSummaryLabel()
        {
            string state = filterInProgress ? " / 絞り込み中" : string.Empty;
            string suffix = string.IsNullOrWhiteSpace(truncationMessage) ? string.Empty : $" / {truncationMessage}";
            summaryLabel.Text = $"表示 {visibleRowCount:N0} 件 / 対象 {matchedRowCount:N0} 件 / 全体 {currentSnapshot.FullPaths.Length:N0} 件 / チェック {checkedPaths.Count:N0} 件{state}{suffix}";
        }

        private void ImageGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (imageGrid.IsCurrentCellDirty)
            {
                imageGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
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
                    e.Value = Path.GetFileName(path);
                    break;
                case "relativePath":
                    e.Value = GetRelativePath(masterIndex, path);
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

            UpdateSummaryLabel();
        }

        private int TryResolveDirectRowIndex(string targetPath)
        {
            if (!showAllRows)
            {
                return -1;
            }

            if (currentSnapshot.CurrentIndex >= visibleStartIndex &&
                currentSnapshot.CurrentIndex < visibleStartIndex + visibleRowCount &&
                currentSnapshot.CurrentIndex < currentSnapshot.FullPaths.Length &&
                string.Equals(currentSnapshot.FullPaths[currentSnapshot.CurrentIndex], targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return currentSnapshot.CurrentIndex - visibleStartIndex;
            }

            int absoluteIndex = Array.FindIndex(
                currentSnapshot.FullPaths,
                path => string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase));

            if (absoluteIndex < visibleStartIndex || absoluteIndex >= visibleStartIndex + visibleRowCount)
            {
                return -1;
            }

            return absoluteIndex - visibleStartIndex;
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

        private string GetRelativePath(int masterIndex, string fullPath)
        {
            if (relativePathCache.TryGetValue(masterIndex, out string cachedValue))
            {
                return cachedValue;
            }

            string relativePath = BuildRelativePath(currentSnapshot.SourceRoot, fullPath);
            relativePathCache[masterIndex] = relativePath;
            return relativePath;
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
                    MessageBox.Show("親画面で設定済みの移動先を選択してください。", "一括移動", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("任意フォルダを指定してください。", "一括移動", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        internal ImageBrowserSnapshot(string[] fullPaths, string sourceRoot, string currentPath, int currentIndex)
        {
            FullPaths = fullPaths ?? Array.Empty<string>();
            SourceRoot = sourceRoot ?? string.Empty;
            CurrentPath = currentPath ?? string.Empty;
            CurrentIndex = currentIndex;
        }

        internal string[] FullPaths { get; }

        internal string SourceRoot { get; }

        internal string CurrentPath { get; }

        internal int CurrentIndex { get; }
    }

    internal sealed class ImageBrowserItem
    {
        public string FileName { get; set; }

        public string RelativePath { get; set; }

        public string FullPath { get; set; }

        public bool IsCurrent { get; set; }
    }

    internal sealed class FilterResult
    {
        private FilterResult(bool showAllRows, int[] indices, int rowCount, int startIndex, int matchedCount, string truncationMessage)
        {
            ShowAllRows = showAllRows;
            Indices = indices ?? Array.Empty<int>();
            RowCount = rowCount;
            StartIndex = startIndex;
            MatchedCount = matchedCount;
            TruncationMessage = truncationMessage ?? string.Empty;
        }

        internal bool ShowAllRows { get; }

        internal int[] Indices { get; }

        internal int RowCount { get; }

        internal int StartIndex { get; }

        internal int MatchedCount { get; }

        internal string TruncationMessage { get; }

        internal static FilterResult Empty { get; } = new FilterResult(false, Array.Empty<int>(), 0, 0, 0, string.Empty);

        internal static FilterResult AllRows(int rowCount, int startIndex, int matchedCount, string truncationMessage)
        {
            return new FilterResult(true, Array.Empty<int>(), rowCount, startIndex, matchedCount, truncationMessage);
        }

        internal static FilterResult Filtered(int[] indices, int matchedCount, string truncationMessage)
        {
            return new FilterResult(false, indices, indices?.Length ?? 0, 0, matchedCount, truncationMessage);
        }
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
