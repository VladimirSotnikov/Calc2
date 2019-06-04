using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Stateless;

namespace Calc2
{
    public partial class Form1 : Form
    {
        public enum States
        {
            StartFirstOperand,
            EnterFirstOperand,
            StartSecondOperand,
            EnterSecondOperand,
            ShowResult,
        }

        public enum Triggers
        {
            InputNumber,
            InputDot,
            InputOperator,
            Calculate,
            Clear,
            Backspace,
            GoStartOperand,
            GoEnterOperand,
            GoShowResult,
        }

        private StateMachine<States, Triggers> Machine { get; }
        private StateMachine<States, Triggers>.TriggerWithParameters<char> InputNumberChar { get; }
        private StateMachine<States, Triggers>.TriggerWithParameters<char> InputOperatorChar { get; }

        private States _state = States.StartFirstOperand;

        private States State
        {
            get => _state;
            set
            {
                _state = value;
                UpdateInput();
            }
        }

        private string _firstOperand = "0";

        private string FirstOperand
        {
            get => _firstOperand;
            set
            {
                _firstOperand = value;
                UpdateInput();
            }
        }

        private string _secondOperand;

        private string SecondOperand
        {
            get => _secondOperand;
            set
            {
                _secondOperand = value;
                UpdateInput();
            }
        }

        private string _operator;

        private string Operator
        {
            get => _operator;
            set
            {
                _operator = value;
                UpdateInput();
            }
        }

        private string _result;
        
        private string Result
        {
            get => _result;
            set
            {
                _result = value;
                UpdateInput();
            }
        }

