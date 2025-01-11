using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace VeriAnalizOtomasyonu
{
    public partial class NullVeriDoldurmaForm : Form
    {
        public string SelectedColumn { get; private set; }
        public string SelectedMethod { get; private set; }
        public string FixedValue { get; private set; } // Sabit değer için özellik

        public NullVeriDoldurmaForm(List<string> sayisalSutunlar)
        {
            InitializeComponent();

            // Sütun isimlerini ComboBox'a ekle
            comboBoxSutunlar.Items.AddRange(sayisalSutunlar.ToArray());

            // Doldurma yöntemlerini ComboBox'a ekle
            comboBoxYontem.Items.AddRange(new[] { "Ortalama", "Medyan", "Sabit Değer" });

            // Yöntem seçildiğinde sabit değer TextBox'ını göster
            comboBoxYontem.SelectedIndexChanged += (sender, e) =>
            {
                if (comboBoxYontem.SelectedItem.ToString() == "Sabit Değer")
                {
                    textBoxFixedValue.Visible = true;
                }
                else
                {
                    textBoxFixedValue.Visible = false;
                }
            };
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (comboBoxSutunlar.SelectedItem != null && comboBoxYontem.SelectedItem != null)
            {
                SelectedColumn = comboBoxSutunlar.SelectedItem.ToString();
                SelectedMethod = comboBoxYontem.SelectedItem.ToString();

                if (SelectedMethod == "Sabit Değer")
                {
                    if (string.IsNullOrWhiteSpace(textBoxFixedValue.Text))
                    {
                        MessageBox.Show("Lütfen bir sabit değer girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    FixedValue = textBoxFixedValue.Text;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Lütfen bir sütun ve doldurma yöntemi seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void comboBoxYontem_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }

}