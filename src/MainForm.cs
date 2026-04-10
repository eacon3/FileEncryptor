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
    /// Main form of the File Encryptor application
    /// </summary>
    public partial class MainForm : Form
    {
        private EncryptionService _encryptionService;
        private string _inputFile;
        private string _outputDirectory;

        /// <summary>
        /// Constructor for MainForm
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            _encryptionService = new EncryptionService();
            UpdateStatus("Ready", true);
        }

        /// <summary>
        /// Initializes form components
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Name = "MainForm";
            this.Text = "File Encryptor";
            this.AllowDrop = true;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.ResumeLayout(false);

            // Create UI elements
            CreateUI();
        }

        /// <summary>
        /// Creates all UI elements for the main form
        /// </summary>
        private void CreateUI()
        {
            // Set form properties for better appearance
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Create a panel for the main content with a border
            Panel mainPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(580, 420),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(mainPanel);

            // Header label
            Label lblHeader = new Label
            {
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(540, 30),
                Text = "File Encryptor",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(64, 64, 64),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblHeader);

            // Input file label
            Label lblInputFile = new Label
            {
                Location = new System.Drawing.Point(40, 70),
                Size = new System.Drawing.Size(100, 20),
                Text = "Input File:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            mainPanel.Controls.Add(lblInputFile);

            // Input file textbox
            TextBox txtInputFile = new TextBox
            {
                Location = new System.Drawing.Point(150, 70),
                Size = new System.Drawing.Size(320, 23),
                Name = "txtInputFile",
                PlaceholderText = "Select a file to encrypt/decrypt",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(txtInputFile);

            // Input file browse button
            Button btnBrowseInput = new Button
            {
                Location = new System.Drawing.Point(480, 68),
                Size = new System.Drawing.Size(90, 23),
                Text = "Browse...",
                Name = "btnBrowseInput",
                Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBrowseInput.FlatAppearance.BorderSize = 0;
            btnBrowseInput.Click += BtnBrowseInput_Click;
            mainPanel.Controls.Add(btnBrowseInput);

            // Output directory label
            Label lblOutputDir = new Label
            {
                Location = new System.Drawing.Point(40, 105),
                Size = new System.Drawing.Size(100, 20),
                Text = "Output Directory:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            mainPanel.Controls.Add(lblOutputDir);

            // Output directory textbox
            TextBox txtOutputDir = new TextBox
            {
                Location = new System.Drawing.Point(150, 105),
                Size = new System.Drawing.Size(320, 23),
                Name = "txtOutputDir",
                PlaceholderText = "Select output directory",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(txtOutputDir);

            // Output directory browse button
            Button btnBrowseOutput = new Button
            {
                Location = new System.Drawing.Point(480, 103),
                Size = new System.Drawing.Size(90, 23),
                Text = "Browse...",
                Name = "btnBrowseOutput",
                Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBrowseOutput.FlatAppearance.BorderSize = 0;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            mainPanel.Controls.Add(btnBrowseOutput);

            // Password label
            Label lblPassword = new Label
            {
                Location = new System.Drawing.Point(40, 140),
                Size = new System.Drawing.Size(100, 20),
                Text = "Password:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            mainPanel.Controls.Add(lblPassword);

            // Password textbox
            TextBox txtPassword = new TextBox
            {
                Location = new System.Drawing.Point(150, 140),
                Size = new System.Drawing.Size(320, 23),
                UseSystemPasswordChar = true,
                Name = "txtPassword",
                PlaceholderText = "Enter encryption/decryption password",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtPassword.GotFocus += TxtPassword_GotFocus;
            txtPassword.KeyDown += TxtPassword_KeyDown;
            mainPanel.Controls.Add(txtPassword);

            // Generate password button
            Button btnGeneratePassword = new Button
            {
                Location = new System.Drawing.Point(480, 138),
                Size = new System.Drawing.Size(90, 23),
                Text = "Generate Key",
                Name = "btnGeneratePassword",
                Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnGeneratePassword.FlatAppearance.BorderSize = 0;
            btnGeneratePassword.Click += BtnGeneratePassword_Click;
            mainPanel.Controls.Add(btnGeneratePassword);

            // Button panel for better organization
            Panel buttonPanel = new Panel
            {
                Location = new System.Drawing.Point(40, 180),
                Size = new System.Drawing.Size(510, 40),
                BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(buttonPanel);

            // Encrypt button
            Button btnEncrypt = new Button
            {
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(110, 30),
                Text = "Encrypt",
                Name = "btnEncrypt",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnEncrypt.FlatAppearance.BorderSize = 0;
            btnEncrypt.Click += BtnEncrypt_Click;
            buttonPanel.Controls.Add(btnEncrypt);

            // Decrypt button
            Button btnDecrypt = new Button
            {
                Location = new System.Drawing.Point(120, 0),
                Size = new System.Drawing.Size(110, 30),
                Text = "Decrypt",
                Name = "btnDecrypt",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDecrypt.FlatAppearance.BorderSize = 0;
            btnDecrypt.Click += BtnDecrypt_Click;
            buttonPanel.Controls.Add(btnDecrypt);

            // Batch operation button
            Button btnBatch = new Button
            {
                Location = new System.Drawing.Point(240, 0),
                Size = new System.Drawing.Size(120, 30),
                Text = "Batch Operation",
                Name = "btnBatch",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBatch.FlatAppearance.BorderSize = 0;
            btnBatch.Click += BtnBatch_Click;
            buttonPanel.Controls.Add(btnBatch);

            // Settings button
            Button btnSettings = new Button
            {
                Location = new System.Drawing.Point(370, 0),
                Size = new System.Drawing.Size(110, 30),
                Text = "Settings",
                Name = "btnSettings",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Click += BtnSettings_Click;
            buttonPanel.Controls.Add(btnSettings);

            // Progress bar with better appearance
            ProgressBar progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(40, 230),
                Size = new System.Drawing.Size(510, 20),
                Name = "progressBar",
                Style = ProgressBarStyle.Continuous,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(52, 152, 219)
            };
            mainPanel.Controls.Add(progressBar);

            // Status indicator with rounded corners
            Panel statusIndicator = new Panel
            {
                Location = new System.Drawing.Point(40, 270),
                Size = new System.Drawing.Size(20, 20),
                BackColor = Color.Green,
                Name = "statusIndicator"
            };
            // Set rounded corners
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, statusIndicator.Width, statusIndicator.Height);
            statusIndicator.Region = new Region(path);
            mainPanel.Controls.Add(statusIndicator);

            // Status text
            Label lblStatusText = new Label
            {
                Location = new System.Drawing.Point(70, 270),
                Size = new System.Drawing.Size(480, 20),
                Text = "Status: Ready",
                Name = "lblStatusText",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            mainPanel.Controls.Add(lblStatusText);

            // File format information
            Label lblFileFormat = new Label
            {
                Location = new System.Drawing.Point(40, 310),
                Size = new System.Drawing.Size(510, 20),
                Text = "Encrypted files are saved with .cls extension. Only .cls files can be decrypted.",
                Name = "lblFileFormat",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblFileFormat);

            // Application information
            Label lblInfo = new Label
            {
                Location = new System.Drawing.Point(40, 340),
                Size = new System.Drawing.Size(510, 20),
                Text = "File Encryptor v1.0 - Secure file encryption using AES-GCM with Argon2id key derivation",
                Name = "lblInfo",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblInfo);

            // Update form size to fit new layout
            this.Size = new Size(600, 450);
        }

        /// <summary>
        /// Form load event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set default output directory to desktop
            TextBox txtOutputDir = this.Controls.Find("txtOutputDir", true).FirstOrDefault() as TextBox;
            if (txtOutputDir != null)
            {
                txtOutputDir.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                _outputDirectory = txtOutputDir.Text;
            }
        }

        /// <summary>
        /// Browse input file button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnBrowseInput_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select File",
                Filter = "All files (*.*)|*.*|Encrypted files (*.cls)|*.cls"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                TextBox txtInputFile = this.Controls.Find("txtInputFile", true).FirstOrDefault() as TextBox;
                if (txtInputFile != null)
                {
                    txtInputFile.Text = openFileDialog.FileName;
                    _inputFile = openFileDialog.FileName;
                }
            }
        }

        /// <summary>
        /// Browse output directory button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnBrowseOutput_Click(object sender, EventArgs e)
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
                    _outputDirectory = folderBrowserDialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// Generate password button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnGeneratePassword_Click(object sender, EventArgs e)
        {
            // Create password generator form
            Form passwordGeneratorForm = new Form
            {
                Size = new Size(400, 300),
                Text = "Password Generator",
                StartPosition = FormStartPosition.CenterParent
            };

            // Password length label
            Label lblLength = new Label
            {
                Location = new Point(20, 30),
                Size = new Size(100, 20),
                Text = "Password Length:"
            };
            passwordGeneratorForm.Controls.Add(lblLength);

            // Password length numeric up-down
            NumericUpDown numLength = new NumericUpDown
            {
                Location = new Point(130, 30),
                Size = new Size(100, 20),
                Minimum = 8,
                Maximum = 128,
                Value = 32
            };
            passwordGeneratorForm.Controls.Add(numLength);

            // Include uppercase checkbox
            CheckBox chkUppercase = new CheckBox
            {
                Location = new Point(20, 70),
                Size = new Size(200, 20),
                Text = "Include Uppercase Letters",
                Checked = true
            };
            passwordGeneratorForm.Controls.Add(chkUppercase);

            // Include lowercase checkbox
            CheckBox chkLowercase = new CheckBox
            {
                Location = new Point(20, 90),
                Size = new Size(200, 20),
                Text = "Include Lowercase Letters",
                Checked = true
            };
            passwordGeneratorForm.Controls.Add(chkLowercase);

            // Include numbers checkbox
            CheckBox chkNumbers = new CheckBox
            {
                Location = new Point(20, 110),
                Size = new Size(200, 20),
                Text = "Include Numbers",
                Checked = true
            };
            passwordGeneratorForm.Controls.Add(chkNumbers);

            // Include special characters checkbox
            CheckBox chkSpecial = new CheckBox
            {
                Location = new Point(20, 130),
                Size = new Size(200, 20),
                Text = "Include Special Characters",
                Checked = true
            };
            passwordGeneratorForm.Controls.Add(chkSpecial);

            // Generated password textbox
            TextBox txtGeneratedPassword = new TextBox
            {
                Location = new Point(20, 170),
                Size = new Size(350, 23),
                ReadOnly = true
            };
            passwordGeneratorForm.Controls.Add(txtGeneratedPassword);

            // Generate button
            Button btnGenerate = new Button
            {
                Location = new Point(20, 200),
                Size = new Size(100, 30),
                Text = "Generate"
            };
            btnGenerate.Click += (s, args) =>
            {
                int length = (int)numLength.Value;
                bool includeUppercase = chkUppercase.Checked;
                bool includeLowercase = chkLowercase.Checked;
                bool includeNumbers = chkNumbers.Checked;
                bool includeSpecial = chkSpecial.Checked;

                try
                {
                    string password = EncryptionService.GeneratePassword(length, includeUppercase, includeLowercase, includeNumbers, includeSpecial);
                    txtGeneratedPassword.Text = password;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            passwordGeneratorForm.Controls.Add(btnGenerate);

            // Copy to clipboard button
            Button btnCopy = new Button
            {
                Location = new Point(130, 200),
                Size = new Size(100, 30),
                Text = "Copy to Clipboard"
            };
            btnCopy.Click += (s, args) =>
            {
                if (!string.IsNullOrEmpty(txtGeneratedPassword.Text))
                {
                    Clipboard.SetText(txtGeneratedPassword.Text);
                    MessageBox.Show("Password copied to clipboard", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            passwordGeneratorForm.Controls.Add(btnCopy);

            // OK button
            Button btnOK = new Button
            {
                Location = new Point(240, 200),
                Size = new Size(100, 30),
                Text = "OK"
            };
            btnOK.Click += (s, args) =>
            {
                if (!string.IsNullOrEmpty(txtGeneratedPassword.Text))
                {
                    TextBox txtPassword = this.Controls.Find("txtPassword", true).FirstOrDefault() as TextBox;
                    if (txtPassword != null)
                    {
                        txtPassword.Text = txtGeneratedPassword.Text;
                    }
                }
                passwordGeneratorForm.Close();
            };
            passwordGeneratorForm.Controls.Add(btnOK);

            passwordGeneratorForm.ShowDialog();
        }

        /// <summary>
        /// Encrypt button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void BtnEncrypt_Click(object sender, EventArgs e)
        {
            TextBox txtInputFile = this.Controls.Find("txtInputFile", true).FirstOrDefault() as TextBox;
            TextBox txtOutputDir = this.Controls.Find("txtOutputDir", true).FirstOrDefault() as TextBox;
            TextBox txtPassword = this.Controls.Find("txtPassword", true).FirstOrDefault() as TextBox;
            ProgressBar progressBar = this.Controls.Find("progressBar", true).FirstOrDefault() as ProgressBar;

            if (txtInputFile == null || txtOutputDir == null || txtPassword == null || progressBar == null)
            {
                MessageBox.Show("UI elements not loaded properly", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string inputFile = txtInputFile.Text;
            string outputDir = txtOutputDir.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
            {
                MessageBox.Show("Please select a valid input file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Please select a valid output directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter a password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                UpdateStatus("Encrypting...", false);
                progressBar.Value = 0;

                string fileName = Path.GetFileNameWithoutExtension(inputFile);
                string outputFile = Path.Combine(outputDir, $"{fileName}.cls");

                await _encryptionService.EncryptFileAsync(inputFile, outputFile, password, (progress) =>
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

                UpdateStatus("Encryption completed successfully", true);
                MessageBox.Show($"File encrypted successfully: {outputFile}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus("Encryption failed", false);
                MessageBox.Show($"Error encrypting file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Decrypt button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void BtnDecrypt_Click(object sender, EventArgs e)
        {
            TextBox txtInputFile = this.Controls.Find("txtInputFile", true).FirstOrDefault() as TextBox;
            TextBox txtOutputDir = this.Controls.Find("txtOutputDir", true).FirstOrDefault() as TextBox;
            TextBox txtPassword = this.Controls.Find("txtPassword", true).FirstOrDefault() as TextBox;
            ProgressBar progressBar = this.Controls.Find("progressBar", true).FirstOrDefault() as ProgressBar;

            if (txtInputFile == null || txtOutputDir == null || txtPassword == null || progressBar == null)
            {
                MessageBox.Show("UI elements not loaded properly", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string inputFile = txtInputFile.Text;
            string outputDir = txtOutputDir.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
            {
                MessageBox.Show("Please select a valid input file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (Path.GetExtension(inputFile).ToLower() != ".cls")
            {
                MessageBox.Show("Only .cls files can be decrypted", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Please select a valid output directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter a password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                UpdateStatus("Decrypting...", false);
                progressBar.Value = 0;

                string fileName = Path.GetFileNameWithoutExtension(inputFile);
                string outputFile = Path.Combine(outputDir, fileName);

                string decryptedFilePath = await _encryptionService.DecryptFileAsync(inputFile, outputFile, password, (progress) =>
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

                UpdateStatus("Decryption completed successfully", true);
                MessageBox.Show($"File decrypted successfully: {decryptedFilePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus("Decryption failed", false);
                MessageBox.Show($"Error decrypting file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Batch operation button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnBatch_Click(object sender, EventArgs e)
        {
            // Open batch operation form
            BatchOperationForm batchForm = new BatchOperationForm();
            batchForm.ShowDialog();
        }

        /// <summary>
        /// Settings button click event handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BtnSettings_Click(object sender, EventArgs e)
        {
            // Create settings form
            Form settingsForm = new Form
            {
                Size = new Size(400, 200),
                Text = "Settings",
                StartPosition = FormStartPosition.CenterParent
            };

            // Encryption strength label
            Label lblStrength = new Label
            {
                Location = new Point(20, 30),
                Size = new Size(150, 20),
                Text = "Encryption Strength:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            settingsForm.Controls.Add(lblStrength);

            // Encryption strength combo box
            ComboBox cmbStrength = new ComboBox
            {
                Location = new Point(180, 30),
                Size = new Size(180, 23),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            cmbStrength.Items.AddRange(new string[] { "Fast (1 iteration)", "Medium (10 iterations)", "High (100 iterations)", "Ultra High (150 iterations)" });
            // Set current selection based on current strength
            switch (_encryptionService.CurrentStrength)
            {
                case EncryptionService.EncryptionStrength.Fast:
                    cmbStrength.SelectedIndex = 0;
                    break;
                case EncryptionService.EncryptionStrength.Medium:
                    cmbStrength.SelectedIndex = 1;
                    break;
                case EncryptionService.EncryptionStrength.High:
                    cmbStrength.SelectedIndex = 2;
                    break;
                case EncryptionService.EncryptionStrength.UltraHigh:
                    cmbStrength.SelectedIndex = 3;
                    break;
            }
            settingsForm.Controls.Add(cmbStrength);

            // OK button
            Button btnOK = new Button
            {
                Location = new Point(290, 120),
                Size = new Size(80, 30),
                Text = "OK",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, args) =>
            {
                // Update encryption strength based on selection
                switch (cmbStrength.SelectedIndex)
                {
                    case 0:
                        _encryptionService.CurrentStrength = EncryptionService.EncryptionStrength.Fast;
                        break;
                    case 1:
                        _encryptionService.CurrentStrength = EncryptionService.EncryptionStrength.Medium;
                        break;
                    case 2:
                        _encryptionService.CurrentStrength = EncryptionService.EncryptionStrength.High;
                        break;
                    case 3:
                        _encryptionService.CurrentStrength = EncryptionService.EncryptionStrength.UltraHigh;
                        break;
                }
                settingsForm.Close();
            };
            settingsForm.Controls.Add(btnOK);

            // Cancel button
            Button btnCancel = new Button
            {
                Location = new Point(200, 120),
                Size = new Size(80, 30),
                Text = "Cancel",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, args) => settingsForm.Close();
            settingsForm.Controls.Add(btnCancel);

            settingsForm.ShowDialog();
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

        /// <summary>
        /// Updates the status display
        /// </summary>
        /// <param name="message">Status message</param>
        /// <param name="isReady">Whether the application is ready</param>
        private void UpdateStatus(string message, bool isReady)
        {
            Label lblStatus = this.Controls.Find("lblStatus", true).FirstOrDefault() as Label;
            Label lblStatusText = this.Controls.Find("lblStatusText", true).FirstOrDefault() as Label;
            Panel statusIndicator = this.Controls.Find("statusIndicator", true).FirstOrDefault() as Panel;

            if (lblStatus != null)
            {
                lblStatus.Text = message;
            }

            if (lblStatusText != null)
            {
                lblStatusText.Text = $"Status: {message}";
            }

            if (statusIndicator != null)
            {
                statusIndicator.BackColor = isReady ? Color.Green : Color.Red;
            }
        }

        /// <summary>
        /// Drag enter event handler for drag and drop functionality
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void MainForm_DragEnter(object sender, DragEventArgs e)
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
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    // Get the first file (if multiple files are dropped)
                    string filePath = files[0];
                    
                    // Check if it's a file (not a directory)
                    if (File.Exists(filePath))
                    {
                        // Update the input file textbox
                        TextBox txtInputFile = this.Controls.Find("txtInputFile", true).FirstOrDefault() as TextBox;
                        if (txtInputFile != null)
                        {
                            txtInputFile.Text = filePath;
                            _inputFile = filePath;
                        }
                    }
                }
            }
        }
    }
}
