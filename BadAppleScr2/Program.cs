using System;
using System.Windows;
using WindowsRecipes.TaskbarSingleInstance;
using WindowsRecipes.TaskbarSingleInstance.Wpf;

namespace BadAppleScr2
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
