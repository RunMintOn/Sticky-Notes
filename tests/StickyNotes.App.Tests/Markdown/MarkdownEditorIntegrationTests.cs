using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using StickyNotes.App.Markdown;
using StickyNotes.App.Services;

namespace StickyNotes.App.Tests.Markdown;

public sealed class MarkdownEditorIntegrationTests
{
    [Fact]
    public void ImageInsertionAndLanguageResourcesWorkWithoutDesktopInput()
    {
        RunOnStaThread(() =>
        {
            var application = new App();
            application.InitializeComponent();

            var editor = new MarkdownEditor();
            const string path = "attachments/note/image.png";
            editor.InsertMarkdownImage(path);
            Assert.Equal($"![image]({path})", editor.Text);

            var english = LoadDictionary("Strings.en.xaml");
            var chinese = LoadDictionary("Strings.zh-CN.xaml");
            var englishKeys = english.Keys.Cast<object>().Select(key => key.ToString()).Order().ToArray();
            var chineseKeys = chinese.Keys.Cast<object>().Select(key => key.ToString()).Order().ToArray();
            Assert.Equal(englishKeys, chineseKeys);

            var originalDataDirectory = Environment.GetEnvironmentVariable("WIN_STICKY_NOTES_DATA_DIR");
            var dataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("WIN_STICKY_NOTES_DATA_DIR", dataDirectory);
            try
            {
                var settings = new UserSettings();
                settings.ValuesChanged += (_, _) => ApplicationResourceUpdater.Apply(settings);
                ApplicationResourceUpdater.Apply(settings);
                Assert.Equal("Settings", Application.Current.FindResource("SettingsText"));

                settings.Language = "中文";
                Assert.Equal("设置", Application.Current.FindResource("SettingsText"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("WIN_STICKY_NOTES_DATA_DIR", originalDataDirectory);
                Directory.Delete(dataDirectory, recursive: true);
            }
        });
    }

    private static ResourceDictionary LoadDictionary(string fileName) => new()
    {
        Source = new Uri($"/StickyNotes.App;component/Resources/{fileName}", UriKind.Relative)
    };

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
