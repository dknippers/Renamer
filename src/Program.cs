namespace Renamer;

public class Program
{
    private readonly Stack<State> _state = new();

    public Program(State initialState)
    {
        _state.Push(initialState);
    }

    private static readonly (char key, string title, Func<State?> fn) _quit = ('q', "Quit", () => null);

    private (char key, string title, Func<State?> fn) Back()
    {
        return ('b', "Back", () =>
            {
                _state.Pop();
                return _state.Pop();
            }
        );
    }

    public static void Main()
    {
        var program = new Program(State.MainMenu);
        program.Run();
    }

    void Run()
    {
        while (true)
        {
            var next = Handle();
            if (next is not State state)
            {
                break;
            }

            if (!_state.TryPeek(out var cur) || state != cur)
            {
                _state.Push(state);
            }
        }

        Console.WriteLine("Good bye.\n");
    }

    private State? Handle()
    {
        switch (_state.Peek())
        {
            case State.MainMenu:
                return HandleMenu("Renamer",
                ('1', "Find + Replace", () => State.FindAndReplace),
                _quit);

            case State.FindAndReplace:
                return RunFindAndReplace();

            default:
                throw new Exception($"Unknown state: {_state}");
        }

        State? RunFindAndReplace(string? initialDir = null)
        {
            FindAndReplace(out string dir, initialDir);

            return HandleMenu(null,
                ('1', $"Again in {dir}", () => RunFindAndReplace(dir)),
                ('2', $"New", () => RunFindAndReplace(null)),
                Back(),
                _quit
            );
        }
    }

    private static State? HandleMenu(string? header, params (char key, string title, Func<State?> fn)[] options)
    {
        var optionsByChar = options.ToDictionary(o => o.key);

        if (header?.Length > 0)
        {
            PrintHeader(header);
        }

        foreach (var (key, title, _) in options)
        {
            Console.WriteLine($"{key}. {title}");
        }

        Console.WriteLine();

        do
        {
            Console.Write("> ");
            char c = Console.ReadKey().KeyChar;

            if (optionsByChar.TryGetValue(c, out var option))
            {
                Console.WriteLine("\n");
                return option.fn();
            }

            Console.WriteLine("\nInvalid input");
            Console.WriteLine();
        } while (true);
    }

    private static void PrintHeader(string header)
    {
        Console.Write("+");
        Console.Write(string.Join("", Enumerable.Repeat('-', header.Length + 2)));
        Console.WriteLine("+");

        Console.WriteLine($"| {header} |");

        Console.Write("+");
        Console.Write(string.Join("", Enumerable.Repeat('-', header.Length + 2)));
        Console.WriteLine("+");
    }

    public enum State
    {
        MainMenu,
        FindAndReplace,
    }

    private static readonly string[] _skipDirs = new[]
    {
        ".git",
        ".vs",
        "bin",
        "obj",
        "node_modules",
    };

    public static void FindAndReplace(out string dir, string? initialDir = null)
    {
        dir = initialDir ?? "";
        while (true)
        {
            if (dir.Length == 0)
            {
                Console.Write("Source directory: ");
                dir = Console.ReadLine() ?? "";
            }

            if (!Directory.Exists(dir))
            {
                Console.WriteLine($"Directory {dir} does not exist");
                dir = "";
                continue;
            }

            break;
        }

        string find;
        while (true)
        {
            Console.Write("Find: ");
            find = Console.ReadLine() ?? "";
            if (string.IsNullOrEmpty(find))
            {
                Console.WriteLine("'find' parameter cannot be empty");
                continue;
            }

            break;
        }

        string replace;
        Console.Write("Replace: ");
        replace = Console.ReadLine() ?? "";

        if (find == replace)
        {
            Console.WriteLine("'find' and 'replace' are equal: nothing to do.");
            return;
        }

        Console.WriteLine($"Replacing \"{find}\" with \"{replace}\" in \"{dir}\"");
        Console.WriteLine("Enter \"go\" to continue or anything else to abort");
        if (Console.ReadLine()?.Trim() != "go")
        {
            Console.WriteLine("Aborting...");
            return;
        }

        Console.WriteLine($"\n1. Exact match: \"{find}\" -> \"{replace}\"\n");

        // Rename exact case
        Go(dir, find, replace, false);

        var findLower = find.ToLowerInvariant();
        var replaceLower = replace.ToLowerInvariant();

        if (findLower != find || replaceLower != replace)
        {
            Console.WriteLine($"\n2. Lower case: \"{findLower}\" -> \"{replaceLower}\"\n");

            // Rename lower cased
            Go(dir, findLower, replaceLower, false);
        }

        Console.WriteLine("Done!\n");

        static void Go(string dir, string find, string replace, bool renameRootDir)
        {
            var dirName = Path.GetFileName(dir);
            if (_skipDirs.Contains(dirName))
            {
                return;
            }

            RenameSubDirs(dir, find, replace);

            var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
            ReplaceInFiles(files, find, replace);
            RenameFiles(files, find, replace);

            if (renameRootDir)
            {
                RenameDirectory(dir, dirName, find, replace);
            }

            static void RenameSubDirs(string dir, string find, string replace)
            {
                var subDirectories = Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly);
                foreach (var subDir in subDirectories)
                {
                    Go(subDir, find, replace, true);
                }
            }

            static void ReplaceInFiles(IEnumerable<string> files, string find, string replace)
            {
                foreach (var file in files)
                {
                    // File contents
                    var content = File.ReadAllText(file);
                    var newContent = content.Replace(find, replace);
                    if (content != newContent)
                    {
                        File.WriteAllText(file, newContent);
                        Console.WriteLine($"Updated {file}");
                    }
                }
            }

            static void RenameFiles(IEnumerable<string> files, string find, string replace)
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);

                    // File name
                    if (!fileName.Contains(find))
                    {
                        continue;
                    }


                    var newFileName = fileName.Replace(find, replace);
                    if (Path.GetDirectoryName(file) is not string fileDir)
                    {
                        continue;
                    }
                    var newFile = Path.Combine(fileDir, newFileName);
                    File.Move(file, newFile);

                    Console.WriteLine($"Renamed {file} to {newFileName}");
                }
            }

            static void RenameDirectory(string dir, string dirName, string find, string replace)
            {
                if (!dirName.Contains(find))
                {
                    return;
                }

                var newDirName = dirName.Replace(find, replace);
                if (Directory.GetParent(dir)?.FullName is not string parentDir)
                {
                    return;
                }

                var newDirLocation = Path.Combine(parentDir, newDirName);
                Directory.Move(dir, newDirLocation);

                Console.WriteLine($"Renamed {dir} to {newDirLocation}");
            }
        }
    }
}
