using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;
using System;
using System.Data.SqlClient;

namespace VeriAnalizOtomasyonu
{


    public partial class üyeFormu : DevExpress.XtraEditors.XtraForm
    {
        public üyeFormu()
        {
            InitializeComponent();
        }

        private void labelControl2_Click(object sender, EventArgs e)
        {

        }

        private void üyeFormu_Load(object sender, EventArgs e)
        {
            KadınErkekChBx.ItemCheck += KadınErkekChBx_ItemCheck;
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            /* kullanıcı tıkladığında başarılı işelm olursa kullanıcının bilgilerini 
             veri tabanı üzerinde  kaydetmemiz gerekir.
             ardından kullanıcıyı giriş formuna tekrar yönlendiririz. kullanıcı isterse
            sisteme giriş yapabilir. 
             
             */
        }

        private void KadınErkekChBx_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            for (int i = 0; i < KadınErkekChBx.Items.Count; i++)
            {
                if (i != e.Index)
                {
                    KadınErkekChBx.SetItemChecked(i, false);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void checkAccount(string username)
        {
            try
            {
                if (DatabaseHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM [User] WHERE UserName = @UserName",
                    cmd => cmd.Parameters.AddWithValue("@UserName", username)) > 0)
                {
                    MessageBox.Show("Bu kullanıcı adı zaten kullanılıyor.",
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string cinsiyet = KadınErkekChBx.CheckedItems[0].ToString();
                insertData(kullanıcıAdı.Text, meslek.Text, cinsiyet, girişFormu.HashPassword(şifre.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcı kontrolü sırasında bir hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void insertData(string kullanıcıAdı, string meslek, string cinsiyet, string şifre)
        {
            try
            {
                string insertQuery = @"
                    INSERT INTO [User](UserName, Password, Gender, Profession, Fullname) 
                    VALUES(@UserName, @Password, @Gender, @Profession, @Fullname)";

                DatabaseHelper.ExecuteNonQuery(insertQuery, cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserName", kullanıcıAdı);
                    cmd.Parameters.AddWithValue("@Password", şifre);
                    cmd.Parameters.AddWithValue("@Gender", cinsiyet == "Kadın" ? 1 : 2);
                    cmd.Parameters.AddWithValue("@Profession", meslek);
                    cmd.Parameters.AddWithValue("@Fullname", textBox1.Text);
                });

                ShowSuccessForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcı kaydı sırasında hata: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSuccessForm()
        {
            var successForm = new Form
            {
                Text = "Başarılı",
                Width = 300,
                Height = 200,
                StartPosition = FormStartPosition.CenterScreen
            };

            var lblMessage = new Label
            {
                Text = "Kullanıcı başarıyla kaydedildi!",
                AutoSize = true,
                Location = new Point(20, 40),
                Font = new Font("Arial", 11, FontStyle.Bold)
            };
            successForm.Controls.Add(lblMessage);

            var btnLogin = new Button
            {
                Text = "Giriş Yap",
                Width = 100,
                Height = 30,
                Location = new Point(60, 100)
            };
            btnLogin.Click += (s, e) =>
            {
                this.Hide();
                using (var loginForm = new girişFormu())
                {
                    successForm.Close();
                    loginForm.ShowDialog();
                }
                this.Close();
            };
            successForm.Controls.Add(btnLogin);

            var btnGoHome = new Button
            {
                Text = "Ana Sayfaya Dön",
                Width = 100,
                Height = 30,
                Location = new Point(160, 100)
            };
            btnGoHome.Click += (s, e) =>
            {
                this.Hide();
                using (var homeForm = new başlangıçFormu())
                {
                    successForm.Close();
                    homeForm.ShowDialog();
                }
                this.Close();
            };
            successForm.Controls.Add(btnGoHome);

            successForm.ShowDialog();
        }

        private void kullanıcıAdı_TextChanged(object sender, EventArgs e)
        {

        }

        private void simpleButton1_Click_1(object sender, EventArgs e)
        {
            if (ValidateInputs())
            {
                checkAccount(kullanıcıAdı.Text);
            }
        }

        private bool ValidateInputs()
        {
            // Boş alan kontrolü
            if (string.IsNullOrWhiteSpace(kullanıcıAdı.Text) ||
                string.IsNullOrWhiteSpace(meslek.Text) ||
                string.IsNullOrWhiteSpace(şifre.Text) ||
                string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Kullanıcı adında boşluk kontrolü
            if (kullanıcıAdı.Text.Contains(" "))
            {
                MessageBox.Show("Kullanıcı adı boşluk içeremez.",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Cinsiyet seçimi kontrolü
            if (KadınErkekChBx.CheckedItems.Count == 0)
            {
                MessageBox.Show("Lütfen cinsiyet seçiniz.",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Şifre uzunluğu kontrolü
            if (şifre.Text.Length < 8)
            {
                MessageBox.Show("Şifre en az 8 karakter uzunluğunda olmalıdır.",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }

}
