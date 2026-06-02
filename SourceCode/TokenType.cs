namespace SimpleCompiler.Compiler
{
    public enum TokenType
    {
        // Anahtar Kelimeler
        Int, Float, String, If, Else, While, For,
        Switch, Case, Default, Print, Bool, True, False,

        // Tanımlayıcılar ve Değerler
        Identifier, IntLiteral, FloatLiteral, StringLiteral,  

        // Operatörler
        Assign, Plus, Minus, Multiply, Divide, 
        Equal, NotEqual, Less, Greater, LessEqual, GreaterEqual, And, Or,

        // Ayırıcılar
        Semicolon, Colon, LeftParen, RightParen, LeftBrace, RightBrace,

        // Özel Durumlar
        EndOfFile, Unknown 
    }
}