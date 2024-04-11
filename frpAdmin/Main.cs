using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Org.BouncyCastle.Crypto.Digests;
using System.Text;
using System.Reflection;


namespace frpAdmin
{
    public partial class Main : Form
    {
        private static Main instance;
        public static Main Instance => instance;

        public JArray TaskThreadArray;


        public Main()
        {
            InitializeComponent();
            instance = this;
            TaskThreadArray = new JArray();
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            toolStrip1.Enabled = false;
            listView1.Enabled = false;
            new Task(InitApp).Start(); 

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            AddForm FormObject = new AddForm();
            FormObject.ShowDialog();
        }


        // 初始化
        private void InitApp() 
        {
            (toolStripStatusLabel1.Owner as StatusStrip).Invoke(new MethodInvoker(() => {
                toolStripStatusLabel1.Text = "初始化中，当前程序已锁定...";
            }));

            JObject DateBaseData = frpAdmin.Common.DateBaseRead();

            if (DateBaseData["code"].ToString() != "success") 
            {
                MessageBox.Show(DateBaseData["message"].ToString() + "\n请重启软件，重启后仍无效请删除掉datebase目录！", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            foreach (JObject objItem in DateBaseData["data"])
            {
                ListAddItem(
                    int.Parse(objItem["id"].ToString()),
                    objItem["name"].ToString(),
                    objItem["pact"].ToString(),
                    objItem["serverip"].ToString() + ":" + objItem["serverport"].ToString(),
                    objItem["serverpassword"].ToString(),
                    int.Parse(objItem["remoteport"].ToString()),
                    int.Parse(objItem["localport"].ToString())
               );
            }


            (toolStripStatusLabel1.Owner as StatusStrip).Invoke(new MethodInvoker(() => {
                toolStripStatusLabel1.Text = "就绪";
            }));

            toolStrip1.Invoke(new Action(() => toolStrip1.Enabled = true));
            listView1.Invoke(new Action(() => listView1.Enabled = true));
        }


        // 列表 -> 添加一个项目

        public JObject ListAddItem(int ItemIndex, string ItemName,string ItemPact,string ItemRemoteIP,string ItemServerPassword,int ItemRemotePort, int ItemLocalPort) {

            try
            {

                ListViewItem AddList = new ListViewItem();
                AddList.Text = ItemIndex.ToString();
                AddList.SubItems.Add(ItemName);
                AddList.SubItems.Add(ItemPact);
                AddList.SubItems.Add("");
                AddList.SubItems.Add(ItemRemoteIP);
                AddList.SubItems.Add($"远程:{ItemRemotePort.ToString()} 本地:{ItemLocalPort.ToString()}");
                AddList.SubItems.Add("未启动");
                AddList.SubItems.Add(ItemServerPassword);
                AddList.SubItems.Add("");

                if (listView1.InvokeRequired)
                {
                    listView1.Invoke(new MethodInvoker(delegate
                    {
                        listView1.Items.Add(AddList);
                    }));
                }
                else
                {
                    listView1.Items.Add(AddList);
                }

                return new JObject
                {
                    ["code"] = "success",
                    ["message"] = "计划添加成功",
                };
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["code"] = "error",
                    ["message"] = ex.Message,
                };
            }
        }


        // 列表 -> 移除一个项目

        public JObject ListRemoveItem(int DeleteID)
        {

            JObject DeleteResult = frpAdmin.Common.DateBaseRemoveItem(DeleteID);

            if (DeleteResult["code"].ToString() != "success")
            {
                return DeleteResult;
            }

            listView1.Invoke(new Action(() =>
            {
                for (int i = listView1.Items.Count - 1; i >= 0; i--)
                {
                    if (listView1.Items[i].SubItems[0].Text == DeleteID.ToString())
                    {
                        listView1.Items.RemoveAt(i);
                        break;
                    }
                }
            }));

            return new JObject
            {
                ["code"] = "success",
                ["message"] = "计划删除成功",
            };
        }


        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (listView1.SelectedItems.Count < 1)
            {
                toolStripMenuItem1.Enabled = false;
                toolStripMenuItem2.Enabled = false;
                toolStripMenuItem3.Enabled = false;
                toolStripMenuItem4.Enabled = false;
            }
            else {
                toolStripMenuItem1.Enabled = true;
                toolStripMenuItem2.Enabled = true;
                toolStripMenuItem3.Enabled = true;
                toolStripMenuItem4.Enabled = true;


                ListViewItem selectedItem = listView1.SelectedItems[0];
                if (selectedItem.SubItems[6].Text == "检测中...")
                {
                    toolStripMenuItem1.Enabled = false;
                    toolStripMenuItem2.Enabled = false;
                    toolStripMenuItem3.Enabled = false;
                    toolStripMenuItem4.Enabled = false;
                }

            }

        }

