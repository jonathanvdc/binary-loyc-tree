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
        /// Arguments are encoded according to the given list of argument
        /// type encodings.
        /// </summary>
        /// <param name="targetSymbolIndex">The index in the symbol table to which a call is built..</param>
        /// <param name="argumentTypes">The encodings of the argument nodes.</param>
        public CallIdNodeTemplate(
            int targetSymbolIndex, 
            IReadOnlyList<NodeEncodingType> argumentTypes)
        {
            TargetSymbolIndex = targetSymbolIndex;
            argTypes = argumentTypes;
        }

        /// <summary>
        /// Gets the index in the symbol table of the symbol
        /// to which this node is a call.
        /// </summary>
        /// <value>The index of the target symbol.</value>
        public int TargetSymbolIndex { get; private set; }

        private IReadOnlyList<NodeEncodingType> argTypes;

        /// <inheritdoc/>
        public override IReadOnlyList<NodeEncodingType> ArgumentTypes
        {
            get { return argTypes; }
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
            int symbolIndex = Reader.Reader.ReadInt32();
            var types = Reader.ReadList(Reader.ReadEncodingType);
            return new CallIdNodeTemplate(symbolIndex, types);
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
            Writer.Writer.Write(TargetSymbolIndex);
            Writer.WriteList(ArgumentTypes, Writer.WriteEncodingType);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var other = obj as CallIdNodeTemplate;
            return other != null && 
                   this.TargetSymbolIndex == other.TargetSymbolIndex && 
                   this.ArgumentTypes.SequenceEqual(other.ArgumentTypes);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int result = (int)TemplateType ^ TargetSymbolIndex;
            foreach (var item in ArgumentTypes)
            {
                result = (result << 1) ^ (int)item;
            }
            return result;
        }
    }
}
