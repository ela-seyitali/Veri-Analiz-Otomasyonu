using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Drawing;

namespace VeriAnalizOtomasyonu
{
    public partial class başlangıçFormu : DevExpress.XtraEditors.XtraForm
    {
        public başlangıçFormu()
        {
            InitializeComponent();
            this.TransparencyKey = Color.Empty;
            this.BackColor = Color.White;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void labelControl1_Click(object sender, EventArgs e)
        {

        }

        private void rjButton1_Click(object sender, EventArgs e)
        {
            girişFormu grfrm = new girişFormu();
            grfrm.ShowDialog();
        }

        private void ÜyeOlGG_btn_Click(object sender, EventArgs e)
        {
            üyeFormu üfrm = new üyeFormu();
            üfrm.ShowDialog();
        }
    }
}
