using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Loyc.Binary
{
    /// <summary>
    /// A type that writes binary encoded loyc trees.
    /// </summary>
    public class LoycBinaryWriter : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.LoycBinaryWriter"/> class.
        /// Data is written to the given binary writer, and literals are encoded using
        /// the given map of encoding functions. 
        /// </summary>
        /// <param name="writer">The binary writer to write data to.</param>
        /// <param name="encoders">A map of literal types to literal encoders.</param>
        public LoycBinaryWriter(
            BinaryWriter writer, 
            IReadOnlyDictionary<Type, BinaryNodeEncoder> encoders)
        {
            Writer = writer;
            LiteralEncoders = encoders;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.LoycBinaryWriter"/> class.
        /// Data is written to the given output stream, and literals are encoded using
        /// the given map of encoding functions. 
        /// </summary>
        /// <param name="outputStream">The output stream to write data to.</param>
        /// <param name="encoders">A map of literal types to literal encoders.</param>
        public LoycBinaryWriter(Stream outputStream, IReadOnlyDictionary<Type, BinaryNodeEncoder> encoders)
            : this(new BinaryWriter(outputStream), encoders)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.LoycBinaryWriter"/> class.
        /// Data is written to the given binary writer, and nodes are encoded using
        /// the given BLT writer's encoders.
        /// </summary>
        /// <param name="writer">The binary writer to write data to.</param>
        /// <param name="other">The BLT writer whose encodings are to be used.</param>
        public LoycBinaryWriter(BinaryWriter writer, LoycBinaryWriter other)
            : this(writer, other.LiteralEncoders)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.LoycBinaryWriter"/> class.
        /// Data is written to the given output stream, and nodes are encoded using
        /// the given BLT writer's encoders.
        /// </summary>
        /// <param name="outputStream">The output stream to write data to.</param>
        /// /// <param name="other">The BLT writer whose encodings are to be used.</param>
        public LoycBinaryWriter(Stream outputStream, LoycBinaryWriter other)
            : this(new BinaryWriter(outputStream), other)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.LoycBinaryWriter"/> class.
        /// Data is written to the given binary writer, and nodes are encoded using the
        /// default encoders.
        /// </summary>
        /// <param name="writer">The binary writer to write data to.</param>
        public LoycBinaryWriter(BinaryWriter writer)
            : this(writer, DefaultEncoders)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Loyc.Binary.LoycBinaryWriter"/> class.
        /// Data is written to the given output stream, and nodes are encoded using the
        /// default encoders.
        /// </summary>
        /// <param name="outputStream">The output stream to write data to.</param>
        public LoycBinaryWriter(Stream outputStream)
            : this(new BinaryWriter(outputStream))
        { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the writer to the underlying stream of this instance.
        /// </summary>
        public BinaryWriter Writer { get; private set; }

        /// <summary>
        /// Gets the set of literal node encoders this writer uses.
        /// </summary>
        public IReadOnlyDictionary<Type, BinaryNodeEncoder> LiteralEncoders { get; private set; }

        #endregion

        #region Static

        /// <summary>
        /// Gets the default set of encoders.
        /// </summary>
        public static IReadOnlyDictionary<Type, BinaryNodeEncoder> DefaultEncoders
        {
            get
            {
                return new Dictionary<Type, BinaryNodeEncoder>()
                {
                    { typeof(sbyte), BinaryNodeEncoder.CreateLiteralEncoder<sbyte>(NodeEncodingType.Int8, (writer, value) => writer.Write(value)) },
                    { typeof(short), BinaryNodeEncoder.CreateLiteralEncoder<short>(NodeEncodingType.Int16, (writer, value) => writer.Write(value)) },
                    { typeof(int), BinaryNodeEncoder.CreateLiteralEncoder<int>(NodeEncodingType.Int32, (writer, value) => writer.Write(value)) },
                    { typeof(long), BinaryNodeEncoder.CreateLiteralEncoder<long>(NodeEncodingType.Int64, (writer, value) => writer.Write(value)) },

                    { typeof(byte), BinaryNodeEncoder.CreateLiteralEncoder<byte>(NodeEncodingType.UInt8, (writer, value) => writer.Write(value)) },
                    { typeof(ushort), BinaryNodeEncoder.CreateLiteralEncoder<ushort>(NodeEncodingType.UInt16, (writer, value) => writer.Write(value)) },
                    { typeof(uint), BinaryNodeEncoder.CreateLiteralEncoder<uint>(NodeEncodingType.UInt32, (writer, value) => writer.Write(value)) },
                    { typeof(ulong), BinaryNodeEncoder.CreateLiteralEncoder<ulong>(NodeEncodingType.UInt64, (writer, value) => writer.Write(value)) },

                    { typeof(float), BinaryNodeEncoder.CreateLiteralEncoder<float>(NodeEncodingType.Float32, (writer, value) => writer.Write(value)) },
                    { typeof(double), BinaryNodeEncoder.CreateLiteralEncoder<double>(NodeEncodingType.Float64, (writer, value) => writer.Write(value)) },
                    { typeof(decimal), BinaryNodeEncoder.CreateLiteralEncoder<decimal>(NodeEncodingType.Decimal, (writer, value) => writer.Write(value)) },

                    { typeof(char), BinaryNodeEncoder.CreateLiteralEncoder<char>(NodeEncodingType.Char, (writer, value) => writer.Write(value)) },
                    { typeof(bool), BinaryNodeEncoder.CreateLiteralEncoder<bool>(NodeEncodingType.Boolean, (writer, value) => writer.Write(value)) },
                    { typeof(string), BinaryNodeEncoder.CreateLiteralEncoder<string>(NodeEncodingType.String, (writer, state, value) => writer.WriteReference(state, value)) },
                    { 
                        typeof(BigInteger), 
                        BinaryNodeEncoder.CreateLiteralEncoder<BigInteger>(
                            NodeEncodingType.BigInteger, (writer, state, value) => writer.WriteBigInteger(value)) 
                    },
                    { typeof(@void), new BinaryNodeEncoder(NodeEncodingType.Void, (state, node) => null, (writer, state, node) => { }) }
                };
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Writes a LEB128 variable-length unsigned integer to the output stream.
        /// </summary>
        /// <param name="Value"></param>
        public void WriteULeb128(uint Value)
        {
            // C# translation of code borrowed from Wikipedia article:
            // https://en.wikipedia.org/wiki/LEB128
            do
            {
                byte b = (byte)(Value & 0x7F);
                Value >>= 7;
                if (Value != 0) /* more bytes to come */
                    b |= 0x80;
                Writer.Write(b);
            } while (Value != 0);
        }

        /// <summary>
        /// Writes a BigInteger to the output stream.
        /// </summary>
        /// <param name="Value">
        /// The BigInteger to write to the output stream.
        /// </param>
        public void WriteBigInteger(BigInteger Value)
        {
            byte[] byteArr = Value.ToByteArray();
            // First, write a prefix that encodes
            // the size of the BigInteger's byte array.
            WriteULeb128((uint)byteArr.Length);
            // Then, write the byte array itself.
            Writer.Write(Value.ToByteArray());
        }

        /// <summary>
        /// Writes the given list of items to the output stream.
        /// The resulting data is length-prefixed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Items"></param>
        /// <param name="WriteItem"></param>
        public void WriteList<T>(IReadOnlyList<T> Items, Action<T> WriteItem)
        {
            WriteULeb128((uint)Items.Count);
            WriteListContents(Items, WriteItem);
        }

        /// <summary>
        /// Writes the contents of the given list of items to the output stream.
        /// The resulting data is unprefixed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Items"></param>
        /// <param name="WriteItem"></param>
        public void WriteListContents<T>(IReadOnlyList<T> Items, Action<T> WriteItem)
        {
            foreach (var item in Items)
            {
                WriteItem(item);
            }
        }

        /// <summary>
        /// Writes the given encoding type to the output stream.
        /// </summary>
        /// <param name="Encoding"></param>
        public void WriteEncodingType(NodeEncodingType Encoding)
        {
            Writer.Write((byte)Encoding);
        }

        /// <summary>
        /// Writes the given template type to the output stream.
        /// </summary>
        /// <param name="Type"></param>
        public void WriteTemplateType(NodeTemplateType Type)
        {
            Writer.Write((byte)Type);
        }

        /// <summary>
        /// Writes a reference to the given symbol.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Value"></param>
        public void WriteReference(WriterState State, Symbol Value)
        {
            WriteULeb128((uint)State.GetIndex(Value));
        }

        /// <summary>
        /// Writes a reference to the given string.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Value"></param>
        public void WriteReference(WriterState State, string Value)
        {
            WriteULeb128((uint)State.GetIndex(Value));
        }

        /// <summary>
        /// Writes a reference to the given node template.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Value"></param>
        public void WriteReference(WriterState State, NodeTemplate Value)
        {
            WriteULeb128((uint)State.GetIndex(Value));
        }

        #endregion

        #region Node Writing

        /// <summary>
        /// Writes a reference to the given node.
        /// </summary>
        /// <param name="State">The writer state.</param>
        /// <param name="Node">The node to reference.</param>
        public void WriteReference(WriterState State, LNode Node)
        {
            WriteULeb128((uint)State.GetIndex(Node));
        }

        /// <summary>
        /// Writes the given writer state's node table to the output stream.
        /// </summary>
        /// <param name="State">The writer state.</param>
        public void WriteNodeTable(
            WriterState State)
        {
            var nodeTable = State.Nodes;
            WriteULeb128((uint)nodeTable.Count);
            foreach (var subTable in nodeTable)
            {
                WriteULeb128((uint)subTable.Item2.Count);
                WriteEncodingType(subTable.Item1);
                foreach (var node in subTable.Item2)
                {
                    State.GetEncoder(node).Encode(this, State, node);
                }
            }
        }

        /// <summary>
        /// Categorizes the given node and its descendants into clusters of literal
        /// and id types.
        /// </summary>
        /// <param name="Node">The node to categorize.</param>
        /// <param name="Literals">A dictionary that maps literal types to nodes.</param>
        /// <param name="NullLiterals">A list of null literal nodes.</param>
        /// <param name="IdNodes">A list of id nodes.</param>
        private static void ClusterLiteralsByType(
            LNode Node,
            Dictionary<Type, List<LNode>> Literals,
            List<LNode> NullLiterals,
            List<LNode> IdNodes)
        {
            if (Node.HasAttrs || Node.IsCall)
            {
                // We can't assign this node to a cluster, but we
                // may be able to assign its child nodes to a cluster.
                if (Node.HasAttrs)
                {
                    foreach (var attr in Node.Attrs)
                    {
                        ClusterLiteralsByType(attr, Literals, NullLiterals, IdNodes);
                    }
                    ClusterLiteralsByType(Node.WithoutAttrs(), Literals, NullLiterals, IdNodes);
                }
                else if (Node.IsCall)
                {
                    ClusterLiteralsByType(Node.Target, Literals, NullLiterals, IdNodes);
                    foreach (var args in Node.Args)
                    {
                        ClusterLiteralsByType(args, Literals, NullLiterals, IdNodes);
                    }
                }
                return;
            }

            List<LNode> nodeList;
            if (Node.IsLiteral)
            {
                if (Node.Value == null)
                {
                    nodeList = NullLiterals;
                }
                else
                {
                    var literalType = Node.Value.GetType();
                    if (!Literals.TryGetValue(literalType, out nodeList))
                    {
                        nodeList = new List<LNode>();
                        Literals[literalType] = nodeList;
                    }
                }
            }
            else if (Node.IsId)
            {
                nodeList = IdNodes;
            }
            else
            {
                // This should never happen, but we might as well handle it
                // gracefully.
                return;
            }

            nodeList.Add(Node);
        }

        /// <summary>
        /// Adds a list of nodes to the node table of a writer state.
        /// </summary>
        /// <param name="State">The writer state to populate.</param>
        /// <param name="Nodes">A list of nodes to add.</param>
        public static void AddToNodeTable(
            WriterState State, IReadOnlyList<LNode> Nodes)
        {
            // We could naively call `State.GetIndex(node);` on every node
            // in `Nodes` and that'd work fine. But doing so would create
            // lots of small sub-tables in the node table, and each of these
            // tables has at least two bytes of overhead.
            //
            // We can improve on this situation by first adding all literals
            // and id nodes and only then adding the top-level nodes. Doing
            // so will create one table per literal/id type and one table
            // for the templated nodes.

            // First, cluster all literal and id nodes.
            var literals = new Dictionary<Type, List<LNode>>();
            var nullLiterals = new List<LNode>();
            var idNodes = new List<LNode>();
            foreach (var node in Nodes)
            {
                ClusterLiteralsByType(node, literals, nullLiterals, idNodes);
            }

            // Add clustered nodes to the node table.
            foreach (var node in nullLiterals)
            {
                State.GetIndex(node);
            }
            foreach (var node in idNodes)
            {
                State.GetIndex(node);
            }
            foreach (var kvPair in literals)
            {
                foreach (var node in kvPair.Value)
                {
                    State.GetIndex(node);
                }
            }

            // Add all other nodes to the node table.
            foreach (var node in Nodes)
            {
                State.GetIndex(node);
            }
        }

        #endregion

        #region Header Writing

        /// <summary>
        /// Writes a symbol to the output stream.
        /// </summary>
        /// <returns></returns>
        public void WriteSymbol(string Symbol)
        {
            byte[] data = UTF8Encoding.UTF8.GetBytes(Symbol);
            WriteULeb128((uint)data.Length);
            Writer.Write(data);
        }

        /// <summary>
        /// Writes the given string table to the output stream.
        /// </summary>
        /// <param name="Table"></param>
        public void WriteSymbolTable(IReadOnlyList<string> Table)
        {
            WriteList(Table, WriteSymbol);
        }

        /// <summary>
        /// Writes the given template definition to the output stream,
        /// prefixed by its template type.
        /// </summary>
        /// <param name="Template"></param>
        public void WriteTemplateDefinition(NodeTemplate Template)
        {
            WriteTemplateType(Template.TemplateType);
            Template.Write(this);
        }

        /// <summary>
        /// Writes the given template table to the output stream.
        /// </summary>
        /// <param name="Table"></param>
        public void WriteTemplateTable(IReadOnlyList<NodeTemplate> Table)
        {
            WriteList(Table, WriteTemplateDefinition);
        }

        /// <summary>
        /// Writes the given header to the output stream.
        /// </summary>
        /// <param name="Header"></param>
        public void WriteHeader(WriterState Header)
        {
            WriteSymbolTable(Header.Symbols);
            WriteTemplateTable(Header.Templates);
        }

        #endregion

        #region File Writing

        /// <summary>
        /// Writes the magic string to the output stream.
        /// </summary>
        public void WriteMagic()
        {
            Writer.Write(LoycBinaryHelpers.Magic.Select(Convert.ToByte).ToArray());
        }

        /// <summary>
        /// Writes the latest BLT version number to the output stream.
        /// </summary>
        public void WriteVersion()
        {
            WriteVersion(LoycBinaryHelpers.MajorVersionNumber, LoycBinaryHelpers.MinorVersionNumber);
        }

        /// <summary>
        /// Writes the given BLT version number to the output stream.
        /// </summary>
        public void WriteVersion(short MajorVersionNumber, short MinorVersionNumber)
        {
            Writer.Write((int)MajorVersionNumber << 16 | (int)MinorVersionNumber);
        }

        /// <summary>
        /// Writes the contents of a binary loyc file to the current output stream.
        /// </summary>
        /// <param name="Nodes">The top-level nodes to encode.</param>
        public void WriteFileContents(IReadOnlyList<LNode> Nodes)
        {
            var state = new WriterState(LiteralEncoders);
            AddToNodeTable(state, Nodes);

            WriteHeader(state);
            WriteNodeTable(state);
            WriteList(Nodes, node => WriteReference(state, node));
        }

        /// <summary>
        /// Writes the given list of loyc nodes to the current output stream,
        /// encoded as a single binary loyc tree file.
        /// </summary>
        /// <param name="Nodes">The list of nodes to write.</param>
        public void WriteFile(IReadOnlyList<LNode> Nodes)
        {
            WriteMagic();
            WriteVersion();
            WriteFileContents(Nodes);
        }

        #endregion

        /// <summary>
        /// Releases all resource used by the <see cref="Loyc.Binary.LoycBinaryWriter"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Loyc.Binary.LoycBinaryWriter"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Loyc.Binary.LoycBinaryWriter"/> in an unusable state.
        /// After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="Loyc.Binary.LoycBinaryWriter"/> so the garbage collector can reclaim the memory that the
        /// <see cref="Loyc.Binary.LoycBinaryWriter"/> was occupying.</remarks>
        public void Dispose()
        {
            Writer.Dispose();
        }
    }
}
