using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace WpfApp1
{
    public class SideApp : Application
    {
        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
        }
    }
}
