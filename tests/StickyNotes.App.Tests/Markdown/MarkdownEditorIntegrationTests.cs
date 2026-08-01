using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
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
            editor.ApplyTemplate();
            editor.Measure(new Size(500, 500));
            editor.Arrange(new Rect(0, 0, 500, 500));
            var textEditor = FindDescendant<TextEditor>(editor);
            Assert.NotNull(textEditor);
            textEditor.TextArea.TextView.EnsureVisualLines();
            var imageLine = Assert.Single(textEditor.TextArea.TextView.VisualLines);
            var syntaxTop = imageLine.GetVisualPosition(
                0,
                ICSharpCode.AvalonEdit.Rendering.VisualYPosition.TextTop).Y;
            Assert.True(syntaxTop < textEditor.TextArea.TextView.DefaultLineHeight);
            Assert.True(imageLine.Height > 80);

            editor.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                () => { });
            var attachment = FindDescendants<FrameworkElement>(editor).FirstOrDefault(element =>
            {
                var name = System.Windows.Automation.AutomationProperties.GetName(element);
                return name is "Loading image…" or "Image unavailable";
            });
            Assert.NotNull(attachment);
            var syntaxBottom = imageLine.GetVisualPosition(
                0,
                ICSharpCode.AvalonEdit.Rendering.VisualYPosition.TextBottom).Y;
            var attachmentTop = attachment.TranslatePoint(
                new Point(),
                textEditor.TextArea.TextView).Y + textEditor.TextArea.TextView.VerticalOffset;
            Assert.True(attachmentTop >= syntaxBottom);

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

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject =>
        FindDescendants<T>(parent).FirstOrDefault();

    private static IEnumerable<T> FindDescendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var nested in FindDescendants<T>(child)) yield return nested;
        }
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
