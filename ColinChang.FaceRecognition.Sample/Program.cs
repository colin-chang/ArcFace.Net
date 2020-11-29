using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ColinChang.FaceRecognition.Models;
using Microsoft.Extensions.Configuration;

namespace ColinChang.FaceRecognition.Sample
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //读取配置
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var options = new FaceRecognitionOptions();
            config.Bind(nameof(FaceRecognitionOptions), options);


            //测试图片
            const string test = "Images/test.jpg";
            const string zys = "Images/zys.jpg";
            const string xy = "Images/xy.jpg";
            const string xy1 = "Images/xy1.jpg";


            using IFaceRecognizer faceRecognizer = new FaceRecognizer(options);

            /*
            //获取激活消息
            var info = await faceRecognizer.GetActiveFileInfoAsync();
            // 获取SDK信息
            var version = await faceRecognizer.GetSdkVersionAsync();
            // 人脸检测
            var faces = await faceRecognizer.DetectFaceAsync(test);
            // 获取年龄
            var ages = await faceRecognizer.GetAgeAsync(test);
            // 获取性别
            var genders = await faceRecognizer.GetGenderAsync(test);
            // 获取3D角度
            var face3DAngleInfo = await faceRecognizer.GetFace3DAngleAsync(test);

            // 提取人脸特征
            var features0 = await faceRecognizer.ExtractFaceFeatureAsync(zys);
            var features1 = await faceRecognizer.ExtractFaceFeatureAsync(xy);
            
            // 人脸比对
            var result = await faceRecognizer.CompareFaceFeatureAsync(features0.Data.Single(), features1.Data.Single());
            
            //RGB活体检测
            var liveness= await faceRecognizer.GetLivenessInfoAsync(Image.FromFile(zys), LivenessMode.RGB);
            //IR活体检测
            liveness= await faceRecognizer.GetLivenessInfoAsync(Image.FromFile(zys), LivenessMode.IR);
            */

            // 初始化人脸库
            await faceRecognizer.InitFaceLibraryAsync(new[] {zys, xy});
            // 搜索人脸库
            var res = await faceRecognizer.SearchFaceAsync(xy1);


            Console.ReadKey();
        }
    }
}