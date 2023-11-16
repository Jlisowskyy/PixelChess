
# MyMathInterpreter

## Table of Contents
1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Installation](#installation)
3. [Roadmap](#roadmap)
4. [License](#license)
## Introduction

A simple chess project as my first C# program, designed to acquire foundational experience in the language. 
The primary objective is to develop an interactive graphical game application using the MonoGame framework, 
emphasizing backend aspects of chess rather than complex UI. 
In the near future, I plan to integrate the game with a chess engine written in C++. 
Currently, the game only supports a human player as the opponent.

## Getting Started

### Prerequisites

- MonoGame framework
- .NET 7.0 or newer

### Installation

Install dotnet template using CLI or just follow MonoGame's tutorial:

```shell
dotnet new --install MonoGame.Templates.CSharp:
```

Clone the repository and change pwd to newly created folder:

```shell
git clone https://github.com/Jlisowskyy/PixelChess ; cd PixelChess
```

You can just use your IDE of choice to compile the program or use c# compiler by yourself:

```shell
dotnet build
```z

## Roadmap

Progress in this repository may be slower due to demanding university duties.
However, future plans include the incorporation of the following features:

- [x] Correct chess rules implemented
- [x] Working simple UI
- [x] Fen notation support
- [ ] UCI implemented to enable connecting with any bot
- [ ] Remote games
- [ ] Position editing tools
- [ ] Own competitive chess-engine written in C++
- 
## License

See `LICENSE.txt` for more information.
