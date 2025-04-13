using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetCrawler_v1.2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async Task<List<string>> GetSharedFoldersAndSubItemsAsync(string ipAddress)
        {
            var folders = new List<string>();
            try
            {
                var shares = GetSharedFoldersUsingNetView(ipAddress);
                foreach (var share in shares)
                {
                    folders.Add(share);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting shared folders: {ex.Message}");
            }
            return folders;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // ... existing code ...
            try
            {
                // Write the analysis to a file using synchronous method
                System.IO.File.WriteAllText(outputPath, analysis);
                MessageBox.Show($"Analysis saved to: {outputPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving analysis: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // ... existing code ...
        }
    }
} 