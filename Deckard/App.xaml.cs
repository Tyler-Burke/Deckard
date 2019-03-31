using System;
using System.Linq;
using System.Windows;

namespace Deckard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            //Passing case path to Main Window to load case folder from docx files (case notes)
            string casePath = "";
            if (args.Length > 0)
            {
                //args[0] is the case path passed from windows when you open a docx file using Deckard
                casePath = args[0].ToString();
                //Modifying path to parent folder
                var s = casePath.Split('\\');
                s = s.Take(s.Count() - 1).ToArray();
                casePath = String.Join("\\", s);
            }

            var application = new App();
            application.InitializeComponent();

            //If a case path was given (opened via docx file) then open Main Window with the case path, otherwise, no starting case folder.
            if (String.IsNullOrEmpty(casePath))
                application.Run(new MainWindow());
            else
                application.Run(new MainWindow(casePath));
        }
    }
}
