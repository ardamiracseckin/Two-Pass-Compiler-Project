using System;
using System.Collections.Generic;

namespace SimpleCompiler.Compiler
{
    public class Symbol
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string MemoryLocation { get; set; } // Hex formatında saklamak için string yapıldı
    }

    public class SymbolTable
    {
        private readonly Dictionary<string, Symbol> _symbols;
        
        // İşletim sistemi bellek mantığına uygun bir başlangıç adresi (Decimal: 1024 -> Hex: 0x0400)
        private int _memoryCounter = 1024; 

        public SymbolTable()
        {
            _symbols = new Dictionary<string, Symbol>();
        }

        public void AddSymbol(string name, string type)
        {
            if (_symbols.ContainsKey(name))
            {
                throw new Exception($"Sembol tablosu hatası: '{name}' isimli değişken zaten tanımlanmış.");
            }

            // RAM adresini 8 haneli Hexadecimal (16'lık taban) formata çevir (Örn: 0x00000400)
            string hexAddress = "0x" + _memoryCounter.ToString("X8");

            _symbols.Add(name, new Symbol 
            { 
                Name = name, 
                Type = type, 
                MemoryLocation = hexAddress 
            });

            // Bellekte kapladığı yere göre adresi artır
            // (int ve float 4 byte, referans tipli stringler 8 byte yer kaplar)
            if (type == "string") 
            {
                _memoryCounter += 8;
            }
            else if (type == "bool") _memoryCounter += 1;
            else 
            {
                _memoryCounter += 4;
            }
        }

        public bool Contains(string name)
        {
            return _symbols.ContainsKey(name);
        }

        public string GetSymbolType(string name)
        {
            if (_symbols.ContainsKey(name))
            {
                return _symbols[name].Type;
            }
            return "unknown";
        }

        public IEnumerable<Symbol> GetAllSymbols()
        {
            return _symbols.Values;
        }
    }
}