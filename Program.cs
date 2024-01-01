namespace Renamer;

internal class Program
{
    private static readonly string[] SkipDirs = new[]
    {
        ".git",
        ".vs",
        "bin",
        "obj",
    };

    public static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: Renamer <directory> <find> <replace>");
            return;
        }

        var dir = args[0];
        if (!Directory.Exists(dir))
        {
            throw new Exception($"Directory {dir} does not exist");
        }

        var find = args[1];

        if (string.IsNullOrEmpty(find))
        {
            Console.WriteLine("'find' parameter cannot be empty");
            return;
        }

        var replace = args[2];

        if (find == replace)
        {
            Console.WriteLine("'find' and 'replace' are equal: nothing to do.");
            return;
        }

        Console.WriteLine($"Dir: {dir}\n----------------------------");

        Console.WriteLine($"\n1. Exact match: '{find}' -> '{replace}'\n");

        // Rename exact case
        Rename(dir, find, replace);

        var findLower = find.ToLowerInvariant();
        var replaceLower = replace.ToLowerInvariant();

        if (findLower == find && replaceLower == replace)
        {
            return;
        }

        Console.WriteLine($"\n2. Lower case: '{findLower}' -> '{replaceLower}'\n");

        // Rename lower cased
        Rename(dir, findLower, replaceLower);

        Console.WriteLine("\nDone!");
    }

    private static void Rename(string dir, string find, string replace)
    {
        var dirName = Path.GetFileName(dir);
        if (SkipDirs.Contains(dirName))
        {
            return;
        }

        RenameSubDirs(dir, find, replace);

        var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
        ReplaceInFiles(files, find, replace);
        RenameFiles(files, find, replace);

        RenameDirectory(dir, dirName, find, replace);

        static void RenameSubDirs(string dir, string find, string replace)
        {
            var subDirectories = Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly);
            foreach (var subDir in subDirectories)
            {
                Rename(subDir, find, replace);
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
