using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Shapes;

namespace CodeManager
{
    public partial class CodeClonesManager : Form
    {
        public CodeClonesManager()
        {
            InitializeComponent();

            ced = new CodeEditor();

            // 1. Create the ElementHost container
            ElementHost host = new ElementHost();
            host.Child = ced;
            host.Dock = DockStyle.Fill;
            tableLayoutPanel1.Controls.Add(host, 1, 0);
            tableLayoutPanel1.SetRowSpan(host, 2);

        }
        CodeEditor ced;
        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }
        string currentDir = "";
        string[] linesToSearch;
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            var d = AutoDialog.DialogHelpers.StartDialog();
            d.AddCustomDialogField("text", "Text to search", () =>
            {
                Form f = new Form();
                var rtb = new RichTextBox() { Dock = DockStyle.Fill };
                f.Controls.Add(rtb);
                f.ShowDialog();
                linesToSearch = rtb.Lines;
            });
            d.AddStringField("ext", "File mask", lastMask);
            d.AddOptionsField("mode", "Mode", ["full line trim", "contains"], modeIdx);
            d.AddBoolField("exitOnFirst", "First match exit", firstMatchExit);
            d.AddBoolField("strictLinesOrder", "Strict lines order", strictLinesOrder);
            if (!d.ShowDialog())
                return;


            strictLinesOrder = d.GetBoolField("strictLinesOrder");
            modeIdx = d.GetOptionsFieldIdx("mode");
            firstMatchExit = d.GetBoolField("exitOnFirst");
            lastMask = d.GetStringField("ext");
            search();
        }

        public List<LineMatch> Matches = new List<LineMatch>();
        public class LineMatch
        {
            public string File;
            public int Line;
        }
        string lastMask = "*.*";
        bool strictLinesOrder = true;
        int modeIdx = 0;
        bool firstMatchExit = false;
        async void search()
        {

            Matches.Clear();
            string[] files = Directory.GetFiles(currentDir, lastMask, SearchOption.AllDirectories);
            listView1.Items.Clear();
            var trimmed = linesToSearch.Select(z => z.Trim()).ToArray();

            foreach (var item in files)
            {
                int lineIdx = 0;
                int index = 0;
                bool first = true;
                int firstLineIdx = 0;
                await foreach (var line in File.ReadLinesAsync(item))
                {
                    lineIdx++;
                    bool res = false;
                    if (modeIdx == 0)
                        res = line.Trim().Equals(trimmed[index], StringComparison.CurrentCultureIgnoreCase);
                    else if (modeIdx == 1)
                        res = line.Contains(trimmed[index], StringComparison.CurrentCultureIgnoreCase);
                    if (res)
                    {
                        if (index == 0)
                            firstLineIdx = lineIdx;
                        index++;
                        if (index == linesToSearch.Length)
                        {
                            if (first)
                                listView1.Items.Add(new ListViewItem(new string[] { System.IO.Path.GetFileName(item),
                                System.IO.Path.GetRelativePath(currentDir, item) })
                                { Tag = item });

                            first = false;
                            Matches.Add(new LineMatch()
                            {
                                File = item,
                                //Line = lineIdx - linesToSearch.Length + 1
                                Line = firstLineIdx
                            });
                            if (firstMatchExit)
                                break;
                            index = 0;
                        }

                    }
                    else
                    {
                        if (strictLinesOrder)
                            index = 0;
                    }
                }
            }

            toolStripStatusLabel1.Text = $"files found: {listView1.Items.Count}; matches: {Matches.Count}";
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var path = listView1.SelectedItems[0].Tag as string;
            ced.Text = File.ReadAllText(path);
            var matches = Matches.Where(z => z.File == path).ToArray();
            if (matches.Any())
            {
                NavigateTo(matches.First());
            }
            listView2.Items.Clear();
            foreach (var item in matches)
            {
                listView2.Items.Add(new ListViewItem(new string[] {
                item.Line.ToString()
                })
                { Tag = item });
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {

        }

        void NavigateTo(LineMatch lm)
        {

            //rtb.textEditor.ScrollToLine(Line);

            TextView textView = ced.textEditor.TextArea.TextView;
            var visualTop = textView.GetVisualTopByDocumentLine(lm.Line);
            ced.textEditor.ScrollToVerticalOffset(visualTop);
            // Get the line information (1-based index)
            var line = ced.textEditor.Document.GetLineByNumber(lm.Line);

            // Select the line (offset, length)
            ced.textEditor.Select(line.Offset, line.TotalLength);
        }
        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            var lm = listView2.SelectedItems[0].Tag as LineMatch;
            NavigateTo(lm);

        }

        private void manualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var d = AutoDialog.DialogHelpers.StartDialog();
            d.AddStringField("dir", "Dir", currentDir);
            if (!d.ShowDialog())
                return;

            currentDir = d.GetStringField("dir");
            Text = currentDir;
        }

        private void byFileSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            currentDir = System.IO.Path.GetDirectoryName(ofd.FileName);
            Text = currentDir;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            search();
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {

            currentDir = System.IO.Directory.GetParent(currentDir).FullName;
            Text = currentDir;
        }
    }
}
