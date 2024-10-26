using System.Collections;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace WatermarkImage
{
    public class DarkWatermarkGenerator
    {
        public static Image<Rgba32> GenerateImageWithWatermark(int width, int height, string watermarkData)
        {
            // 创建一个随机图像（灰度）
            var image = new Image<Rgba32>(width, height);
            Random rand = new Random();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte gray = (byte)rand.Next(100, 200);  // 随机灰度值
                    image[x, y] = new Rgba32(gray, gray, gray);
                }
            }

            // 将水印数据转换为比特数组
            byte[] watermarkBytes = Encoding.UTF8.GetBytes(watermarkData);
            BitArray watermarkBits = new BitArray(watermarkBytes);

            // 将水印嵌入到图像的特定像素位置
            int bitIndex = 0;
            for (int x = 0; x < width && bitIndex < watermarkBits.Length; x++)
            {
                for (int y = 0; y < height && bitIndex < watermarkBits.Length; y++)
                {
                    Rgba32 originalColor = image[x, y];
                    byte lsb = watermarkBits[bitIndex] ? (byte)1 : (byte)0; // 获取水印比特
                    byte modifiedGray = (byte)((originalColor.R & ~1) | lsb);  // 修改灰度值的最低位

                    // 更新像素值，嵌入水印
                    image[x, y] = new Rgba32(modifiedGray, modifiedGray, modifiedGray);
                    bitIndex++;
                }
            }

            return image;
        }
    }
}
