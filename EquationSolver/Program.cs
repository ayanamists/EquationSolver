using System;
using System.Collections.Generic;

namespace EquationSolver
{
    class LanguageTreeNode
    {
        public Object value { get; set; }
        public LanguageTreeNode left = null;
        public LanguageTreeNode right = null;

        public LanguageTreeNode(Object value, LanguageTreeNode left, LanguageTreeNode right)
        {
            this.value = value;
            this.left = left;
            this.right = right;
        }
    }
    class ParserHelper
    {
    }
    public class Binding
    {
        const string ErrMessage = "Binding: ";
        private Dictionary<string, double> bind = new Dictionary<string, double>();
        private string magic = "x";
        public string GetMagic() { return magic; }
        public void SetMagic(string what) { magic = what; }
        public Binding() { }
        public Binding(string target)
        {
            Parse(target);
        }
        internal void Parse(string target)
        {
            if(target.Length == 0)
            {
                return;
            }
            int begin = 0;
            for(int i = 0; i < target.Length ;++i)
            {
                if(target[i] == ',')
                {
                    Parse(target.Substring(begin, i - begin));
                    begin = i + 1;
                }
            }

            target = target.Substring(begin, target.Length - begin);
            var sb = new System.Text.StringBuilder();
            foreach(var i in target)
            {
                if((i >= 'A' && i <= 'Z') || (i >= 'a' && i <= 'z') || (i >= '0' && i <= '9') || (i == '='))
                {
                    sb.Append(i);
                }
            }
            var formattedTarget = sb.ToString();
            var positionOfEqu = formattedTarget.IndexOf('=');
            if(positionOfEqu < 0)
            {
                throw new FormatException($"{ErrMessage}format of binding string invaild!");
            }
            var symbol = formattedTarget.Substring(0, positionOfEqu);
            var value = Convert.ToDouble(formattedTarget.Substring(
                positionOfEqu + 1, formattedTarget.Length - positionOfEqu - 1));

            SetBind(symbol, value);
        }
        public void SetBind(string symbol, double value)
        {
            if (bind.ContainsKey(symbol)) {
                bind.Remove(symbol);
            }
            bind.Add(symbol, value);
        }
        public double GetBind(string symbol)
        {
            return bind.GetValueOrDefault(symbol);
        }
        public bool Contain(string symbol)
        {
            return bind.ContainsKey(symbol);
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            foreach(var i in bind)
            {
                sb.Append($"{i.Key}: {i.Value}\n");
            }
            return sb.ToString();
        }
    }
    class SolverLanguageTree
    {
        private LanguageTreeNode root { get; set; }
        public Binding Bind { get; set; } = new Binding();
        const string ErrMessage = "Solver Language Tree Error : ";

        public SolverLanguageTree() { }
        public SolverLanguageTree(string str)
        {
            root = LanguageParser.Parse(str);
        }
        public SolverLanguageTree(object value, LanguageTreeNode left, LanguageTreeNode right)
        {
            root = new LanguageTreeNode(value, left, right);
        }
        static public SolverLanguageTree operator+ (SolverLanguageTree a, SolverLanguageTree b){
            return new SolverLanguageTree('+', a.root, b.root);
        }
        
        static public SolverLanguageTree operator- (SolverLanguageTree a, SolverLanguageTree b)
        {
            return new SolverLanguageTree('-', a.root, b.root);
        }

        static public SolverLanguageTree operator*(SolverLanguageTree a, SolverLanguageTree b)
        {
            return new SolverLanguageTree('*', a.root, b.root);
        }

        static public SolverLanguageTree operator/(SolverLanguageTree a, SolverLanguageTree b)
        {
            return new SolverLanguageTree('/', a.root, b.root);
        }
        public double Eval(Binding bind)
        {
            Bind = bind;
            LanguageTreeNode tempNode = root;
            return _Eval(tempNode);
        }
        /*
         * We need to set the number value to double type before we eval the tree
         */
        private double _Eval(LanguageTreeNode root)
        {
            if(root == null)
            {
                return 0;
            }
            else
            {
                if(root.value.GetType() == typeof(double))
                {
                    return (double)root.value;
                }
                else if(root.value.GetType() == typeof(char))
                {
                    switch (root.value)
                    {
                        case '+':
                            return _Eval(root.left) + _Eval(root.right);
                        case '*':
                            return _Eval(root.left) * _Eval(root.right);
                        case '-':
                            return _Eval(root.left) - _Eval(root.right);
                        case '/':
                            return _Eval(root.left) / _Eval(root.right);
                        case '^':
                            {
                                // a ^ b
                                var a = _Eval(root.left);
                                var b = _Eval(root.right);
                                return Math.Pow(a, b);
                            }
                        case '=':
                            return Convert.ToDouble(_Eval(root.left) == _Eval(root.right));
                        case '@':
                            {
                                /*
                                 * df/dx
                                 */
                                const double deltaX = 0.000000001;
                                Binding xAndDeltaX = new Binding();
                                xAndDeltaX.SetBind(Bind.GetMagic(), Bind.GetBind(Bind.GetMagic()) + deltaX);
                                var f_x = _Eval(root.left);
                                var temp = Bind;
                                Bind = xAndDeltaX;
                                var f_xAndDeltaX = _Eval(root.left);
                                Bind = temp;
                                return (f_xAndDeltaX - f_x) / deltaX;
                            }
                        default:
                            throw new FormatException($"{ErrMessage}unknown operator");
                    }
                }
                else if(root.value.GetType() == typeof(string))
                {
                    string valueOfRoot = (string)root.value;
                    if (Bind.Contain(valueOfRoot))
                    {
                        return Bind.GetBind(valueOfRoot);
                    }
                    else
                    {
                        throw new FormatException($"{ErrMessage}unbind symbol : {valueOfRoot}");
                    }
                }
                else
                {
                    throw new FormatException($"{ErrMessage}unknown value type : {root.value}");
                }
            }
        }
        public SolverLanguageTree Der()
        {
            return new SolverLanguageTree('@', root, null);
        }
        public object GetRootValue()
        {
            return root.value;
        }
        public void SetRootValue(object value)
        {
            root.value = value;
        }
    }
    
