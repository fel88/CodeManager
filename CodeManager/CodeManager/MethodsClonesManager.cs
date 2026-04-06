using ICSharpCode.AvalonEdit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
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

        public class NodeInfo
        {
            public List<NodeInfoItem> Items = new List<NodeInfoItem>();
            public SyntaxNode Node;
        }

        public class NodeInfoItem
        {
            public string File;
            public SyntaxNode Node;
        }
        List<NodeInfo> allNodes = new List<NodeInfo>();
        void UpdateList()
        {
            listView1.Items.Clear();
            foreach (var item in allNodes)
            {
                var name = item.Node.ToString();
                if (name.IndexOf('\n') >= 0)
                    name = name.Substring(0, name.IndexOf("\n"));
                if (item.Node is MethodDeclarationSyntax mds)
                {
                    name = mds.Identifier.Text;
                }
                if (!string.IsNullOrEmpty(textBox1.Text))
                    if (!name.Contains(textBox1.Text, StringComparison.OrdinalIgnoreCase))
                        continue;

                if (item.Items.Count < minumumClones)
                    continue;

                listView1.Items.Add(new System.Windows.Forms.ListViewItem(new string[] { name, item.Node.GetType().Name,
                item.Node.ToFullString().CountLines().ToString()})
                { Tag = item });
            }
        }

        private async void search()
        {
            allNodes.Clear();
            var code = File.ReadAllText(lastFile);
            // 1. Generate the AST
            if (autoFormat)
                code = NormalizeCodeWithRoslyn(code);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            // 2. Extract all methods
            var nodes = root.DescendantNodes();
            //.OfType<MethodDeclarationSyntax>();
            foreach (var item in nodes)
            {
                if (!(item is MethodDeclarationSyntax mds || item is PropertyDeclarationSyntax || item is FieldDeclarationSyntax
                    || item is EnumDeclarationSyntax || item is ClassDeclarationSyntax))
                    continue;

                if (!searchFields && item is FieldDeclarationSyntax)
                    continue;

                if (!searchMethods && item is MethodDeclarationSyntax)
                    continue;

                if (!searchProps && item is PropertyDeclarationSyntax)
                    continue;

                if (!searchEnums && item is EnumDeclarationSyntax)
                    continue;

                if (!searchClasses && item is ClassDeclarationSyntax)
                    continue;

                allNodes.Add(new NodeInfo()
                {
                    Node = item,
                    Items = new List<NodeInfoItem>() { new NodeInfoItem (){

                                    File=lastFile,
                        Node=item
                                }}
                });
                //listView1.Items.Add(new ListViewItem(new string[] { item.Identifier.Text }) { Tag = allMethods.Last() });
            }

            string[] files = Directory.GetFiles(currentDir, lastMask, SearchOption.AllDirectories);
            files = files.Except([lastFile]).ToArray();
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
                        if (autoFormat)
                            code = NormalizeCodeWithRoslyn(code);
                        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

                        // 2. Extract all methods
                        var methods = root.DescendantNodes();
                        //.OfType<MethodDeclarationSyntax>();
                        foreach (var item in methods)
                        {
                            var fr = allNodes.FirstOrDefault(z => AreEquals(z.Node, item));
                            if (fr != null)
                                fr.Items.Add(new NodeInfoItem() { File = file, Node = item });
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
            allNodes.RemoveAll(z => z.Items.Count < 2);
            UpdateList();
        }

        public enum CompareMethodEnum
        {
            RoslynEquivalent, TrimedLines, Any
        }
        CompareMethodEnum CompareMethod = CompareMethodEnum.Any;
        private bool AreEquals(SyntaxNode item, SyntaxNode item2)
        {
            if (CompareMethod == CompareMethodEnum.RoslynEquivalent || CompareMethod == CompareMethodEnum.Any)
            {
                var res = item.IsEquivalentTo(item2);
                if (res || CompareMethod == CompareMethodEnum.RoslynEquivalent)
                    return res;
            }

            var lns1 = item.ToFullString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(z => z.Trim()).ToArray();
            var lns2 = item2.ToFullString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(z => z.Trim()).ToArray();
            if (lns1.Length != lns2.Length)
                return false;

            for (int i = 0; i < lns1.Length; i++)
            {
                if (lns1[i] != lns2[i])
                    return false;
            }
            return true;
        }

        bool searchMethods = true;
        bool searchProps = true;
        bool searchEnums = true;
        bool searchClasses = true;
        bool searchFields = true;
        bool autoFormat = false;
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
        string lastFile = "";
        private void searchByFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            lastFile = ofd.FileName;
            search();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var b = listView1.SelectedItems[0].Tag as NodeInfo;
            ced.textEditor.Text = b.Node.ToFullString();
            selectedMethodInfo = b;
            UpdateList2();
            toolStripStatusLabel2.Text = $"{b.Items.Count()} clones";
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            var b = listView2.SelectedItems[0].Tag as NodeInfoItem;
            var code = File.ReadAllText(b.File);
            if (autoFormat)
                code = NormalizeCodeWithRoslyn(code);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            ced.textEditor.Text = code;
            var searchText = b.Node.ToString();
            // Search from the current cursor position to the end
            int index = ced.textEditor.Document.IndexOf(searchText, 0, ced.textEditor.Document.TextLength, StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                ced.textEditor.Select(index, searchText.Length);
                ced.textEditor.ScrollToLine(ced.textEditor.Document.GetLineByOffset(index).LineNumber);
            }
        }

        private void removeFromSourceFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            if (selectedMethodInfo == null)
                return;

            if (MessageBox.Show("Are you sure you want to modify selected files?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int modified = 0;
            for (int i = 0; i < listView2.SelectedItems.Count; i++)
            {
                var b = listView2.SelectedItems[i].Tag as NodeInfoItem;
                var code = File.ReadAllText(b.File);
                if (autoFormat)
                    code = NormalizeCodeWithRoslyn(code);
                code = code.Replace(b.Node.ToFullString(), string.Empty);
                File.WriteAllText(b.File, code);
                modified++;
                selectedMethodInfo.Items.Remove(b);
            }

            UpdateList2();

            MessageBox.Show($"{modified} files were modified", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        NodeInfo selectedMethodInfo = null;
        private void UpdateList2()
        {
            listView2.Items.Clear();
            foreach (var item in selectedMethodInfo.Items)
            {
                listView2.Items.Add(new System.Windows.Forms.ListViewItem(new string[] {
                    Path.GetFileName(item.File), Path.GetRelativePath(currentDir, item.File), item.Node.Span.Start.ToString() })
                { Tag = item });
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            var d = AutoDialog.DialogHelpers.StartDialog();

            d.AddStringField("ext", "File mask", lastMask);
            d.AddBoolField("fields", "Search fields", searchFields);
            d.AddBoolField("props", "Search props", searchProps);
            d.AddBoolField("methods", "Search methods", searchMethods);
            d.AddBoolField("enums", "Search enums", searchEnums);
            d.AddBoolField("classes", "Search classes", searchClasses);
            d.AddBoolField("autoFormat", "Auto format code", autoFormat);
            d.AddEnumField<CompareMethodEnum>("compare", "Compare method", CompareMethod);

            if (!d.ShowDialog())
                return;

            autoFormat = d.GetBoolField("autoFormat");
            searchFields = d.GetBoolField("fields");
            searchProps = d.GetBoolField("props");
            searchMethods = d.GetBoolField("methods");
            searchClasses = d.GetBoolField("classes");
            searchEnums = d.GetBoolField("enums");
            lastMask = d.GetStringField("ext");
            CompareMethod = d.GetEnumField<CompareMethodEnum>("compare");

        }

        public string NormalizeCodeWithRoslyn(string csCode)
        {
            var tree = CSharpSyntaxTree.ParseText(csCode);
            var root = tree.GetRoot().NormalizeWhitespace();
            return root.ToFullString();
        }

        private void showInExpolrerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            var b = listView2.SelectedItems[0].Tag as NodeInfoItem;
            ShowFileInExplorer(b.File);
        }

        public static void ShowFileInExplorer(string path)
        {
            string args = string.Format("/e, /select, \"{0}\"", path);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "explorer";
            info.Arguments = args;
            Process.Start(info);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            search();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateList();
        }

        int minumumClones = 0;
        private void button1_Click(object sender, EventArgs e)
        {
            var d = AutoDialog.DialogHelpers.StartDialog();

            d.AddInt("clones", "Minimum clones", minumumClones);

            if (!d.ShowDialog())
                return;

            minumumClones = d.GetInt("clones");
            UpdateList();
        }
    }
}
