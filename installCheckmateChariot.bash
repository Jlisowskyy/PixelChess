#!/bin/bash

# Scripts downloads the dependencies and builds the executable
mkdir Deps
mkdir Game
cd Deps || exit
git clone https://github.com/Jlisowskyy/Checkmate-Chariot engine
cd engine || exit
cmake CMakeLists.txt
make
cp Checkmate-Chariot ../
cp uci_ready ../../
cd .. || exit
rm -rf engine || exit
cd ..
dotnet new --install MonoGame.Templates.CSharp:
dotnet build
cp -r bin/Debug/net7.0/* Game
mv  Deps Game/
echo "ChessEngineDir=Deps/Checkmate-Chariot" > Game/init_config
echo "ChessEngineSearchTimeMs=10000" >> Game/init_config
