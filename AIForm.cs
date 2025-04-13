using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace NetCrawler
{
    public partial class AIForm : Form
    {
        public List<string> Keywords { get; private set; }
        public List<string> FileTypes { get; private set; }
        public bool AnalyzeContent { get; private set; }

        public AIForm()
        {
            InitializeComponent();
            Keywords = new List<string>();
            FileTypes = new List<string>();
        }

        private void InitializeComponent()
        {
            this.txtKeywords = new TextBox();
            this.txtFileTypes = new TextBox();
            this.chkAnalyzeContent = new CheckBox();
            this.btnAnalyze = new Button();
            this.lblKeywords = new Label();
            this.lblFileTypes = new Label();
            this.SuspendLayout();

            // Keywords Label
            this.lblKeywords.AutoSize = true;
            this.lblKeywords.Location = new System.Drawing.Point(12, 15);
            this.lblKeywords.Name = "lblKeywords";
            this.lblKeywords.Size = new System.Drawing.Size(56, 13);
            this.lblKeywords.Text = "Keywords:";

            // Keywords TextBox
            this.txtKeywords.Location = new System.Drawing.Point(12, 31);
            this.txtKeywords.Multiline = true;
            this.txtKeywords.Name = "txtKeywords";
            this.txtKeywords.Size = new System.Drawing.Size(260, 60);
            this.txtKeywords.TabIndex = 0;

            // File Types Label
            this.lblFileTypes.AutoSize = true;
            this.lblFileTypes.Location = new System.Drawing.Point(12, 104);
            this.lblFileTypes.Name = "lblFileTypes";
            this.lblFileTypes.Size = new System.Drawing.Size(58, 13);
            this.lblFileTypes.Text = "File Types:";

            // File Types TextBox
            this.txtFileTypes.Location = new System.Drawing.Point(12, 120);
            this.txtFileTypes.Multiline = true;
            this.txtFileTypes.Name = "txtFileTypes";
            this.txtFileTypes.Size = new System.Drawing.Size(260, 60);
            this.txtFileTypes.TabIndex = 1;

            // Analyze Content CheckBox
            this.chkAnalyzeContent.AutoSize = true;
            this.chkAnalyzeContent.Location = new System.Drawing.Point(12, 186);
            this.chkAnalyzeContent.Name = "chkAnalyzeContent";
            this.chkAnalyzeContent.Size = new System.Drawing.Size(260, 17);
            this.chkAnalyzeContent.Text = "Analyze File Content (Slower but more thorough)";
            this.chkAnalyzeContent.TabIndex = 2;

            // Analyze Button
            this.btnAnalyze.Location = new System.Drawing.Point(12, 209);
            this.btnAnalyze.Name = "btnAnalyze";
            this.btnAnalyze.Size = new System.Drawing.Size(260, 30);
            this.btnAnalyze.Text = "Analyze Files";
            this.btnAnalyze.Click += new EventHandler(btnAnalyze_Click);

            // AIForm
            this.ClientSize = new System.Drawing.Size(284, 251);
            this.Controls.Add(this.btnAnalyze);
            this.Controls.Add(this.chkAnalyzeContent);
            this.Controls.Add(this.txtFileTypes);
            this.Controls.Add(this.lblFileTypes);
            this.Controls.Add(this.txtKeywords);
            this.Controls.Add(this.lblKeywords);
            this.Name = "AIForm";
            this.Text = "AI Risk Analysis Configuration";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private TextBox txtKeywords;
        private TextBox txtFileTypes;
        private CheckBox chkAnalyzeContent;
        private Button btnAnalyze;
        private Label lblKeywords;
        private Label lblFileTypes;

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            Keywords.Clear();
            FileTypes.Clear();

            // Parse keywords
            foreach (string keyword in txtKeywords.Text.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Keywords.Add(keyword.Trim());
            }

            // Parse file types
            foreach (string fileType in txtFileTypes.Text.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                FileTypes.Add(fileType.Trim().ToLower());
            }

            AnalyzeContent = chkAnalyzeContent.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
} 