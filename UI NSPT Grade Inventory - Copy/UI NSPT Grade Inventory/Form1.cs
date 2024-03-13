using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace UI_NSPT_Grade_Inventory
{
    public partial class Form1 : Form
    {
        //Fields for UI
        private int borderSize = 2;
        private Size formSize; //Keep form size when it is minimized and restored.Since the form is resized because it takes into account the size of the title bar and borders.
        //-------------------------

        connect con = new connect();
        MySqlCommand cmd;
        MySqlDataReader reader; // read data from database in row
        MySqlDataAdapter adapter; // Use to populate DataSet or DataTable, we can update the DataTable using this class
        DataTable dt;
        bool btnCancelPres = false;

        //string[] selectedFilePaths;
        List<string> selectedFilePathForValidFormat = new List<string>();
        List<string> selectedFileWithExForValidFormat = new List<string>();

        string[] selectedFileNamesWithEX;
        int[] studIdInt;
        int numOfUploadFileCall = 0;

        private string tempFilePath;

        private int rowIndex = 0; // for dataGridView1_CellMouseUp

        //Constructor
        public Form1()
        {
            InitializeComponent();

            //For UI
            CollapseMenu();
            this.Padding = new Padding(borderSize); //Border size
            this.BackColor = Color.FromArgb(0, 191, 99); //Border color
            this.FormBorderStyle = FormBorderStyle.Sizable;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            formSize = this.ClientSize; //For UI
            //dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            //dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("tahoma", 10, FontStyle.Bold);
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            //dataGridView1.DefaultCellStyle.ForeColor = Color.White;
        }

        //-------------------------
        //For UI
        //Drag Form
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        private void panelTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        //Overridden methods
        protected override void WndProc(ref Message m)
        {
            const int WM_NCCALCSIZE = 0x0083;//Standar Title Bar - Snap Window
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020; //Minimize form (Before)
            const int SC_RESTORE = 0xF120; //Restore form (Before)
            const int WM_NCHITTEST = 0x0084;//Win32, Mouse Input Notification: Determine what part of the window corresponds to a point, allows to resize the form.
            const int resizeAreaSize = 10;
            #region Form Resize
            // Resize/WM_NCHITTEST values
            const int HTCLIENT = 1; //Represents the client area of the window
            const int HTLEFT = 10;  //Left border of a window, allows resize horizontally to the left
            const int HTRIGHT = 11; //Right border of a window, allows resize horizontally to the right
            const int HTTOP = 12;   //Upper-horizontal border of a window, allows resize vertically up
            const int HTTOPLEFT = 13;//Upper-left corner of a window border, allows resize diagonally to the left
            const int HTTOPRIGHT = 14;//Upper-right corner of a window border, allows resize diagonally to the right
            const int HTBOTTOM = 15; //Lower-horizontal border of a window, allows resize vertically down
            const int HTBOTTOMLEFT = 16;//Lower-left corner of a window border, allows resize diagonally to the left
            const int HTBOTTOMRIGHT = 17;//Lower-right corner of a window border, allows resize diagonally to the right
            ///<Doc> More Information: https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-nchittest </Doc>
            if (m.Msg == WM_NCHITTEST)
            { //If the windows m is WM_NCHITTEST
                base.WndProc(ref m);
                if (this.WindowState == FormWindowState.Normal)//Resize the form if it is in normal state
                {
                    if ((int)m.Result == HTCLIENT)//If the result of the m (mouse pointer) is in the client area of the window
                    {
                        Point screenPoint = new Point(m.LParam.ToInt32()); //Gets screen point coordinates(X and Y coordinate of the pointer)                           
                        Point clientPoint = this.PointToClient(screenPoint); //Computes the location of the screen point into client coordinates                          
                        if (clientPoint.Y <= resizeAreaSize)//If the pointer is at the top of the form (within the resize area- X coordinate)
                        {
                            if (clientPoint.X <= resizeAreaSize) //If the pointer is at the coordinate X=0 or less than the resizing area(X=10) in 
                                m.Result = (IntPtr)HTTOPLEFT; //Resize diagonally to the left
                            else if (clientPoint.X < (this.Size.Width - resizeAreaSize))//If the pointer is at the coordinate X=11 or less than the width of the form(X=Form.Width-resizeArea)
                                m.Result = (IntPtr)HTTOP; //Resize vertically up
                            else //Resize diagonally to the right
                                m.Result = (IntPtr)HTTOPRIGHT;
                        }
                        else if (clientPoint.Y <= (this.Size.Height - resizeAreaSize)) //If the pointer is inside the form at the Y coordinate(discounting the resize area size)
                        {
                            if (clientPoint.X <= resizeAreaSize)//Resize horizontally to the left
                                m.Result = (IntPtr)HTLEFT;
                            else if (clientPoint.X > (this.Width - resizeAreaSize))//Resize horizontally to the right
                                m.Result = (IntPtr)HTRIGHT;
                        }
                        else
                        {
                            if (clientPoint.X <= resizeAreaSize)//Resize diagonally to the left
                                m.Result = (IntPtr)HTBOTTOMLEFT;
                            else if (clientPoint.X < (this.Size.Width - resizeAreaSize)) //Resize vertically down
                                m.Result = (IntPtr)HTBOTTOM;
                            else //Resize diagonally to the right
                                m.Result = (IntPtr)HTBOTTOMRIGHT;
                        }
                    }
                }
                return;
            }
            #endregion
            //Remove border and keep snap window
            if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
            {
                return;
            }
            //Keep form size when it is minimized and restored. Since the form is resized because it takes into account the size of the title bar and borders.
            if (m.Msg == WM_SYSCOMMAND)
            {
                /// <see cref="https://docs.microsoft.com/en-us/windows/win32/menurc/wm-syscommand"/>
                /// Quote:
                /// In WM_SYSCOMMAND messages, the four low - order bits of the wParam parameter 
                /// are used internally by the system.To obtain the correct result when testing 
                /// the value of wParam, an application must combine the value 0xFFF0 with the 
                /// wParam value by using the bitwise AND operator.
                int wParam = (m.WParam.ToInt32() & 0xFFF0);
                if (wParam == SC_MINIMIZE)  //Before
                    formSize = this.ClientSize;
                if (wParam == SC_RESTORE)// Restored form(Before)
                    this.Size = formSize;
            }
            base.WndProc(ref m);
        }

        //Event methods
        private void Form1_Resize(object sender, EventArgs e)
        {
            AdjustForm();
        }

        //Private methods
        private void AdjustForm()
        {
            switch (this.WindowState)
            {
                case FormWindowState.Maximized:
                    this.Padding = new Padding(8,8,8,0);
                    break;
                case FormWindowState.Normal:
                    if(this.Padding.Top != borderSize)
                        this.Padding = new Padding(borderSize);
                    break;

            }
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            formSize = this.ClientSize;
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                formSize = this.ClientSize;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.Size = formSize;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnMenu_Click(object sender, EventArgs e)
        {
            CollapseMenu();
        }

        private void CollapseMenu()
        {
            if(this.panelMenu.Width > 200) //Collapse Menu
            {
                panelMenu.Width = 100;
                pictureBox1.Visible = false;
                btnMenu.Dock = DockStyle.Top;
                foreach (Button menuButton in panelMenu.Controls.OfType<Button>())
                {
                    menuButton.Text = "";
                    menuButton.ImageAlign = ContentAlignment.MiddleCenter;
                    menuButton.Padding = new Padding(0);
                }
            }
            else //Expand menu
            { 
                panelMenu.Width = 230;
                pictureBox1.Visible = true;
                btnMenu.Dock = DockStyle.None;
                foreach (Button menuButton in panelMenu.Controls.OfType<Button>())
                {
                    menuButton.Text ="   "+ menuButton.Tag.ToString();
                    menuButton.ImageAlign = ContentAlignment.MiddleLeft;
                    menuButton.Padding = new Padding(10,0,0,0);
                }
            }
        }
        //-------------------------

        // Placeholder text for textbox search
        private void txtStudId_Enter(object sender, EventArgs e)
        {
            if (txtStudId.Text == "Search file here .....")
            {
                txtStudId.Text = "";

                txtStudId.ForeColor = Color.Black;
            }
        }

        // Placeholder text for textbox search
        private void txtStudId_Leave(object sender, EventArgs e)
        {
            if (txtStudId.Text == "")
            {
                txtStudId.Text = "Search file here .....";

                txtStudId.ForeColor = Color.DarkGray;
            }
        }

        //-------------------------
        // btn upload sample run
        //private void btnUpload_Click(object sender, EventArgs e)
        //{

        //}

        //private void timer1_Tick(object sender, EventArgs e)
        //{
        //    if (progressBarClass1.Value < progressBarClass1.Maximum)
        //    {
        //        progressBarClass1.Value++;
        //    }
        //}

        //-------------------------




        public int UploadFile(string filePath, int studId, int indexOfSelectedFileName, string fileNamesWithEX)
        {
            // try catch if database connection failed and database or table don't exist
            try
            {
                numOfUploadFileCall++;
                con.connection(); // open connection
                using (FileStream fstream = File.OpenRead(filePath))
                {
                    byte[] contents = new byte[fstream.Length];
                    fstream.Read(contents, 0, (int)fstream.Length);

                    using (cmd = new MySqlCommand("insert into student_grade(stud_id,pdf_name,pdf_file) values(@stud_id,@pdf_name,@pdf_file)", con.con))
                    {
                        cmd.Parameters.AddWithValue("@stud_id", studId);
                        cmd.Parameters.AddWithValue("@pdf_name", fileNamesWithEX);
                        cmd.Parameters.AddWithValue("@pdf_file", contents);
                        try
                        {
                            return cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            //if student ID pdf name already exist, Change 'Uploading..' to 'Failed'
                            for (int j = 0; j < dataGridView1.Rows.Count; j++)
                            {
                                if (dataGridView1.Rows[j].Cells[0].Value == selectedFileNamesWithEX[indexOfSelectedFileName]) // selectedFileNamesWithEX[i] i is the index of file being uploaded
                                {
                                    dataGridView1.Rows[j].Cells[1].Value = "Failed";
                                }
                            }

                            string errorMessage = ex.Message;

                            // Translate specific exception messages
                            if (errorMessage.Contains("Data too long for column 'pdf_file'"))
                            {
                                errorMessage = $"{fileNamesWithEX} too large! PDF size must be less than or equal to 16 mb only.";
                            }
                            if (errorMessage.Contains("Duplicate entry"))
                            {
                                errorMessage = $"Duplicate entry {fileNamesWithEX}";
                            }
                            if (errorMessage.Contains("Connection must be valid and open."))
                            {
                                errorMessage = "Database connection must be valid and open.";
                            }

                            // Display the translated message                            
                            DialogResult dialog = MessageBox.Show(errorMessage, "PDF INVENTORY TRIAL 2", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            // if last upload is failed for key duplicate, and ok is press, make btnCancel invisible and make btnUpload visible, and turn progress bar to 0%.                            
                            if (dialog == DialogResult.OK && numOfUploadFileCall == dataGridView1.Rows.Count)
                            {
                                Invoke(new Action(() =>
                                {
                                    btnCancel.Visible = false;
                                    btnUpload.Visible = true;

                                    // if last upload is failed and ok is press, make btnSearch and txtbox available
                                    btnSearch.Enabled = true;
                                    txtStudId.Enabled = true;
                                }));
                                backgroundWorker1.ReportProgress(0);
                            }

                        }

                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 0;
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            deletePDFToolStripMenuItem.Visible = false; // if bnt upload is press make contentMenuToolStrip for delete invisible

            dispose(); // delete file path and file
            axAcroPDF1.LoadFile("Empty"); // Display nothing on pdf viewer            

            // when uploading make btnSearch and textbox not available
            btnSearch.Enabled = false;
            txtStudId.Enabled = false;

            numOfUploadFileCall = 0;
            selectedFilePathForValidFormat.Clear(); // reset the list value

            dt = new DataTable();

            // This creates an instance of OpenFileDialog class and sets its properties. It filters files to only show PDF files and validates the file names.
            using (OpenFileDialog dlg = new OpenFileDialog() { Filter = "Text Documents (*.pdf) |*.pdf", ValidateNames = true })
            {
                // Enable multiple file selection
                dlg.Multiselect = true;

                if (dlg.ShowDialog() == DialogResult.OK) // This displays the file dialog and checks if the user has selected a file and clicked "OK"
                {
                    string[] selectedFiles = dlg.FileNames; // put selected file path to selectedFiles

                    // check if file name format is valid 
                    for (int j = 0; j < selectedFiles.Length; j++)
                    {
                        // Check if pdf name follow: 2 numbers before dash and after dash up to 8 length of numbers (e.g., 19-12345.pdf, 19-12345678.pdf)
                        if (System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(selectedFiles[j]), @"^\d{2}-\d{1,8}$"))
                        {
                            selectedFilePathForValidFormat.Add(selectedFiles[j]);
                            selectedFileWithExForValidFormat.Add(Path.GetFileName(selectedFiles[j]));
                        }
                        // else display error message for invalid file format
                        else
                        {
                            MessageBox.Show($"Invalid file name:\n{Path.GetFileNameWithoutExtension(selectedFiles[j])}. Please select files with valid name: 2 numbers before dash and after dash up to 8 numbers (e.g., 19-12345.pdf, 19-12345678.pdf).", "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    // check if there is valid file to upload 
                    if (selectedFilePathForValidFormat.Count > 0)
                    {
                        // Create a DataTable and fill data gridview with header
                        //dt = new DataTable();
                        dt.Columns.Add("PDF NAME", typeof(string));
                        dt.Columns.Add("UPLOAD STATUS", typeof(string));

                        dataGridView1.DataSource = dt; // Bind datable to DataGridView

                        // Initialize dialog box
                        DialogResult dialog = new DialogResult();

                        int len = selectedFilePathForValidFormat.Count; // Number of selected valid pdf 

                        // Convert list elements from selectedFileNamesWithEXForDisplay list to a string
                        string listContent = string.Join(Environment.NewLine, selectedFileWithExForValidFormat);

                        // Display valid file to upload in MessageBox
                        if (len == 1) // if single file selected
                        {
                            // This displays a message box asking the user to confirm if they want to upload the selected file.
                            dialog = MessageBox.Show($"Are you sure you want to upload this file!?\n\n{listContent}", "PDF INVENTORY TRIAL 1", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            selectedFileWithExForValidFormat.Clear(); // empty selectedFileNamesWithEXForDisplay list enable to not display previous transac

                        }
                        if (len > 1) // if multiple file selected 
                        {
                            dialog = MessageBox.Show($"Are you sure you want to upload this files!?\n\n{listContent}", "PDF INVENTORY TRIAL 1", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            selectedFileWithExForValidFormat.Clear();
                        }

                        if (dialog == DialogResult.Yes) // If the user confirms by clicking "Yes" in the message box, it proceeds to upload the file.
                        {
                            // Extract just the filenames without paths
                            selectedFileNamesWithEX = new string[len];
                            string[] selectedFileNamesNoEx = new string[len];
                            string[] cleanStudId = new string[len];
                            studIdInt = new int[len];

                            for (int i = 0; i < len; i++)
                            {
                                selectedFileNamesWithEX[i] = Path.GetFileName(selectedFilePathForValidFormat[i]);
                                selectedFileNamesNoEx[i] = Path.GetFileNameWithoutExtension(selectedFilePathForValidFormat[i]);
                                cleanStudId[i] = selectedFileNamesNoEx[i].Replace("-", "");
                                studIdInt[i] = int.Parse(cleanStudId[i]);
                            }

                            // Populate DataGridView with the file selected
                            for (int i = 0; i < len; i++)
                            {
                                dt.Rows.Add(selectedFileNamesWithEX[i], "Uploading...");
                            }
                            dataGridView1.DataSource = dt; // Display to DataGridView

                            // Call BackGroundWorker for uploading pdf to database
                            if (!backgroundWorker1.IsBusy)
                            {
                                backgroundWorker1.RunWorkerAsync(); // start execution of background operation
                            }
                        }
                        else
                        {
                            dataGridView1.DataSource = null; // Display nothing when no upload happen
                            btnSearch.Enabled = true; // make btnSearch and textbox available if no upload happen
                            txtStudId.Enabled = true;
                            return; // If the user chooses "No" in the confirmation message box or cancels the file dialog, the control returns without uploading the file.
                        }

                    }

                }
                else
                {
                    btnSearch.Enabled = true; // make btnSearch and textbox available if no upload happen
                    txtStudId.Enabled = true;
                    dataGridView1.DataSource = dt; // Bind datable to DataGridView
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int len = dataGridView1.Rows.Count;
            int progressBarValue = 0;

            // When uploading make btn Upload invisible and btn Cancel visible.
            Invoke(new Action(() =>
            {
                btnCancel.Visible = true;
                btnUpload.Visible = false;
            }));

            for (int i = 0; i < len; i++)
            {
                // return 1 if individual row successfully uploaded to database, row affected.

                if (UploadFile(selectedFilePathForValidFormat[i], studIdInt[i], i, selectedFileNamesWithEX[i]) == 1)
                {
                    progressBarValue += 100 / len;
                    backgroundWorker1.ReportProgress(progressBarValue);

                    if (i == len - 1 && progressBarValue % 100 != 0) // if len is not divisble by 100
                    {
                        progressBarValue += 100 - progressBarValue;
                        backgroundWorker1.ReportProgress(progressBarValue);
                    }

                    //if the return value of UploadFile() func is 1, change 'Uploading..' to 'Done'.
                    for (int j = 0; j < len; j++)
                    {
                        if (dataGridView1.Rows[j].Cells[0].Value == selectedFileNamesWithEX[i]) // selectedFileNamesWithEX[i] i is the index of file being uploaded
                        {
                            dataGridView1.Rows[j].Cells[1].Value = "Done";
                        }
                    }

                }


                // if button cancel is press, cancel upload and return progress bar to zero.
                if (backgroundWorker1.CancellationPending && btnCancelPres == true)
                {
                    DialogResult dialog = new DialogResult();
                    if (len == 1)
                    {
                        Thread.Sleep(300);
                        dialog = MessageBox.Show("Are you sure you want to cancel uploading this file!?", "PDF INVENTORY TRIAL 1", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    }
                    if (len > 1)
                    {
                        Thread.Sleep(300);
                        dialog = MessageBox.Show("Are you sure you want to cancel uploading this files!?", "PDF INVENTORY TRIAL 1", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    }

                    if (dialog == DialogResult.Yes)
                    {
                        // If button cancel is press turn 'Uploading...' to 'Canceled'
                        for (int j = 0; j < len; j++)
                        {
                            if (dataGridView1.Rows[j].Cells[1].Value == "Uploading...") // selectedFileNamesWithEX[i] i is the index of file being uploaded
                            {
                                dataGridView1.Rows[j].Cells[1].Value = "Canceled";
                            }
                        }
                        e.Cancel = true; // cancel backgroundWorker1.RunWorkerAsync();
                        backgroundWorker1.ReportProgress(0); // when upload is canceled make progress bar zero

                        // when upload is canceled make btn Upload visible
                        Invoke(new Action(() =>
                        {
                            btnCancel.Visible = false;
                            btnUpload.Visible = true;
                        }));

                        return;
                    }

                    if (dialog == DialogResult.No)
                    {
                        btnCancelPres = false;
                    }

                }


                if (progressBarValue == 100)
                {
                    if (len == 1)
                    {
                        MessageBox.Show("FINISHED UPLOADING FILE", "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    if (len > 1)
                    {
                        MessageBox.Show("FINISHED UPLOADING FILES", "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    backgroundWorker1.ReportProgress(0); // zero the progress bar                                      

                    // When progress bar is 100% make btn Cancel invisible and btn Upload visible.
                    Invoke(new Action(() =>
                    {
                        btnCancel.Visible = false;
                        btnUpload.Visible = true;

                        btnSearch.Enabled = true;  // make btnSearch and textbox available if upload is 100%
                        txtStudId.Enabled = true;
                    }));
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            btnCancelPres = true;
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("UPLOAD CANCELED", "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnSearch.Enabled = true;  // make btnSearch and textbox available if upload was canceled
                txtStudId.Enabled = true;
            }
            else if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSearch.Enabled = true; // make btnSearch and textbox error occur on background worker
                txtStudId.Enabled = true;
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (txtStudId.Text.Length >= 3 && txtStudId.Text != "Search file here .....") // whem btn is press instead of enter in keyboard
            {
                con.connection(); // Open connection to db
                dispose(); // delete previous search file path and file for display

                try
                {
                    dataGridView1.DataSource = null;

                    // It executes the SQL query provided, against the MySQL database connection (con.con), fetching data from the student_grade table.
                    // It also assigns column aliases to the selected columns, which will be used as column headers in the DataGridView.
                    using (adapter = new MySqlDataAdapter("SELECT stud_id AS 'STUDENT NUMBER', pdf_name AS 'PDF FILE' FROM student_grade WHERE stud_id =  @stud_id", con.con))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@stud_id", txtStudId.Text);
                        dt = new DataTable();

                        adapter.Fill(dt); // This executes the SQL query using the adapter and fills the DataTable dt with the retrieved data from the database. 

                        if (dt.Rows.Count > 0)
                        {
                            dataGridView1.DataSource = dt;


                            using (cmd = new MySqlCommand("SELECT pdf_name, pdf_file FROM student_grade WHERE stud_id = @stud_id", con.con))
                            {
                                cmd.Parameters.AddWithValue("@stud_id", txtStudId.Text);

                                using (reader = cmd.ExecuteReader(CommandBehavior.Default)) // This executes the SQL query and creates a MySqlDataReader object reader to read the results.
                                {
                                    if (reader.Read())
                                    {
                                        try
                                        {
                                            string pdfName = (string)reader.GetValue(0); // This retrieves the PDF data from the first column of the first row returned by the query and stores it in a byte array fileData.
                                            byte[] fileData = (byte[])reader.GetValue(1); // This retrieves the PDF data from the 2nd column of the first row returned by the query and stores it in a byte array fileData.

                                            tempFilePath = Path.Combine(Path.GetTempPath(), pdfName);

                                            using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite))
                                            {
                                                using (BinaryWriter bw = new BinaryWriter(fs)) // This creates a BinaryWriter to write binary data to the FileStream.
                                                {
                                                    bw.Write(fileData); // This writes the PDF data to the file.
                                                    bw.Close();
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show(ex.Message, "PDF INVENTORY TRIAL 4.3", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }

                                        try
                                        {
                                            axAcroPDF1.src = tempFilePath;
                                            //Path of temporary file for search result
                                            //MessageBox.Show(tempFilePath, "PDF INVENTORY TRIAL 4.2", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                                            deletePDFToolStripMenuItem.Visible = true; // if student found make contentMenuToolStrip visible for delete pdf
                                            deletePDFToolStripMenuItem.Enabled = true; // if student found enable delete pdf
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show(ex.Message, "PDF INVENTORY TRIAL 4.1", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }

                                    }
                                    reader.Close(); // This closes the data reader.
                                }
                            }

                        }
                        else
                        {
                            axAcroPDF1.LoadFile("Empty"); // Display nothing on pdf viewer
                            MessageBox.Show("No data found for the provided student ID number " + txtStudId.Text, "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }

                    }

                }
                catch (Exception ex)
                {
                    string errorMessage = ex.Message;

                    // Translate specific exception messages
                    if (errorMessage.Contains("Connection must be valid and open."))
                    {
                        errorMessage = "Database connection must be valid and open.";
                        MessageBox.Show(errorMessage, "PDF INVENTORY TRIAL 4", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show(ex.Message, "PDF INVENTORY TRIAL 4", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
            }

            else
            {
                MessageBox.Show("Maximum input 3 digit number.", "PDF INVENTORY TRIAL 1.2", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void dispose()
        {
            // Dispose AxAcroPDF control
            //axAcroPDF1.Dispose();

            // Delete temporary file
            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    //MessageBox to show the file path of search result has been deleted
                    //MessageBox.Show("Temp FilePath Successfully deleted!", "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    // Handle any exceptions related to file deletion                    
                    MessageBox.Show(ex.Message, "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Ignore the key press that are not neccessary for search
        private void txtStudId_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow digits, Backspace key, Clear key (Delete), Ctrl+A (select all), Ctrl+C (copy), Ctrl+X (cut), Ctrl+V (paste), and the Enter key (ASCII value 13)                        
            if (!(char.IsDigit(e.KeyChar) ||
                  (e.KeyChar == (char)Keys.Enter) ||
                  (e.KeyChar == (char)1 /* Ctrl+A */) ||
                  (e.KeyChar == (char)3 /* Ctrl+C */) ||
                  (e.KeyChar == (char)22 /* Ctrl+V */) ||
                  (e.KeyChar == (char)24 /* Ctrl+X */) ||
                  (e.KeyChar == (char)Keys.Delete) /* Clear key */ ||
                  (e.KeyChar == (char)Keys.Back) /* Backspace key */
                 ))
            {
                e.Handled = true; // Ignore the key press
            }

            // Limit to 10 characters
            if (txtStudId.Text.Length >= 10 && !((e.KeyChar == (char)Keys.Enter) ||
                  (e.KeyChar == (char)1 /* Ctrl+A */) ||
                  (e.KeyChar == (char)3 /* Ctrl+C */) ||
                  (e.KeyChar == (char)22 /* Ctrl+V */) ||
                  (e.KeyChar == (char)24 /* Ctrl+X */) ||
                  (e.KeyChar == (char)Keys.Delete) /* Clear key */ ||
                  (e.KeyChar == (char)Keys.Back) /* Backspace key */
                 )) // Allow Backspace
            {
                e.Handled = true; // Ignore the key press
            }
        }

        // if enter is pressed
        private void txtStudId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && txtStudId.Text.Length >= 3 && txtStudId.Text != "Search file here .....")
            {
                btnSearch.PerformClick();
            }
            else if (e.KeyCode == Keys.Enter && txtStudId.Text.Length < 3 || txtStudId.Text == "Search file here .....")
            {
                MessageBox.Show("Maximum input 3 digit number.", "PDF INVENTORY TRIAL 1.1", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            dispose();
        }

        // event handler for delete student pdf grade, visible only when stud id num search was successful
        private void deletePDFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.dataGridView1.Rows[this.rowIndex].IsNewRow && this.rowIndex == 0)
            {
                this.dataGridView1.Rows.RemoveAt(this.rowIndex);

                try
                {
                    con.connection();
                    using (cmd = new MySqlCommand("Delete From student_grade where stud_id = @stud_id", con.con)) ;
                    {
                        cmd.Parameters.AddWithValue("@stud_id", txtStudId.Text);

                        // if cmd.ExecuteNonQuery() return is 1 meaning there is row affected/deleted
                        if (cmd.ExecuteNonQuery() == 1)
                        {
                            dispose(); // delete file path and file of current search
                            axAcroPDF1.LoadFile("Empty"); // Display nothing on pdf viewer  

                            MessageBox.Show("Successfully Deleted", "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            deletePDFToolStripMenuItem.Visible = false; // if bnt upload is press make contentMenuToolStrip for delete invisible
                        }
                        else
                        {
                            MessageBox.Show("Failed to Delete!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }

        // private int rowIndex = 0; // for dataGridView1_CellMouseUp
        // event handler for right click, visible only when stud id num search was successful
        private void dataGridView1_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex == 0)
            {
                this.dataGridView1.Rows[e.RowIndex].Selected = true;
                this.rowIndex = e.RowIndex;
                this.dataGridView1.CurrentCell = this.dataGridView1.Rows[e.RowIndex].Cells[1];
                this.contextMenuStrip1.Show(this.dataGridView1, e.Location);
                contextMenuStrip1.Show(Cursor.Position);
            }
        }

        //-------------------------
    }
}
