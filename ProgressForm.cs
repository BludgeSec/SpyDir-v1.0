using System;
using System.Windows.Forms;

namespace NetCrawler
{
    public partial class ProgressForm : Form
    {
        public ProgressForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.progressBar = new ProgressBar();
            this.lblStatus = new Label();
            this.SuspendLayout();

            // Progress Bar
            this.progressBar.Style = ProgressBarStyle.Marquee;
            this.progressBar.Location = new System.Drawing.Point(12, 12);
            this.progressBar.Size = new System.Drawing.Size(260, 23);
            this.progressBar.TabIndex = 0;

            // Status Label
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 38);
            this.lblStatus.Size = new System.Drawing.Size(260, 13);
            this.lblStatus.Text = "Analyzing files...";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // ProgressForm
            this.ClientSize = new System.Drawing.Size(284, 61);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Analysis Progress";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private ProgressBar progressBar;
        private Label lblStatus;

        public void UpdateStatus(string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(status)));
                return;
            }
            lblStatus.Text = status;
        }
    }
} 