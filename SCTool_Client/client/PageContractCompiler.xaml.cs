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
    public partial class ContractCompiler : Page
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
          
        }
        public static string CalcScriptHashString(byte[] script)
        {
            var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hash256 = sha256.ComputeHash(script);
            var ripemd160 = new NEO.Cryptography.Cryptography.RIPEMD160Managed();
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
