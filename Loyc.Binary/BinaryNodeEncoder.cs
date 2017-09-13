using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// Defines a node encoder for the binary loyc tree format.
    /// </summary>
    public class BinaryNodeEncoder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.BinaryNodeEncoder"/> class
        /// that encodes the given encoding type using the given function.
        /// </summary>
        /// <param name="EncodingType">The encoding type.</param>
        /// <param name="GetTemplate">A function that figures out what the right template for a node is.</param>
        /// <param name="Encode">The function that encodes nodes.</param>
        public BinaryNodeEncoder(
            NodeEncodingType EncodingType,
            Func<WriterState, LNode, NodeTemplate> GetTemplate,
            Action<LoycBinaryWriter, WriterState, LNode> Encode)
        {
            this.EncodingType = EncodingType;
            this.GetTemplate = GetTemplate;
            this.Encode = Encode;
        }

        /// <summary>
        /// Gets the encoder's encoding type.
        /// </summary>
        public NodeEncodingType EncodingType { get; private set; }

        /// <summary>
        /// Gets the template for the given node, or null if the node does not need a template.
        /// </summary>
        public Func<WriterState, LNode, NodeTemplate> GetTemplate { get; private set; }

        /// <summary>
        /// Encodes a given node.
        /// </summary>
        public Action<LoycBinaryWriter, WriterState, LNode> Encode { get; private set; }

        /// <summary>
        /// Creates a binary node encoder that encodes literals of a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Encoding"></param>
        /// <param name="ValueEncoder"></param>
        /// <returns></returns>
        public static BinaryNodeEncoder CreateLiteralEncoder<T>(NodeEncodingType Encoding, Action<BinaryWriter, T> ValueEncoder)
        {
            return new BinaryNodeEncoder(
                Encoding,
                (state, node) => null,
                (writer, state, node) => ValueEncoder(writer.Writer, (T)node.Value));
        }

        /// <summary>
        /// Creates a binary node encoder that encodes literals of a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Encoding"></param>
        /// <param name="ValueEncoder"></param>
        /// <returns></returns>
        public static BinaryNodeEncoder CreateLiteralEncoder<T>(NodeEncodingType Encoding, Action<LoycBinaryWriter, WriterState, T> ValueEncoder)
        {
            return new BinaryNodeEncoder(
                Encoding,
                (state, node) => null,
                (writer, state, node) => ValueEncoder(writer, state, (T)node.Value));
        }

        /// <summary>
        /// Gets the binary node encoder for id nodes.
        /// </summary>
        public static readonly BinaryNodeEncoder IdEncoder = 
            new BinaryNodeEncoder(
                NodeEncodingType.IdNode,
                (state, node) => null,
                (writer, state, node) => writer.WriteReference(state, node.Name));

        /// <summary>
        /// Gets the binary node encoder for null literals.
        /// </summary>
        public static readonly BinaryNodeEncoder NullEncoder =
            new BinaryNodeEncoder(
                NodeEncodingType.Null,
                (state, node) => null,
                (writer, state, node) => { });

        /// <summary>
        /// Gets the binary node encoder for attribute literals.
        /// </summary>
        public static readonly BinaryNodeEncoder AttributeEncoder =
            new BinaryNodeEncoder(
                NodeEncodingType.TemplatedNode,
                (state, node) => new AttributeNodeTemplate(node.AttrCount),
                (writer, state, node) =>
                {
                    var template = new AttributeNodeTemplate(node.AttrCount);
                    writer.WriteReference(state, node.WithoutAttrs());
                    foreach (var attr in node.Attrs)
                    {
                        writer.WriteReference(state, attr);
                    }
                });

        /// <summary>
        /// Gets the binary node encoder for call nodes whose target is not an id node.
        /// </summary>
        public static readonly BinaryNodeEncoder CallEncoder =
            new BinaryNodeEncoder(NodeEncodingType.TemplatedNode,
                (state, node) => new CallNodeTemplate(node.ArgCount),
                (writer, state, node) =>
                {
                    var template = new CallNodeTemplate(node.ArgCount);
                    writer.WriteReference(state, node.Target);
                    foreach (var arg in node.Args)
                    {
                        writer.WriteReference(state, arg);
                    }
                });

        /// <summary>
        /// Gets the binary node encoder for call nodes whose target is an id node.
        /// </summary>
        public static readonly BinaryNodeEncoder CallIdEncoder =
            new BinaryNodeEncoder(NodeEncodingType.TemplatedNode,
                (state, node) => new CallIdNodeTemplate(state.GetIndex(node.Target.Name), node.ArgCount),
                (writer, state, node) =>
                {
                    int nodeTarget = state.GetIndex(node.Target.Name);
                    var template = new CallIdNodeTemplate(nodeTarget, node.ArgCount);
                    foreach (var arg in node.Args)
                    {
                        writer.WriteReference(state, arg);
                    }
                });
    }
}
