using Newtonsoft.Json.Linq;
using System;
using System.Windows.Forms;

namespace frpAdmin
{
    public partial class AddForm : Form
    {
        public AddForm()
        {
            InitializeComponent();
        }

        private void AddForm_Shown(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string AddItemName = textBox1.Text;
            string AddItemPact = comboBox1.Text;
            int AddItemRemotePort = int.Parse(string.IsNullOrEmpty(textBox2.Text) ? "0" : textBox2.Text);
            int AddItemLocalPort = int.Parse(string.IsNullOrEmpty(textBox3.Text) ? "0" : textBox3.Text);
            string AddItemRemoteIPandPort = textBox4.Text;
            string AddItemServerPassword = textBox5.Text;

            if (AddItemRemotePort < 1 || AddItemRemotePort > 65536) {
                MessageBox.Show("远程端口最小值不能低于1最大不能高于65536！","发生错误",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }

            if (AddItemLocalPort < 1 || AddItemLocalPort > 65536)
            {
                MessageBox.Show("远程端口最小值不能低于1最大不能高于65536！", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (AddItemName == "" || AddItemRemoteIPandPort == "") {
                MessageBox.Show("信息不完整，添加失败！", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] AddItemRemoteIPandPortEx = AddItemRemoteIPandPort.Split(':');
            if (AddItemRemoteIPandPortEx.Length != 2 || AddItemRemoteIPandPortEx.Length > 2) 
            {
                MessageBox.Show("远程地址填写错误！正确格式: \n IP(你的服务器公网IP):Port(你的服务器监听端口)", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 锁定界面并且执行写入

            this.Enabled = false;

            JObject AddResult = frpAdmin.Common.DateBaseAddItem(AddItemName, AddItemPact, AddItemRemoteIPandPortEx[0],int.Parse(AddItemRemoteIPandPortEx[1]), AddItemServerPassword, AddItemRemotePort, AddItemLocalPort);

            this.Enabled = true;

            if (AddResult["code"].ToString() == "success") {
                MessageBox.Show($"计划{AddItemName}已经成功添加！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
                return;
            }

            MessageBox.Show($"添加失败，{AddResult["message"].ToString()}", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
