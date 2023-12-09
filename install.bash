#!/bin/bash

# Sripts downloads the dependiences and builds the executable
mkdir Deps
cd Deps
git clone https://github.com/official-stockfish/Stockfish
cd Stockfish
cd src
make -j profile-build ARCH=x86-64-avx2 COMP=gcc
cp stockfish ../..
cd ../../..
dotnet new --install MonoGame.Templates.CSharp:
dotnet build
cp -r bin/Debug/net7.0/ Game
