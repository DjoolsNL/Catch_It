using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Catch_It
{
    public partial class Form1 : Form
    {
        #region Fields and properties
        /// <summary>
        /// Timer1 event fires elke seconde en checkt of 'clipboardText' afwijkt van 'lastEntry'. 
        /// Wijkt hij af dan wordt clipboardText toegevoegd aan Record. Bool 'clipboardGewijzigd' 
        /// vergelijkt beide.
        /// Beide strings starten met de waarde "a" zodat het programma bij start niet meteen de
        /// bestaande inhoud vh clipb opslaat.
        /// </summary>
        private Timer Timer1;
        private int widthForm = 1165;
        private int heightForm = 728;
        private string clipboardText = "a";
        private string lastEntry = "a";
        private bool clipboardGewijzigd
        {
            get
            {
                if (!Clipboard.ContainsText())
                    return false;

                clipboardText = Clipboard.GetText();

                return !string.IsNullOrWhiteSpace(clipboardText)
                    && clipboardText != lastEntry;
            }
        }
        private List<Record> Records = [];
        private Dictionary<string, string> styleDictionary = [];
        private static BindingSource Source;
        /// <summary>
        /// Root including all records and entries. Used for reading and writing XML file.
        /// </summary>
        private XElement xmlRoot;
        private bool negeerSelectedItem = false;
        string file = "";
        int teller;
        string name;
        #endregion

        public Form1()
        {
            InitializeComponent();
            Timer1 = new System.Windows.Forms.Timer();
            Timer1.Enabled = false;
            Timer1.Tick += new System.EventHandler(Timer1_Tick);
            Timer1.Interval = 1000;
            
            Source = new BindingSource();
            dataGridView1.DataSource = Source;
            menuStripTop.Cursor = System.Windows.Forms.Cursors.Arrow;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            toolStripRecords.Cursor = Cursors.Default;
            toolStripBrowser.Cursor = Cursors.Default;
            toolStripTextEditor.Cursor = Cursors.Default;
            richTextBox1.SelectionAlignment = HorizontalAlignment.Left;
            richTextBox1.SelectionIndent = 20;
            this.Text = "Catch / © 2022 by Djools";
            splitContainer2.IsSplitterFixed = false;
            dataGridView1.Cursor = System.Windows.Forms.Cursors.Default;
        }

        #region form1 methods (read/write XML, load/save file, resize)
        private void Form1_Load(object sender, EventArgs e)
        {
            LeesXMLFile();
            if (menuStripTop_ComboBoxSelectView.Items.Count != 0)
            {
                // deze setting triggered de menucomboBox1_SelectedIndexChanged_1 event en die zorgt ook voor de layout.
                menuStripTop_ComboBoxSelectView.SelectedItem = menuStripTop_ComboBoxSelectView.Items[0];
            }
            // update statusStrip1_LabelCurrentClipboard met de inhoud van het clipboard bij opstarten van de app.
            if (clipboardGewijzigd)
            {
                int lengte = clipboardText.Length;
                string weergaveStatusStrip = Regex.Replace(clipboardText, @"\r\n?|\n", " ");
                if (lengte < 25)
                {

                    statusStrip1_LabelCurrentClipboard.Text = weergaveStatusStrip.Trim();
                }
                else
                {
                    statusStrip1_LabelCurrentClipboard.Text = weergaveStatusStrip[..25].Trim() + "...";
                }
            }

            // zo kun je in een keer de kleur van alle items aanpassen.
            //foreach (ToolStripItem item in menuStripTop.Items)
            //{
            //    item.ForeColor = Color.NavajoWhite;
            //}

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SchrijfML();
        }
        /// <summary>
        /// Loads XML data from a file, creates records, populates UI elements, and initializes data bindings.
        /// </summary>
        private void LeesXMLFile()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            String path = Directory.GetCurrentDirectory();
            xmlRoot = XElement.Load(path + "\\Catch.xml");

            foreach (var xmlRecord in xmlRoot.Elements())
            {
                Record record = new Record();
                record.Name = (string)xmlRecord.FirstAttribute;
                menuStripTop_ComboBoxSelectRecord.Items.Add(record.Name);
                Records.Add(record);

                // Add a ToolStripMenuItem for deleting the record to the tsmiDeleteRecord dropdown
                ToolStripMenuItem tsmiDelete = new ToolStripMenuItem(record.Name);
                menuStripTop_MenuItemDeleteRecord.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiDelete });
                // Add the click event handler for the delete menu item
                tsmiDelete.Click += new System.EventHandler(menuStripTop_DeleteRecord_Click);

                // Add the entries to the record's BindingList and populate the styleDictionary
                foreach (var xElement in xmlRecord.Elements())
                {
                    // Get the style attribute value
                    string style = (string)xElement.FirstAttribute;
                    Veld veld = new Veld();
                    veld.Entry = xElement.Value;

                    if (!styleDictionary.ContainsKey(record.Name + veld.Entry))
                    {
                        styleDictionary.Add(record.Name + veld.Entry, style);
                    }
                    record.Entries.Add(veld);
                }
            }

            Record recordDisplayed = new Record();
            recordDisplayed.Entries = Records.FirstOrDefault().Entries;

            Source = new BindingSource(recordDisplayed.Entries, null);

            if (menuStripTop_ComboBoxSelectRecord.Items.Count != 0)
            {
                // deze setting triggered de menucomboBox1_SelectedIndexChanged_1 event en die zorgt ook voor de layout.
                menuStripTop_ComboBoxSelectRecord.SelectedItem = menuStripTop_ComboBoxSelectRecord.Items[0];
            }
        }
        private void SchrijfML()
        {
            IEnumerable<XElement> LoopFranz()
            {
                foreach (var record in Records)
                {
                    XElement xRecord = new XElement("record");
                    XAttribute xName = new XAttribute("name", record.Name);
                    xRecord.Add(xName);

                    foreach (var veld in record.Entries)
                    {
                        XElement xEntry = new XElement("entry", veld.Entry);

                        //FormatKeyStyleDictionary(veld.Entry);

                        string s = styleDictionary[record.Name + veld.Entry];
                        XAttribute style = new XAttribute("style", s);
                        xEntry.Add(style);
                        xRecord.Add(xEntry);
                    }
                    yield return xRecord;
                }
            }
            XElement doc = new XElement("root", LoopFranz());

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string path = Directory.GetCurrentDirectory();
            doc.Save(path + "\\Catch.xml");
        }
        #endregion

        #region GUI methods
        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
            this.SuspendLayout();
            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
        }
        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                textBoxAddRow.Text = "been there";

            }

            this.ResumeLayout();

            splitContainer2.FixedPanel = FixedPanel.None;
        }
        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }
        #endregion

        #region timer1 methods
        private void Timer1_Tick(object sender, EventArgs e)
        {
            teller++;
            if (clipboardGewijzigd)
            {
                // in juiste record opslaan
                string recordName = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
                Record record = Records.FirstOrDefault(r => r.Name == recordName);
                Veld veld = new();
                veld.Entry = clipboardText;
                record.Entries.Add(veld);

                // toevoeging aan StyleDictionary voor de opmaak van het record en de entries in de ui
                //FormatKeyStyleDictionary(veld.Entry);

                if (!styleDictionary.ContainsKey(record.Name + veld.Entry))
                {
                    styleDictionary.Add(record.Name + veld.Entry, "Regular");
                }

                // de BindingSource zorgt ervoor dat alle opmaak bij elke wijziging wegvalt en 
                // onderstaande void brengt die weer terug
                dataGridView1_PasLayoutToe();

                // zorgt ervoor dat niets meer wordt toegevoegd tenzij clipboardText wijzigt.
                lastEntry = clipboardText;
                // update statusStrip
                int lengte = clipboardText.Length;
                string weergaveStatusStrip = Regex.Replace(clipboardText, @"\r\n?|\n", " ");
                if (lengte < 25)
                {

                    statusStrip1_LabelCurrentClipboard.Text = weergaveStatusStrip.Trim();
                }
                else
                {
                    statusStrip1_LabelCurrentClipboard.Text = weergaveStatusStrip[..25].Trim() + "...";
                }
            }
        }
        #endregion

        #region menuStripTop methods
        /// <summary>
        /// Turns the clipboard catching on or off based on the current status, updating the timer, form title, menu item text, and status strip label accordingly.
        /// </summary>
        private void menuStripTop_MenuItemStartRecording_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem status = sender as ToolStripMenuItem;

            if (status.Text == "Stop Catching Clipboard")
            {
                Timer1.Stop();
                Timer1.Enabled = false;
                this.Text = "Catch / Status: off / © 2022 by Djools";
                status.Text = "Start Catching Clipboard";
                statusStrip1_LabelCurrentOnOff.Text = "off";
            }
            else
            {
                Timer1.Enabled = true;
                Timer1.Start();
                this.Text = "Catch / Status: on / © 2022 by Djools";
                status.Text = "Stop Catching Clipboard";
                statusStrip1_LabelCurrentOnOff.Text = "on";
            }
        }
        private void menuStripTop_ComboBoxSelectRecord_Click(object sender, EventArgs e)
        {
            negeerSelectedItem = false;
        }
        private void menuStripTop_ComboBoxSelectRecord_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!negeerSelectedItem)
            {
                string recordName = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
                Record record = Records.FirstOrDefault(r => r.Name == recordName);

                dataGridView1.DefaultCellStyle.Font = new System.Drawing.Font("Consolas", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);

                Source = new BindingSource(record.Entries, null);
                dataGridView1.DataSource = Source;
            }

            negeerSelectedItem = false;
            // een hele gemene plek om PasLayoutToe bij opstart te verstoppen!
            dataGridView1_PasLayoutToe();
        }
        // event also fires for menuStripTop_MenuItemTools
        private void menuStripTop_MenuItemManageRecords_DropDownClosed(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
            tsmi.ForeColor = Color.NavajoWhite;
        }
        // event also fires for menuStripTop_MenuItemTools
        private void menuStripTop_MenuItemManageRecords_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
            tsmi.ForeColor = Color.Black;
        }
        private void menuStripTop_TextBoxNewRecord_KeyDown(object sender, KeyEventArgs e)
        {
            ToolStripTextBox textBox = sender as ToolStripTextBox;

            if (e.KeyCode == Keys.Enter)
            {
                if (textBox.TextLength > 0)
                {
                    // naam mag niet te lang zijn 
                    string newName = textBox.Text;
                    if (newName.Length > 12)
                    {
                        newName = newName[..12];
                    }

                    // en geen whitespace bevatten
                    while (newName.Contains(' '))
                    {
                        newName = newName.Replace(" ", "");
                    }

                    // en de eerste letter wordt een hoofdletter
                    string last = newName[1..];
                    string first = newName[..1].ToUpper();
                    newName = first + last;

                    // We maken een nieuw Record aan en voegen dat toe aan Franz
                    // Aan het nieuwe Record wordt alvast 1 veld toegevoegd
                    Record record = new Record();
                    Records.Add(record);
                    record.Name = newName;
                    Veld veld = new Veld();
                    veld.Entry = "Cought";
                    record.Entries.Add(veld);

                    // nieuwe veld in Record wordt ook in StyleDictionary opgenomen
                    //FormatKeyStyleDictionary(veld.Entry);

                    if (!styleDictionary.ContainsKey(record.Name + veld.Entry))
                    {
                        styleDictionary.Add(record.Name + veld.Entry, "Regular");
                    }

                    negeerSelectedItem = false;

                    // Hele zooi wordt opnieuw gebonden
                    Source = new BindingSource(record.Entries, null);
                    dataGridView1.DataSource = Source;

                    // nieuw Record wordt aan combobox toegevoegd
                    menuStripTop_ComboBoxSelectRecord.Items.Add(record.Name);
                    menuStripTop_ComboBoxSelectRecord.SelectedItem = record.Name;

                    // In de menustrip wordt een item en event toegevoegd zodat we dit nieuwe Record ook weer
                    // kunnen verwijderen
                    ToolStripMenuItem tsmiDelete = new ToolStripMenuItem(record.Name);
                    menuStripTop_MenuItemDeleteRecord.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiDelete });
                    tsmiDelete.Click += new System.EventHandler(menuStripTop_DeleteRecord_Click);
                    textBox.Text = "";
                }

            }
        }
        private void menuStripTop_MenuItemSaveRecord_Click(object sender, EventArgs e)
        {
            SchrijfML();
        }
        private void menuStripTop_MenuItemOpenNotepad_Click(object sender, EventArgs e)
        {
            Process process = new Process();
            process.StartInfo.FileName = @"C:\Program Files\Notepad++\notepad++.exe";
            process.StartInfo.Arguments = "-n";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            process.Start();
        }
        private void menuStripTop_MenuItemLoadFile_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Text Files (*.txt)|*.txt|Rich Text Files (*.rtf)|*.rtf|All Files (*.*)|*.*";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                file = dialog.FileName;
                try
                {
                    string text = System.IO.File.ReadAllText(file);
                    richTextBox1.Text = text;
                }
                catch (IOException)
                {
                    textBoxAddRow.Text = "something wrong with reading file";
                }
            }
        }
        private void menuStripTop_MenuItemSaveFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                FileName = file,
                //InitialDirectory = Application.StartupPath + "\\Scripts\\",
                Title = "Save Text Files",
                //CheckPathExists = true,
                DefaultExt = "txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(sfd.FileName, richTextBox1.Text);
            }
        }
        private void menuStripTop_ComboBoxSelectView_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dictionary<string, int> view = new Dictionary<string, int>
            {
                { "Text Editor", 0 },
                { "Browser", 1 },
                { "Pictures", 2 }
            };

            ToolStripComboBox cb = sender as ToolStripComboBox;

            switch (cb.SelectedIndex)
            {
                case 0: // Text Editor
                    richTextBox1.Enabled = true;
                    richTextBox1.Visible = true;
                    Browser.Enabled = false;
                    Browser.Visible = false;
                    pictureBox1.Enabled = false;
                    pictureBox1.Visible = false;

                    toolStripTextEditor.Visible = true;
                    toolStripTextEditor.Enabled = true;
                    toolStripBrowser.Visible = false;
                    toolStripBrowser.Enabled = false;

                    // update statusStrip
                    statusStrip1_labelCurrentView.Text = "Text Editor";
                    break;
                case 1: // Browser
                    toolStripBrowser_ButtonOpenDevTools.Visible = true;
                    Browser.Enabled = true;
                    Browser.Visible = true;
                    richTextBox1.Enabled = false;
                    richTextBox1.Visible = false;
                    pictureBox1.Enabled = false;
                    pictureBox1.Visible = false;

                    toolStripBrowser.Visible = true;
                    toolStripBrowser.Enabled = true;
                    toolStripTextEditor.Visible = false;
                    toolStripTextEditor.Enabled = false;

                    // update statusStrip
                    statusStrip1_labelCurrentView.Text = "Browser";
                    break;
                case 2: // Pictures
                    pictureBox1.Visible = true;
                    pictureBox1.Enabled = true;
                    if (File.Exists(name))
                    {
                        pictureBox1.Image = System.Drawing.Image.FromFile(name);
                    }
                    richTextBox1.Enabled = false;
                    richTextBox1.Visible = false;
                    Browser.Enabled = false;
                    Browser.Visible = false;

                    toolStripTextEditor.Visible = false;
                    toolStripTextEditor.Enabled = false;
                    toolStripBrowser.Visible = false;
                    toolStripBrowser.Enabled = false;

                    // update statusStrip
                    statusStrip1_labelCurrentView.Text = "Pictures";
                    break;
            }
        }
        private void menuStripTop_DeleteRecord_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
            Record record = new();
            record = Records.FirstOrDefault(x => x.Name == tsmi.Text);
            if (record.Name != "22")
            {
                menuStripTop_ComboBoxSelectRecord.SelectedItem = menuStripTop_ComboBoxSelectRecord.Items[0];
                // teardown
                Records.Remove(record);
                // unregister events
                foreach (ToolStripDropDownItem item in menuStripTop_MenuItemDeleteRecord.DropDownItems)
                {
                    item.Click -= new System.EventHandler(menuStripTop_DeleteRecord_Click);
                }
                menuStripTop_MenuItemDeleteRecord.DropDownItems.Clear();

                negeerSelectedItem = true;
                menuStripTop_ComboBoxSelectRecord.Items.Clear();

                // set-up
                foreach (Record rec in Records)
                {
                    menuStripTop_ComboBoxSelectRecord.Items.Add(rec.Name);
                    ToolStripMenuItem tsmiDelete = new ToolStripMenuItem(rec.Name);
                    menuStripTop_MenuItemDeleteRecord.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiDelete });
                    tsmiDelete.Click += new System.EventHandler(menuStripTop_DeleteRecord_Click);
                }
                Record r = new()
                {
                    Entries = Records.FirstOrDefault().Entries
                };
                Source = new BindingSource(r.Entries, null);

                if (menuStripTop_ComboBoxSelectRecord.Items.Count != 0)
                {
                    menuStripTop_ComboBoxSelectRecord.SelectedItem = menuStripTop_ComboBoxSelectRecord.Items[0];
                }
            }
        }
        #endregion

        #region toolStripRecords methods
        private void toolStripRecords_ButtonRowDown_Click(object sender, EventArgs e)
        {
            int row = dataGridView1.CurrentCell.RowIndex;
            int totalrows = dataGridView1.Rows.Count;

            string name = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
            Record record = Records.First(n => n.Name == name);

            Veld veld = new();
            veld = record.Entries[row];

            if (row < totalrows - 1)
            {
                record.Entries.RemoveAt(row);
                record.Entries.Insert(row + 1, veld);
                dataGridView1.CurrentCell = dataGridView1[0, row + 1];

                dataGridView1_PasLayoutToe();
            }
        }
        private void toolStripRecords_ButtonRowUp_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                int rowIndex = dataGridView1.SelectedCells[0].RowIndex;

                if (rowIndex > 0)
                {
                    DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
                    DataGridViewRow rowAbove = dataGridView1.Rows[rowIndex - 1];

                    // Swap the rows
                    toolStripRecords_SwapRows(selectedRow, rowAbove);

                    // Update the selected row
                    dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex - 1].Cells[0];
                    dataGridView1_PasLayoutToe();
                }
            }
        }
        private void toolStripRecords_SwapRows(DataGridViewRow row1, DataGridViewRow row2)
        {
            DataGridViewRow temp = (DataGridViewRow)row1.Clone();
            for (int i = 0; i < row1.Cells.Count; i++)
            {
                temp.Cells[i].Value = row1.Cells[i].Value;
                row1.Cells[i].Value = row2.Cells[i].Value;
                row2.Cells[i].Value = temp.Cells[i].Value;
            }
        }
        private void toolStripRecords_ButtonRowTop_Click(object sender, EventArgs e)
        {
            if (toolStripRecords_ButtonRowTop.Text == "Top ↑")
            {
                toolStripRecords_RowToTop();
                toolStripRecords_ButtonRowTop.Text = "Bottom ↓";
            }
            else
            {
                toolStripRecords_RowToBottom();
                toolStripRecords_ButtonRowTop.Text = "Top ↑";
            }
        }
        private void toolStripRecords_RowToTop()
        {
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            int totalrows = dataGridView1.Rows.Count;
            //int bottom = totalrows - 1;

            string name = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
            Record record = Records.First(n => n.Name == name);

            Veld veld = new Veld();
            veld = record.Entries[rowIndex];

            if (rowIndex != 0)
            {
                record.Entries.RemoveAt(rowIndex);
                record.Entries.Insert(0, veld);
                dataGridView1.CurrentCell = dataGridView1[0, 0];

                dataGridView1_PasLayoutToe();
            }
        }
        private void toolStripRecords_RowToBottom()
        {
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            int totalrows = dataGridView1.Rows.Count;
            int bottom = totalrows - 1;

            string name = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
            Record record = Records.First(n => n.Name == name);

            Veld veld = new Veld();
            veld = record.Entries[rowIndex];

            if (rowIndex < bottom)
            {
                record.Entries.RemoveAt(rowIndex);
                record.Entries.Insert(bottom, veld);
                dataGridView1.CurrentCell = dataGridView1[0, bottom];

                dataGridView1_PasLayoutToe();
            }
        }
        private void toolStripRecords_ButtonRecordToTextEditor_Click(object sender, EventArgs e)
        {
            menuStripTop_ComboBoxSelectView.SelectedItem = menuStripTop_ComboBoxSelectView.Items[0];
            this.Size = new Size(widthForm, heightForm);
            splitContainer2.SplitterDistance = 364;
            richTextBox1.Visible = true;
            richTextBox1.BackColor = System.Drawing.Color.FromArgb(210, 210, 210);
            panelViews.BackColor = System.Drawing.Color.FromArgb(210, 210, 210);

            richTextBox1.Clear();
            string name = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
            Record record = Records.First(r => r.Name == name);
            richTextBox1.SelectionAlignment = HorizontalAlignment.Left;
            richTextBox1.SelectionIndent = 20;

            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;

            //System.Drawing.Font f = richTextBox1.SelectionFont;
            foreach (var veld in record.Entries)
            {
                teller++;
                if (teller % 2 == 0)
                {
                    richTextBox1.SelectionColor = Color.Black;
                }
                else
                {
                    richTextBox1.SelectionColor = Color.Black;
                }
                richTextBox1.AppendText(veld.Entry);
                richTextBox1.AppendText(Environment.NewLine);
                richTextBox1.SelectionColor = Color.FromArgb(102, 102, 102);
            }
            splitContainer2.FixedPanel = FixedPanel.None;
        }
        private void toolStripRecords_TextBoxFind_KeyDown(object sender, KeyEventArgs e)
        {
            // zoekfunctie in alle records en entries
            ToolStripTextBox textBox = sender as ToolStripTextBox;
            if (e.KeyCode == Keys.Enter)
            {
                foreach (var record in Records)
                {
                    var aaa = record.Entries.Where(v => v.Entry.Contains(textBox.Text, StringComparison.OrdinalIgnoreCase));
                    if (aaa.Any())
                    {
                        foreach (var item in aaa)
                        {
                            richTextBox1.AppendText(Environment.NewLine);
                            richTextBox1.AppendText(item.Entry);
                        }
                    }
                }
            }
        }
        private void toolStripRecords_ButtonClose_Click(object sender, EventArgs e)
        {
            ToolStripButton tsBtn = sender as ToolStripButton;
            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;

            if (tsBtn.Text == "Open →")
            {
                this.Size = new Size(widthForm, heightForm);
                splitContainer2.SplitterDistance = 364;
                tsBtn.Text = "← Close";
                splitContainer2.Panel2Collapsed = false;

                panelViews.BackColor = System.Drawing.Color.FromArgb(210, 210, 210);
            }
            else if (tsBtn.Text == "← Close")
            {
                this.Size = new Size(423, heightForm);
                splitContainer2.SplitterDistance = 364;
                tsBtn.Text = "Open →";
                splitContainer2.Panel2Collapsed = true;
            }

            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.None;
        }
        #endregion

        #region dataGridView1 methods
        private void dataGridView1_SizeChanged(object sender, EventArgs e)
        {
            textBoxAddRow.Text = splitContainer2.SplitterDistance.ToString();
        }
        private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.CurrentCell.Style.SelectionBackColor = Color.DarkSlateGray;
            dataGridView1.CurrentCell.Style.SelectionForeColor = Color.FromArgb(255, 255, 255);
            clipboardText = (string)dataGridView1.CurrentCell.Value;
            int lengte = clipboardText.Length;
            string weergaveStatusStrip = Regex.Replace(clipboardText, @"\r\n?|\n", " ");
            if (lengte < 25)
            {
                statusStrip1_LabelCurrentClipboard.Text = weergaveStatusStrip.Trim();
            }
            else
            {
                statusStrip1_LabelCurrentClipboard.Text = weergaveStatusStrip[..25].Trim() + "...";
            }
        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex > -1)
            {
                string s = (string)dataGridView1.CurrentCell.Value;
                if (!String.IsNullOrEmpty(s))
                {
                    lastEntry = s;
                    Clipboard.SetText(s);
                    int l = s.Length;
                    textBoxAddRow.Text = l.ToString();
                }

                if (dataGridView1.CurrentCell.GetType() == typeof(DataGridViewLinkCell))
                {
                    DialogResult result;
                    result = MessageBox.Show("Link Verwijderen?", "Catch Alert", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        if (!String.IsNullOrEmpty((string)dataGridView1.CurrentCell.Value))
                        {
                            int rowindex = e.RowIndex;
                            dataGridView1.Rows.RemoveAt(rowindex);
                        }
                    }
                    else
                    {
                        //catch exception
                        string url = (string)dataGridView1.CurrentCell.Value;
                        Process.Start(url);
                    }
                }
            }
        }
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DialogResult result;
            result = MessageBox.Show("Item verwijderen?", "Catch Alert", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                if (!String.IsNullOrEmpty((string)dataGridView1.CurrentCell.Value))
                {
                    int rowindex = e.RowIndex;
                    Source.RemoveAt(rowindex);
                    Source.DataSource = null;

                    if (!negeerSelectedItem)
                    {
                        string recordName = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
                        Record record = Records.FirstOrDefault(r => r.Name == recordName);

                        dataGridView1.DefaultCellStyle.Font = new System.Drawing.Font("Consolas", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);

                        Source = new BindingSource(record.Entries, null);
                        dataGridView1.DataSource = Source;
                    }

                    negeerSelectedItem = false;
                    // focus zou nu op row boven de verwijderde row moeten liggen.
                    dataGridView1_PasLayoutToe();
                }
            }
        }
        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                //if (System.Uri.IsWellFormedUriString(r.Cells[0].Value.ToString(), UriKind.Absolute))
                //{
                //    r.Cells[0] = new DataGridViewLinkCell();
                //    DataGridViewLinkCell c = r.Cells[0] as DataGridViewLinkCell;
                //    c.LinkColor = Color.Green;
                //}
            }
        }
        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            int row = e.RowIndex;
            int col = e.ColumnIndex;
            DataGridViewCell cell = dataGridView1.Rows[row].Cells[col];

            string s = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();

            if ((string)cell.Value != null)
            {
                //FormatKeyStyleDictionary((string)cell.Value);
                if (!styleDictionary.ContainsKey(s + (string)cell.Value))
                {
                    styleDictionary.Add(s + (string)cell.Value, "Regular");
                }
            }

        }
        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            //dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
        }
        /// <summary>
        /// Applies customized lay-out to datagridview
        /// </summary>
        private void dataGridView1_PasLayoutToe()
        {
            dataGridView1.CurrentCell.Style.SelectionBackColor = Color.DarkSlateGray;
            string recordName = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
            Record record = Records.FirstOrDefault(x => x.Name == recordName);

            //int count = r.RecordList.Count;
            int counter = 0;
            foreach (var item in record.Entries)
            {
                DataGridViewCell cell = dataGridView1.Rows[counter].Cells[0];
                cell.Style.Padding = new System.Windows.Forms.Padding(6, 3, 1, 3);
                cell.Style.BackColor = SystemColors.ControlDark;
                //FormatKeyStyleDictionary(item.Entry);

                string key = record.Name + item.Entry;

                if (styleDictionary[key] == "White on black")
                {
                    //cell.Style.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
                    cell.Style.ForeColor = Color.White;
                    cell.Style.BackColor = Color.FromArgb(90, 90, 90);
                }
                if (styleDictionary[key] == "Red on white")
                {
                    cell.Style.ForeColor = Color.Red;
                    cell.Style.BackColor = Color.White;
                }
                if (styleDictionary[key] == "Light purplish")
                {
                    cell.Style.ForeColor = Color.Purple;
                    cell.Style.BackColor = Color.FromArgb(200, 200, 200);
                }
                if (styleDictionary[key] == "Light pinkish")
                {
                    cell.Style.ForeColor = Color.LightPink;
                    cell.Style.BackColor = Color.FromArgb(90, 90, 90);
                }
                if (styleDictionary[key] == "Light blueish")
                {
                    cell.Style.ForeColor = Color.FromArgb(156, 220, 218);
                    cell.Style.BackColor = Color.FromArgb(90, 90, 90);
                }
                if (styleDictionary[key] == "Regular")
                {
                    cell.Style.ForeColor = Color.FromArgb(0, 0, 0);
                    cell.Style.BackColor = SystemColors.ControlLight;
                }

                counter++;
                textBoxAddRow.Text = counter.ToString();
            }
        }
        #endregion

        #region contextMenuStripRecords methods
        private void contextMenuStripRecords_MenuItemToTextBox_Click(object sender, EventArgs e)
        {
            string s = (string)dataGridView1.CurrentCell.Value;
            if (!String.IsNullOrEmpty(s))
            {
                richTextBox1.AppendText(Environment.NewLine + s);
            }
        }
        private void contextMenuStripRecords_MenuItemFreeze_Click(object sender, EventArgs e)
        {
            int row = dataGridView1.CurrentCell.RowIndex;
            dataGridView1.Rows[row].Frozen = true;
        }
        private void contextMenuStripRecords_MenuItemWhiteOnBlack_Click(object sender, EventArgs e)
        {
            contextMenuStripRecords_CellLayout("White on black");
        }
        private void contextMenuStripRecords_MenuItemPurplish_Click(object sender, EventArgs e)
        {
            contextMenuStripRecords_CellLayout("Light purplish");
        }
        private void contextMenuStripRecords_MenuItemPinkish_Click(object sender, EventArgs e)
        {
            contextMenuStripRecords_CellLayout("Light pinkish");
        }
        private void contextMenuStripRecords_MenuItemBlueish_Click(object sender, EventArgs e)
        {
            contextMenuStripRecords_CellLayout("Light blueish");
        }
        private void contextMenuStripRecords_MenuItemRedOnWhite_Click(object sender, EventArgs e)
        {
            contextMenuStripRecords_CellLayout("Red on white");
        }
        private void contextMenuStripRecords_MenuItemRegular_Click(object sender, EventArgs e)
        {
            contextMenuStripRecords_CellLayout("Regular");
        }
        private void contextMenuStripRecords_CellLayout(string LayoutName)
        {
            string s = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
            Record record = Records.FirstOrDefault(r => r.Name == s);
            string value = dataGridView1.CurrentCell.Value.ToString();
            Veld v = record.Entries.FirstOrDefault(v => v.Entry == value);

            if (v.Entry == value)
            {
                //FormatKeyStyleDictionary(v.Entry);
                styleDictionary[record.Name + v.Entry] = LayoutName;

                dataGridView1_PasLayoutToe();
            }
        }
        #endregion

        #region panelViews methods
        /// <summary>
        /// Handles the ClientSizeChanged event of the panelViews control and adjusts the width of the toolStripBrowser_TextBoxUrl.
        /// </summary>
        private void panelViews_ClientSizeChanged(object sender, EventArgs e)
        {
            Panel p = sender as Panel;
            int width = p.Width -
                toolStripBrowser_ButtonOpenDevTools.Width -
                toolStripBrowser_ButtonScreenshot.Width -
                toolStripBrowser_LabelGoTo.Width;

            toolStripBrowser_TextBoxUrl.Width = width - 80;
            textBoxAddRow.Text = toolStripBrowser_TextBoxUrl.Width.ToString();
        }
        #endregion

        #region toolStripAddRow methods
        private void toolStripAddRow_ButtonClear_Click(object sender, EventArgs e)
        {
            textBoxAddRow.Clear();
        }
        private void toolStripAddRow_ButtonAddToRecord_Click(object sender, EventArgs e)
        {
            if (textBoxAddRow.TextLength > 0)
            {
                // voeg toe aan bestaand record

                string recordName = menuStripTop_ComboBoxSelectRecord.SelectedItem.ToString();
                Record record = Records.FirstOrDefault(x => x.Name == recordName);
                Veld veld = new Veld();
                veld.Entry = textBoxAddRow.Text;
                record.Entries.Add(veld);

                //FormatKeyStyleDictionary(veld.Entry);

                if (!styleDictionary.ContainsKey(record.Name + veld.Entry))
                {
                    styleDictionary.Add(record.Name + veld.Entry, "Regular");
                }

                dataGridView1_PasLayoutToe();

                textBoxAddRow.Clear();
            }
        }
        #endregion

        #region toolStripTextEditor methods
        private void toolStripTextEditor_ButtonFind_Click(object sender, EventArgs e)
        {
            Timer1.Stop();
            Timer1.Enabled = false;

            string word = toolStripTextEditor_TextBoxFind.Text.Length > 0 ? toolStripTextEditor_TextBoxFind.Text : (string)dataGridView1.CurrentCell.Value;

            richTextBox1.SelectionStart = 0;
            richTextBox1.SelectionLength = richTextBox1.Text.Length;
            //richTextBox1.SelectionBackColor = System.Drawing.Color.Black;

            int i = 0;
            while (i < richTextBox1.TextLength)
            {
                int wordStartIndex = richTextBox1.Find(word, i, RichTextBoxFinds.None);
                if (wordStartIndex > -1)
                {
                    richTextBox1.SelectionStart = wordStartIndex;
                    richTextBox1.SelectionLength = word.Length;
                    richTextBox1.SelectionBackColor = System.Drawing.Color.Yellow;

                    i = wordStartIndex + word.Length;
                }
                else
                    break;
            }

            Timer1.Enabled = true;
            Timer1.Start();
        }
        private void toolStripTextEditor_ButtonReplace_Click(object sender, EventArgs e)
        {
            Timer1.Stop();
            Timer1.Enabled = false;

            if (richTextBox1.Text.Length > 0)
            {
                richTextBox1.Text = richTextBox1.Text.Replace(toolStripTextEditor_TextBoxFind.Text, toolStripTextEditor_TextBoxReplace.Text);
            }

            Timer1.Enabled = true;
            Timer1.Start();
        }
        private void toolStripTextEditor_ButtonClear_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            this.Refresh();
        }
        private void toolStripTextEditor_ButtonIncreaseFont_Click(object sender, EventArgs e)
        {
            richTextBox1.Font = new System.Drawing.Font(
                richTextBox1.Font.FontFamily,
                richTextBox1.Font.Size + 1,
                richTextBox1.Font.Style,
                richTextBox1.Font.Unit
            );
        }
        private void toolStripTextEditor_ButtonDecreaseFont_Click(object sender, EventArgs e)
        {
            richTextBox1.Font = new System.Drawing.Font(
                richTextBox1.Font.FontFamily,
                richTextBox1.Font.Size - 1,
                richTextBox1.Font.Style,
                richTextBox1.Font.Unit);
        }
        #endregion

        #region toolStripBrowser methods
        private void toolStripBrowser_ButtonOpenDevTools_Click(object sender, EventArgs e)
        {
            Browser.CoreWebView2.OpenDevToolsWindow();
        }
        private async void toolStripBrowser_ButtonScreenshot_Click(object sender, EventArgs e)
        {
            using SaveFileDialog saveDialog = new()
            {
                Filter = "PNG Image (*.png)|*.png",
                DefaultExt = "png",
                AddExtension = true,
                FileName = $"Screenshot_{DateTime.Now:yyyy_dd_MM_HHmmss}.png"
            };

            if (saveDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                name = saveDialog.FileName;
                await using FileStream stream = new(
                    saveDialog.FileName,
                    FileMode.Create,
                    FileAccess.Write);

                await Browser.CoreWebView2.CapturePreviewAsync(
                    CoreWebView2CapturePreviewImageFormat.Png,
                    stream);

                MessageBox.Show(
                    $"Screenshot saved to:\n{saveDialog.FileName}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void toolStripBrowser_TextBoxUrl_KeyDown(object sender, KeyEventArgs e)
        {
            ToolStripTextBox textBox = sender as ToolStripTextBox;
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox.Text.StartsWith("http"))
                {
                    Browser.CoreWebView2.Navigate(textBox.Text);
                }
                else
                {
                    Browser.CoreWebView2.Navigate("https://www.startpage.com");
                    //await Browser.CoreWebView2.ExecuteScriptAsync("""
                    //     document.querySelector('#q');
                    //    """
                    //    );
                }
            }
        }
        private void toolStripBrowser_TextBoxUrl_Click(object sender, EventArgs e)
        {
            ToolStripTextBox textBox = sender as ToolStripTextBox;
            textBox.Text = string.Empty;
        }

        #endregion

        #region browser methods
        private void Browser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            WebView2 br = sender as WebView2;
            // you could now add the url to a record or do something else with it 
            textBoxAddRow.Text = br.Source.ToString();    
        }
        #endregion
    }

    public class Record
    {
        /*
        public BindingList<string> Entries= new BindingList<string>(); 
         */
        public Record() { }
        public string Name { get; set; }
        public BindingList<Veld> Entries = [];
    }

    public class Veld
    {
        public string Entry { get; set; }
    }

    public class Form2 : Form1
    {
        public Form2()
        {
            Load += Form2_Load!;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(1040, this.Height);
        }
    }
}