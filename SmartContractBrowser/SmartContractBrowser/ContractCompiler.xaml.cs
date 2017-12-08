using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
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
    /// ContractCompiler.xaml 的交互逻辑
    /// </summary>
    public partial class ContractCompiler : Page, Neo.Compiler.ILogger
    {
        public ContractCompiler()
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
        public class HighlightCurrentLineBackgroundRenderer : IBackgroundRenderer
        {
            private TextEditor _editor;

            public HighlightCurrentLineBackgroundRenderer(TextEditor editor)
            {
                _editor = editor;
            }

            public KnownLayer Layer
            {
                get { return KnownLayer.Selection; }
            }

            public void Draw(TextView textView, DrawingContext drawingContext)
            {
                if (_editor.Document == null)
                    return;

                textView.EnsureVisualLines();
                var currentLine = _editor.Document.GetLineByOffset(_editor.CaretOffset);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, currentLine))
                {
                    drawingContext.DrawRectangle(
                        new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0xFF)), null,
                        new Rect(rect.Location, new Size(textView.ActualWidth - 32, rect.Height)));
                }
            }
        }
        Result buildResult = null;
        Neo.Debug.Helper.AddrMap debugInfo = null;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "*.dll|*.dll";
            if (ofd.ShowDialog() == true)
            {
                buildResult = null;
                labelDll.Content = ofd.FileName;
                try
                {
                    this.buildResult = CompileDll(this, ofd.FileName);

                    this.codeEdit.Load(buildResult.srcfile);
                    this.Log("cs file=" + buildResult.srcfile);
                    StringBuilder hexscript = new StringBuilder();
                    foreach (var b in buildResult.avm)
                    {
                        hexscript.Append(b.ToString("x02"));
                    }
                    textHexScript.Text = hexscript.ToString();
                    textScriptHash.Text = buildResult.script_hash;
                    textDebugInfo.Text = buildResult.debuginfo;
                    var ops = Neo.Compiler.Avm2Asm.Trans(buildResult.avm);
                    debugInfo = Neo.Debug.Helper.AddrMap.FromJsonStr(buildResult.debuginfo);
                    listASM.Items.Clear();
                    foreach (var o in ops)
                    {
                        listASM.Items.Add(o);
                    }
                    if (ops.Length > 0 && ops.Last().error == true)
                    {
                        listASM.Items.Add("fail end.");
                    }
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

        private static Result CompileDll(Neo.Compiler.ILogger logger, string name)
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
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ICSharpCode.AvalonEdit.TextEditor code = codeEdit;
            codeEdit.TextArea.TextView.BackgroundRenderers.Add(
    new HighlightCurrentLineBackgroundRenderer(code));
            codeEdit.TextArea.Caret.PositionChanged += (s, ee) =>
              {
                  if (this.debugInfo == null)
                      return;
                  var pos = codeEdit.CaretOffset;
                  var line = codeEdit.Document.GetLineByOffset(pos).LineNumber;
                  var addr = this.debugInfo.GetAddrBack(line);
                  if (addr >= 0)
                  {
                      foreach (Neo.Compiler.Op item in this.listASM.Items)
                      {
                          if (item != null && item.addr == addr)
                          {
                              this.listASM.SelectedItem = item;
                              this.listASM.ScrollIntoView(item);
                              break;
                          }
                      }
                  }
              };
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {//save button
            var filename = this.buildResult.script_hash;
            var targetpath = textTargetScriptPath.Text;
            if (System.IO.Directory.Exists(targetpath) == false)
                System.IO.Directory.CreateDirectory(targetpath);

            var targetSrcFile = System.IO.Path.Combine(targetpath, filename + ".cs");
            var targetAvmFile = System.IO.Path.Combine(targetpath, filename + ".avm");
            var targetDebugFile = System.IO.Path.Combine(targetpath, filename + ".debug.json");
            System.IO.File.Delete(targetSrcFile);
            System.IO.File.Delete(targetAvmFile);
            System.IO.File.Delete(targetDebugFile);

            System.IO.File.Copy(this.buildResult.srcfile, targetSrcFile);
            System.IO.File.WriteAllBytes(targetAvmFile, this.buildResult.avm);
            System.IO.File.WriteAllText(targetDebugFile, this.buildResult.debuginfo);
        }

        private void listASM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var op = this.listASM.SelectedItem as Neo.Compiler.Op;
            if (op == null) return;
            var line = this.debugInfo.GetLineBack(op.addr);
            textAsm.Text = "srcline=" + line;
            if (line > 0)
            {
                var ioff = this.codeEdit.Document.Lines[line - 1].Offset;
                var len = this.codeEdit.Document.Lines[line - 1].Length;
                this.codeEdit.CaretOffset = ioff;
                //this.codeEdit.Select(ioff, 0);
                this.codeEdit.ScrollToLine(line - 1);
                codeEdit.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
            }

        }
    }
}
