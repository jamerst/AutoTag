using AutoTag.Core.Movie;
using AutoTag.Core.TV;

namespace AutoTag.CLI;

public class CLIInterface : IUserInterface
{
    private TaggingFile _file = null!;

    private bool Success;
    private int Warnings;

    public void SetCurrentFile(TaggingFile file)
    {
        _file = file;
    }
    
    public void SetFilePath(string path)
    {
    }

    public void SetStatus(string status, MessageType type)
    {
        if (type == MessageType.Error && !_file.Success)
        {
            _file.Status += Environment.NewLine + status;
        }
        else if (type == MessageType.Error)
        {
            Success = false;
            _file.Success = false;
            Console.ForegroundColor = ConsoleColor.Red;
            _file.Status = status;
        }
        else if (_file.Success)
        {
            _file.Status = status;
        }

        if (type == MessageType.Warning)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Warnings++;
        }

        Console.WriteLine($"    {_file.Status}");
        Console.ResetColor();
    }

    public int? SelectOption(List<(string, string)> options)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("    Please choose an option, or press enter to skip file:");
        Console.ResetColor();
        for (int i = 0; i < options.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"        {i}: {options[i].Item1} ({options[i].Item2})");
        }
        Console.ResetColor();

        int? choice = null;
        bool inputSuccess = false;
        while (!inputSuccess)
        {
            choice = InputResult(options.Count, out inputSuccess);
        }

        return choice;
    }
    
    private int? InputResult(int count, out bool success)
    {
        success = true;
        Console.Write($"    Choose an option [0-{count - 1}]: ");
        string? choice = Console.ReadLine();

        int chosen;
        if (int.TryParse(choice, out chosen) && chosen >= 0 && chosen < count)
        {
            return chosen;
        }
        else if (!string.IsNullOrEmpty(choice))
        {
            // if entry is either not a number or out of range
            success = false;
        }

        return null;
    }
}