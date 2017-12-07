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

namespace SmartContractBrowser
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, Neo.Compiler.ILogger
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void Log(string log)
        {
            Action<string> safelog = (_log) =>
             {
                 this.listDebug.Items.Add(_log);
             };
            this.Dispatcher.Invoke(safelog, log);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "*.dll|*.dll";
            if (ofd.ShowDialog() == true)
            {
                labelDll.Content = ofd.FileName;
                try
                {
                    var r = CompileDll(this,ofd.FileName);

                    this.codeEdit.Load(r.srcfile);
                    this.Log("cs file=" + r.srcfile);
                    StringBuilder hexscript = new StringBuilder();
                    foreach (var b in r.avm)
                    {
                        hexscript.Append(b.ToString("x02"));
                    }
                    textHexScript.Text = hexscript.ToString();
                    textScriptHash.Text = r.script_hash;
                    textDebugInfo.Text = r.debuginfo;
                }
                catch (Exception err)
                {
                    MessageBox.Show("ErrCompileDll:" + err.Message);
                }
            }
        }
        public static string CalcScriptHashString(byte[] script)
        {
            var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hash256 = sha256.ComputeHash(script);
            var ripemd160 = new Neo.Cryptography.RIPEMD160Managed();
            var hash = ripemd160.ComputeHash(hash256);
            StringBuilder sb = new StringBuilder();
            sb.Append("0x");
            foreach (var b in hash.Reverse().ToArray())
            {
                sb.Append(b.ToString("x02"));
            }
            return sb.ToString();
        }
        public class Result
        {
            public string script_hash;
            public string srcfile;
            public byte[] avm;
            public string debuginfo;
        }

        private static Result CompileDll(Neo.Compiler.ILogger logger,string name)
        {
            Result r = new Result();
            var namepdb = name.Substring(0, name.Length - 4) + ".pdb";
            if (System.IO.File.Exists(name) == false || System.IO.File.Exists(namepdb) == false)
            {
                throw new Exception("必须同时拥有dll 和 pdb 文件");
            }
            var stream = System.IO.File.OpenRead(name);
            var streampdb = System.IO.File.OpenRead(namepdb);

            Neo.Compiler.MSIL.ILModule module = new Neo.Compiler.MSIL.ILModule();
            module.LoadModule(stream, streampdb);


            Neo.Compiler.MSIL.ModuleConverter converter = new Neo.Compiler.MSIL.ModuleConverter(logger);
            converter.Convert(module);

            string srcfile = null;
            {//gen debug info
                Neo.Compiler.MyJson.JsonNode_Array arr = new Neo.Compiler.MyJson.JsonNode_Array();
                foreach (var m in converter.outModule.mapMethods)
                {
                    Neo.Compiler.MyJson.JsonNode_Object item = new Neo.Compiler.MyJson.JsonNode_Object();
                    arr.Add(item);
                    item.SetDictValue("name", m.Value.displayName);
                    item.SetDictValue("addr", m.Value.funcaddr.ToString("X04"));
                    Neo.Compiler.MyJson.JsonNode_Array infos = new Neo.Compiler.MyJson.JsonNode_Array();
                    item.SetDictValue("map", infos);
                    foreach (var c in m.Value.body_Codes)
                    {
                        if (c.Value.debugcode != null)
                        {
                            var debugcode = c.Value.debugcode.ToLower();
                            if (debugcode.Contains(".cs"))
                            {
                                if (srcfile == null)
                                    srcfile = debugcode;

                                infos.AddArrayValue(c.Value.addr.ToString("X04") + "-" + c.Value.debugline.ToString());
                            }
                        }
                    }
                }
                r.debuginfo = arr.ToString();
            }
            r.srcfile = srcfile;


            {//gen hexscript
                var bytes = converter.outModule.Build();
                var hashstr = CalcScriptHashString(bytes);
                r.avm = bytes;
                r.script_hash = hashstr;
            }
            return r;
        }
    }
}
