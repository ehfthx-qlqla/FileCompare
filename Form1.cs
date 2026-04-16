using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitListViewControls();
        }

        private void InitListViewControls()
        {
            // 리스트뷰 설정 초기화
            lvwLeftDir.View = View.Details;
            lvwLeftDir.FullRowSelect = true;
            lvwLeftDir.Columns.Clear();
            lvwLeftDir.Columns.Add("이름", 250);
            lvwLeftDir.Columns.Add("크기", 100);
            lvwLeftDir.Columns.Add("수정일", 180);

            lvwrightDir.View = View.Details;
            lvwrightDir.FullRowSelect = true;
            lvwrightDir.Columns.Clear();
            lvwrightDir.Columns.Add("이름", 250);
            lvwrightDir.Columns.Add("크기", 100);
            lvwrightDir.Columns.Add("수정일", 180);
        }

        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                    RefreshFileSystem();
                }
            }
        }

        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                    RefreshFileSystem();
                }
            }
        }

        private void RefreshFileSystem()
        {
            CompareAndPopulate(txtLeftDir.Text, txtRightDir.Text, lvwLeftDir);
            CompareAndPopulate(txtRightDir.Text, txtLeftDir.Text, lvwrightDir);
        }

        private void CompareAndPopulate(string sourcePath, string targetPath, ListView lv)
        {
            lv.Items.Clear();
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath)) return;

            try
            {
                DirectoryInfo diSource = new DirectoryInfo(sourcePath);
                FileInfo[] files = diSource.GetFiles("*.*");

                Dictionary<string, FileInfo> targetDict = new Dictionary<string, FileInfo>();
                if (!string.IsNullOrWhiteSpace(targetPath) && Directory.Exists(targetPath))
                {
                    foreach (var f in new DirectoryInfo(targetPath).GetFiles("*.*"))
                    {
                        if (!targetDict.ContainsKey(f.Name)) targetDict.Add(f.Name, f);
                    }
                }

                lv.BeginUpdate();
                foreach (FileInfo file in files.OrderBy(f => f.Name))
                {
                    ListViewItem item = new ListViewItem(file.Name);
                    item.SubItems.Add((file.Length / 1024.0).ToString("N0") + " KB");
                    item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));

                    if (targetDict.TryGetValue(file.Name, out FileInfo targetFile))
                    {
                        TimeSpan diff = file.LastWriteTime - targetFile.LastWriteTime;
                        if (Math.Abs(diff.TotalSeconds) < 1)
                            item.ForeColor = Color.Black; // 동일
                        else if (file.LastWriteTime > targetFile.LastWriteTime)
                            item.ForeColor = Color.Red;   // 신규(New)
                        else
                            item.ForeColor = Color.Gray;  // 과거(Old)
                    }
                    else
                    {
                        item.ForeColor = Color.Purple; // 단독 존재
                    }

                    lv.Items.Add(item);
                }
            }
            catch (Exception ex) { MessageBox.Show("목록 갱신 오류: " + ex.Message); }
            finally { lv.EndUpdate(); }
        }

        // --- [과제 3 핵심] 파일 복사 버튼 이벤트 ---
        private void btnCopyFromLeft_Click(object sender, EventArgs e)
            => ProcessFileCopy(lvwLeftDir, txtLeftDir.Text, txtRightDir.Text);

        private void btnCopyFromRight_Click(object sender, EventArgs e)
            => ProcessFileCopy(lvwrightDir, txtRightDir.Text, txtLeftDir.Text);

        private void ProcessFileCopy(ListView sourceLv, string sDir, string tDir)
        {
            if (sourceLv.SelectedItems.Count == 0)
            {
                MessageBox.Show("복사할 파일을 선택해주세요.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sDir) || string.IsNullOrWhiteSpace(tDir)) return;

            foreach (ListViewItem item in sourceLv.SelectedItems)
            {
                string fileName = item.Text;
                string sPath = Path.Combine(sDir, fileName);
                string tPath = Path.Combine(tDir, fileName);

                if (File.Exists(tPath))
                {
                    FileInfo sInfo = new FileInfo(sPath);
                    FileInfo tInfo = new FileInfo(tPath);

                    // 과제 요건: 대상 파일이 더 최신인 경우 정보를 표시하고 확인 받기
                    if (sInfo.LastWriteTime < tInfo.LastWriteTime)
                    {
                        string msg = $"대상의 파일이 더 최신입니다. 덮어쓰시겠습니까?\n\n" +
                                     $"[원본] {sInfo.LastWriteTime}\n" +
                                     $"[대상] {tInfo.LastWriteTime}";

                        if (MessageBox.Show(msg, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            continue;
                    }
                }

                try
                {
                    File.Copy(sPath, tPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{fileName} 복사 실패: {ex.Message}");
                }
            }

            // 복사 완료 후 화면 갱신
            RefreshFileSystem();
            MessageBox.Show("복사가 완료되었습니다.");
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) { }
    }
}