using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using SimpleCompiler.Compiler;

namespace SimpleCompiler
{
    public partial class Form1 : Form
    {
        private RichTextBox codeEditor;
        private PictureBox lineNumbersPic; 
        private DataGridView tokenTable;
        private DataGridView symbolTableView;
        private TreeView astTreeView;
        private ListBox errorListBox;
        private ListBox outputConsole; 
        private Button btnCompile;
        private Button btnLoadFile;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Two-Pass Compiler - Professional IDE";
            this.Size = new Size(1200, 800); 
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // --- 1. ANA BÖLÜCÜ (Sol: Kod ve Hatalar | Sağ: Analiz ve Konsol) ---
            // Sürüklenebilir bar (SplitterWidth = 5) eklendi
            SplitContainer mainSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 450, SplitterWidth = 5, BackColor = Color.LightGray };
            this.Controls.Add(mainSplit);

            // Arka planı düzeltmek için panellerin rengini beyaz yapıyoruz
            mainSplit.Panel1.BackColor = SystemColors.Control;
            mainSplit.Panel2.BackColor = SystemColors.Control;

            // --- SOL BÖLGE (KOD VE HATALAR) ---
            SplitContainer leftSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 550, SplitterWidth = 5, BackColor = Color.LightGray };
            leftSplit.Panel1.BackColor = SystemColors.Control;
            leftSplit.Panel2.BackColor = SystemColors.Control;
            mainSplit.Panel1.Controls.Add(leftSplit);
            
            // Sol Üst: Kod Alanı
            Panel codePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            Label lblCode = new Label { Text = "Source Code", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lineNumbersPic = new PictureBox { Dock = DockStyle.Left, Width = 45, BackColor = Color.FromArgb(240, 240, 240) };
            lineNumbersPic.Paint += LineNumbersPic_Paint; 
            
            codeEditor = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 12), BorderStyle = BorderStyle.None, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both, MaxLength = int.MaxValue };
            codeEditor.AcceptsTab = true; 
            codeEditor.TextChanged += CodeEditor_TextChanged; 
            codeEditor.VScroll += (s, e) => lineNumbersPic.Invalidate(); 
            codeEditor.HScroll += (s, e) => lineNumbersPic.Invalidate(); 
            codeEditor.Resize += (s, e) => lineNumbersPic.Invalidate();
            
            codePanel.Controls.Add(codeEditor);
            codePanel.Controls.Add(lineNumbersPic);
            codePanel.Controls.Add(lblCode);
            leftSplit.Panel1.Controls.Add(codePanel);

            // Sol Alt: Hatalar ve Butonlar
            Panel errorPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            Label lblErrors = new Label { Text = "Error Console", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            errorListBox = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
            
            Panel buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(0, 10, 0, 0) };
            btnLoadFile = new Button { Text = "Load File", Width = 150, Dock = DockStyle.Left, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnLoadFile.Click += BtnLoadFile_Click;
            btnCompile = new Button { Text = "Run Compiler", Width = 150, Dock = DockStyle.Right, Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.LightGreen };
            btnCompile.Click += BtnCompile_Click;
            buttonPanel.Controls.Add(btnLoadFile);
            buttonPanel.Controls.Add(btnCompile);

            errorPanel.Controls.Add(errorListBox);
            errorPanel.Controls.Add(buttonPanel);
            errorPanel.Controls.Add(lblErrors);
            leftSplit.Panel2.Controls.Add(errorPanel);

            // --- SAĞ BÖLGE (TABLOLAR, AST VE KONSOL) ---
            SplitContainer rightSplitMain = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 250, SplitterWidth = 5, BackColor = Color.LightGray };
            rightSplitMain.Panel1.BackColor = SystemColors.Control;
            rightSplitMain.Panel2.BackColor = SystemColors.Control;
            mainSplit.Panel2.Controls.Add(rightSplitMain);

            // Sağ Üst: Token ve Sembol Tablosunu bölen dikey alan
            SplitContainer tablesSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 350, SplitterWidth = 5, BackColor = Color.LightGray };
            tablesSplit.Panel1.BackColor = SystemColors.Control;
            tablesSplit.Panel2.BackColor = SystemColors.Control;
            rightSplitMain.Panel1.Controls.Add(tablesSplit);

            // Token Tablosu
            Panel tokenPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            Label lblTokens = new Label { Text = "Token Stream", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            tokenTable = new DataGridView { Dock = DockStyle.Fill, ColumnCount = 3, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
            tokenTable.Columns[0].Name = "Line"; tokenTable.Columns[0].Width = 40;
            tokenTable.Columns[1].Name = "Token"; tokenTable.Columns[1].Width = 100;
            tokenTable.Columns[2].Name = "Type"; tokenTable.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            tokenPanel.Controls.Add(tokenTable);
            tokenPanel.Controls.Add(lblTokens);
            tablesSplit.Panel1.Controls.Add(tokenPanel);

            // Sembol Tablosu
            Panel symbolPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            Label lblSymbol = new Label { Text = "Symbol Table", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            symbolTableView = new DataGridView { Dock = DockStyle.Fill, ColumnCount = 3, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
            symbolTableView.Columns[0].Name = "Name"; symbolTableView.Columns[0].Width = 100;
            symbolTableView.Columns[1].Name = "Type"; symbolTableView.Columns[1].Width = 80;
            symbolTableView.Columns[2].Name = "Memory"; symbolTableView.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            symbolPanel.Controls.Add(symbolTableView);
            symbolPanel.Controls.Add(lblSymbol);
            tablesSplit.Panel2.Controls.Add(symbolPanel);

            // Sağ Alt: AST ve Konsolu bölen yatay alan
            SplitContainer astConsoleSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 250, SplitterWidth = 5, BackColor = Color.LightGray };
            astConsoleSplit.Panel1.BackColor = SystemColors.Control;
            astConsoleSplit.Panel2.BackColor = SystemColors.Control;
            rightSplitMain.Panel2.Controls.Add(astConsoleSplit);

            // AST Ağacı
            Panel astPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            Label lblAST = new Label { Text = "Abstract Syntax Tree (AST)", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            astTreeView = new TreeView { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
            astPanel.Controls.Add(astTreeView);
            astPanel.Controls.Add(lblAST);
            astConsoleSplit.Panel1.Controls.Add(astPanel);

            // Çıktı Konsolu
            Panel consolePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            Label lblConsole = new Label { Text = "Execution Output (Console)", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            outputConsole = new ListBox { Dock = DockStyle.Fill, BackColor = Color.Black, ForeColor = Color.Lime, Font = new Font("Consolas", 12, FontStyle.Bold), BorderStyle = BorderStyle.None };
            consolePanel.Controls.Add(outputConsole);
            consolePanel.Controls.Add(lblConsole);
            astConsoleSplit.Panel2.Controls.Add(consolePanel);
        }

        // --- SATIR NUMARALARI ÇİZİMİ ---
        private void LineNumbersPic_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            StringFormat format = new StringFormat() { Alignment = StringAlignment.Far };

            if (string.IsNullOrEmpty(codeEditor.Text))
            {
                e.Graphics.DrawString("1", codeEditor.Font, Brushes.DimGray, new RectangleF(0, 2, lineNumbersPic.Width - 5, 20), format);
                return;
            }

            int firstIndex = codeEditor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = codeEditor.GetLineFromCharIndex(firstIndex);
            int lastIndex = codeEditor.GetCharIndexFromPosition(new Point(0, codeEditor.ClientSize.Height));
            int lastLine = codeEditor.GetLineFromCharIndex(lastIndex);

            for (int i = firstLine; i <= lastLine + 1; i++)
            {
                if (i >= codeEditor.Lines.Length) break;
                int charIndex = codeEditor.GetFirstCharIndexFromLine(i);
                Point p = codeEditor.GetPositionFromCharIndex(charIndex);
                e.Graphics.DrawString((i + 1).ToString(), codeEditor.Font, Brushes.DimGray, new RectangleF(0, p.Y + 2, lineNumbersPic.Width - 5, 20), format);
            }
        }

        // --- DOSYA YÜKLEME ---
        private void BtnLoadFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Title = "Kaynak Kod Dosyası Seç", Filter = "Text Files (*.txt)|*.txt", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) })
            {
                if (ofd.ShowDialog() == DialogResult.OK) { codeEditor.Text = File.ReadAllText(ofd.FileName); }
            }
        }

        // --- SÖZDİZİMİ RENKLENDİRME ---
        private void CodeEditor_TextChanged(object sender, EventArgs e)
        {
            if (codeEditor.Text.Length > 50000) { lineNumbersPic.Invalidate(); return; }

            int originalIndex = codeEditor.SelectionStart;
            int originalLength = codeEditor.SelectionLength;
            Color originalColor = Color.Black;

            codeEditor.TextChanged -= CodeEditor_TextChanged;
            codeEditor.SelectionStart = 0; 
            codeEditor.SelectionLength = codeEditor.Text.Length;
            codeEditor.SelectionColor = Color.Black;

            HighlightRegex(@"\b(int|float|string|bool|if|else|while|for|switch|case|default|print|true|false)\b", Color.Blue);
            HighlightRegex("\".*?\"", Color.DarkOrange);
            HighlightRegex(@"\b\d+(\.\d+)?\b", Color.Purple);
            HighlightRegex(@"//.*", Color.Green); 

            codeEditor.SelectionStart = originalIndex;
            codeEditor.SelectionLength = originalLength;
            codeEditor.SelectionColor = originalColor;

            codeEditor.TextChanged += CodeEditor_TextChanged;
            lineNumbersPic.Invalidate(); 
        }

        private void HighlightRegex(string pattern, Color color)
        {
            foreach (Match match in new Regex(pattern).Matches(codeEditor.Text))
            {
                codeEditor.SelectionStart = match.Index; 
                codeEditor.SelectionLength = match.Length; 
                codeEditor.SelectionColor = color;
            }
        }

        // --- DERLEYİCİ VE YORUMLAYICI ---
        private void BtnCompile_Click(object sender, EventArgs e)
        {
            string sourceCode = codeEditor.Text;
            if (string.IsNullOrWhiteSpace(sourceCode)) return;

            tokenTable.Rows.Clear();
            symbolTableView.Rows.Clear();
            astTreeView.Nodes.Clear();
            errorListBox.Items.Clear();
            outputConsole.Items.Clear(); 
            errorListBox.ForeColor = Color.Red;

            try
            {
                Lexer lexer = new Lexer(sourceCode);
                List<Token> tokens = lexer.Tokenize();
                foreach (var t in tokens) tokenTable.Rows.Add(t.Line, t.Value, t.Type.ToString());

                SymbolTable symbolTable = new SymbolTable();
                Parser parser = new Parser(tokens, symbolTable);
                ASTNode root = parser.Parse();

                foreach (var sym in symbolTable.GetAllSymbols())
                {
                    symbolTableView.Rows.Add(sym.Name, sym.Type, sym.MemoryLocation);
                }

                TreeNode uiRoot = new TreeNode(root.Name);
                astTreeView.Nodes.Add(uiRoot);
                PopulateTreeView(root, uiRoot);
                astTreeView.ExpandAll();

                List<string> errors = parser.GetErrors();
                if (errors.Count > 0)
                {
                    foreach (var err in errors) errorListBox.Items.Add(err);
                    outputConsole.Items.Add("! Derleme hatalı. Kod çalıştırılamadı.");
                    outputConsole.ForeColor = Color.Red;
                }
                else
                {
                    errorListBox.ForeColor = Color.Green;
                    errorListBox.Items.Add("Derleme başarılı! Lexical, Syntax veya Semantic hata bulunamadı.");
                    outputConsole.ForeColor = Color.Lime;
                    outputConsole.Items.Add("--- PROGRAM BAŞLADI ---");
                    
                    Interpreter interpreter = new Interpreter();
                    interpreter.Interpret(root);
                    
                    foreach(var output in interpreter.ConsoleOutput)
                    {
                        outputConsole.Items.Add(output);
                    }
                    
                    outputConsole.Items.Add("--- PROGRAM BİTTİ ---");
                }
            }
            catch (Exception ex)
            {
                errorListBox.Items.Add("Beklenmeyen Sistem Hatası: " + ex.Message);
            }
        }

        private void PopulateTreeView(ASTNode astNode, TreeNode treeNode)
        {
            foreach (var child in astNode.Children)
            {
                string nodeText = child.ValueType != "unknown" ? $"{child.Name} ({child.ValueType})" : child.Name;
                TreeNode childNode = new TreeNode(nodeText);
                treeNode.Nodes.Add(childNode);
                PopulateTreeView(child, childNode); 
            }
        }
    }
}