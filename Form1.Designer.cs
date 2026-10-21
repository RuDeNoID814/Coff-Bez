namespace BezoutCoefficients;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    private NumericUpDown numA = null!;
    private NumericUpDown numB = null!;
    private Button btnCompute = null!;
    private Label lblResult = null!;

    private NumericUpDown numMaxN = null!;
    private NumericUpDown numStep = null!;
    private NumericUpDown numRuns = null!;
    private Button btnRunExperiment = null!;
    private ProgressBar progressBar1 = null!;
    private Panel panelChart = null!;
    private Label lblApprox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 700);
        Text = "Коэффициенты Безу — рекурсивный расширенный алгоритм Евклида";
        MinimumSize = new Size(820, 600);

        var lblTitle = new Label
        {
            Text = "Коэффициенты Безу: a·x + b·y = НОД(a, b)",
            Location = new Point(20, 15),
            Size = new Size(600, 24),
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        var lblA = new Label { Text = "a:", Location = new Point(20, 55), Size = new Size(20, 23) };
        numA = new NumericUpDown
        {
            Location = new Point(45, 53),
            Size = new Size(140, 23),
            Minimum = 1,
            Maximum = 2_000_000_000,
            Value = 240
        };

        var lblB = new Label { Text = "b:", Location = new Point(200, 55), Size = new Size(20, 23) };
        numB = new NumericUpDown
        {
            Location = new Point(225, 53),
            Size = new Size(140, 23),
            Minimum = 1,
            Maximum = 2_000_000_000,
            Value = 46
        };

        btnCompute = new Button
        {
            Text = "Вычислить",
            Location = new Point(380, 52),
            Size = new Size(120, 26)
        };
        btnCompute.Click += BtnCompute_Click;

        lblResult = new Label
        {
            Location = new Point(20, 90),
            Size = new Size(840, 70),
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.FixedSingle,
            Text = "Введите a и b, нажмите «Вычислить»."
        };

        var lblExpTitle = new Label
        {
            Text = "Замер времени (как в лабе 1): случайные a, b в диапазоне [1, n], реальные миллисекунды",
            Location = new Point(20, 175),
            Size = new Size(820, 20),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var lblMaxN = new Label { Text = "Макс. n:", Location = new Point(20, 207), Size = new Size(65, 23), TextAlign = ContentAlignment.MiddleLeft };
        numMaxN = new NumericUpDown
        {
            Location = new Point(90, 203),
            Size = new Size(170, 23),
            Minimum = 1000,
            Maximum = 9_000_000_000_000_000_000m,
            Value = 1_000_000_000_000_000_000m,
            Increment = 1
        };

        var lblStep = new Label { Text = "Точек:", Location = new Point(275, 207), Size = new Size(50, 23), TextAlign = ContentAlignment.MiddleLeft };
        numStep = new NumericUpDown
        {
            Location = new Point(330, 203),
            Size = new Size(70, 23),
            Minimum = 5,
            Maximum = 100,
            Value = 25,
            Increment = 1
        };

        var lblRuns = new Label { Text = "Серий (мин.):", Location = new Point(410, 207), Size = new Size(95, 23), TextAlign = ContentAlignment.MiddleLeft };
        numRuns = new NumericUpDown
        {
            Location = new Point(510, 203),
            Size = new Size(70, 23),
            Minimum = 1,
            Maximum = 50,
            Value = 15
        };

        btnRunExperiment = new Button
        {
            Text = "Запустить эксперимент",
            Location = new Point(600, 202),
            Size = new Size(200, 28),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        btnRunExperiment.Click += BtnRunExperiment_Click;

        progressBar1 = new ProgressBar
        {
            Location = new Point(20, 235),
            Size = new Size(840, 20)
        };

        panelChart = new Panel
        {
            Location = new Point(20, 265),
            Size = new Size(840, 360),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        panelChart.Paint += PanelChart_Paint;

        lblApprox = new Label
        {
            Location = new Point(20, 635),
            Size = new Size(840, 40),
            Font = new Font("Consolas", 9)
        };

        Controls.AddRange(new Control[]
        {
            lblTitle, lblA, numA, lblB, numB, btnCompute, lblResult,
            lblExpTitle, lblMaxN, numMaxN, lblStep, numStep, lblRuns, numRuns,
            btnRunExperiment, progressBar1, panelChart, lblApprox
        });
    }
}
