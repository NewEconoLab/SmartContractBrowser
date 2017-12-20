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

namespace client
{
    /// <summary>
    /// PageWelcome.xaml 的交互逻辑
    /// </summary>
    public partial class PageWelcome : Page
    {
        public PageWelcome()
        {
            InitializeComponent();
        }

        private void textAvm_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textAvm.Text.Length % 2 != 0)//必须是双数
                return;
            if (listASM == null)
                return;
            byte[] data = null;
            try
            {
                data = ThinNeo.Helper.HexString2Bytes(textAvm.Text);
            }
            catch (Exception err)
            {
                MessageBox.Show("string->hex error:" + err.Message);
                return;
            }
            ThinNeo.Compiler.Op[] ops = null;
            try
            {
                ops = ThinNeo.Compiler.Avm2Asm.Trans(data);
            }
            catch (Exception err)
            {
                MessageBox.Show("avm->asm error:" + err.Message);
                return;
            }
            listASM.Items.Clear();
            foreach (var op in ops)
            {
                try
                {
                    var str = op.ToString();
                    listASM.Items.Add(op);
                }
                catch
                {
                    listASM.Items.Add("format error:");

                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {//load avm from file
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "*.avm|*.avm";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var bytes = System.IO.File.ReadAllBytes(ofd.FileName);
                    this.textAvm.Text = ThinNeo.Helper.Bytes2HexString(bytes);
                }
                catch (Exception err)
                {
                    MessageBox.Show("load file error:" + ofd.FileName);
                    return;
                }
            }
        }
    }
}