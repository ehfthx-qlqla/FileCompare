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
            // ListView 설정: 상세 보기 모드 및 전체 행 선택
            SetupListView(lvwLeftDir);
            SetupListView(lvwrightDir);
        }

        private void SetupListView(ListView lv)
        {
            lv.View = View.Details;
            lv.FullRowSelect = true;
            // 컬럼이 없다면 기본 컬럼 생성 (이름, 크기, 수정일)
            if (lv.Columns.Count == 0)
            {
                lv.Columns.Add("이름", 200);
                lv.Columns.Add("크기", 100);
                lv.Columns.Add("수정일", 150);
            }
        }

        // [과제 기능] 폴더 선택 및 리스트 갱신 (왼쪽)
        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                    RefreshFileSystem(); // 양쪽 모두 다시 비교하여 표시
                }
            }
        }

        // [과제 기능] 폴더 선택 및 리스트 갱신 (오른쪽)
        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                    RefreshFileSystem(); // 양쪽 모두 다시 비교하여 표시
                }
            }
        }

        // 양쪽 리스트를 새로고침하고 비교하는 메인 로직
        private void RefreshFileSystem()
        {
            CompareAndPopulate(txtLeftDir.Text, txtRightDir.Text, lvwLeftDir, true);
            CompareAndPopulate(txtRightDir.Text, txtLeftDir.Text, lvwrightDir, false);
        }

        private void CompareAndPopulate(string sourcePath, string targetPath, ListView lv, bool isLeft)
        {
            lv.Items.Clear();
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath)) return;

            try
            {
                // 1. 소스 폴더의 모든 파일 가져오기 (확장자 제한 없음)
                var sourceDirInfo = new DirectoryInfo(sourcePath);
                var sourceFiles = sourceDirInfo.GetFiles("*.*") // 모든 파일 형태 지정
                                              .ToDictionary(f => f.Name);

                // 2. 대상 폴더 파일 목록 (비교용)
                Dictionary<string, FileInfo> targetFiles = new Dictionary<string, FileInfo>();
                if (!string.IsNullOrWhiteSpace(targetPath) && Directory.Exists(targetPath))
                {
                    targetFiles = new DirectoryInfo(targetPath).GetFiles("*.*")
                                                               .ToDictionary(f => f.Name);
                }

                lv.BeginUpdate();
                // 3. 이름 순으로 정렬하여 리스트뷰에 추가
                foreach (var file in sourceFiles.Values.OrderBy(f => f.Name))
                {
                    var item = new ListViewItem(file.Name); // 파일명 (이미지, ppt, hwp 등 포함)

                    // 크기 변환 (KB 단위)
                    long fileSizeInBytes = file.Length;
                    string sizeStr = (fileSizeInBytes / 1024.0).ToString("N0") + " KB";
                    item.SubItems.Add(sizeStr);

                    // 수정일
                    item.SubItems.Add(file.LastWriteTime.ToString("g"));

                    // 4. [과제 핵심] 파일 비교 및 색상 결정
                    if (targetFiles.TryGetValue(file.Name, out var targetFile))
                    {
                        // 양쪽 폴더에 파일명이 같은 파일이 있는 경우
                        if (file.LastWriteTime == targetFile.LastWriteTime)
                        {
                            item.ForeColor = Color.Black; // 동일 파일: 검정
                        }
                        else if (file.LastWriteTime > targetFile.LastWriteTime)
                        {
                            item.ForeColor = Color.Red;   // 내가 더 최신(New): 빨강
                        }
                        else
                        {
                            item.ForeColor = Color.Gray;  // 내가 더 오래됨(Old): 회색
                        }
                    }
                    else
                    {
                        // 상대 폴더에 없는 파일인 경우
                        item.ForeColor = Color.Purple;    // 단독 파일: 보라
                    }

                    lv.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 목록을 읽는 중 오류 발생: {ex.Message}");
            }
            finally
            {
                lv.EndUpdate();
            }
        }

        // [과제 기능] 왼쪽 -> 오른쪽 복사
        private void btnCopyFromLeft_Click(object sender, EventArgs e)
        {
            CopySelectedFiles(lvwLeftDir, txtLeftDir.Text, txtRightDir.Text);
        }

        // [과제 기능] 오른쪽 -> 왼쪽 복사
        private void btnCopyFromRight_Click(object sender, EventArgs e)
        {
            CopySelectedFiles(lvwrightDir, txtRightDir.Text, txtLeftDir.Text);
        }

        private void CopySelectedFiles(ListView lv, string sourceDir, string destDir)
        {
            if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(destDir)) return;

            foreach (ListViewItem item in lv.SelectedItems)
            {
                string fileName = item.Text;
                string sourcePath = Path.Combine(sourceDir, fileName);
                string destPath = Path.Combine(destDir, fileName);

                if (File.Exists(destPath))
                {
                    FileInfo srcInfo = new FileInfo(sourcePath);
                    FileInfo destInfo = new FileInfo(destPath);

                    // [과제 기능] 날짜 확인 및 확인창 띄우기
                    if (srcInfo.LastWriteTime < destInfo.LastWriteTime)
                    {
                        var result = MessageBox.Show(
                            $"{fileName}은(는) 대상 폴더의 파일보다 오래되었습니다. 덮어쓰시겠습니까?",
                            "복사 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                        if (result == DialogResult.No) continue;
                    }
                }

                try
                {
                    File.Copy(sourcePath, destPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 복사 중 오류 발생: {ex.Message}");
                }
            }
            RefreshFileSystem();
        }// 복사 후 리스트 갱신
            // 디자인 파일과의 연결 오류를 해결하기 위한 빈 메서드
            private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
            // 아무 내용도 적지 않아도 됩니다.
        }
    }
}
