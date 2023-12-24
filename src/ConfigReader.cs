using System;
using System.Globalization;
using System.IO;

namespace PixelChess;

public partial class PixelChess
{
    private struct InitOptions
        {
            public string ChessEngineDir { get; set; }
            public string StartPosition { get; set; }
            public bool LoggingToFileEnabled { get; set; }

            public override string ToString()
                => $"ChessEngineDir={ChessEngineDir}\nStartPosition={StartPosition}\nLoggingToFileEnabled={LoggingToFileEnabled}";
        }

    private abstract class ConfigReader
    {
        public static InitOptions LoadSettings()
        {
            try
            {
                return _loadSettings();
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine($"[ ERROR ] Not able to correctly read configuration file!\nLoading default settings...\nCause:\n{exc}");
                return DefaultOptions;
            }
        }
        
        private static InitOptions _loadSettings()
        {
            using FileStream confFile = new FileStream("init_config", FileMode.OpenOrCreate, FileAccess.ReadWrite);

            if (confFile.Length == 0)
                // empty file or no previous configuration, so we init one.
            {
                using StreamWriter sw = new StreamWriter(confFile);
                sw.Write(DefaultOptions.ToString());
                return DefaultOptions;
            }
            
            // Reading configuration.
            using StreamReader sr = new StreamReader(confFile);
            string confFileContent = _readLimitedSizeFile(sr, MaxConfFileSize);
            
            // Processing configuration.
            return _processConfigFile(confFileContent);
        }

        private static InitOptions _processConfigFile(string fileContent)
        {
            var records = fileContent.Split("\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            InitOptions retOptions = DefaultOptions;
            foreach (var record in records)
            {
                var keyValuePair = record.Split("=",
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                
                // Detecting invalid record structure.
                if (keyValuePair.Length != 2)
                {
                    _announceInvalidRecord(record);
                    continue;
                }

                switch (keyValuePair[0])
                {
                    case "ChessEngineDir":
                        retOptions.ChessEngineDir = keyValuePair[1];
                        break;
                    case "StartPosition":
                        retOptions.StartPosition = keyValuePair[1];
                        break;
                    case "LoggingToFileEnabled":

                        if (bool.TryParse(keyValuePair[1], out bool value))
                            retOptions.LoggingToFileEnabled = value;
                        else
                            _announceInvalidRecord(record);
                        break;
                    
                    default:
                        _announceInvalidRecord(record);
                        break;
                }
            }

            return retOptions;
        }
        
        private static string _readLimitedSizeFile(StreamReader sr, int maxRead)
        {
            if (sr.BaseStream.Length <= maxRead)
                return sr.ReadToEnd();
            
            // Reads only 32k characters
            char[] buffer = new char[maxRead];
            sr.Read(buffer, 0, maxRead);
            return new string(buffer);
        }
        
        private static void _announceInvalidRecord(string record)
            => Console.Error.WriteLine($"[ WARNING ] Invalid record inside configuration file:\n{record}");
        
        // ------------------------------
        // private fields
        // ------------------------------
        
        private static readonly InitOptions DefaultOptions = new InitOptions()
            { ChessEngineDir = "NONE", LoggingToFileEnabled = false, StartPosition = "NONE" };
        
        private const int MaxConfFileSize = 32 * 1024; // 32kB
    }

    private void _applyInitSettings(InitOptions opt)
    {
        if (opt.StartPosition != "NONE")
            _board.ChangeGameLayout(opt.StartPosition);

        if (opt.ChessEngineDir != "NONE")
            Console.WriteLine();

        _debugToFile = opt.LoggingToFileEnabled;
        if (_debugToFile)
            _enableLoggingToFile();
    }
    
    private void _enableLoggingToFile()
        // Function enables logging to file, what means that stdout and stderr will be redirected to the file
        // if possible. Therefore every calls to Console.Write or Console.Error.Write will be written to the file,
        // during game object lifetime across whole program.
    {
        var dT = DateTime.Now;
        
        try
        {
            Directory.CreateDirectory("./Logs");
            string fName = $"Logs/{dT.Day}_{dT.Month}_{dT.Year}_{dT.Hour}:{dT.Minute}:{dT.Second}.log";
            _debugFStream = new FileStream(fName, FileMode.CreateNew, FileAccess.Write);
            _debugSWriter = new StreamWriter(_debugFStream);
            
            Console.SetError(_debugSWriter);
            Console.SetOut(_debugSWriter);
            _debugToFile = true;
        }
        catch (Exception exc)
        {
            Console.WriteLine($"[ ERROR ] Not able to correctly prepare debugging file:\n{exc}");
            _debugToFile = false;
            Console.SetError(_stdErr);
            Console.SetOut(_stdOut);
        }
    }
}