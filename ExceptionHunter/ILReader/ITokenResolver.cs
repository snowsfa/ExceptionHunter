using System;
using System.Reflection;

namespace ExceptionHunter.ILReader
{
    public interface ITokenResolver
    {
        MethodBase AsMethod(int token);
        FieldInfo AsField(int token);
        Type AsType(int token);
        String AsString(int token);
        MemberInfo AsMember(int token);
        byte[] AsSignature(int token);
    }
}