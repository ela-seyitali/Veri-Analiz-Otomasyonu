using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;

namespace VeriAnalizOtomasyonu
{
    public partial class girişFormu : DevExpress.XtraEditors.XtraForm
    {
        public girişFormu()
        {
            InitializeComponent();
        }

        private void kullanıcıAdı_TextChanged(object sender, EventArgs e)
        {

        }

        private void girişFormu_Load(object sender, EventArgs e)
        {

        }

        private void sistemeGiriş_Click(object sender, EventArgs e)
        {

        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(kullanıcıAdı.Text) || string.IsNullOrEmpty(şifre.Text))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve şifreyi girin.", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try 
            {
                checkAccount(kullanıcıAdı.Text, HashPassword(şifre.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Giriş sırasında bir hata oluştu: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkAccount(string username, string password)
        {
            try
            {
                using (var connection = DatabaseHelper.OpenConnection())
                {
                    string query = "SELECT UserId FROM [User] WHERE UserName = @UserName AND Password = @Password";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserName", username);
                        command.Parameters.AddWithValue("@Password", password);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {   
                                int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                                MessageBox.Show("Giriş başarılı! Uygulamaya hoş geldiniz.", 
                                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                
                                this.Hide();
                                using (var mainForm = new mainForm(userId))
                                {
                                    mainForm.ShowDialog();
                                }
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Kullanıcı adı veya şifre yanlış.", 
                                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Giriş kontrolü sırasında hata: " + ex.Message, ex);
            }
        }

        private void ŞifreyiDeğiştir_HyperLink_Click(object sender, EventArgs e)
        {
            var result = KullanıcıGirişFormu(out string userName, 
                out string currentPassword, out string newPassword);

            if (!result || string.IsNullOrEmpty(userName) || 
                string.IsNullOrEmpty(currentPassword) || 
                string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("İşlem iptal edildi veya eksik bilgi girildi.", 
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (newPassword.Length < 8)
            {
                MessageBox.Show("Yeni şifre en az 8 karakter olmalıdır.", 
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Kullanıcı kontrolü
                string checkQuery = @"
                    SELECT COUNT(*) 
                    FROM [User] 
                    WHERE UserName = @UserName AND Password = @CurrentPassword";

                int userExists = DatabaseHelper.ExecuteScalar<int>(checkQuery, cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    cmd.Parameters.AddWithValue("@CurrentPassword", HashPassword(currentPassword));
                });

                if (userExists == 0)
                {
                    MessageBox.Show("Kullanıcı adı veya mevcut şifre yanlış.", 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Şifre güncelleme
                string updateQuery = @"
                    UPDATE [User] 
                    SET Password = @NewPassword 
                    WHERE UserName = @UserName";

                try
                {
                    DatabaseHelper.ExecuteNonQuery(updateQuery, cmd =>
                    {
                        cmd.Parameters.AddWithValue("@NewPassword", HashPassword(newPassword));
                        cmd.Parameters.AddWithValue("@UserName", userName);
                    });

                    MessageBox.Show("Şifreniz başarıyla değiştirildi.", 
                        "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                    MessageBox.Show("Şifre değiştirme işlemi başarısız oldu.", 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şifre değiştirme sırasında hata: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Kullanıcı giriş formunu oluşturan metod
        private bool KullanıcıGirişFormu(out string userName, out string currentPassword, out string newPassword)
        {
            userName = string.Empty;
            currentPassword = string.Empty;
            newPassword = string.Empty;

            Form form = new Form()
            {
                Width = 400,
                Height = 300,
                Text = "Şifre Değiştir",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen
            };

            Label lblUserName = new Label() { Left = 20, Top = 20, Text = "Kullanıcı Adı:", Width = 120 };
            TextBox txtUserName = new TextBox() { Left = 150, Top = 20, Width = 200 };

            Label lblCurrentPassword = new Label() { Left = 20, Top = 60, Text = "Mevcut Şifre:", Width = 120 };
            TextBox txtCurrentPassword = new TextBox() { Left = 150, Top = 60, Width = 200, PasswordChar = '*' };

            Label lblNewPassword = new Label() { Left = 20, Top = 100, Text = "Yeni Şifre:", Width = 120 };
            TextBox txtNewPassword = new TextBox() { Left = 150, Top = 100, Width = 200, PasswordChar = '*' };

            Button btnOk = new Button() { Text = "Tamam", Left = 200, Width = 90, Top = 160, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "İptal", Left = 100, Width = 90, Top = 160, DialogResult = DialogResult.Cancel };

            btnOk.Click += (sender, e) => { form.Close(); };
            btnCancel.Click += (sender, e) => { form.Close(); };

            form.Controls.Add(lblUserName);
            form.Controls.Add(txtUserName);
            form.Controls.Add(lblCurrentPassword);
            form.Controls.Add(txtCurrentPassword);
            form.Controls.Add(lblNewPassword);
            form.Controls.Add(txtNewPassword);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);

            form.AcceptButton = btnOk;

            var dialogResult = form.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                userName = txtUserName.Text;
                currentPassword = txtCurrentPassword.Text;
                newPassword = txtNewPassword.Text;
                return true;
            }
            return false;
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

    }
}
