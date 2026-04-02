namespace CodeManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            CodeClonesManager ccm = new CodeClonesManager();
            ccm.MdiParent = this;
            ccm.Show();
        }
    }
}
