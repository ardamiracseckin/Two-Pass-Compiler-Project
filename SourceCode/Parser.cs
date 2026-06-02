using System;
using System.Collections.Generic;

namespace SimpleCompiler.Compiler
{
    public class ASTNode
    {
        public string Name { get; set; }
        public string ValueType { get; set; } // YENİ EKLENDİ: İfadenin tipini tutacak (int, float vb.)
        public List<ASTNode> Children { get; set; }

        public ASTNode(string name)
        {
            Name = name;
            ValueType = "unknown";
            Children = new List<ASTNode>();
        }

        public void AddChild(ASTNode child)
        {
            Children.Add(child);
        }
    }

    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _current = 0;
        private readonly SymbolTable _symbolTable;
        private readonly List<string> _errors;

        public Parser(List<Token> tokens, SymbolTable symbolTable)
        {
            _tokens = tokens;
            _symbolTable = symbolTable;
            _errors = new List<string>();
        }

        public List<string> GetErrors() => _errors;

        public ASTNode Parse()
        {
            ASTNode root = new ASTNode("Program");
            while (!IsAtEnd() && Peek().Type != TokenType.EndOfFile)
            {
                try
                {
                    root.AddChild(ParseStatement());
                }
                catch (Exception ex)
                {
                    _errors.Add($"[Satır {Peek().Line}] Hata: {ex.Message}");
                    Synchronize();
                }
            }
            return root;
        }

        private ASTNode ParseStatement()
        {
            if (Match(TokenType.Int) || Match(TokenType.Float) || Match(TokenType.String) || Match(TokenType.Bool)) return Declaration();
            if (Match(TokenType.Int) || Match(TokenType.Float))  return Declaration();
            if (Match(TokenType.Print)) return PrintStatement();
            if (Match(TokenType.If)) return IfStatement();
            if (Match(TokenType.While)) return WhileStatement();
            if (Match(TokenType.LeftBrace)) return Block();
            if (Match(TokenType.Identifier)) return Assignment();
            if (Match(TokenType.For)) return ForStatement();
            if (Match(TokenType.Switch)) return SwitchStatement();

            throw new Exception($"Beklenmeyen token: '{Peek().Value}'.");
        }

