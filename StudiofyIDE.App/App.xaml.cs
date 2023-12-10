using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.WindowsAppRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using WinUIEx;

namespace WindowsCode.Studio
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            int length = 0;
            System.Text.StringBuilder sb = new(0);
            int result = GetCurrentPackageFullName(ref length, sb);
            if (result == 15700L)
            {
                // Not a packaged app. Configure file-based persistence instead
                WindowManager.PersistenceStorage = new FilePersistence("WinUIExPersistence.json");
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, System.Text.StringBuilder packageFullName);

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            DeploymentResult managerStatus = DeploymentManager.GetStatus();
            if (managerStatus.Status == DeploymentStatus.PackageInstallRequired)
            {
                _ = DeploymentManager.Initialize();
            }

            //if (Debugger.IsAttached)
            //{
            //    m_Window = new MainWindow();
            //    m_Window.Show();
            //}
            //else
            //{
            SplashScreen s_Window = new(typeof(MainWindow));
            s_Window.CenterOnScreen();
            _ = s_Window.Show();
            s_Window.Completed += Window_Completed;
            //}
        }

        private void Window_Completed(object sender, Window e)
        {
            m_Window = e;
            m_Window.Maximize();
        }

        private Window m_Window;
    }

    internal class FilePersistence : IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _data = new();
        private readonly string _file;

        public FilePersistence(string filename)
        {
            _file = filename;
            try
            {
                if (File.Exists(filename))
                {
                    JsonObject JsonObj = JsonNode.Parse(File.ReadAllText(filename)) as JsonObject;
                    foreach (KeyValuePair<string, JsonNode> node in JsonObj)
                    {
                        if (node.Value is JsonValue jvalue && jvalue.TryGetValue<string>(out string value))
                        {
                            _data[node.Key] = value;
                        }
                    }
                }
            }
            catch { }
        }
        private void Save()
        {
            JsonObject JsonObj = new();
            foreach (KeyValuePair<string, object> item in _data)
            {
                if (item.Value is string s) // In this case we only need string support. TODO: Support other types
                {
                    JsonObj.Add(item.Key, s);
                }
            }
            File.WriteAllText(_file, JsonObj.ToJsonString());
        }
        public object this[string key] { get => _data[key]; set { _data[key] = value; Save(); } }

        public ICollection<string> Keys => _data.Keys;

        public ICollection<object> Values => _data.Values;

        public int Count => _data.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            _data.Add(key, value); Save();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _data.Add(item.Key, item.Value); Save();
        }

        public void Clear()
        {
            _data.Clear(); Save();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _data.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _data.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException(); // TODO
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException(); // TODO
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException(); // TODO
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException(); // TODO
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            throw new NotImplementedException(); // TODO
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // TODO
        }
    }
}
