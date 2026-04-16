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
            // 실행 시점에 리스트뷰 형식을 강제로 초기화합니다.
            InitListViewControls();
        }

        private void InitListViewControls()
        {
            // 왼쪽 리스트뷰 초기화
            lvwLeftDir.View = View.Details;
            lvwLeftDir.FullRowSelect = true;
            lvwLeftDir.Columns.Clear();
            lvwLeftDir.Columns.Add("이름", 250);
            lvwLeftDir.Columns.Add("크기", 100);
            lvwLeftDir.Columns.Add("수정일", 180);

            // 오른쪽 리스트뷰 초기화
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
            // 두 경로 중 하나라도 있으면 실행
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
                // [수정] 모든 파일을 가져오도록 명시
                FileInfo[] files = diSource.GetFiles("*.*");

                // 비교 대상 폴더의 파일들 준비
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
                    // 1. 아이템 생성 (파일명)
                    ListViewItem item = new ListViewItem(file.Name);

                    // 2. 서브 아이템 추가 (크기, 날짜)
                    item.SubItems.Add((file.Length / 1024.0).ToString("N0") + " KB");
                    item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));

                    // 3. 색상 결정 로직
                    if (targetDict.TryGetValue(file.Name, out FileInfo targetFile))
                    {
                        // 초 단위 절삭 후 비교 (미세한 차이 방지)
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
            catch (Exception ex)
            {
                MessageBox.Show("오류 발생: " + ex.Message);
            }
            finally
            {
                lv.EndUpdate();
            }
        }

        // 복사 버튼 이벤트들 (연결 확인 필요)
        private void btnCopyFromLeft_Click(object sender, EventArgs e) => CopyFiles(lvwLeftDir, txtLeftDir.Text, txtRightDir.Text);
        private void btnCopyFromRight_Click(object sender, EventArgs e) => CopyFiles(lvwrightDir, txtRightDir.Text, txtLeftDir.Text);

        private void CopyFiles(ListView sourceLv, string sDir, string tDir)
        {
            if (sourceLv.SelectedItems.Count == 0) return;
            foreach (ListViewItem item in sourceLv.SelectedItems)
            {
                string sPath = Path.Combine(sDir, item.Text);
                string tPath = Path.Combine(tDir, item.Text);

                if (File.Exists(tPath))
                {
                    if (new FileInfo(sPath).LastWriteTime < new FileInfo(tPath).LastWriteTime)
                    {
                        if (MessageBox.Show($"{item.Text}는 대상보다 오래된 파일입니다. 덮어쓸까요?", "확인", MessageBoxButtons.YesNo) == DialogResult.No)
                            continue;
                    }
                }
                File.Copy(sPath, tPath, true);
            }
            RefreshFileSystem();
        }

        // 이전 에러 방지용 빈 메서드 (필요시 유지)
        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) { }
    }
}