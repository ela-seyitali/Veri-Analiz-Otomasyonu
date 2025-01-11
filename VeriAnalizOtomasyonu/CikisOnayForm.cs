using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VeriAnalizOtomasyonu
{
    public partial class CikisOnayForm : DevExpress.XtraEditors.XtraForm
    {
        public bool IsConfirmed { get; private set; } = false;

        public CikisOnayForm()
        {
            InitializeComponent();
        }

        
      

        private void btnIptalEt_Click_1(object sender, EventArgs e)
        {

            IsConfirmed = false;
            this.Close();
        }

        private void btnCikisYap_Click_1(object sender, EventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }
    }
}