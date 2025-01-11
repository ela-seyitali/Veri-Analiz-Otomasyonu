namespace VeriAnalizOtomasyonu
{
    partial class NullVeriDoldurmaForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.comboBoxSutunlar = new System.Windows.Forms.ComboBox();
            this.comboBoxYontem = new System.Windows.Forms.ComboBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.textBoxFixedValue = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // comboBoxSutunlar
            // 
            this.comboBoxSutunlar.FormattingEnabled = true;
            this.comboBoxSutunlar.Location = new System.Drawing.Point(24, 33);
            this.comboBoxSutunlar.Name = "comboBoxSutunlar";
            this.comboBoxSutunlar.Size = new System.Drawing.Size(256, 21);
            this.comboBoxSutunlar.TabIndex = 0;
            // 
            // comboBoxYontem
            // 
            this.comboBoxYontem.FormattingEnabled = true;
            this.comboBoxYontem.Location = new System.Drawing.Point(24, 83);
            this.comboBoxYontem.Name = "comboBoxYontem";
            this.comboBoxYontem.Size = new System.Drawing.Size(256, 21);
            this.comboBoxYontem.TabIndex = 1;
            this.comboBoxYontem.SelectedIndexChanged += new System.EventHandler(this.comboBoxYontem_SelectedIndexChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(124, 171);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "İptal";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(205, 171);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "Doldur";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // textBoxFixedValue
            // 
            this.textBoxFixedValue.Location = new System.Drawing.Point(24, 120);
            this.textBoxFixedValue.Name = "textBoxFixedValue";
            this.textBoxFixedValue.Size = new System.Drawing.Size(256, 20);
            this.textBoxFixedValue.TabIndex = 4;
            this.textBoxFixedValue.Visible = false;
            // 
            // NullVeriDoldurmaForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 206);
            this.Controls.Add(this.textBoxFixedValue);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.comboBoxYontem);
            this.Controls.Add(this.comboBoxSutunlar);
            this.Name = "NullVeriDoldurmaForm";
            this.Text = "NullVeriDoldurmaForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxSutunlar;
        private System.Windows.Forms.ComboBox comboBoxYontem;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TextBox textBoxFixedValue;
    }
}