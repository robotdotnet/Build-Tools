using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Md5Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Md5Sum(args[0]));
        }

        //Grab the MD5 sum of an existing file
        private static string Md5Sum(string fileName)
        {
            byte[] fileMd5Sum = null;


            if (File.Exists(fileName))
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Open))
                {
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        fileMd5Sum = md5.ComputeHash(stream);
                    }
                }
            }

            if (fileMd5Sum == null)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            foreach (var b in fileMd5Sum)
            {
                builder.Append(b);
            }
            return builder.ToString();
        }
    }
}
