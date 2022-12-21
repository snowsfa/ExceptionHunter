using System.Reflection.Emit;

namespace ExceptionHunter.ILReader
{
    public abstract class ILInstruction
    {
        protected Int32 m_offset;
        protected OpCode m_opCode;

        public Int32 Offset
        {
            get
            {
                return m_offset;
            }
        }

        public OpCode OpCode
        {
            get
            {
                return m_opCode;
            }
        }

        internal ILInstruction(Int32 offset, OpCode opCode)
        {
            m_offset = offset;
            m_opCode = opCode;
        }

        public abstract void Accept(ILInstructionVisitor vistor);
    }
}