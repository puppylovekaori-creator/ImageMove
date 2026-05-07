[CmdletBinding()]
param(
    [int]$TextFileCount = 20000,
    [int]$SyntheticImageCount = 250000,
    [int]$ExpectedVisibleRowLimit = 100000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::SetUnhandledExceptionMode([System.Windows.Forms.UnhandledExceptionMode]::CatchException)
$script:threadExceptions = New-Object 'System.Collections.Generic.List[string]'
$threadExceptionHandler = [System.Threading.ThreadExceptionEventHandler]{
    param($sender, $e)
    $script:threadExceptions.Add($e.Exception.ToString())
}
[System.Windows.Forms.Application]::add_ThreadException($threadExceptionHandler)

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Join-Path $repoRoot 'ImageMove.sln'
$releaseExePath = Join-Path $repoRoot 'release\ImageMove.exe'
$msbuildPath = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'

function Invoke-PrivateMethod {
    param(
        [Parameter(Mandatory = $true)] $Target,
        [Parameter(Mandatory = $true)][string]$MethodName,
        [object[]]$Arguments = @()
    )

    $method = $Target.GetType().GetMethod($MethodName, [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    if ($null -eq $method) {
        throw "Method not found: $MethodName"
    }

    return $method.Invoke($Target, $Arguments)
}

function Get-PrivateFieldValue {
    param(
        [Parameter(Mandatory = $true)] $Target,
        [Parameter(Mandatory = $true)][string]$FieldName
    )

    $field = $Target.GetType().GetField($FieldName, [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    if ($null -eq $field) {
        throw "Field not found: $FieldName"
    }

    return $field.GetValue($Target)
}

function Set-PrivateFieldValue {
    param(
        [Parameter(Mandatory = $true)] $Target,
        [Parameter(Mandatory = $true)][string]$FieldName,
        [Parameter(Mandatory = $true)] $Value
    )

    $field = $Target.GetType().GetField($FieldName, [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    if ($null -eq $field) {
        throw "Field not found: $FieldName"
    }

    $field.SetValue($Target, $Value)
}

function Wait-Until {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Condition,
        [int]$TimeoutMs = 15000
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while (-not (& $Condition)) {
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 25
        if ($stopwatch.ElapsedMilliseconds -gt $TimeoutMs) {
            throw "Timeout after $TimeoutMs ms."
        }
    }
}

function New-JpegFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Text
    )

    $bitmap = New-Object System.Drawing.Bitmap 48, 48
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::White)
            $graphics.DrawRectangle([System.Drawing.Pens]::DarkBlue, 1, 1, 46, 46)
            if (-not [string]::IsNullOrWhiteSpace($Text)) {
                $graphics.DrawString($Text, [System.Drawing.SystemFonts]::DefaultFont, [System.Drawing.Brushes]::Black, 4, 16)
            }
        }
        finally {
            $graphics.Dispose()
        }

        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    }
    finally {
        $bitmap.Dispose()
    }
}

function New-TempDirectory {
    param([string]$Prefix)

    $path = Join-Path ([System.IO.Path]::GetTempPath()) ($Prefix + '_' + [Guid]::NewGuid().ToString('N'))
    [System.IO.Directory]::CreateDirectory($path) | Out-Null
    return $path
}

Write-Host 'BUILD: Release'
& $msbuildPath $solutionPath /t:Build /p:Configuration=Release /m | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $releaseExePath)) {
    throw "Release exe not found: $releaseExePath"
}

[void][System.Reflection.Assembly]::LoadFrom($releaseExePath)

$mainForm = New-Object ImageMove.Main
$mainForm.Show()
[System.Windows.Forms.Application]::DoEvents()

$browserForm = $null
$mixedRoot = $null
$prefetchRoot = $null
$historyRoot = $null
$historySettingDir = $null
$historyReloadForm = $null
$summaryRoot = $null
$syntheticRoot = $null
$gridPagingRoot = $null
$gridOpsSourceRoot = $null
$gridOpsExcludeRoot = $null

