using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    static class Serializer
    {
        public static T Deserialize<T>(string filePath)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fstream = new FileStream(filePath, FileMode.Open))
            {
                return (T)formatter.Deserialize(fstream);
            }
        }

        public static void Serialize<T>(T toSerialize, string filePath)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fstream = new FileStream(filePath, FileMode.Create))
            {
                formatter.Serialize(fstream, toSerialize);
            }
        }
    }
}
