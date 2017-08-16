using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Testers
{
    public static class ForestLoader
    {
        public static DecisionTree[] FromDirectory(string forestDirectory)
        {
            BinaryFormatter bf = new BinaryFormatter();
            string[] treePaths = Directory.GetFiles(forestDirectory, "*.dat");
            DecisionTree[] forest = new DecisionTree[treePaths.Length];
            for (int c = 0; c < treePaths.Length; c++)
            {
                string treePath = treePaths[c];
                using (FileStream fin = File.OpenRead(treePath))
                {
                    forest[c] = (DecisionTree)bf.Deserialize(fin);
                }
            }
            return forest;
        }
    }
}
