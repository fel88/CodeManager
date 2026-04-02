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

            currentDir = Path.GetDirectoryName(ofd.FileName);
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

        async void search()
        {
            var d = AutoDialog.DialogHelpers.StartDialog();
            d.AddStringField("ext", "File mask", "*.*");
            if (!d.ShowDialog())
                return;

            string[] files = Directory.GetFiles(currentDir, d.GetStringField("ext"), SearchOption.AllDirectories);
            listView1.Items.Clear();
            var trimmed = linesToSearch.Select(z => z.Trim()).ToArray();
            foreach (var item in files)
            {
                int index = 0;
                await foreach (var line in File.ReadLinesAsync(item))
                {
                    if (line.Trim().Equals(trimmed[index], StringComparison.CurrentCultureIgnoreCase))
                    {
                        index++;
                        if (index == linesToSearch.Length)
                        {
                            listView1.Items.Add(new ListViewItem(new string[] { Path.GetFileName(item),

                                Path.GetRelativePath(currentDir, item) })
                            { Tag = item });
                            break;
                        }
                    }
                }
            }

            toolStripStatusLabel1.Text = $"files found: {listView1.Items.Count}";
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            ced.Text = File.ReadAllText(listView1.SelectedItems[0].Tag as string);

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
    }
}
