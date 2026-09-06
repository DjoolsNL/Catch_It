using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace Catch_It
{
    public partial class Form1 : Form
    {
        #region Fields
        /// <summary>
        /// Timer1 event fires elke seconde en checkt of 'clipboardText' afwijkt van 'lastEntry'. 
        /// Wijkt hij af dan wordt clipboardText toegevoegd aan Record. Bool 'clipboardGewijzigd' 
        /// vergelijkt beide.
        /// Beide strings starten met de waarde "a" zodat het programma bij start niet meteen de
        /// bestaande inhoud vh clipb opslaat.
        /// </summary>
        private Timer Timer1;

        private string clipboardText = "a";
        private string lastEntry = "a";
        private bool clipboardGewijzigd
        {
            get
            {
                bool changed = false;
                clipboardText = Clipboard.GetText();

                if (clipboardText != lastEntry)
                {
                    changed = true;
                }
                return changed;
            }
        }

        private List<Record> Franz = new List<Record>();
        private Dictionary<string, string> styleDictionary = new Dictionary<string, string>();
        private static BindingSource Source;
        private XElement xmlRoot;
        private bool negeerSelectedItem = false;
        string file = "";
        int teller;
        #endregion

        public Form1()
        {
            InitializeComponent();
            Timer1 = new System.Windows.Forms.Timer();
            Timer1.Enabled = true;
            Timer1.Tick += new System.EventHandler(Timer1_Tick);
            Timer1.Interval = 1000;

            Source = new BindingSource();
            dataGridView1.DataSource = Source;
            menuStrip1.Cursor = System.Windows.Forms.Cursors.Arrow;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        #region Data File Dingen: Lees en Schrijf Xml - kan in aparte class

        private void LeesXMLFile()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            String path = Directory.GetCurrentDirectory();
            xmlRoot = XElement.Load(path + "\\Catch.xml");

            foreach (var xmlRecord in xmlRoot.Elements())
            {
                Record record = new Record();
                record.Name = (string)xmlRecord.FirstAttribute;
                menucomboBox1.Items.Add(record.Name);
                Franz.Add(record);

                // Add a ToolStripMenuItem for deleting the record to the tsmiDeleteRecord dropdown
                ToolStripMenuItem tsmiDelete = new ToolStripMenuItem(record.Name);
                tsmiDeleteRecord.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiDelete });
                // Add the click event handler for the delete menu item
                tsmiDelete.Click += new System.EventHandler(menuDelete_Click);

                // Add the entries to the record's RecordList
                foreach (var xElement in xmlRecord.Elements())
                {
                    // Get the style attribute value
                    string style = (string)xElement.FirstAttribute;
                    Veld veld = new Veld();
                    veld.Entry = xElement.Value;

                    string substring = veld.Entry;
                    FormatKeyStyleDictionary(substring);

                    if (!styleDictionary.ContainsKey(record.Name + substring))
                    {
                        styleDictionary.Add(record.Name + substring, style);    
                    }

                    record.Velden.Add(veld);
                }
            }

            Record recordDisplayed = new Record();
            recordDisplayed.Velden = Franz.FirstOrDefault().Velden;

            Source = new BindingSource(recordDisplayed.Velden, null);

            if (menucomboBox1.Items.Count != 0)
            {
                menucomboBox1.SelectedItem = menucomboBox1.Items[0];
            }
        }

        private void SchrijfML()
        {
            IEnumerable<XElement> LoopFranz()
            {
                foreach (var record in Franz)
                {
                    XElement xRecord = new XElement("record");
                    XAttribute xName = new XAttribute("name", record.Name);
                    xRecord.Add(xName);

                    foreach (var veld in record.Velden)
                    {
                        XElement xEntry = new XElement("entry", veld.Entry);

                        string substring = veld.Entry;
                        FormatKeyStyleDictionary(substring);

                        string s = styleDictionary[record.Name + substring];
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

        #region Events Form Dingen
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Catch / Status: on / © 2022 by Djools";
            LeesXMLFile();
            splitContainer2.IsSplitterFixed = false;
            dataGridView1.Cursor = System.Windows.Forms.Cursors.Default;
            buttonStop.Cursor = System.Windows.Forms.Cursors.Default;
            buttonListToRichTextBox.Cursor = System.Windows.Forms.Cursors.Default;
            buttonOpen.Cursor = System.Windows.Forms.Cursors.Default;
            buttonRowUp.Cursor = System.Windows.Forms.Cursors.Default;
            buttonRowDown.Cursor = System.Windows.Forms.Cursors.Default;
            buttonRowTopBottom.Cursor = System.Windows.Forms.Cursors.Default;
            buttonReplace.Cursor = System.Windows.Forms.Cursors.Default;
            buttonFind.Cursor = System.Windows.Forms.Cursors.Default;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SchrijfML();
        }

        // wordt aangeroepen door button in top panel
        private void OpenForm(Button btn)
        {
            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;

            if (btn.Text == "Open →")
            {
                toolTip1.SetToolTip(this.buttonOpen, "Close Right Textbox Section");
                this.Size = new Size(1040, this.Height);
                splitContainer2.SplitterDistance = 364;
                btn.Text = "Close";

                buttonClearRichTB.Visible = true;
                buttonClearRichTB.Enabled = true;
                richTextBox1.Visible = true;
                richTextBox1.BackColor = Color.DarkSlateBlue;
                panelRichTextBox.BackColor = Color.DarkSlateBlue;
                richTextBox1.ForeColor = Color.White;   
            }
            else
            {
                toolTip1.SetToolTip(this.buttonOpen, "Open Right Textbox Section");
                this.Size = new Size(423, 580);
                splitContainer2.SplitterDistance = 364;
                btn.Text = "Open →";

                buttonClearRichTB.Visible = false;
                buttonClearRichTB.Enabled = false;
                richTextBox1.Visible = false;

                richTextBox1.BackColor = System.Drawing.Color.FromArgb(125, 125, 125);
                panelRichTextBox.BackColor = System.Drawing.Color.FromArgb(125, 125, 125);
            }

            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.None;
        }


        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
            this.SuspendLayout();
            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                textBox1.Text = "been there";

            }

            if (this.Size.Width > 480)
            {
                buttonClearRichTB.Visible = true;
                buttonClearRichTB.Enabled = true;
            }
            else
            {
                buttonClearRichTB.Visible = false;
                buttonClearRichTB.Enabled = false;
            }

            this.ResumeLayout();

            splitContainer2.FixedPanel = FixedPanel.None;
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (this.Size.Width > 480)
            {
                buttonClearRichTB.Visible = true;
                buttonClearRichTB.Enabled = true;
            }
            else
            {
                buttonClearRichTB.Visible = false;
                buttonClearRichTB.Enabled = false;
            }
        }
        #endregion

        #region Events Timer Dingen

        private void Timer1_Tick(object sender, EventArgs e)
        {
            teller++;
            if (teller == 1)
            {
                // Rendering bij opstart is niet optimaal als datagridview samen met de rest gepaint wordt
                // dus we renderen hem een vertraging van 1 seconde
                dataGridView1.Visible = true;
            }
            if (clipboardGewijzigd)
            {
                int count = clipboardText.Count();
                if (count > 0)
                {
                    // voeg clipboardText toe aan bestaand record
                    if (!String.IsNullOrEmpty(menucomboBox1.SelectedItem.ToString()))
                    {
                        // in juiste record opslaan
                        string recordName = menucomboBox1.SelectedItem.ToString();
                        Record record = Franz.FirstOrDefault(r => r.Name == recordName);
                        Veld veld = new Veld();
                        veld.Entry = clipboardText;
                        record.Velden.Add(veld);

                        // toevoeging aan StyleDictionary voor de opmaak van het record en de entries in de ui
                        string substr = veld.Entry;
                        //int countt = substr.Count();

                        //if (countt > 30)
                        //{
                        //    substr.Substring(0, 30);
                        //}
                        FormatKeyStyleDictionary(substr);

                        if (!styleDictionary.ContainsKey(record.Name + substr))
                        {
                            styleDictionary.Add(record.Name + substr, "Regular");
                        }

                        // de BindingSource zorgt ervoor dat alle opmaak bij elke wijziging wegvalt en 
                        // onderstaande void brengt die weer terug
                        PasLayoutToe();
                    }

                    // zorgt ervoor dat niets meer wordt toegevoegd tenzij clipboardText wijzigt.
                    lastEntry = clipboardText;
                }
            }
        }
        #endregion

        #region Events MenuStrip Dingen

        private void menucomboBox1_Click(object sender, EventArgs e)
        {
            negeerSelectedItem = false;
        }

        private void menucomboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (!negeerSelectedItem)
            {
                string a = menucomboBox1.SelectedItem.ToString();

                Record record = Franz.FirstOrDefault(r => r.Name == a);

                // simpele manier om een record in een groter font weer te geven
                if (record.Name.Contains('*'))
                {
                    dataGridView1.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
                }
                else
                {
                    dataGridView1.DefaultCellStyle.Font = new System.Drawing.Font("Consolas", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
                }

                Source = new BindingSource(record.Velden, null);
                dataGridView1.DataSource = Source;

                PasLayoutToe();
            }

            negeerSelectedItem = false;
            PasLayoutToe();
        }

        private void menuAddNew_Click(object sender, EventArgs e)
        {
            if (menutextBox2.TextLength > 0)
            {
                // naam mag niet te lang zijn 
                string newName = menutextBox2.Text;
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
                Franz.Add(record);
                record.Name = newName;
                Veld veld = new Veld();
                veld.Entry = "Cought";
                record.Velden.Add(veld);

                // nieuwe veld in Record wordt ook in StyleDictionary opgenomen
                string substring = veld.Entry;
                FormatKeyStyleDictionary(substring);

                if (!styleDictionary.ContainsKey(record.Name + substring))
                {
                    styleDictionary.Add(record.Name + substring, "Regular");
                }

                negeerSelectedItem = false;

                // Hele zooi wordt opnieuw gebonden
                Source = new BindingSource(record.Velden, null);
                dataGridView1.DataSource = Source;

                // nieuw Record wordt aan combobox toegevoegd
                menucomboBox1.Items.Add(record.Name);
                menucomboBox1.SelectedItem = record.Name;

                // In de menustrip wordt een item en event toegevoegd zodat we dit nieuwe Record ook weer
                // kunnen verwijderen
                ToolStripMenuItem tsmiDelete = new ToolStripMenuItem(record.Name);
                tsmiDeleteRecord.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiDelete });
                tsmiDelete.Click += new System.EventHandler(menuDelete_Click);
            }

            // nu het nieuwe Record is toegevoegd poetsen we de textbox waar we de naam ingaven.
            menutextBox2.Text = "";
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            SchrijfML();
        }

        private void menuDelete_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
            string nameRecord = tsmi.Text;
            Record record = new Record();
            record = Franz.FirstOrDefault(x => x.Name == nameRecord);
            if (record.Name != "22")
            {
                menucomboBox1.SelectedItem = menucomboBox1.Items[0];
                Franz.Remove(record);
                tsmiDeleteRecord.DropDownItems.Clear();
                negeerSelectedItem = true;
                menucomboBox1.Items.Clear();

                foreach (Record rec in Franz)
                {
                    menucomboBox1.Items.Add(rec.Name);
                    ToolStripMenuItem tsmiDelete = new ToolStripMenuItem(rec.Name);
                    tsmiDeleteRecord.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiDelete });
                    tsmiDelete.Click += new System.EventHandler(menuDelete_Click);
                }
                Record recc = new Record();
                recc.Velden = Franz.FirstOrDefault().Velden;
                Source = new BindingSource(recc.Velden, null);

                if (menucomboBox1.Items.Count != 0)
                {
                    menucomboBox1.SelectedItem = menucomboBox1.Items[0];
                }
            }
        }

        private void menuOpenNotepadPlusPlus_Click(object sender, EventArgs e)
        {
            Process process = new Process();
            process.StartInfo.FileName = @"C:\Program Files\Notepad++\notepad++.exe";
            process.StartInfo.Arguments = "-n";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            process.Start();
        }

        private void menuLoadRTB_Click(object sender, EventArgs e)
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
                    textBox1.Text = "something wrong with reading file";
                }
            }
        }

        private void menuSaveRichTextBox_Click(object sender, EventArgs e)
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

        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel1.BackColor = Color.FromArgb(125, 125, 122);
            menuStrip1.BackColor = SystemColors.ActiveBorder;
            dataGridView1.BackColor = Color.FromArgb(240, 240, 240);
            richTextBox1.BackColor = Color.FromArgb(235, 235, 235);
            panelBottom.BackColor = Color.FromArgb(125, 125, 125);
            panelBottomLeft.BackColor = Color.FromArgb(125, 125, 125);
            textBox1.BackColor = Color.FromArgb(255, 255, 244);
            this.BackColor = Color.FromArgb(125, 125, 122);
            menutextBox2.BackColor = Color.FromArgb(255, 255, 244);
            menucomboBox1.BackColor = Color.FromArgb(255, 255, 244);
            foreach (var item in panel1.Controls.OfType<Button>())
            {
                item.ForeColor = Color.FromArgb(255, 255, 255);
            }
        }

        private void colorfulToolStripMenuItem_Click(object sender, EventArgs e)
        {
            menuStrip1.BackColor = Color.FromArgb(104, 204, 153);
            panel1.BackColor = Color.FromArgb(255, 204, 162);
            dataGridView1.BackColor = Color.FromArgb(146, 146, 209);
            richTextBox1.BackColor = Color.FromArgb(255, 155, 204);
            panelBottom.BackColor = Color.FromArgb(255, 104, 102);
            textBox1.BackColor = Color.FromArgb(155, 255, 204);
        }

        private void Dark_Click(object sender, EventArgs e)
        {
            panel1.BackColor = Color.FromArgb(125, 125, 122);
            menuStrip1.BackColor = SystemColors.ActiveBorder;
            dataGridView1.BackColor = Color.FromArgb(240, 240, 240);
            richTextBox1.BackColor = Color.FromArgb(235, 235, 235);
            panelBottom.BackColor = Color.FromArgb(125, 125, 125);
            panelBottomLeft.BackColor = Color.FromArgb(125, 125, 125);
            textBox1.BackColor = Color.FromArgb(255, 255, 244);
            this.BackColor = Color.FromArgb(125, 125, 122);
            menutextBox2.BackColor = Color.FromArgb(255, 255, 244);
            menucomboBox1.BackColor = Color.FromArgb(255, 255, 244);
        }

        #endregion

        #region Events Buttons Top Panel

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;

            if (btn.Text == "Stop")
            {
                Timer1.Stop();
                Timer1.Enabled = false;
                this.Text = "Catch / Status: off / © 2022 by Djools";
                btn.Text = "Start";
            }
            else
            {
                Timer1.Enabled = true;
                Timer1.Start();
                this.Text = "Catch / Status: on / © 2022 by Djools";
                btn.Text = "Stop";
            }
        }

        private void buttonRowDown_Click(object sender, EventArgs e)
        {
            RowDown();
        }

        private void buttonRowUp_Click(object sender, EventArgs e)
        {
            RowUp();
        }

        private void buttonRowTopBottom_Click_1(object sender, EventArgs e)
        {
            if (buttonRowTopBottom.Text == "Top ↑")
            {
                RowToTop();
                buttonRowTopBottom.Text = "Bottom ↓";
            }
            else
            {
                RowToBottom();
                buttonRowTopBottom.Text = "Top ↑";
            }
        }

        private void buttonRecordToRichTextBox_Click(object sender, EventArgs e)
        {
            this.Size = new Size(1040, this.Height);
            splitContainer2.SplitterDistance = 364;
            buttonClearRichTB.Visible = true;
            buttonClearRichTB.Enabled = true;
            richTextBox1.Visible = true;
            richTextBox1.BackColor = Color.White;
            panelRichTextBox.BackColor = Color.White;

            richTextBox1.Clear();
            string name = menucomboBox1.SelectedItem.ToString();
            Record record = Franz.First(r => r.Name == name);
            richTextBox1.SelectionAlignment = HorizontalAlignment.Left;
            richTextBox1.SelectionIndent = 20;

            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            if (this.Size.Width <= 480)
            {
                buttonClearRichTB.Visible = false;
                buttonClearRichTB.Enabled = false;
            }
            else
            {
                buttonClearRichTB.Visible = true;
                buttonClearRichTB.Enabled = true;
            }
            if (this.Size.Width > 480)
            {
                buttonClearRichTB.Visible = true;
                buttonClearRichTB.Enabled = true;
            }
            else
            {
                buttonClearRichTB.Visible = false;
                buttonClearRichTB.Enabled = false;
            }

            buttonOpen.Text = "Close";
            //System.Drawing.Font f = richTextBox1.SelectionFont;
            foreach (var veld in record.Velden)
            {
                teller++;
                if (teller % 2 == 0)
                {
                    richTextBox1.SelectionColor = Color.Red;
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

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            OpenForm(btn);
        }

        private void buttonFind_Click(object sender, EventArgs e)
        {
            Timer1.Stop();
            Timer1.Enabled = false;

            string word = textBoxFind.Text.Length > 0 ? textBoxFind.Text : (string)dataGridView1.CurrentCell.Value;

            richTextBox1.SelectionStart = 0;
            richTextBox1.SelectionLength = richTextBox1.Text.Length;
            richTextBox1.SelectionBackColor = System.Drawing.Color.White;

            int Index = 0;
            while (Index < richTextBox1.TextLength)
            {
                int wordStartIndex = richTextBox1.Find(word, Index, RichTextBoxFinds.None);
                if (wordStartIndex > -1)
                {
                    richTextBox1.SelectionStart = wordStartIndex;
                    richTextBox1.SelectionLength = word.Length;
                    richTextBox1.SelectionBackColor = System.Drawing.Color.Yellow;

                    Index = wordStartIndex + word.Length;
                }
                else
                    break;
            }

            Timer1.Enabled = true;
            Timer1.Start();
        }

        private void buttonReplace_Click(object sender, EventArgs e)
        {
            Timer1.Stop();
            Timer1.Enabled = false;

            if (richTextBox1.Text.Length > 0)
            {
                richTextBox1.Text = richTextBox1.Text.Replace(textBoxFind.Text, textBoxReplace.Text);
            }

            Timer1.Enabled = true;
            Timer1.Start();
        }


        #endregion

        #region Events en andere DatagridView Dingen
        private void InsertRow()
        {
            int row = dataGridView1.CurrentCell.RowIndex;
            int indexRecordList = row + 1;

            string rName = menucomboBox1.SelectedItem.ToString();
            Record r = Franz.First(n => n.Name == rName);
            Veld f = new Veld();
            f.Entry = "";
            r.Velden.Insert(indexRecordList, f);

            PasLayoutToe();
        }
        private void RowToTop()
        {
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            int totalrows = dataGridView1.Rows.Count;
            int bottom = totalrows - 1;

            string name = menucomboBox1.SelectedItem.ToString();
            Record record = Franz.First(n => n.Name == name);

            Veld veld = new Veld();
            veld = record.Velden[rowIndex];

            if (rowIndex != 0)
            {
                record.Velden.RemoveAt(rowIndex);
                record.Velden.Insert(0, veld);
                dataGridView1.CurrentCell = dataGridView1[0, 0];

                PasLayoutToe();
            }
        }
        private void RowToBottom()
        {
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            int totalrows = dataGridView1.Rows.Count;
            int bottom = totalrows - 1;

            string name = menucomboBox1.SelectedItem.ToString();
            Record record = Franz.First(n => n.Name == name);

            Veld veld = new Veld();
            veld = record.Velden[rowIndex];

            if (rowIndex < bottom)
            {
                record.Velden.RemoveAt(rowIndex);
                record.Velden.Insert(bottom, veld);
                dataGridView1.CurrentCell = dataGridView1[0, bottom];

                PasLayoutToe();
            }
        }
        private void RowDown()
        {
            int row = dataGridView1.CurrentCell.RowIndex;
            int totalrows = dataGridView1.Rows.Count;

            string rName = menucomboBox1.SelectedItem.ToString();
            Record r = Franz.First(n => n.Name == rName);

            Veld f = new Veld();
            f = r.Velden[row];

            if (row < totalrows - 1)
            {
                r.Velden.RemoveAt(row);
                r.Velden.Insert(row + 1, f);
                dataGridView1.CurrentCell = dataGridView1[0, row + 1];

                PasLayoutToe();
            }
        }
        private void RowUp()
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                int rowIndex = dataGridView1.SelectedCells[0].RowIndex;

                if (rowIndex > 0)
                {
                    DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
                    DataGridViewRow rowAbove = dataGridView1.Rows[rowIndex - 1];

                    // Swap the rows
                    SwapRows(selectedRow, rowAbove);

                    // Update the selected row
                    dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex - 1].Cells[0];
                    PasLayoutToe();
                }
            }
        }
        private void SwapRows(DataGridViewRow row1, DataGridViewRow row2)
        {
            DataGridViewRow temp = (DataGridViewRow)row1.Clone();
            for (int i = 0; i < row1.Cells.Count; i++)
            {
                temp.Cells[i].Value = row1.Cells[i].Value;
                row1.Cells[i].Value = row2.Cells[i].Value;
                row2.Cells[i].Value = temp.Cells[i].Value;
            }
        }
        private void dataGridView1_SizeChanged(object sender, EventArgs e)
        {
            textBox1.Text = splitContainer2.SplitterDistance.ToString();
        }

        private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.CurrentCell.Style.SelectionBackColor = Color.FromArgb(50, 50, 50);
            dataGridView1.CurrentCell.Style.SelectionForeColor = Color.LightGreen;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.CurrentCell.Style.SelectionBackColor = Color.FromArgb(50, 50, 50);
            dataGridView1.CurrentCell.Style.SelectionForeColor = Color.LightGreen;
            if (e.ColumnIndex > -1)
            {
                string s = (string)dataGridView1.CurrentCell.Value;
                if (!String.IsNullOrEmpty(s))
                {
                    lastEntry = s;
                    Clipboard.SetText(s);
                    int l = s.Length;
                    textBox1.Text = l.ToString();
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
                    dataGridView1.Rows.RemoveAt(rowindex);
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

            string s = menucomboBox1.SelectedItem.ToString();

            string substring = (string)cell.Value;
            //int l = substring.Length;
            if (substring != null)
            {
                FormatKeyStyleDictionary(substring);
                if (!styleDictionary.ContainsKey(s + substring))
                {
                    styleDictionary.Add(s + substring, "Regular");
                    //EntryLengthDictionary.Add(s + substring, l);
                }
            }

        }

        private void CellLayout(string LayoutName)
        {
            string value = dataGridView1.CurrentCell.Value.ToString();
            string s = menucomboBox1.SelectedItem.ToString();
            Record r = Franz.FirstOrDefault(x => x.Name == s);

            foreach (Veld veld in r.Velden)
            {
                if (veld.Entry == value)
                {
                    string substring = veld.Entry;
                    FormatKeyStyleDictionary(substring);
                    styleDictionary[r.Name + substring] = LayoutName;

                    PasLayoutToe();
                }
            }
        }

        #region Events Contextmenu DatagridView
        private void CellLayout1_tsMenuItem_Click(object sender, EventArgs e)
        {
            CellLayout("White on black");
        }
        private void CellLayout2_tsMenuItem_Click(object sender, EventArgs e)
        {
            CellLayout("Light purplish");
        }
        private void CellLayout3_tsMenuItem_Click(object sender, EventArgs e)
        {
            CellLayout("Light pinkish");
        }
        private void CellLayout4_tsMenuItem_Click(object sender, EventArgs e)
        {
            CellLayout("Light blueish");
        }
        private void CellLayout5_tsMenuItem_Click(object sender, EventArgs e)
        {
            CellLayout("Red on white");
        }
        private void cellLayout6_tsMenuItem_Click(object sender, EventArgs e)
        {
            CellLayout("Regular");
        }
        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
        }
        private void FreezeStripMenuItem_Click(object sender, EventArgs e)
        {
            int row = dataGridView1.CurrentCell.RowIndex;
            dataGridView1.Rows[row].Frozen = true;
        }
        #endregion
        /// <summary>
        /// Applies customized lay-out to datagridview
        /// </summary>
        private void PasLayoutToe()
        {
            dataGridView1.CurrentCell.Style.SelectionBackColor = Color.FromArgb(50, 50, 50);
            string recordName = menucomboBox1.SelectedItem.ToString();
            Record r = Franz.FirstOrDefault(x => x.Name == recordName);

            //int count = r.RecordList.Count;
            int counter = 0;
            foreach (var item in r.Velden)
            {
                DataGridViewCell cell = dataGridView1.Rows[counter].Cells[0];
                cell.Style.Padding = new System.Windows.Forms.Padding(6, 3, 1, 3);
                cell.Style.BackColor = Color.FromArgb(100, 100, 100);

                string substring = item.Entry;
                FormatKeyStyleDictionary(substring);

                string key = r.Name + substring;

                if (styleDictionary[key] == "White on black")
                {
                    cell.Style.ForeColor = Color.White;
                    cell.Style.BackColor = Color.FromArgb(50, 50, 50);

                }
                if (styleDictionary[key] == "Red on white")
                {
                    //cell.Style.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
                    cell.Style.ForeColor = Color.Red;
                    cell.Style.BackColor = Color.White;
                }
                if (styleDictionary[key] == "Light purplish")
                {
                    //cell.Style.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
                    cell.Style.ForeColor = Color.Purple;
                    cell.Style.BackColor = Color.FromArgb(200, 200, 200);

                }
                if (styleDictionary[key] == "Light pinkish")
                {
                    //cell.Style.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
                    cell.Style.ForeColor = Color.LightPink;
                    cell.Style.BackColor = Color.FromArgb(50, 50, 50);
                }
                if (styleDictionary[key] == "Light blueish")
                {
                    //cell.Style.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
                    cell.Style.ForeColor = Color.FromArgb(156, 220, 218);
                    cell.Style.BackColor = Color.FromArgb(30, 30, 30);
                }
                if (styleDictionary[key] == "Regular")
                {
                    //cell.Style.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
                    cell.Style.ForeColor = Color.White;
                    cell.Style.BackColor = Color.FromArgb(100, 100, 100);
                }

                counter++;
            }
        }

        private string FormatKeyStyleDictionary(string value)
        {
            if (value.Length > 30)
            {
                return value.Substring(0, 30);
            }
            return value;
        }
        #endregion

        #region Events Buttons Bottom Panel
        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (textBox1.TextLength > 0)
            {
                // voeg toe aan bestaand record

                string recordName = menucomboBox1.SelectedItem.ToString();
                Record r = Franz.FirstOrDefault(x => x.Name == recordName);
                Veld f = new Veld();
                f.Entry = textBox1.Text;
                r.Velden.Add(f);

                string substring = f.Entry;
                FormatKeyStyleDictionary(substring);

                if (!styleDictionary.ContainsKey(r.Name + substring))
                {
                    styleDictionary.Add(r.Name + substring, "Regular");
                    //EntryLengthDictionary.Add(r.Name + substring, f.Entry.Length);
                }

                PasLayoutToe();

                textBox1.Clear();
            }
        }

        private void buttonClearRichTB_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            this.Refresh();
        }

        private void btnIncreaseFont_Click(object sender, EventArgs e)
        {
            richTextBox1.Font = new System.Drawing.Font(
                richTextBox1.Font.FontFamily,
                richTextBox1.Font.Size + 1,
                richTextBox1.Font.Style,
                richTextBox1.Font.Unit);
        }

        private void btnDecreaseFont_Click(object sender, EventArgs e)
        {
            richTextBox1.Font = new System.Drawing.Font(
                richTextBox1.Font.FontFamily,
                richTextBox1.Font.Size - 1,
                richTextBox1.Font.Style,
                richTextBox1.Font.Unit);
        }


        #endregion

        #region Helper Code om app verder te ontwikkelen
        // event dat ??
        private void tsmiPrintColors_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in panel1.Controls.OfType<Button>())
            {
                sb.Append($"item.BackColor = Color.FromArgb({item.BackColor.R.ToString()}, {item.BackColor.G.ToString()}, {item.BackColor.B.ToString()}); ");
                sb.Append(Environment.NewLine);
            }
            foreach (var item in panel1.Controls.OfType<Button>())
            {
                sb.Append($"item.ForeColor = Color.FromArgb({item.ForeColor.R.ToString()}, {item.ForeColor.G.ToString()}, {item.ForeColor.B.ToString()}); ");
                sb.Append(Environment.NewLine);
            }

            sb.Append($"panel1.BackColor = Color.FromArgb({panel1.BackColor.R.ToString()}, {panel1.BackColor.G.ToString()}, {panel1.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            sb.Append($"menuStrip1.BackColor = Color.FromArgb({menuStrip1.BackColor.R.ToString()}, {menuStrip1.BackColor.G.ToString()}, {menuStrip1.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            sb.Append($"dataGridView1.BackColor = Color.FromArgb({dataGridView1.BackColor.R.ToString()}, {dataGridView1.BackColor.G.ToString()}, {dataGridView1.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            sb.Append($"richTextBox1.BackColor = Color.FromArgb({richTextBox1.BackColor.R.ToString()}, {richTextBox1.BackColor.G.ToString()}, {richTextBox1.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            sb.Append($"panelBottom.BackColor = Color.FromArgb({panelBottom.BackColor.R.ToString()}, {panelBottom.BackColor.G.ToString()}, {panelBottom.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            sb.Append($"textBox1.BackColor = Color.FromArgb({textBox1.BackColor.R.ToString()}, {textBox1.BackColor.G.ToString()}, {textBox1.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            sb.Append($"this.BackColor = Color.FromArgb({this.BackColor.R.ToString()}, {this.BackColor.G.ToString()}, {this.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            sb.Append($"menutextBox2.BackColor = Color.FromArgb({menutextBox2.BackColor.R.ToString()}, {menutextBox2.BackColor.G.ToString()}, {menutextBox2.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            sb.Append($"menucomboBox1.BackColor = Color.FromArgb({menucomboBox1.BackColor.R.ToString()}, {menucomboBox1.BackColor.G.ToString()}, {menucomboBox1.BackColor.B.ToString()});");
            sb.Append(Environment.NewLine);

            richTextBox1.Text = sb.ToString();
        }
        #endregion

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

    public class Record
    {
        public Record() { }
        public string Name { get; set; }
        public BindingList<Veld> Velden = new BindingList<Veld>();
    }

    public class Veld
    {
        public string Entry { get; set; }
    }
}
