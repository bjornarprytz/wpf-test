using System;

namespace WpfApp1
{
    public class EntryPoint
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new App();
            app.InitializeComponent();

            app.Run();

        }
    }
}
