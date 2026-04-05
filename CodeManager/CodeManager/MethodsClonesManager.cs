using ICSharpCode.AvalonEdit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using static CodeManager.SimilarFiles;

namespace CodeManager
{
    public partial class MethodsClonesManager : Form
    {
        public MethodsClonesManager()
        {
            InitializeComponent();
            ced = new CodeEditor();

            ElementHost host = new ElementHost() { Dock = DockStyle.Fill };
            host.Child = ced;
            panel1.Controls.Add(host);
        }

        CodeEditor ced;
        string currentDir = "";
        string lastMask = "*.cs";

        public class MethodInfo
        {
            public List<MethodInfoItem> Items = new List<MethodInfoItem>();
            public MethodDeclarationSyntax Method;
        }

        public class MethodInfoItem
        {
            public string File;
            public MethodDeclarationSyntax Method;
        }

        private async void search()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            currentDir = Path.GetDirectoryName(ofd.FileName);
            string[] files = Directory.GetFiles(currentDir, lastMask, SearchOption.AllDirectories);
            List<MethodInfo> allMethods = new List<MethodInfo>();
            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Maximum = files.Length;
            toolStripStatusLabel1.Visible = true;
            await Task.Run(() =>
            {


                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    statusStrip1.Invoke(() =>
                    {
                        toolStripStatusLabel1.Text = $"{i} / {files.Length}";
                        toolStripProgressBar1.Value = i;
                    });
                    try
                    {
                        var code = File.ReadAllText(file);
                        // 1. Generate the AST
                        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

                        // 2. Extract all methods
                        var methods = root.DescendantNodes()
                                          .OfType<MethodDeclarationSyntax>();
                        foreach (var item in methods)
                        {
                            if (allMethods.Any(z => z.Method.IsEquivalentTo(item)))
                            {

                                var fr = allMethods.First(z => z.Method.IsEquivalentTo(item));
                                fr.Items.Add(new MethodInfoItem() { File = file, Method = item });
                            }
                            else
                            {
                                allMethods.Add(new MethodInfo()
                                {
                                    Method = item,
                                    Items = new List<MethodInfoItem>() { new MethodInfoItem (){

                                    File=file,Method=item
                                }}
                                });
                                listView1.Items.Add(new ListViewItem(new string[] { item.Identifier.Text }) { Tag = allMethods.Last() });
                            }
                        }

                        // Console.WriteLine($"Found method: {method.Identifier.Text}");

                    }
                    catch (Exception ex)
                    {

                    }

                }
            });
            toolStripProgressBar1.Visible = false;
            toolStripStatusLabel1.Visible = false;

        }

        private void setDirectoryFromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            currentDir = System.IO.Path.GetDirectoryName(ofd.FileName);
            Text = currentDir;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            currentDir = System.IO.Directory.GetParent(currentDir).FullName;
            Text = currentDir;
        }

        private void searchByFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            search();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var b = listView1.SelectedItems[0].Tag as MethodInfo;
            ced.textEditor.Text = b.Method.ToFullString();
            listView2.Items.Clear();
            foreach (var item in b.Items)
            {
                listView2.Items.Add(new ListViewItem(new string[] { Path.GetFileName(item.File), Path.GetRelativePath(currentDir, item.File), item.Method.Span.Start.ToString() }) { Tag = item });
            }
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            var b = listView2.SelectedItems[0].Tag as MethodInfoItem;
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(b.File));
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            ced.textEditor.Text = File.ReadAllText(b.File);
            var searchText = b.Method.ToString();
            // Search from the current cursor position to the end
            int index = ced.textEditor.Document.IndexOf(searchText, 0, ced.textEditor.Document.TextLength, StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                ced.textEditor.Select(index, searchText.Length);
                ced.textEditor.ScrollToLine(ced.textEditor.Document.GetLineByOffset(index).LineNumber);
            }
        }
    }
}
