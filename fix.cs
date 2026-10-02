using System.IO;
using System.Text;
class Fixer {
    static void Main() {
        var path = @"c:\Users\mike\source\repos\INNEASE.CRM\INNEASE.CRM\FormMain.cs";
        var text = File.ReadAllText(path);
        text = System.Text.RegularExpressions.Regex.Replace(text, ".*?KPI DASHBOARD.*?typeof.*?", "                    items.Add(new MenuItem(\"KPI DASHBOARD\", \"??\", typeof(UcCustomerReports)));");
        text = System.Text.RegularExpressions.Regex.Replace(text, "📊", "??");
        File.WriteAllText(path, text, Encoding.UTF8);
    }
}
