using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Syntax;

namespace Loyc.Binary
{    
    /// <summary>
    /// Defines a mutable view of a binary encoded loyc tree's header,
    /// </summary>
    public class WriterState
    {
        /// <summary>
        /// Creates a new mutable binary encoded loyc tree header.
        /// </summary>
        /// <param name="LiteralEncoders">A mapping of literal types to binary node encoders.</param>
        public WriterState(IReadOnlyDictionary<Type, BinaryNodeEncoder> LiteralEncoders)
        {
            this.LiteralEncoders = LiteralEncoders;
            this.stringTable = new Dictionary<string, int>();
            this.stringList = new List<string>();
            this.templates = new List<NodeTemplate>();
            this.templateTable = new Dictionary<NodeTemplate, int>();
            this.nodes = new List<Pair<NodeEncodingType, IReadOnlyList<LNode>>>();
            this.nodeTable = new Dictionary<LNode, int>();
        }

        /// <summary>
        /// Gets the set of literal node encoders this writer uses.
        /// </summary>
        public IReadOnlyDictionary<Type, BinaryNodeEncoder> LiteralEncoders { get; private set; }

        /// <summary>
        /// Gets the encoded loyc tree's symbol table.
        /// </summary>
        public IReadOnlyList<string> Symbols { get { return stringList; } }

        private Dictionary<string, int> stringTable;
        private List<string> stringList;

        /// <summary>
        /// Gets the encoded loyc tree's list of templates.
        /// </summary>
        public IReadOnlyList<NodeTemplate> Templates { get { return templates; } }

        private Dictionary<NodeTemplate, int> templateTable;
        private List<NodeTemplate> templates;

        /// <summary>
        /// Gets the encoded loyc tree's list of nodes.
        /// </summary>
        public IReadOnlyList<Pair<NodeEncodingType, IReadOnlyList<LNode>>> Nodes
        {
            get { return nodes; }
        }

        private Dictionary<LNode, int> nodeTable;
        private List<Pair<NodeEncodingType, IReadOnlyList<LNode>>> nodes;

        private static int GetOrAddIndex<T>(T Value, Dictionary<T, int> Table, List<T> Items)
        {
            int result;
            if (Table.TryGetValue(Value, out result))
            {
                return result;
            }
            else
            {
                int index = Table.Count;
                Items.Add(Value);
                Table[Value] = index;
                return index;
            }
        }

        /// <summary>
        /// Gets a symbol's index in the string table.
        /// The given value is added to the table
        /// if it's not already in there.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public int GetIndex(Symbol Value)
        {
            return GetIndex(Value.Name);
        }

        /// <summary>
        /// Gets a string's index in the string table.
        /// The given value is added to the table
        /// if it's not already in there.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public int GetIndex(string Value)
        {
            return GetOrAddIndex(Value, stringTable, stringList);
        }

        /// <summary>
        /// Gets a template's index in the template table.
        /// The given value is added to the table
        /// if it's not already in there.
        /// </summary>
        /// <param name="Template"></param>
        /// <returns></returns>
        public int GetIndex(NodeTemplate Template)
        {
            return GetOrAddIndex(Template, templateTable, templates);
        }

        /// <summary>
        /// Gets a node's index in the node table. The given node is
        /// added to the table if it hasn't been added already.
        /// </summary>
        /// <param name="Node">A node.</param>
        /// <returns>The index of the node in the table.</returns>
        public int GetIndex(LNode Node)
        {
            int result;
            if (nodeTable.TryGetValue(Node, out result))
            {
                return result;
            }
            else
            {
                // Add child nodes (attributes, target, arguments) to the node table
                // before adding their parent node.
                if (Node.HasAttrs)
                {
                    foreach (var attr in Node.Attrs)
                    {
                        GetIndex(attr);
                    }
                    GetIndex(Node.WithoutAttrs());
                }
                else if (Node.IsCall)
                {
                    if (Node.Target.IsId && !Node.Target.HasAttrs)
                    {
                        GetIndex(Node.Name);
                    }
                    else
                    {
                        GetIndex(Node.Target);
                    }
                    foreach (var arg in Node.Args)
                    {
                        GetIndex(arg);
                    }
                }
                else if (Node.IsId)
                {
                    GetIndex(Node.Name);
                }
                else if (Node.IsLiteral && Node.Value is string)
                {
                    GetIndex((string)Node.Value);
                }

                var encoder = GetEncoder(Node);
                var template = encoder.GetTemplate(this, Node);
                if (template != null)
                {
                    GetIndex(template);
                }

                int index = nodeTable.Count;
                GetNodeTable(encoder).Add(Node);
                nodeTable[Node] = index;
                return index;
            }
        }

        private List<LNode> GetNodeTable(BinaryNodeEncoder Encoder)
        {
            if (nodes.Count == 0
                || nodes[nodes.Count - 1].Item1 != Encoder.EncodingType)
            {
                var newTable = new List<LNode>();
                nodes.Add(new Pair<NodeEncodingType, IReadOnlyList<LNode>>(Encoder.EncodingType, newTable));
                return newTable;
            }
            else
            {
                return (List<LNode>)nodes[nodes.Count - 1].Item2;
            }
        }

        /// <summary>
        /// Gets a node encoder for the given node.
        /// </summary>
        /// <param name="Node">The node to encode.</param>
        /// <returns>A binary node encoder.</returns>
        public BinaryNodeEncoder GetEncoder(LNode Node)
        {
            if (Node.HasAttrs)
            {
                return BinaryNodeEncoder.AttributeEncoder;
            }
            else if (Node.IsCall)
            {
                if (Node.Target.IsId && !Node.Target.HasAttrs)
                {
                    return BinaryNodeEncoder.CallIdEncoder;
                }
                else
                {
                    return BinaryNodeEncoder.CallEncoder;
                }
            }
            else if (Node.IsId)
            {
                return BinaryNodeEncoder.IdEncoder;
            }
            else
            {
                object nodeVal = Node.Value;
                if (nodeVal == null)
                {
                    return BinaryNodeEncoder.NullEncoder;
                }
                else
                {
                    BinaryNodeEncoder result;
                    if (LiteralEncoders.TryGetValue(nodeVal.GetType(), out result))
                    {
                        return result;
                    }
                    else
                    {
                        throw new NotSupportedException(
                            "No suitable encoder for node '" + Node.Print() + "'.");
                    }
                }
            }
        }
    }
}
