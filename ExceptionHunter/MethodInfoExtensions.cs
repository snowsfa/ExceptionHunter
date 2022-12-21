using System.Reflection;
using System.Text;

namespace ExceptionHunter
{
    public static class MethodInfoExtensions
    {
        public static string GetFriendlyName(this MethodInfo methodInfo)
        {
            try
            {
                StringBuilder sb = new();
                sb.AppendFormat("{0}(", methodInfo.Name);
                List<string> parameterDescriptions = new();

                foreach (ParameterInfo parameter in methodInfo.GetParameters())
                {
                    parameterDescriptions.Add(string.Format("{0} {1}", parameter.GetType().Name, parameter.Name));
                }

                sb.Append(string.Join(", ", parameterDescriptions));
                sb.Append(')');

                return sb.ToString();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static MethodBase GetMethodBase(this MethodInfo method)
        {
            try
            {
                return MethodBase.GetMethodFromHandle(method.MethodHandle);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
