using Hma.Application.Abstractions.Import;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Dispatching.Import;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Reporting;
using Microsoft.Win32;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.ViewModels;

public partial class DispatchImportWorkspaceViewModel(
    DispatchImportService importer,
    IDispatchImportParser parser,
    ICurrentUser current,
    IUserPrompt prompt, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private DispatchImportSheetModel? selectedSheet;
    [ObservableProperty] private string? fileName;

    public ObservableCollection<DispatchImportSheetModel> Sheets { get; } = [];

    public override bool HasUnsavedChanges => Sheets.Any(s => s.Items.Count > 0);

    public override Task LoadAsync()
    {
        UsePermissions(current, ScreenKeys.DispatchOrders);
        UsePrompt(prompt);
        if (Sheets.Count == 0)
            ResetOpsBoardSheets();
        return Task.CompletedTask;
    }

    public void ClearDrafts()
    {
        foreach (var sheet in Sheets)
            sheet.Clear();
        FileName = null;
        ResetOpsBoardSheets();
    }

    [RelayCommand]
    private void DownloadTemplate()
    {
        if (!CanCreate) return;
        var dlg = new SaveFileDialog
        {
            Title = "Lưu mẫu nhập lệnh",
            Filter = "Excel|*.xlsx",
            FileName = "Bang-dieu-xe.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        using var stream = File.Create(dlg.FileName);
        parser.WriteTemplate(stream);
        Status = "Đã tải mẫu Excel.";
        ShowToast(Status);
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        if (!CanCreate) return;
        var dlg = new OpenFileDialog
        {
            Title = "Mở file nhập lệnh",
            Filter = "Excel|*.xlsx;*.xls"
        };
        if (dlg.ShowDialog() != true) return;
        await RunAsync(async () =>
        {
            await using var stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var rows = parser.Parse(stream).Select(DispatchImportDraft.FromRow).ToList();
            Distribute(rows);
            FileName = Path.GetFileName(dlg.FileName);
            var count = AllDrafts().Count();
            Status = $"{count} dòng từ {FileName}.";
            if (count == 0)
                throw new InvalidOperationException("File không có dòng lệnh nào.");
            await CheckCoreAsync();
        });
    }

    [RelayCommand]
    private async Task CheckRows()
    {
        if (!CanCreate) return;
        if (!AllDrafts().Any())
        {
            ShowToast("Chưa mở file hoặc chưa có dòng.", isError: true);
            return;
        }

        await RunAsync(CheckCoreAsync);
    }

    [RelayCommand]
    private async Task ImportSelected()
    {
        if (!CanCreate) return;
        var chosen = AllDrafts().Where(r => r.IsSelected).ToList();
        if (chosen.Count == 0)
        {
            ShowToast("Chọn ít nhất một dòng để nhập.", isError: true);
            return;
        }

        DispatchImportResult? imported = null;
        await RunAsync(async () =>
        {
            var all = AllDrafts().ToList();
            var check = await importer.CheckAsync(all.Select(d => d.ToRow()).ToList());
            ApplyCheck(all, check, syncSelection: false);
            RefreshSheetHeaders();
            if (check.FileError is not null)
                throw new InvalidOperationException(check.FileError);
            var stillOk = chosen.Where(d => d.IsValid && d.IsSelected).Select(d => d.ToRow()).ToList();
            if (stillOk.Count == 0)
                throw new InvalidOperationException("Các dòng đã chọn vẫn lỗi. Sửa rồi kiểm tra lại.");
            imported = await importer.ImportAsync(stillOk);
            var again = await importer.CheckAsync(AllDrafts().Select(d => d.ToRow()).ToList());
            ApplyCheck(AllDrafts().ToList(), again, syncSelection: true);
            RefreshSheetHeaders();
        });
        if (imported is null)
            return;
        var skipped = chosen.Count - imported.Saved;
        Status = skipped > 0
            ? $"Đã nhập {imported.Saved} lệnh. Bỏ qua {skipped} dòng lỗi."
            : $"Đã nhập {imported.Saved} lệnh.";
        ShowToast(Status, isError: skipped > 0);
    }

    private async Task CheckCoreAsync()
    {
        var all = AllDrafts().ToList();
        var result = await importer.CheckAsync(all.Select(d => d.ToRow()).ToList());
        ApplyCheck(all, result, syncSelection: true);
        RefreshSheetHeaders();
        if (result.FileError is not null)
            throw new InvalidOperationException(result.FileError);
        Status = $"Đúng {result.ValidCount} dòng, lỗi {result.ErrorCount} dòng.";
        ShowToast(Status, isError: result.ErrorCount > 0);
    }

    private static void ApplyCheck(IReadOnlyList<DispatchImportDraft> drafts, DispatchImportCheckResult result, bool syncSelection)
    {
        foreach (var draft in drafts)
        {
            var hit = result.Rows.FirstOrDefault(c =>
                c.Row.ExcelRow == draft.ExcelRow
                && string.Equals(c.Row.SheetName, draft.SheetName, StringComparison.Ordinal)
                && c.Row.StartColumn == draft.StartColumn);
            draft.ApplyErrors(hit?.Errors ?? (result.FileError is null ? [] : [result.FileError]));
            if (syncSelection)
                draft.IsSelected = draft.IsValid;
        }
    }

    private void Distribute(IReadOnlyList<DispatchImportDraft> rows)
    {
        if (rows.Count == 0 || rows.All(r => string.IsNullOrWhiteSpace(r.SheetName)))
        {
            Sheets.Clear();
            var sheet = new DispatchImportSheetModel("Nhập");
            sheet.Replace(rows);
            Sheets.Add(sheet);
            SelectedSheet = sheet;
            return;
        }

        ResetOpsBoardSheets();
        var grouped = rows.GroupBy(r => r.SheetName ?? "", StringComparer.Ordinal);
        foreach (var group in grouped)
        {
            var sheet = Sheets.FirstOrDefault(s => string.Equals(s.Title, group.Key, StringComparison.Ordinal));
            if (sheet is null)
            {
                sheet = new DispatchImportSheetModel(group.Key);
                Sheets.Add(sheet);
            }

            sheet.Replace(group);
        }

        SelectedSheet = Sheets.FirstOrDefault(s => s.Items.Count > 0) ?? Sheets.FirstOrDefault();
    }

    private void ResetOpsBoardSheets()
    {
        Sheets.Clear();
        foreach (var name in DispatchImportParser.OpsBoardSheetNames)
            Sheets.Add(new DispatchImportSheetModel(name));
        SelectedSheet = Sheets.Count > 0 ? Sheets[0] : null;
    }

    private void RefreshSheetHeaders()
    {
        foreach (var sheet in Sheets)
            sheet.RefreshAllSelected();
    }

    private IEnumerable<DispatchImportDraft> AllDrafts() =>
        Sheets.SelectMany(s => s.Items);
}
