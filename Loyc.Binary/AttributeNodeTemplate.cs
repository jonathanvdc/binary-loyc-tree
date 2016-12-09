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
        /// Initializes a new instance of the <see cref="Loyc.Binary.AttributeNodeTemplate"/> class
        /// from a non-empty list of template argument types. The first of these types
        /// is interpreted as the target node encoding, the remainder is interpreted
        /// as a list of encodings for the attribute nodes.
        /// </summary>
        /// <param name="templateArgumentTypes">Template argument types.</param>
        public AttributeNodeTemplate(IReadOnlyList<NodeEncodingType> templateArgumentTypes)
        {
            Debug.Assert(Enumerable.Any(templateArgumentTypes));

            argTypes = templateArgumentTypes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.AttributeNodeTemplate"/> 
        /// class that attaches a list of attributes encoded with the given
        /// list of encodings to a node encoded with the given encoding.
        /// </summary>
        /// <param name="attributeTargetType">The encoding to encode the target with.</param>
        /// <param name="attributeArgumentTypes">The encoding to encode the arguments with..</param>
        public AttributeNodeTemplate(
            NodeEncodingType attributeTargetType, 
            IReadOnlyList<NodeEncodingType> attributeArgumentTypes)
        {
            var argTys = new List<NodeEncodingType>();
            argTys.Add(attributeTargetType);
            argTys.AddRange(attributeArgumentTypes);
            argTypes = argTys;
        }

        private IReadOnlyList<NodeEncodingType> argTypes;

        /// <inheritdoc/>
        public override IReadOnlyList<NodeEncodingType> ArgumentTypes
        {
            get { return argTypes; }
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
            return new AttributeNodeTemplate(Reader.ReadList(Reader.ReadEncodingType));
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
            Writer.WriteList(ArgumentTypes, Writer.WriteEncodingType);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is AttributeNodeTemplate && ArgumentTypes.SequenceEqual(((AttributeNodeTemplate)obj).ArgumentTypes);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int result = (int)TemplateType;
            foreach (var item in ArgumentTypes)
            {
                result = (result << 1) ^ (int)item;
            }
            return result;
        }
    }
}
