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

        // 리스트뷰 초기 설정 (컬럼 구성)
        private void InitListViewControls()
        {
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

        // [과제 핵심] 파일 비교 및 색상 결정 로직
        private void CompareAndPopulate(string sourcePath, string targetPath, ListView lv)
        {
            lv.Items.Clear();
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath)) return;

            try
            {
                DirectoryInfo diSource = new DirectoryInfo(sourcePath);
                FileInfo[] files = diSource.GetFiles("*.*");

                // 상대 폴더 파일 딕셔너리 준비
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

                    // --- [과제 2단계 & 3단계] 비교 및 색상 적용 ---
                    if (targetDict.TryGetValue(file.Name, out FileInfo targetFile))
                    {
                        // 1초 미만 오차 보정
                        TimeSpan diff = file.LastWriteTime - targetFile.LastWriteTime;
                        double seconds = Math.Abs(diff.TotalSeconds);

                        if (seconds < 1)
                        {
                            // 동일 파일: 검은색
                            item.ForeColor = Color.Black;
                        }
                        else if (file.LastWriteTime > targetFile.LastWriteTime)
                        {
                            // [New] 내 파일이 더 최신: 빨간색
                            item.ForeColor = Color.Red;
                        }
                        else
                        {
                            // [Old] 내 파일이 더 과거: 회색
                            item.ForeColor = Color.Gray;
                        }
                    }
                    else
                    {
                        // [단독] 상대방 쪽에 없음: 보라색
                        item.ForeColor = Color.Purple;
                    }

                    lv.Items.Add(item);
                }
            }
            catch (Exception ex) { MessageBox.Show("오류: " + ex.Message); }
            finally { lv.EndUpdate(); }
        }

        // [과제 3] 복사 기능
        private void btnCopyFromLeft_Click(object sender, EventArgs e)
            => ProcessFileCopy(lvwLeftDir, txtLeftDir.Text, txtRightDir.Text);

        private void btnCopyFromRight_Click(object sender, EventArgs e)
            => ProcessFileCopy(lvwrightDir, txtRightDir.Text, txtLeftDir.Text);

        private void ProcessFileCopy(ListView sourceLv, string sDir, string tDir)
        {
            if (sourceLv.SelectedItems.Count == 0) return;
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

                    // 사진 속 경고창 내용 반영
                    string msg = "대상에 동일한 이름의 파일이 이미 있습니다.\n" +
                                 "대상 파일이 더 신규 파일입니다. 덮어쓰시겠습니까?\n\n" +
                                 $"원본: {sPath}\n" +
                                 $"(날짜: {sInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss})\n\n" +
                                 $"대상: {tPath}\n" +
                                 $"(날짜: {tInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss})";

                    var result = MessageBox.Show(msg, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) continue;
                }

                try { File.Copy(sPath, tPath, true); }
                catch (Exception ex) { MessageBox.Show("복사 오류: " + ex.Message); }
            }
            RefreshFileSystem();
        }

        // 디자인 에러 방지용
        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) { }
    }
}