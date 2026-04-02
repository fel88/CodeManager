using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using System.Windows.Controls;

namespace CodeManager
{
    /// <summary>
    /// Interaction logic for CodeEditor.xaml
    /// </summary>
    public partial class CodeEditor : System.Windows.Controls.UserControl
    {
        public CodeEditor()
        {
            InitializeComponent();
            SearchPanel.Install(textEditor);            
        }
        public TextEditor TextEditor => textEditor;
        
        public string Text { get => textEditor.Text; set => textEditor.Text = value; }
    }
}
