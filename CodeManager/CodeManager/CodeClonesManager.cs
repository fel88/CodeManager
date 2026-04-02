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
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            currentDir = System.IO.Path.GetDirectoryName(ofd.FileName);
            Text = currentDir;
        }
        string currentDir = "";
        string[] linesToSearch;
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Form f = new Form();
            var rtb = new RichTextBox() { Dock = DockStyle.Fill };
            f.Controls.Add(rtb);
            f.ShowDialog();
            linesToSearch = rtb.Lines;
            search();
        }
        public List<LineMatch> Matches = new List<LineMatch>();
        public class LineMatch
        {
            public string File;
            public int Line;
        }
        async void search()
        {
            var d = AutoDialog.DialogHelpers.StartDialog();
            d.AddStringField("ext", "File mask", "*.*");
            d.AddOptionsField("mode", "Mode", ["full line trim", "contains"], 0);
            d.AddBoolField("exitOnFirst", "First match exit", false);
            if (!d.ShowDialog())
                return;

            Matches.Clear();
            int mode = d.GetOptionsFieldIdx("mode");
            bool firstOnly = d.GetBoolField("exitOnFirst");
            string[] files = Directory.GetFiles(currentDir, d.GetStringField("ext"), SearchOption.AllDirectories);
            listView1.Items.Clear();
            var trimmed = linesToSearch.Select(z => z.Trim()).ToArray();

            foreach (var item in files)
            {
                int lineIdx = 0;
                int index = 0;
                bool first = true;
                await foreach (var line in File.ReadLinesAsync(item))
                {
                    lineIdx++;
                    bool res = false;
                    if (mode == 0)
                        res = line.Trim().Equals(trimmed[index], StringComparison.CurrentCultureIgnoreCase);
                    else if (mode == 1)
                        res = line.Contains(trimmed[index], StringComparison.CurrentCultureIgnoreCase);
                    if (res)
                    {
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
                                Line = lineIdx - linesToSearch.Length + 1
                            });
                            if (firstOnly)
                                break;
                            index = 0;
                        }

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
            listView2.Items.Clear();
            foreach (var item in Matches.Where(z => z.File == path))
            {
                listView2.Items.Add(new ListViewItem(new string[] {
                item.Line.ToString()
                })
                { Tag = item });
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            var d = AutoDialog.DialogHelpers.StartDialog();
            d.AddStringField("dir", "Dir", currentDir);
            if (!d.ShowDialog())
                return;

            currentDir = d.GetStringField("dir");
            Text = currentDir;
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            var lm = listView2.SelectedItems[0].Tag as LineMatch;

            //rtb.textEditor.ScrollToLine(Line);

            TextView textView = ced.textEditor.TextArea.TextView;
            var visualTop = textView.GetVisualTopByDocumentLine(lm.Line);
            ced.textEditor.ScrollToVerticalOffset(visualTop);
            // Get the line information (1-based index)
            var line = ced.textEditor.Document.GetLineByNumber(lm.Line);

            // Select the line (offset, length)
            ced.textEditor.Select(line.Offset, line.TotalLength);

        }
    }
}
