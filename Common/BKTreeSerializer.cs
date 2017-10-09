using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class BKTreeSerializer
    {
        private static void RecursivelySerialize(BKTree parentNode,
            BinaryWriter fout)
        {
            if (parentNode != null)
            {
                fout.Write(parentNode.Index);
                fout.Write(parentNode.StringValue);
                int childLength = parentNode.Children.Length;
                fout.Write(childLength);
                for (int c = 0; c < parentNode.Children.Length; c++)
                {
                    fout.Write(parentNode.Children[c] != null);
                }
                for (int c = 0; c < parentNode.Children.Length; c++)
                {
                    BKTree child = parentNode.Children[c];
                    if (child != null)
                    {
                        RecursivelySerialize(child, fout);
                    }
                }
            }
        }

        private static void RecursivelyDeserialize(BKTree parent, BinaryReader fin)
        {
            int index = fin.ReadInt32();
            string stringValue = fin.ReadString();
            int childLength = fin.ReadInt32();

            parent.Index = index;
            parent.StringValue = stringValue;
            parent.Children = new BKTree[childLength];

            for (int c = 0; c < childLength; c++)
            {
                if (fin.ReadBoolean())
                {
                    parent.Children[c] = new BKTree();
                }
            }

            for (int c = 0; c < childLength; c++)
            {
                if (parent.Children[c] != null)
                {
                    RecursivelyDeserialize(parent.Children[c], fin);
                }
            }
        }

        public static void SerializeTo(BKTree tree, string outputPath)
        {
            using (FileStream fout = File.Create(outputPath))
            {
                using (BinaryWriter bw = new BinaryWriter(fout))
                {
                    RecursivelySerialize(tree, bw);
                }
            }
        }

        public static BKTree DeserializeFrom(string file)
        {
            BKTree ret = new BKTree();

            using (FileStream fin = File.OpenRead(file))
            {
                using (BinaryReader br = new BinaryReader(fin))
                {
                    RecursivelyDeserialize(ret, br);
                }
            }

            return ret;
        }
    }
}
