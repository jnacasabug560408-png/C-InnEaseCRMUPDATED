using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"c:\Users\mike\source\repos\INNEASE.CRM\INNEASE.CRM\FormMain.cs";
        string content = File.ReadAllText(path);

        content = content.Replace("â”€", "─");
        content = content.Replace("â€”", "—");
        content = content.Replace("â˜°", "☰");

        File.WriteAllText(path, content, new UTF8Encoding(true));
        Console.WriteLine("Done.");
    }
}
