namespace ExceptionHunter.ILReader
{
    public interface IILStringCollector
    {
        void Process(ILInstruction ilInstruction, string operandString);
    }
}