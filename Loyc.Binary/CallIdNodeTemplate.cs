using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// A template for call nodes that have an id node as their target.
    /// </summary>
    public class CallIdNodeTemplate : NodeTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.CallIdNodeTemplate"/> class
        /// that encodes a call to the symbol with the specified index.
        /// </summary>
        /// <param name="targetSymbolIndex">The index in the symbol table to which a call is built..</param>
        /// <param name="argumentCount">The number of argument nodes.</param>
        public CallIdNodeTemplate(
            int targetSymbolIndex, int argumentCount)
        {
            TargetSymbolIndex = targetSymbolIndex;
            argCount = argumentCount;
        }

        /// <summary>
        /// Gets the index in the symbol table of the symbol
        /// to which this node is a call.
        /// </summary>
        /// <value>The index of the target symbol.</value>
        public int TargetSymbolIndex { get; private set; }

        private int argCount;

        /// <inheritdoc/>
        public override int ArgumentCount
        {
            get { return argCount; }
        }

        /// <inheritdoc/>
        public override LNode Instantiate(ReaderState State, IEnumerable<LNode> Arguments)
        {
            return State.NodeFactory.Call(State.SymbolTable[TargetSymbolIndex], Arguments);
        }

        /// <summary>
        /// Reads a call id node template definition.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static CallIdNodeTemplate Read(LoycBinaryReader Reader)
        {
            int symbolIndex = (int)Reader.ReadULeb128();
            int argCount = (int)Reader.ReadULeb128();
            return new CallIdNodeTemplate(symbolIndex, argCount);
        }

        /// <inheritdoc/>
        public override NodeTemplateType TemplateType
        {
            get { return NodeTemplateType.CallIdNode; }
        }

        /// <summary>
        /// Writes a call node template definition.
        /// </summary>
        /// <param name="Writer"></param>
        public override void Write(LoycBinaryWriter Writer)
        {
            Writer.WriteULeb128((uint)TargetSymbolIndex);
            Writer.WriteULeb128((uint)argCount);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var other = obj as CallIdNodeTemplate;
            return other != null &&
                   this.TargetSymbolIndex == other.TargetSymbolIndex &&
                   this.ArgumentCount == other.ArgumentCount;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ((int)TemplateType << 24) ^ ((int)TargetSymbolIndex << 3) ^ ArgumentCount;
        }
    }
}