        public Form1()
        {
            InitializeComponent();

            Machine = new StateMachine<States, Triggers>(
                () => State,
                s => State = s);

            InputNumberChar = Machine.SetTriggerParameters<char>(Triggers.InputNumber);
            InputOperatorChar = Machine.SetTriggerParameters<char>(Triggers.InputOperator);

            Machine.Configure(States.StartFirstOperand)
                .PermitReentry(Triggers.InputOperator)
                .PermitReentry(Triggers.InputNumber)
                .PermitReentry(Triggers.Backspace)
                .PermitReentry(Triggers.InputDot)
                .PermitReentry(Triggers.Clear)
                .Permit(Triggers.GoEnterOperand, States.EnterFirstOperand)
                .Permit(Triggers.GoStartOperand, States.StartSecondOperand)
                .OnEntryFrom(InputNumberChar, symbol =>
                {
                    if (symbol == '0') return;

                    if (symbol > '0' && symbol <= '9')
                    {
                        FirstOperand = FirstOperand.StartsWith("-") ? $"-{symbol}" : symbol.ToString();
                        Machine.Fire(Triggers.GoEnterOperand);
                    }
                })
                .OnEntryFrom(InputOperatorChar, symbol =>
                {
                    switch (symbol)
                    {
                        case '+':
                            if (FirstOperand.StartsWith("-")) FirstOperand = "0";
                            break;
                        case '-':
                            if (!FirstOperand.StartsWith("-")) FirstOperand = "-0";
                            break;
                        case '±':
                            FirstOperand = FirstOperand.StartsWith("-") ? "0" : "-0";
                            break;
                        default:
                            FirstOperand = "0";
                            Operator = symbol.ToString();
                            Machine.Fire(Triggers.GoStartOperand);
                            break;
                    }
                })
                .OnEntryFrom(Triggers.InputDot, () =>
                {
                    FirstOperand += ".";
                    Machine.Fire(Triggers.GoEnterOperand);
                })
                .OnEntryFrom(Triggers.Backspace, () =>
                {
                    FirstOperand = FirstOperand.Remove(0, 1);
                    if (string.IsNullOrEmpty(FirstOperand))
                    {
                        FirstOperand = "0";
                    }
                })
                .OnEntryFrom(Triggers.Clear, () =>
                {
                    FirstOperand = "0";
                    SecondOperand = "";
                    Operator = "";
                    Result = "";
                });

            Machine.Configure(States.EnterFirstOperand)
                .PermitReentry(Triggers.InputNumber)
                .PermitReentry(Triggers.InputOperator)
                .PermitReentry(Triggers.InputDot)
                .PermitReentry(Triggers.Backspace)
                .Permit(Triggers.GoStartOperand, States.StartFirstOperand)
                .Permit(Triggers.GoEnterOperand, States.StartSecondOperand)
                .Permit(Triggers.Clear, States.StartFirstOperand)
                .OnEntryFrom(InputNumberChar, symbol =>
                {
                    if (symbol == '0')
                    {
                        if (FirstOperand != "-0" && FirstOperand != "0")
                        {
                            FirstOperand += symbol;
                        }
                    }
                    else if (symbol > '0' && symbol <= '9')
                    {
                        FirstOperand += symbol;
                    }
                })
                .OnEntryFrom(InputOperatorChar, symbol =>
                {
                    if (symbol == '±')
                    {
                        FirstOperand = FirstOperand.StartsWith("-")
                            ? FirstOperand.Remove(0, 1)
                            : FirstOperand.Insert(0, "-");
                    }
                    else
                    {
                        Operator = symbol.ToString();
                        Machine.Fire(Triggers.GoEnterOperand);
                    }
                })
                .OnEntryFrom(Triggers.InputDot, () =>
                {
                    if (!FirstOperand.Contains(".")) FirstOperand += ".";
                })
                .OnEntryFrom(Triggers.Backspace, () =>
                {
                    FirstOperand = FirstOperand.Remove(FirstOperand.Length - 1, 1);
                    if (string.IsNullOrEmpty(FirstOperand))
                    {
                        FirstOperand = "0";
                        Machine.Fire(Triggers.GoStartOperand);
                    }
                })
                .OnEntryFrom(Triggers.Clear, () =>
                {
                    Machine.Fire(Triggers.Clear);
                });

            Machine.Configure(States.StartSecondOperand)
                .PermitReentry(Triggers.InputDot)
                .PermitReentry(Triggers.InputOperator)
                .PermitReentry(Triggers.InputNumber)
                .Permit(Triggers.GoEnterOperand, States.EnterSecondOperand)
                .Permit(Triggers.Clear, States.StartFirstOperand)
                .Ignore(Triggers.Backspace)
                .OnEntryFrom(InputNumberChar, symbol =>
                {
                    SecondOperand += symbol;
                    Machine.Fire(Triggers.GoEnterOperand);
                })
                .OnEntryFrom(InputOperatorChar, symbol =>
                {
                    if (symbol == '±')
                    {
                        return;
                    }

                    Operator = symbol.ToString();
                })
                .OnEntryFrom(Triggers.InputDot, () =>
                {
                    SecondOperand += "0.";
                    Machine.Fire(Triggers.GoEnterOperand);
                })
                .OnEntryFrom(Triggers.Clear, () =>
                {
                    Machine.Fire(Triggers.Clear);
                });

            Machine.Configure(States.EnterSecondOperand)
                .PermitReentry(Triggers.InputDot)
                .PermitReentry(Triggers.Backspace)
                .PermitReentry(Triggers.InputNumber)
                .PermitReentry(Triggers.InputOperator)
                .Permit(Triggers.GoStartOperand, States.StartSecondOperand)
                .Permit(Triggers.Clear, States.StartFirstOperand)
                .OnEntryFrom(InputNumberChar, symbol =>
                {
                    if (symbol == '0')
                    {
                        if (SecondOperand != "0" && SecondOperand != "-0")
                        {
                            SecondOperand += symbol;
                        }
                    }
                    else if (symbol > '0' && symbol <= '9')
                    {
                        SecondOperand += symbol;
                    }
                })
                .OnEntryFrom(InputOperatorChar, symbol =>
                {
                    if (symbol == '±')
                    {
                        SecondOperand = SecondOperand.StartsWith("-")
                            ? SecondOperand.Remove(0, 1)
                            : SecondOperand.Insert(0, "-");
                    }
                    else
                    {
                        Calculate();
                        FirstOperand = Result;
                        SecondOperand = "";
                        Operator = symbol.ToString();
                        Machine.Fire(Triggers.GoStartOperand);
                    }
                })
                .OnEntryFrom(Triggers.Backspace, () =>
                {
                    SecondOperand = SecondOperand.Remove(SecondOperand.Length - 1, 1);
                    if (string.IsNullOrEmpty(SecondOperand))
                    {
                        Machine.Fire(Triggers.GoStartOperand);
                    }
                })
                .OnEntryFrom(Triggers.InputDot, () =>
                {
                    if (!SecondOperand.Contains(".")) SecondOperand += ".";
                })
                .OnEntryFrom(Triggers.Clear, () =>
                {
                    Machine.Fire(Triggers.Clear);
                });

            Machine.Configure(States.ShowResult)
                .PermitReentry(Triggers.InputDot)
                .PermitReentry(Triggers.InputNumber)
                .PermitReentry(Triggers.InputOperator)
                .Permit(Triggers.GoEnterOperand, States.StartSecondOperand)
                .Permit(Triggers.GoStartOperand, States.StartFirstOperand)
                .Permit(Triggers.Clear, States.StartFirstOperand)
                .Ignore(Triggers.Backspace)
                .OnEntryFrom(InputNumberChar, symbol =>
                {
                    FirstOperand = symbol.ToString();
                    Result = "";
                    Machine.Fire(Triggers.GoStartOperand);
                })
                .OnEntryFrom(Triggers.InputDot, () =>
                {
                    FirstOperand = "0.";
                    Result = "";
                    Machine.Fire(Triggers.GoStartOperand);
                })
                .OnEntryFrom(InputOperatorChar, symbol =>
                {
                    FirstOperand = Result;
                    Operator = symbol.ToString();
                    Result = "";
                    Machine.Fire(Triggers.GoEnterOperand);
                })
                .OnEntryFrom(Triggers.Clear, () =>
                {
                    Machine.Fire(Triggers.Clear);
                });

            UpdateInput();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private int Division(int a, int b)
        {
            if (b == 0) throw new ArgumentException("Деление на ноль невозможно!");

            return a / b;
        }

        private void Calculate()
        {
            var firstOperandNumber = double.Parse(FirstOperand, NumberStyles.Any, CultureInfo.InvariantCulture);
            var secondOperandNumber = double.Parse(SecondOperand, NumberStyles.Any, CultureInfo.InvariantCulture);
            switch (Operator)
            {
                case "+":
                    Result = $"{firstOperandNumber + secondOperandNumber}";
                    break;
                case "-":
                    Result = $"{firstOperandNumber - secondOperandNumber}";
                    break;
                case "*":
                    Result = $"{firstOperandNumber * secondOperandNumber}";
                    break;
                case "/":
                    Result = $"{firstOperandNumber / secondOperandNumber}";
                    break;


            }
        }

        private void UpdateInput()
        {
            switch (State)
            {
                case States.StartFirstOperand:
                case States.EnterFirstOperand:
                    textBoxInput.Text = FirstOperand;
                    break;
                case States.StartSecondOperand:
                case States.EnterSecondOperand:
                    textBoxInput.Text = $@"{FirstOperand} {Operator} {SecondOperand}";
                    break;
                case States.ShowResult:
                    textBoxInput.Text = Result;
                    break;
            }

            var debug = new StringBuilder();
            debug.AppendLine($"         State : {State}");
            debug.AppendLine($" First operand : {FirstOperand}");
            debug.AppendLine($"Second operand : {SecondOperand}");
            debug.AppendLine($"      Operator : {Operator}");
            debug.AppendLine($"        Result : {Result}");

            textBoxDebug.Text = debug.ToString();
        }

        
        private void Evaluate()
        {
            Calculate();
            FirstOperand = "";
            SecondOperand = "";
            Operator = "";
            State = States.ShowResult;
        }

        private void buttonSymbol_Click(object sender, EventArgs e)
        {
            //EnterSymbol(((Button)sender).Text[0]);
            var symbol = ((Button) sender).Text[0];
            if (symbol >= '0' && symbol <= '9')
            {
                Machine.Fire(InputNumberChar, symbol);
            }
            else if (symbol == '.')
            {
                Machine.Fire(Triggers.InputDot);
            }
            else if (symbol == '←')
            {
                Machine.Fire(Triggers.Backspace);
            }
            else
            {
                Machine.Fire(InputOperatorChar, symbol);
            }
        }

        private void buttonCalculate_Click(object sender, EventArgs e)
        {
            Evaluate();
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            Machine.Fire(Triggers.Clear);
        }
    }
}
