using System;
using System.Collections.Generic;

namespace SimpleCompiler.Compiler
{
    public class Lexer
    {
        private readonly string _source;
        private int _position;
        private int _line;

        public Lexer(string source)
        {
            _source = source;
            _position = 0;
            _line = 1;
        }

        private bool IsAtEnd => _position >= _source.Length;
        private char CurrentChar => IsAtEnd ? '\0' : _source[_position];
        
        // YENİ EKLENDİ: Bir sonraki karakteri önceden görmek için
        private char PeekNext() => (_position + 1 >= _source.Length) ? '\0' : _source[_position + 1];

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (!IsAtEnd)
            {
                if (char.IsWhiteSpace(CurrentChar))
                {
                    if (CurrentChar == '\n') _line++;
                    _position++;
                    continue;
                }

                // YENİ EKLENDİ: Yorum Satırı (//) Kontrolü
                if (CurrentChar == '/' && PeekNext() == '/')
                {
                    // Satır sonuna (veya dosya sonuna) kadar her şeyi atla
                    while (!IsAtEnd && CurrentChar != '\n')
                    {
                        _position++;
                    }
                    continue; 
                }

                if (char.IsLetter(CurrentChar))
                {
                    tokens.Add(ReadIdentifierOrKeyword());
                    continue;
                }

                if (char.IsDigit(CurrentChar))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }

                if (CurrentChar == '"')
                {
                    tokens.Add(ReadString());
                    continue;
                }

                tokens.Add(ReadOperatorOrDelimiter());
            }

            tokens.Add(new Token(TokenType.EndOfFile, "EOF", _line));
            return tokens;
        }

        private Token ReadIdentifierOrKeyword()
        {
            int start = _position;
            while (!IsAtEnd && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
            {
                _position++;
            }

            string text = _source.Substring(start, _position - start);
            
            TokenType type = text switch
            {
                "int" => TokenType.Int,
                "float" => TokenType.Float,
                "string" => TokenType.String, // YENİ EKLENDİ
                "if" => TokenType.If,
                "else" => TokenType.Else,
                "while" => TokenType.While,
                "print" => TokenType.Print,
                "for" => TokenType.For,
                "switch" => TokenType.Switch,
                "case" => TokenType.Case,
                "default" => TokenType.Default,
                "bool" => TokenType.Bool,       // YENİ
                "true" => TokenType.True,       // YENİ
                "false" => TokenType.False,
                _ => TokenType.Identifier
            };

            return new Token(type, text, _line);
        }

        private Token ReadNumber()
        {
            int start = _position;
            bool isFloat = false;

            while (!IsAtEnd && (char.IsDigit(CurrentChar) || CurrentChar == '.'))
            {
                if (CurrentChar == '.') isFloat = true;
                _position++;
            }

            string text = _source.Substring(start, _position - start);
            return new Token(isFloat ? TokenType.FloatLiteral : TokenType.IntLiteral, text, _line);
        }

        private Token ReadString()
        {
            _position++; 
            int start = _position;

            while (!IsAtEnd && CurrentChar != '"')
            {
                _position++;
            }

            string text = _source.Substring(start, _position - start);
            _position++; 
            return new Token(TokenType.StringLiteral, text, _line);
        }

        private Token ReadOperatorOrDelimiter()
        {
            char c = CurrentChar;
            _position++; 
            
            if (!IsAtEnd)
            {
                char nextC = CurrentChar;
                if (c == '=' && nextC == '=') { _position++; return new Token(TokenType.Equal, "==", _line); }
                if (c == '!' && nextC == '=') { _position++; return new Token(TokenType.NotEqual, "!=", _line); }
                if (c == '<' && nextC == '=') { _position++; return new Token(TokenType.LessEqual, "<=", _line); }
                if (c == '>' && nextC == '=') { _position++; return new Token(TokenType.GreaterEqual, ">=", _line); }
                if (c == '&' && nextC == '&') { _position++; return new Token(TokenType.And, "&&", _line); }
                if (c == '|' && nextC == '|') { _position++; return new Token(TokenType.Or, "||", _line); }
            }

            TokenType type = c switch
            {
                '=' => TokenType.Assign, '+' => TokenType.Plus, '-' => TokenType.Minus,
                '*' => TokenType.Multiply, '/' => TokenType.Divide, '<' => TokenType.Less,
                '>' => TokenType.Greater, ';' => TokenType.Semicolon, ':' => TokenType.Colon, // Colon eklendi
                '(' => TokenType.LeftParen, ')' => TokenType.RightParen, 
                '{' => TokenType.LeftBrace, '}' => TokenType.RightBrace,
                _ => TokenType.Unknown
            };

            return new Token(type, c.ToString(), _line);
        }
    }
}