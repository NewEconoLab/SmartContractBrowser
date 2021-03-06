﻿using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// ContractDebug.xaml 的交互逻辑
    /// </summary>
    public partial class ContractDebug : Page
    {
        public ContractDebug()
        {
            InitializeComponent();
        }
        public Neo.Debug.DebugTool debugtool = new Neo.Debug.DebugTool();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string pathLog = textLogPath.Text;
            string pathScript = textScriptDebug.Text;
            this.listLoadInfo.Items.Clear();
            try
            {
                debugtool.Load(pathLog, pathScript, this.textTid.Text);
                this.listLoadInfo.Items.Add("load finish");
                List<string> scriptnames = new List<string>();
                debugtool.fullLog.script.GetAllScriptName(scriptnames);
                foreach (var s in scriptnames)
                {
                    var b = debugtool.LoadScript(s);
                    this.listLoadInfo.Items.Add("script:" + b + ":" + s);
                }

                InitTreeCode();
                InitCareList();
            }
            catch (Exception err)
            {

                this.listLoadInfo.Items.Add(err.Message);
            }
        }
        void InitTreeCode()
        {
            treeCode.Items.Clear();
            TreeViewItem item = new TreeViewItem();
            item.Header = "Execute Order:" + debugtool.fullLog.state.ToString();
            treeCode.Items.Add(item);
            if (string.IsNullOrEmpty(debugtool.fullLog.error) == false)
            {
                TreeViewItem erritem = new TreeViewItem();
                erritem.Header = "error:" + debugtool.fullLog.error;
                treeCode.Items.Add(erritem);
            }
            {
                TreeViewItem resultitem = new TreeViewItem();
                resultitem.Header = "result:" + debugtool.fullLog.state.ToString();
                treeCode.Items.Add(resultitem);
            }
            TreeViewItem itemScript = new TreeViewItem();
            item.Items.Add(itemScript);
            FillTreeScript(itemScript, debugtool.fullLog.script);
            item.ExpandSubtree();
            item.IsSelected = true;
            item.BringIntoView();
        }
        void InitCareList()
        {
            listCare.Items.Clear();
            foreach (var c in debugtool.simvm.careinfo)
            {
                listCare.Items.Add(c);
            }
        }
        void SetTreeDataItem(ItemCollection parent, Neo.SmartContract.Debug.StackItem item)
        {
            if (item == null)
            {
                TreeViewItem titem = new TreeViewItem();
                titem.Header = "<null>";
                parent.Add(titem);
                return;
            }
            {//type
                TreeViewItem titem = new TreeViewItem();
                titem.Header = "deftype:" + item.type;
                parent.Add(titem);
            }
            if (item.type != "Array" && item.type != "Struct")
            {
                {//value
                    TreeViewItem vitem = new TreeViewItem();
                    vitem.Header = "value:" + item.strvalue;
                    parent.Add(vitem);
                }
                if (item.type == "ByteArray")//asstr
                {
                    var bt = Neo.Debug.DebugTool.HexString2Bytes(item.strvalue);
                    {
                        var asstr = System.Text.Encoding.UTF8.GetString(bt);
                        TreeViewItem vitem = new TreeViewItem();
                        vitem.Header = "asStr:" + asstr;
                        parent.Add(vitem);
                    }
                    if (bt.Length <= 8)
                    {
                        System.Numerics.BigInteger num = new System.Numerics.BigInteger(bt);
                        TreeViewItem vitem = new TreeViewItem();
                        vitem.Header = "asNum:" + num.ToString();
                        parent.Add(vitem);
                    }
                }
            }
            else
            {
                {//value
                    TreeViewItem vitem = new TreeViewItem();
                    vitem.Header = "subitems:" + item.subItems.Count;
                    parent.Add(vitem);
                    vitem.IsExpanded = true;
                    for (var i = 0; i < item.subItems.Count; i++)
                    {
                        TreeViewItem sitem = new TreeViewItem();
                        vitem.Items.Add(sitem);
                        sitem.Header = "item(" + i + ")";
                        sitem.IsExpanded = true;

                        SetTreeDataItem(sitem.Items, item.subItems[i]);
                    }
                }
            }

        }
        void SetTreeData(Neo.SmartContract.Debug.StackItem item)
        {
            treeData.Items.Clear();
            SetTreeDataItem(treeData.Items, item);

        }
        void FillTreeScript(TreeViewItem treeitem, Neo.SmartContract.Debug.LogScript script)
        {
            treeitem.Tag = script;
            treeitem.Header = "script:" + script.hash;
            foreach (var op in script.ops)
            {
                TreeViewItem itemop = new TreeViewItem();

                itemop.Header = op.GetHeader();
                if (op.op == Neo.VM.OpCode.SYSCALL && op.param != null)
                {
                    string p = System.Text.Encoding.ASCII.GetString(op.param);
                    itemop.Header = op.GetHeader() + " " + p;
                }
                itemop.Tag = op;
                treeitem.Items.Add(itemop);
                if (op.subScript != null)
                {
                    TreeViewItem subscript = new TreeViewItem();
                    itemop.Items.Add(subscript);
                    FillTreeScript(subscript, op.subScript);
                }
            }
        }

        private void treeCode_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Neo.SmartContract.Debug.LogScript script = null;
            Neo.SmartContract.Debug.LogOp logop = null;
            if (treeCode.SelectedItem != null)
            {
                var treenode = treeCode.SelectedItem as TreeViewItem;
                var treenodep = treenode != null ? treenode.Parent as TreeViewItem : null;
                script = treenode.Tag as Neo.SmartContract.Debug.LogScript;
                logop = treenode.Tag as Neo.SmartContract.Debug.LogOp;
                if (script == null && treenodep != null)
                    script = treenodep.Tag as Neo.SmartContract.Debug.LogScript;
            }
            listStack.Items.Clear();
            listAltStack.Items.Clear();
            var stateid = -1;
            if (logop != null)
            {
                if (this.debugtool.simvm.mapState.ContainsKey(logop))
                {
                    stateid = debugtool.simvm.mapState[logop];
                    if (debugtool.simvm.stateClone.ContainsKey(stateid))
                    {
                        var state = debugtool.simvm.stateClone[stateid];
                        foreach (var l in state.CalcStack)
                        {
                            listStack.Items.Add(l);
                        }
                        foreach (var l in state.AltStack)
                        {
                            listAltStack.Items.Add(l);
                        }
                    }
                }
            }
            if (script == null)
            {
                selectScript.Content = "未选中脚本";

            }
            else
            {
                selectScript.Content = "选中脚本" + script.hash;
                if (debugtool.scripts.ContainsKey(script.hash))
                {
                    selectScriptDebug.Content = "有调试信息";
                    var debugscript = debugtool.scripts[script.hash];

                    if (debugscript != this.listBoxASM.Tag)
                    {
                        this.listBoxASM.Tag = debugscript;
                        this.listBoxASM.Items.Clear();
                        foreach (var op in debugscript.codes)
                        {
                            this.listBoxASM.Items.Add(op);
                        }

                        this.codeEdit.Text = debugscript.srcfile;
                    }
                    if (logop != null)
                    {
                        foreach (Neo.Compiler.Op op in listBoxASM.Items)
                        {
                            if (op.addr == (ushort)logop.addr)
                            {
                                listBoxASM.SelectedItem = op;
                                listBoxASM.ScrollIntoView(op);
                                break;
                            }

                        }

                    }
                }
                else
                {
                    selectScriptDebug.Content = "没有调试信息";
                    this.listBoxASM.Tag = null;
                    this.listBoxASM.Items.Clear();
                    this.codeEdit.Text = "";
                }
            }
        }

        private void listBoxASM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var op = this.listBoxASM.SelectedItem as Neo.Compiler.Op;
            if (op == null) return;
            var tag = this.listBoxASM.Tag as Neo.Debug.DebugScript;
            if (tag == null) return;
            var line = tag.maps.GetLineBack(op.addr);
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
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //高亮功能
            ICSharpCode.AvalonEdit.TextEditor code = codeEdit;
            codeEdit.TextArea.TextView.BackgroundRenderers.Add(new HighlightCurrentLineBackgroundRenderer(code));

        }

        private void listStack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetTreeData(listStack.SelectedItem as Neo.SmartContract.Debug.StackItem);
        }

        private void listAltStack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetTreeData(listAltStack.SelectedItem as Neo.SmartContract.Debug.StackItem);
        }

        private void listCare_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var data = listCare.SelectedItem as Neo.Debug.CareItem;
            SetTreeData(data.item);

        }
    }
}
