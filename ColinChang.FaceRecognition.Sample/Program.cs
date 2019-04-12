using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ColinChang.FaceRecognition.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var appId = ConfigurationManager.Core.ConfigurationManager.Configuration["AppId"];
            var fdKeY = ConfigurationManager.Core.ConfigurationManager.Configuration["FdKeY"];
            var frKeY = ConfigurationManager.Core.ConfigurationManager.Configuration["FrKeY"];
            var image = "test.jpg";
            using (var fr = new Recognizer(appId, fdKeY, frKeY))
            {
                fr.Register("FaceLibrary");

                var results = fr.Compare(image, 0.5f);
                if (results != null && results.Any())
                {
                    Console.WriteLine($"Image:{image}");
                    foreach (var feature in results.Keys)
                    {
                        Console.WriteLine($"SerialNo:{feature.SerialNo}\tX:{feature.Position.X}\tY:{feature.Position.Y}\tWidth:{feature.Position.Width}\tHeight:{feature.Position.Height}");
                        var res = results[feature];
                        var cr = res.FirstOrDefault(kv => kv.Value >= res.Max(kva => kva.Value));
                        var img = Path.GetFileNameWithoutExtension(cr.Key);
                        var key = Path.GetFileNameWithoutExtension(img);
                        Console.WriteLine($"To:{key}\tSimilarity:{cr.Value}\r\n");
                    }
                }
            }

            Console.ReadKey();
        }
    }
}