using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace ExceptionHunter.ILReader
{
    public sealed class ILReader : IEnumerable<ILInstruction>
    {
        #region Static members

        private static Type s_runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
        private static Type s_runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

        private static OpCode[] s_OneByteOpCodes;
        private static OpCode[] s_TwoByteOpCodes;

        static ILReader()
        {
            s_OneByteOpCodes = new OpCode[0x100];
            s_TwoByteOpCodes = new OpCode[0x100];

            foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var opCode = (OpCode)fi.GetValue(null);
                var value = (UInt16)opCode.Value;
                if (value < 0x100)
                {
                    s_OneByteOpCodes[value] = opCode;
                }
                else if ((value & 0xff00) == 0xfe00)
                {
                    s_TwoByteOpCodes[value & 0xff] = opCode;
                }
            }
        }

        #endregion

        private byte[] m_byteArray;
        private IILProvider m_ilProvider;
        private Int32 m_position;
        private ITokenResolver m_resolver;

        public ILReader(MethodBase method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            Type rtType = method.GetType();
            if (rtType != s_runtimeMethodInfoType && rtType != s_runtimeConstructorInfoType)
            {
                throw new ArgumentException("method must be RuntimeMethodInfo or RuntimeConstructorInfo for this constructor.");
            }

            m_ilProvider = new MethodBaseILProvider(method);
            m_resolver = new ModuleScopeTokenResolver(method);
            m_byteArray = m_ilProvider.GetByteArray();
            m_position = 0;
        }

        public ILReader(IILProvider ilProvider, ITokenResolver tokenResolver)
        {
            if (ilProvider == null)
            {
                throw new ArgumentNullException("ilProvider");
            }

            m_resolver = tokenResolver;
            m_ilProvider = ilProvider;
            m_byteArray = m_ilProvider.GetByteArray();
            m_position = 0;
        }

        #region IEnumerable<ILInstruction> Members

        public IEnumerator<ILInstruction> GetEnumerator()
        {
            while (m_position < m_byteArray.Length)
            {
                yield return Next();
            }

            m_position = 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private ILInstruction Next()
        {
            Int32 offset = m_position;
            OpCode opCode;
            Int32 token;

            // read first 1 or 2 bytes as opCode
            Byte code = ReadByte();
            if (code != 0xFE)
            {
                opCode = s_OneByteOpCodes[code];
            }
            else
            {
                code = ReadByte();
                opCode = s_TwoByteOpCodes[code];
            }

            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    return new InlineNoneInstruction(offset, opCode);

                //The operand is an 8-bit integer branch target.
                case OperandType.ShortInlineBrTarget:
                    SByte shortDelta = ReadSByte();
                    return new ShortInlineBrTargetInstruction(offset, opCode, shortDelta);

                //The operand is a 32-bit integer branch target.
                case OperandType.InlineBrTarget:
                    Int32 delta = ReadInt32();
                    return new InlineBrTargetInstruction(offset, opCode, delta);

                //The operand is an 8-bit integer: 001F  ldc.i4.s, FE12  unaligned.
                case OperandType.ShortInlineI:
                    Byte int8 = ReadByte();
                    return new ShortInlineIInstruction(offset, opCode, int8);

                //The operand is a 32-bit integer.
                case OperandType.InlineI:
                    Int32 int32 = ReadInt32();
                    return new InlineIInstruction(offset, opCode, int32);

                //The operand is a 64-bit integer.
                case OperandType.InlineI8:
                    Int64 int64 = ReadInt64();
                    return new InlineI8Instruction(offset, opCode, int64);

                //The operand is a 32-bit IEEE floating point number.
                case OperandType.ShortInlineR:
                    Single float32 = ReadSingle();
                    return new ShortInlineRInstruction(offset, opCode, float32);

                //The operand is a 64-bit IEEE floating point number.
                case OperandType.InlineR:
                    Double float64 = ReadDouble();
                    return new InlineRInstruction(offset, opCode, float64);

                //The operand is an 8-bit integer containing the ordinal of a local variable or an argument
                case OperandType.ShortInlineVar:
                    Byte index8 = ReadByte();
                    return new ShortInlineVarInstruction(offset, opCode, index8);

                //The operand is 16-bit integer containing the ordinal of a local variable or an argument.
                case OperandType.InlineVar:
                    UInt16 index16 = ReadUInt16();
                    return new InlineVarInstruction(offset, opCode, index16);

                //The operand is a 32-bit metadata string token.
                case OperandType.InlineString:
                    token = ReadInt32();
                    return new InlineStringInstruction(offset, opCode, token, m_resolver);

                //The operand is a 32-bit metadata signature token.
                case OperandType.InlineSig:
                    token = ReadInt32();
                    return new InlineSigInstruction(offset, opCode, token, m_resolver);

                //The operand is a 32-bit metadata token.
                case OperandType.InlineMethod:
                    token = ReadInt32();
                    return new InlineMethodInstruction(offset, opCode, token, m_resolver);

                //The operand is a 32-bit metadata token.
                case OperandType.InlineField:
                    token = ReadInt32();
                    return new InlineFieldInstruction(m_resolver, offset, opCode, token);

                //The operand is a 32-bit metadata token.
                case OperandType.InlineType:
                    token = ReadInt32();
                    return new InlineTypeInstruction(offset, opCode, token, m_resolver);

                //The operand is a FieldRef, MethodRef, or TypeRef token.
                case OperandType.InlineTok:
                    token = ReadInt32();
                    return new InlineTokInstruction(offset, opCode, token, m_resolver);

                //The operand is the 32-bit integer argument to a switch instruction.
                case OperandType.InlineSwitch:
                    Int32 cases = ReadInt32();
                    var deltas = new Int32[cases];
                    for (Int32 i = 0; i < cases; i++)
                    {
                        deltas[i] = ReadInt32();
                    }
                    return new InlineSwitchInstruction(offset, opCode, deltas);

                default:
                    throw new BadImageFormatException("unexpected OperandType " + opCode.OperandType);
            }
        }

        public void Accept(ILInstructionVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException("visitor");
            }

            foreach (ILInstruction instruction in this)
            {
                instruction.Accept(visitor);
            }
        }

        #region read in operands

        private Byte ReadByte()
        {
            return m_byteArray[m_position++];
        }

        private SByte ReadSByte()
        {
            return (SByte)ReadByte();
        }

        private UInt16 ReadUInt16()
        {
            int pos = m_position;
            m_position += 2;
            return BitConverter.ToUInt16(m_byteArray, pos);
        }

        private Int32 ReadInt32()
        {
            int pos = m_position;
            m_position += 4;
            return BitConverter.ToInt32(m_byteArray, pos);
        }

        private Int64 ReadInt64()
        {
            int pos = m_position;
            m_position += 8;
            return BitConverter.ToInt64(m_byteArray, pos);
        }

        private Single ReadSingle()
        {
            int pos = m_position;
            m_position += 4;
            return BitConverter.ToSingle(m_byteArray, pos);
        }

        private Double ReadDouble()
        {
            int pos = m_position;
            m_position += 8;
            return BitConverter.ToDouble(m_byteArray, pos);
        }

        #endregion
    }
}