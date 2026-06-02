using System;
using System.Collections.Generic;
using System.Globalization;

namespace SimpleCompiler.Compiler
{
    public class Interpreter
    {
        // Çalışma zamanındaki değişkenleri ve değerlerini hafızada tutar
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();
        
        // Ekrana basılacak çıktıları (print) toplar
        public List<string> ConsoleOutput { get; private set; } = new List<string>();

        public void Interpret(ASTNode root)
        {
            if (root.Name == "Program")
            {
                foreach (var child in root.Children)
                {
                    Execute(child);
                }
            }
        }

        private void Execute(ASTNode node)
        {
            if (node == null) return;

            switch (node.Name)
            {
                case "Declaration":
                    string varName = node.Children[1].Name;
                    // Eğer tanımlama satırında atama da yapıldıysa (int x = 5; gibi)
                    if (node.Children.Count > 2 && node.Children[2].Name == "=")
                    {
                        _variables[varName] = Evaluate(node.Children[3]);
                    }
                    else
                    {
                        _variables[varName] = 0; // Varsayılan değer
                    }
                    break;

                case "Assignment":
                    string assignName = node.Children[0].Name;
                    _variables[assignName] = Evaluate(node.Children[2]);
                    break;

                case "Print":
                    object printVal = Evaluate(node.Children[0]);
                    ConsoleOutput.Add("> " + printVal?.ToString());
                    break;

                case "IfStatement":
                    ASTNode conditionNode = node.Children[0].Children[0];
                    ASTNode thenBranch = node.Children[1].Children[0];
                    
                    if (IsTruthy(Evaluate(conditionNode)))
                    {
                        Execute(thenBranch);
                    }
                    else if (node.Children.Count > 2) // Else bloğu varsa
                    {
                        Execute(node.Children[2].Children[0]);
                    }
                    break;

                case "WhileStatement":
                    ASTNode whileCond = node.Children[0].Children[0];
                    ASTNode body = node.Children[1].Children[0];
                    
                    int loopSecurity = 0; // Sonsuz döngüden programın çökmesini engellemek için
                    while (IsTruthy(Evaluate(whileCond)))
                    {
                        Execute(body);
                        loopSecurity++;
                        if (loopSecurity > 5000) 
                        {
                            ConsoleOutput.Add("! HATA: Sonsuz döngü tespit edildi ve durduruldu.");
                            break;
                        }
                    }
                    break;
                    case "SwitchStatement":
                    object switchValue = Evaluate(node.Children[0].Children[0]); // Kontrol edilen değer
                    bool isMatched = false;

                    // 1. İndeksten başla (Çünkü 0. indeks Condition'dır)
                    for (int i = 1; i < node.Children.Count; i++)
                    {
                        ASTNode currentCase = node.Children[i];

                        if (currentCase.Name == "CaseBlock" && !isMatched)
                        {
                            object caseMatchValue = Evaluate(currentCase.Children[0]);
                            
                            // Değerler eşleşiyorsa içeri gir
                            if (switchValue.ToString() == caseMatchValue.ToString())
                            {
                                isMatched = true;
                                // Case'in içindeki kodları çalıştır (0. indeks değer, o yüzden 1'den başlar)
                                for (int j = 1; j < currentCase.Children.Count; j++)
                                {
                                    Execute(currentCase.Children[j]);
                                }
                                break; // Modern dillerdeki gibi otomatik Break yapar (Fall-through yok)
                            }
                        }
                        else if (currentCase.Name == "DefaultBlock" && !isMatched)
                        {
                            // Hiçbiri eşleşmediyse default kodlarını çalıştır
                            for (int j = 0; j < currentCase.Children.Count; j++)
                            {
                                Execute(currentCase.Children[j]);
                            }
                        }
                    }
                    break;

                case "Block":
                    foreach (var stmt in node.Children)
                    {
                        Execute(stmt);
                    }
                    break;
            }
        }

        // Matematiksel işlemleri ve değişken okumalarını yapar
        private object Evaluate(ASTNode node)
        {
            if (node.Name == "Arithmetic" || node.Name == "Term")
            {
                float left = Convert.ToSingle(Evaluate(node.Children[0]), CultureInfo.InvariantCulture);
                string op = node.Children[1].Name;
                float right = Convert.ToSingle(Evaluate(node.Children[2]), CultureInfo.InvariantCulture);

                if (op == "+") return (node.ValueType == "int") ? (object)(int)(left + right) : (left + right);
                if (op == "-") return (node.ValueType == "int") ? (object)(int)(left - right) : (left - right);
                if (op == "*") return (node.ValueType == "int") ? (object)(int)(left * right) : (left * right);
                if (op == "/") return (node.ValueType == "int") ? (object)(int)(left / right) : (left / right);
            }
            
            if (node.Name == "Relational" || node.Name == "Equality")
            {
                float left = Convert.ToSingle(Evaluate(node.Children[0]), CultureInfo.InvariantCulture);
                string op = node.Children[1].Name;
                float right = Convert.ToSingle(Evaluate(node.Children[2]), CultureInfo.InvariantCulture);

                return op switch {
                    ">" => left > right, "<" => left < right, ">=" => left >= right,
                    "<=" => left <= right, "==" => left == right, "!=" => left != right,
                    _ => false
                };
            }

            // ÖNCE: Değişkenin kendisi ise, hafızadan (Dictionary) değerini getir
            if (_variables.ContainsKey(node.Name))
            {
                return _variables[node.Name];
            }

            // SONRA: Temel Veri Tiplerini Çözümleme (Sayı veya Metin)
            if (node.ValueType == "int") return int.Parse(node.Name);
            if (node.ValueType == "float") return float.Parse(node.Name, CultureInfo.InvariantCulture);
            if (node.ValueType == "string") return node.Name.Replace("\"", ""); // Tırnakları temizle
            if (node.ValueType == "bool") return node.Name == "true";

            return 0;
        }

        // Koşul ifadelerinin (if/while) true-false denetimi
        private bool IsTruthy(object val)
        {
            if (val is bool b) return b;
            if (val is int i) return i != 0;
            if (val is float f) return f != 0.0f;
            return false;
        }
    }
}