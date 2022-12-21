using System.Reflection;

namespace ExceptionHunter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage ExceptionHunter.exe [assemblyPath]");
                return;
            }

            string filePath = args[0];

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found");
                return;
            }

            EnumerateAssembly(filePath);
        }

        private static void EnumerateAssembly(string filePath)
        {
            AssemblyEnumerator? assemblyEnumerator = new(filePath);

            foreach (string? currentNameSpace in assemblyEnumerator.NameSpaces)
            {
                if (string.IsNullOrEmpty(currentNameSpace))
                {
                    continue;
                }

                foreach (Type? classType in assemblyEnumerator.GetClasses(currentNameSpace))
                {
                    if (classType == null)
                    {
                        continue;
                    }

                    foreach (MethodInfo? method in assemblyEnumerator.GetMethodds(classType))
                    {
                        if (method != null && !method.IsGenericMethod)
                        {
                            if (method.GetMethodBase() != null && method.HasAnyExceptions())
                            {
                                string methodName = method.GetFriendlyName();
                                if (methodName == null)
                                {
                                    continue;
                                }

                                Console.WriteLine($"\n{currentNameSpace}.{classType.Name}.{method.GetFriendlyName()}");

                                foreach (Type? exception in method.GetAllExceptions())
                                {
                                    if (exception != null)
                                    {
                                        Console.WriteLine($"    {exception.FullName}");
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

    }
}