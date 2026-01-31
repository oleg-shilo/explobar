using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Explobar;

public class MyButton : CustomButton
{
    public MyButton()
    {
        IconIndex = 276;
        IconPath = "shell32.dll";
        Tooltip = "My Plugin";
    }

    public override void OnClick(ClickArgs args)
    {
        MessageBox.Show("Plugin executed!");
    }
}

public class FolderContentButton : CustomButton
{
    public FolderContentButton()
    {
        IconIndex = 19;
        IconPath = "shell32.dll";
        Tooltip = "Copy to clipboard the list of the folder content";
    }

    public override void OnClick(ClickArgs args)
    {
        var folderContent = new StringBuilder();

        foreach (var item in Directory.GetDirectories(args.Context.RootPath))
            folderContent.AppendLine($"d: {item}");

        foreach (var item in Directory.GetFiles(args.Context.RootPath))
            folderContent.AppendLine($"f: {item}");

        Clipboard.SetText(folderContent.ToString());

        MessageBox.Show("Clipboard is updated with the names of the folder content.");
    }
}