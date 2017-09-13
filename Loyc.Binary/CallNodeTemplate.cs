using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// A template for a call node.
    /// </summary>
    public class CallNodeTemplate : NodeTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.CallNodeTemplate"/> class.
        /// </summary>
        /// <param name="callArgumentCount">The number of arguments taken by the call.</param>
        public CallNodeTemplate(int callArgumentCount)
        {
            callArgCount = callArgumentCount;
        }

        private int callArgCount;

        /// <inheritdoc/>
        public override int ArgumentCount
        {
            get { return callArgCount + 1; }
        }

        /// <inheritdoc/>
        public override NodeTemplateType TemplateType
        {
            get { return NodeTemplateType.CallNode; }
        }

        /// <inheritdoc/>
        public override LNode Instantiate(ReaderState State, IEnumerable<LNode> Arguments)
        {
            return State.NodeFactory.Call(Arguments.First(), Arguments.Skip(1));
        }

        /// <summary>
        /// Reads a call node template definition.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static CallNodeTemplate Read(LoycBinaryReader Reader)
        {
            return new CallNodeTemplate((int)Reader.ReadULeb128());
        }

        /// <summary>
        /// Writes a call node template definition.
        /// </summary>
        /// <param name="Writer"></param>
        public override void Write(LoycBinaryWriter Writer)
        {
            Writer.WriteULeb128((uint)callArgCount);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is CallNodeTemplate && callArgCount == ((CallNodeTemplate)obj).callArgCount;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (int)TemplateType << 5 ^ callArgCount;
        }
    }
}
