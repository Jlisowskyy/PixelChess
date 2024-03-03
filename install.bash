#!/bin/bash

# Scripts downloads the dependencies and builds the executable
mkdir Deps
mkdir Game
cd Deps || exit
git clone https://github.com/official-stockfish/Stockfish
cd Stockfish || exit
cd src || exit
make -j profile-build ARCH=x86-64-avx2 COMP=gcc
cp stockfish ../..
cd ../../..
dotnet new --install MonoGame.Templates.CSharp:
dotnet build
cp -r bin/Debug/net7.0/* Game
mv  Deps Game/ || exit
rm -rf Deps
