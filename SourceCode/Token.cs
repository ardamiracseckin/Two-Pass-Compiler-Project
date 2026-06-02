namespace SimpleCompiler.Compiler
{
    public class Token
    {
        // Token'ın kategorisi (ör. TokenType.IntLiteral)
        public TokenType Type { get; set; }
        
        // Token'ın taşıdığı metinsel değer (ör. "10", "+", "x")
        public string Value { get; set; }
        
        // Hata ayıklama için token'ın bulunduğu satır numarası
        public int Line { get; set; }

        public Token(TokenType type, string value, int line)
        {
            Type = type;
            Value = value;
            Line = line;
        }

        // Token listesini ekrana veya konsola kolayca yazdırmak için
        public override string ToString()
        {
            return $"Line: {Line}, Token: '{Value}', Type: {Type}";
        }
    }
}