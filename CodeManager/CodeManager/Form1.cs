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

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            SimilarFiles ccm = new SimilarFiles();
            ccm.MdiParent = this;
            ccm.Show();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            MethodsClonesManager ccm = new MethodsClonesManager();
            ccm.MdiParent = this;
            ccm.Show();
        }
    }
}
