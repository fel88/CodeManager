using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.WindowsForms.Controls;
using System.Data;
using System.IO;
using Path = System.IO.Path;

namespace CodeManager
{
    public partial class SimilarFiles : Form
    {
        public SimilarFiles()
        {
            InitializeComponent();

            dv = new DiffViewer();
            dv.Dock = DockStyle.Fill;
            panel1.Controls.Add(dv);
        }
        DiffViewer dv;
        string currentDir = "";
        string lastMask = "*.*";
        bool strictLinesOrder = true;
        int modeIdx = 0;
        bool firstMatchExit = false;

        public class FileMatchInfo
        {
            public FileMatchInfo(FileMatchBatchInfo b)
            {
                Parent = b;
            }
            public FileMatchBatchInfo Parent;
            public string File;
            public double Match;
        }
        public class FileMatchBatchInfo
        {

            public string File;
            public List<FileMatchInfo> Matches = new List<FileMatchInfo>();
        }

        public double GetSimilarityPercentage(string oldText, string newText)
        {
            if (string.IsNullOrEmpty(oldText) || string.IsNullOrEmpty(newText))
                return oldText == newText ? 100.0 : 0.0;

            // Build the diff (line by line)
            var diff = InlineDiffBuilder.Diff(oldText, newText);

            // Count characters in unchanged lines
            double unchangedCharacters = diff.Lines
                .Where(l => l.Type == ChangeType.Unchanged).Count();


            // Calculate similarity based on the longer string to account for additions/deletions
            int maxLength = Math.Max(oldText.CountLines(), newText.CountLines());
            return (unchangedCharacters / maxLength);
        }




        List<FileMatchBatchInfo> matches = new List<FileMatchBatchInfo>();
        private async void searchByFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            var d = AutoDialog.DialogHelpers.StartDialog();
            d.AddBoolField("sameNameOnly", "Same name only", false);
            d.AddDouble("koef", "Pass perc", 90, min: 1, max: 100m);
            if (!d.ShowDialog())
                return;

            var sameNameOnly = d.GetBoolField("sameNameOnly");
            var passPerc = d.GetDouble("koef") / 100.0;
            matches.Clear();
            var text1 = File.ReadAllText(ofd.FileName);
            string[] files = Directory.GetFiles(currentDir, lastMask, SearchOption.AllDirectories);
            listView1.Items.Clear();
            matches.Add(new FileMatchBatchInfo()
            {
                File = ofd.FileName


            });
            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Maximum = files.Length;
            toolStripStatusLabel1.Visible = true;

            await Task.Run(() =>
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string? item = files[i];
                    statusStrip1.Invoke(() =>
                    {
                        toolStripStatusLabel1.Text = $"{i} / {files.Length}";
                        toolStripProgressBar1.Value = i;
                    });
                    if (item == ofd.FileName)
                        continue;
                    if (sameNameOnly && Path.GetFileName(item) != Path.GetFileName(ofd.FileName))
                        continue;

                    var text2 = File.ReadAllText(item);
                    //var results = DiffUtil.Diff(text1, text2);
                    //results = DiffUtil.Order(results, DiffOrderType.GreedyDeleteFirst);
                    //var len = results.Where(z => z.Status == DiffStatus.Equal).Count();
                    var perc = GetSimilarityPercentage(text1, text2);
                    //var perc = len / (double)Math.Max(text1.Length, text2.Length);
                    var fr = matches.First();

                    if (perc >= passPerc)
                    {
                        fr.Matches.Add(new FileMatchInfo(fr) { File = item, Match = perc });
                    }
                }
            });
            toolStripProgressBar1.Visible = false;
            toolStripStatusLabel1.Visible = false;


            listView1.Items.Clear();
            foreach (var item in matches)
            {
                listView1.Items.Add(new System.Windows.Forms.ListViewItem(new string[] {Path.GetFileName( item.File ),

                Path.GetRelativePath(currentDir,Path.GetDirectoryName(item.File))})
                { Tag = item });
            }

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var b = listView1.SelectedItems[0].Tag as FileMatchBatchInfo;
            listView2.Items.Clear();
            foreach (var item in b.Matches)
            {
                listView2.Items.Add(new System.Windows.Forms.ListViewItem(new string[]

                {  Path.GetFileName(item.File),
                Path.GetRelativePath(currentDir,Path.GetDirectoryName(item.File)),
                    $"{Math.Round(item.Match * 100, 2)}%" })
                {
                    Tag = item
                });
            }
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            var b = listView2.SelectedItems[0].Tag as FileMatchInfo;
            dv.NewTextHeader = b.File;
            dv.OldTextHeader = b.Parent.File;
            dv.NewText = File.ReadAllText(b.File);
            dv.OldText = File.ReadAllText(b.Parent.File);
        }

        private void setDirectoryFromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            currentDir = System.IO.Path.GetDirectoryName(ofd.FileName);
            Text = currentDir;
        }

        private void setManualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var d = AutoDialog.DialogHelpers.StartDialog();
            d.AddStringField("dir", "Dir", currentDir);
            if (!d.ShowDialog())
                return;

            currentDir = d.GetStringField("dir");
            Text = currentDir;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            currentDir = System.IO.Directory.GetParent(currentDir).FullName;
            Text = currentDir;
        }

        private void autoDetectToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
