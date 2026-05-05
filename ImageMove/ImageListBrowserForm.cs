using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageMove
{
    internal sealed class ImageListBrowserForm : Form
    {
        private readonly Main ownerMain;
        private readonly HashSet<string> checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly TextBox filterTextBox;
        private readonly DataGridView imageGrid;
        private readonly Label summaryLabel;
        private List<ImageBrowserItem> allItems = new List<ImageBrowserItem>();

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
            filterTextBox.TextChanged += (_, __) => ApplyFilter();
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
                EditMode = DataGridViewEditMode.EditOnEnter
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
            imageGrid.CellValueChanged += ImageGrid_CellValueChanged;
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
                ApplyFilter();
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
            allItems = ownerMain.GetImageBrowserItems().ToList();
            checkedPaths.RemoveWhere(path => allItems.All(item => !string.Equals(item.FullPath, path, StringComparison.OrdinalIgnoreCase)));
            ApplyFilter(selectedPath);
        }

        private void ApplyFilter(string preferredSelectedPath = null)
        {
            string filter = (filterTextBox.Text ?? string.Empty).Trim();
            IEnumerable<ImageBrowserItem> filteredItems = allItems;

            if (!string.IsNullOrWhiteSpace(filter))
            {
                filteredItems = filteredItems.Where(item =>
                    item.FileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.RelativePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            imageGrid.Rows.Clear();
            foreach (ImageBrowserItem item in filteredItems)
            {
                int rowIndex = imageGrid.Rows.Add(
                    checkedPaths.Contains(item.FullPath),
                    item.IsCurrent ? "●" : string.Empty,
                    item.FileName,
                    item.RelativePath,
                    item.FullPath);

                DataGridViewRow row = imageGrid.Rows[rowIndex];
                row.Tag = item;
                row.Cells["fullPath"].ToolTipText = item.FullPath;
                row.Cells["relativePath"].ToolTipText = item.RelativePath;

                if (item.IsCurrent)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 196);
                }
            }

            summaryLabel.Text = $"表示 {imageGrid.Rows.Count} 件 / 全体 {allItems.Count} 件 / チェック {checkedPaths.Count} 件";
            RestoreSelectedRow(preferredSelectedPath);
        }

        private void RestoreSelectedRow(string preferredSelectedPath)
        {
            string targetPath = preferredSelectedPath;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                targetPath = allItems.FirstOrDefault(item => item.IsCurrent)?.FullPath;
            }

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            foreach (DataGridViewRow row in imageGrid.Rows)
            {
                if (row.Tag is ImageBrowserItem item &&
                    string.Equals(item.FullPath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    if (row.Cells.Count > 1)
                    {
                        imageGrid.CurrentCell = row.Cells[1];
                    }

                    break;
                }
            }
        }

        private string GetSelectedPath()
        {
            if (imageGrid.CurrentRow?.Tag is ImageBrowserItem currentItem)
            {
                return currentItem.FullPath;
            }

            return null;
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

            RefreshItems();
        }

        private void BatchMoveButton_Click(object sender, EventArgs e)
        {
            List<string> selectedPaths = checkedPaths
                .Where(path => allItems.Any(item => string.Equals(item.FullPath, path, StringComparison.OrdinalIgnoreCase)))
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

        private void ImageGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (imageGrid.IsCurrentCellDirty)
            {
                imageGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void ImageGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != imageGrid.Columns["checked"].Index)
            {
                return;
            }

            if (!(imageGrid.Rows[e.RowIndex].Tag is ImageBrowserItem item))
            {
                return;
            }

            bool isChecked = Convert.ToBoolean(imageGrid.Rows[e.RowIndex].Cells["checked"].Value);
            if (isChecked)
            {
                checkedPaths.Add(item.FullPath);
            }
            else
            {
                checkedPaths.Remove(item.FullPath);
            }

            summaryLabel.Text = $"表示 {imageGrid.Rows.Count} 件 / 全体 {allItems.Count} 件 / チェック {checkedPaths.Count} 件";
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
                if (!(configuredDestinationComboBox.SelectedItem is DestinationChoice choice))
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

    internal sealed class ImageBrowserItem
    {
        public string FileName { get; set; }

        public string RelativePath { get; set; }

        public string FullPath { get; set; }

        public bool IsCurrent { get; set; }
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
