using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StickyNotes.App.Models;

namespace StickyNotes.App.Services;

internal sealed class AttachmentService
{
    private readonly string _assetRoot = AppDataDirectory.Resolve();

    internal string AssetRoot => _assetRoot;

    internal string? ImportFromClipboardOrPicker(NoteItem note)
    {
        try
        {
            if (Clipboard.ContainsImage())
            {
                var bitmap = Clipboard.GetImage();
                if (bitmap is null) return null;
                var fileName = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.png";
                var relativePath = RelativePath(note, fileName);
                var fullPath = PreparePath(relativePath);
                using var stream = File.Create(fullPath);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return relativePath.Replace('\\', '/');
            }

            var picker = new OpenFileDialog
            {
                Title = "Insert image",
                Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*"
            };
            if (picker.ShowDialog() != true) return null;

            var extension = Path.GetExtension(picker.FileName);
            var importedName = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}{extension}";
            var importedRelativePath = RelativePath(note, importedName);
            File.Copy(picker.FileName, PreparePath(importedRelativePath));
            return importedRelativePath.Replace('\\', '/');
        }
        catch (Exception)
        {
            // Clipboard formats and selected files are untrusted input. Import failure stays local.
            return null;
        }
    }

    private static string RelativePath(NoteItem note, string fileName) =>
        Path.Combine("attachments", note.Id.ToString("N"), fileName);

    private string PreparePath(string relativePath)
    {
        var fullPath = Path.Combine(_assetRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return fullPath;
    }
}
