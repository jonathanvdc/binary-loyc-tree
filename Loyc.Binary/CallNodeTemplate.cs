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
        /// Initializes a new instance of the <see cref="Loyc.Binary.CallNodeTemplate"/> class
        /// from the given non-empty list of template argument node encodings. The first encoding
        /// is interpreted as the call target's encoding. The remainder of the encoding list
        /// describes how the call's argument nodes are encoded.
        /// </summary>
        /// <param name="templateArgumentTypes">The list of template argument encodings.</param>
        public CallNodeTemplate(IReadOnlyList<NodeEncodingType> templateArgumentTypes)
        {
            Debug.Assert(Enumerable.Any(templateArgumentTypes));

            argTypes = templateArgumentTypes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.CallNodeTemplate"/> class
        /// from the specified call target and call argument encodings.
        /// </summary>
        /// <param name="callTargetType">The encoding of the call target node.</param>
        /// <param name="callArgumentTypes">The encoding of the call's arguments.</param>
        public CallNodeTemplate(
            NodeEncodingType callTargetType, 
            IReadOnlyList<NodeEncodingType> callArgumentTypes)
        {
            var argTyList = new List<NodeEncodingType>();
            argTyList.Add(callTargetType);
            argTyList.AddRange(callArgumentTypes);
            argTypes = argTyList;
        }

        private IReadOnlyList<NodeEncodingType> argTypes;

        /// <inheritdoc/>
        public override IReadOnlyList<NodeEncodingType> ArgumentTypes
        {
            get { return argTypes; }
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
            return new CallNodeTemplate(Reader.ReadList(Reader.ReadEncodingType));
        }

        /// <summary>
        /// Writes a call node template definition.
        /// </summary>
        /// <param name="Writer"></param>
        public override void Write(LoycBinaryWriter Writer)
        {
            Writer.WriteList(ArgumentTypes, Writer.WriteEncodingType);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is CallNodeTemplate && ArgumentTypes.SequenceEqual(((CallNodeTemplate)obj).ArgumentTypes);
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
