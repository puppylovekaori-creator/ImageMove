using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace ImageMove
{
    internal enum ReviewModeKind
    {
        SingleImage,
        GridReview
    }

    internal enum GridReviewItemStatus
    {
        Unprocessed,
        Excluded,
        Restored
    }

    internal enum GridReviewSortMode
    {
        ExistingOrder,
        FileName,
        LastWriteTime,
        CreationTime,
        PixelSize,
        FileSize
    }

    internal enum GridReviewSortDirection
    {
        Ascending,
        Descending
    }

    internal enum GridReviewCheckFilter
    {
        All,
        CheckedOnly,
        UncheckedOnly
    }

    internal enum GridReviewAspectRatioFilter
    {
        All,
        Landscape,
        Portrait,
        NearSquare
    }

    internal enum GridReviewImageSizeFilter
    {
        All,
        Small,
        Medium,
        Large
    }

    internal enum GridThumbnailSizePreset
    {
        Small,
        Medium,
        Large,
        Custom
    }

    internal enum GridPageSizePreset
    {
        P100,
        P300,
        P500,
        P1000,
        Custom
    }

    internal enum MoveOperationKind
    {
        ExcludeToDestination,
        RestoreToSource
    }

    internal enum MoveOperationOrigin
    {
        SingleReview,
        ImageListBrowser,
        GridReview,
        Undo
    }

    internal sealed class GridReviewSettingsState
    {
        internal static GridReviewSettingsState CreateDefault()
        {
            return new GridReviewSettingsState
            {
                ReviewMode = ReviewModeKind.SingleImage,
                LastExcludeFolderPath = string.Empty,
                ThumbnailSizePreset = GridThumbnailSizePreset.Medium,
                ThumbnailCustomSize = 240,
                PageSizePreset = GridPageSizePreset.P1000,
                PageCustomSize = 1000,
                ShowFileName = true,
                ShowImageId = true,
                ShowCheckBoxes = true,
                SortMode = GridReviewSortMode.ExistingOrder,
                SortDirection = GridReviewSortDirection.Ascending,
                SearchText = string.Empty,
                ExtensionFilter = string.Empty,
                StatusFilter = string.Empty,
                CheckFilter = GridReviewCheckFilter.All,
                AspectRatioFilter = GridReviewAspectRatioFilter.All,
                ImageSizeFilter = GridReviewImageSizeFilter.All,
                CurrentPage = 1,
                SidebarWidth = 420,
                HistoryColumnWidths = string.Empty
            };
        }

        public ReviewModeKind ReviewMode { get; set; }

        public string LastExcludeFolderPath { get; set; }

        public GridThumbnailSizePreset ThumbnailSizePreset { get; set; }

        public int ThumbnailCustomSize { get; set; }

        public GridPageSizePreset PageSizePreset { get; set; }

        public int PageCustomSize { get; set; }

        public bool ShowFileName { get; set; }

        public bool ShowImageId { get; set; }

        public bool ShowCheckBoxes { get; set; }

        public GridReviewSortMode SortMode { get; set; }

        public GridReviewSortDirection SortDirection { get; set; }

        public string SearchText { get; set; }

        public string ExtensionFilter { get; set; }

        public string StatusFilter { get; set; }

        public GridReviewCheckFilter CheckFilter { get; set; }

        public GridReviewAspectRatioFilter AspectRatioFilter { get; set; }

        public GridReviewImageSizeFilter ImageSizeFilter { get; set; }

        public int CurrentPage { get; set; }

        public int SidebarWidth { get; set; }

        public string HistoryColumnWidths { get; set; }
    }

    internal sealed class GridReviewFilterState
    {
        public string SearchText { get; set; } = string.Empty;

        public string ExtensionFilter { get; set; } = string.Empty;

        public string StatusFilter { get; set; } = string.Empty;

        public GridReviewCheckFilter CheckFilter { get; set; } = GridReviewCheckFilter.All;

        public GridReviewAspectRatioFilter AspectRatioFilter { get; set; } = GridReviewAspectRatioFilter.All;

        public GridReviewImageSizeFilter ImageSizeFilter { get; set; } = GridReviewImageSizeFilter.All;
    }

    internal sealed class GridReviewItemRecord
    {
        public int ExistingOrder { get; set; }

        public string OriginalSourcePath { get; set; }

        public string CurrentPath { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        public long FileSize { get; set; }

        public DateTime LastWriteTimeUtc { get; set; }

        public DateTime CreationTimeUtc { get; set; }

        public Size PixelSize { get; set; }

        public bool PixelSizeKnown { get; set; }

        public bool PixelSizeFailed { get; set; }

        public bool WasExcludedOnce { get; set; }

        public GridReviewItemStatus Status { get; set; } = GridReviewItemStatus.Unprocessed;

        public DateTime StatusChangedAtUtc { get; set; } = DateTime.UtcNow;

        public string LastDestinationPath { get; set; }

        public string DisplayFileName
        {
            get
            {
                return string.IsNullOrWhiteSpace(FileName)
                    ? System.IO.Path.GetFileName(CurrentPath) ?? string.Empty
                    : FileName;
            }
        }

        public string StatusLabel
        {
            get
            {
                switch (Status)
                {
                    case GridReviewItemStatus.Excluded:
                        return "除外済み";
                    case GridReviewItemStatus.Restored:
                        return "戻し済み";
                    default:
                        return "未処理";
                }
            }
        }
    }

    internal sealed class GridReviewRestoreRequest
    {
        public string StableSourcePath { get; set; }

        public string CurrentPath { get; set; }

        public string RestoreTargetPath { get; set; }

        public int OriginalIndex { get; set; }
    }

    internal sealed class GridReviewSelectionSummary
    {
        public int CheckedCount { get; set; }

        public int VisibleCount { get; set; }

        public int FilteredCount { get; set; }

        public int TotalCount { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public string StatusText { get; set; } = string.Empty;
    }

    internal sealed class GridReviewThumbnailProgress
    {
        public int TotalCount { get; set; }

        public int CompletedCount { get; set; }

        public int FailedCount { get; set; }

        public bool IsRunning { get; set; }

        public string CurrentFileName { get; set; } = string.Empty;

        public string StatusText { get; set; } = string.Empty;
    }

    internal sealed class GridReviewOperationRecord
    {
        public string OperationId { get; set; } = string.Empty;

        public string ExecutedAt { get; set; } = string.Empty;

        public string Who { get; set; } = "ImageMove";

        public string OperationType { get; set; } = string.Empty;

        public int TargetCount { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public string TargetFolder { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;
    }

    internal sealed class FileOperationFailureDetail
    {
        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public string ErrorMessage { get; set; }
    }

    internal sealed class GridReviewOperationLogEntry
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public string Message { get; set; } = string.Empty;

        public string Who { get; set; } = "ImageMove";

        public IReadOnlyList<KeyValuePair<string, string>> Details { get; set; } = Array.Empty<KeyValuePair<string, string>>();
    }

    internal sealed class GridReviewHistoryBindingList : BindingList<GridReviewOperationRecord>
    {
    }
}