        private ASTNode Block()
        {
            ASTNode node = new ASTNode("Block");
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                node.AddChild(ParseStatement());
            }
            Consume(TokenType.RightBrace, "Blok sonuna '}' bekleniyor.");
            return node;
        }

        private ASTNode IfStatement()
        {
            Consume(TokenType.LeftParen, "'if' komutundan sonra '(' bekleniyor.");
            ASTNode condition = Expression();
            Consume(TokenType.RightParen, "')' parantezi kapatılmamış.");

            ASTNode thenBranch = ParseStatement();
            ASTNode node = new ASTNode("IfStatement");
            node.AddChild(new ASTNode("Condition") { Children = { condition } });
            node.AddChild(new ASTNode("Then") { Children = { thenBranch } });

            if (Match(TokenType.Else))
            {
                ASTNode elseBranch = ParseStatement();
                node.AddChild(new ASTNode("Else") { Children = { elseBranch } });
            }
            return node;
        }

        private ASTNode WhileStatement()
        {
            Consume(TokenType.LeftParen, "'while' komutundan sonra '(' bekleniyor.");
            ASTNode condition = Expression();
            Consume(TokenType.RightParen, "')' parantezi kapatılmamış.");

            ASTNode body = ParseStatement();
            ASTNode node = new ASTNode("WhileStatement");
            node.AddChild(new ASTNode("Condition") { Children = { condition } });
            node.AddChild(new ASTNode("Body") { Children = { body } });
            return node;
        }
        private ASTNode ForStatement()
        {
            Consume(TokenType.LeftParen, "'for' komutundan sonra '(' bekleniyor.");

            // 1. BAŞLANGIÇ (Initializer: int i = 0; veya sadece ;)
            ASTNode initializer = null;
            if (Match(TokenType.Semicolon)) {
                initializer = null;
            } else if (Match(TokenType.Int) || Match(TokenType.Float) || Match(TokenType.String)) {
                initializer = Declaration(); // Noktalı virgülü kendi içinde tüketir
            } else if (Match(TokenType.Identifier)) {
                initializer = Assignment(); // Noktalı virgülü kendi içinde tüketir
            } else {
                throw new Exception("Geçersiz 'for' döngüsü başlangıcı.");
            }

            // 2. KOŞUL (Condition: i < 5;)
            ASTNode condition = null;
            if (!Check(TokenType.Semicolon)) {
                condition = Expression();
            }
            Consume(TokenType.Semicolon, "'for' koşulundan sonra ';' bekleniyor.");

            // 3. ARTIRIM (Increment: i = i + 1) -> Burada noktalı virgül olmamalı
            ASTNode increment = null;
            if (!Check(TokenType.RightParen)) {
                if (Match(TokenType.Identifier)) {
                    Token nameToken = Previous();
                    if (!_symbolTable.Contains(nameToken.Value))
                        throw new Exception($"'{nameToken.Value}' tanımlanmadan kullanılamaz.");
                    
                    string expectedType = _symbolTable.GetSymbolType(nameToken.Value);
                    Consume(TokenType.Assign, "'=' atama operatörü bekleniyor.");
                    ASTNode valueNode = Expression();

                    // Tip Uyuşmazlığı Kontrolü
                    if (expectedType == "int" && valueNode.ValueType != "int") throw new Exception($"Tip Uyuşmazlığı.");
                    if (expectedType == "float" && valueNode.ValueType == "string") throw new Exception($"Tip Uyuşmazlığı.");
                    if (expectedType == "string" && valueNode.ValueType != "string") throw new Exception($"Tip Uyuşmazlığı.");

                    increment = new ASTNode("Assignment");
                    increment.AddChild(new ASTNode(nameToken.Value));
                    increment.AddChild(new ASTNode("="));
                    increment.AddChild(valueNode);
                } else {
                    throw new Exception("'for' artırım kısmında geçerli bir atama işlemi bekleniyor.");
                }
            }
            Consume(TokenType.RightParen, "'for' komutundan sonra ')' bekleniyor.");

            // 4. GÖVDE (Döngü içi kodlar)
            ASTNode body = ParseStatement();

            // --- SÖZDİZİMSEL ŞEKER (SYNTACTIC SUGAR) ÇEVİRİSİ ---
            
            // Eğer artırım varsa, döngü gövdesinin en sonuna ekle
            if (increment != null) {
                ASTNode newBody = new ASTNode("Block");
                newBody.AddChild(body);
                newBody.AddChild(increment);
                body = newBody;
            }

            // Eğer koşul boş bırakıldıysa (örneğin for(;;)), sonsuz döngü (true) yap
            if (condition == null) {
                condition = new ASTNode("1") { ValueType = "int" };
            }

            // While döngüsü ağacını oluştur
            ASTNode whileNode = new ASTNode("WhileStatement");
            whileNode.AddChild(new ASTNode("Condition") { Children = { condition } });
            whileNode.AddChild(new ASTNode("Body") { Children = { body } });

            // Başlangıç tanımı varsa (int i = 0), scope (kapsam) için her şeyi bir bloğa sar
            if (initializer != null) {
                ASTNode blockNode = new ASTNode("Block");
                blockNode.AddChild(initializer);
                blockNode.AddChild(whileNode);
                return blockNode;
            }

            return whileNode;
        }

        private ASTNode SwitchStatement()
        {
            Consume(TokenType.LeftParen, "'switch' komutundan sonra '(' bekleniyor.");
            ASTNode condition = Expression(); // Neyi kontrol edeceğiz? (örn: x)
            Consume(TokenType.RightParen, "')' parantezi kapatılmamış.");
            Consume(TokenType.LeftBrace, "Switch bloğu için '{' bekleniyor.");

            ASTNode switchNode = new ASTNode("SwitchStatement");
            switchNode.AddChild(new ASTNode("Condition") { Children = { condition } });

            // Bloğun sonuna ( '}' ) gelene kadar case ve default'ları oku
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                if (Match(TokenType.Case))
                {
                    ASTNode caseValue = Expression(); // Hangi değere eşitse (örn: 1)
                    Consume(TokenType.Colon, "Case değerinden sonra ':' bekleniyor.");
                    
                    ASTNode caseBlock = new ASTNode("CaseBlock");
                    caseBlock.AddChild(caseValue);
                    
                    // Bir sonraki case, default veya } gelene kadar içindeki kodları oku
                    while (!Check(TokenType.Case) && !Check(TokenType.Default) && !Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        caseBlock.AddChild(ParseStatement());
                    }
                    switchNode.AddChild(caseBlock);
                }
                else if (Match(TokenType.Default))
                {
                    Consume(TokenType.Colon, "Default anahtar kelimesinden sonra ':' bekleniyor.");
                    ASTNode defaultBlock = new ASTNode("DefaultBlock");
                    
                    while (!Check(TokenType.Case) && !Check(TokenType.Default) && !Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        defaultBlock.AddChild(ParseStatement());
                    }
                    switchNode.AddChild(defaultBlock);
                }
                else
                {
                    throw new Exception("Switch bloğu içerisinde sadece 'case' veya 'default' kullanılabilir.");
                }
            }
            Consume(TokenType.RightBrace, "Switch bloğunun sonuna '}' bekleniyor.");
            
            return switchNode;
        }

        private ASTNode Declaration()
        {
Token typeToken = Previous();
    Token nameToken = Consume(TokenType.Identifier, "Değişken adı bekleniyor.");
    
    string typeString = typeToken.Type switch
    {
        TokenType.Int => "int",
        TokenType.Float => "float",
        TokenType.String => "string",
        TokenType.Bool => "bool",
        _ => "unknown"
    };
    
    _symbolTable.AddSymbol(nameToken.Value, typeString);

    ASTNode node = new ASTNode("Declaration");
    node.AddChild(new ASTNode(typeToken.Value));
    node.AddChild(new ASTNode(nameToken.Value));

    // Eğer tanımlamadan hemen sonra eşittir (=) gelirse, bunu bir atama işlemi olarak da işle
    if (Match(TokenType.Assign))
    {
        ASTNode valueNode = Expression(); 
        
        // Tip Kontrolü
        if (typeString == "int" && valueNode.ValueType != "int")
            throw new Exception($"Tip Uyuşmazlığı: '{nameToken.Value}' (int) değişkenine '{valueNode.ValueType}' atanamaz.");
        if (typeString == "float" && valueNode.ValueType == "string")
            throw new Exception($"Tip Uyuşmazlığı: '{nameToken.Value}' (float) değişkenine 'string' atanamaz.");
        if (typeString == "string" && valueNode.ValueType != "string")
            throw new Exception($"Tip Uyuşmazlığı: '{nameToken.Value}' (string) değişkenine '{valueNode.ValueType}' atanamaz.");

        node.AddChild(new ASTNode("="));
        node.AddChild(valueNode);
    }

    Consume(TokenType.Semicolon, "Satır sonuna ';' bekleniyor.");
    return node;
        }

        private ASTNode PrintStatement()
        {
            Consume(TokenType.LeftParen, "Print komutundan sonra '(' bekleniyor.");
            ASTNode expr = Expression();
            Consume(TokenType.RightParen, "Print komutundan sonra ')' bekleniyor.");
            Consume(TokenType.Semicolon, "Satır sonuna ';' bekleniyor.");

            ASTNode node = new ASTNode("Print");
            node.AddChild(expr);
            return node;
        }

        private ASTNode Assignment()
        {
           Token nameToken = Previous();

            if (!_symbolTable.Contains(nameToken.Value))
                throw new Exception($"'{nameToken.Value}' isimli değişken tanımlanmadan kullanılamaz.");

            string expectedType = _symbolTable.GetSymbolType(nameToken.Value);

            Consume(TokenType.Assign, "'=' atama operatörü bekleniyor.");
            ASTNode valueNode = Expression(); 
            
            // YENİ EKLENDİ: Kapsamlı Tip Kontrolü
            if (expectedType == "int" && valueNode.ValueType != "int")
                throw new Exception($"Tip Uyuşmazlığı: '{nameToken.Value}' (int) değişkenine '{valueNode.ValueType}' atanamaz.");
            if (expectedType == "float" && valueNode.ValueType == "string")
                throw new Exception($"Tip Uyuşmazlığı: '{nameToken.Value}' (float) değişkenine 'string' atanamaz.");
            if (expectedType == "string" && valueNode.ValueType != "string")
                throw new Exception($"Tip Uyuşmazlığı: '{nameToken.Value}' (string) değişkenine '{valueNode.ValueType}' atanamaz.");

            Consume(TokenType.Semicolon, "Satır sonuna ';' bekleniyor.");

            ASTNode node = new ASTNode("Assignment");
            node.AddChild(new ASTNode(nameToken.Value));
            node.AddChild(new ASTNode("="));
            node.AddChild(valueNode);
            return node;
        }

        // --- İFADE VE İŞLEM ÖNCELİĞİ ---

        private ASTNode Expression() => LogicalOr();

        private ASTNode LogicalOr()
        {
            ASTNode node = LogicalAnd();
            while (Match(TokenType.Or))
            {
                Token op = Previous();
                ASTNode right = LogicalAnd();
                ASTNode newNode = new ASTNode("LogicalOr") { ValueType = "bool" };
                newNode.AddChild(node); newNode.AddChild(new ASTNode(op.Value)); newNode.AddChild(right);
                node = newNode;
            }
            return node;
        }

        private ASTNode LogicalAnd()
        {
            ASTNode node = Equality();
            while (Match(TokenType.And))
            {
                Token op = Previous();
                ASTNode right = Equality();
                ASTNode newNode = new ASTNode("LogicalAnd") { ValueType = "bool" };
                newNode.AddChild(node); newNode.AddChild(new ASTNode(op.Value)); newNode.AddChild(right);
                node = newNode;
            }
            return node;
        }

        private ASTNode Equality()
        {
            ASTNode node = Relational();
            while (Match(TokenType.Equal, TokenType.NotEqual))
            {
                Token op = Previous();
                ASTNode right = Relational();
                ASTNode newNode = new ASTNode("Equality") { ValueType = "bool" };
                newNode.AddChild(node); newNode.AddChild(new ASTNode(op.Value)); newNode.AddChild(right);
                node = newNode;
            }
            return node;
        }

        private ASTNode Relational()
        {
            ASTNode node = Arithmetic();
            while (Match(TokenType.Less, TokenType.Greater, TokenType.LessEqual, TokenType.GreaterEqual))
            {
                Token op = Previous();
                ASTNode right = Arithmetic();
                ASTNode newNode = new ASTNode("Relational") { ValueType = "bool" };
                newNode.AddChild(node); newNode.AddChild(new ASTNode(op.Value)); newNode.AddChild(right);
                node = newNode;
            }
            return node;
        }

        private ASTNode Arithmetic()
        {
            ASTNode node = Term();
            while (Match(TokenType.Plus, TokenType.Minus))
            {
                Token op = Previous();
                ASTNode right = Term();
                ASTNode newNode = new ASTNode("Arithmetic");
                // Eğer sol veya sağ taraftan biri float ise, sonuç float olur. İkisi de int ise sonuç int olur.
                newNode.ValueType = (node.ValueType == "float" || right.ValueType == "float") ? "float" : "int";
                
                newNode.AddChild(node); newNode.AddChild(new ASTNode(op.Value)); newNode.AddChild(right);
                node = newNode;
            }
            return node;
        }

        private ASTNode Term()
        {
            ASTNode node = Factor();
            while (Match(TokenType.Multiply, TokenType.Divide))
            {
                Token op = Previous();
                ASTNode right = Factor();
                ASTNode newNode = new ASTNode("Term");
                newNode.ValueType = (node.ValueType == "float" || right.ValueType == "float") ? "float" : "int";

                newNode.AddChild(node); newNode.AddChild(new ASTNode(op.Value)); newNode.AddChild(right);
                node = newNode;
            }
            return node;
        }

        private ASTNode Factor()
        {
            if (Match(TokenType.True)) return new ASTNode("true") { ValueType = "bool" };
            if (Match(TokenType.False)) return new ASTNode("false") { ValueType = "bool" };
            if (Match(TokenType.IntLiteral))
                return new ASTNode(Previous().Value) { ValueType = "int" };
                
            if (Match(TokenType.FloatLiteral))
                return new ASTNode(Previous().Value) { ValueType = "float" };
                
            if (Match(TokenType.StringLiteral))
                return new ASTNode(Previous().Value) { ValueType = "string" };

            if (Match(TokenType.Identifier))
            {
                Token idToken = Previous();
                if (!_symbolTable.Contains(idToken.Value))
                    throw new Exception($"'{idToken.Value}' tanımlanmadan kullanıldı.");
                
                string varType = _symbolTable.GetSymbolType(idToken.Value);
                return new ASTNode(idToken.Value) { ValueType = varType };
            }

            if (Match(TokenType.LeftParen))
            {
                ASTNode expr = Expression();
                Consume(TokenType.RightParen, "')' parantezi kapatılmamış.");
                return expr;
            }

            throw new Exception($"Geçersiz ifade: '{Peek().Value}'");
        }

        // --- YARDIMCI METOTLAR ---

        private Token Peek() => _tokens[_current];
        private Token Previous() => _tokens[_current - 1];
        private bool IsAtEnd() => _current >= _tokens.Count;
        
        private Token Advance() { if (!IsAtEnd()) _current++; return Previous(); }
        private bool Check(TokenType type) { if (IsAtEnd()) return false; return Peek().Type == type; }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type)) { Advance(); return true; }
            }
            return false;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new Exception(message);
        }

        private void Synchronize()
        {
            Advance();
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.Semicolon) return;
                switch (Peek().Type)
                {
                    case TokenType.Int: case TokenType.Float: case TokenType.If:
                    case TokenType.While: case TokenType.Print: return;
                }
                Advance();
            }
        }
    }
}