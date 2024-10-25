using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.Security.Cryptography;

namespace SharedLibrary.Rsa
{
    public class Rsa
    {
        private readonly RSA _rsa;
        public static byte[] SignData(byte[] dataToSign, RSA rsa)
        {
            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(dataToSign);

                    // 对哈希值进行签名 (PKCS#1 v1.5 填充)
                    byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                    return signature;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("ERROR IN SIGNDATA !!!");
            }
        }
        public static byte[] BlockEncrypt(byte[] dataToEncrypt, RSA rsa)
        {
            // RSA block size (PKCS#1 v1.5 padding means we subtract 11 bytes from the key size)
            int chunkSize = rsa.KeySize / 8 - 11;

            int dataLength = dataToEncrypt.Length;
            int numChunks = (int)Math.Ceiling((double)dataLength / chunkSize);

            using (MemoryStream encryptedStream = new MemoryStream())
            {
                for (int i = 0; i < numChunks; i++)
                {
                    byte[] chunk = new byte[Math.Min(chunkSize, dataLength - i * chunkSize)];
                    Array.Copy(dataToEncrypt, i * chunkSize, chunk, 0, chunk.Length);

                    // Encrypt the chunk
                    byte[] encryptedChunk = rsa.Encrypt(chunk, RSAEncryptionPadding.Pkcs1);
                    encryptedStream.Write(encryptedChunk, 0, encryptedChunk.Length);
                }

                return encryptedStream.ToArray();
            }
        }
        public static byte[] BlockRsaDecrypt(byte[] dataToDecrypt, RSA rsa)
        {
            int chunkSize = rsa.KeySize / 8;
            int dataLength = dataToDecrypt.Length;
            int numChunks = dataLength / chunkSize;

            using (MemoryStream decryptedStream = new MemoryStream())
            {
                for (int i = 0; i < numChunks; i++)
                {
                    byte[] chunk = new byte[chunkSize];
                    Array.Copy(dataToDecrypt, i * chunkSize, chunk, 0, chunkSize);

                    byte[] decryptedChunk = rsa.Decrypt(chunk, RSAEncryptionPadding.Pkcs1);
                    decryptedStream.Write(decryptedChunk, 0, decryptedChunk.Length);
                }

                return decryptedStream.ToArray();
            }
        }
        public static RSA GetPrivateKeyFromPem(string pemFilePath)
        {
            try
            {
                using (var reader = File.OpenText(pemFilePath))
                {
                    var pemReader = new PemReader(reader);
                    var pemObject = pemReader.ReadObject();

                    // 检查是否为PKCS#1格式的私钥
                    if (pemObject is RsaPrivateCrtKeyParameters privateKeyParams)
                    {
                        return ConvertPrivateKey(privateKeyParams);
                    }
                    else if (pemObject is AsymmetricCipherKeyPair keyPair && keyPair.Private is RsaPrivateCrtKeyParameters privateParams)
                    {
                        return ConvertPrivateKey(privateParams);
                    }
                    else
                    {
                        // 检测证书格式
                        string detectedFormat = DetectKeyFormat(pemObject);

                        throw new ArgumentException($"不支持的密钥格式：{detectedFormat}。仅支持PKCS#1格式的私钥。");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"读取私钥时出错：{ex.Message}");
            }
        }


        public static RSA GetPublicKeyFromPem(string pemFilePath)
        {
            try
            {
                using (var reader = File.OpenText(pemFilePath))
                {
                    var pemReader = new PemReader(reader);
                    var pemObject = pemReader.ReadObject();

                    // 检查是否为PKCS#1格式的公钥
                    if (pemObject is RsaKeyParameters publicKeyParams && !publicKeyParams.IsPrivate)
                    {
                        return ConvertPublicKey(publicKeyParams);
                    }
                    else
                    {
                        // 检测证书格式
                        string detectedFormat = DetectKeyFormat(pemObject);

                        throw new ArgumentException($"不支持的密钥格式：{detectedFormat}。仅支持PKCS#1格式的公钥。");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"读取公钥时出错：{ex.Message}");
            }
        }


        // 辅助方法，用于检测密钥格式
        private static string DetectKeyFormat(object pemObject)
        {
            if (pemObject is RsaPrivateCrtKeyParameters)
            {
                return "PKCS#1 私钥";
            }
            else if (pemObject is AsymmetricCipherKeyPair keyPair)
            {
                if (keyPair.Private is RsaPrivateCrtKeyParameters)
                {
                    return "PKCS#1 私钥";
                }
                else
                {
                    return "非PKCS#1格式的私钥（可能是PKCS#8）";
                }
            }
            else if (pemObject is RsaKeyParameters keyParameters)
            {
                if (keyParameters.IsPrivate)
                    return "PKCS#1 私钥";
                else
                    return "PKCS#1 公钥";
            }
            else if (pemObject is AsymmetricKeyParameter keyParameter)
            {
                if (keyParameter.IsPrivate)
                    return "非PKCS#1格式的私钥";
                else
                    return "非PKCS#1格式的公钥";
            }
            else
            {
                return "未知格式";
            }
        }



        private static RSA ConvertPrivateKey(RsaPrivateCrtKeyParameters privateKeyParams)
        {
            var rsaParams = new RSAParameters
            {
                Modulus = privateKeyParams.Modulus.ToByteArrayUnsigned(),
                Exponent = privateKeyParams.PublicExponent.ToByteArrayUnsigned(),
                D = privateKeyParams.Exponent.ToByteArrayUnsigned(),
                P = privateKeyParams.P.ToByteArrayUnsigned(),
                Q = privateKeyParams.Q.ToByteArrayUnsigned(),
                DP = privateKeyParams.DP.ToByteArrayUnsigned(),
                DQ = privateKeyParams.DQ.ToByteArrayUnsigned(),
                InverseQ = privateKeyParams.QInv.ToByteArrayUnsigned()
            };

            var rsa = RSA.Create();
            rsa.ImportParameters(rsaParams);
            return rsa;
        }

        private static RSA ConvertPublicKey(RsaKeyParameters publicKeyParams)
        {
            var rsaParams = new RSAParameters
            {
                Modulus = publicKeyParams.Modulus.ToByteArrayUnsigned(),
                Exponent = publicKeyParams.Exponent.ToByteArrayUnsigned()
            };

            var rsa = RSA.Create();
            rsa.ImportParameters(rsaParams);
            return rsa;
        }
    }
}
