using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// A template for nodes that add a sequence of attributes to an inner node.
    /// </summary>
    public class AttributeNodeTemplate : NodeTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.AttributeNodeTemplate"/> class.
        /// </summary>
        /// <param name="attributeCount">The number of attributes to attach to a node.</param>
        public AttributeNodeTemplate(int attributeCount)
        {
            argCount = attributeCount;
        }

        private int argCount;

        /// <inheritdoc/>
        public override int ArgumentCount
        {
            get { return argCount + 1; }
        }

        /// <inheritdoc/>
        public override LNode Instantiate(ReaderState State, IEnumerable<LNode> Arguments)
        {
            return Arguments.First().WithAttrs(Arguments.Skip(1).ToArray());
        }

        /// <summary>
        /// Reads an attribute list node template definition.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static AttributeNodeTemplate Read(LoycBinaryReader Reader)
        {
            return new AttributeNodeTemplate((int)Reader.ReadULeb128());
        }

        /// <inheritdoc/>
        public override NodeTemplateType TemplateType
        {
            get { return NodeTemplateType.AttributeNode; }
        }

        /// <summary>
        /// Writes an attribute list node template definition.
        /// </summary>
        /// <param name="Writer"></param>
        public override void Write(LoycBinaryWriter Writer)
        {
            Writer.WriteULeb128((uint)argCount);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is AttributeNodeTemplate && argCount == ((AttributeNodeTemplate)obj).argCount;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (int)TemplateType << 5 ^ argCount;
        }
    }
}
