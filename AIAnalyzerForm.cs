using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NetCrawler
{
    public partial class AIAnalyzerForm : Form
    {
        private AIAnalysisService aiService;
        private string currentAuditFile;

        public AIAnalyzerForm()
        {
            InitializeComponent();
            aiService = new AIAnalysisService(Path.Combine(Application.StartupPath, "AnalysisResults"));
        }

        private void InitializeComponent()
        {
            this.txtAuditFile = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnAnalyze = new System.Windows.Forms.Button();
            this.lblAuditFile = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtAuditFile
            // 
            this.txtAuditFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAuditFile.Location = new System.Drawing.Point(12, 25);
            this.txtAuditFile.Name = "txtAuditFile";
            this.txtAuditFile.Size = new System.Drawing.Size(460, 20);
            this.txtAuditFile.TabIndex = 0;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(478, 23);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnAnalyze
            // 
            this.btnAnalyze.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAnalyze.Location = new System.Drawing.Point(478, 52);
            this.btnAnalyze.Name = "btnAnalyze";
            this.btnAnalyze.Size = new System.Drawing.Size(75, 23);
            this.btnAnalyze.TabIndex = 2;
            this.btnAnalyze.Text = "Analyze";
            this.btnAnalyze.UseVisualStyleBackColor = true;
            this.btnAnalyze.Click += new System.EventHandler(this.btnAnalyze_Click);
            // 
            // lblAuditFile
            // 
            this.lblAuditFile.AutoSize = true;
            this.lblAuditFile.Location = new System.Drawing.Point(12, 9);
            this.lblAuditFile.Name = "lblAuditFile";
            this.lblAuditFile.Size = new System.Drawing.Size(52, 13);
            this.lblAuditFile.TabIndex = 3;
            this.lblAuditFile.Text = "Audit File:";
            // 
            // AIAnalyzerForm
            // 
            this.ClientSize = new System.Drawing.Size(565, 87);
            this.Controls.Add(this.lblAuditFile);
            this.Controls.Add(this.btnAnalyze);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtAuditFile);
            this.Name = "AIAnalyzerForm";
            this.Text = "AI File Analysis Tool";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox txtAuditFile;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.Label lblAuditFile;

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text Files|*.txt|All Files|*.*";
                openFileDialog.Title = "Select Audit File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtAuditFile.Text = openFileDialog.FileName;
                    currentAuditFile = openFileDialog.FileName;
                }
            }
        }

        private async void btnAnalyze_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtAuditFile.Text))
            {
                MessageBox.Show("Please select an audit file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var configForm = new AIForm())
            {
                if (configForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                var keywords = configForm.Keywords;
                var fileTypes = configForm.FileTypes;

                using (var progressForm = new ProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateStatus("Analyzing files...");

                    try
                    {
                        var results = await aiService.AnalyzeFiles(currentAuditFile, keywords, fileTypes, configForm.AnalyzeContent);

                        // Generate HTML report
                        var outputPath = Path.Combine(Path.GetDirectoryName(currentAuditFile), "analysis_results.html");
                        aiService.GenerateHtmlReport(results, outputPath);

                        progressForm.Close();

                        MessageBox.Show($"Analysis complete! HTML report generated at:\n{outputPath}\n\nOpen the report in your web browser to view the results.",
                                      "Analysis Complete",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        progressForm.Close();
                        MessageBox.Show($"Error during analysis: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
} 