namespace StickyNotes.App;

public sealed record HelpEntry(string Name, string Description, string Shortcut = "");
public sealed record HelpGroup(string Title, IReadOnlyList<HelpEntry> Entries);
public sealed record HelpPageContent(string Title, string Introduction, IReadOnlyList<HelpGroup> Groups)
{
    public static HelpPageContent Create(string language) => language == "中文" ? Chinese : English;

    private static HelpPageContent English { get; } = new(
        "Help & shortcuts",
        "A quick reference for editing and managing your notes.",
        [
            new("Notes", [
                new("New note", "Create and open a new note.", "Ctrl+N"),
                new("Find open notes", "Bring every open Note Window in front of ordinary windows."),
                new("Pin a note", "Use the pin in a Note Window to keep that note above other windows."),
                new("Note Card menu", "Right-click a Note Card to open, close, or delete it."),
                new("Search", "Filter Note Cards by their text."),
                new("Help", "Open this page from anywhere.", "F1")
            ]),
            new("Formatting", [
                new("Bold", "Format the selection, or the current line when nothing is selected.", "Ctrl+B"),
                new("Highlight", "Wrap text with ==highlight markers==.", "Ctrl+H"),
                new("Strikethrough", "Wrap text with ~~strikethrough markers~~.", "Ctrl+D"),
                new("Inline code", "Select text and type one backtick."),
                new("Code block", "Select one or more lines and type three backticks."),
                new("Horizontal rule", "Type --- on its own line.")
            ]),
            new("Lists & media", [
                new("Continue lists", "Enter continues bullets, numbers, tasks, and quotes. Enter on an empty item exits."),
                new("Task list", "Use - [ ] for an open task and - [x] for a completed task."),
                new("Images", "Paste an image or use the image command. A standalone ![image](path) line shows the image below its editable Markdown; select it for a larger preview and drag the lower-right corner to resize.", "Ctrl+V")
            ]),
            new("Live Preview", [
                new("Edit markers", "Move the caret onto a line to reveal its Markdown markers."),
                new("Pointer reveal", "Optional hover reveal is available in Settings."),
                new("Code copy", "The copy icon copies code content without its fences or language name."),
                new("Local data", "Notes, settings, and imported attachments stay under the local app-data folder.")
            ])
        ]);

    private static HelpPageContent Chinese { get; } = new(
        "帮助与快捷键",
        "便签编辑与管理功能的快速参考。",
        [
            new("便签", [
                new("新建便签", "创建并打开一个新便签。", "Ctrl+N"),
                new("找回打开的便签", "将所有打开的便签窗口移动到普通窗口前面。"),
                new("置顶便签", "使用便签窗口中的图钉，让该便签持续位于其他窗口上方。"),
                new("便签卡片菜单", "右键便签卡片可以打开、关闭或删除便签。"),
                new("搜索", "根据正文筛选便签卡片。"),
                new("帮助", "从任意窗口打开本页面。", "F1")
            ]),
            new("文字格式", [
                new("加粗", "有选区时格式化选区，否则格式化光标所在行。", "Ctrl+B"),
                new("高亮", "使用 ==高亮标记== 包裹文字。", "Ctrl+H"),
                new("删除线", "使用 ~~删除线标记~~ 包裹文字。", "Ctrl+D"),
                new("行内代码", "选中文字后输入一个反引号。"),
                new("代码块", "选中一行或多行后连续输入三个反引号。"),
                new("水平分隔线", "单独输入一行 ---。")
            ]),
            new("列表与图片", [
                new("自动续写列表", "回车可续写项目符号、编号、任务和引用；空项目再次回车退出。"),
                new("任务列表", "使用 - [ ] 表示未完成，使用 - [x] 表示已完成。"),
                new("图片", "从剪贴板粘贴图片或使用图片命令；单独成行的 ![image](路径) 会在可编辑语法下显示图片，点击可查看大图，拖动右下角可调整预览尺寸。", "Ctrl+V")
            ]),
            new("实时预览", [
                new("编辑标记", "将光标移到一行即可显示该行的 Markdown 标记。"),
                new("鼠标显示", "可以在设置中选择经过一行时显示标记。"),
                new("复制代码", "代码块右上角按钮只复制正文，不复制围栏和语言名称。"),
                new("本地数据", "便签、设置和导入附件都保存在本地应用数据目录。")
            ])
        ]);
}
