using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        const string URI_SCHEME = "d10";
        const string URI_KEY = "URL: Test Scheme";
        const string PIPE_NAME = "TestAPP";
        CancellationTokenSource ctr;
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            ctr = new CancellationTokenSource();

            await SingleInstanceChecker.EnsureIsSingleInstance(PIPE_NAME,
                () => RegisterUriScheme(),
                () => SendTextToPipe(e.Args[1], () => { }, ctr.Token));


            base.OnStartup(e);

            Task.Run(async () =>
            {
                while (true)
                {
                    await ReceiveTextFromPipe(ctr.Token);
                };
            });
        }



        void RegisterUriScheme()
        {
            string location = typeof(App).Assembly.Location;

            EnsureKeyExists(Registry.CurrentUser, $"Software/Classes/{URI_SCHEME}", "URL:BadCo Applications");
            SetValue(Registry.CurrentUser, $"Software/Classes/{URI_SCHEME}", "URL Protocol", string.Empty);
            EnsureKeyExists(Registry.CurrentUser, $"Software/Classes/{URI_SCHEME}/DefaultIcon", $"{location},1");
            EnsureKeyExists(Registry.CurrentUser, $"Software/Classes/{URI_SCHEME}/shell/open/command", $"\"{location}\" \"%1\"");
        }


        // ##########################

        private async Task<string> ReceiveTextFromPipe(CancellationToken cancellationToken)
        {
            string receivedText;

            PipeSecurity ps = new PipeSecurity();
            System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
            PipeAccessRule par = new PipeAccessRule(sid, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            ps.AddAccessRule(par);

            using (var pipeStream = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 4096, 4096, ps))
            {
                await pipeStream.WaitForConnectionAsync(cancellationToken);

                using (var streamReader = new StreamReader(pipeStream))
                {
                    receivedText = await streamReader.ReadToEndAsync();
                }
            }

            return receivedText;
        }

        private async Task SendTextToPipe(string msg, Action onSendFailed, CancellationToken cancellationToken)
        {
            using (var client = new NamedPipeClientStream(PIPE_NAME))
            {
                try
                {
                    var millisecondsTimeout = 2000;
                    await client.ConnectAsync(millisecondsTimeout);
                }
                catch (Exception)
                {
                    onSendFailed();
                    return;
                }

                if (!client.IsConnected)
                {
                    onSendFailed();
                }

                using (StreamWriter writer = new StreamWriter(client))
                {
                    writer.Write(msg);
                    writer.Flush();
                }
            }
        }

        // ##########################

        private void SetValue(RegistryKey rootKey, string keys, string valueName, string value)
        {
            var key = this.EnsureKeyExists(rootKey, keys);
            key.SetValue(valueName, value);
        }

        private RegistryKey EnsureKeyExists(RegistryKey rootKey, string keys, string defaultValue = null)
        {
            if (rootKey == null)
            {
                throw new Exception("Root key is (null)");
            }

            var currentKey = rootKey;
            foreach (var key in keys.Split('/'))
            {
                currentKey = currentKey.OpenSubKey(key, RegistryKeyPermissionCheck.ReadWriteSubTree)
                             ?? currentKey.CreateSubKey(key, RegistryKeyPermissionCheck.ReadWriteSubTree);

                if (currentKey == null)
                {
                    throw new Exception("Could not get or create key");
                }
            }

            if (defaultValue != null)
            {
                currentKey.SetValue(string.Empty, defaultValue);
            }

            return currentKey;
        }

        // ##########################

        internal class SingleInstanceChecker
        {
            private static Mutex Mutex { get; set; }

            public static async Task EnsureIsSingleInstance(string id, Action onIsSingleInstance, Func<Task> onIsSecondaryInstance)
            {
                SingleInstanceChecker.Mutex = new Mutex(true, id, out var isOnlyInstance);
                if (!isOnlyInstance)
                {
                    await onIsSecondaryInstance();
                    Application.Current.Shutdown(0);
                }
                else
                {
                    onIsSingleInstance();
                }
            }
        }


    }

}