try {
    $sourceTextBox = Get-PrivateFieldValue -Target $mainForm -FieldName 'textBox1'
    $reviewModeLayoutMethod = $mainForm.GetType().GetMethod('IsReviewModeLayoutValidForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $setReviewModeMethod = $mainForm.GetType().GetMethod('SetReviewModeForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridBusyMethod = $mainForm.GetType().GetMethod('IsGridReviewBusyForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridVisibleCountMethod = $mainForm.GetType().GetMethod('GridReviewVisibleItemCountForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridFilteredCountMethod = $mainForm.GetType().GetMethod('GridReviewFilteredItemCountForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridTotalCountMethod = $mainForm.GetType().GetMethod('GridReviewTotalItemCountForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridCheckedCountMethod = $mainForm.GetType().GetMethod('GridReviewCheckedCountForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridSidebarOverflowMethod = $mainForm.GetType().GetMethod('GridReviewHasSidebarHorizontalOverflowForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridSidebarMinWidthMethod = $mainForm.GetType().GetMethod('GridReviewSidebarMinimumWidthForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridSidebarCurrentWidthMethod = $mainForm.GetType().GetMethod('GridReviewSidebarCurrentWidthForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridStatusCountMethod = $mainForm.GetType().GetMethod('GridReviewStatusCountForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridSetPageSizeMethod = $mainForm.GetType().GetMethod('GridReviewSetPageSizeForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridGoToPageMethod = $mainForm.GetType().GetMethod('GridReviewGoToPageForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridCheckVisibleMethod = $mainForm.GetType().GetMethod('GridReviewCheckVisiblePageForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridSetStatusFilterMethod = $mainForm.GetType().GetMethod('GridReviewSetStatusFilterForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridExcludeCheckedMethod = $mainForm.GetType().GetMethod('GridReviewExcludeCheckedToFolderForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $gridRestoreCheckedMethod = $mainForm.GetType().GetMethod('GridReviewRestoreCheckedForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    if ($null -eq $reviewModeLayoutMethod -or $null -eq $setReviewModeMethod -or $null -eq $gridBusyMethod -or $null -eq $gridVisibleCountMethod -or
        $null -eq $gridFilteredCountMethod -or $null -eq $gridTotalCountMethod -or $null -eq $gridCheckedCountMethod -or
        $null -eq $gridSidebarOverflowMethod -or $null -eq $gridSidebarMinWidthMethod -or $null -eq $gridSidebarCurrentWidthMethod -or
        $null -eq $gridStatusCountMethod -or $null -eq $gridSetPageSizeMethod -or $null -eq $gridGoToPageMethod -or
        $null -eq $gridCheckVisibleMethod -or $null -eq $gridSetStatusFilterMethod -or $null -eq $gridExcludeCheckedMethod -or
        $null -eq $gridRestoreCheckedMethod) {
        throw 'Grid review test helper methods were not found on Main.'
    }

    if (-not [bool]$reviewModeLayoutMethod.Invoke($mainForm, @())) {
        throw 'Review mode layout is invalid for the existing single-image screen.'
    }

    Write-Host "TEST1: mixed scan with $TextFileCount text files"
    $mixedRoot = New-TempDirectory -Prefix 'ImageMoveMixedScan'
    $txtRoot = Join-Path $mixedRoot 'txt'
    $imgRoot = Join-Path $mixedRoot 'img'
    [System.IO.Directory]::CreateDirectory($txtRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory($imgRoot) | Out-Null

    for ($index = 0; $index -lt $TextFileCount; $index++) {
        $subDir = Join-Path $txtRoot ('group_' + ($index % 20).ToString('00'))
        if (-not (Test-Path $subDir)) {
            [System.IO.Directory]::CreateDirectory($subDir) | Out-Null
        }

        $filePath = Join-Path $subDir ('note_' + $index.ToString('000000') + '.txt')
        [System.IO.File]::WriteAllText($filePath, [string]::Empty, [System.Text.Encoding]::UTF8)
    }

    1..3 | ForEach-Object {
        New-JpegFile -Path (Join-Path $imgRoot ("sample_$($_).jpg")) -Text $_
    }

    $sourceTextBox.Text = $mixedRoot
    $reloadStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-PrivateMethod -Target $mainForm -MethodName 'ReloadImages' | Out-Null
    $reloadStopwatch.Stop()

    $loadedPaths = Get-PrivateFieldValue -Target $mainForm -FieldName 'imagePaths'
    if ($loadedPaths.Count -ne 3) {
        throw "Expected 3 images after mixed scan, actual=$($loadedPaths.Count)"
    }

    Write-Host ("RESULT1: loaded={0} elapsed_ms={1}" -f $loadedPaths.Count, $reloadStopwatch.ElapsedMilliseconds)

    Write-Host 'TEST1B: prefetch cache warm-up'
    $prefetchRoot = New-TempDirectory -Prefix 'ImageMovePrefetch'
    1..24 | ForEach-Object {
        New-JpegFile -Path (Join-Path $prefetchRoot ("prefetch_$($_.ToString('000')).jpg")) -Text $_
    }

    $sourceTextBox.Text = $prefetchRoot
    Invoke-PrivateMethod -Target $mainForm -MethodName 'ReloadImages' | Out-Null
    $prefetchPaths = Get-PrivateFieldValue -Target $mainForm -FieldName 'imagePaths'
    $cacheEntries = Get-PrivateFieldValue -Target $mainForm -FieldName 'imageCacheEntries'

    Wait-Until -TimeoutMs 10000 -Condition { $cacheEntries.Count -ge 6 }
    if (-not $cacheEntries.ContainsKey($prefetchPaths[1])) {
        throw 'Prefetch cache did not warm the next image.'
    }

    $nextStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-PrivateMethod -Target $mainForm -MethodName 'ShowNextImage' | Out-Null
    $nextStopwatch.Stop()

    $currentIndexAfterNext = [int](Get-PrivateFieldValue -Target $mainForm -FieldName 'currentImageIndex')
    if ($currentIndexAfterNext -ne 1) {
        throw "Expected current index 1 after next image, actual=$currentIndexAfterNext"
    }

    Wait-Until -TimeoutMs 10000 -Condition { $cacheEntries.Count -ge 8 }
    Write-Host ("RESULT1B: cache={0} next_ms={1}" -f $cacheEntries.Count, $nextStopwatch.ElapsedMilliseconds)

    Write-Host 'TEST1C: folder history combo and save/load'
    $historyRoot = New-TempDirectory -Prefix 'ImageMoveHistory'
    $historySettingDir = New-TempDirectory -Prefix 'ImageMoveHistorySetting'
    $historySettingPath = Join-Path $historySettingDir 'setting.xml'
    $sourceHistoryDirs = 1..7 | ForEach-Object {
        $dir = Join-Path $historyRoot ("source_" + $_.ToString('00'))
        [System.IO.Directory]::CreateDirectory($dir) | Out-Null
        $dir
    }
    $destinationHistoryDirs = 1..8 | ForEach-Object {
        $dir = Join-Path $historyRoot ("destination_" + $_.ToString('00'))
        [System.IO.Directory]::CreateDirectory($dir) | Out-Null
        $dir
    }

    $sourceCombo = Get-PrivateFieldValue -Target $mainForm -FieldName 'textBox1'
    $destinationCombo = Get-PrivateFieldValue -Target $mainForm -FieldName 'textBox2'
    if ($sourceCombo.GetType().FullName -ne 'System.Windows.Forms.ComboBox' -or $destinationCombo.GetType().FullName -ne 'System.Windows.Forms.ComboBox') {
        throw 'Path controls are not ComboBox.'
    }

    Set-PrivateFieldValue -Target $mainForm -FieldName 'settingFileName' -Value $historySettingPath
    Set-PrivateFieldValue -Target $mainForm -FieldName 'recentFolderHistoryLimit' -Value 5
    $rememberSourceMethod = $mainForm.GetType().GetMethod('RememberSourceDirectory', [System.Reflection.BindingFlags]'Instance, NonPublic')
    $rememberDestinationMethod = $mainForm.GetType().GetMethod('RememberDestinationDirectory', [System.Reflection.BindingFlags]'Instance, NonPublic')
    if ($null -eq $rememberSourceMethod -or $null -eq $rememberDestinationMethod) {
        throw 'Recent folder history methods were not found.'
    }

    foreach ($dir in $sourceHistoryDirs) {
        $rememberSourceMethod.Invoke($mainForm, @([string]$dir, $true)) | Out-Null
    }

    foreach ($dir in $destinationHistoryDirs) {
        $rememberDestinationMethod.Invoke($mainForm, @([string]$dir, $true)) | Out-Null
    }

    $sourceCombo.Text = $sourceHistoryDirs[-1]
    $destinationCombo.Text = $destinationHistoryDirs[-1]
    Invoke-PrivateMethod -Target $mainForm -MethodName 'SaveSetting' | Out-Null

    $historyReloadForm = New-Object ImageMove.Main
    $historyReloadForm.Show()
    [System.Windows.Forms.Application]::DoEvents()
    Set-PrivateFieldValue -Target $historyReloadForm -FieldName 'settingFileName' -Value $historySettingPath
    Invoke-PrivateMethod -Target $historyReloadForm -MethodName 'LoadSetting' | Out-Null

    $reloadedSourceCombo = Get-PrivateFieldValue -Target $historyReloadForm -FieldName 'textBox1'
    $reloadedDestinationCombo = Get-PrivateFieldValue -Target $historyReloadForm -FieldName 'textBox2'
    $reloadedLimit = [int](Get-PrivateFieldValue -Target $historyReloadForm -FieldName 'recentFolderHistoryLimit')
    if ($reloadedLimit -ne 5) {
        throw "Expected recent folder history limit 5, actual=$reloadedLimit"
    }

    if ($reloadedSourceCombo.Items.Count -ne 5 -or $reloadedDestinationCombo.Items.Count -ne 5) {
        throw "Unexpected history item counts: source=$($reloadedSourceCombo.Items.Count) destination=$($reloadedDestinationCombo.Items.Count)"
    }

    if ($reloadedSourceCombo.Items[0] -ne $sourceHistoryDirs[-1] -or $reloadedDestinationCombo.Items[0] -ne $destinationHistoryDirs[-1]) {
        throw 'Recent folder history order was not preserved.'
    }

    Write-Host ("RESULT1C: source_items={0} destination_items={1} limit={2}" -f $reloadedSourceCombo.Items.Count, $reloadedDestinationCombo.Items.Count, $reloadedLimit)

    Write-Host 'TEST1D: summary grid for grouped serial prefixes'
    $summaryRoot = New-TempDirectory -Prefix 'ImageMoveSummary'
    $summaryPaths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($relativePath in @(
        'clipA_0001.jpg',
        'clipA_0002.jpg',
        'clipA_0003.jpg',
        'clipB_0001.jpg',
        'clipB_0002.jpg',
        'single_file.jpg'
    )) {
        $summaryPaths.Add((Join-Path $summaryRoot $relativePath))
    }

    Set-PrivateFieldValue -Target $mainForm -FieldName 'imagePaths' -Value $summaryPaths
    Set-PrivateFieldValue -Target $mainForm -FieldName 'currentImageIndex' -Value 0
    Invoke-PrivateMethod -Target $mainForm -MethodName 'RebuildImagePathCache' | Out-Null
    Invoke-PrivateMethod -Target $mainForm -MethodName 'OpenImageListBrowser_Click' -Arguments @($null, [System.EventArgs]::Empty) | Out-Null

    $browserForm = Get-PrivateFieldValue -Target $mainForm -FieldName 'imageListBrowserForm'
    if ($null -eq $browserForm) {
        throw 'Browser form was not created for summary test.'
    }

    $summaryFilterMethod = $browserForm.GetType().GetMethod('IsFilterInProgressForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $summaryGroupCountMethod = $browserForm.GetType().GetMethod('SummaryGroupCountForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $checkedPathCountMethod = $browserForm.GetType().GetMethod('CheckedPathCountForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $setSummaryGroupCheckedMethod = $browserForm.GetType().GetMethod('SetSummaryGroupCheckedForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    if ($null -eq $summaryFilterMethod -or $null -eq $summaryGroupCountMethod -or $null -eq $checkedPathCountMethod -or $null -eq $setSummaryGroupCheckedMethod) {
        throw 'Summary test helper methods were not found on ImageListBrowserForm.'
    }

    Wait-Until -TimeoutMs 30000 -Condition { -not [bool]$summaryFilterMethod.Invoke($browserForm, @()) }
    $summaryGroupCount = [int]$summaryGroupCountMethod.Invoke($browserForm, @())
    if ($summaryGroupCount -ne 2) {
        throw "Expected 2 summary groups, actual=$summaryGroupCount"
    }

    $setSummaryGroupCheckedMethod.Invoke($browserForm, @(0, $true)) | Out-Null
    $checkedPathCount = [int]$checkedPathCountMethod.Invoke($browserForm, @())
    if ($checkedPathCount -ne 3) {
        throw "Expected 3 checked paths after first summary group selection, actual=$checkedPathCount"
    }

    $setSummaryGroupCheckedMethod.Invoke($browserForm, @(1, $true)) | Out-Null
    $checkedPathCount = [int]$checkedPathCountMethod.Invoke($browserForm, @())
    if ($checkedPathCount -ne 5) {
        throw "Expected 5 checked paths after second summary group selection, actual=$checkedPathCount"
    }

    Write-Host ("RESULT1D: groups={0} checked={1}" -f $summaryGroupCount, $checkedPathCount)

    $browserForm.Close()
    $browserForm.Dispose()
    $browserForm = $null

    Write-Host "TEST2: virtual browser with $SyntheticImageCount synthetic image paths"
    $syntheticRoot = New-TempDirectory -Prefix 'ImageMoveSyntheticBrowser'
    $sourceTextBox.Text = $syntheticRoot

    $syntheticPaths = New-Object 'System.Collections.Generic.List[string]'
    for ($index = 0; $index -lt $SyntheticImageCount; $index++) {
        $bucket = Join-Path $syntheticRoot ('bucket_' + ($index % 100).ToString('000'))
        $syntheticPaths.Add((Join-Path $bucket ('sample_' + $index.ToString('000000') + '.jpg')))
    }

    Set-PrivateFieldValue -Target $mainForm -FieldName 'imagePaths' -Value $syntheticPaths
    Set-PrivateFieldValue -Target $mainForm -FieldName 'currentImageIndex' -Value 0

    $openStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-PrivateMethod -Target $mainForm -MethodName 'OpenImageListBrowser_Click' -Arguments @($null, [System.EventArgs]::Empty) | Out-Null
    $openStopwatch.Stop()

    $browserForm = Get-PrivateFieldValue -Target $mainForm -FieldName 'imageListBrowserForm'
    if ($null -eq $browserForm) {
        throw 'Browser form was not created.'
    }

    $isFilterMethod = $browserForm.GetType().GetMethod('IsFilterInProgressForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $rowCountMethod = $browserForm.GetType().GetMethod('VisibleRowCountForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $filterMethod = $browserForm.GetType().GetMethod('StartImmediateFilterForTest', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $updateCurrentPathMethod = $browserForm.GetType().GetMethod('UpdateCurrentPath', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')

    if ($null -eq $isFilterMethod -or $null -eq $rowCountMethod -or $null -eq $filterMethod -or $null -eq $updateCurrentPathMethod) {
        throw 'Test helper methods were not found on ImageListBrowserForm.'
    }

    if ($openStopwatch.ElapsedMilliseconds -gt 5000) {
        throw "Browser open returned too slowly: $($openStopwatch.ElapsedMilliseconds) ms"
    }

    Wait-Until -TimeoutMs 30000 -Condition { -not [bool]$isFilterMethod.Invoke($browserForm, @()) }
    $visibleCount = [int]$rowCountMethod.Invoke($browserForm, @())
    $expectedVisibleCount = [Math]::Min($SyntheticImageCount, $ExpectedVisibleRowLimit)
    if ($visibleCount -ne $expectedVisibleCount) {
        throw "Expected $expectedVisibleCount visible rows, actual=$visibleCount"
    }

    $currentPathUpdateStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    for ($index = 0; $index -lt 50; $index++) {
        $path = $syntheticPaths[($index * 997) % $syntheticPaths.Count]
        $updateCurrentPathMethod.Invoke($browserForm, @($path)) | Out-Null
        [System.Windows.Forms.Application]::DoEvents()
    }
    $currentPathUpdateStopwatch.Stop()
    if ($currentPathUpdateStopwatch.ElapsedMilliseconds -gt 300) {
        throw "Current-path update is too slow: $($currentPathUpdateStopwatch.ElapsedMilliseconds) ms"
    }

    $filterTerm = 'sample_001999'
    $filterStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $filterMethod.Invoke($browserForm, @($filterTerm)) | Out-Null
    Wait-Until -TimeoutMs 30000 -Condition {
        $filterInProgress = [bool]$isFilterMethod.Invoke($browserForm, @())
        $currentVisibleCount = [int]$rowCountMethod.Invoke($browserForm, @())
        (-not $filterInProgress) -and $currentVisibleCount -eq 1
    }
    $filterStopwatch.Stop()

    $filteredCount = [int]$rowCountMethod.Invoke($browserForm, @())
    if ($filteredCount -ne 1) {
        throw "Filtered row count is invalid for '$filterTerm': $filteredCount"
    }

    $chainTerms = @('sample_001', 'sample_0019', 'sample_00199', 'sample_001999')
    $chainResults = @()
    foreach ($term in $chainTerms) {
        $chainStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $filterMethod.Invoke($browserForm, @($term)) | Out-Null
        Wait-Until -TimeoutMs 30000 -Condition { -not [bool]$isFilterMethod.Invoke($browserForm, @()) }
        $chainStopwatch.Stop()

        $chainVisibleCount = [int]$rowCountMethod.Invoke($browserForm, @())
        $chainResults += ("{0}={1}ms/{2}" -f $term, $chainStopwatch.ElapsedMilliseconds, $chainVisibleCount)
    }

    Write-Host ("RESULT2: open_ms={0} visible={1} current_update_ms={2} filter_ms={3} filtered={4}" -f $openStopwatch.ElapsedMilliseconds, $visibleCount, $currentPathUpdateStopwatch.ElapsedMilliseconds, $filterStopwatch.ElapsedMilliseconds, $filteredCount)
    Write-Host ("RESULT2B: {0}" -f ($chainResults -join ', '))
    Write-Host 'TEST2C: close browser while filter is still running'
    $filterMethod.Invoke($browserForm, @('sample_001')) | Out-Null
    Wait-Until -TimeoutMs 1000 -Condition { [bool]$isFilterMethod.Invoke($browserForm, @()) }
    $browserForm.Close()
    $browserForm.Dispose()
    $browserForm = $null

    $closeWait = [System.Diagnostics.Stopwatch]::StartNew()
    while ($closeWait.ElapsedMilliseconds -lt 1500) {
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 25
    }

    if ($script:threadExceptions.Count -gt 0) {
        throw ("Unhandled WinForms exception after close: " + $script:threadExceptions[0])
    }

    Write-Host 'RESULT2C: close_during_filter_ok'

    Write-Host 'TEST3: grid review paging with 1100 actual images'
    $gridPagingRoot = New-TempDirectory -Prefix 'ImageMoveGridPaging'
    for ($index = 0; $index -lt 1100; $index++) {
        $bucket = Join-Path $gridPagingRoot ('group_' + ($index % 20).ToString('00'))
        if (-not (Test-Path $bucket)) {
            [System.IO.Directory]::CreateDirectory($bucket) | Out-Null
        }

        New-JpegFile -Path (Join-Path $bucket ('grid_' + $index.ToString('0000') + '.jpg')) -Text ($index % 100)
    }

    $sourceTextBox.Text = $gridPagingRoot
    Invoke-PrivateMethod -Target $mainForm -MethodName 'ReloadImages' | Out-Null
    $setReviewModeMethod.Invoke($mainForm, @(1)) | Out-Null
    $gridSetPageSizeMethod.Invoke($mainForm, @(1000)) | Out-Null
    $gridSidebarHasOverflow = [bool]$gridSidebarOverflowMethod.Invoke($mainForm, @())
    if ($gridSidebarHasOverflow) {
        $gridSidebarMinWidth = [int]$gridSidebarMinWidthMethod.Invoke($mainForm, @())
        $gridSidebarCurrentWidth = [int]$gridSidebarCurrentWidthMethod.Invoke($mainForm, @())
        throw "Grid sidebar layout is invalid: overflow=$gridSidebarHasOverflow current_width=$gridSidebarCurrentWidth min_width=$gridSidebarMinWidth"
    }

    Wait-Until -TimeoutMs 30000 -Condition { [int]$gridVisibleCountMethod.Invoke($mainForm, @()) -eq 1000 }
    $gridPagingVisibleCount = [int]$gridVisibleCountMethod.Invoke($mainForm, @())
    $gridPagingFilteredCount = [int]$gridFilteredCountMethod.Invoke($mainForm, @())
    $gridPagingTotalCount = [int]$gridTotalCountMethod.Invoke($mainForm, @())
    if ($gridPagingVisibleCount -ne 1000 -or $gridPagingFilteredCount -ne 1100 -or $gridPagingTotalCount -ne 1100) {
        throw "Grid paging counts are invalid: visible=$gridPagingVisibleCount filtered=$gridPagingFilteredCount total=$gridPagingTotalCount"
    }

    $gridCheckVisibleMethod.Invoke($mainForm, @()) | Out-Null
    $gridCheckedAfterPage1 = [int]$gridCheckedCountMethod.Invoke($mainForm, @())
    if ($gridCheckedAfterPage1 -ne 1000) {
        throw "Expected 1000 checked images on page 1, actual=$gridCheckedAfterPage1"
    }

    $gridGoToPageMethod.Invoke($mainForm, @(2)) | Out-Null
    Wait-Until -TimeoutMs 30000 -Condition { [int]$gridVisibleCountMethod.Invoke($mainForm, @()) -eq 100 }
    $gridPage2VisibleCount = [int]$gridVisibleCountMethod.Invoke($mainForm, @())
    $gridCheckedAfterPageMove = [int]$gridCheckedCountMethod.Invoke($mainForm, @())
    if ($gridPage2VisibleCount -ne 100 -or $gridCheckedAfterPageMove -ne 1000) {
        throw "Grid paging state was not preserved: page2_visible=$gridPage2VisibleCount checked=$gridCheckedAfterPageMove"
    }

    Write-Host ("RESULT3: visible={0} filtered={1} total={2} checked_after_page_move={3}" -f $gridPagingVisibleCount, $gridPagingFilteredCount, $gridPagingTotalCount, $gridCheckedAfterPageMove)

    Write-Host 'TEST4: grid review exclude / restore / undo'
    $gridOpsSourceRoot = New-TempDirectory -Prefix 'ImageMoveGridOpsSource'
    $gridOpsExcludeRoot = New-TempDirectory -Prefix 'ImageMoveGridOpsExclude'
    for ($index = 0; $index -lt 12; $index++) {
        New-JpegFile -Path (Join-Path $gridOpsSourceRoot ('ops_' + $index.ToString('000') + '.jpg')) -Text $index
    }

    $sourceTextBox.Text = $gridOpsSourceRoot
    Invoke-PrivateMethod -Target $mainForm -MethodName 'ReloadImages' | Out-Null
    $setReviewModeMethod.Invoke($mainForm, @(1)) | Out-Null
    $gridSetPageSizeMethod.Invoke($mainForm, @(10)) | Out-Null
    $gridSetStatusFilterMethod.Invoke($mainForm, @(-1)) | Out-Null

    Wait-Until -TimeoutMs 30000 -Condition { [int]$gridVisibleCountMethod.Invoke($mainForm, @()) -eq 10 }
    $gridCheckVisibleMethod.Invoke($mainForm, @()) | Out-Null
    $gridExcludeCheckedMethod.Invoke($mainForm, @([string]$gridOpsExcludeRoot)) | Out-Null

    $postExcludeImagePaths = Get-PrivateFieldValue -Target $mainForm -FieldName 'imagePaths'
    $excludedStatusCount = [int]$gridStatusCountMethod.Invoke($mainForm, @(1))
    if ($postExcludeImagePaths.Count -ne 2 -or $excludedStatusCount -ne 10) {
        throw "Exclude result is invalid: queue=$($postExcludeImagePaths.Count) excluded=$excludedStatusCount"
    }

    $gridSetStatusFilterMethod.Invoke($mainForm, @(1)) | Out-Null
    Wait-Until -TimeoutMs 30000 -Condition { [int]$gridVisibleCountMethod.Invoke($mainForm, @()) -eq 10 }
    $gridCheckVisibleMethod.Invoke($mainForm, @()) | Out-Null
    $gridRestoreCheckedMethod.Invoke($mainForm, @()) | Out-Null

    $postRestoreImagePaths = Get-PrivateFieldValue -Target $mainForm -FieldName 'imagePaths'
    $restoredStatusCount = [int]$gridStatusCountMethod.Invoke($mainForm, @(2))
    if ($postRestoreImagePaths.Count -ne 12 -or $restoredStatusCount -lt 10) {
        throw "Restore result is invalid: queue=$($postRestoreImagePaths.Count) restored=$restoredStatusCount"
    }

    Invoke-PrivateMethod -Target $mainForm -MethodName 'UndoLastMoveAction' | Out-Null
    $postUndoRestoreImagePaths = Get-PrivateFieldValue -Target $mainForm -FieldName 'imagePaths'
    $excludedAfterUndoRestore = [int]$gridStatusCountMethod.Invoke($mainForm, @(1))
    if ($postUndoRestoreImagePaths.Count -ne 2 -or $excludedAfterUndoRestore -ne 10) {
        throw "Undo restore result is invalid: queue=$($postUndoRestoreImagePaths.Count) excluded=$excludedAfterUndoRestore"
    }

    Invoke-PrivateMethod -Target $mainForm -MethodName 'UndoLastMoveAction' | Out-Null
    $postUndoExcludeImagePaths = Get-PrivateFieldValue -Target $mainForm -FieldName 'imagePaths'
    if ($postUndoExcludeImagePaths.Count -ne 12) {
        throw "Undo exclude result is invalid: queue=$($postUndoExcludeImagePaths.Count)"
    }

    Write-Host ("RESULT4: exclude_queue={0} restore_queue={1} undo_restore_queue={2} final_queue={3}" -f $postExcludeImagePaths.Count, $postRestoreImagePaths.Count, $postUndoRestoreImagePaths.Count, $postUndoExcludeImagePaths.Count)
    Write-Host 'IMAGE_MOVE_LARGE_LIST_SMOKE_OK'
}
finally {
    [System.Windows.Forms.Application]::remove_ThreadException($threadExceptionHandler)
    if ($browserForm -ne $null -and -not $browserForm.IsDisposed) {
        $browserForm.Close()
        $browserForm.Dispose()
    }

    if ($historyReloadForm -ne $null -and -not $historyReloadForm.IsDisposed) {
        $historyReloadForm.Close()
        $historyReloadForm.Dispose()
    }

    if ($mainForm -ne $null -and -not $mainForm.IsDisposed) {
        $mainForm.Close()
        $mainForm.Dispose()
    }

    foreach ($path in @($mixedRoot, $prefetchRoot, $historyRoot, $historySettingDir, $summaryRoot, $syntheticRoot, $gridPagingRoot, $gridOpsSourceRoot, $gridOpsExcludeRoot)) {
        if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path)) {
            Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
