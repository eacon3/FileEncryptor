using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileEncryptor
{
    /// <summary>
    /// Batch operation form for encrypting and decrypting multiple files
    /// </summary>
    public partial class BatchOperationForm : Form
    {
        private EncryptionService _encryptionService;
        private List<string> _fileList;

        /// <summary>
        /// Constructor for BatchOperationForm
        /// </summary>
        public BatchOperationForm()
        {
            InitializeComponent();
            _encryptionService = new EncryptionService();
            _fileList = new List<string>();
        }

        /// <summary>
        /// Initializes form components
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // BatchOperationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 500);
            this.Name = "BatchOperationForm";
            this.Text = "Batch Operations";
            this.Load += new System.EventHandler(this.BatchOperationForm_Load);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.BatchOperationForm_DragEnter);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.BatchOperationForm_DragDrop);
            this.ResumeLayout(false);

            // Create UI elements
            CreateUI();
        }

        /// <summary>
        /// Creates all UI elements for the batch operation form
        /// </summary>
        private void CreateUI()
        {
            // File list label
            Label lblFileList = new Label
            {
                Location = new System.Drawing.Point(20, 30),
                Size = new System.Drawing.Size(100, 20),
                Text = "File List:" 
            };
            this.Controls.Add(lblFileList);

            // File list box
            ListBox lstFiles = new ListBox
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(560, 200),
                Name = "lstFiles"
            };
            this.Controls.Add(lstFiles);

            // Add files button
            Button btnAddFiles = new Button
            {
                Location = new System.Drawing.Point(20, 260),
                Size = new System.Drawing.Size(100, 30),
                Text = "Add Files",
                Name = "btnAddFiles"
            };
            btnAddFiles.Click += BtnAddFiles_Click;
            this.Controls.Add(btnAddFiles);

            // Add directory button
            Button btnAddDirectory = new Button
            {
                Location = new System.Drawing.Point(130, 260),
                Size = new System.Drawing.Size(100, 30),
                Text = "Add Directory",
                Name = "btnAddDirectory"
            };
            btnAddDirectory.Click += BtnAddDirectory_Click;
            this.Controls.Add(btnAddDirectory);

            // Clear button
            Button btnClear = new Button
            {
                Location = new System.Drawing.Point(240, 260),
                Size = new System.Drawing.Size(100, 30),
                Text = "Clear",
                Name = "btnClear"
            };
            btnClear.Click += BtnClear_Click;
            this.Controls.Add(btnClear);

            // Password label
            Label lblPassword = new Label
            {
                Location = new System.Drawing.Point(20, 300),
                Size = new System.Drawing.Size(100, 20),
                Text = "Password:" 
            };
            this.Controls.Add(lblPassword);

            // Password textbox
            TextBox txtPassword = new TextBox
            {
                Location = new System.Drawing.Point(120, 300),
                Size = new System.Drawing.Size(350, 23),
                UseSystemPasswordChar = true,
                Name = "txtPassword",
                PlaceholderText = "Enter encryption/decryption password"
            };
            txtPassword.GotFocus += TxtPassword_GotFocus;
            txtPassword.KeyDown += TxtPassword_KeyDown;
            this.Controls.Add(txtPassword);

            // Output directory label
            Label lblOutputDir = new Label
            {
                Location = new System.Drawing.Point(20, 340),
                Size = new System.Drawing.Size(100, 20),
                Text = "Output Directory:" 
            };
            this.Controls.Add(lblOutputDir);

            // Output directory textbox
            TextBox txtOutputDir = new TextBox
            {
                Location = new System.Drawing.Point(120, 340),
                Size = new System.Drawing.Size(350, 23),
                Name = "txtOutputDir",
                PlaceholderText = "Select output directory"
            };
            this.Controls.Add(txtOutputDir);

            // Output directory browse button
            Button btnBrowseOutputDir = new Button
            {
                Location = new System.Drawing.Point(480, 338),
                Size = new System.Drawing.Size(100, 23),
                Text = "Browse...",
                Name = "btnBrowseOutputDir"
            };
            btnBrowseOutputDir.Click += BtnBrowseOutputDir_Click;
            this.Controls.Add(btnBrowseOutputDir);

            // Encrypt button
            Button btnEncrypt = new Button
            {
                Location = new System.Drawing.Point(120, 380),
                Size = new System.Drawing.Size(100, 30),
                Text = "Batch Encrypt",
                Name = "btnEncrypt"
            };
            btnEncrypt.Click += BtnEncrypt_Click;
            this.Controls.Add(btnEncrypt);

            // Decrypt button
            Button btnDecrypt = new Button
            {
                Location = new System.Drawing.Point(240, 380),
                Size = new System.Drawing.Size(100, 30),
                Text = "Batch Decrypt",
                Name = "btnDecrypt"
            };
            btnDecrypt.Click += BtnDecrypt_Click;
            this.Controls.Add(btnDecrypt);

            // Cancel button
            Button btnCancel = new Button
            {
                Location = new System.Drawing.Point(360, 380),
                Size = new System.Drawing.Size(100, 30),
                Text = "Cancel",
                Name = "btnCancel"
            };
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);

            // Progress bar
            ProgressBar progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(20, 430),
                Size = new System.Drawing.Size(560, 20),
                Name = "progressBar"
            };
            this.Controls.Add(progressBar);

            // Status label
            Label lblStatus = new Label
            {
                Location = new System.Drawing.Point(20, 460),
                Size = new System.Drawing.Size(560, 20),
                Text = "Ready",
                Name = "lblStatus"
            };
            this.Controls.Add(lblStatus);

            // Drag and drop instruction
            Label lblDragDrop = new Label
            {
                Location = new System.Drawing.Point(20, 480),
                Size = new System.Drawing.Size(560, 20),
                Text = "Drag and drop files or folders here",
                Name = "lblDragDrop"
            };
            this.Controls.Add(lblDragDrop);
        }

        /// <summary>
        /// Form load event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BatchOperationForm_Load(object sender, EventArgs e)
        {
            // Set default output directory to desktop
            TextBox txtOutputDir = this.Controls.Find("txtOutputDir", true).FirstOrDefault() as TextBox;
            if (txtOutputDir != null)
            {
                txtOutputDir.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

        /// <summary>
        /// Drag enter event handler for drag and drop functionality
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BatchOperationForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// Drag drop event handler for drag and drop functionality
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BatchOperationForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddItems(items);
            }
        }

        /// <summary>
        /// Add files button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnAddFiles_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Files",
                Filter = "All files (*.*)|*.*|Encrypted files (*.cls)|*.cls",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                AddItems(openFileDialog.FileNames);
            }
        }

        /// <summary>
        /// Add directory button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnAddDirectory_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select Directory"
            };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string[] files = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*", SearchOption.AllDirectories);
                AddItems(files);
            }
        }

        /// <summary>
        /// Clear button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnClear_Click(object sender, EventArgs e)
        {
            ListBox lstFiles = this.Controls.Find("lstFiles", true).FirstOrDefault() as ListBox;
            if (lstFiles != null)
            {
                lstFiles.Items.Clear();
                _fileList.Clear();
            }
        }

        /// <summary>
        /// Browse output directory button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnBrowseOutputDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select Output Directory"
            };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                TextBox txtOutputDir = this.Controls.Find("txtOutputDir", true).FirstOrDefault() as TextBox;
                if (txtOutputDir != null)
                {
                    txtOutputDir.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// Batch encrypt button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void BtnEncrypt_Click(object sender, EventArgs e)
        {
            TextBox txtPassword = this.Controls.Find("txtPassword", true).FirstOrDefault() as TextBox;
            TextBox txtOutputDir = this.Controls.Find("txtOutputDir", true).FirstOrDefault() as TextBox;
            ProgressBar progressBar = this.Controls.Find("progressBar", true).FirstOrDefault() as ProgressBar;
            Label lblStatus = this.Controls.Find("lblStatus", true).FirstOrDefault() as Label;

            if (txtPassword == null || txtOutputDir == null || progressBar == null || lblStatus == null)
            {
                MessageBox.Show("UI elements not loaded properly", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string password = txtPassword.Text;
            string outputDir = txtOutputDir.Text;

            if (_fileList.Count == 0)
            {
                MessageBox.Show("Please add files to encrypt", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter a password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Please select a valid output directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                lblStatus.Text = "Encrypting files...";
                progressBar.Value = 0;

                await _encryptionService.BatchEncryptAsync(_fileList, outputDir, password, (progress) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action<int>((p) => progressBar.Value = p), progress);
                    }
                    else
                    {
                        progressBar.Value = progress;
                    }
                });

                lblStatus.Text = "Encryption completed successfully";
                MessageBox.Show("All files encrypted successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Encryption failed";
                MessageBox.Show($"Error encrypting files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Batch decrypt button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void BtnDecrypt_Click(object sender, EventArgs e)
        {
            TextBox txtPassword = this.Controls.Find("txtPassword", true).FirstOrDefault() as TextBox;
            TextBox txtOutputDir = this.Controls.Find("txtOutputDir", true).FirstOrDefault() as TextBox;
            ProgressBar progressBar = this.Controls.Find("progressBar", true).FirstOrDefault() as ProgressBar;
            Label lblStatus = this.Controls.Find("lblStatus", true).FirstOrDefault() as Label;

            if (txtPassword == null || txtOutputDir == null || progressBar == null || lblStatus == null)
            {
                MessageBox.Show("UI elements not loaded properly", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string password = txtPassword.Text;
            string outputDir = txtOutputDir.Text;

            if (_fileList.Count == 0)
            {
                MessageBox.Show("Please add files to decrypt", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if all files are .cls files
            foreach (string file in _fileList)
            {
                if (Path.GetExtension(file).ToLower() != ".cls")
                {
                    MessageBox.Show("Only .cls files can be decrypted", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter a password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Please select a valid output directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                lblStatus.Text = "Decrypting files...";
                progressBar.Value = 0;

                await _encryptionService.BatchDecryptAsync(_fileList, outputDir, password, (progress) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action<int>((p) => progressBar.Value = p), progress);
                    }
                    else
                    {
                        progressBar.Value = progress;
                    }
                });

                lblStatus.Text = "Decryption completed successfully";
                MessageBox.Show("All files decrypted successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Decryption failed";
                MessageBox.Show($"Error decrypting files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Cancel button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Adds items (files or directories) to the file list
        /// </summary>
        /// <param name="items">Array of items to add</param>
        private void AddItems(string[] items)
        {
            ListBox lstFiles = this.Controls.Find("lstFiles", true).FirstOrDefault() as ListBox;
            if (lstFiles != null)
            {
                foreach (string item in items)
                {
                    if (Directory.Exists(item))
                    {
                        // Add all files in directory
                        string[] files = Directory.GetFiles(item, "*", SearchOption.AllDirectories);
                        foreach (string file in files)
                        {
                            if (!_fileList.Contains(file))
                            {
                                _fileList.Add(file);
                                lstFiles.Items.Add(file);
                            }
                        }
                    }
                    else if (File.Exists(item))
                    {
                        // Add single file
                        if (!_fileList.Contains(item))
                        {
                            _fileList.Add(item);
                            lstFiles.Items.Add(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Password textbox got focus event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void TxtPassword_GotFocus(object sender, EventArgs e)
        {
            // Check if Caps Lock is on
            if (Control.IsKeyLocked(Keys.CapsLock))
            {
                MessageBox.Show("Caps Lock is on. Passwords are case-sensitive!", "Caps Lock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Password textbox key down event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if Caps Lock is on
            if (Control.IsKeyLocked(Keys.CapsLock))
            {
                MessageBox.Show("Caps Lock is on. Passwords are case-sensitive!", "Caps Lock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