    class LanguageParser
    {
        const string ErrMessage = "Parser: ";

        // Operator means can be used as operator and get value
        static List<char> Operator = new List<char>{ '+', '-', '*', '/', '^', '=' };
        
        // if a Symbol is in front of '-', this '-' is negative symbol
        static List<char> Symbol = new List<char> { '+', '-', '*', '/', '(', '^', '='};
        static List<char> Order = new List<char> { '=', '+', '-', '*', '/', '^', 'n' };
        static bool CheckOperator(char what)
        {
            return Operator.Contains(what);
        }
        public static LanguageTreeNode Parse(string target)
        {
            if (target.Length == 0)
            {
                return null;
            }

            Dictionary<char, int> symbolTable = new Dictionary<char, int>();
            Stack<int> stack = new Stack<int>();
            int i = target.Length - 1;
            for (; i >= 0; --i)
            {
                if (target[i] == ')')
                {
                    stack.Push(i);
                }
                else if (target[i] == '(')
                {
                    var next = 0;
                    try
                    {
                        next = stack.Pop();
                    }
                    catch (InvalidOperationException)
                    {
                        throw new FormatException($"{ErrMessage}Paren unmatch");
                    }
                    if (i == 0 && next == target.Length - 1)
                    {
                        return Parse(target.Substring(1, target.Length - 2));
                    }
                }
                else if (stack.Count == 0 && Operator.Contains(target[i]))
                {
                    if (target[i] == '-' && (i == 0 || Symbol.Contains(target[i - 1])))
                    {
                        // 'n' represents 'negative'
                        symbolTable.Add('n', i); 
                    }
                    else
                    {
                        if (!symbolTable.ContainsKey(target[i]))
                        {
                            symbolTable.Add(target[i], i);
                        }
                    }
                }
            }

            i = -1;
            foreach(var sym in Order)
            {
                if (symbolTable.ContainsKey(sym))
                {
                    i = symbolTable[sym];
                    break;
                }
            }
            if (i != -1)
            {
                return new LanguageTreeNode(target[i],
                        Parse(target.Substring(0, i)),
                        Parse(target.Substring(i + 1, target.Length - i - 1)));
            }

            bool isWord = true;
            foreach (var letter in target)
            {
                if (!((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')))
                {
                    isWord = false;
                    break;
                }
            }
            if (isWord)
            {
                return new LanguageTreeNode(target, null, null);
            }

            return new LanguageTreeNode(Convert.ToDouble(target), null, null);
        }
    }

    public class Solver
    {
        private int count = 0;
        const int MaxIteration = 5000;
        const string ErrMessage = "Solver: ";
        SolverLanguageTree tree;
        private string Target { get; set; }
        public Solver(string target)
        {
            Target = target;
            try
            {
                tree = new SolverLanguageTree(target);
            }
            catch (FormatException e)
            {
                Console.WriteLine($"Format error:{e.Message}\n");
            }
            
        }
        public double Eval(Binding bind)
        {
            return tree.Eval(bind);
        }

        public Binding EquationSolver(string target, Binding initBinding)
        {
            tree.Bind.SetMagic(target);
            if((char)tree.GetRootValue() != '=')
            {
                throw new FormatException($"{ErrMessage}Not a equation!\n");
            }
            tree.SetRootValue('-');
            var binding = tree.Bind;
            binding.SetBind(binding.GetMagic(), initBinding.GetBind(target));
            return FindFixPoint(new SolverLanguageTree(target) - tree / tree.Der(), binding);
        }
        
        internal Binding FindFixPoint(SolverLanguageTree tree, Binding binding)
        {
            int count = 0;
            const double win = 0.0000001;
            while(Math.Abs(tree.Eval(binding) - binding.GetBind(binding.GetMagic())) > win)
            {
                binding.SetBind(binding.GetMagic() ,tree.Eval(binding));
                count++;
                if(count > MaxIteration)
                {
                    return null;
                }
            }
            return binding;
        }

    }

    class Program
    {
        static void Main()
        {
            Console.WriteLine("Please enter an equation");
            var str = Console.ReadLine();
            var solver = new Solver(str);
            Console.WriteLine("Please enter an initial value (such as x = 1, y = 2)");
            str = Console.ReadLine();
            var binding = new Binding(str);
            Console.WriteLine("Please enter the symbol for solve (such as x)");
            str = Console.ReadLine();

            Console.WriteLine(solver.EquationSolver(str, binding));
        }
    }
}
