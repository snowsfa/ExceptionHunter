using System.Diagnostics;
using System.Reflection;

namespace ExceptionHunter
{
    [Serializable]
    class AssemblyEnumerator
    {
        private readonly string _filePath;
        private readonly Assembly _assembly;

        public AssemblyEnumerator(string filePath)
        {
            _filePath = filePath;
            _assembly = Assembly.LoadFrom(filePath);
        }

        public string FilePath
        {
            get
            {
                return _filePath;
            }
        }

        public IEnumerable<string> NameSpaces
        {
            get
            {
                try
                {
                    Type[] types = _assembly.GetTypes();
                    return types.Select(t => t.Namespace).Distinct();
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (Exception? loaderException in e.LoaderExceptions)
                    {
                        Debug.Print(loaderException.ToString());
                    }

                    return Enumerable.Empty<string>();
                }
            }
        }

        public IEnumerable<Type> GetClasses(string nameSpace)
        {
            return _assembly.GetTypes().Where(t => !t.IsInterface && String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }

        public IEnumerable<MethodInfo> GetMethodds(Type classType)
        {
            return classType.GetMethods();
        }
    }
}
