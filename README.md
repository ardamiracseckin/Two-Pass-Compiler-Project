# Design and Implementation of a Two-Pass Compiler & Responsive IDE

[span_0](start_span)[span_1](start_span)An advanced software engineering project featuring a custom **Two-Pass Compiler**, an integrated **Real-Time Interpreter**, and a modern **Responsive IDE** built from scratch[span_0](end_span)[span_1](end_span). [span_2](start_span)Developed as the Final Project for the System Programming course at Istanbul Health and Technology University (İSTÜN)[span_2](end_span).

---

## 🚀 System Architecture & Compiler Pipeline

[span_3](start_span)The system is split into two logical and functional core layers to process a custom high-level programming language subset[span_3](end_span):

### 1. Pass 1: Lexical Analysis (Lexer)
* **[span_4](start_span)Engine Design:** Built as a Deterministic Finite State Machine (DFSM) that character-by-character scans the source code input stream[span_4](end_span).
* **[span_5](start_span)Tokenization:** Converts valid character sequences into strongly typed, meaningful `Token` objects[span_5](end_span).
* **[span_6](start_span)Filtering:** Automatically strips out whitespaces and inline single-line comments (`//`) without generating empty tokens[span_6](end_span).
* **[span_7](start_span)Lookahead Mechanism:** Implements a safe `PeekNext()` methodology to accurately differentiate between single and double operators (e.g., `<`, `<=`, `>`, `>=`)[span_7](end_span).
* **[span_8](start_span)Rich Vocabulary:** Supports core keywords including `int`, `float`, `string`, `bool`, `if`, `else`, `while`, `for`, `switch`, `case`, `default`, `print`, `true`, and `false`[span_8](end_span).

### 2. Pass 2: Syntax & Semantic Analysis (Parser)
* **[span_9](start_span)[span_10](start_span)Parsing Technique:** Utilizes a top-down **Recursive Descent Parsing** strategy matching a strict Backus-Naur Form (BNF) grammar definition[span_9](end_span)[span_10](end_span).
* **[span_11](start_span)AST Construction:** Generates a structured, hierarchical Abstract Syntax Tree (AST) representing the program's execution logic[span_11](end_span).
* **[span_12](start_span)Operator Precedence:** Mathematical and logical precedence is hardcoded into the C# Call Stack depth natively (e.g., nested execution calls where `Term()` evaluation takes priority within `Arithmetic()`)[span_12](end_span).
* **[span_13](start_span)Semantic Integrity:** Enforces **Strong Type Checking**[span_13](end_span). [span_14](start_span)It detects undeclared variables at compile time and throws instant "Type Mismatch" errors when conflicting types interact (e.g., assigning a `string` to an `int`)[span_14](end_span).

---

## 🛠️ Advanced Compilation Techniques

* **[span_15](start_span)[span_16](start_span)Syntactic Sugar Architecture:** To minimize interpreter execution complexity, complex loops (e.g., `for` loops) are silently rewritten into an optimized `WhileStatement` structural block within the AST layer[span_15](end_span)[span_16](end_span). [span_17](start_span)Initializers are scoped inside a separate code block, and loop iterators are automatically appended to the end of the execution block[span_17](end_span).
* **[span_18](start_span)Hexadecimal Memory Address Simulation:** Implements a realistic 32-bit operating system runtime memory model[span_18](end_span). [span_19](start_span)Variables mapped inside the Symbol Table use a high-speed `Dictionary<string, Symbol>` data structure ensuring `O(1)` lookup complexity[span_19](end_span). 
* **[span_20](start_span)Dynamic Byte Allocation:** Simulates physical RAM consumption by allocating exact byte offsets dynamically depending on the data type (4 bytes for `int`/`float`, 8 bytes for reference-type `string`, and 1 byte for `bool`)[span_20](end_span). [span_21](start_span)Allocated scopes are rendered inside the UI using professional 8-digit Hexadecimal strings (e.g., `0x00000400`)[span_21](end_span).
* **[span_22](start_span)Panic Mode Error Handling & Synchronization:** Built with a `Synchronize()` recovery method[span_22](end_span). [span_23](start_span)[span_24](start_span)If a compile error occurs, the parser logs the exception with line data and advances the stream pointer to the next semantic anchor (semicolons `;` or structural keywords like `if`, `while`, `for`), enabling complete multi-error detection without system crashes[span_23](end_span)[span_24](end_span).

---

## 🖥️ Modern Responsive IDE Layout

* **[span_25](start_span)Esnek SplitContainer Architecture:** Eschews legacy static forms for a modern desktop layout segmented across 5 independent reactive tracking zones: Code Editor, Error Console, Token Stream Grid, Symbol Table, and the AST Output Terminal[span_25](end_span).
* **[span_26](start_span)Pixel-Perfect Line Numbering:** Features an automated right-aligned numbering component built with a native `PictureBox`[span_26](end_span). [span_27](start_span)It dynamically hooks into the underlying editor's `VScroll` and `Paint` triggers to align coordinate rendering smoothly without scrolling drift[span_27](end_span).
* **[span_28](start_span)Real-Time Regex Syntax Highlighting:** Integrated custom Regex token parsing that colors key grammar structures instantly on `TextChanged` events (Keywords ➡️ Blue, Strings ➡️ Orange, Numeric Literals ➡️ Purple, Comments ➡️ Green)[span_28](end_span).
* **[span_29](start_span)Classic Hacker-Themed Console:** Execution output streams straight from the Interpreter engine to an isolated custom green-on-black terminal window layout[span_29](end_span).

---

## ⚙️ Tech Stack & Development Environment

* **[span_30](start_span)Language:** C#[span_30](end_span)
* **[span_31](start_span)Framework:** .NET 8.0 Runtime[span_31](end_span)
* **[span_32](start_span)UI Technology:** Windows Forms Custom Responsive Engine[span_32](end_span)
* **[span_33](start_span)Development Platform:** Windows OS Deployment Target[span_33](end_span)

---

## 👨‍💻 Team & Responsibility Distribution

[span_34](start_span)The workload was balanced meticulously across core engine modules and user experience interfaces[span_34](end_span):

* **[span_35](start_span)Mert Mak (230609026)**[span_35](end_span)
  * [span_36](start_span)Designed and coded the Lexer FSM and multi-pass recursive token scanner[span_36](end_span).
  * [span_37](start_span)Engineered the Parser engine, handling operator depth rules and structural Syntactic Sugar transformations[span_37](end_span).
  * [span_38](start_span)Developed the core real-time Interpreter execution environment[span_38](end_span).
  * [span_39](start_span)Wrote the Semantic Analyzer algorithm, Symbol Table lookup logic, and 32-bit Hex byte memory allocator[span_39](end_span).

* **[span_40](start_span)Miraç Arda Seçkin (230609034)**[span_40](end_span)
  * [span_41](start_span)Designed the modern Responsive IDE layout and flexible SplitContainer framework[span_41](end_span).
  * [span_42](start_span)Implemented advanced editor algorithms, including real-time Regex-based lexical color highlights and absolute-precision pixel line numbers[span_42](end_span).
  * [span_43](start_span)Managed full-system cross-module integration: automated local file I/O operations, continuous databinding interfaces for Token/Symbol tables, and connected compiler event pipelines directly to the terminal UI console[span_43](end_span).