        private async void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ListViewItem selectedItem = listView1.SelectedItems[0];
            if (selectedItem.SubItems[3].Text != "")
            {
                MessageBox.Show("计划已启动，请停止计划后再试！", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string TaskTmpFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "taskfile");

                if (!Directory.Exists(TaskTmpFile))
                {
                    Directory.CreateDirectory(TaskTmpFile);
                }

                string FrpcPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "frpc", "frpc.exe");
                string TaskTmpName = GetMD5Hash(selectedItem.SubItems[0].Text + "-" + selectedItem.SubItems[1].Text + "-" + selectedItem.SubItems[2].Text);
                string TaskTmpPath = Path.Combine(TaskTmpFile, TaskTmpName);
                Directory.CreateDirectory(TaskTmpPath);
                File.Copy(FrpcPath, Path.Combine(TaskTmpPath, $"frpc_{TaskTmpName}.exe"), true);

                selectedItem.SubItems[3].Text = $"frpc_{TaskTmpName}";
                selectedItem.SubItems[8].Text = TaskTmpPath;

                JObject ReadResult = frpAdmin.Common.DateBaseReadByID(int.Parse(selectedItem.SubItems[0].Text));
                if (ReadResult["code"].ToString() != "success")
                {
                    MessageBox.Show("数据读取错误，请检查数据库！", "创建任务失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                JObject ReadResultData = (JObject)ReadResult["data"];
                string tomlContent = $@"
serverAddr = ""{ReadResultData["serverip"].ToString()}""
serverPort = {int.Parse(ReadResultData["serverport"].ToString())}
auth.token = ""{ReadResultData["serverpassword"].ToString()}""

[[proxies]]
name = ""{ReadResultData["name"].ToString()}""
type = ""{ReadResultData["pact"].ToString()}""
localIP = ""127.0.0.1""
localPort = {int.Parse(ReadResultData["localport"].ToString())}
remotePort = {int.Parse(ReadResultData["remoteport"].ToString())}
";

                File.WriteAllText(Path.Combine(TaskTmpPath, "config.toml"), tomlContent);

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = Path.Combine(TaskTmpPath, selectedItem.SubItems[3].Text);
                startInfo.Arguments = "-c " + Path.Combine(TaskTmpPath, "config.toml");
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;

                Process.Start(startInfo);
                selectedItem.SubItems[6].Text = "检测中...";

                await Task.Delay(10000);  // 等待10秒

                Process[] processes = Process.GetProcessesByName(selectedItem.SubItems[3].Text);

                if (processes.Length < 1)
                {
                    MessageBox.Show("计划启动失败，服务器连接错误！", "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Directory.Delete(selectedItem.SubItems[8].Text, true);
                    selectedItem.SubItems[3].Text = "";
                    selectedItem.SubItems[6].Text = "未启动";
                    selectedItem.SubItems[8].Text = "";
                    return;
                }

                selectedItem.SubItems[6].Text = "运行中";

                MessageBox.Show("计划已启动，进程已运行！", "启动成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "创建任务失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // 计算MD5哈希值

        private static string GetMD5Hash(string AText) 
        {
            MD5Digest digest = new MD5Digest();
            byte[] inputBytes = Encoding.UTF8.GetBytes(AText);
            digest.BlockUpdate(inputBytes, 0, inputBytes.Length);
            byte[] result = new byte[digest.GetDigestSize()];
            digest.DoFinal(result, 0);

            return BitConverter.ToString(result).Replace("-", "").ToLowerInvariant();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            ListViewItem selectedItem = listView1.SelectedItems[0];
            if (selectedItem.SubItems[3].Text == "")
            {
                MessageBox.Show("计划未启动，请启动计划后在试！", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Process[] processesList = Process.GetProcessesByName(selectedItem.SubItems[3].Text);

            foreach (Process process in processesList)
            {
                if (!process.CloseMainWindow())
                {
                    process.Kill();
                }
                process.WaitForExit();
            }

            try
            {
                Directory.Delete(selectedItem.SubItems[8].Text, true);

                // 清理显示

                selectedItem.SubItems[3].Text = "";
                selectedItem.SubItems[6].Text = "未启动";
                selectedItem.SubItems[8].Text = "";


                MessageBox.Show("计划已终止，临时文件已删除！", "删除成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message + "\n请手动删除掉此目录！", "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        private async void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            ListViewItem selectedItem = listView1.SelectedItems[0];
            if (selectedItem.SubItems[3].Text == "")
            {
                MessageBox.Show("计划未启动，请启动计划后在试！", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Process[] processesList = Process.GetProcessesByName(selectedItem.SubItems[3].Text);
            foreach (Process process in processesList)
            {
                if (!process.CloseMainWindow())
                {
                    process.Kill();
                }
                process.WaitForExit();
            }
            string RunFile = selectedItem.SubItems[8].Text;

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(RunFile,selectedItem.SubItems[3].Text);
            startInfo.Arguments = "-c " + Path.Combine(RunFile, "config.toml");
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            try {

                Process.Start(startInfo);

                selectedItem.SubItems[6].Text = "检测中...";

                await Task.Delay(10000);  // 等待10秒

                Process[] processes = Process.GetProcessesByName(selectedItem.SubItems[3].Text);

                if (processes.Length < 1)
                {
                    MessageBox.Show("计划启动失败，服务器连接错误！", "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Directory.Delete(selectedItem.SubItems[8].Text, true);
                    selectedItem.SubItems[3].Text = "";
                    selectedItem.SubItems[6].Text = "未启动";
                    selectedItem.SubItems[8].Text = "";
                    return;
                }

                selectedItem.SubItems[6].Text = "运行中";

                MessageBox.Show("重启已发起，进程已经重启成功！", "启动成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"重启失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            ListViewItem selectedItem = listView1.SelectedItems[0];
            if (selectedItem.SubItems[3].Text != "")
            {
                MessageBox.Show("计划已启动，请停止计划后在试！", "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            JObject DeleteResult = ListRemoveItem(int.Parse(selectedItem.SubItems[0].Text));
            if (DeleteResult["code"].ToString() != "success") {
                MessageBox.Show(DeleteResult["message"].ToString(), "发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("计划删除成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            var MessageBoxResult = MessageBox.Show("您是否要退出管理器?", "frpAdmin", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (MessageBoxResult == DialogResult.No)
            {
                e.Cancel = true;
            }
            else 
            {
                // 终止掉列表中正在运行的所有项目

                foreach (ListViewItem item in listView1.Items)
                {
                    if (item.SubItems[3].Text != "") 
                    {
                        // 终止进程
                        Process[] processesList = Process.GetProcessesByName(item.SubItems[3].Text);
                        foreach (Process process in processesList)
                        {
                            if (!process.CloseMainWindow())
                            {
                                process.Kill();
                            }
                            process.WaitForExit();
                        }
                        // 删除目录
                        Directory.Delete(item.SubItems[8].Text, true);
                    }
                }

            }

        }

        private void Main_Load(object sender, EventArgs e)
        {

        }
    }
}
