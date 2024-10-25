using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class Xor
    {
        public static byte[] XorEncryptDecrypt(byte[] text, byte[] binaryKey)
        {
            byte[] result = new byte[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                // 对输入字节和密钥字节进行异或操作
                result[i] = (byte)(text[i] ^ binaryKey[i % binaryKey.Length]);
            }

            return result;
        }
    }
}
