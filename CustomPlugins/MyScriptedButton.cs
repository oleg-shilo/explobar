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
        Tooltip = "My Scripted Plugin";
    }

    public override void OnClick(ClickArgs args)
    {
        // hjkghjkh
        MessageBox.Show("Scripted Plugin executed$$$$");
    }
}